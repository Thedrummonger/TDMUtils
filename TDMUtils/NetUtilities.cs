using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.Serialization;
using System.Text;

namespace TDMUtils
{
    public static class NetUtilities
    {
        private static int DefaultNetworkPort = 35251;

        /// <summary>
        /// Sets the global default port used when parsing addresses that do not
        /// explicitly specify one.
        /// </summary>
        /// <param name="port">Port number to use as the default.</param>
        public static void SetGlobalDefaultPort(int port) => DefaultNetworkPort = port;

        /// <summary>
        /// Parses a host/IP string in the form "host" or "host:port".
        /// </summary>
        /// <param name="input">
        /// Address string to parse. May contain only a host/IP or a host/IP and port.
        /// </param>
        /// <returns>
        /// A tuple containing the parsed host/IP and port. If the input is null or
        /// whitespace, returns (null, 0).
        /// </returns>
        /// <remarks>
        /// If no port is specified or parsing fails, the current global default port
        /// is used.
        /// </remarks>
        public static (string? Ip, int Port) ParseIpAddress(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return (null, 0);

            var parts = input.Split(':');
            string ip = parts[0];

            int port = parts.Length > 1 && int.TryParse(parts[1], out var parsedPort)
                ? parsedPort
                : DefaultNetworkPort;

            return (ip, port);
        }
    }
    /// <summary>
    /// Lightweight WebSocket client for sending and receiving packets of a single type.
    /// </summary>
    /// <typeparam name="T">
    /// Packet type transmitted by the client.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="SimpleWebClient{T}"/> is intended as a minimal transport layer for
    /// applications that want straightforward send/receive behavior without a large
    /// networking framework.
    /// </para>
    /// <para>
    /// The client does not impose packet structure or application semantics. Any
    /// higher-level behavior, such as request/response handling, acknowledgements,
    /// or packet routing, is expected to be implemented by application code or by
    /// optional helper utilities built on top of this client.
    /// </para>
    /// <para>
    /// All traffic sent and received by a given instance uses the same packet type.
    /// </para>
    /// </remarks>
    public sealed class SimpleWebClient<T> : IDisposable
    {
        private readonly Uri _uri;
        private ClientWebSocket? _ws;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public event Action? ServerConnectionEstablished;
        public event Action? ServerConnectionLost;
        public event Action<T>? PacketReceived;

        public string Address => _uri.OriginalString;

        public SimpleWebClient(Uri uri) => _uri = uri;
        public SimpleWebClient(string address)
        {
            var parsed = NetUtilities.ParseIpAddress(address);
            _uri = new Uri($"ws://{parsed.Ip}:{parsed.Port}/");
        }

        public async Task<bool> ConnectAsync()
        {
            if (_ws != null && _ws.State == WebSocketState.Open)
                return true;

            _ws = new ClientWebSocket();

            try
            {
                await _ws.ConnectAsync(_uri, CancellationToken.None).ConfigureAwait(false);
                ServerConnectionEstablished?.Invoke();
                _ = Task.Run(ReceiveLoopAsync);
                return true;
            }
            catch
            {
                _ws.Dispose();
                _ws = null;
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_ws == null)
                return;

            try
            {
                if (_ws.State == WebSocketState.Open)
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None).ConfigureAwait(false);
            }
            catch { }

            _ws.Dispose();
            _ws = null;

            ServerConnectionLost?.Invoke();
        }

        public async Task SendAsync(T packet)
        {
            if (_ws?.State != WebSocketState.Open) return;
            var bytes = PacketCodec.Encode(packet);
            await PacketCodec.SendBinaryAsync(_ws, bytes, _cts.Token).ConfigureAwait(false);
        }

        private async Task ReceiveLoopAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested && _ws.State == WebSocketState.Open)
                {
                    var msg = await PacketCodec.ReceiveFullBinaryMessageAsync(_ws, _cts.Token).ConfigureAwait(false);
                    if (msg == null) break;

                    var packet = PacketCodec.Decode<T>(msg);
                    PacketReceived?.Invoke(packet);
                }
            }
            finally
            {
                ServerConnectionLost?.Invoke();
                try { _ws?.Dispose(); } catch { }
            }
        }

        public void Dispose()
        {
            try { _cts.Cancel(); } catch { }
            try { _ws?.Abort(); } catch { }
            try { _ws?.Dispose(); } catch { }
            try { _cts.Dispose(); } catch { }
        }
    }
    /// <summary>
    /// Lightweight WebSocket server for sending and receiving packets of a single type.
    /// </summary>
    /// <typeparam name="T">
    /// Packet type transmitted by the server and its connected clients.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// <see cref="SimpleWebServer{T}"/> is intended as a minimal transport layer for
    /// applications that want simple client connection management and packet exchange
    /// without a large networking framework.
    /// </para>
    /// <para>
    /// The server does not enforce application-level protocol rules. Features such as
    /// request/response routing, validation, acknowledgements, or custom packet handling
    /// are expected to be implemented by application code or by optional helper utilities
    /// layered on top of the server.
    /// </para>
    /// <para>
    /// All traffic sent and received by a given instance uses the same packet type.
    /// </para>
    /// </remarks>
    public sealed class SimpleWebServer<T> : IDisposable
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new ConcurrentDictionary<Guid, WebSocket>();

        private readonly string _prefix;
        private readonly int _maxClients;

        public event Action<Guid>? ClientConnect;
        public event Action<Guid>? ClientDisconnect;
        public event Action<Guid, T>? PacketReceived;

        public string Address => _prefix;
        public SimpleWebServer(Uri uri, int maxClients = 1)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));

            var scheme = uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? uri.Scheme : "http";
            var portPart = uri.IsDefaultPort ? "" : ":" + uri.Port;

            _prefix = $"{scheme}://{uri.Host}{portPart}/";
            _maxClients = maxClients;
        }
        public SimpleWebServer(string address, int maxClients = 1)
        {
            var parsed = NetUtilities.ParseIpAddress(address);
            _prefix = $"http://{parsed.Ip}:{parsed.Port}/";
            _maxClients = maxClients;
        }

        public void Start()
        {
            _listener.Prefixes.Add(_prefix);
            _listener.Start();
            _ = Task.Run(AcceptLoopAsync);
        }

        public void Broadcast(T packet, params Guid[] targets)
        {
            var bytes = PacketCodec.Encode(packet);
            var set = targets.Length == 0 ? null : new HashSet<Guid>(targets);

            foreach (var ws in _clients.Where(x => set == null || set.Contains(x.Key)).Select(x => x.Value))
            {
                if (ws == null || ws.State != WebSocketState.Open) continue;
                _ = PacketCodec.SendBinaryAsync(ws, bytes, _cts.Token); // fire-and-forget
            }
        }

        private async Task AcceptLoopAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                HttpListenerContext ctx = null;

                try { ctx = await _listener.GetContextAsync().ConfigureAwait(false); }
                catch { break; }

                if (ctx == null) continue;

                if (!ctx.Request.IsWebSocketRequest)
                {
                    ctx.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    ctx.Response.Close();
                    continue;
                }

                if (_clients.Count >= _maxClients)
                {
                    ctx.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                    ctx.Response.Close();
                    continue;
                }

                HttpListenerWebSocketContext? wsCtx = null;
                try { wsCtx = await ctx.AcceptWebSocketAsync(subProtocol: null).ConfigureAwait(false); }
                catch
                {
                    try { ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError; ctx.Response.Close(); } catch { }
                    continue;
                }

                var id = Guid.NewGuid();
                var ws = wsCtx.WebSocket;
                _clients[id] = ws;

                ClientConnect?.Invoke(id);
                _ = Task.Run(() => ClientLoopAsync(id, ws));
            }
        }

        private async Task ClientLoopAsync(Guid id, WebSocket ws)
        {
            try
            {
                while (!_cts.IsCancellationRequested && ws.State == WebSocketState.Open)
                {
                    var msg = await PacketCodec.ReceiveFullBinaryMessageAsync(ws, _cts.Token).ConfigureAwait(false);
                    if (msg == null) break;

                    var packet = PacketCodec.Decode<T>(msg);
                    PacketReceived?.Invoke(id, packet);
                }
            }
            catch { }
            finally
            {
                _clients.TryRemove(id, out WebSocket? removed);

                try { ws.Abort(); } catch { }
                try { ws.Dispose(); } catch { }

                ClientDisconnect?.Invoke(id);
            }
        }

        public void Stop()
        {
            try { _cts.Cancel(); } catch { }
            try { _listener.Stop(); } catch { }

            foreach (var kvp in _clients.ToArray())
            {
                try { kvp.Value?.Abort(); } catch { }
                try { kvp.Value?.Dispose(); } catch { }
            }

            _clients.Clear();
        }

        public void Dispose()
        {
            Stop();
            try { _listener.Close(); } catch { }
            try { _cts.Dispose(); } catch { }
        }
    }
    internal static class PacketCodec
    {
        static readonly JsonSerializerSettings Settings = new()
        {
            TypeNameHandling = TypeNameHandling.None,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Error
        };

        public static byte[] Encode<T>(T packet)
        {
            var json = JsonConvert.SerializeObject(packet, Settings);
            var raw = Encoding.UTF8.GetBytes(json);

            using var gzMs = new MemoryStream();
            using (var gz = new GZipStream(gzMs, CompressionLevel.Optimal, leaveOpen: true))
                gz.Write(raw, 0, raw.Length);

            return gzMs.ToArray();
        }

        public static T Decode<T>(byte[] bytes)
        {
            using var gzMs = new MemoryStream(bytes);
            using var gz = new GZipStream(gzMs, CompressionMode.Decompress);
            using var sr = new StreamReader(gz, Encoding.UTF8);
            var json = sr.ReadToEnd();

            return JsonConvert.DeserializeObject<T>(json, Settings)!;
        }
        public static async Task<byte[]?> ReceiveFullBinaryMessageAsync(WebSocket ws, CancellationToken ct)
        {
            var buffer = new byte[16 * 1024];

            using var ms = new MemoryStream();
            while (true)
            {
                var seg = new ArraySegment<byte>(buffer);
                var result = await ws.ReceiveAsync(seg, ct).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                    return null;

                if (result.MessageType != WebSocketMessageType.Binary)
                    continue;

                if (result.Count > 0)
                    ms.Write(buffer, 0, result.Count);

                if (result.EndOfMessage)
                    return ms.ToArray();
            }
        }

        public static Task SendBinaryAsync(WebSocket ws, byte[] payload, CancellationToken ct) =>
            ws.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Binary, endOfMessage: true, cancellationToken: ct);
    }

    public static class WebClientExtensions
    {
        /// <summary>
        /// Sends a request packet and asynchronously waits for a matching response.
        /// </summary>
        /// <typeparam name="T">
        /// Packet type implementing <see cref="ISimpleWebPacket"/> and providing a
        /// parameterless constructor.
        /// </typeparam>
        /// <param name="client">The client used to send the request.</param>
        /// <param name="requestType">User-defined request type identifier.</param>
        /// <param name="transform">
        /// Optional callback invoked before sending, allowing additional fields or
        /// sub-packets to be populated.
        /// </param>
        /// <param name="timeout">
        /// Optional maximum time to wait for a response. If omitted, waits indefinitely.
        /// </param>
        /// <returns>
        /// The first received packet whose <see cref="SimpleRequestInfo"/> matches the
        /// request identifier and type. If a timeout occurs, a new packet containing
        /// error metadata is returned.
        /// </returns>
        /// <remarks>
        /// The method does not throw for protocol errors or timeouts. All outcomes are
        /// reported via the returned packet's <see cref="SimpleRequestInfo"/>.
        /// </remarks>
        public static async Task<T> RequestAsync<T>(this SimpleWebClient<T> client, string requestType, Action<T>? transform = null, TimeSpan? timeout = null)
            where T : class, ISimpleWebPacket, new()
        {
            var packet = new T { RequestInfo = SimpleRequestInfo.CreateRequest(requestType) };
            var requestId = packet.RequestInfo!.Id!;

            transform?.Invoke(packet);

            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

            void Handler(T incoming)
            {
                var info = incoming?.RequestInfo;
                if (info == null) return;
                if (!info.IsResponse) return;
                if (!string.Equals(info.Id, requestId, StringComparison.Ordinal)) return;
                if (!string.Equals(info.Type, requestType, StringComparison.Ordinal)) return;

                tcs.TrySetResult(incoming!);
            }

            client.PacketReceived += Handler;

            try
            {
                await client.SendAsync(packet).ConfigureAwait(false);

                if (timeout == null)
                    return await tcs.Task.ConfigureAwait(false);

                var completed = await Task.WhenAny(tcs.Task, Task.Delay(timeout.Value)).ConfigureAwait(false);

                if (completed != tcs.Task)
                    return new T { RequestInfo = SimpleRequestInfo.CreateError(requestId, requestType, "Request timed out.") };

                return await tcs.Task.ConfigureAwait(false);
            }
            finally
            {
                client.PacketReceived -= Handler;
            }
        }

        /// <summary>
        /// Determines whether the packet contains request/response metadata.
        /// </summary>
        /// <typeparam name="T">Packet type implementing <see cref="ISimpleWebPacket"/>.</typeparam>
        /// <param name="packet">The packet to inspect.</param>
        /// <returns>
        /// <c>true</c> if <see cref="ISimpleWebPacket.RequestInfo"/> is present; otherwise <c>false</c>.
        /// </returns>
        public static bool HasRequestInfo<T>(this T packet)
            where T : class, ISimpleWebPacket
            => packet.RequestInfo != null;

        /// <summary>
        /// Determines whether the packet represents a request.
        /// </summary>
        /// <typeparam name="T">Packet type implementing <see cref="ISimpleWebPacket"/>.</typeparam>
        /// <param name="packet">The packet to inspect.</param>
        /// <returns>
        /// <c>true</c> if the packet contains request metadata and is not marked as a response;
        /// otherwise <c>false</c>.
        /// </returns>
        public static bool IsRequest<T>(this T packet)
            where T : class, ISimpleWebPacket
            => packet.RequestInfo?.IsResponse == false;

        /// <summary>
        /// Determines whether the packet represents a response.
        /// </summary>
        /// <typeparam name="T">Packet type implementing <see cref="ISimpleWebPacket"/>.</typeparam>
        /// <param name="packet">The packet to inspect.</param>
        /// <returns>
        /// <c>true</c> if the packet contains request metadata marked as a response;
        /// otherwise <c>false</c>.
        /// </returns>
        public static bool IsResponse<T>(this T packet)
            where T : class, ISimpleWebPacket
            => packet.RequestInfo?.IsResponse == true;
    }

    /// <summary>
    /// Optional marker interface for packets that support SimpleWeb request/response helpers.
    /// </summary>
    /// <remarks>
    /// Implementing this interface allows the packet to participate in helper features
    /// such as request/response routing, timeouts, and automatic replies.
    ///
    /// Packets are not required to implement this interface. Packets without it are
    /// treated as ordinary messages by the transport layer.
    ///
    /// The <see cref="RequestInfo"/> property contains only lightweight metadata used
    /// for correlating requests and responses. Application code remains fully responsible
    /// for interpreting packet contents and handling behavior.
    /// </remarks>
    public interface ISimpleWebPacket
    {
        /// <summary>
        /// Optional request/response metadata associated with this packet.
        /// </summary>
        /// <remarks>
        /// When null, the packet is treated as a normal message with no request semantics.
        /// When present, helper utilities may use this information to correlate requests
        /// and responses or generate replies.
        /// </remarks>
        public SimpleRequestInfo? RequestInfo { get; set; }
    }

    /// <summary>
    /// Lightweight metadata describing a request/response interaction for packets
    /// implementing <see cref="ISimpleWebPacket"/>.
    /// </summary>
    /// <remarks>
    /// This object contains only correlation and status information. It does not
    /// impose any transport or application semantics.
    ///
    /// A packet may include this information to participate in helper features
    /// such as request/response routing, timeouts, or automatic replies. Packets
    /// without this information are treated as ordinary messages.
    ///
    /// Error state is inferred from <see cref="ErrorMessage"/> being non-null.
    /// </remarks>
    public sealed class SimpleRequestInfo
    {
        /// <summary>
        /// Unique identifier for the logical request.
        /// </summary>
        /// <remarks>
        /// For requests, this value is generated by the sender.
        /// For responses, this must match the originating request's Id.
        /// </remarks>
        public string? Id { get; set; }

        /// <summary>
        /// User-defined request type identifier.
        /// </summary>
        /// <remarks>
        /// This value is not interpreted by the helper library and may be used
        /// for routing or dispatch by application code.
        /// </remarks>
        public string? Type { get; set; }

        /// <summary>
        /// Indicates whether this packet represents a response to a request.
        /// </summary>
        /// <remarks>
        /// When <c>false</c>, the packet is treated as a request.
        /// When <c>true</c>, the packet is treated as a response.
        /// </remarks>
        public bool IsResponse { get; set; }

        /// <summary>
        /// Optional error description associated with the response.
        /// </summary>
        /// <remarks>
        /// A non-null value indicates that the response represents a failure
        /// condition. Interpretation of the message is left to application code.
        /// </remarks>
        public string? ErrorMessage { get; set; }

        public bool IsError() => ErrorMessage != null;

        /// <summary>
        /// Creates a new request metadata instance with a generated identifier.
        /// </summary>
        /// <param name="type">User-defined request type.</param>
        /// <returns>A populated <see cref="SimpleRequestInfo"/> representing a request.</returns>
        public static SimpleRequestInfo CreateRequest(string type) =>
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Type = type,
                IsResponse = false,
                ErrorMessage = null
            };

        /// <summary>
        /// Creates metadata for a successful response corresponding to a request.
        /// </summary>
        /// <param name="id">Identifier of the original request.</param>
        /// <param name="type">Request type being answered.</param>
        /// <returns>A populated <see cref="SimpleRequestInfo"/> representing a response.</returns>
        public static SimpleRequestInfo CreateResponse(string id, string type) =>
            new()
            {
                Id = id,
                Type = type,
                IsResponse = true,
                ErrorMessage = null
            };

        /// <summary>
        /// Creates metadata for an error response corresponding to a request.
        /// </summary>
        /// <param name="id">Identifier of the original request.</param>
        /// <param name="type">Request type being answered.</param>
        /// <param name="message">Optional error description.</param>
        /// <returns>A populated <see cref="SimpleRequestInfo"/> representing a failed response.</returns>
        public static SimpleRequestInfo CreateError(string id, string type, string? message) =>
            new()
            {
                Id = id,
                Type = type,
                IsResponse = true,
                ErrorMessage = message
            };
    }
    /// <summary>
    /// Helper for handling request packets on a <see cref="SimpleWebServer{T}"/>.
    /// </summary>
    /// <typeparam name="T">
    /// Packet type implementing <see cref="ISimpleWebPacket"/> and providing a
    /// parameterless constructor.
    /// </typeparam>
    /// <remarks>
    /// <para>
    /// This router provides a lightweight mechanism for mapping request types to
    /// handler callbacks that produce response packets.
    /// </para>
    /// <para>
    /// It does not automatically subscribe to server events. Callers are expected
    /// to invoke <see cref="TryHandle"/> from their packet processing logic to
    /// determine whether a packet was handled.
    /// </para>
    /// <para>
    /// The router only processes packets containing valid request metadata. All
    /// other packets are ignored and should be handled by application code.
    /// </para>
    /// </remarks>
    /// <remarks>
    /// Initializes a new router associated with the specified server.
    /// </remarks>
    /// <param name="server">Server used to send response packets.</param>
    /// <param name="replyToUnknownRequests">
    /// If true, the router will automatically send an error response when a
    /// request type has no registered handler. If false, unknown requests are
    /// ignored and may be handled elsewhere.
    /// </param>
    public sealed class SimpleServerRequestRouter<T>(SimpleWebServer<T> server, bool replyToUnknownRequests = false) where T : class, ISimpleWebPacket, new()
    {
        private readonly SimpleWebServer<T> _server = server ?? throw new ArgumentNullException(nameof(server));
        private readonly bool _replyToUnknownRequests = replyToUnknownRequests;
        private readonly Dictionary<string, Action<Guid, T, T>> _handlers = new(StringComparer.Ordinal);

        /// <summary>
        /// Registers a handler for a specific request type.
        /// </summary>
        /// <param name="requestType">User-defined request type identifier.</param>
        /// <param name="handler">
        /// Callback invoked when a matching request is received. The handler is
        /// provided the client identifier, the original request packet, and a
        /// preconstructed response packet to populate.
        /// </param>
        /// <remarks>
        /// The handler should modify the supplied response packet rather than
        /// creating a new instance.
        /// </remarks>
        public void Register(string requestType, Action<Guid, T, T> handler)
        {
            if (string.IsNullOrWhiteSpace(requestType))
                throw new ArgumentException("Request type cannot be null or whitespace.", nameof(requestType));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            _handlers[requestType] = handler;
        }

        /// <summary>
        /// Attempts to process the packet as a request.
        /// </summary>
        /// <param name="clientId">Identifier of the client that sent the packet.</param>
        /// <param name="packet">Incoming packet to evaluate.</param>
        /// <returns>
        /// <c>true</c> if the router handled the packet (response sent or consumed);
        /// otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// If the packet contains valid request metadata and a matching handler is
        /// registered, the handler is invoked and a response is sent to the originating
        /// client.
        /// </para>
        /// <para>
        /// If no handler exists and unknown-request replies are enabled, an error
        /// response is sent automatically.
        /// </para>
        /// <para>
        /// Returning <c>false</c> indicates the packet was not processed and may be
        /// handled by other application logic.
        /// </para>
        /// </remarks>
        public bool TryHandle(Guid clientId, T packet)
        {
            var info = packet?.RequestInfo;

            if (info == null || info.IsResponse ||
                string.IsNullOrWhiteSpace(info.Id) ||
                string.IsNullOrWhiteSpace(info.Type))
                return false;

            var response = new T { RequestInfo = SimpleRequestInfo.CreateResponse(info.Id!, info.Type!) };

            if (!_handlers.TryGetValue(info.Type!, out var handler))
            {
                if (!_replyToUnknownRequests)
                    return false;

                response.RequestInfo = SimpleRequestInfo.CreateError(
                    info.Id!,
                    info.Type!,
                    $"Unknown request type '{info.Type}'.");

                _server.Broadcast(response, clientId);
                return true;
            }

            handler(clientId, packet!, response);

            _server.Broadcast(response, clientId);
            return true;
        }
    }


}
