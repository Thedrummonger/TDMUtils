using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TDMUtils
{
    public static class StringUtilities
    {
        /// <summary>
        /// Replaces a range of text in a string with a replacement string
        /// </summary>
        /// <param name="s">The source string</param>
        /// <param name="index">The starting index of the text to replace</param>
        /// <param name="length">The length of the text to replace</param>
        /// <param name="replacement">The replacement text</param>
        /// <returns></returns>
        public static string Replace(this string s, int index, int length, string replacement)
        {
#if NETCOREAPP2_1_OR_GREATER
            return string.Create(s.Length - length + replacement.Length, (s, index, length, replacement),
                static (dst, state) =>
                {
                    var (src, i, len, rep) = state;
                    src.AsSpan(0, i).CopyTo(dst);
                    rep.AsSpan().CopyTo(dst.Slice(i));
                    src.AsSpan(i + len).CopyTo(dst.Slice(i + rep.Length));
                });
#else
            return new StringBuilder(s.Length - length + replacement.Length)
                .Append(s.Substring(0, index))
                .Append(replacement)
                .Append(s.Substring(index + length))
                .ToString();
#endif
        }
        public static bool IsNullOrWhiteSpace(this string s) => string.IsNullOrWhiteSpace(s);
        public static bool IsNullOrEmpty(this string s) => string.IsNullOrEmpty(s);
        /// <summary>
        /// Splits a string by a substring and trims all the resulting substrings
        /// </summary>
        /// <param name="s">The source string</param>
        /// <returns></returns>
        /// 
        public static string[] SplitAtNewLine(this string s) => s.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        /// <summary>
        /// Splits a string and trims the values
        /// </summary>
        /// <param name="s">The source string</param>
        /// <param name="Val">The substring to split on</param>
        /// <returns></returns>
        /// 
        public static string[] TrimSplit(this string s, string Val)
        {
#if NET5_0_OR_GREATER
            return [.. s.Split(Val).Select(x => x.Trim())];
#else
            return [.. s.Split([Val], StringSplitOptions.None).Select(x => x.Trim())];
#endif
        }
        /// <summary>
        /// Splits a string by the first occurrence of the given char
        /// </summary>
        /// <param name="input">The string to split</param>
        /// <param name="Split">The Char to split at</param>
        /// <param name="LastOccurrence">Split by the last occurrence rather than the first</param>
        /// <returns>A tuple containing the the two resulting strings</returns>
        public static Tuple<string, string> SplitOnce(this string input, char Split, bool LastOccurrence = false)
        {
            int idx = LastOccurrence ? input.LastIndexOf(Split) : input.IndexOf(Split);

            if (idx != -1)
                return new Tuple<string, string>(input.Substring(0, idx), input.Substring(idx + 1));

            return new Tuple<string, string>(input, string.Empty);
        }
        /// <summary>
        /// Checks if the string represents an integer range
        /// </summary>
        /// <param name="x">The input string</param>
        /// <param name="Values">A tuple containing the min and max of the given range</param>
        /// <returns>Was the string a range?</returns>
        public static bool IsIntegerRange(this string x, out Tuple<int, int> Values)
        {
            Values = new(-1, -1);
            if (!x.Contains('-')) { return false; }
            var Segments = x.Split('-');
            if (Segments.Length != 2) { return false; }
            if (!int.TryParse(Segments[0], out int RawInt1)) { return false; }
            if (!int.TryParse(Segments[1], out int RawInt2)) { return false; }
            Values = new(Math.Min(RawInt1, RawInt2), Math.Max(RawInt1, RawInt2));
            return true;
        }

        public static bool IsIpAddress(string Input, out IPAddress IP)
        {
            bool WasIP = true;
            var Segments = Input.Split('.');
            if (Segments.Length != 4 || Segments.Any(x => x.Any(y => !char.IsDigit(y)))) { WasIP = false; }
            if (!IPAddress.TryParse(Input, out IP)) { WasIP = false; }
            if (!WasIP)
            {
                IPAddress[] addresslist;
                try { addresslist = Dns.GetHostAddresses(Input); }
                catch { addresslist = []; }
                if (addresslist.Length != 0)
                {
                    WasIP = true;
                    IP = addresslist.First();
                }
            }
            return WasIP;
        }
        /// <summary>
        /// Removes extra white spaces in a string so the string will never contain more than one whitespace in a row
        /// </summary>
        /// <param name="myString">String to trim</param>
        /// <returns></returns>
        public static string TrimSpaces(this string myString) => Regex.Replace(myString, @"\s+", " ");
        /// <summary>
        /// Converts a string representing a version to a version object
        /// </summary>
        /// <param name="version">Version String</param>
        /// <returns></returns>
        public static Version AsVersion(this string version)
        {
            if (!version.Any(x => char.IsDigit(x))) { version = "0"; }
            if (!version.Contains('.')) { version += ".0"; }
            return new Version(string.Join("", version.Where(x => char.IsDigit(x) || x == '.')));
        }
        /// <summary>
        /// Checks if the given string is enclosed in single quotes
        /// </summary>
        /// <param name="ID">The source string</param>
        /// <param name="CleanedID">the source string with quotes removed</param>
        /// <returns></returns>
        public static bool IsLiteralID(this string ID, out string CleanedID)
        {
            bool Literal = false;
            CleanedID = ID.Trim();
            if (CleanedID.Length >= 2 && CleanedID[0] == '\'' && CleanedID[CleanedID.Length - 1] == '\'')
            {
                Literal = true;
                CleanedID = CleanedID.Substring(1, CleanedID.Length - 2);
            }
            return Literal;
        }
        /// <summary>
        /// Capitalizes the first letter of words in the given string
        /// </summary>
        /// <param name="Input">Source string</param>
        /// <returns></returns>
        public static string ConvertToCamelCase(this string Input)
        {
            string NiceName = Input.ToLower();
            TextInfo cultInfo = new CultureInfo("en-US", false).TextInfo;
            NiceName = cultInfo.ToTitleCase(NiceName);
            return NiceName;
        }

        public static string AddWordSpacing(this string Input)
        {
            return Regex.Replace(Input, "([a-z])([A-Z])", "$1 $2");
        }
    }
}
