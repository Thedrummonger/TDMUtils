using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using TDMUtils;
using TDMUtils.CLITools;
using TDMUtils.Tokenizer;
using static TDMUtils.WebClientExtensions;

namespace TDMUtilsTests
{
    /// <summary>
    /// A collection of tests for the TDMUtils logic utilities.
    /// </summary>
    public static class Tests
    {
        /// <summary>
        /// The entry point for the test suite.
        /// </summary>
        public static void Main()
        {
            ColoredString.AppDeafultTextColor = System.Drawing.Color.White;
            //TestWebServer().GetAwaiter().GetResult();
            //TestApplets();
            DumpAPData().GetAwaiter().GetResult();
        }

        public static async Task DumpAPData()
        {
            Directory.CreateDirectory("Dumps");
            ArchipelagoSession Session = ArchipelagoSessionFactory.CreateSession("");
            LoginResult Result = Session.TryConnectAndLogin("", "", Archipelago.MultiClient.Net.Enums.ItemsHandlingFlags.AllItems, new(0, 6, 1));
            if (Result is LoginFailure failure)
                throw new Exception(string.Join('\n', failure.Errors));

            LoginSuccessful loginSuccessful = (Result as LoginSuccessful)!;
            var SessionInfo = await TDMUtils.Archipelago.MultiClientExtensions.APSeedPlayerData.FromSessionAsync(Session, loginSuccessful.SlotData);
            File.WriteAllText(Path.Combine("Dumps", "SeedData.json"), SessionInfo.ToFormattedJson());

            var RunTimeData = await TDMUtils.Archipelago.MultiClientExtensions.APClientRuntimeData.FromSessionAsync(Session);
            File.WriteAllText(Path.Combine("Dumps", "RuntimeData.json"), RunTimeData.ToFormattedJson());
        }


        public static void TestColoredString()
        {
            ColoredString coloredString = new ColoredString();
            coloredString.AddText("This").AddText("is", System.Drawing.Color.Red).AddText("A", System.Drawing.Color.Blue).AddText("Colored").AddText("String", System.Drawing.Color.YellowGreen);
            Console.WriteLine(coloredString.BuildAnsi());
            Console.WriteLine(coloredString.BuildBbCode());
            Console.WriteLine(coloredString.BuildHtml());
            Console.WriteLine(coloredString.BuildRtf());
            Console.WriteLine(coloredString.BuildTextMeshPro());
        }

        public static async Task TestWebServer()
        {
            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.S)
            {
                var server = new SimpleWebServer<BasePacket>("localhost");
                var Router = new SimpleServerRequestRouter<BasePacket>(server);
                server.ClientConnect += (g) => { Console.WriteLine($"New Client Connected {g}"); };
                server.ClientDisconnect += (g) => { Console.WriteLine($"Client DisConnected {g}"); };
                server.PacketReceived += (g, s) => 
                {
                    if (Router.TryHandle(g, s))
                        return;
                    if (s.Message is not null)
                        Console.WriteLine(s.Message);
                };
                Router.Register("ping", (g, p, r) => { r.Message = "Pong"; });
                server.Start();
                Console.WriteLine($"Started Server on {server.Address}");
                while (true)
                {
                    var Message = Console.ReadLine();
                    if (Message == null || Message == "exit")
                        break;
                    server.Broadcast(new BasePacket() { Message = $"Server: {Message}" });
                }
            }
            else
            {
                var client = new SimpleWebClient<BasePacket>("localhost");
                client.ServerConnectionEstablished += () => { Console.WriteLine($"Server Connected"); };
                client.ServerConnectionLost += () => { Console.WriteLine($"Server DisConnected"); };
                client.PacketReceived += (s) => 
                {
                    if (s.HasRequestInfo())
                        return;
                    if (s.Message is not null)
                        Console.WriteLine(s.Message); 
                };
                Console.WriteLine($"Connecting to {client.Address}");
                var Result = await client.ConnectAsync();
                Console.WriteLine($"Connection Success: {Result}");
                if (!Result)
                    return;
                while (true)
                {
                    var Message = Console.ReadLine();
                    if (Message == null || Message == "exit")
                        break;
                    if (Message == "ping")
                    {
                        var Response = await client.RequestAsync("ping");
                        if (!Response.RequestInfo!.IsError())
                            Console.WriteLine($"Response: {Response.Message}");
                        continue;
                    }
                    await client.SendAsync(new BasePacket() { Message = $"Client: {Message}" });
                }
            }
        }
        public static void TestTokenizer()
        {
            TokenizerConfig config = TokenizerConfig.NewBuilder().UseCStyle().SetQuote('\'').SetModifierChars('!').SetSplitOnWhitespace(true).Build();
            Tokenizer tokenizer = new(config);

            var Test =
                "soul_enemy(SOUL_ENEMY_TORCH_SLUG) && (!setting(restoreBrokenActors) || soul_keese) && (can_use_sticks || has_weapon || (has_explosives && has_nuts)) && has_bombchu && can_use_slingshot";

            var Tokens = tokenizer.Tokenize(Test);
            LogicTreeParser.IBoolExpr expr1 = LogicTreeParser.Parse(Tokens);

            Console.WriteLine(Tokens.ToFormattedJson());
            Console.WriteLine(expr1.ToFormattedJson());
        }
        public static void TestApplets()
        {
            AppletScreen testScreen = new(new TestApplet(), new TestApplet2());
            testScreen.Show();
        }

        public class BasePacket : ISimpleWebPacket
        {
            public SimpleRequestInfo? RequestInfo { get; set; } = null;

            public string? Message { get; set; } = null;
        }

        public class TestApplet : Applet
        {
            public override string Title() => "Test Applet";
            public override bool StaticSize() => false;
            public override bool StartAtEnd() => false;
            public override ColoredString[] Values() =>
            [
                new ColoredString("Value 1 ==", System.Drawing.Color.Aqua),
                new ColoredString("Value 2 ==", System.Drawing.Color.Aqua),
                new ColoredString("Value 3 ==", System.Drawing.Color.Aqua),
                new ColoredString("Value 4 == ==", System.Drawing.Color.Aqua),
                new ColoredString("Value 5 == ==", System.Drawing.Color.Aqua),
                new ColoredString("Value 6 == ==", System.Drawing.Color.Aqua),
                new ColoredString("Value 7 == == ==", System.Drawing.Color.Aqua),
                new ColoredString("Value 8 == == ==", System.Drawing.Color.Aqua),
                new ColoredString("Value 9 == == ==", System.Drawing.Color.Aqua),
            ];
        }

        public class TestApplet2 : Applet
        {
            public override string Title() => "Test 2 Applet";
            public override bool StaticSize() => true;
            public override bool StartAtEnd() => true;
            public override ColoredString[] Values() =>
            [
                new ColoredString("Value 1 ==", System.Drawing.Color.Purple),
                new ColoredString("Value 2 ==", System.Drawing.Color.Red),
                new ColoredString("Value 3 ==", System.Drawing.Color.Blue),
            ];
        }

    }
}