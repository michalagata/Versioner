using System.Collections.Generic;
using System.Linq;
using AnubisWorks.Tools.Versioner.Helper;
using AnubisWorks.Tools.Versioner.Interfaces;
using AnubisWorks.Tools.Versioner.Model;

namespace AnubisWorks.Tools.Versioner.Infrastructure.Services
{
    public class VersionPatternService : IVersionPatternService
    {
        public string GenerateAssemblyVersionPattern(
            VersioningBaseConfiguration config,
            TimeModel timeModel,
            string pos0,
            string pos1,
            string pos2,
            string pos3,
            string pos4,
            string pos5)
        {
            if (!timeModel.OverrideWithLocalFile)
            {
                return string.Format(config.AssemblyVersionFormat, pos0, pos1, pos2, pos3, pos4, pos5);
            }
            else
            {
                return string.Format(config.AssemblyVersionFormatOverride, pos0, pos1, pos2, pos3, pos4, pos5);
            }
        }

        public string GenerateAssemblyInfoVersionPattern(
            VersioningBaseConfiguration config,
            TimeModel timeModel,
            string pos0,
            string pos1,
            string pos2,
            string pos3,
            string pos4,
            string pos5)
        {
            if (!timeModel.OverrideWithLocalFile)
            {
                return string.Format(config.AssemblyInfoVersionFormat, pos0, pos1, pos2, pos3, pos4, pos5);
            }
            else
            {
                return string.Format(config.AssemblyInfoVersionFormatOverride, pos0, pos1, pos2, pos3, pos4, pos5);
            }
        }

        public string GenerateAssemblyFileVersionPattern(
            VersioningBaseConfiguration config,
            TimeModel timeModel,
            string pos0,
            string pos1,
            string pos2,
            string pos3,
            string pos4,
            string pos5)
        {
            if (!timeModel.OverrideWithLocalFile)
            {
                return string.Format(config.AssemblyFileVersionFormat, pos0, pos1, pos2, pos3, pos4, pos5);
            }
            else
            {
                return string.Format(config.AssemblyFileVersionFormatOverride, pos0, pos1, pos2, pos3, pos4, pos5);
            }
        }

        public string CalculateVersionFromPattern(string currentVersion, string pattern)
        {
            if (string.IsNullOrEmpty(currentVersion)) currentVersion = "1.0.0.0";
            if (!pattern.Contains("*") || string.IsNullOrEmpty(pattern)) return pattern;
            
            List<string> nVer = new List<string>();
            string[] t_patt = pattern.Split(".", System.StringSplitOptions.None);
            string[] t_curr = currentVersion.Split(".", System.StringSplitOptions.None);
            
            for (int i = 0; i < t_patt.Length; i++)
            {
                string vp = t_curr.Length > i ? t_curr[i] : (i + 1).ToString();

                if (!t_patt[i].Contains("*")) vp = t_patt.Length > i ? t_patt[i] : "";

                if (!string.IsNullOrWhiteSpace(vp)) nVer.Add(vp);
            }

            string outt = string.Join(".", nVer);
            return outt;
        }
    }
} 