using System;
using System.Collections.Generic;
using System.Linq;
using AnubisWorks.Tools.Versioner.Helper;
using AnubisWorks.Tools.Versioner.Model;
using AnubisWorks.Tools.Versioner.Interfaces;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Services
{
    public interface IDockerVersioningService
    {
        string CalculateDockerVersion(string workFolder, bool overrideExists, MajorMinorPatchHotfixModel overrideModel);
        void SetDockerEnvironmentVariables(string dockerVersion);
    }

    public class DockerVersioningService : IDockerVersioningService
    {
        private readonly ILogger _log;
        private readonly IGitOperations _gitOperations;
        private readonly IFileOperations _fileOperations;

        public DockerVersioningService(ILogger log, IGitOperations gitOperations, IFileOperations fileOperations)
        {
            _log = log;
            _gitOperations = gitOperations;
            _fileOperations = fileOperations;
        }

        public string CalculateDockerVersion(string workFolder, bool overrideExists, MajorMinorPatchHotfixModel overrideModel)
        {
            _log.Debug("Start browsing the directory to search for files Dockerfile");
            var dockerFiles = _fileOperations.FindDockerfiles(workFolder);
            _log.Debug("Found {count} Dockerfile entries. Loading...", dockerFiles.Count);

            if (!dockerFiles.Any())
            {
                return "unspecified";
            }

            _log.Information("Found Dockerfiles, calculating docker semVer");
            List<GitLogEntry> logEntries = _gitOperations.GetGitLogs(workFolder);
            MajorMinorPatchDockerModel mDL = SemVerHelper.ReturnDockerVersionBase(logEntries);
            
            if (overrideExists)
            {
                mDL.Major = overrideModel.Major.ToString();
                mDL.Minor = overrideModel.Minor.ToString();
            }

            string dockerVersion = $"{mDL.Major}.{mDL.Minor}.{mDL.Patch}";
            _log.Information("Calculated DockerBuildLabel: {DockerBuildLabel}", dockerVersion);
            
            return dockerVersion;
        }

        public void SetDockerEnvironmentVariables(string dockerVersion)
        {
            System.Environment.SetEnvironmentVariable("env.DockerBuildLabel", dockerVersion);
        }
    }
} 