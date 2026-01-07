using System;
using System.Net;
using System.IO;
using AnubisWorks.Lib.ArgsInterceptor;
using AnubisWorks.Tools.Versioner.Cli.Helper;
using AnubisWorks.Tools.Versioner.Model;
using AnubisWorks.Tools.Versioner.Helper;

namespace AnubisWorks.Tools.Versioner.Cli
{
    class Program
    {
        public static int Main(string[] args)
        {
            bool DependentErrors = false;

            ArgsInterceptor<ConfigurationsArgs> p = new ArgsInterceptor<ConfigurationsArgs>();

            p.Setup(arg => arg.UseDefaults)
                .As('d', "usedefaults")
                .SetDefault(true)
                .WithDescription("Should Defaults be used.");

            p.Setup(arg => arg.WorkingFolder)
                .As('w', "workingfolder")
                .Required()
                .WithDescription("Full path to working folder");

            p.Setup(arg => arg.StoreVersionFile)
                .As('s', "storeversionfile")
                .SetDefault(false)
                .WithDescription("Should version.txt be stored in project directory.");

            p.Setup(arg => arg.LogLevel)
                .As('l', "loglevel")
                .SetDefault("V")
                .WithDescription("Set Loglevel (V,D,I,W,E,F)");

            //MonoRepo Mode
            p.Setup(arg => arg.IsMonoRepo)
                .As('m', "ismonorepo")
                .SetDefault(false)
                .WithDescription("MonoRepo Mode.");

            p.Setup(arg => arg.AllSlnLocations)
                .As('a', "allslnlocations")
                .SetDefault(false)
                .WithDescription("MonoRepo Mode: Should search for SLN file not only in top directory, but recursive?");

            //SetProjectGuid
            p.Setup(arg => arg.SetProjectGuid)
                .As('g', "setprojectguid")
                .SetDefault(false)
                .WithDescription("Should take care of Project Guid settings within CSPROJ files? Required for SonarQube analysis.");

            //ProjectsGuidConfiguration
            p.Setup(arg => arg.ExactProjectFile)
                .As('p', "projectfile")
                .WithDescription("MonoRepo Mode: ProjectFile (csproj) location. Will be the base to calculate version from. Cannot be used with a (allslnlocations) parameter!");

            //ExactProjectFile
            p.Setup(arg => arg.ProjectsGuidConfiguration)
                .As('f', "projectsguidconfig")
                .SetDefault(AnubisWorks.Tools.Versioner.Helper.PathValidator.GetPlatformDefaultPath("projectguids.json"))
                .WithDescription("Projects Guid Configuration location. Required for parameter g (setprojectguid)");

            //CustomConfigurationFile
            p.Setup(arg => arg.CustomConfigurationFile)
                .As('u', "customprojectconfig")
                .SetDefault(AnubisWorks.Tools.Versioner.Helper.PathValidator.GetPlatformDefaultPath("customprojectconfig.json"))
                .WithDescription("Custom Projects Settings Configuration location");

            // VersionNuspecsEvenIfOtherFilesExist parameter removed - use --versionItems="nuget" instead

            //PreReleaseSuffix
            p.Setup(arg => arg.PreReleaseSuffix)
                .As('x', "prereleasesuffix")
                .SetDefault(string.Empty)
                .WithDescription("PreRelease suffix for PreRelease Nuget Versioning (SemVer X.Y.Z.W-suffix). Required for parameter x (prereleasesuffix)");

            //DefinedPatch
            p.Setup(arg => arg.DefinedPatch)
                .As('z', "definedpatch")
                .SetDefault(string.Empty)
                .WithDescription("Defined Patch Value for Nuget Versioning (SemVer X.Y.Z.patch). Required for parameter z (definedpatch)");

            //VersionOnlyProps
            p.Setup(arg => arg.VersionOnlyProps)
                .As('n', "versiononlyprops")
                .SetDefault(true)
                .WithDescription("Should only .props file be identified for versioning if exists? Setting to false shall treat props along with project files.");

            //WebhookUrl (Phase 3)
            p.Setup(arg => arg.WebhookUrl)
                .As('h', "webhookurl")
                .SetDefault(string.Empty)
                .WithDescription("Optional webhook URL for notifications. Tool works normally if webhook is not provided or fails.");

            //WebhookToken (Phase 3)
            p.Setup(arg => arg.WebhookToken)
                .As('t', "webhooktoken")
                .SetDefault(string.Empty)
                .WithDescription("Optional HMAC secret for webhook signature verification. Only used if webhookurl is provided.");

            //VersionItems (Phase 5)
            p.Setup(arg => arg.VersionItems)
                .As("versionitems")
                .SetDefault(string.Empty)
                .WithDescription("Comma-separated list of artifact types to version (e.g., 'nuget,npm,docker'). If not set, versions all artifacts. Supported: dotnet,props,nuget,npm,docker,python,go,rust,java,yaml,helm");

            ICommandLineParserResult result = p.Parse(args);

            // Validate VersionItems parameter if provided
            if (result.HasErrors == false && !string.IsNullOrWhiteSpace(p.Object.VersionItems))
            {
                var (isValid, _, errorMessage) = AnubisWorks.Tools.Versioner.Helper.VersionItemsParser.Parse(p.Object.VersionItems);
                if (!isValid)
                {
                    Console.Error.WriteLine($"ERROR: {errorMessage}");
                    DependentErrors = true;
                }
            }

            // Normalize and validate file path parameters for cross-platform compatibility
            if (result.HasErrors == false)
            {
                // Normalize and validate CustomConfigurationFile path
                if (!string.IsNullOrWhiteSpace(p.Object.CustomConfigurationFile))
                {
                    string normalizedPath = FilePathHelper.NormalizePathForPlatform(p.Object.CustomConfigurationFile);
                    p.Object.CustomConfigurationFile = normalizedPath;

                    var validation = PathValidator.ValidatePath(normalizedPath, "CustomConfigurationFile", mustExist: false);
                    if (!validation.IsValid)
                    {
                        Console.WriteLine($"ERROR: {validation.ErrorMessage}");
                        DependentErrors = true;
                    }
                }

                // Normalize and validate ProjectsGuidConfiguration path
                if (!string.IsNullOrWhiteSpace(p.Object.ProjectsGuidConfiguration))
                {
                    string normalizedPath = FilePathHelper.NormalizePathForPlatform(p.Object.ProjectsGuidConfiguration);
                    p.Object.ProjectsGuidConfiguration = normalizedPath;

                    var validation = PathValidator.ValidatePath(normalizedPath, "ProjectsGuidConfiguration", mustExist: false);
                    if (!validation.IsValid)
                    {
                        Console.WriteLine($"ERROR: {validation.ErrorMessage}");
                        DependentErrors = true;
                    }
                }

                // Normalize other file path parameters
                if (!string.IsNullOrWhiteSpace(p.Object.ExactProjectFile))
                {
                    p.Object.ExactProjectFile = FilePathHelper.NormalizePathForPlatform(p.Object.ExactProjectFile);

                    var validation = PathValidator.ValidatePath(p.Object.ExactProjectFile, "ExactProjectFile", mustExist: false);
                    if (!validation.IsValid)
                    {
                        Console.WriteLine($"ERROR: {validation.ErrorMessage}");
                        DependentErrors = true;
                    }
                }

                if (!string.IsNullOrWhiteSpace(p.Object.WorkingFolder))
                {
                    p.Object.WorkingFolder = FilePathHelper.NormalizePathForPlatform(p.Object.WorkingFolder);

                    var validation = PathValidator.ValidatePath(p.Object.WorkingFolder, "WorkingFolder", mustExist: false);
                    if (!validation.IsValid)
                    {
                        Console.WriteLine($"ERROR: {validation.ErrorMessage}");
                        DependentErrors = true;
                    }
                }
            }

            if (result.HasErrors == false)
            {
                if (p.Object.SetProjectGuid && string.IsNullOrEmpty(p.Object.ProjectsGuidConfiguration))
                {
                    Console.WriteLine("Setup of project guids chosen, while ProjectsGuidConfiguration setting is missing!");
                    Console.WriteLine("Turning OFF ProjectsGuidConfiguration");
                    p.Object.SetProjectGuid = false;
                    //DependentErrors = true;
                }

                //if(p.Object.IsMonoRepo)
                //{
                //    if(string.IsNullOrEmpty(p.Object.ExactProjectFile) && !p.Object.AllSlnLocations)
                //    {
                //        Console.WriteLine("If IsMonoRepo parameter is used, either ExactProjectFile or AllSlnLocations must be set!");
                //        DependentErrors = true;
                //    }
                //}

                // Validate IsMonoRepo conflicts with incompatible parameters
                if (p.Object.IsMonoRepo)
                {
                    bool hasConflict = false;

                    if (!p.Object.UseDefaults)
                    {
                        Console.WriteLine("ERROR: IsMonoRepo parameter cannot be used when UseDefaults is false!");
                        Console.WriteLine("IsMonoRepo mode requires UseDefaults to be enabled.");
                        hasConflict = true;
                    }

                    if (!string.IsNullOrEmpty(p.Object.CustomConfigurationFile))
                    {
                        Console.WriteLine("ERROR: IsMonoRepo parameter cannot be used with CustomConfigurationFile parameter!");
                        Console.WriteLine("These options are mutually exclusive. Please remove one of them.");
                        hasConflict = true;
                    }
                    
                    if (hasConflict)
                    {
                        DependentErrors = true;
                    }
                }

                if (!string.IsNullOrEmpty(p.Object.ExactProjectFile) && p.Object.AllSlnLocations)
                {
                    Console.WriteLine("Switch allslnlocations cannot be used together with projectfile!");
                    Console.WriteLine("Turning OFF AllSlnLocations");
                    p.Object.AllSlnLocations = false;
                }

            }

            if (result.HasErrors == false && DependentErrors == false)
            {
                string allSln = p.Object.AllSlnLocations ? "YES" : "NO";
                string setProjectGuids = p.Object.SetProjectGuid ? "YES" : "NO";
                string projectGuidsConfig = p.Object.ProjectsGuidConfiguration;
                string projectfile = p.Object.ExactProjectFile;

                Console.WriteLine($"Options are ->  UseDefaults: {p.Object.UseDefaults}, WorkingFolder: {p.Object.WorkingFolder}, Loglevel: {p.Object.LogLevel}, StoreVersionFile: {p.Object.StoreVersionFile}, SearchInAllLocationsForSln: {allSln}, SetProjectGuids: {setProjectGuids}, ProjectsGuidConfiguration: {projectGuidsConfig}, ExactProjectFile: {projectfile}");
            }
            else
            {
                HelpPrinter.Print(p);

                return 406;
            }

            PrimalPerformer wker = new PrimalPerformer(p.Object);
            wker.Execute().GetAwaiter().GetResult();

            return 0;
        }
            //=> CommandLineApplication.Execute<Worker>(args);
    }
}
