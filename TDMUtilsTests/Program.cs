using System;
using System.Collections.Generic;
using System.Linq;
using TDMUtils;
using TDMUtils.CLITools;
using TDMUtils.Tokenizer;

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
            //TestWebServer().GetAwaiter().GetResult();
            TestApplets();
        }

        public static async Task TestWebServer()
        {
            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.S)
            {
                var server = new SimpleWebServer<string>("localhost");
                server.ClientConnect += (g) => { Console.WriteLine($"New Client Connected {g}"); };
                server.ClientDisconnect += (g) => { Console.WriteLine($"Client DisConnected {g}"); };
                server.PacketReceived += (g, s) => { Console.WriteLine($"Got message from {g}: {s}"); };
                server.Start();
                Console.WriteLine($"Started Server on {server.Address}");
                while (true)
                {
                    var Message = Console.ReadLine();
                    if (Message == null || Message == "exit")
                        break;
                    server.Broadcast(Message);
                }
            }
            else
            {
                var client = new SimpleWebClient<string>("localhost");
                client.ServerConnectionEstablished += () => { Console.WriteLine($"Server Connected"); };
                client.ServerConnectionLost += () => { Console.WriteLine($"Server DisConnected"); };
                client.PacketReceived += (s) => { Console.WriteLine($"Got message from Server: {s}"); };
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
                    await client.SendAsync(Message);
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