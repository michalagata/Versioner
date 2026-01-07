using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using AnubisWorks.Tools.Versioner.Helper;
using AnubisWorks.Tools.Versioner.Domain.Interfaces;
using AnubisWorks.Tools.Versioner.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Entity
{
    public class VersionManager : IVersionManager
    {
        private readonly ILogger _logger;
        private readonly VersionExtractor _versionExtractor;
        private readonly VersionSetter _versionSetter;

        public VersionManager(ILogger logger)
        {
            _logger = logger;
            _versionExtractor = new VersionExtractor(logger);
            _versionSetter = new VersionSetter();
        }

        public void SetVersion(ref bool saveprops, ref bool savecsproj, ref bool saveNuSpec, XDocument project,
            VersioningBaseConfiguration config, ProjectType projectType, string description, string assemblyFileVersion,
            string assemblyVersion, string assemblyInformationalVersion, string filePath, string assemblyInfoFilePath,
            string PrereleaseSuffix = null)
        {
            switch (projectType)
            {
                case ProjectType.Sdk:
                    if (string.IsNullOrEmpty(PrereleaseSuffix))
                    {
                        _versionSetter.SetNewVersionsToSdk(project, config, description, assemblyFileVersion,
                            assemblyVersion, assemblyInformationalVersion);
                    }
                    else
                    {
                        _versionSetter.SetNewVersionsToSdk(project, config, description, assemblyFileVersion,
                            assemblyVersion, assemblyInformationalVersion, PrereleaseSuffix);
                    }

                    savecsproj = true;
                    break;

                case ProjectType.Props:
                    if (string.IsNullOrEmpty(PrereleaseSuffix))
                    {
                        _versionSetter.SetNewVersionsToProps(project, config, description, assemblyFileVersion,
                            assemblyVersion, assemblyInformationalVersion);
                    }
                    else
                    {
                        _versionSetter.SetNewVersionsToProps(project, config, description, assemblyFileVersion,
                            assemblyVersion, assemblyInformationalVersion, PrereleaseSuffix);
                    }

                    saveprops = true;
                    break;

                case ProjectType.NuSpec:
                    if (string.IsNullOrEmpty(PrereleaseSuffix))
                    {
                        _versionSetter.SetNewVersionsToNuSpec(project, assemblyVersion, config);
                    }
                    else
                    {
                        _versionSetter.SetNewVersionsToNuSpec(project, assemblyVersion, config, PrereleaseSuffix);
                    }

                    saveNuSpec = true;
                    break;

                case ProjectType.AssemblyInfo:
                    if (string.IsNullOrEmpty(PrereleaseSuffix))
                    {
                        _versionSetter.SetNewVersionsToAssemblyInfoFile(assemblyInfoFilePath, assemblyVersion,
                            assemblyInformationalVersion, assemblyFileVersion);
                    }
                    else
                    {
                        _versionSetter.SetNewVersionsToAssemblyInfoFile(assemblyInfoFilePath, assemblyVersion,
                            assemblyInformationalVersion, assemblyFileVersion, PrereleaseSuffix);
                    }
                    break;

                case ProjectType.PackageJson:
                    JObject json;
                    json = string.IsNullOrEmpty(PrereleaseSuffix)
                        ? _versionSetter.SetNewVersionsToPackageJson(filePath, assemblyVersion)
                        : _versionSetter.SetNewVersionsToPackageJson(filePath, assemblyVersion, PrereleaseSuffix);

                    File.WriteAllText(filePath, json.ToString(Formatting.Indented));
                    break;
            }
        }

        public void ExtractVersion(ProjectType projectType, XDocument project,
            VersioningBaseConfiguration config, string filePath, ref string description, ref string assemblyFileVersion,
            ref string assemblyVersion, ref string assemblyInformationalVersion, ref string assemblyInfoFilePath,
            string defaultVersion)
        {
            switch (projectType)
            {
                case ProjectType.Sdk:
                    _versionExtractor.ExtractCurrentVersionsFromSdk(project, config, ref description,
                        ref assemblyFileVersion,
                        ref assemblyVersion, ref assemblyInformationalVersion, defaultVersion);
                    break;

                case ProjectType.Props:
                    _versionExtractor.ExtractCurrentVersionsFromProps(project, config, ref description,
                        ref assemblyFileVersion,
                        ref assemblyVersion, ref assemblyInformationalVersion, defaultVersion);
                    break;

                case ProjectType.AssemblyInfo:
                    string ret = _versionExtractor.ExtractCurrentVersionFromAssemblyInfo(config.AssemblyInfoFile,
                        filePath, ref assemblyInfoFilePath, ref assemblyFileVersion, ref assemblyInformationalVersion,
                        ref assemblyVersion);
                    if (!string.IsNullOrEmpty(ret)) _logger.Warning(ret);
                    break;

                case ProjectType.NuSpec:
                    _versionExtractor.ExtractCurrentVersionsFromNuSpec(project, ref assemblyVersion, defaultVersion);
                    break;

                case ProjectType.PackageJson:
                    _versionExtractor.ExtractCurrentVersionsFromPackageJson(filePath, ref assemblyFileVersion,
                        ref assemblyInformationalVersion, ref assemblyVersion);
                    break;
            }
        }

        public void CreateVersionTextFile(bool storeVersionFile, string projDir, ProjectType projectType,
            string assemblyVersion, string PrereleaseSuffix = null)
        {
            if (storeVersionFile && projectType != ProjectType.PackageJson)
            {
                try
                {
                    string vertxtpath = Path.Combine(projDir, "version.txt");
                    string versionContent = assemblyVersion;
                    if (!string.IsNullOrEmpty(PrereleaseSuffix))
                    {
                        versionContent = assemblyVersion + "-" + PrereleaseSuffix;
                    }
                    File.WriteAllText(vertxtpath, versionContent);
                }
                catch (Exception e)
                {
                    _logger.Warning("Problem creating version.txt file. {err}", e.Message);
                }
            }
        }

        public void ConsoleOutPutCalculatedVersions(ref List<string> consoleOutputs, string workingFolder, string filePath,
            ProjectType projectType, string assemblyVersion, string assemblyInformationalVersion,
            string assemblyFileVersion)
        {
            var relativeDir = Path.GetRelativePath(workingFolder, Path.GetDirectoryName(filePath)).NormalizePath();

            string verEnvname = relativeDir.Replace("/", "_");
            consoleOutputs.Add($"##teamcity[setParameter name='Version.{verEnvname}' value='{assemblyVersion}']");
            if (projectType != ProjectType.PackageJson)
            {
                consoleOutputs.Add(
                    $"##teamcity[setParameter name='VersionInfo.{verEnvname}' value='{assemblyInformationalVersion}']");
                consoleOutputs.Add(
                    $"##teamcity[setParameter name='VersionFile.{verEnvname}' value='{assemblyFileVersion}']");
            }
        }
    }
} 