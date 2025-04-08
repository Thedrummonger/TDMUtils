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

        public ColoredString()
        {
            words = [];
        }

        public ColoredString(string text)
        {
            words = [];
            AddText(text);
        }

        public ColoredString(string text, Color defaultColor)
        {
            words = [];
            AddText(text, defaultColor);
        }

        // Adds a word with default color (black), auto-spaces by default
        public ColoredString AddText(string text, bool addSpace = true)
        {
            words.Add((text + (addSpace ? " " : ""), null));
            return this;
        }

        // Adds a word with a specific color, auto-spaces by default
        public ColoredString AddText(string text, Color color, bool addSpace = true)
        {
            words.Add((text + (addSpace ? " " : ""), color));
            return this;
        }

        // Gets the plain text version without trailing spaces
        public override string ToString()
        {
            StringBuilder sb = new();
            foreach (var (text, _) in words)
            {
                sb.Append(text);
            }
            return sb.ToString().TrimEnd(); // Remove final space
        }

        // Exposes the words for rendering
        public List<(string Text, Color? Color)> Words => words;

        /// <summary>
        /// Builds an RTF snippet for the given colored strings.
        /// </summary>
        public static string BuildColoredStringsRtf(IEnumerable<ColoredString> coloredStrings, Color? DefaultForeColor = null)
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
            var sb = new StringBuilder(@"{\rtf1\ansi")
                .Append(@"{\colortbl ;")
                .Append(string.Join("", colorMap.Select(kv => $@"\red{kv.Key.R}\green{kv.Key.G}\blue{kv.Key.B};")))
                .Append('}');
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
