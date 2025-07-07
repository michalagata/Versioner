using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using AnubisWorks.Tools.Versioner.Entity;
using AnubisWorks.Tools.Versioner.Helper;
using AnubisWorks.Tools.Versioner.Interfaces;
using AnubisWorks.Tools.Versioner.Model;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Infrastructure.Services
{
    public class ProjectVersionCalculatorService : IProjectVersionCalculator
    {
        private readonly ILogger _log;
        private readonly IGitLogService _gitLogService;
        private readonly IVersionPatternService _versionPatternService;
        private readonly IProjectConfigurationService _projectConfigurationService;
        private readonly IProjectFileService _projectFileService;

        public ProjectVersionCalculatorService(
            ILogger log,
            IGitLogService gitLogService,
            IVersionPatternService versionPatternService,
            IProjectConfigurationService projectConfigurationService,
            IProjectFileService projectFileService)
        {
            _log = log;
            _gitLogService = gitLogService;
            _versionPatternService = versionPatternService;
            _projectConfigurationService = projectConfigurationService;
            _projectFileService = projectFileService;
        }

        public VersionedSetModel CalculateVersion(
            string gitPath,
            string filePath,
            string workingFolder,
            ref List<string> consoleOutputs,
            string buildLabel,
            bool storeVersionFile,
            MajorMinorPatchHotfixModel patchHotfixModel,
            CustomProjectSettings customProjectSettings,
            int semVersion,
            string prereleaseSuffix,
            string definedPatch,
            bool calculateMonoMode = false)
        {
            Stopwatch sw = Stopwatch.StartNew();
            
            var relative = Path.GetRelativePath(workingFolder, filePath).ToLinuxPath();
            _log.Information("Loading file: {relative}", relative);

            // Określenie typu projektu
            ProjectType projectType = ProjectType.Sdk;
            XDocument project = null;
            ProjectEntityStatic.DecideProjectTypeFromFile(filePath, ref projectType, ref project);

            // Konfiguracja
            var config = _projectConfigurationService.GetProjectConfiguration(relative);
            _projectConfigurationService.ApplyCustomProjectSettings(
                config, 
                Path.GetFileNameWithoutExtension(config.ProjectFile), 
                customProjectSettings);

            // Pobranie logów Git
            var logEntries = _gitLogService.GetLogEntries(gitPath, workingFolder, filePath, config);
            
            if (logEntries.Count == 0)
            {
                _log.Error("Error while calculating version for project: {project}", relative);
                Environment.Exit(602);
            }

            var lastEntry = _gitLogService.GetLatestLogEntry(logEntries);
            var timeModel = State.TimeFrameConfiguration.GetTimeFrameConfig(lastEntry.Date.Date);

            // Zastosowanie nadpisań wersji
            if (patchHotfixModel.Major != 0)
            {
                if (patchHotfixModel.Major != 0) timeModel.Version.Major = patchHotfixModel.Major;
                if (patchHotfixModel.Minor != 0) timeModel.Version.Minor = patchHotfixModel.Minor;
                if (patchHotfixModel.Patch != 0) timeModel.Version.Patch = patchHotfixModel.Patch;
                if (patchHotfixModel.Hotfix != 0) timeModel.Version.Hotfix = patchHotfixModel.Hotfix;
                timeModel.OverrideWithLocalFile = true;
            }

            // Przygotowanie zmiennych wersji
            string dayOfTheYear = lastEntry.Date.DayOfYear.ToString();
            string pos0 = timeModel.Version.Major.ToString();
            string pos1 = timeModel.OverrideWithLocalFile 
                ? timeModel.Version.Minor.ToString() 
                : timeModel.Version.Minor.ToString().EnsurePropperDateMonthsFormat();
            string pos2 = logEntries.Count.ToString();
            
            if (!string.IsNullOrEmpty(definedPatch))
            {
                pos2 = int.Parse(definedPatch).ToString();
            }

            string pos3 = lastEntry.ShortHash;
            string pos4 = lastEntry.LongHash;
            string pos5 = dayOfTheYear;

            if (pos0 == "0") pos0 = "1";

            if (timeModel.OverrideWithLocalFile)
            {
                if (timeModel.Version.Patch != 0) pos5 = timeModel.Version.Patch.ToString();
                if (timeModel.Version.Hotfix != 0) pos2 = timeModel.Version.Hotfix.ToString();
            }

            // Generowanie wzorców wersji
            string assemblyVersionPattern = _versionPatternService.GenerateAssemblyVersionPattern(
                config, timeModel, semVersion, pos0, pos1, pos2, pos3, pos4, pos5);
            string assemblyInfoVersionPattern = _versionPatternService.GenerateAssemblyInfoVersionPattern(
                config, timeModel, semVersion, pos0, pos1, pos2, pos3, pos4, pos5);
            string assemblyFileVersionPattern = _versionPatternService.GenerateAssemblyFileVersionPattern(
                config, timeModel, semVersion, pos0, pos1, pos2, pos3, pos4, pos5);

            // Obsługa PackageJson
            if (projectType == ProjectType.PackageJson)
            {
                string temp = !timeModel.OverrideWithLocalFile
                    ? string.Format(config.AssemblyVersionFormatJS, pos0, pos1, pos2, pos3, pos4)
                    : string.Format(config.AssemblyVersionFormatJSOverride, pos0, pos1, pos2, pos3, pos4);

                assemblyFileVersionPattern = assemblyInfoVersionPattern = assemblyVersionPattern = temp;
            }

            // Pobranie obecnych wersji z plików
            string description = "";
            string assemblyFileVersion = "1.0.0.0";
            string assemblyVersion = "1.0.0.0";
            string assemblyInformationalVersion = "1.0.0.0";
            string assemblyInfoFilePath = "";
            string defaultVersion = "1.0.0.0";

            ProjectEntityStatic.GetVersionsFromFiles(projectType, project, config, filePath, 
                ref description, ref assemblyFileVersion, ref assemblyVersion, 
                ref assemblyInformationalVersion, ref assemblyInfoFilePath, defaultVersion, _log);

            // Kalkulacja nowych wersji
            if (assemblyInformationalVersion != assemblyVersion)
            {
                assemblyInformationalVersion = assemblyVersion;
            }

            assemblyInformationalVersion = _versionPatternService.CalculateVersionFromPattern(
                assemblyInformationalVersion, assemblyInfoVersionPattern);
            assemblyVersion = _versionPatternService.CalculateVersionFromPattern(
                assemblyVersion, assemblyVersionPattern);
            assemblyFileVersion = _versionPatternService.CalculateVersionFromPattern(
                assemblyFileVersion, assemblyFileVersionPattern);

            if (string.IsNullOrEmpty(description))
            {
                description = string.Empty;
            }

            if (config.HashAsDescription)
            {
                description = description.Length < 100 && !description.Contains("|")
                    ? $"ChangeHash:{lastEntry.ShortHash},BuildLabel:{buildLabel}|{description}"
                    : $"ChangeHash:{lastEntry.ShortHash},BuildLabel:{buildLabel}";
            }

            // Wyjście do konsoli
            ProjectEntityStatic.ConsoleOutPutCalculatedVersions(ref consoleOutputs, workingFolder, filePath, 
                projectType, assemblyVersion, assemblyInformationalVersion, assemblyFileVersion);

            // Utworzenie pliku wersji
            if (string.IsNullOrEmpty(prereleaseSuffix))
            {
                _projectFileService.CreateVersionTextFile(storeVersionFile, Path.GetDirectoryName(filePath), 
                    projectType, assemblyVersion);
            }
            else
            {
                _projectFileService.CreateVersionTextFile(storeVersionFile, Path.GetDirectoryName(filePath), 
                    projectType, assemblyVersion, prereleaseSuffix);
            }

            // Wersjonowanie plików
            bool saveprops = false, savecsproj = false, saveNuSpec = false;
            
            if (string.IsNullOrEmpty(prereleaseSuffix))
            {
                ProjectEntityStatic.VersionFile(ref saveprops, ref savecsproj, ref saveNuSpec, project, config, 
                    projectType, description, assemblyFileVersion, assemblyVersion, assemblyInformationalVersion, 
                    filePath, assemblyInfoFilePath);
            }
            else
            {
                ProjectEntityStatic.VersionFile(ref saveprops, ref savecsproj, ref saveNuSpec, project, config, 
                    projectType, description, assemblyFileVersion, assemblyVersion, assemblyInformationalVersion, 
                    filePath, assemblyInfoFilePath, prereleaseSuffix);
            }

            // Obsługa GUID projektów
            if (State.MaintainProjectGuids && projectType != ProjectType.PackageJson)
            {
                if (ProjectEntityStatic.SetProjectGuid(project, relative))
                {
                    savecsproj = true;
                    saveprops = true;
                }
            }

            // Zapisanie plików
            _projectFileService.ProcessProjectFile(saveprops, savecsproj, saveNuSpec, project, filePath, assemblyVersion);

            // Obsługa NuSpec
            if (saveNuSpec && !string.IsNullOrEmpty(assemblyVersion))
            {
                consoleOutputs.Add($"##teamcity[setParameter name='env.BuildNuspecVersion' value='{assemblyVersion}']");
                System.Environment.SetEnvironmentVariable("env.BuildNuspecVersion", assemblyVersion);
                _log.Information("BuildNuspecVersion: {BuildNuspecVersion}", assemblyVersion);
            }

            // Wersjonowanie pliku TypeScript
            _projectFileService.VersionTypeScriptFile(filePath, buildLabel, lastEntry.ShortHash, 
                assemblyVersion, assemblyFileVersion, assemblyInformationalVersion);

            sw.Stop();
            _log.Verbose("Project processed in {elapsed} ms", sw.ElapsedMilliseconds);

            return calculateMonoMode 
                ? new VersionedSetModel(assemblyVersion, assemblyInformationalVersion, assemblyFileVersion, 
                    buildLabel, description, lastEntry.ShortHash)
                : new VersionedSetModel();
        }

        public VersionedSetModel ProcessMonoRepoProject(
            string filePath,
            string workingFolder,
            ref List<string> tcOutputs,
            string buildLabel,
            bool storeVersionFile,
            CustomProjectSettings customProjectSettings,
            VersionedSetModel calculatedModel)
        {
            Stopwatch sw = Stopwatch.StartNew();
            
            var relative = Path.GetRelativePath(workingFolder, filePath).ToLinuxPath();
            _log.Information("Loading file: {relative}", relative);

            ProjectType projectType = ProjectType.Sdk;
            XDocument project = null;
            ProjectEntityStatic.DecideProjectTypeFromFile(filePath, ref projectType, ref project);

            var config = _projectConfigurationService.GetProjectConfiguration(relative);

            string assemblyVersion = calculatedModel.AssemblyVersion;
            string assemblyInformationalVersion = calculatedModel.AssemblyInfoVersion;
            string assemblyFileVersion = calculatedModel.AssemblyFileVersion;
            string description = "";

            if (config.HashAsDescription)
                description = $"{calculatedModel.ShortHash}:{calculatedModel.BuildLabel}|{calculatedModel.Description}";

            var shortHash = calculatedModel.ShortHash;

            if (string.IsNullOrEmpty(shortHash) && assemblyInformationalVersion != assemblyVersion)
            {
                assemblyInformationalVersion = assemblyVersion;
            }

            if (projectType == ProjectType.PackageJson)
            {
                assemblyVersion = assemblyInformationalVersion = assemblyFileVersion = assemblyVersion.ToNpmSemver();
            }

            ProjectEntityStatic.ConsoleOutPutCalculatedVersions(ref tcOutputs, workingFolder, filePath, projectType,
                assemblyVersion, assemblyInformationalVersion, assemblyFileVersion);

            if (storeVersionFile)
            {
                _projectFileService.CreateVersionTextFile(storeVersionFile, Path.GetDirectoryName(filePath), 
                    projectType, assemblyVersion);
            }

            bool saveprops = false, savecsproj = false, saveNuSpec = false;
            ProjectEntityStatic.VersionFile(ref saveprops, ref savecsproj, ref saveNuSpec, project, config, 
                projectType, description, assemblyFileVersion, assemblyVersion, assemblyInformationalVersion, 
                filePath, "");

            if (State.MaintainProjectGuids && projectType != ProjectType.PackageJson)
            {
                if (ProjectEntityStatic.SetProjectGuid(project, relative)) savecsproj = true;
            }

            _projectFileService.ProcessProjectFile(saveprops, savecsproj, saveNuSpec, project, filePath, assemblyVersion);

            _projectFileService.VersionTypeScriptFile(filePath, buildLabel, shortHash, 
                assemblyVersion, assemblyFileVersion, assemblyInformationalVersion);

            sw.Stop();
            _log.Verbose("Project processed in {elapsed} ms", sw.ElapsedMilliseconds);

            return new VersionedSetModel();
        }
    }
} 