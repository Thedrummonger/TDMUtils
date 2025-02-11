using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDMUtils.Tokenizer
{
    /// <summary>
    /// Provides utility methods for converting a logic tree to DNF and flattening it.
    /// </summary>
    public static class LogicTreeUtil
    {
        /// <summary>
        /// Converts a boolean expression tree to its Disjunctive Normal Form (DNF).
        /// </summary>
        public static LogicTreeParser.IBoolExpr ToDNF(this LogicTreeParser.IBoolExpr expr)
        {
            switch (expr)
            {
                case LogicTreeParser.VarExpr v:
                    return v;
                case LogicTreeParser.AndExpr andExpr:
                    var leftAnd = ToDNF(andExpr.Left);
                    var rightAnd = ToDNF(andExpr.Right);
                    return DistributeAnd(leftAnd, rightAnd);
                case LogicTreeParser.OrExpr orExpr:
                    var leftOr = ToDNF(orExpr.Left);
                    var rightOr = ToDNF(orExpr.Right);
                    return new LogicTreeParser.OrExpr(leftOr, rightOr);
                default:
                    throw new Exception("Unknown expression type in DNF converter");
            }
        }

        private static LogicTreeParser.IBoolExpr DistributeAnd(LogicTreeParser.IBoolExpr left, LogicTreeParser.IBoolExpr right)
        {
            if (left is LogicTreeParser.OrExpr leftOr)
            {
                var leftDistributed = DistributeAnd(leftOr.Left, right);
                var rightDistributed = DistributeAnd(leftOr.Right, right);
                return new LogicTreeParser.OrExpr(leftDistributed, rightDistributed);
            }
            else if (right is LogicTreeParser.OrExpr rightOr)
            {
                var leftDistributed = DistributeAnd(left, rightOr.Left);
                var rightDistributed = DistributeAnd(left, rightOr.Right);
                return new LogicTreeParser.OrExpr(leftDistributed, rightDistributed);
            }
            else
            {
                return new LogicTreeParser.AndExpr(left, right);
            }
        }

        /// <summary>
        /// Flattens a DNF expression tree into a list of clauses, where each clause is represented as a list of <see cref="IToken"/>.
        /// </summary>
        /// <remarks>
        /// This method converts a boolean expression in Disjunctive Normal Form (DNF) into a flattened structure consisting of a list of clauses.
        /// Each clause represents a conjunction (logical AND) of tokens, and the overall list represents a disjunction (logical OR) of these clauses.
        /// The flattened representation is useful for evaluation: if any clause (i.e., a list of tokens) evaluates to true,
        /// then the overall DNF expression is true.
        /// </remarks>
        public static List<List<IToken>> FlattenDnf(LogicTreeParser.IBoolExpr expr)
        {
            switch (expr)
            {
                case LogicTreeParser.VarExpr v:
                    return new List<List<IToken>> { new List<IToken> { v.Token } };
                case LogicTreeParser.AndExpr andExpr:
                    var leftClauses = FlattenDnf(andExpr.Left);
                    var rightClauses = FlattenDnf(andExpr.Right);
                    var result = new List<List<IToken>>();
                    foreach (var lc in leftClauses)
                    {
                        foreach (var rc in rightClauses)
                        {
                            var combined = new List<IToken>(lc);
                            combined.AddRange(rc);
                            result.Add(combined);
                        }
                    }
                    return result;
                case LogicTreeParser.OrExpr orExpr:
                    var leftOr = FlattenDnf(orExpr.Left);
                    var rightOr = FlattenDnf(orExpr.Right);
                    leftOr.AddRange(rightOr);
                    return leftOr;
                default:
                    throw new Exception("Unknown expression type in FlattenDnf");
            }
        }
        /// <summary>
        /// Reconstructs a boolean expression tree (in DNF) from a flattened DNF representation.
        /// </summary>
        /// <param name="clauses">A list of clauses, where each clause is a list of <see cref="IToken"/> objects.
        /// Each clause represents a conjunction (AND) of tokens, and the overall list represents a disjunction (OR) of these clauses.</param>
        /// <returns>
        /// An <see cref="LogicTreeParser.IBoolExpr"/> representing the reconstructed boolean expression tree.
        /// If <paramref name="clauses"/> is empty, a <c>VarExpr</c> representing the literal "true" is returned.
        /// </returns>
        public static LogicTreeParser.IBoolExpr BuildExprFromFlattenedDNF(List<List<IToken>> clauses)
        {
            if (clauses == null || clauses.Count == 0)
            {
                // For an empty list, we assume the expression is trivially true.
                return new LogicTreeParser.VarExpr(new VariableToken { Value = "true" });
            }

            // Build an expression for each clause.
            List<LogicTreeParser.IBoolExpr> clauseExprs = new List<LogicTreeParser.IBoolExpr>();
            foreach (var clause in clauses)
            {
                if (clause.Count == 0)
                    continue;
                // If there's only one token in the clause, wrap it as a VarExpr.
                LogicTreeParser.IBoolExpr clauseExpr = new LogicTreeParser.VarExpr(clause[0]);
                // For multiple tokens, combine them with AND.
                for (int i = 1; i < clause.Count; i++)
                {
                    clauseExpr = new LogicTreeParser.AndExpr(clauseExpr, new LogicTreeParser.VarExpr(clause[i]));
                }
                clauseExprs.Add(clauseExpr);
            }

            // Combine all clause expressions with OR.
            LogicTreeParser.IBoolExpr result = clauseExprs[0];
            for (int i = 1; i < clauseExprs.Count; i++)
            {
                result = new LogicTreeParser.OrExpr(result, clauseExprs[i]);
            }
            return result;
        }
        /// <summary>
        /// Injects a replacement sub–expression into a flattened DNF at the specified clause and token indexes.
        /// </summary>
        /// <param name="flattenedDnf">
        /// The flattened DNF representation as a list of clauses, where each clause is a list of <see cref="IToken"/>.
        /// </param>
        /// <param name="clauseIndex">
        /// The index of the clause in which the token to be replaced is located.
        /// </param>
        /// <param name="tokenIndex">
        /// The index (within the specified clause) of the token to be replaced.
        /// </param>
        /// <param name="replacementExpr">
        /// The replacement boolean expression (of type <see cref="LogicTreeParser.IBoolExpr"/>). This expression
        /// will be converted to DNF and then flattened.
        /// </param>
        /// <returns>
        /// A new flattened DNF (i.e. a <c>List&lt;List&lt;IToken&gt;&gt;</c>) in which the token at the specified location
        /// has been replaced by the tokens produced from <paramref name="replacementExpr"/>.
        /// </returns>
        public static List<List<IToken>> InjectSubexpression(
            List<List<IToken>> flattenedDnf,
            int clauseIndex,
            int tokenIndex,
            LogicTreeParser.IBoolExpr replacementExpr)
        {
            // Convert the replacement expression to DNF (safe even if already in DNF)
            var dnfReplacement = replacementExpr.ToDNF();
            // Flatten the replacement expression.
            List<List<IToken>> newClauses = LogicTreeUtil.FlattenDnf(dnfReplacement);

            // Retrieve the original clause where the token will be replaced.
            List<IToken> originalClause = flattenedDnf[clauseIndex];
            // Split the clause into prefix (tokens before the target token) and suffix (tokens after).
            List<IToken> prefix = originalClause.Take(tokenIndex).ToList();
            List<IToken> suffix = originalClause.Skip(tokenIndex + 1).ToList();

            // Using the new C# 8 spread syntax to combine lists.
            List<List<IToken>> injectedClauses = [];
            foreach (var newClause in newClauses)
            {
                // DotNet 8 allows list initialization with spread, e.g.:
                List<IToken> combined = [.. prefix, .. newClause, .. suffix];
                injectedClauses.Add(combined);
            }

            // Build the new flattened DNF by replacing the original clause with the injected clauses.
            List<List<IToken>> newFlattenedDnf = [];
            for (int i = 0; i < flattenedDnf.Count; i++)
            {
                if (i == clauseIndex)
                    newFlattenedDnf.AddRange(injectedClauses);
                else
                    newFlattenedDnf.Add(flattenedDnf[i]);
            }
            return newFlattenedDnf;
        }

        /// <summary>
        /// Injects a replacement sub–expression into a flattened DNF by searching for the first occurrence of a token that matches.
        /// </summary>
        /// <param name="flattenedDnf">
        /// The flattened DNF representation as a list of clauses, where each clause is a list of <see cref="IToken"/>.
        /// </param>
        /// <param name="tokenToReplace">
        /// The <see cref="IToken"/> object to replace.
        /// </param>
        /// <param name="replacementExpr">
        /// The replacement boolean expression (of type <see cref="LogicTreeParser.IBoolExpr"/>). This expression
        /// will be converted to DNF (if not already) and then flattened.
        /// </param>
        /// <returns>
        /// A new flattened DNF (a <c>List&lt;List&lt;IToken&gt;&gt;</c>) in which the first occurrence of the specified token
        /// has been replaced by the tokens produced from <paramref name="replacementExpr"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown if the specified token is not found in the flattened DNF.
        /// </exception>
        public static List<List<IToken>> InjectSubexpression(
            List<List<IToken>> flattenedDnf,
            IToken tokenToReplace,
            LogicTreeParser.IBoolExpr replacementExpr)
        {
            for (int clauseIndex = 0; clauseIndex < flattenedDnf.Count; clauseIndex++)
            {
                List<IToken> clause = flattenedDnf[clauseIndex];
                for (int tokenIndex = 0; tokenIndex < clause.Count; tokenIndex++)
                {
                    if (clause[tokenIndex].Equals(tokenToReplace))
                    {
                        return InjectSubexpression(flattenedDnf, clauseIndex, tokenIndex, replacementExpr);
                    }
                }
            }
            throw new Exception("Specified token to replace was not found in the flattened DNF.");
        }
    }
}
