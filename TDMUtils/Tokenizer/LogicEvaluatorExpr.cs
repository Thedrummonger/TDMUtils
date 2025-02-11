using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDMUtils.Tokenizer
{
    /// <summary>
    /// Provides methods to evaluate logic expressions.
    /// The expression may be provided as either a flattened DNF (a list of clauses where each clause is a list of <see cref="IToken"/>)
    /// or as a boolean expression tree (an <see cref="LogicTreeParser.IBoolExpr"/>) assumed to be in DNF.
    /// The evaluation is based on a user–supplied token–evaluation function.
    /// </summary>
    public static class LogicEvaluatorExpr
    {
        /// <summary>
        /// Evaluates the flattened DNF expression using a token–evaluation function.
        /// </summary>
        /// <param name="clauses">
        /// The flattened DNF expression as a list of clauses, where each clause is a list of <see cref="IToken"/>.
        /// </param>
        /// <param name="tokenEvaluator">
        /// A function that accepts an <see cref="IToken"/> and returns <c>true</c> if that token is valid,
        /// or <c>false</c> otherwise.
        /// </param>
        /// <returns>
        /// <c>true</c> if at least one clause has every token evaluating to <c>true</c> according to <paramref name="tokenEvaluator"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool EvaluateExpr(List<List<IToken>> clauses, Func<IToken, bool> tokenEvaluator) =>
            EvaluateExpr(clauses, tokenEvaluator, out _);

        /// <summary>
        /// Evaluates the flattened DNF expression using a token–evaluation function and returns the clause that evaluated to true.
        /// </summary>
        /// <param name="clauses">
        /// The flattened DNF expression as a list of clauses, where each clause is a list of <see cref="IToken"/>.
        /// </param>
        /// <param name="tokenEvaluator">
        /// A function that accepts an <see cref="IToken"/> and returns <c>true</c> if that token is valid,
        /// or <c>false</c> otherwise.
        /// </param>
        /// <param name="successfulClause">
        /// When the method returns <c>true</c>, this out parameter is set to the clause (as an array of <see cref="IToken"/>)
        /// that evaluated to true; otherwise, it is set to an empty array.
        /// </param>
        /// <returns>
        /// <c>true</c> if at least one clause has every token evaluating to <c>true</c> according to <paramref name="tokenEvaluator"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public static bool EvaluateExpr(List<List<IToken>> clauses, Func<IToken, bool> tokenEvaluator, out IToken[] successfulClause)
        {
            foreach (var clause in clauses)
            {
                if (clause.All(tokenEvaluator))
                {
                    successfulClause = [.. clause];
                    return true;
                }
            }
            successfulClause = [];
            return false;
        }

        /// <summary>
        /// Evaluates a boolean expression tree (assumed to be in DNF) using a token–evaluation function.
        /// Returns <c>true</c> if at least one clause (a disjunct) evaluates to <c>true</c>.
        /// </summary>
        /// <param name="expr">
        /// The boolean expression (in DNF) to evaluate.
        /// </param>
        /// <param name="tokenEvaluator">
        /// A function that accepts an <see cref="IToken"/> and returns <c>true</c> if that token is considered valid; otherwise <c>false</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if at least one clause evaluates to <c>true</c>; otherwise, <c>false</c>.
        /// </returns>
        public static bool EvaluateExpr(this LogicTreeParser.IBoolExpr expr, Func<IToken, bool> tokenEvaluator) =>
            EvaluateExpr(expr, tokenEvaluator, out _);

        /// <summary>
        /// Evaluates a boolean expression tree (assumed to be in DNF) using a token–evaluation function and returns
        /// the clause that evaluated to <c>true</c>.
        /// </summary>
        /// <param name="expr">
        /// The boolean expression (in DNF) to evaluate.
        /// </param>
        /// <param name="tokenEvaluator">
        /// A function that accepts an <see cref="IToken"/> and returns <c>true</c> if that token is considered valid; otherwise <c>false</c>.
        /// </param>
        /// <param name="successfulClause">
        /// When the method returns <c>true</c>, this out parameter is set to the clause (as an <see cref="LogicTreeParser.IBoolExpr"/>)
        /// that evaluated to <c>true</c>; otherwise, it is set to <c>null</c>.
        /// </param>
        /// <returns>
        /// <c>true</c> if at least one clause evaluates to <c>true</c>; otherwise, <c>false</c>.
        /// </returns>
        public static bool EvaluateExpr(this LogicTreeParser.IBoolExpr expr, Func<IToken, bool> tokenEvaluator, out LogicTreeParser.IBoolExpr successfulClause)
        {
            List<LogicTreeParser.IBoolExpr> clauses = GetClauses(expr);
            foreach (var clause in clauses)
            {
                if (EvaluateClause(clause, tokenEvaluator))
                {
                    successfulClause = clause;
                    return true;
                }
            }
            successfulClause = null;
            return false;
        }

        /// <summary>
        /// Recursively extracts all the disjunctive clauses from a boolean expression.
        /// If the expression is an OR, it returns the flattened list of its disjuncts; otherwise, it returns a list containing the expression itself.
        /// </summary>
        /// <param name="expr">The boolean expression to extract clauses from.</param>
        /// <returns>A list of clauses represented as <see cref="LogicTreeParser.IBoolExpr"/> objects.</returns>
        private static List<LogicTreeParser.IBoolExpr> GetClauses(this LogicTreeParser.IBoolExpr expr)
        {
            var list = new List<LogicTreeParser.IBoolExpr>();
            if (expr is LogicTreeParser.OrExpr orExpr)
            {
                list.AddRange(GetClauses(orExpr.Left));
                list.AddRange(GetClauses(orExpr.Right));
            }
            else
            {
                list.Add(expr);
            }
            return list;
        }

        /// <summary>
        /// Evaluates a single clause (assumed to be a conjunction of variable expressions) using the token–evaluation function.
        /// </summary>
        /// <param name="clause">The clause to evaluate.</param>
        /// <param name="tokenEvaluator">The function to evaluate each token.</param>
        /// <returns><c>true</c> if every token in the clause evaluates to <c>true</c>; otherwise, <c>false</c>.</returns>
        private static bool EvaluateClause(this LogicTreeParser.IBoolExpr clause, Func<IToken, bool> tokenEvaluator)
        {
            if (clause is LogicTreeParser.VarExpr v)
                return tokenEvaluator(v.Token);
            else if (clause is LogicTreeParser.AndExpr a)
                return EvaluateClause(a.Left, tokenEvaluator) && EvaluateClause(a.Right, tokenEvaluator);
            else if (clause is LogicTreeParser.OrExpr o)
                return EvaluateClause(o.Left, tokenEvaluator) || EvaluateClause(o.Right, tokenEvaluator);
            else
                throw new Exception("Unexpected clause type in EvaluateClause");
        }
    }
}
