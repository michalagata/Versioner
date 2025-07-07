using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace AnubisWorks.Tools.Versioner.Helper
{
    public static class StringParseHelper
    {
        #region Unused

        private const string regexPatternV0 =
            @"^(\d|[1-9]\d*)\.(\d|[1-9]\d*)\.(\d|[1-9]\d*)(-(0|[1-9A-Za-z-][0-9A-Za-z-]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)(\.(0|[1-9A-Za-z-][0-9A-Za-z-]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*))*)?(\+[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$
";

        const string regexMaskV1 =
            @"(?<Major>0|(?:[1-9]\d*))(?:\.(?<Minor>0|(?:[1-9]\d*))(?:\.(?<Patch>0|(?:[1-9]\d*)))?(?:\-(?<PreRelease>[0-9A-Z\.-]+))?(?:\+(?<Meta>[0-9A-Z\.-]+))?)?";


        private const string regexMaskPropperSemver =
            @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$";

        private const string regexMaskFallback = @"(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)";

        private const string regexMaskFallback3 = @"(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)";
        #endregion

        private const string regexMaskPrimal =
            @"(?:[\.\-](?<Major>0|(?:[1-9]\d*)))(?:\.(?<Minor>0|(?:[1-9]\d*))(?:\.(?<Patch>0|(?:[1-9]\d*)))(?:\.(?<Hotfix>0|(?:[1-9]\d*))))?";

        private const string regexMaskPrimal3 =
            @"(?:[\.\-](?<Major>0|(?:[1-9]\d*)))(?:\.(?<Minor>0|(?:[1-9]\d*))(?:\.(?<Patch>0|(?:[1-9]\d*))))?";
        
        private static readonly string[] wrongCharacters = new[] {".", "-", " "};

        public static string ReplaceSemver(this string input, string repl)
        {
            Match version;

            version = ReturnMatchedString(input, regexMaskPrimal);
            if (version.Success && version.Value.Length > 6)
            {
                return input.Replace(version.Value.ReplaceWrongBeginning(), repl);
            }

            //version = ReturnMatchedString(input, regexMaskFallback);
            //if (version.Success && version.Value.Length > 6)
            //{
            //    return input.Replace(version.Value, repl);
            //}

            version = ReturnMatchedString(input, regexMaskPrimal3);
            if (version.Success && version.Value.Length > 4)
            {
                return input.Replace(version.Value.ReplaceWrongBeginning(), repl);
            }

            //version = ReturnMatchedString(input, regexMaskFallback3);
            //if (version.Success && version.Value.Length > 4)
            //{
            //    return input.Replace(version.Value, repl);
            //}

            else return input;
        }

        private static Match ReturnMatchedString(string input, string mask)
        {
            Regex regEx = new Regex(mask, RegexOptions.Compiled);
            return regEx.Match(input);
        }

        public static string ReplaceWrongBeginning(this string input)
        {
            string ret = input;

            foreach (string wrongCharacter in wrongCharacters)
            {
                if(ret.StartsWith(wrongCharacter)) ret = ret.Substring(1, input.Length - 1);
            }

            return ret;
        }

        public static string GetParameterValue(this string input, string wordIndex, string quantificator)
        {
            int zeroIndex = input.IndexOf(wordIndex);
            int startIndex = input.IndexOf(quantificator, zeroIndex) + 1;
            int endIndex = input.IndexOf(quantificator, startIndex + 1);
            string workingString = input.Substring(startIndex, endIndex - startIndex);
            
            return workingString;//.Replace(quantificator,"");
        }

        public static string InputDictionaryIntoString(this List<string> input, string nameParameter,
            string valueParameter, string quantificator, string separator)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string s in input)
            {
                sb.Append(s.GetParameterValue(nameParameter, quantificator));
                sb.Append("=");
                sb.Append(s.GetParameterValue(valueParameter, quantificator));
                sb.Append(separator);
            }

            if (sb.ToString().EndsWith(separator)) return sb.ToString().Substring(0, sb.ToString().Length - 1); else
                return sb.ToString();
        }

        public static string EnsurePropperDateMonthsFormat(this string inp)
        {
            if (inp.Length == 0) throw new Exception("Inpropper month number");

            if (inp.Length == 1) return $"0{inp}";

            return inp;
        }
    }
}