using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using AnubisWorks.Tools.Versioner;
using Serilog;
using AnubisWorks.Tools.Versioner.Model;

namespace AnubisWorks.Tools.Versioner.Helper
{
    public class GitLogEntry
    {
        public DateTime Date { get; set; }
        public string LongHash { get; set; }
        public string ShortHash { get; set; }
        public string Directory { get; set; }
    }

    public static class GitCommands
    {
        public static List<GitLogEntry> GitLogs(string gitPath, string projDir, ILogger log)
        {
            var fullLog4ProjectDir = ProcessWrapper.RunProccess(gitPath, $" log --date=format:\"%Y-%m-%d %H:%M:%S\" --pretty=format:%ad,%H,%h -- \"{projDir}\"");
            if(fullLog4ProjectDir.err.IsNotEmpty())
            {
                log.Error("Error getting log for project dir '{project_dir}'. {err}", projDir, fullLog4ProjectDir.err);
                Environment.Exit(603);
            }

            if(string.IsNullOrWhiteSpace(fullLog4ProjectDir.soutput))
            {
                log.Warning("git log for dir '{projDir}' is empty", projDir);

                return new List<GitLogEntry>();
            }

            return fullLog4ProjectDir.soutput
                .Split("\n")
                .Where(w => !string.IsNullOrWhiteSpace(w))
                .Select(l => l.Trim().Split(","))
                .Select(t => new GitLogEntry
                {
                    Date = DateTime.ParseExact(t[0], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeLocal),
                    LongHash = t[1],
                    ShortHash = t[2],
                    Directory = Path.GetRelativePath(Directory.GetCurrentDirectory(), projDir)
                })
                .OrderByDescending(o => o.Date)
                .ToList();
        }
    }
}
