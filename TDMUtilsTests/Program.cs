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
            TokenizerConfig config = TokenizerConfig.NewBuilder().UseMatlabStyle().SetModifierChars('!').Build();
            Tokenizer tokenizer = new(config);

            var Test ="/t|(/c&#lake)";

            var Tokens = tokenizer.Tokenize(Test);
            LogicTreeParser.IBoolExpr expr1 = LogicTreeParser.Parse(Tokens);

            Console.WriteLine(expr1.ToDNF().ToClauseList().ToFormattedJson());

        }

    }
}