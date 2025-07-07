using System.Collections.Generic;
using AnubisWorks.Tools.Versioner.Model;
using AnubisWorks.Tools.Versioner.Helper;

namespace AnubisWorks.Tools.Versioner.Interfaces
{
    public interface IGitLogService
    {
        List<GitLogEntry> GetLogEntries(
            string gitPath, 
            string workingFolder, 
            string projectFilePath, 
            VersioningBaseConfiguration config);
            
        GitLogEntry GetLatestLogEntry(List<GitLogEntry> logEntries);
    }
} 