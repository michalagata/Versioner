using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using AnubisWorks.Tools.Versioner.Helper;
using AnubisWorks.Tools.Versioner.Interfaces;
using AnubisWorks.Tools.Versioner.Model;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Infrastructure.Services
{
    public class GitLogService : IGitLogService
    {
        private readonly ILogger _log;

        public GitLogService(ILogger log)
        {
            _log = log;
        }

        public List<GitLogEntry> GetLogEntries(
            string gitPath,
            string workingFolder,
            string projectFilePath,
            VersioningBaseConfiguration config)
        {
            var projDir = Path.GetDirectoryName(projectFilePath);
            var logEntries = new List<GitLogEntry>();

            if (config.Directories?.Count > 0)
            {
                _log.Information(
                    "Project configuration contains multi-directory definition. Calculating sum of all commits.");
                
                config.Directories.ForEach(subDir =>
                {
                    string sdir = string.Empty;
                    if (PlatformDetector.GetOperatingSystem() == OSPlatform.Windows)
                        sdir = Path.Combine(workingFolder, subDir.ToWindowsPath());
                    if (PlatformDetector.GetOperatingSystem() == OSPlatform.Linux ||
                        PlatformDetector.GetOperatingSystem() == OSPlatform.OSX)
                        sdir = Path.Combine(workingFolder, subDir.ToLinuxPath());
                    
                    if (!Directory.Exists(sdir))
                        _log.Error("Directory {subDir} does not exists", subDir);
                    else
                    {
                        logEntries.AddRange(GitCommands.GitLogs(gitPath, sdir, _log));
                    }
                });
                
                logEntries = logEntries.OrderByDescending(o => o.Date).ToList();
            }
            else
            {
                logEntries.AddRange(GitCommands.GitLogs(gitPath, projDir, _log));
            }

            return logEntries;
        }

        public GitLogEntry GetLatestLogEntry(List<GitLogEntry> logEntries)
        {
            return logEntries.FirstOrDefault();
        }
    }
} 