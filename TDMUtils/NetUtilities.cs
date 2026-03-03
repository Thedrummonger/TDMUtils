using System;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net;
using System.Net.WebSockets;
using System.Runtime.Serialization;

namespace TDMUtils
{
    public static class NetUtilities
    {
        private static int DefaultNetworkPort = 443;
        public static void SetGlobalDefaultPort(int port) => DefaultNetworkPort = port;
        public static (string? Ip, int Port) ParseIpAddress(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return (null, 0);
            var parts = input.Split(':');
            string ip = parts[0];
            int port = parts.Length > 1 && int.TryParse(parts[1], out var parsedPort) ? parsedPort : DefaultNetworkPort;
            return (ip, port);
        }
    }

    public sealed class SimpleWebClient<T> : IDisposable
    {
        private readonly Uri _uri;
        private ClientWebSocket? _ws;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public event Action? ServerConnectionEstablished;
        public event Action? ServerConnectionLost;
        public event Action<T>? PacketReceived;

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
            catch {}
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
        public static byte[] Encode<T>(T packet)
        {
            var serializer = new DataContractSerializer(typeof(T));

            using var rawMs = new MemoryStream();
            serializer.WriteObject(rawMs, packet);
            rawMs.Position = 0;

            using var gzMs = new MemoryStream();
            using (var gz = new GZipStream(gzMs, CompressionLevel.Optimal, leaveOpen: true))
                rawMs.CopyTo(gz);

            return gzMs.ToArray();
        }

        public static T Decode<T>(byte[] bytes)
        {
            var serializer = new DataContractSerializer(typeof(T));

            using var gzMs = new MemoryStream(bytes);
            using var gz = new GZipStream(gzMs, CompressionMode.Decompress);
            using var outMs = new MemoryStream();
            gz.CopyTo(outMs);
            outMs.Position = 0;
            return (T)serializer.ReadObject(outMs);
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
}
