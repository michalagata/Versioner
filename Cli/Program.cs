using System;
using System.Net;
using AnubisWorks.Lib.ArgsInterceptor;
using AnubisWorks.Tools.Versioner.Cli.Helper;
using AnubisWorks.Tools.Versioner.Model;

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

            p.Setup(arg => arg.ConfigurationFile)
                .As('c', "configurationfile")
                .WithDescription("Set full path to configuration file");

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
                .SetDefault("C:\\projectguids.json")
                .WithDescription("Projects Guid Configuration location. Required for parameter g (setprojectguid)");

            //CustomConfigurationFile
            p.Setup(arg => arg.CustomConfigurationFile)
                .As('u', "customprojectconfig")
                .SetDefault("C:\\customprojectconfig.json")
                .WithDescription("Custom Projects Settings Configuration location");

            //SetProjectGuid
            p.Setup(arg => arg.VersionNuspecsEvenIfOtherFilesExist)
                .As('e', "enforcenuspecversioningwithotherfiles")
                .SetDefault(false)
                .WithDescription("Should be NuSPEC files be versioned if other identified files for versioning exist?");

            //SemVersion
            p.Setup(arg => arg.SemVersion)
                .As('v', "semversion")
                .SetDefault(1)
                .WithDescription("Semantic Versioning Format Version (1/2)");

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
                .WithDescription("Should only .props file be identified for versioning if exists? Setting to false shall trat props along with project files.");

            ICommandLineParserResult result = p.Parse(args);

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

                if(p.Object.IsMonoRepo && !string.IsNullOrEmpty(p.Object.CustomConfigurationFile))
                {
                    Console.WriteLine("If IsMonoRepo parameter is used, parameter CustomConfigurationFile cannot be used!");
                    Console.WriteLine("CustomConfigurationFile parameter will be ignored!");
                    p.Object.CustomConfigurationFile = null;
                }

                if (!string.IsNullOrEmpty(p.Object.ExactProjectFile) && p.Object.AllSlnLocations)
                {
                    Console.WriteLine("Switch allslnlocations cannot be used together with projectfile!");
                    Console.WriteLine("Turning OFF AllSlnLocations");
                    p.Object.AllSlnLocations = false;
                }

                if (p.Object.SemVersion != 1 && p.Object.SemVersion != 2)
                {
                    Console.WriteLine("Wrong value for semversion switch! Please use 1 or 2.");
                    Console.WriteLine($"Setting default SemVersion value = 1");
                    p.Object.SemVersion = 1;
                    //DependentErrors = true;
                }
            }

            if (result.HasErrors == false && DependentErrors == false)
            {
                string allSln = p.Object.AllSlnLocations ? "YES" : "NO";
                string setProjectGuids = p.Object.SetProjectGuid ? "YES" : "NO";
                string projectGuidsConfig = p.Object.ProjectsGuidConfiguration;
                string projectfile = p.Object.ExactProjectFile;

                Console.WriteLine($"Options are ->  UseDefaults: {p.Object.UseDefaults}, WorkingFolder: {p.Object.WorkingFolder}, ConfigurationFile: {p.Object.ConfigurationFile}, Loglevel: {p.Object.LogLevel}, StoreVersionFile: {p.Object.StoreVersionFile}, SearchInAllLocationsForSln: {allSln}, SetProjectGuids: {setProjectGuids}, ProjectsGuidConfiguration: {projectGuidsConfig}, ExactProjectFile: {projectfile}");
            }
            else
            {
                HelpPrinter.Print(p);

                return 406;
            }

            PrimalPerformer wker = new PrimalPerformer(p.Object);
            wker.Execute();

            return 0;
        }
            //=> CommandLineApplication.Execute<Worker>(args);
    }
}
