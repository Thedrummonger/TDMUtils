using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDMUtils.Tokenizer
{
    /// <summary>
    /// Provides methods for parsing a list of <see cref="IToken"/> into a logic tree (AST).
    /// </summary>
    public static class LogicTreeParser
    {
        /// <summary>
        /// Represents a boolean expression.
        /// </summary>
        public interface IBoolExpr { }

        /// <summary>
        /// Represents a variable expression (leaf node) preserving an <see cref="IToken"/>.
        /// </summary>
        internal class VarExpr : IBoolExpr
        {
            /// <summary>
            /// Gets the token for this variable.
            /// </summary>
            public IToken Token { get; }
            public VarExpr(IToken token) { Token = token; }
        }

        /// <summary>
        /// Represents an AND expression.
        /// </summary>
        internal class AndExpr : IBoolExpr
        {
            public IBoolExpr Left { get; }
            public IBoolExpr Right { get; }
            public AndExpr(IBoolExpr left, IBoolExpr right) { Left = left; Right = right; }
        }

        /// <summary>
        /// Represents an OR expression.
        /// </summary>
        internal class OrExpr : IBoolExpr
        {
            public IBoolExpr Left { get; }
            public IBoolExpr Right { get; }
            public OrExpr(IBoolExpr left, IBoolExpr right) { Left = left; Right = right; }
        }

        /// <summary>
        /// Parses a list of tokens into a boolean expression tree.
        /// </summary>
        /// <param name="tokens">The list of tokens.</param>
        /// <returns>The root of the boolean expression tree.</returns>
        public static IBoolExpr Parse(List<IToken> tokens)
        {
            int index = 0;
            return ParseOrExpr(tokens, ref index);
        }

        private static IBoolExpr ParseOrExpr(List<IToken> tokens, ref int index)
        {
            IBoolExpr left = ParseAndExpr(tokens, ref index);
            while (index < tokens.Count && IsOrOp(tokens[index]))
            {
                index++; // Skip OR token.
                IBoolExpr right = ParseAndExpr(tokens, ref index);
                left = new OrExpr(left, right);
            }
            return left;
        }

        private static IBoolExpr ParseAndExpr(List<IToken> tokens, ref int index)
        {
            IBoolExpr left = ParsePrimary(tokens, ref index);
            while (index < tokens.Count && IsAndOp(tokens[index]))
            {
                index++; // Skip AND token.
                IBoolExpr right = ParsePrimary(tokens, ref index);
                left = new AndExpr(left, right);
            }
            return left;
        }

        private static IBoolExpr ParsePrimary(List<IToken> tokens, ref int index)
        {
            if (index >= tokens.Count)
                throw new Exception("Unexpected end of tokens while parsing.");

            IToken token = tokens[index];

            if (IsOpenParen(token))
            {
                index++; // Skip '('
                IBoolExpr expr = ParseOrExpr(tokens, ref index);
                if (index >= tokens.Count || !IsCloseParen(tokens[index]))
                    throw new Exception("Missing closing parenthesis.");
                index++; // Skip ')'
                return expr;
            }
            else
            {
                index++;
                return new VarExpr(token);
            }
        }

        private static bool IsAndOp(IToken token)
        {
            return token is AndToken;
        }

        private static bool IsOrOp(IToken token)
        {
            return token is OrToken;
        }

        private static bool IsOpenParen(IToken token)
        {
            return token is OpenContainerToken;
        }

        private static bool IsCloseParen(IToken token)
        {
            return token is CloseContainerToken;
        }
    }
}
