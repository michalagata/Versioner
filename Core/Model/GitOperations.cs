using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using AnubisWorks.Tools.Versioner.Helper;
using AnubisWorks.Tools.Versioner.Interfaces;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Model
{
    public class GitOperations : IGitOperations
    {
        private readonly ILogger _logger;
        private string _gitExecutable;

        public GitOperations(ILogger logger)
        {
            _logger = logger;
            _gitExecutable = InitializeGitExecutable();
        }

        private string InitializeGitExecutable()
        {
            if (PlatformDetector.GetOperatingSystem() == OSPlatform.Windows)
            {
                return "git";
            }

            if (PlatformDetector.GetOperatingSystem() == OSPlatform.Linux || 
                PlatformDetector.GetOperatingSystem() == OSPlatform.OSX)
            {
                if (File.Exists("/bin/git")) return "/bin/git";
                if (File.Exists("/usr/bin/git")) return "/usr/bin/git";
                if (File.Exists("/usr/local/bin/git")) return "/usr/local/bin/git";
            }

            return "git";
        }

        public string GetGitExecutablePath() => _gitExecutable;

        public string GetGitVersion()
        {
            var (output, error) = ProcessWrapper.RunProccess(_gitExecutable, "--version");
            if (!string.IsNullOrEmpty(error))
            {
                _logger.Error("Error getting git version: {error}", error);
                throw new Exception("Failed to get git version");
            }
            return output;
        }

        public (string output, string error) GetGitLog(string workingDirectory)
        {
            return ProcessWrapper.RunProccess(_gitExecutable, 
                $"log -1 --date=format:%Y,%y,%m,%d,%H,%M,%S --format=format:%ad,%H,%h");
        }

        public List<GitLogEntry> GetGitLogs(string workingDirectory)
        {
            return GitCommands.GitLogs(_gitExecutable, workingDirectory, _logger);
        }

        public string GetBuildLabel(string workingDirectory)
        {
            var (output, error) = GetGitLog(workingDirectory);
            if (!string.IsNullOrEmpty(error))
            {
                _logger.Error("Error getting BuildLabel: {error}", error);
                throw new Exception("Failed to get build label");
            }

            var table = output.Split(",", StringSplitOptions.None);
            var year = table[0];
            var month = table[2].EnsurePropperDateMonthsFormat();
            var day = table[3];
            var hour = table[4];
            var minute = table[5];
            var hash_s = table[8];

            return $"REV_{year:0000}{month:00}{day:00}_{hour:00}{minute:00}_{hash_s}";
        }

        public string GetGitHash(string workingDirectory)
        {
            var (output, error) = GetGitLog(workingDirectory);
            if (!string.IsNullOrEmpty(error))
            {
                _logger.Error("Error getting git hash: {error}", error);
                throw new Exception("Failed to get git hash");
            }

            var table = output.Split(",", StringSplitOptions.None);
            return table[8];
        }

        public string GetGitRepositoryRoot(string workingDirectory)
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _gitExecutable,
                    Arguments = "rev-parse --show-toplevel",
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0 || !string.IsNullOrEmpty(error))
                {
                    _logger.Error("Error getting git repository root: {error}", error);
                    throw new Exception($"Failed to get git repository root: {error}");
                }

                var rootPath = output.Trim().Replace("\n", "").Replace("\r", "");
                if (string.IsNullOrWhiteSpace(rootPath))
                {
                    throw new Exception("Git repository root path is empty");
                }

                return rootPath;
            }
            finally
            {
                process?.Dispose();
            }
        }

        public bool IsGitRepository(string directory)
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _gitExecutable,
                    Arguments = "rev-parse --git-dir",
                    WorkingDirectory = directory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return process.ExitCode == 0 && string.IsNullOrEmpty(error);
            }
            catch
            {
                return false;
            }
            finally
            {
                process?.Dispose();
            }
        }
    }
} 