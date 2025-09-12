using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AnubisWorks.Tools.Versioner
{
    public static class StringExtensions
    {
        public static string RemoveIllegalCharactersWindowsPath(this string path)
        {
            return path.RemoveQuotesFromString().RemoveIllegalPathCharacters().ToWindowsPath();
        }

        public static string RemoveIllegalCharactersLinuxPath(this string path)
        {
            return path.RemoveQuotesFromString().RemoveIllegalPathCharacters().ToLinuxPath();
        }

        public static string RemoveIllegalPathCharacters(this string path)
        {
            string regex = $"[{Regex.Escape(new string(Path.GetInvalidPathChars()))}]";
            Regex removeInvalidChars = new Regex(regex, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant);

            return removeInvalidChars.Replace(path, "");

        }

        public static string RemoveQuotesFromString(this string path)
        {
            if (path == null) return string.Empty;
            return path.Replace("\"", "").Replace("/\"", "").Replace("//","/").Replace("\"\"","\"");
        }

        public static string ToLinuxPath(this string path)
        {
            return path?.Replace("\\", "/");
        }

        public static string ToWindowsPath(this string path)
        {
            return path?.Replace("/", "\\");
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