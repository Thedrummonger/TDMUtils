using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDMUtils.Tokenizer
{
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
        public List<char> Modifiers { get; set; } = new List<char>();

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
    public class FunctionToken : IToken
    {
        /// <summary>
        /// The full text value of the function token.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// The function name, after stripping any modifiers.
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// The parameters of the function, split into a collection of strings.
        /// </summary>
        public List<string> Parameters { get; set; } = new List<string>();

        /// <summary>
        /// Any modifier characters (for example, '!' in "!setting(...)") removed from the function name.
        /// </summary>
        public List<char> Modifiers { get; set; } = new List<char>();

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
        /// Creates a new builder for the tokenizer configuration.
        /// </summary>
        public static Builder NewBuilder() => new Builder();

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
            // Default modifiers if not overridden manually.
            internal HashSet<char> _modifierChars = new HashSet<char> { '!', '%', '$' };
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
            public Builder SetModifierChars(params char[] modifierChars) { _modifierChars = new HashSet<char>(modifierChars); return this; }

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
                // Reset modifiers to empty – application specific.
                _modifierChars = new HashSet<char>();
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
                // Reset modifiers to empty.
                _modifierChars = new HashSet<char>();
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
                // Reset modifiers to empty.
                _modifierChars = new HashSet<char>();
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
                // Reset modifiers to empty.
                _modifierChars = new HashSet<char>();
                _splitOnWhitespace = true;
                _parameterSeparator = ',';
                return this;
            }

            /// <summary>
            /// Builds the <see cref="TokenizerConfig"/> instance.
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
    public class Tokenizer
    {
        private readonly TokenizerConfig _config;
        private readonly char _escapeChar = '\\';

        /// <summary>
        /// Initializes a new instance of the <see cref="Tokenizer"/> class with the given configuration.
        /// </summary>
        public Tokenizer(TokenizerConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Tokenizes the input string into a list of tokens.
        /// </summary>
        /// <param name="input">The input string to tokenize.</param>
        /// <returns>A list of tokens.</returns>
        public List<IToken> Tokenize(string input)
        {
            List<IToken> tokens = new List<IToken>();
            StringBuilder buffer = new StringBuilder();
            int i = 0;
            while (i < input.Length)
            {
                char c = input[i];

                // Handle whitespace.
                if (char.IsWhiteSpace(c))
                {
                    if (_config.SplitOnWhitespace)
                    {
                        FlushBufferAsVariableToken(buffer, tokens);
                    }
                    else
                    {
                        buffer.Append(c);
                    }
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
                        int quoteStart = i;  // Save position.
                        string quotedLiteral = ReadQuoted(input, ref i); // Reads content inside quotes (excluding the quotes).

                        // Look ahead, ignoring whitespace, to decide if the token ends here.
                        int temp = i;
                        while (temp < input.Length && char.IsWhiteSpace(input[temp]))
                        {
                            temp++;
                        }
                        bool tokenComplete = (temp >= input.Length) || IsDelimiter(input[temp]);
                        if (tokenComplete)
                        {
                            tokens.Add(new VariableToken { Value = quotedLiteral });
                        }
                        else
                        {
                            // Not at token boundaries; treat the quotes as literal text.
                            buffer.Append(input.Substring(quoteStart, i - quoteStart));
                        }
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
                        // If buffered text starts with a quote, treat it as a literal variable rather than a function call.
                        if (buffer[0] == _config.Quote)
                        {
                            string literal = buffer.ToString();
                            if (literal.Length >= 2 && literal[0] == _config.Quote && literal[literal.Length - 1] == _config.Quote)
                            {
                                literal = literal.Substring(1, literal.Length - 2);
                            }
                            tokens.Add(new VariableToken { Value = literal });
                            buffer.Clear();
                            tokens.Add(new OpenContainerToken { Value = _config.OpenContainer.ToString() });
                            i++;
                            continue;
                        }
                        else
                        {
                            // Process function token.
                            string rawFunctionName = buffer.ToString();
                            buffer.Clear();
                            List<char> modChars = new List<char>();
                            string functionName = rawFunctionName;
                            while (functionName.Length > 0 && _config.ModifierChars.Contains(functionName[0]))
                            {
                                modChars.Add(functionName[0]);
                                functionName = functionName.Substring(1);
                            }
                            if (!TryParseBalanced(input, i, out string containerContent, out int newIndex))
                            {
                                throw new Exception("Unbalanced container starting at position " + i);
                            }
                            // Extract the parameter string (excluding the outer container characters).
                            string paramStr = containerContent.Substring(1, containerContent.Length - 2);
                            // Split parameters at top-level occurrences of the parameter separator.
                            List<string> paramList = SplitParameters(paramStr);
                            string fullFunctionValue = (modChars.Count > 0 ? new string(modChars.ToArray()) : "") + functionName + containerContent;
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
        /// Splits a parameter string into a list of parameters at top-level occurrences of the separator.
        /// </summary>
        private List<string> SplitParameters(string paramStr)
        {
            List<string> parameters = new List<string>();
            StringBuilder current = new StringBuilder();
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
            {
                parameters.Add(current.ToString().Trim());
            }
            return parameters;
        }

        /// <summary>
        /// Flushes any buffered text as a variable token, processing any leading modifier characters.
        /// </summary>
        private void FlushBufferAsVariableToken(StringBuilder buffer, List<IToken> tokens)
        {
            if (buffer.Length > 0)
            {
                string tokenValue = buffer.ToString();
                buffer.Clear();

                List<char> modifiers = new List<char>();
                while (tokenValue.Length > 0 && _config.ModifierChars.Contains(tokenValue[0]))
                {
                    modifiers.Add(tokenValue[0]);
                    tokenValue = tokenValue.Substring(1);
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
        private bool IsMatchOperator(string input, int index, string op)
        {
            if (string.IsNullOrEmpty(op)) return false;
            if (index + op.Length > input.Length) return false;
            return input.Substring(index, op.Length) == op;
        }

        /// <summary>
        /// Reads a quoted string from the input (handling escapes) and returns its content (excluding the quotes).
        /// </summary>
        private string ReadQuoted(string input, ref int index)
        {
            index++; // Skip the opening quote.
            StringBuilder sb = new StringBuilder();
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
        /// Parses a balanced container (supporting nested containers) starting at the specified index.
        /// </summary>
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
                        containerContent = input.Substring(startIndex, i - startIndex);
                        newIndex = i;
                        return true;
                    }
                }
                i++;
            }
            return false;
        }

        /// <summary>
        /// Determines if the specified character is considered a delimiter.
        /// </summary>
        private bool IsDelimiter(char ch)
        {
            if (char.IsWhiteSpace(ch))
                return true;
            if (ch == _config.OpenContainer || ch == _config.CloseContainer)
                return true;
            if (!string.IsNullOrEmpty(_config.AndOperator) && ch == _config.AndOperator[0])
                return true;
            if (!string.IsNullOrEmpty(_config.OrOperator) && ch == _config.OrOperator[0])
                return true;
            return false;
        }
    }
}
