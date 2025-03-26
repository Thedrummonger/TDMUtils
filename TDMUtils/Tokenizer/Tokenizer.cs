using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace TDMUtils.Tokenizer
{
    /// <summary>
    /// Represents a tokenized item.
    /// </summary>
    public interface IToken
    {
        /// <summary>
        /// Gets the text value of the token.
        /// </summary>
        string Value { get; }
    }

    /// <summary>
    /// Represents a variable token.
    /// </summary>
    public class VariableToken : IToken
    {
        /// <summary>
        /// The text value of the variable.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Any leading modifier characters removed from the variable.
        /// </summary>
        public List<char> Modifiers { get; set; } = [];

        /// <inheritdoc />
        public override string ToString()
        {
            string mods = (Modifiers.Count > 0) ? $" [Modifiers: {string.Join(",", Modifiers)}]" : "";
            return $"{{ Value = \"{Value}\", Type = Variable{mods} }}";
        }
    }

    /// <summary>
    /// Represents an "and" operator token.
    /// </summary>
    public class AndToken : IToken
    {
        /// <summary>
        /// The text value of the operator.
        /// </summary>
        public string Value { get; set; }

        /// <inheritdoc />
        public override string ToString() => $"{{ Value = \"{Value}\", Type = And }}";
    }

    /// <summary>
    /// Represents an "or" operator token.
    /// </summary>
    public class OrToken : IToken
    {
        /// <summary>
        /// The text value of the operator.
        /// </summary>
        public string Value { get; set; }

        /// <inheritdoc />
        public override string ToString() => $"{{ Value = \"{Value}\", Type = Or }}";
    }

    /// <summary>
    /// Represents an open container token (e.g. an opening parenthesis).
    /// </summary>
    public class OpenContainerToken : IToken
    {
        /// <summary>
        /// The text value of the token.
        /// </summary>
        public string Value { get; set; }

        /// <inheritdoc />
        public override string ToString() => $"{{ Value = \"{Value}\", Type = OpenContainer }}";
    }

    /// <summary>
    /// Represents a close container token (e.g. a closing parenthesis).
    /// </summary>
    public class CloseContainerToken : IToken
    {
        /// <summary>
        /// The text value of the token.
        /// </summary>
        public string Value { get; set; }

        /// <inheritdoc />
        public override string ToString() => $"{{ Value = \"{Value}\", Type = CloseContainer }}";
    }

    /// <summary>
    /// Represents a function token with a function name, a collection of parameters, and any modifiers.
    /// </summary>
    public class FunctionToken : VariableToken
    {
        /// <summary>
        /// The function name, after stripping any modifiers.
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// The parameters of the function, split into a collection of strings.
        /// </summary>
        public List<string> Parameters { get; set; } = [];

        /// <inheritdoc />
        public override string ToString()
        {
            string mods = (Modifiers.Count > 0) ? $" [Modifiers: {string.Join(",", Modifiers)}]" : "";
            string paramStr = string.Join(", ", Parameters);
            return $"{{ Value = \"{Value}\", Type = Function, FunctionName = \"{FunctionName}\", Parameters = \"{paramStr}\"{mods} }}";
        }
    }

    /// <summary>
    /// Configuration for the tokenizer.
    /// </summary>
    public class TokenizerConfig
    {
        /// <summary>
        /// Gets the operator used for "and".
        /// </summary>
        public string AndOperator { get; private set; }

        /// <summary>
        /// Gets the operator used for "or".
        /// </summary>
        public string OrOperator { get; private set; }

        /// <summary>
        /// Gets the character used for opening containers (e.g. '(').
        /// </summary>
        public char OpenContainer { get; private set; }

        /// <summary>
        /// Gets the character used for closing containers (e.g. ')').
        /// </summary>
        public char CloseContainer { get; private set; }

        /// <summary>
        /// Gets the character used for quoting strings.
        /// </summary>
        public char Quote { get; private set; }

        /// <summary>
        /// Gets a value indicating whether operators inside quoted text should be ignored.
        /// </summary>
        public bool IgnoreOperatorsInQuotes { get; private set; }

        /// <summary>
        /// Gets the set of modifier characters.
        /// </summary>
        public HashSet<char> ModifierChars { get; private set; }

        /// <summary>
        /// Gets a value indicating whether whitespace is used as a delimiter.
        /// </summary>
        public bool SplitOnWhitespace { get; private set; }

        /// <summary>
        /// Gets the character used to separate parameters in a function call.
        /// </summary>
        public char ParameterSeparator { get; private set; }

        // Private constructor.
        private TokenizerConfig() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenizerConfig"/> class.
        /// </summary>
        public TokenizerConfig(string andOperator, string orOperator, char openContainer, char closeContainer, char quote,
                           bool ignoreOperatorsInQuotes, HashSet<char> modifierChars, bool splitOnWhitespace, char parameterSeparator)
        {
            AndOperator = andOperator;
            OrOperator = orOperator;
            OpenContainer = openContainer;
            CloseContainer = closeContainer;
            Quote = quote;
            IgnoreOperatorsInQuotes = ignoreOperatorsInQuotes;
            ModifierChars = modifierChars;
            SplitOnWhitespace = splitOnWhitespace;
            ParameterSeparator = parameterSeparator;
        }

        /// <summary>
        /// Creates a new builder for constructing a <see cref="TokenizerConfig"/>.
        /// </summary>
        public static Builder NewBuilder() => new();

        /// <summary>
        /// Builder class for constructing a <see cref="TokenizerConfig"/>.
        /// </summary>
        public class Builder
        {
            internal string _andOperator = "&&";
            internal string _orOperator = "||";
            internal char _openContainer = '(';
            internal char _closeContainer = ')';
            internal char _quote = '\''; // Default: single quote.
            internal bool _ignoreOperatorsInQuotes = true;
            internal HashSet<char> _modifierChars = [];
            internal bool _splitOnWhitespace = true;
            internal char _parameterSeparator = ','; // Default separator.

            /// <summary>
            /// Sets the "and" operator.
            /// </summary>
            public Builder SetAndOperator(string op) { _andOperator = op; return this; }

            /// <summary>
            /// Sets the "or" operator.
            /// </summary>
            public Builder SetOrOperator(string op) { _orOperator = op; return this; }

            /// <summary>
            /// Sets the open container character.
            /// </summary>
            public Builder SetOpenContainer(char c) { _openContainer = c; return this; }

            /// <summary>
            /// Sets the close container character.
            /// </summary>
            public Builder SetCloseContainer(char c) { _closeContainer = c; return this; }

            /// <summary>
            /// Sets the quote character.
            /// </summary>
            public Builder SetQuote(char c) { _quote = c; return this; }

            /// <summary>
            /// Sets whether operators inside quoted text should be ignored.
            /// </summary>
            public Builder SetIgnoreOperatorsInQuotes(bool ignore) { _ignoreOperatorsInQuotes = ignore; return this; }

            /// <summary>
            /// Sets the modifier characters.
            /// </summary>
            /// <param name="modifierChars">A params array of modifier characters.</param>
            public Builder SetModifierChars(params char[] modifierChars) { _modifierChars = [.. modifierChars]; return this; }

            /// <summary>
            /// Sets whether whitespace should be used as a delimiter.
            /// </summary>
            public Builder SetSplitOnWhitespace(bool split) { _splitOnWhitespace = split; return this; }

            /// <summary>
            /// Sets the parameter separator character used in function calls.
            /// </summary>
            public Builder SetParameterSeparator(char sep) { _parameterSeparator = sep; return this; }

            /// <summary>
            /// Configures the builder for C-style languages (C, C++, Java).
            /// </summary>
            public Builder UseCStyle()
            {
                _andOperator = "&&";
                _orOperator = "||";
                _openContainer = '(';
                _closeContainer = ')';
                _quote = '\"'; // C-style typically uses double quotes.
                _ignoreOperatorsInQuotes = true;
                _splitOnWhitespace = true;
                _parameterSeparator = ',';
                return this;
            }

            /// <summary>
            /// Configures the builder for Python-style languages.
            /// </summary>
            public Builder UsePythonStyle()
            {
                _andOperator = "and";
                _orOperator = "or";
                _openContainer = '(';
                _closeContainer = ')';
                _quote = '\''; // Python commonly uses single quotes.
                _ignoreOperatorsInQuotes = true;
                _splitOnWhitespace = true;
                _parameterSeparator = ',';
                return this;
            }

            /// <summary>
            /// Configures the builder for MATLAB/R-style syntax.
            /// </summary>
            public Builder UseMatlabStyle()
            {
                _andOperator = "&";
                _orOperator = "|";
                _openContainer = '(';
                _closeContainer = ')';
                _quote = '\'';
                _ignoreOperatorsInQuotes = true;
                _splitOnWhitespace = true;
                _parameterSeparator = ',';
                return this;
            }

            /// <summary>
            /// Configures the builder for SQL-style syntax.
            /// </summary>
            public Builder UseSQLStyle()
            {
                _andOperator = "AND";
                _orOperator = "OR";
                _openContainer = '(';
                _closeContainer = ')';
                _quote = '\'';
                _ignoreOperatorsInQuotes = true;
                _splitOnWhitespace = true;
                _parameterSeparator = ',';
                return this;
            }

            /// <summary>
            /// Builds and returns the <see cref="TokenizerConfig"/> instance.
            /// </summary>
            public TokenizerConfig Build()
            {
                return new TokenizerConfig(
                    _andOperator,
                    _orOperator,
                    _openContainer,
                    _closeContainer,
                    _quote,
                    _ignoreOperatorsInQuotes,
                    _modifierChars,
                    _splitOnWhitespace,
                    _parameterSeparator
                );
            }
        }
    }

    /// <summary>
    /// Tokenizes an input string into tokens using the specified configuration.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="Tokenizer"/> class with the given configuration.
    /// </remarks>
    /// <param name="config">The tokenizer configuration.</param>
    public class Tokenizer(TokenizerConfig config)
    {
        private readonly TokenizerConfig _config = config;
        private readonly char _escapeChar = '\\';

        /// <summary>
        /// Tokenizes the input string into a list of tokens.
        /// </summary>
        /// <param name="input">The input string to tokenize.</param>
        /// <returns>A list of tokens.</returns>
        /// <exception cref="Exception">
        /// Thrown if the input cannot be tokenized (for example, due to an unbalanced container or invalid operator placement).
        /// </exception>
        public List<IToken> Tokenize(string input)
        {
            List<IToken> tokens = [];
            StringBuilder buffer = new();
            int i = 0;
            while (i < input.Length)
            {
                char c = input[i];

                // Handle whitespace.
                if (char.IsWhiteSpace(c))
                {
                    if (_config.SplitOnWhitespace)
                        FlushBufferAsVariableToken(buffer, tokens);
                    else
                        buffer.Append(c);
                    i++;
                    continue;
                }

                // Process escape characters: append the next character literally.
                if (c == _escapeChar)
                {
                    i++;
                    if (i < input.Length)
                    {
                        buffer.Append(input[i]);
                        i++;
                    }
                    continue;
                }

                // Process quote characters.
                if (c == _config.Quote)
                {
                    // Only treat as a quoted token if this quote is at the start of a new token.
                    if (buffer.Length == 0)
                    {
                        string quotedLiteral = ReadQuoted(input, ref i); // Reads content inside quotes (excluding the quotes).
                        tokens.Add(new VariableToken { Value = quotedLiteral });
                        continue;
                    }
                    else
                    {
                        // In the middle of a token, treat the quote as a literal character.
                        buffer.Append(c);
                        i++;
                        continue;
                    }
                }

                // Check for the "and" operator.
                if (IsMatchOperator(input, i, _config.AndOperator))
                {
                    FlushBufferAsVariableToken(buffer, tokens);
                    tokens.Add(new AndToken { Value = _config.AndOperator });
                    i += _config.AndOperator.Length;
                    continue;
                }

                // Check for the "or" operator.
                if (IsMatchOperator(input, i, _config.OrOperator))
                {
                    FlushBufferAsVariableToken(buffer, tokens);
                    tokens.Add(new OrToken { Value = _config.OrOperator });
                    i += _config.OrOperator.Length;
                    continue;
                }

                // Check for an open container.
                if (c == _config.OpenContainer)
                {
                    if (buffer.Length > 0)
                    {
                        // Process function token.
                        string functionName = buffer.ToString();
                        buffer.Clear();
                        List<char> modChars = [];
                        while (functionName.Length > 0 && _config.ModifierChars.Contains(functionName[0]))
                        {
                            modChars.Add(functionName[0]);
                            functionName = functionName[1..];
                        }
                        if (!TryParseBalanced(input, i, out string containerContent, out int newIndex))
                            throw new Exception("Unbalanced container starting at position " + i);
                        // Extract the parameter string (excluding the outer container characters).
                        string paramStr = containerContent[1..^1];
                        // Split parameters at top-level occurrences of the parameter separator.
                        List<string> paramList = SplitParameters(paramStr);
                        string fullFunctionValue = (modChars.Count > 0 ? new string([.. modChars]) : "") + functionName + containerContent;
                        tokens.Add(new FunctionToken
                        {
                            Value = fullFunctionValue,
                            FunctionName = functionName,
                            Parameters = paramList,
                            Modifiers = modChars
                        });
                        i = newIndex;
                        continue;
                    }
                    else
                    {
                        FlushBufferAsVariableToken(buffer, tokens);
                        tokens.Add(new OpenContainerToken { Value = _config.OpenContainer.ToString() });
                        i++;
                        continue;
                    }
                }

                // Check for a close container.
                if (c == _config.CloseContainer)
                {
                    FlushBufferAsVariableToken(buffer, tokens);
                    tokens.Add(new CloseContainerToken { Value = _config.CloseContainer.ToString() });
                    i++;
                    continue;
                }

                // Otherwise, accumulate the character.
                buffer.Append(c);
                i++;
            }
            FlushBufferAsVariableToken(buffer, tokens);
            return tokens;
        }

        /// <summary>
        /// Attempts to tokenize the given input string.
        /// If tokenization succeeds, returns <c>true</c> and sets the out parameter to the resulting tokens.
        /// If an exception is thrown during tokenization, returns <c>false</c> and sets the out parameter to an empty list.
        /// </summary>
        /// <param name="input">The input string to tokenize.</param>
        /// <param name="tokens">When successful, the list of tokens produced from the input; otherwise, an empty list.</param>
        /// <returns><c>true</c> if tokenization succeeds; otherwise, <c>false</c>.</returns>
        public bool TryTokenize(string input, out List<IToken> tokens)
        {
            try
            {
                tokens = Tokenize(input);
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Unable to parse line\n{input}\n{e}");
                tokens = [];
                return false;
            }
        }

        /// <summary>
        /// Splits a parameter string into a list of parameters at top-level occurrences of the parameter separator.
        /// </summary>
        /// <param name="paramStr">The parameter string to split.</param>
        /// <returns>A list of trimmed parameter strings.</returns>
        private List<string> SplitParameters(string paramStr)
        {
            List<string> parameters = [];
            StringBuilder current = new();
            int depth = 0;
            int i = 0;
            while (i < paramStr.Length)
            {
                char c = paramStr[i];

                // Handle escape characters.
                if (c == _escapeChar)
                {
                    if (i + 1 < paramStr.Length)
                    {
                        current.Append(paramStr[i + 1]);
                        i += 2;
                        continue;
                    }
                    else
                    {
                        current.Append(c);
                        i++;
                        continue;
                    }
                }
                // Increase depth on open container.
                if (c == _config.OpenContainer)
                {
                    depth++;
                    current.Append(c);
                    i++;
                    continue;
                }
                // Decrease depth on close container.
                if (c == _config.CloseContainer)
                {
                    depth--;
                    current.Append(c);
                    i++;
                    continue;
                }
                // If we encounter the parameter separator at top level, split.
                if (c == _config.ParameterSeparator && depth == 0)
                {
                    parameters.Add(current.ToString().Trim());
                    current.Clear();
                    i++;
                    continue;
                }
                current.Append(c);
                i++;
            }
            if (current.Length > 0)
                parameters.Add(current.ToString().Trim());
            return parameters;
        }

        /// <summary>
        /// Flushes any buffered text as a variable token, processing any leading modifier characters.
        /// </summary>
        /// <param name="buffer">The buffer containing accumulated characters.</param>
        /// <param name="tokens">The list of tokens to which the new token is added.</param>
        private void FlushBufferAsVariableToken(StringBuilder buffer, List<IToken> tokens)
        {
            if (buffer.Length > 0)
            {
                string tokenValue = buffer.ToString();
                buffer.Clear();

                List<char> modifiers = [];
                while (tokenValue.Length > 0 && _config.ModifierChars.Contains(tokenValue[0]))
                {
                    modifiers.Add(tokenValue[0]);
                    tokenValue = tokenValue[1..];
                }
                if (!string.IsNullOrEmpty(tokenValue))
                {
                    tokens.Add(new VariableToken
                    {
                        Value = tokenValue,
                        Modifiers = modifiers
                    });
                }
            }
        }

        /// <summary>
        /// Checks whether the substring at the current index matches the given operator.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="index">The starting index.</param>
        /// <param name="op">The operator string to match.</param>
        /// <returns><c>true</c> if the substring matches; otherwise, <c>false</c>.</returns>
        private static bool IsMatchOperator(string input, int index, string op)
        {
            if (string.IsNullOrEmpty(op)) return false;
            if (index + op.Length > input.Length) return false;
            return input.Substring(index, op.Length) == op;
        }

        /// <summary>
        /// Reads a quoted string from the input (handling escapes) and returns its content (excluding the quotes).
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="index">
        /// The current index in the input string. The index is updated to point after the closing quote.
        /// </param>
        /// <returns>The content inside the quotes.</returns>
        private string ReadQuoted(string input, ref int index)
        {
            index++; // Skip the opening quote.
            StringBuilder sb = new();
            while (index < input.Length)
            {
                char c = input[index];
                if (c == _escapeChar)
                {
                    index++;
                    if (index < input.Length)
                    {
                        sb.Append(input[index]);
                        index++;
                    }
                    continue;
                }
                if (c == _config.Quote)
                {
                    index++; // Skip the closing quote.
                    break;
                }
                sb.Append(c);
                index++;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Attempts to parse a balanced container (supporting nested containers) starting at the specified index.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="startIndex">The starting index where the container is expected.</param>
        /// <param name="containerContent">
        /// When successful, contains the substring corresponding to the balanced container (including the outer container characters).
        /// </param>
        /// <param name="newIndex">
        /// When successful, contains the index immediately after the closing container.
        /// </param>
        /// <returns><c>true</c> if a balanced container was successfully parsed; otherwise, <c>false</c>.</returns>
        private bool TryParseBalanced(string input, int startIndex, out string containerContent, out int newIndex)
        {
            containerContent = "";
            newIndex = startIndex;
            if (startIndex >= input.Length || input[startIndex] != _config.OpenContainer)
                return false;
            int count = 0;
            int i = startIndex;
            while (i < input.Length)
            {
                char c = input[i];
                if (c == _escapeChar)
                {
                    i += 2;
                    continue;
                }
                if (c == _config.OpenContainer)
                    count++;
                else if (c == _config.CloseContainer)
                {
                    count--;
                    if (count == 0)
                    {
                        i++;
                        containerContent = input[startIndex..i];
                        newIndex = i;
                        return true;
                    }
                }
                i++;
            }
            return false;
        }

        /// <summary>
        /// Checks the tokenized list for rule violations according to defined logic rules:
        /// - Two consecutive logic operator tokens (AndToken or OrToken) are not allowed.
        /// - A logic operator cannot immediately follow an OpenContainerToken.
        /// - An OpenContainerToken must only appear at the start of the token list or immediately following an operator or another OpenContainerToken.
        /// </summary>
        /// <param name="tokens">The token list to check.</param>
        /// <exception cref="Exception">Throws an exception with a descriptive message if any rule violation is found.</exception>
        public void CheckTokenList(List<IToken> tokens)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                // Two consecutive logic operators are not allowed.
                if (i > 0 && (tokens[i] is AndToken || tokens[i] is OrToken) &&
                    (tokens[i - 1] is AndToken || tokens[i - 1] is OrToken))
                {
                    throw new Exception($"Error: Two consecutive logic operators found at positions {i - 1} and {i}.");
                }

                // An operator cannot immediately follow an OpenContainerToken.
                if (i > 0 && (tokens[i] is AndToken || tokens[i] is OrToken) &&
                    (tokens[i - 1] is OpenContainerToken))
                {
                    throw new Exception($"Error: A logic operator at position {i} cannot immediately follow an open container.");
                }

                // Rule 3: An open container must only appear at the start or after an operator or another open container.
                if (tokens[i] is OpenContainerToken && i > 0 &&
                    !(tokens[i - 1] is AndToken || tokens[i - 1] is OrToken || tokens[i - 1] is OpenContainerToken))
                {
                    throw new Exception($"Error: An open container at position {i} must follow an operator or another open container.");
                }
            }
        }
    }
}
