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
        private List<(string Text, Color? Color)> words;
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
            int needed = Math.Max(0, totalWidth - visibleLength);
            if (needed > 0)
                words.Add((new string(paddingChar, needed), null));
            return this;
        }

        /// <summary>
        /// Pads the visible text on the left to the specified total width.
        /// ANSI formatting is preserved.
        /// </summary>
        public ColoredString PadLeft(int totalWidth, char paddingChar = ' ')
        {
            int visibleLength = ToString().Length;
            int needed = Math.Max(0, totalWidth - visibleLength);
            if (needed > 0)
                words.Insert(0, (new string(paddingChar, needed), null));
            return this;
        }


        /// <summary>
        /// Builds an ANSI-formatted string that will display colored text in supported console environments.
        /// </summary>
        /// <param name="DefaultForeColor">The fallback color for text without a defined color.</param>
        /// <returns>An ANSI escape-coded string suitable for console output.</returns>
        public string Build(Color? DefaultForeColor = null)
        {
            DefaultForeColor ??= Color.White;
            var sb = new StringBuilder();
            foreach (var (text, color) in words)
            {
                var c = color ?? DefaultForeColor.Value;
                sb.Append($"\x1b[38;2;{c.R};{c.G};{c.B}m").Append(text);
            }
            return sb.Append("\x1b[0m").ToString();
        }

        /// <summary>
        /// Builds an RTF-formatted string representing the given <see cref="ColoredString"/>.
        /// </summary>
        /// <param name="DefaultForeColor">The fallback color for text without a defined color.</param>
        /// <returns>An RTF string suitable for use in a <see cref="System.Windows.Forms.RichTextBox"/>.</returns>
        public string BuildRtf(Color? DefaultForeColor = null) => BuildRtf([this], DefaultForeColor);

        /// <summary>
        /// Builds an RTF-formatted string from a collection of <see cref="ColoredString"/> objects.
        /// </summary>
        /// <param name="coloredStrings">A collection of colored strings to include.</param>
        /// <param name="DefaultForeColor">The fallback color for text without a defined color.</param>
        /// <returns>An RTF string suitable for use in a <see cref="System.Windows.Forms.RichTextBox"/>.</returns>
        public static string BuildRtf(IEnumerable<ColoredString> coloredStrings, Color? DefaultForeColor = null)
        {
            DefaultForeColor ??= Color.Black;
            Dictionary<Color, int> colorMap = [];
            int nextIndex = 1;
            foreach (var cs in coloredStrings)
                foreach (var (word, color) in cs.Words)
                {
                    var c = color ?? DefaultForeColor.Value;
                    if (!colorMap.ContainsKey(c))
                        colorMap[c] = nextIndex++;
                }
            var sb = new StringBuilder(@"{\rtf1\ansi{\colortbl ;").Append(string.Join("", colorMap.Keys.Select(k => $@"\red{k.R}\green{k.G}\blue{k.B};"))).Append('}');
            foreach (var cs in coloredStrings)
            {
                foreach (var (word, color) in cs.Words)
                    sb.Append($@"\cf{colorMap[color ?? DefaultForeColor.Value]} {EscapeRtf(word)}");
                sb.Append(@"\line ");
            }
            return sb.Append('}').ToString();

            static string EscapeRtf(string text) => text is null ? "" : text.Replace(@"\", @"\\").Replace("{", @"\{").Replace("}", @"\}");
        }
    }
}
