using System.Collections.Generic;
using AnubisWorks.Tools.Versioner.Helper;

namespace AnubisWorks.Tools.Versioner.Interfaces
{
    public interface IVersioningOperations
    {
        string CalculateArtifactVersion(List<GitLogEntry> logEntries);
        string CalculateMonoRepoVersion(string artifactVersion);
        string ReplacePatchUnit(string version, string newPatch);
        string GetDockerVersion(string artifactVersion);
    }
} 