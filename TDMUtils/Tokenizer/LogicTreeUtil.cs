using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDMUtils.Tokenizer
{
    public static class LogicTreeUtil
    {
        /// <summary>
        /// Converts the specified boolean expression tree into its Disjunctive Normal Form (DNF).
        /// </summary>
        /// <param name="expr">The root of the expression tree to convert.</param>
        /// <returns>
        /// An <see cref="LogicTreeParser.IBoolExpr"/> in DNF, logically equivalent to <paramref name="expr"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown if the expression type is not recognized.
        /// </exception>
        public static LogicTreeParser.IBoolExpr ToDNF(this LogicTreeParser.IBoolExpr expr)
        {
            switch (expr)
            {
                case LogicTreeParser.VarExpr v:
                    return v;

                case LogicTreeParser.AndExpr andExpr:
                    var leftAnd = andExpr.Left.ToDNF();
                    var rightAnd = andExpr.Right.ToDNF();
                    return DistributeAnd(leftAnd, rightAnd);

                case LogicTreeParser.OrExpr orExpr:
                    var leftOr = orExpr.Left.ToDNF();
                    var rightOr = orExpr.Right.ToDNF();
                    return new LogicTreeParser.OrExpr(leftOr, rightOr);

                default:
                    throw new Exception("Unknown expression type in DNF converter");
            }
        }

        /// <summary>
        /// Distributes an AND operation over OR operations in the subexpressions,
        /// ensuring the result remains in DNF.
        /// </summary>
        /// <param name="left">A DNF subexpression on the left side of an AND.</param>
        /// <param name="right">A DNF subexpression on the right side of an AND.</param>
        /// <returns>
        /// A DNF expression representing (left AND right).
        /// </returns>
        private static LogicTreeParser.IBoolExpr DistributeAnd(
            LogicTreeParser.IBoolExpr left,
            LogicTreeParser.IBoolExpr right)
        {
            // (X || Y) && Z => (X && Z) || (Y && Z)
            if (left is LogicTreeParser.OrExpr leftOr)
            {
                var leftDistributed = DistributeAnd(leftOr.Left, right);
                var rightDistributed = DistributeAnd(leftOr.Right, right);
                return new LogicTreeParser.OrExpr(leftDistributed, rightDistributed);
            }
            // X && (Y || Z) => (X && Y) || (X && Z)
            else if (right is LogicTreeParser.OrExpr rightOr)
            {
                var leftDistributed = DistributeAnd(left, rightOr.Left);
                var rightDistributed = DistributeAnd(left, rightOr.Right);
                return new LogicTreeParser.OrExpr(leftDistributed, rightDistributed);
            }
            // Neither side is an OR => simple AndExpr
            else
            {
                return new LogicTreeParser.AndExpr(left, right);
            }
        }

        /// <summary>
        /// Flattens a boolean expression into a list of clauses (each clause is a list of <see cref="IToken"/>).
        /// Each clause represents a logical AND of tokens, and the full list represents a logical OR of these clauses.
        /// </summary>
        /// <param name="expr">The root of the boolean expression tree to flatten.</param>
        /// <returns>
        /// A list of clauses in the form <c>List&lt;List&lt;IToken&gt;&gt;</c>, where each sub-list is a conjunction of tokens.
        /// </returns>
        /// <remarks>
        /// For best results, call <see cref="ToDNF(LogicTreeParser.IBoolExpr)"/> first to ensure the expression
        /// is in Disjunctive Normal Form before flattening.
        /// </remarks>
        /// <exception cref="Exception">
        /// Thrown if an unrecognized expression type is encountered.
        /// </exception>
        public static List<List<IToken>> ToClauseList(this LogicTreeParser.IBoolExpr expr)
        {
            switch (expr)
            {
                case LogicTreeParser.VarExpr v:
                    return [[v.Token]];

                case LogicTreeParser.AndExpr andExpr:
                    var leftClauses = andExpr.Left.ToClauseList();
                    var rightClauses = andExpr.Right.ToClauseList();
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
                    var leftOr = orExpr.Left.ToClauseList();
                    var rightOr = orExpr.Right.ToClauseList();
                    leftOr.AddRange(rightOr);
                    return leftOr;

                default:
                    throw new Exception("Unknown expression type in FlattenDnf");
            }
        }

        /// <summary>
        /// Reconstructs a boolean expression tree (of type <see cref="LogicTreeParser.IBoolExpr"/>)
        /// from a list of clauses in DNF form.
        /// </summary>
        /// <param name="clauses">
        /// A list of clauses, where each clause is a list of <see cref="IToken"/> objects.
        /// Each clause represents a conjunction (AND) of tokens, and the overall list represents a disjunction (OR) of these clauses.
        /// </param>
        /// <returns>
        /// An <see cref="LogicTreeParser.IBoolExpr"/> representing the rebuilt expression tree. If
        /// <paramref name="clauses"/> is empty, a <see cref="LogicTreeParser.VarExpr"/> with a "true" token is returned.
        /// </returns>
        public static LogicTreeParser.IBoolExpr BuildExprFromClauseList(List<List<IToken>> clauses)
        {
            if (clauses == null || clauses.Count == 0)
            {
                // For an empty list, treat as logically "true".
                return new LogicTreeParser.VarExpr(new VariableToken { Value = "true" });
            }

            // Build an expression for each clause, then combine them with OR.
            var clauseExprs = new List<LogicTreeParser.IBoolExpr>();
            foreach (var clause in clauses)
            {
                if (clause.Count == 0)
                    continue;

                // Start the clause expression with the first token
                LogicTreeParser.IBoolExpr clauseExpr =
                    new LogicTreeParser.VarExpr(clause[0]);

                // AND any additional tokens in this clause
                for (int i = 1; i < clause.Count; i++)
                {
                    clauseExpr = new LogicTreeParser.AndExpr(
                        clauseExpr,
                        new LogicTreeParser.VarExpr(clause[i]));
                }
                clauseExprs.Add(clauseExpr);
            }

            // Combine all clauses with OR
            LogicTreeParser.IBoolExpr result = clauseExprs[0];
            for (int i = 1; i < clauseExprs.Count; i++)
            {
                result = new LogicTreeParser.OrExpr(result, clauseExprs[i]);
            }
            return result;
        }

        /// <summary>
        /// Injects a replacement sub-expression into one specific location in a flattened DNF clause list.
        /// </summary>
        /// <param name="flattenedDnf">
        /// A list of clauses (each a list of <see cref="IToken"/>), in DNF form.
        /// </param>
        /// <param name="clauseIndex">
        /// The index of the clause to modify.
        /// </param>
        /// <param name="tokenIndex">
        /// The index within that clause of the token to be replaced.
        /// </param>
        /// <param name="replacementExpr">
        /// The new sub-expression to inject, which will be converted to DNF and flattened before insertion.
        /// </param>
        /// <returns>
        /// A new list of clauses in DNF where the specified token was replaced 
        /// by the flattened representation of <paramref name="replacementExpr"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown if <paramref name="clauseIndex"/> or <paramref name="tokenIndex"/> is out of range.
        /// </exception>
        public static List<List<IToken>> InjectSubexpression(List<List<IToken>> flattenedDnf, int clauseIndex, int tokenIndex, LogicTreeParser.IBoolExpr replacementExpr)
        {
            var newClauses = replacementExpr.ToDNF().ToClauseList();

            var originalClause = flattenedDnf[clauseIndex];
            var prefix = originalClause.Take(tokenIndex).ToList();
            var suffix = originalClause.Skip(tokenIndex + 1).ToList();

            var injectedClauses = new List<List<IToken>>();
            foreach (var subClause in newClauses)
            {
                var combined = new List<IToken>(prefix);
                combined.AddRange(subClause);
                combined.AddRange(suffix);
                injectedClauses.Add(combined);
            }

            var newFlattenedDnf = new List<List<IToken>>();
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
        /// Searches for the first occurrence of a specific <see cref="IToken"/> in a flattened DNF clause list 
        /// and replaces it with a given sub-expression.
        /// </summary>
        /// <param name="flattenedDnf">
        /// A list of clauses (each a list of <see cref="IToken"/>), in DNF form.
        /// </param>
        /// <param name="tokenToReplace">
        /// The specific token instance to locate.
        /// </param>
        /// <param name="replacementExpr">
        /// The sub-expression to inject, which is converted to DNF and flattened before insertion.
        /// </param>
        /// <returns>
        /// A new list of clauses in DNF where the first occurrence of <paramref name="tokenToReplace"/> 
        /// is replaced by <paramref name="replacementExpr"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// Thrown if <paramref name="tokenToReplace"/> is not found in <paramref name="flattenedDnf"/>.
        /// </exception>
        public static List<List<IToken>> InjectSubexpression(List<List<IToken>> flattenedDnf, IToken tokenToReplace, LogicTreeParser.IBoolExpr replacementExpr)
        {
            for (int clauseIndex = 0; clauseIndex < flattenedDnf.Count; clauseIndex++)
            {
                var clause = flattenedDnf[clauseIndex];
                for (int tokenIndex = 0; tokenIndex < clause.Count; tokenIndex++)
                    if (clause[tokenIndex].Equals(tokenToReplace))
                        return InjectSubexpression(flattenedDnf, clauseIndex, tokenIndex, replacementExpr);
            }
            throw new Exception("Specified token to replace was not found in the flattened DNF.");
        }

        /// <summary>
        /// Recursively injects a replacement sub-expression at the AST level, 
        /// replacing any <see cref="LogicTreeParser.VarExpr"/> that holds the specified token instance.
        /// </summary>
        /// <remarks>
        /// Because each <see cref="LogicTreeParser.VarExpr"/> is created uniquely by the parser, 
        /// only one node in the expression tree can reference a particular <see cref="IToken"/> object. 
        /// Consequently, this effectively replaces at most one occurrence in the AST.
        /// </remarks>
        /// <param name="expr">
        /// The root of the expression tree to search within.
        /// </param>
        /// <param name="tokenToReplace">
        /// The specific token instance to find.
        /// </param>
        /// <param name="replacement">
        /// The sub-expression to use instead of the matched <see cref="LogicTreeParser.VarExpr"/>.
        /// </param>
        /// <returns>
        /// An <see cref="LogicTreeParser.IBoolExpr"/> in which the matched token (if present) 
        /// is replaced by <paramref name="replacement"/>.
        /// </returns>
        public static LogicTreeParser.IBoolExpr InjectSubexpression(LogicTreeParser.IBoolExpr expr, IToken tokenToReplace, LogicTreeParser.IBoolExpr replacement)
        {
            switch (expr)
            {
                case LogicTreeParser.VarExpr v:
                    if (v.Token.Equals(tokenToReplace))
                        return replacement;
                    else
                        return expr;

                case LogicTreeParser.AndExpr andExpr:
                    {
                        var newLeft = InjectSubexpression(andExpr.Left, tokenToReplace, replacement);
                        var newRight = InjectSubexpression(andExpr.Right, tokenToReplace, replacement);
                        return new LogicTreeParser.AndExpr(newLeft, newRight);
                    }

                case LogicTreeParser.OrExpr orExpr:
                    {
                        var newLeft = InjectSubexpression(orExpr.Left, tokenToReplace, replacement);
                        var newRight = InjectSubexpression(orExpr.Right, tokenToReplace, replacement);
                        return new LogicTreeParser.OrExpr(newLeft, newRight);
                    }

                default:
                    return expr;
            }
        }

        /// <summary>
        /// Combines two expression trees with a logical AND, returning the result as an <see cref="LogicTreeParser.IBoolExpr"/>.
        /// </summary>
        /// <param name="leftExpr">The left sub-expression.</param>
        /// <param name="rightExpr">The right sub-expression.</param>
        /// <returns>
        /// An <see cref="LogicTreeParser.AndExpr"/> representing (leftExpr && rightExpr).
        /// </returns>
        public static LogicTreeParser.IBoolExpr CombineWithAnd(
            this LogicTreeParser.IBoolExpr leftExpr,
            LogicTreeParser.IBoolExpr rightExpr)
            => new LogicTreeParser.AndExpr(leftExpr, rightExpr);

        /// <summary>
        /// Combines two expression trees with a logical OR, returning the result as an <see cref="LogicTreeParser.IBoolExpr"/>.
        /// </summary>
        /// <param name="leftExpr">The left sub-expression.</param>
        /// <param name="rightExpr">The right sub-expression.</param>
        /// <returns>
        /// An <see cref="LogicTreeParser.OrExpr"/> representing (leftExpr || rightExpr).
        /// </returns>
        public static LogicTreeParser.IBoolExpr CombineWithOr(
            this LogicTreeParser.IBoolExpr leftExpr,
            LogicTreeParser.IBoolExpr rightExpr)
            => new LogicTreeParser.OrExpr(leftExpr, rightExpr);

        /// <summary>
        /// Combines two flattened DNF clause lists with a logical AND,
        /// returning a new flattened DNF clause list that represents (leftClauses && rightClauses).
        /// </summary>
        /// <param name="leftClauses">The first set of clauses, treated as an OR of ANDs.</param>
        /// <param name="rightClauses">The second set of clauses, treated as an OR of ANDs.</param>
        /// <returns>
        /// A new list of clauses in DNF representing the logical AND of the two clause sets.
        /// </returns>
        public static List<List<IToken>> CombineWithAnd(
            this List<List<IToken>> leftClauses,
            List<List<IToken>> rightClauses)
        {
            var combinedExpr = BuildExprFromClauseList(leftClauses)
                .CombineWithAnd(BuildExprFromClauseList(rightClauses));
            return combinedExpr.ToDNF().ToClauseList();
        }

        /// <summary>
        /// Combines two flattened DNF clause lists with a logical OR,
        /// returning a new flattened DNF clause list that represents (leftClauses || rightClauses).
        /// </summary>
        /// <param name="leftClauses">The first set of clauses, treated as an OR of ANDs.</param>
        /// <param name="rightClauses">The second set of clauses, treated as an OR of ANDs.</param>
        /// <returns>
        /// A new list of clauses in DNF representing the logical OR of the two clause sets.
        /// </returns>
        public static List<List<IToken>> CombineWithOr(
            this List<List<IToken>> leftClauses,
            List<List<IToken>> rightClauses)
        {
            var combinedExpr = BuildExprFromClauseList(leftClauses)
                .CombineWithOr(BuildExprFromClauseList(rightClauses));
            return combinedExpr.ToDNF().ToClauseList();
        }

        /// <summary>
        /// Attempts to factor out common tokens (variables) across all clauses in a DNF expression,
        /// producing an equivalent, potentially more compact form.
        /// </summary>
        /// <param name="expr">
        /// The boolean expression, assumed to be in DNF. 
        /// It is recommended to call <see cref="ToDNF(LogicTreeParser.IBoolExpr)"/> beforehand.
        /// </param>
        /// <returns>
        /// An <see cref="LogicTreeParser.IBoolExpr"/> that is logically equivalent to <paramref name="expr"/>, 
        /// with any tokens common to every clause factored out as a conjunction.
        /// </returns>
        /// <remarks>
        /// If the expression is empty, or if no tokens are common to all clauses, the returned expression
        /// will match <paramref name="expr"/> or may be fully expanded. This is not a complete "minimal form" 
        /// algorithm, but a partial simplification.
        /// </remarks>
        public static LogicTreeParser.IBoolExpr MinimizeDNF(
            LogicTreeParser.IBoolExpr expr)
        {
            // Flatten the DNF expression
            var flattened = expr.ToClauseList();
            if (flattened == null || flattened.Count == 0)
            {
                // Treat as logical "true" if empty
                return new LogicTreeParser.VarExpr(new VariableToken { Value = "true" });
            }

            // Find tokens common to every clause
            var common = new HashSet<IToken>(flattened[0]);
            foreach (var clause in flattened)
            {
                common.IntersectWith(clause);
            }

            // If no common tokens, just rebuild the original expression
            if (common.Count == 0)
            {
                return BuildExprFromClauseList(flattened);
            }

            // Build expression from the common tokens (A && B && ...)
            LogicTreeParser.IBoolExpr? commonExpr = null;
            foreach (var token in common)
            {
                var varExpr = new LogicTreeParser.VarExpr(token);
                commonExpr = (commonExpr == null) ? varExpr : new LogicTreeParser.AndExpr(commonExpr, varExpr);
            }

            // Remove the common tokens from each clause, then rebuild the remainder
            var remainderExprs = new List<LogicTreeParser.IBoolExpr>();
            foreach (var clause in flattened)
            {
                var remainder = clause.Where(t => !common.Contains(t)).ToList();
                if (remainder.Count == 0)
                {
                    // If the entire clause was covered by the common tokens,
                    // that clause effectively becomes "true" and doesn't limit the OR
                    continue;
                }

                // Rebuild this clause as an AND chain
                LogicTreeParser.IBoolExpr clauseExpr = new LogicTreeParser.VarExpr(remainder[0]);
                for (int i = 1; i < remainder.Count; i++)
                {
                    clauseExpr = new LogicTreeParser.AndExpr(clauseExpr, new LogicTreeParser.VarExpr(remainder[i]));
                }
                remainderExprs.Add(clauseExpr);
            }

            // Combine all remainder clauses with OR
            LogicTreeParser.IBoolExpr? remainderExpr = null;
            if (remainderExprs.Count > 0)
            {
                remainderExpr = remainderExprs[0];
                for (int i = 1; i < remainderExprs.Count; i++)
                {
                    remainderExpr = new LogicTreeParser.OrExpr(remainderExpr, remainderExprs[i]);
                }
            }

            // If every clause was fully covered by the common tokens, remainderExpr is null
            // => the result is just the common conjunction
            if (remainderExpr == null)
                return commonExpr!;

            // Otherwise, combine the common factors with the remainder
            return new LogicTreeParser.AndExpr(commonExpr!, remainderExpr);
        }
    }
}
