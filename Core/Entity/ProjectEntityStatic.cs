using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using AnubisWorks.Tools.Versioner.Helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Entity
{
    public static class ProjectEntityStatic
    {
        private static readonly FileVersioner _fileVersioner = new FileVersioner(Log.ForContext("Context", nameof(FileVersioner), true));
        private static readonly VersionManager _versionManager = new VersionManager(Log.ForContext("Context", nameof(VersionManager), true));
        private static readonly ProjectFileHandler _projectFileHandler = new ProjectFileHandler();
        private static readonly AssemblyInfoHandler _assemblyInfoHandler = new AssemblyInfoHandler(Log.ForContext("Context", nameof(AssemblyInfoHandler), true));

        const string pat =
            @"(^\s*\[\s*assembly\s*:\s*((System\s*\.)?\s*Reflection\s*\.)?\s*TYPE(Attribute)?\s*\(\s*@?\"")(?<version>(([0-9\*])+\.?)+(\W(\w)+)?)(?'smiec'.+)?(\""\s*\)\s*\])";

        private const RegexOptions
            opts = RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Compiled;

        static readonly Regex rAFV = new Regex(pat.Replace("TYPE", "AssemblyFileVersion"), opts);
        static readonly Regex rAIV = new Regex(pat.Replace("TYPE", "AssemblyInformationalVersion"), opts);
        static readonly Regex rAV = new Regex(pat.Replace("TYPE", "AssemblyVersion"), opts);

        public static string GetSha256FromString(string strData)
        {
            var message = Encoding.ASCII.GetBytes(strData);
            SHA256Managed hashString = new SHA256Managed();
            string hex = "";

            var hashValue = hashString.ComputeHash(message);
            foreach (byte x in hashValue)
            {
                hex += $"{x:x2}";
            }

            return hex;
        }

        public static bool SetProjectGuid(XDocument csproj, string relative)
        {
            var modified = false;
            string prGuid = Guid.NewGuid().ToVsMode();
            string fileGuid = string.Empty;

            var guidPair =
                State.ProjGuidsConfiguration.Entities.FirstOrDefault(
                    f => f.ProjectHash == ProjectEntityStatic.GetSha256FromString(relative));
            if (guidPair == null)
            {
                guidPair = new ProjGuidPair()
                    {ProjectHash = ProjectEntityStatic.GetSha256FromString(relative), ProjectGuid = prGuid};
                State.ProjGuidsConfiguration.Entities.Add(guidPair);
                State.ProjGuidsConfiguration.StateChanged = true;
            }
            else
            {
                fileGuid = guidPair.ProjectGuid;
                prGuid = Guid.Parse(guidPair.ProjectGuid).ToVsMode();
            }

            XElement xProj = csproj.Elements().First(f => f.Name.LocalName == "Project");
            {
                var xPropertyGroups = xProj
                    .Descendants()
                    .Where(w => w.Name.LocalName == "PropertyGroup")
                    .ToList();
                xPropertyGroups
                    .ForEach(xnPG =>
                    {
                        if (xnPG.Attribute("Condition") != null && xnPG.Element("ProjectGuid") != null)
                        {
                            prGuid = Guid.Parse(xnPG.Element("ProjectGuid").Value).ToVsMode();
                            xnPG.Element("ProjectGuid")?.Remove();
                        }
                    });
                var propertyGroup = xPropertyGroups.FirstOrDefault(w => w.Attribute("Condition") == null);
                if (propertyGroup == null)
                {
                    xProj.AddFirst(
                        new XElement("PropertyGroup", new XElement("ProjectGuid", prGuid)));
                    modified = true;
                }
                else
                {
                    var xProjectGuid = propertyGroup.Descendants()
                        .FirstOrDefault(w => w.Name.LocalName == "ProjectGuid");
                    if (xProjectGuid != null)
                    {
                        if (!Guid.TryParse(xProjectGuid.Value, out Guid p))
                        {
                            xProjectGuid.SetValue(prGuid);
                            modified = true;
                        }

                        guidPair.ProjectGuid = (prGuid = p.ToVsMode());

                        foreach (ProjGuidPair obj in State.ProjGuidsConfiguration.Entities)
                        {
                            if (obj.ProjectHash == guidPair.ProjectHash)
                            {
                                if (obj.ProjectGuid != guidPair.ProjectGuid)
                                {
                                    obj.ProjectGuid = guidPair.ProjectGuid;
                                    State.ProjGuidsConfiguration.StateChanged = true;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        propertyGroup.AddFirst(new XElement("ProjectGuid", prGuid));
                        modified = true;
                    }
                }

                if (guidPair.ProjectGuid != fileGuid) State.ProjGuidsConfiguration.StateChanged = true;

                return modified;
            }
        }

        public static string VersionFromPattern(string current, string pattern)
        {
            if (string.IsNullOrEmpty(current)) current = "1.0.0.0";
            if (!pattern.Contains("*") || string.IsNullOrEmpty(pattern)) return pattern;
            List<string> nVer = new List<string>();
            string[] t_patt = pattern.Split(".", StringSplitOptions.None);
            string[] t_curr = current.Split(".", StringSplitOptions.None);
            for (int i = 0; i < t_patt.Length; i++)
            {
                string vp = t_curr.GetValueOrDefault(i, (i + 1).ToString());

                if (!t_patt[i].Contains("*")) vp = t_patt.GetValueOrDefault(i, "");

                if (!string.IsNullOrWhiteSpace(vp)) nVer.Add(vp);
            }

            string outt = string.Join(".", nVer);

            return outt;
        }

        public static void ExtractCurrentVersionsFromNuSpec(XDocument nuSpec, ref string assemblyVersion,
            string defaultVersion)
        {
            string description = string.Empty;
            string assemblyFileVersion = string.Empty;
            string assemblyInformationalVersion = string.Empty;
            string assemblyInfoFilePath = string.Empty;
            _versionManager.ExtractVersion(ProjectType.NuSpec, nuSpec, null, null, ref description,
                ref assemblyFileVersion, ref assemblyVersion, ref assemblyInformationalVersion, ref assemblyInfoFilePath,
                defaultVersion);
        }

        public static void SetNewVersionsToNuSpec(XDocument nuSpec, string assemblyVersion,
            VersioningBaseConfiguration config, string PrereleaseSuffix = null)
        {
            bool saveprops = false, savecsproj = false, saveNuSpec = false;
            _versionManager.SetVersion(ref saveprops, ref savecsproj, ref saveNuSpec, nuSpec, config,
                ProjectType.NuSpec, string.Empty, string.Empty, assemblyVersion, string.Empty, null, null,
                PrereleaseSuffix);
        }

        public static void SetNewVersionsToSdk(XDocument project, VersioningBaseConfiguration config,
            string description,
            string assemblyFileVersion, string assemblyVersion, string assemblyInformationalVersion, string PrereleaseSuffix = null)
        {
            bool saveprops = false, savecsproj = false, saveNuSpec = false;
            _versionManager.SetVersion(ref saveprops, ref savecsproj, ref saveNuSpec, project, config,
                ProjectType.Sdk, description, assemblyFileVersion, assemblyVersion, assemblyInformationalVersion,
                null, null, PrereleaseSuffix);
        }

        public static void SetNewVersionsToProps(XDocument project, VersioningBaseConfiguration config,
            string description,
            string assemblyFileVersion, string assemblyVersion, string assemblyInformationalVersion, string PrereleaseSuffix = null)
        {
            bool saveprops = false, savecsproj = false, saveNuSpec = false;
            _versionManager.SetVersion(ref saveprops, ref savecsproj, ref saveNuSpec, project, config,
                ProjectType.Props, description, assemblyFileVersion, assemblyVersion, assemblyInformationalVersion,
                null, null, PrereleaseSuffix);
        }

        public static void ExtractCurrentVersionsFromSdk(XDocument csproj, VersioningBaseConfiguration config,
            ref string description, ref string assemblyFileVersion, ref string assemblyVersion,
            ref string assemblyInformationalVersion,
            string defaultVersion)
        {
            string assemblyInfoFilePath = string.Empty;
            _versionManager.ExtractVersion(ProjectType.Sdk, csproj, config, null, ref description,
                ref assemblyFileVersion, ref assemblyVersion, ref assemblyInformationalVersion, ref assemblyInfoFilePath,
                defaultVersion);
        }

        public static void ExtractCurrentVersionsFromProps(XDocument csproj, VersioningBaseConfiguration config,
            ref string description, ref string assemblyFileVersion, ref string assemblyVersion,
            ref string assemblyInformationalVersion,
            string defaultVersion)
        {
            string assemblyInfoFilePath = string.Empty;
            _versionManager.ExtractVersion(ProjectType.Props, csproj, config, null, ref description,
                ref assemblyFileVersion, ref assemblyVersion, ref assemblyInformationalVersion, ref assemblyInfoFilePath,
                defaultVersion);
        }

        public static void SetNewVersionsToAssemblyInfoFile(string assemblyInfoFilePath, string assemblyVersion,
            string assemblyInformationalVersion, string assemblyFileVersion, string PrereleaseSuffix = null)
        {
            _assemblyInfoHandler.SetNewVersionsToAssemblyInfoFile(assemblyInfoFilePath, assemblyVersion,
                assemblyInformationalVersion, assemblyFileVersion, PrereleaseSuffix);
        }

        public static JObject SetNewVersionsToPackageJson(string filePath, string assemblyVersion, string PrereleaseSuffix = null)
        {
            bool saveprops = false, savecsproj = false, saveNuSpec = false;
            _versionManager.SetVersion(ref saveprops, ref savecsproj, ref saveNuSpec, null, null,
                ProjectType.PackageJson, string.Empty, string.Empty, assemblyVersion, string.Empty, filePath, null,
                PrereleaseSuffix);
            return JObject.Parse(File.ReadAllText(filePath));
        }

        public static void ExtractCurrentVersionsFromPackageJson(string filePath, ref string assemblyFileVersion,
            ref string assemblyInformationalVersion, ref string assemblyVersion)
        {
            string description = string.Empty;
            string assemblyInfoFilePath = string.Empty;
            _versionManager.ExtractVersion(ProjectType.PackageJson, null, null, filePath, ref description,
                ref assemblyFileVersion, ref assemblyVersion, ref assemblyInformationalVersion, ref assemblyInfoFilePath,
                null);
        }

        public static string ExtractCurrentVersionFromAssemblyInfo(string cfgEntryAssemblyInfoFile, string filePath,
            ref string assemblyInfoFilePath, ref string assemblyFileVersion, ref string assemblyInformationalVersion,
            ref string assemblyVersion)
        {
            string description = string.Empty;
            _versionManager.ExtractVersion(ProjectType.AssemblyInfo, null, null, filePath, ref description,
                ref assemblyFileVersion, ref assemblyVersion, ref assemblyInformationalVersion, ref assemblyInfoFilePath,
                null);
            return string.Empty;
        }

        public static void VersionTsFile(ILogger log, string filePath, string buildLabel, string shortHash,
            string assemblyVersion, string assemblyFileVersion, string assemblyInformationalVersion)
        {
            _assemblyInfoHandler.VersionTsFile(filePath, buildLabel, shortHash, assemblyVersion, assemblyFileVersion,
                assemblyInformationalVersion);
        }

        public static void VersionNuspecFile(XDocument project, string filePath, string assemblyVersion)
        {
            _projectFileHandler.VersionNuspecFile(project, filePath, assemblyVersion);
        }

        public static void VersionCsProjFile(XDocument project, string filePath)
        {
            _projectFileHandler.VersionCsProjFile(project, filePath);
        }

        public static void VersionPropsFile(XDocument project, string filePath)
        {
            _projectFileHandler.VersionPropsFile(project, filePath);
        }

        public static void VersionFile(ref bool saveprops, ref bool savecsproj, ref bool saveNuSpec, XDocument project,
            VersioningBaseConfiguration config, ProjectType projectType, string description, string assemblyFileVersion,
            string assemblyVersion, string assemblyInformationalVersion, string filePath, string assemblyInfoFilePath,
            string PrereleaseSuffix = null)
        {
            _versionManager.SetVersion(ref saveprops, ref savecsproj, ref saveNuSpec, project, config, projectType,
                description, assemblyFileVersion, assemblyVersion, assemblyInformationalVersion, filePath,
                assemblyInfoFilePath, PrereleaseSuffix);
        }

        public static void CreateVersionTextFile(bool storeVersionFile, string projDir, ProjectType projectType,
            string assemblyVersion, ILogger log, string PrereleaseSuffix = null)
        {
            _versionManager.CreateVersionTextFile(storeVersionFile, projDir, projectType, assemblyVersion, PrereleaseSuffix);
        }

        public static void ConsoleOutPutCalculatedVersions(ref List<string> consoleOutputs, string workingFolder, string filePath,
            ProjectType projectType, string assemblyVersion, string assemblyInformationalVersion,
            string assemblyFileVersion)
        {
            _versionManager.ConsoleOutPutCalculatedVersions(ref consoleOutputs, workingFolder, filePath, projectType,
                assemblyVersion, assemblyInformationalVersion, assemblyFileVersion);
        }

        public static void GetVersionsFromFiles(ProjectType projectType, XDocument project,
            VersioningBaseConfiguration config, string filePath, ref string description, ref string assemblyFileVersion,
            ref string assemblyVersion, ref string assemblyInformationalVersion, ref string assemblyInfoFilePath,
            string defaultVersion, ILogger log)
        {
            _versionManager.ExtractVersion(projectType, project, config, filePath, ref description,
                ref assemblyFileVersion, ref assemblyVersion, ref assemblyInformationalVersion, ref assemblyInfoFilePath,
                defaultVersion);
        }

        public static void DecideProjectTypeFromFile(string filePath, ref ProjectType projectType,
            ref XDocument project)
        {
            _projectFileHandler.DecideProjectTypeFromFile(filePath, ref projectType, ref project);
        }
    }
}