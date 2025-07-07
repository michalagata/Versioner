using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AnubisWorks.Tools.Versioner.Helper;

namespace AnubisWorks.Tools.Versioner.Interfaces
{
    public interface IGitOperations
    {
        string GetGitExecutablePath();
        string GetGitVersion();
        (string output, string error) GetGitLog(string workingDirectory);
        List<GitLogEntry> GetGitLogs(string workingDirectory);
        string GetBuildLabel(string workingDirectory);
        string GetGitHash(string workingDirectory);
    }
} 