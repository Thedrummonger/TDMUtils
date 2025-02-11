using System;
using System.Collections.Generic;
using System.Linq;
using TDMUtils;
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
            TokenizerConfig config = TokenizerConfig.NewBuilder().UseCStyle().SetModifierChars('!').Build();
            Tokenizer tokenizer = new(config);

            var Test =
                "soul_iron_knuckle && ((small_keys_spirit(7) && (has_weapon || can_use_sticks)) || " +
                "(is_adult && small_keys_spirit(4) && has_lens && (can_play_time || can_play_elegy) && soul_floormaster))";

            LogicTreeParser.IBoolExpr expr1 = LogicTreeParser.Parse(tokenizer.Tokenize(Test));

            Console.WriteLine(expr1.ToDNF().ToClauseList().ToFormattedJson());

        }

    }
}