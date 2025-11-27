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
            TokenizerConfig config = TokenizerConfig.NewBuilder().UseCStyle().SetQuote('\'').SetModifierChars('!').SetSplitOnWhitespace(true).Build();
            Tokenizer tokenizer = new(config);

            var Test = 
                "soul_enemy(SOUL_ENEMY_TORCH_SLUG) && (!setting(restoreBrokenActors) || soul_keese) && (can_use_sticks || has_weapon || (has_explosives && has_nuts)) && has_bombchu && can_use_slingshot";

            var Tokens = tokenizer.Tokenize(Test);
            LogicTreeParser.IBoolExpr expr1 = LogicTreeParser.Parse(Tokens);

            //Console.WriteLine(Tokens.ToFormattedJson());

            AppletScreen testScreen = new AppletScreen([new TestApplet()]);
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

    }
}