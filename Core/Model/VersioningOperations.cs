using System.Collections.Generic;
using AnubisWorks.Tools.Versioner.Helper;
using AnubisWorks.Tools.Versioner.Interfaces;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Model
{
    public class VersioningOperations : IVersioningOperations
    {
        private readonly ILogger _logger;

        public VersioningOperations(ILogger logger)
        {
            _logger = logger;
        }

        public string CalculateArtifactVersion(List<GitLogEntry> logEntries)
        {
            return SemVerHelper.ReturnArtifactVersionBase(logEntries);
        }

        public string CalculateMonoRepoVersion(string artifactVersion)
        {
            return SemVerHelper.ReturnMonoRepoArtifactVersion(artifactVersion);
        }

        public string ReplacePatchUnit(string version, string newPatch)
        {
            if (string.IsNullOrEmpty(newPatch))
            {
                return version;
            }
            return version.ReplaceLastUnit(newPatch);
        }

        public string GetDockerVersion(string artifactVersion)
        {
            return artifactVersion;
        }
    }
} 