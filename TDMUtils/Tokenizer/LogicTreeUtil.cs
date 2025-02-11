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
        /// Extracts the disjunctive clauses from a boolean expression into a list of clauses,
        /// where each clause is represented as a list of <see cref="IToken"/>.
        /// </summary>
        /// <remarks>
        /// This method recursively traverses an <see cref="LogicTreeParser.IBoolExpr"/> and produces a flattened structure of clauses.
        /// Each clause represents a conjunction (logical AND) of tokens, and the overall result represents a disjunction (logical OR) of these clauses.
        /// Although the method works correctly even if the input is not strictly in Disjunctive Normal Form (DNF),
        /// it is recommended to call <c>ToDNF()</c> first to avoid redundant or nested clauses.
        /// </remarks>
        public static List<List<IToken>> ToClauseList(this LogicTreeParser.IBoolExpr expr)
        {
            switch (expr)
            {
                case LogicTreeParser.VarExpr v:
                    return [[v.Token]];
                case LogicTreeParser.AndExpr andExpr:
                    var leftClauses = ToClauseList(andExpr.Left);
                    var rightClauses = ToClauseList(andExpr.Right);
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
                    var leftOr = ToClauseList(orExpr.Left);
                    var rightOr = ToClauseList(orExpr.Right);
                    leftOr.AddRange(rightOr);
                    return leftOr;
                default:
                    throw new Exception("Unknown expression type in FlattenDnf");
            }
        }
        /// <summary>
        /// Reconstructs a boolean expression tree from a clause list.
        /// </summary>
        /// <param name="clauses">
        /// A list of clauses, where each clause is a list of <see cref="IToken"/> objects.
        /// Each clause represents a conjunction (logical AND) of tokens, and the overall list represents a disjunction (logical OR) of these clauses.
        /// </param>
        /// <returns>
        /// An <see cref="LogicTreeParser.IBoolExpr"/> representing the reconstructed boolean expression tree.
        /// If <paramref name="clauses"/> is empty, a <see cref="LogicTreeParser.VarExpr"/> representing the literal "true" is returned.
        /// </returns>
        public static LogicTreeParser.IBoolExpr BuildExprFromClauseList(List<List<IToken>> clauses)
        {
            if (clauses == null || clauses.Count == 0)
            {
                // For an empty list, assume the expression is trivially true.
                return new LogicTreeParser.VarExpr(new VariableToken { Value = "true" });
            }

            // Build an expression for each clause.
            List<LogicTreeParser.IBoolExpr> clauseExprs = [];
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
            List<List<IToken>> newClauses = LogicTreeUtil.ToClauseList(dnfReplacement);

            // Retrieve the original clause where the token will be replaced.
            List<IToken> originalClause = flattenedDnf[clauseIndex];
            // Split the clause into prefix (tokens before the target token) and suffix (tokens after).
            List<IToken> prefix = [.. originalClause.Take(tokenIndex)];
            List<IToken> suffix = [.. originalClause.Skip(tokenIndex + 1)];

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
        /// <summary>
        /// Minimizes a boolean expression that is in DNF by factoring out common tokens across all clauses.
        /// </summary>
        /// <param name="expr">
        /// The boolean expression (in DNF) to minimize. It is assumed that <paramref name="expr"/> was produced by <c>ToDNF()</c>.
        /// </param>
        /// <returns>
        /// An <see cref="LogicTreeParser.IBoolExpr"/> that is logically equivalent to <paramref name="expr"/>, but with
        /// common tokens factored out. If no common tokens exist, a fully expanded (but equivalent) expression is returned.
        /// </returns>
        public static LogicTreeParser.IBoolExpr MinimizeDNF(LogicTreeParser.IBoolExpr expr)
        {
            // Flatten the DNF expression to get a list of clauses.
            var flattened = expr.ToClauseList();
            if (flattened == null || flattened.Count == 0)
                return new LogicTreeParser.VarExpr(new VariableToken { Value = "true" });

            // Compute the common tokens that appear in every clause.
            // We assume that IToken implements a proper Equals() method.
            var common = new HashSet<IToken>(flattened[0]);
            foreach (var clause in flattened)
            {
                common.IntersectWith(clause);
            }

            if (common.Count == 0)
            {
                // No common tokens found; return the fully expanded expression.
                return BuildExprFromClauseList(flattened);
            }
            // Build the common expression as an AND of the common tokens.
            LogicTreeParser.IBoolExpr? commonExpr = null;
            foreach (var token in common)
            {
                var varExpr = new LogicTreeParser.VarExpr(token);
                commonExpr = (commonExpr == null) ? varExpr : new LogicTreeParser.AndExpr(commonExpr, varExpr);
            }

            // For each clause, remove the common tokens.
            List<LogicTreeParser.IBoolExpr> remainderExprs = [];
            foreach (var clause in flattened)
            {
                var remainder = clause.Where(t => !common.Contains(t)).ToList();
                if (remainder.Count == 0)
                {
                    // This clause is completely covered by the common tokens.
                    // In a conjunction, true is the identity; so we can consider this clause as "true"
                    // (and thus it doesn't restrict the overall disjunction).
                    continue;
                }
                else
                {
                    // Rebuild the clause as an AND expression.
                    LogicTreeParser.IBoolExpr clauseExpr = new LogicTreeParser.VarExpr(remainder[0]);
                    for (int i = 1; i < remainder.Count; i++)
                    {
                        clauseExpr = new LogicTreeParser.AndExpr(clauseExpr, new LogicTreeParser.VarExpr(remainder[i]));
                    }
                    remainderExprs.Add(clauseExpr);
                }
            }

            LogicTreeParser.IBoolExpr? remainderExpr = null;
            if (remainderExprs.Count > 0)
            {
                remainderExpr = remainderExprs[0];
                for (int i = 1; i < remainderExprs.Count; i++)
                {
                    remainderExpr = new LogicTreeParser.OrExpr(remainderExpr, remainderExprs[i]);
                }
            }

            // If every clause was fully factored (i.e. remainderExpr is null), the minimized expression is just the common factors.
            if (remainderExpr == null)
                return commonExpr!;
            else
                return new LogicTreeParser.AndExpr(commonExpr!, remainderExpr);
        }
    }
}
