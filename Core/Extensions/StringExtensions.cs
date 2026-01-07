using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using AnubisWorks.Tools.Versioner.Helper;

namespace AnubisWorks.Tools.Versioner
{
    /// <summary>
    /// Provides cross-platform string extension methods for path handling and string manipulation.
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Removes illegal characters from a path and normalizes it for the current platform.
        /// </summary>
        /// <param name="path">The path to clean.</param>
        /// <returns>A cleaned path normalized for the current platform.</returns>
        public static string RemoveIllegalCharactersPath(this string path)
        {
            return path.RemoveQuotesFromString().RemoveIllegalPathCharacters().NormalizePath();
        }

        /// <summary>
        /// Removes illegal path characters from a string.
        /// </summary>
        /// <param name="path">The path to clean.</param>
        /// <returns>A string with illegal path characters removed.</returns>
        public static string RemoveIllegalPathCharacters(this string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            
            string regex = $"[{Regex.Escape(new string(Path.GetInvalidPathChars()))}]";
            Regex removeInvalidChars = new Regex(regex, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant);

            return removeInvalidChars.Replace(path, "");
        }

        /// <summary>
        /// Removes quotes and normalizes path separators in a string.
        /// </summary>
        /// <param name="path">The path to clean.</param>
        /// <returns>A cleaned path string.</returns>
        public static string RemoveQuotesFromString(this string path)
        {
            if (path == null) return string.Empty;
            return path.Replace("\"", "").Replace("/\"", "").Replace("//", "/").Replace("\"\"", "\"");
        }

        /// <summary>
        /// Normalizes a path to use the appropriate path separator for the current platform.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>A normalized path string.</returns>
        public static string NormalizePath(this string path)
        {
            if (string.IsNullOrEmpty(path)) return path;
            
            char separator = PlatformDetector.GetPathSeparator();
            char oppositeSeparator = separator == '/' ? '\\' : '/';
            
            return path.Replace(oppositeSeparator, separator);
        }

        /// <summary>
        /// Converts a path to use forward slashes (Unix-style).
        /// </summary>
        /// <param name="path">The path to convert.</param>
        /// <returns>A path with forward slashes.</returns>
        public static string ToUnixPath(this string path)
        {
            return path?.Replace("\\", "/");
        }

        public static bool AreEqual(this string str1, string str2)
        {
            return string.Compare(str1, str2, StringComparison.InvariantCultureIgnoreCase) == 0;
        }

        public static string Rlb(this string str, string delimiter = " ")
        {
            return str?.Replace("\r", "").Replace("\n", delimiter);
        }

        public static bool IsNotEmpty(this string str)
        {
            return !string.IsNullOrWhiteSpace(str);
        }

        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }

        public static string ReturnPropperIntNumberings(this int inp)
        {
            string _temp = inp.ToString();

            if(_temp.Length == 0) throw new Exception("Empty INT for conversion");

            if(_temp.Length == 1)
            {
                _temp = "0" + _temp;
            }

            return _temp;
        }

        public static string ReturnNoLeadingZero(this string str)
        {
            if (!string.IsNullOrEmpty(str) && str[0] == '0')
            {
                str = "1" + str.Substring(1);
            }

            return str;
        }
    }

    public static class TableExtensions
    {
        public static T GetValueOrDefault<T>(this T[] arr, int idx, T def)
        {
            if(idx<arr.Length) return arr[idx];

            return def;
        }
    }

    public static class RegexExtensions
    {
        public static string ReplaceGroup(
            this Regex regex, string input, string groupName, string replacement)
        {
            return regex.Replace(
                input,
                m =>
                {
                    var group = m.Groups[groupName];
                    var sb = new StringBuilder();
                    var previousCaptureEnd = 0;
                    foreach(var capture in group.Captures.Cast<Capture>())
                    {
                        var currentCaptureEnd =
                            capture.Index + capture.Length - m.Index;
                        var currentCaptureLength =
                            capture.Index - m.Index - previousCaptureEnd;
                        sb.Append(
                            m.Value.Substring(
                                previousCaptureEnd, currentCaptureLength));
                        sb.Append(replacement);
                        previousCaptureEnd = currentCaptureEnd;
                    }

                    sb.Append(m.Value.Substring(previousCaptureEnd));

                    return sb.ToString();
                });
        }
    }
}