using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;
using AnubisWorks.Tools.Versioner.Helper;
using AnubisWorks.Tools.Versioner.Interfaces;

namespace AnubisWorks.Tools.Versioner.Model
{
    public class VersioningRequest : IRequest<VersioningResponse>
    {
        public string WorkingFolder { get; set; }
        public string Configuration { get; set; }
        public bool IsMonoRepo { get; set; }
        public string DefinedPatch { get; set; }
        public bool VersionOnlyProps { get; set; }
        public bool VersionNuspecsEvenIfOtherFilesExist { get; set; }
        public bool SetProjectGuid { get; set; }
        public string ProjectsGuidConfiguration { get; set; }
    }

    public class VersioningResponse
    {
        public string BuildLabel { get; set; }
        public string ArtifactVersion { get; set; }
        public string MonoRepoUpperVersion { get; set; }
        public List<string> ConsoleOutputs { get; set; } = new List<string>();
    }

    public class VersioningRequestHandler : IRequestHandler<VersioningRequest, VersioningResponse>
    {
        private readonly IGitOperations _gitOperations;
        private readonly IFileOperations _fileOperations;
        private readonly IVersioningOperations _versioningOperations;
        private readonly ILogger _log;

        public VersioningRequestHandler(
            IGitOperations gitOperations,
            IFileOperations fileOperations,
            IVersioningOperations versioningOperations,
            ILogger log)
        {
            _gitOperations = gitOperations;
            _fileOperations = fileOperations;
            _versioningOperations = versioningOperations;
            _log = log;
        }

        public async Task<VersioningResponse> Handle(VersioningRequest request)
        {
            var response = new VersioningResponse();

            // Obliczenie BuildLabel i wersji
            _log.Information("Calculating BuildLabel");
            response.BuildLabel = _gitOperations.GetBuildLabel(request.WorkingFolder);
            response.ConsoleOutputs.Add($"##teamcity[setParameter name='env.BuildLabel' value='{response.BuildLabel}']");
            _log.Information("BuildLabel: {BuildLabel}", response.BuildLabel);

            string gitHash = _gitOperations.GetGitHash(request.WorkingFolder);
            _log.Information("Calculating semVer for artifact version");
            List<GitLogEntry> logEntries = _gitOperations.GetGitLogs(request.WorkingFolder);
            response.ArtifactVersion = _versioningOperations.CalculateArtifactVersion(logEntries);
            response.MonoRepoUpperVersion = _versioningOperations.CalculateMonoRepoVersion(response.ArtifactVersion);

            if (!string.IsNullOrEmpty(request.DefinedPatch))
            {
                response.MonoRepoUpperVersion = _versioningOperations.ReplacePatchUnit(response.MonoRepoUpperVersion, request.DefinedPatch);
            }

            response.ConsoleOutputs.Add($"##teamcity[setParameter name='env.ArtifactVersion' value='{response.ArtifactVersion}']");
            _log.Information("Calculated ArtifactVersion: {ArtifactVersion}", response.ArtifactVersion);

            return response;
        }
    }
} 