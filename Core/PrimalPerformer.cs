using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AnubisWorks.Tools.Versioner.Helper;
using AnubisWorks.Tools.Versioner.Model;
using AnubisWorks.Tools.Versioner.Interfaces;
using AnubisWorks.Tools.Versioner.Sln;
using AnubisWorks.Tools.Versioner.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Events;

namespace AnubisWorks.Tools.Versioner
{
    public class PrimalPerformer
    {
        private ILogger log = Log.ForContext<PrimalPerformer>();
        private readonly ConfigurationsArgs _config;
        private readonly IGitOperations _gitOperations;
        private readonly IFileOperations _fileOperations;
        private readonly IVersioningOperations _versioningOperations;
        private readonly IMediator _mediator;
        private readonly IDockerVersioningService _dockerVersioningService;
        private readonly IOverrideVersioningService _overrideVersioningService;
        private readonly IConfigurationService _configurationService;
        public string BuildLabel { get; set; }
        public string dockVersion { get; set; } = "unspecified";
        public string artifactVersion { get; set; } = "unspecified";
        public List<string> ConsoleOutputs = new List<string>();
        public string Configuration { get; set; }
        public string WorkFolder { get; set; } = Directory.GetCurrentDirectory();
        public string gitExec { get; set; } = "git";
        private bool propsFound { get; set; } = false;

        public PrimalPerformer(ConfigurationsArgs cfg)
        {
            this._config = cfg;
            this._gitOperations = VersionerFactory.CreateGitOperations(Log.ForContext<PrimalPerformer>());
            this._fileOperations = VersionerFactory.CreateFileOperations(Log.ForContext<PrimalPerformer>());
            this._versioningOperations = VersionerFactory.CreateVersioningOperations(Log.ForContext<PrimalPerformer>());
            this._mediator = new Mediator(VersionerFactory.CreateServiceProvider());
            this._dockerVersioningService = VersionerFactory.CreateDockerVersioningService(Log.ForContext<PrimalPerformer>(), _gitOperations, _fileOperations);
            this._overrideVersioningService = VersionerFactory.CreateOverrideVersioningService(Log.ForContext<PrimalPerformer>(), _fileOperations);
            this._configurationService = VersionerFactory.CreateConfigurationService();

            if (PlatformDetector.GetOperatingSystem() == OSPlatform.Linux || PlatformDetector.GetOperatingSystem() == OSPlatform.OSX)
            {
                this._config.WorkingFolder = this._config.WorkingFolder.RemoveIllegalCharactersLinuxPath();
            }

            if (PlatformDetector.GetOperatingSystem() == OSPlatform.Windows)
            {
                this._config.WorkingFolder = this._config.WorkingFolder.RemoveIllegalCharactersWindowsPath();
            }
        }

        public PrimalPerformer(ConfigurationsArgs cfg, IGitOperations gitOperations, IFileOperations fileOperations, IVersioningOperations versioningOperations, IMediator mediator)
        {
            this._config = cfg;
            this._gitOperations = gitOperations;
            this._fileOperations = fileOperations;
            this._versioningOperations = versioningOperations;
            this._mediator = mediator;
            this._dockerVersioningService = VersionerFactory.CreateDockerVersioningService(Log.ForContext<PrimalPerformer>(), _gitOperations, _fileOperations);
            this._overrideVersioningService = VersionerFactory.CreateOverrideVersioningService(Log.ForContext<PrimalPerformer>(), _fileOperations);
            this._configurationService = VersionerFactory.CreateConfigurationService();

            if (PlatformDetector.GetOperatingSystem() == OSPlatform.Linux || PlatformDetector.GetOperatingSystem() == OSPlatform.OSX)
            {
                this._config.WorkingFolder = this._config.WorkingFolder.RemoveIllegalCharactersLinuxPath();
            }

            if (PlatformDetector.GetOperatingSystem() == OSPlatform.Windows)
            {
                this._config.WorkingFolder = this._config.WorkingFolder.RemoveIllegalCharactersWindowsPath();
            }
        }

        //List<ProjectEntity> Projects = new List<ProjectEntity>();

        public async Task Execute()
        {
            log = Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .WriteTo.Console(_config.logLevel,
                    "{Timestamp:HH:mm:ss.fff} [{Level:u1}] [{Context:u25}] {Message}{NewLine}{Exception}")
                .CreateLogger()
                .ForContext("Context", nameof(PrimalPerformer), true);
            CommitInfoTypeExtensions.CommitInformation commitInformation = typeof(PrimalPerformer).GetCommitInformation();
            log.Information("Versioner: Version: {FileVersion}, BuildLabel: {BuildLabel}", commitInformation.FileVersion,
                commitInformation.BuildLabel);
            try
            {
                // Set working directory
                WorkFolder = Path.GetFullPath(_config.WorkingFolder);
                _fileOperations.SetCurrentDirectory(WorkFolder);
                log.Information("Processing working directory set to {wkDir}", WorkFolder);

                // Check and load configuration
                Configuration = _configurationService.LoadConfiguration(_config.ConfigurationFile, _fileOperations);
                if (!string.IsNullOrWhiteSpace(_config.ConfigurationFile))
                {
                    log.Information("ConfigurationUse URL: {url}", _config.ConfigurationFile);
                }

                // Check if git is available
                try
                {
                    string gitVersion = _gitOperations.GetGitVersion();
                    log.Information("Git command: {GitExec}", _gitOperations.GetGitExecutablePath());
                }
                catch (Exception ex)
                {
                    log.Error("Most probably git executable is missing! Please install git or review below Exception Message!");
                    log.Error($"Exception occurred: {ex.Message}");
                    throw;
                }

                // Initialize configuration
                VersionedSetModel versionedSetModel = new VersionedSetModel();
                log.Information("Initializing configuration...");
                _configurationService.InitializeConfiguration(Configuration, log);
                log.Information("Configuration initialized. State.Config is null: {isNull}", State.Config == null);

                // Calculate MonoRepo mode
                if (!_config.IsMonoRepo && (_config.AllSlnLocations || !string.IsNullOrEmpty(_config.ExactProjectFile)))
                {
                    _config.IsMonoRepo = true;
                }

                // Handle ProjectGuids
                if (_config.SetProjectGuid)
                {
                    State.MaintainProjectGuids = _config.SetProjectGuid;
                    State.ProjectGuidsConfigurationFile = _config.ProjectsGuidConfiguration;
                    if (_fileOperations.FileExists(State.ProjectGuidsConfigurationFile))
                    {
                        string jsonString = _fileOperations.ReadFileContent(State.ProjectGuidsConfigurationFile);
                        State.ProjGuidsConfiguration = JsonConvert.DeserializeObject<ProjGuidsConfiguration>(jsonString);
                    }
                }

                // Execute versioning operations through mediator
                var versioningRequest = new VersioningRequest
                {
                    WorkingFolder = WorkFolder,
                    Configuration = Configuration,
                    IsMonoRepo = _config.IsMonoRepo,
                    DefinedPatch = _config.DefinedPatch,
                    VersionOnlyProps = _config.VersionOnlyProps,
                    VersionNuspecsEvenIfOtherFilesExist = _config.VersionNuspecsEvenIfOtherFilesExist,
                    SetProjectGuid = _config.SetProjectGuid,
                    ProjectsGuidConfiguration = _config.ProjectsGuidConfiguration
                };

                var versioningResponse = await _mediator.Send<VersioningRequest, VersioningResponse>(versioningRequest);
                
                BuildLabel = versioningResponse.BuildLabel;
                artifactVersion = versioningResponse.ArtifactVersion;
                ConsoleOutputs.AddRange(versioningResponse.ConsoleOutputs);

                string gitHash = _gitOperations.GetGitHash(WorkFolder);
                if (_config.IsMonoRepo)
                {
                    versionedSetModel = new VersionedSetModel(versioningResponse.MonoRepoUpperVersion, versioningResponse.MonoRepoUpperVersion + $"+{gitHash}", versioningResponse.MonoRepoUpperVersion, BuildLabel, $"Version: {BuildLabel}", gitHash);
                }
                else
                {
                    versionedSetModel = new VersionedSetModel(artifactVersion, artifactVersion + $"+{gitHash}", artifactVersion, BuildLabel, $"Version: {BuildLabel}", gitHash);
                }

                // Search for project files
                log.Debug("Start browsing the directory to search for files *.csproj");
                var files = _fileOperations.FindFiles("*.props", WorkFolder);
                if (files.Count == 0)
                {
                    _config.VersionOnlyProps = false;
                }

                if (!_config.VersionOnlyProps)
                {
                    files = _fileOperations.FindFiles("*.csproj", WorkFolder);
                }

                log.Debug("Start browsing the directory to search for files package.json");
                files.AddRange(_fileOperations.FindFiles("package.json", WorkFolder).Where(w => !w.Contains("node_modules")).ToList());

                log.Debug("Start browsing the directory to search for files *.nuspec");
                files.AddRange(_fileOperations.FindFiles("*.nuspec", WorkFolder));

                log.Debug("Found {count} props/csproj/package.json/nuspec entries. Loading...", files.Count);

                if (files.Count == 0)
                {
                    log.Error($"No version-based files found!");
                    Environment.Exit(500);
                }

                // Filter nuspec files
                bool otherfiles = false;
                bool nuspecs = false;
                foreach (string file in files)
                {
                    if (file.Contains(".nuspec"))
                    {
                        nuspecs = true;
                    }
                    else
                    {
                        otherfiles = true;
                    }
                }

                List<string> nextFiles = new List<string>();
                if (nuspecs && otherfiles && !_config.VersionNuspecsEvenIfOtherFilesExist)
                {
                    foreach (string file in files)
                    {
                        if (!file.Contains(".nuspec")) nextFiles.Add(file);
                    }
                    files.Clear();
                    foreach (string nextFile in nextFiles)
                    {
                        files.Add(nextFile);
                    }
                    nextFiles.Clear();
                }

                // Handle ProjectOverride
                var (overrideExists, overrideFileUrl, overrideModel) = _overrideVersioningService.LoadOverrideConfiguration(WorkFolder, _config.DefinedPatch);

                // Load CustomProjectSettings
                CustomProjectSettings customProjectSettings = new CustomProjectSettings();
                if (!string.IsNullOrEmpty(_config.CustomConfigurationFile) && _fileOperations.FileExists(_config.CustomConfigurationFile))
                {
                    log.Debug("Found custom configuration file {count}.", _config.CustomConfigurationFile);
                    customProjectSettings = JsonConvert.DeserializeObject<CustomProjectSettings>(_fileOperations.ReadFileContent(_config.CustomConfigurationFile));
                    if (customProjectSettings.ProjectSettings.Count > 0)
                    {
                        log.Debug("Loaded {count} custom settings.", customProjectSettings.ProjectSettings.Count);
                    }
                }

                // Handle MonoRepo
                if (_config.IsMonoRepo && (!string.IsNullOrEmpty(_config.ExactProjectFile) || _config.AllSlnLocations))
                {
                    string mainFile;
                    if (string.IsNullOrEmpty(_config.ExactProjectFile))
                    {
                        SlnHelper sHelper = new SlnHelper(_config.WorkingFolder, _config.AllSlnLocations);
                        sHelper.Initialize();
                        mainFile = sHelper.GetStartingProjectFile();
                    }
                    else
                    {
                        _config.ExactProjectFile = FilePathHelper.ParseSimpleInput(_config.ExactProjectFile);
                        if (!_config.ExactProjectFile.StartsWith("\\") || _config.ExactProjectFile.Left(2).Right(1) != ":")
                        {
                            _config.ExactProjectFile = Path.Combine(WorkFolder, _config.ExactProjectFile);
                        }

                        if (!_fileOperations.FileExists(_config.ExactProjectFile) && Path.GetExtension(_config.ExactProjectFile) != ".csproj")
                        {
                            log.Error("Pointed exact project file {projFile} does not exist", _config.ExactProjectFile);
                            Environment.Exit(600);
                        }

                        mainFile = _config.ExactProjectFile;
                    }

                    ProjectEntity proj = new ProjectEntity(_gitOperations.GetGitExecutablePath(), mainFile, WorkFolder, ref this.ConsoleOutputs, BuildLabel,
                        this._config.StoreVersionFile, overrideModel, customProjectSettings, _config.SemVersion, _config.PreReleaseSuffix, _config.DefinedPatch, true);

                    versionedSetModel = proj.ReturnCalculatedModel();

                    if (!files.Remove(mainFile))
                    {
                        throw new ArgumentOutOfRangeException("Processed element could not be removed");
                    }
                }

                // Version files
                if (_config.IsMonoRepo)
                {
                    this.log.Information("Running MonoRepo versioning...");
                    foreach (string file in files)
                    {
                        ProjectEntityMonoRepo proj = new ProjectEntityMonoRepo(file, WorkFolder, ref this.ConsoleOutputs,
                            BuildLabel,
                            this._config.StoreVersionFile, customProjectSettings, versionedSetModel);
                    }
                }
                else
                {
                    this.log.Information("Running SingleRepo versioning...");
                    foreach (string file in files)
                    {
                        ProjectEntity proj = new ProjectEntity(_gitOperations.GetGitExecutablePath(), file, WorkFolder, ref this.ConsoleOutputs, BuildLabel,
                            this._config.StoreVersionFile, overrideModel, customProjectSettings, _config.SemVersion, _config.PreReleaseSuffix, _config.DefinedPatch);
                    }
                }

                // Update ProjectGuids
                if (State.ProjGuidsConfiguration.StateChanged)
                {
                    string jsonString = JsonConvert.SerializeObject(State.ProjGuidsConfiguration, Formatting.Indented);
                    _fileOperations.WriteFileContent(State.ProjectGuidsConfigurationFile, jsonString);
                }

                // Handle Docker
                dockVersion = _dockerVersioningService.CalculateDockerVersion(WorkFolder, overrideExists, overrideModel);
                if (dockVersion != "unspecified")
                {
                    this.ConsoleOutputs.Add($"##teamcity[setParameter name='env.DockerBuildLabel' value='{dockVersion}']");
                }
            }
            catch (Exception ex)
            {
                log.Error(ex, "Error during execution");
                throw;
            }
            finally
            {
                this.ConsoleOutputs.ForEach(o => Console.Out.WriteLine(o));
                if (_config.DebugMode)
                {
                    System.Environment.SetEnvironmentVariable("env.BuildLabel", BuildLabel);
                    _dockerVersioningService.SetDockerEnvironmentVariables(dockVersion);
                    System.Environment.SetEnvironmentVariable("env.allBuildLabel", this.ConsoleOutputs.InputDictionaryIntoString("name", "value", "\'", "|"));
                }
            }
        }

        private string FillConfigWithDefaults(string json)
        {
            JObject o = JObject.Parse(json);
            VersioningBaseConfiguration defEntry = new VersioningBaseConfiguration();
            if(o.ContainsKey("Default"))
            {
                defEntry = o["Default"].ToObject<VersioningBaseConfiguration>();
            }

            var def = JToken.FromObject(defEntry);
            try
            {
                if(o.ContainsKey("Items"))
                {
                    foreach(JToken item in o.SelectToken("Items").Children())
                    {
                        if(item["AssemblyInfoFile"] == null) item["AssemblyInfoFile"] = def["AssemblyInfoFile"];
                        if(item["AssemblyInfoVersionSet"] == null) item["AssemblyInfoVersionSet"] = def["AssemblyInfoVersionSet"];
                        if(item["AssemblyInfoVersionFormat"] == null) item["AssemblyInfoVersionFormat"] = def["AssemblyInfoVersionFormat"];
                        if(item["AssemblyVersionSet"] == null) item["AssemblyVersionSet"] = def["AssemblyVersionSet"];
                        if(item["AssemblyVersionFormat"] == null) item["AssemblyVersionFormat"] = def["AssemblyVersionFormat"];
                        if(item["AssemblyFileVersionSet"] == null) item["AssemblyFileVersionSet"] = def["AssemblyFileVersionSet"];
                        if(item["AssemblyFileVersionFormat"] == null) item["AssemblyFileVersionFormat"] = def["AssemblyFileVersionFormat"];
                        if(item["HashAsDescription"] == null) item["HashAsDescription"] = def["HashAsDescription"];
                    }
                }
            }
            catch(Exception e)
            {
                log.Error("Error filling with defaults.");
                Environment.Exit(701);
            }

            return o.ToString(Formatting.Indented);
        }
    }
}