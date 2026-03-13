using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TDMUtils
{
    public class ColoredString
    {
        public static Color AppDeafultTextColor { get; set; } = Color.Black;
        public Color _DefaultColor { get; private set; } = AppDeafultTextColor;
        private List<(string Text, Color? Color)> words;
        /// <summary>
        /// Creates a deep copy of another <see cref="ColoredString"/>.
        /// </summary>
        public ColoredString(ColoredString other)
        {
            words = [.. other.words];
            _DefaultColor = other._DefaultColor;
        }
        /// <summary>
        /// Initializes an empty <see cref="ColoredString"/>.
        /// </summary>
        public ColoredString()
        {
            words = [];
        }
        /// <summary>
        /// Initializes a <see cref="ColoredString"/> with plain text and no color formatting.
        /// </summary>
        /// <param name="text">The text to add.</param>
        public ColoredString(string text)
        {
            words = [];
            AddText(text);
        }
        /// <summary>
        /// Initializes a <see cref="ColoredString"/> with text using a default color.
        /// </summary>
        /// <param name="text">The text to add.</param>
        /// <param name="defaultColor">The color to use for the text.</param>
        public ColoredString(string text, Color defaultColor)
        {
            words = [];
            AddText(text, defaultColor);
        }
        /// <summary>
        /// Adds text to the string with no specific color.
        /// </summary>
        /// <param name="text">The text to add.</param>
        /// <param name="addSpace">If true, a space is appended after the text.</param>
        /// <returns>The current <see cref="ColoredString"/> instance for chaining.</returns>
        public ColoredString AddText(string text, bool addSpace = true)
        {
            words.Add((text + (addSpace ? " " : ""), null));
            return this;
        }
        /// <summary>
        /// Adds colored text to the string.
        /// </summary>
        /// <param name="text">The text to add.</param>
        /// <param name="color">The color to apply to the text.</param>
        /// <param name="addSpace">If true, a space is appended after the text.</param>
        /// <returns>The current <see cref="ColoredString"/> instance for chaining.</returns>
        public ColoredString AddText(string text, Color color, bool addSpace = true)
        {
            words.Add((text + (addSpace ? " " : ""), color));
            return this;
        }
        /// <summary>
        /// Returns the plain text representation of this <see cref="ColoredString"/> without color codes.
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new();
            foreach (var (text, _) in words)
            {
                sb.Append(text);
            }
            return sb.ToString().TrimEnd();
        }
        /// <summary>
        /// Gets the collection of text segments and their assigned colors.
        /// </summary>
        public List<(string Text, Color? Color)> Words => words;

        /// <summary>
        /// Pads the visible text on the right to the specified total width.
        /// ANSI formatting is preserved.
        /// </summary>
        public ColoredString PadRight(int totalWidth, char paddingChar = ' ')
        {
            int visibleLength = ToString().Length;
            int needed = Math.Max(0, totalWidth - visibleLength - 1);
            if (needed > 0)
                AddText(new string(paddingChar, needed), false);
            return this;
        }

        /// <summary>
        /// Pads the visible text on the left to the specified total width.
        /// ANSI formatting is preserved.
        /// </summary>
        public ColoredString PadLeft(int totalWidth, char paddingChar = ' ')
        {
            int visibleLength = ToString().Length;
            int needed = Math.Max(0, totalWidth - visibleLength - 1);
            if (needed > 0)
                AddText(new string(paddingChar, needed), false);
            return this;
        }

        public ColoredString WithDefaultColor(Color color) 
        { 
            _DefaultColor = color; 
            return this; 
        }

        /// <summary>
        /// Builds an ANSI-formatted string that will display colored text in supported console environments.
        /// </summary>
        /// <returns>An ANSI escape-coded string suitable for console output.</returns>
        public string BuildAnsi() => Build([this], FormatAnsi, null, () => "\x1b[0m");

        /// <summary>
        /// Builds a TextMeshPro / Unity Rich Text string using &lt;color=#RRGGBB&gt; tags.
        /// </summary>
        /// <returns>A string suitable for TextMeshPro or Unity UI text.</returns>
        public string BuildTextMeshPro() => Build([this], FormatTextMeshPro);

        /// <summary>
        /// Builds an HTML string using &lt;span style="color:#RRGGBB"&gt; tags.
        /// </summary>
        /// <returns>An HTML string suitable for web pages, emails, or WebView content.</returns>
        public string BuildHtml() => Build([this], FormatHtml);

        /// <summary>
        /// Builds a BBCode string using [color=#RRGGBB] tags.
        /// </summary>
        /// <returns>A BBCode string suitable for forums or BBCode-enabled systems.</returns>
        public string BuildBbCode() => Build([this], FormatBbCode);

        /// <summary>
        /// Builds an RTF-formatted string representing this <see cref="ColoredString"/>.
        /// </summary>
        /// <returns>An RTF string suitable for use in a <see cref="System.Windows.Forms.RichTextBox"/>.</returns>
        public string BuildRtf() => BuildRtf(this);

        /// <summary>
        /// Builds an RTF-formatted string from a collection of <see cref="ColoredString"/> objects.
        /// </summary>
        /// <param name="coloredStrings">A collection of colored strings to include.</param>
        /// <returns>An RTF string suitable for use in a <see cref="System.Windows.Forms.RichTextBox"/>.</returns>
        public static string BuildRtf(params IEnumerable<ColoredString> coloredStrings)
        {
            Dictionary<Color, int> colorMap = [];
            int nextIndex = 1;

            foreach (var cs in coloredStrings)
                foreach (var (_, color) in cs.Words)
                {
                    var c = color ?? cs._DefaultColor;
                    if (!colorMap.ContainsKey(c))
                        colorMap[c] = nextIndex++;
                }

            StringBuilder sb = new();
            sb.Append(BuildRtfPrefix(colorMap));

            foreach (var cs in coloredStrings)
            {
                foreach (var (text, color) in cs.Words)
                    sb.Append($@"\cf{colorMap[color ?? cs._DefaultColor]} {EscapeRtf(text)}");

                sb.Append(@"\line ");
            }

            return sb.Append('}').ToString();
        }

        /// <summary>
        /// Builds a string from multiple <see cref="ColoredString"/> objects using a custom formatter
        /// with optional document prefix and suffix.
        /// </summary>
        /// <param name="coloredStrings">The colored strings to format.</param>
        /// <param name="formatter">Formats each text segment with its resolved color.</param>
        /// <param name="prefix">Optional function that appends a document prefix.</param>
        /// <param name="suffix">Optional function that appends a document suffix.</param>
        /// <returns>The formatted string.</returns>
        public static string Build(IEnumerable<ColoredString> coloredStrings, Func<string, Color, string> formatter, Func<string>? prefix = null, Func<string>? suffix = null)
        {
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));

            StringBuilder sb = new();

            if (prefix != null)
                sb.Append(prefix());

            foreach (var cs in coloredStrings)
                foreach (var (text, color) in cs.Words)
                    sb.Append(formatter(text, color ?? cs._DefaultColor));

            if (suffix != null)
                sb.Append(suffix());

            return sb.ToString();
        }

        private static string FormatAnsi(string text, Color color) => 
            $"\x1b[38;2;{color.R};{color.G};{color.B}m{text}";

        private static string FormatTextMeshPro(string text, Color color) =>
            $@"<color=#{color.R:X2}{color.G:X2}{color.B:X2}>{EscapeRichText(text)}</color>";

        private static string FormatHtml(string text, Color color) =>
            $@"<span style=""color:#{color.R:X2}{color.G:X2}{color.B:X2}"">{EscapeHtml(text)}</span>";

        private static string FormatBbCode(string text, Color color) =>
            $@"[color=#{color.R:X2}{color.G:X2}{color.B:X2}]{text}[/color]";

        private static string BuildRtfPrefix(Dictionary<Color, int> colorMap) =>
            new StringBuilder(@"{\rtf1\ansi{\colortbl ;").Append(string.Join("", colorMap.Keys.Select(k => $@"\red{k.R}\green{k.G}\blue{k.B};"))).Append('}').ToString();

        private static string EscapeHtml(string text) =>
            string.IsNullOrEmpty(text) ? string.Empty : 
            text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");

        private static string EscapeRtf(string text) =>
            text is null ? string.Empty : text.Replace(@"\", @"\\").Replace("{", @"\{").Replace("}", @"\}");

        private static string EscapeRichText(string text) =>
            string.IsNullOrEmpty(text) ? string.Empty :
            text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    }
}
