using AnubisWorks.Tools.Versioner.Model;

namespace AnubisWorks.Tools.Versioner.Interfaces
{
    public interface IVersionPatternService
    {
        string GenerateAssemblyVersionPattern(
            VersioningBaseConfiguration config,
            TimeModel timeModel,
            int semVersion,
            string pos0,
            string pos1,
            string pos2,
            string pos3,
            string pos4,
            string pos5);

        string GenerateAssemblyInfoVersionPattern(
            VersioningBaseConfiguration config,
            TimeModel timeModel,
            int semVersion,
            string pos0,
            string pos1,
            string pos2,
            string pos3,
            string pos4,
            string pos5);

        string GenerateAssemblyFileVersionPattern(
            VersioningBaseConfiguration config,
            TimeModel timeModel,
            int semVersion,
            string pos0,
            string pos1,
            string pos2,
            string pos3,
            string pos4,
            string pos5);

        string CalculateVersionFromPattern(string currentVersion, string pattern);
    }
} 