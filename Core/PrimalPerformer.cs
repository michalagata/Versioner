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
        private readonly IGlobalRepoVersioningService _globalRepoVersioningService;
        private readonly IArtifactDiscoveryService _artifactDiscoveryService;
        private readonly IWebhookService _webhookService;
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
            this._globalRepoVersioningService = VersionerFactory.CreateGlobalRepoVersioningService(Log.ForContext<PrimalPerformer>(), _fileOperations);
            this._artifactDiscoveryService = VersionerFactory.CreateArtifactDiscoveryService(Log.ForContext<PrimalPerformer>(), _fileOperations);
            this._webhookService = VersionerFactory.CreateWebhookService(Log.ForContext<PrimalPerformer>());

            this._config.WorkingFolder = this._config.WorkingFolder.RemoveIllegalCharactersPath();
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
            this._globalRepoVersioningService = VersionerFactory.CreateGlobalRepoVersioningService(Log.ForContext<PrimalPerformer>(), _fileOperations);
            this._artifactDiscoveryService = VersionerFactory.CreateArtifactDiscoveryService(Log.ForContext<PrimalPerformer>(), _fileOperations);
            this._webhookService = VersionerFactory.CreateWebhookService(Log.ForContext<PrimalPerformer>());

            this._config.WorkingFolder = this._config.WorkingFolder.RemoveIllegalCharactersPath();
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

                // Use default configuration (empty JSON, will be filled with defaults)
                // All configuration is now done via command line parameters and defaults
                Configuration = "{}";

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

                // Initialize configuration with defaults
                // State.Config will be populated with default values and used throughout versioning
                // ProjectOverride.json only overrides version components (Major, Minor, Patch, Hotfix), not format settings
                VersionedSetModel versionedSetModel = new VersionedSetModel();
                log.Information("Initializing configuration with defaults...");
                _configurationService.InitializeConfiguration(Configuration, log);
                log.Information("Configuration initialized. State.Config is null: {isNull}", State.Config == null);

                // Handle new IsMonoRepo mode (Global Repository Versioning)
                if (_config.IsMonoRepo)
                {
                    log.Information("Global Repository Versioning mode enabled (IsMonoRepo)");
                    
                    // Check if working folder is a git repository
                    if (!_gitOperations.IsGitRepository(WorkFolder))
                    {
                        log.Error("Working folder is not a git repository. Global Repository Versioning requires a git repository.");
                        throw new Exception("Working folder is not a git repository");
                    }

                    // Find git repository root
                    string repoRoot = _gitOperations.GetGitRepositoryRoot(WorkFolder);
                    log.Information("Git repository root: {root}", repoRoot);

                    // Load ProjectOverride.json from repository root
                    var (globalOverrideExists, globalOverrideFileUrl, globalOverrideModel) = _overrideVersioningService.LoadOverrideConfiguration(repoRoot, _config.DefinedPatch);
                    
                    if (globalOverrideExists)
                    {
                        log.Information("Found ProjectOverride.json at repository root: {file}", globalOverrideFileUrl);
                    }
                    else
                    {
                        log.Information("No ProjectOverride.json found at repository root, using default versioning");
                    }

                    // Calculate version using standard logic (date + git log)
                    var globalVersioningRequest = new VersioningRequest
                    {
                        WorkingFolder = repoRoot,
                        Configuration = Configuration,
                        IsMonoRepo = false, // Use standard versioning logic, not old MonoRepo
                        DefinedPatch = _config.DefinedPatch,
                        VersionOnlyProps = _config.VersionOnlyProps,
                        VersionNuspecsEvenIfOtherFilesExist = false, // Removed parameter - use VersionItems instead
                        SetProjectGuid = false,
                        ProjectsGuidConfiguration = null
                    };

                    var globalVersioningResponse = await _mediator.Send<VersioningRequest, VersioningResponse>(globalVersioningRequest);
                    
                    BuildLabel = globalVersioningResponse.BuildLabel;
                    artifactVersion = globalVersioningResponse.ArtifactVersion;
                    ConsoleOutputs.AddRange(globalVersioningResponse.ConsoleOutputs);

                    // Apply override if exists
                    if (globalOverrideExists)
                    {
                        log.Information("Applying ProjectOverride: Major={Major}, Minor={Minor}, Patch={Patch}, Hotfix={Hotfix}",
                            globalOverrideModel.Major, globalOverrideModel.Minor, globalOverrideModel.Patch, globalOverrideModel.Hotfix);
                        
                        // Modify artifact version with override values
                        var parts = artifactVersion.Split('.');
                        if (parts.Length >= 2)
                        {
                            parts[0] = globalOverrideModel.Major.ToString();
                            parts[1] = globalOverrideModel.Minor.ToString();
                            if (parts.Length >= 3 && globalOverrideModel.Patch > 0)
                            {
                                parts[2] = globalOverrideModel.Patch.ToString();
                            }
                            if (parts.Length >= 4 && globalOverrideModel.Hotfix > 0)
                            {
                                parts[3] = globalOverrideModel.Hotfix.ToString();
                            }
                            artifactVersion = string.Join(".", parts);
                        }
                        else
                        {
                            // Build version from override
                            artifactVersion = $"{globalOverrideModel.Major}.{globalOverrideModel.Minor}";
                            if (globalOverrideModel.Patch > 0)
                            {
                                artifactVersion += $".{globalOverrideModel.Patch}";
                            }
                            if (globalOverrideModel.Hotfix > 0)
                            {
                                artifactVersion += $".{globalOverrideModel.Hotfix}";
                            }
                        }
                    }

                    string globalGitHash = _gitOperations.GetGitHash(repoRoot);
                    VersionedSetModel globalVersionedSetModel = new VersionedSetModel(
                        artifactVersion,
                        artifactVersion + $"+{globalGitHash}",
                        artifactVersion,
                        BuildLabel,
                        $"Version: {BuildLabel}",
                        globalGitHash);

                    log.Information("Calculated global version: {version}", artifactVersion);
                    log.Information("VersionedSetModel: AssemblyVersion={AssemblyVersion}, AssemblyFileVersion={AssemblyFileVersion}, AssemblyInfoVersion={AssemblyInfoVersion}",
                        globalVersionedSetModel.AssemblyVersion, globalVersionedSetModel.AssemblyFileVersion, globalVersionedSetModel.AssemblyInfoVersion);

                    // Version all files in repository with the same version
                    // Parse VersionItems if provided
                    List<string>? monorepoAllowedTypes = null;
                    if (!string.IsNullOrWhiteSpace(_config.VersionItems))
                    {
                        var (isValid, types, _) = AnubisWorks.Tools.Versioner.Helper.VersionItemsParser.Parse(_config.VersionItems);
                        if (isValid && types.Count > 0)
                        {
                            monorepoAllowedTypes = types;
                            log.Information("VersionItems filter applied: {types}", string.Join(", ", types));
                        }
                    }

                    _globalRepoVersioningService.VersionGlobalRepository(
                        repoRoot,
                        BuildLabel,
                        globalVersionedSetModel,
                        _config.StoreVersionFile,
                        _config.PreReleaseSuffix,
                        monorepoAllowedTypes);

                    log.Information("Global repository versioning completed successfully");
                    return;
                }

                // Calculate old MonoRepo mode (legacy - only if AllSlnLocations or ExactProjectFile are set)
                if (!_config.IsMonoRepo && (_config.AllSlnLocations || !string.IsNullOrEmpty(_config.ExactProjectFile)))
                {
                    _config.IsMonoRepo = true;
                    log.Information("Legacy MonoRepo mode enabled (via AllSlnLocations or ExactProjectFile)");
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
                    VersionNuspecsEvenIfOtherFilesExist = false, // Removed parameter - use VersionItems instead
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

                // Use ArtifactDiscoveryService for automatic artifact detection (replaces manual file search)
                // Parse VersionItems if provided
                List<string>? allowedTypes = null;
                if (!string.IsNullOrWhiteSpace(_config.VersionItems))
                {
                    var (isValid, types, errorMessage) = AnubisWorks.Tools.Versioner.Helper.VersionItemsParser.Parse(_config.VersionItems);
                    if (!isValid)
                    {
                        log.Error("Invalid VersionItems parameter: {error}", errorMessage);
                        Environment.Exit(501);
                    }
                    if (types.Count > 0)
                    {
                        allowedTypes = types;
                        log.Information("VersionItems filter applied: {types}", string.Join(", ", types));
                    }
                }
                // If VersionItems is not provided, all artifact types are included (including nuget by default)

                // Discover artifacts using ArtifactDiscoveryService
                var discoveryResult = _artifactDiscoveryService.DiscoverArtifacts(WorkFolder, recursive: true, allowedTypes: allowedTypes);
                
                log.Information("Found artifacts to version: {total} total (dotnet: {dotnet}, props: {props}, nuget: {nuget}, npm: {npm}, docker: {docker}, python: {python}, go: {go}, rust: {rust}, java: {java}, helm: {helm}, yaml: {yaml})",
                    discoveryResult.TotalArtifacts,
                    discoveryResult.DotNetProjects.Count,
                    discoveryResult.PropsFiles.Count,
                    discoveryResult.NuGetPackages.Count,
                    discoveryResult.NpmPackages.Count,
                    discoveryResult.DockerArtifacts.Count,
                    discoveryResult.PythonProjects.Count,
                    discoveryResult.GoModules.Count,
                    discoveryResult.RustProjects.Count,
                    discoveryResult.JavaProjects.Count,
                    discoveryResult.HelmCharts.Count,
                    discoveryResult.YamlConfigs.Count);

                if (discoveryResult.TotalArtifacts == 0)
                {
                    log.Error("No version-based files found!");
                    Environment.Exit(500);
                }

                // Build list of files to version (combine all artifact types)
                var files = new List<string>();
                
                // In standard mode, only version .csproj files, skip .props files
                files.AddRange(discoveryResult.DotNetProjects.Select(a => a.FilePath));
                
                // Add other artifact types (NPM, NuGet, Docker, etc.)
                files.AddRange(discoveryResult.NpmPackages.Select(a => a.FilePath));
                files.AddRange(discoveryResult.NuGetPackages.Select(a => a.FilePath));
                files.AddRange(discoveryResult.DockerArtifacts.Select(a => a.FilePath));
                files.AddRange(discoveryResult.PythonProjects.Select(a => a.FilePath));
                files.AddRange(discoveryResult.GoModules.Select(a => a.FilePath));
                files.AddRange(discoveryResult.RustProjects.Select(a => a.FilePath));
                files.AddRange(discoveryResult.JavaProjects.Select(a => a.FilePath));
                files.AddRange(discoveryResult.HelmCharts.Select(a => a.FilePath));
                files.AddRange(discoveryResult.YamlConfigs.Select(a => a.FilePath));

                log.Debug("Total files to version: {count}", files.Count);

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
                        this._config.StoreVersionFile, overrideModel, customProjectSettings, _config.PreReleaseSuffix, _config.DefinedPatch, true);

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
                    
                    // In monorepo mode, create version.txt only in repository root (not in each project directory)
                    if (_config.StoreVersionFile)
                    {
                        string repoRoot = _gitOperations.GetGitRepositoryRoot(WorkFolder);
                        var versionText = string.IsNullOrEmpty(_config.PreReleaseSuffix) 
                            ? versionedSetModel.AssemblyVersion 
                            : $"{versionedSetModel.AssemblyVersion}-{_config.PreReleaseSuffix}";
                        
                        var versionFilePath = Path.Combine(repoRoot, "version.txt");
                        _fileOperations.WriteFileContent(versionFilePath, versionText);
                        this.log.Information("Stored version file in repository root: {file} with version: {version}", versionFilePath, versionText);
                    }
                }
                else
                {
                    this.log.Information("Running SingleRepo versioning...");
                    foreach (string file in files)
                    {
                        // In standard mode, only version .csproj files, skip .props files
                        if (Path.GetExtension(file).Equals(".props", StringComparison.OrdinalIgnoreCase))
                        {
                            this.log.Information("Skipping .props file in standard mode: {File}", file);
                            continue;
                        }
                        
                        ProjectEntity proj = new ProjectEntity(_gitOperations.GetGitExecutablePath(), file, WorkFolder, ref this.ConsoleOutputs, BuildLabel,
                            this._config.StoreVersionFile, overrideModel, customProjectSettings, _config.PreReleaseSuffix, _config.DefinedPatch, false);
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

                // Send webhook notification (non-blocking, resilient to failures)
                if (!string.IsNullOrWhiteSpace(_config.WebhookUrl))
                {
                    var webhookResult = new Services.VersioningResult
                    {
                        BuildLabel = BuildLabel,
                        ArtifactVersion = artifactVersion,
                        GitHash = _gitOperations.GetGitHash(WorkFolder),
                        WorkingFolder = WorkFolder,
                        IsMonoRepo = _config.IsMonoRepo,
                        Timestamp = DateTime.UtcNow
                    };
                    
                    _ = _webhookService.SendWebhookAsync(_config.WebhookUrl, _config.WebhookToken, webhookResult);
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