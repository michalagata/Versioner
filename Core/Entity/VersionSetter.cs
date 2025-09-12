using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnubisWorks.Tools.Versioner.Entity
{
    public class VersionSetter
    {
        private const string pat = @"(^\s*\[\s*assembly\s*:\s*((System\s*\.)?\s*Reflection\s*\.)?\s*TYPE(Attribute)?\s*\(\s*@?\"")(?<version>(([0-9\*])+\.?)+(\W(\w)+)?)(?'smiec'.+)?(\""\s*\)\s*\])";
        private const RegexOptions opts = RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Compiled;
        private readonly Regex rAFV = new Regex(pat.Replace("TYPE", "AssemblyFileVersion"), opts);
        private readonly Regex rAIV = new Regex(pat.Replace("TYPE", "AssemblyInformationalVersion"), opts);
        private readonly Regex rAV = new Regex(pat.Replace("TYPE", "AssemblyVersion"), opts);

        public void SetNewVersionsToNuSpec(XDocument nuSpec, string assemblyVersion,
            VersioningBaseConfiguration config, string PrereleaseSuffix = null)
        {
            var ns = nuSpec.Root.Name.Namespace;
            if (config.AssemblyVersionSet)
            {
                XElement version = nuSpec.Root.Elements().Where(o => o.Name.LocalName == "metadata").Elements()
                    .Where(o => o.Name.LocalName == "version").SingleOrDefault();
                if (version != null)
                {
                    if (string.IsNullOrEmpty(PrereleaseSuffix))
                    {
                        version.Value = assemblyVersion;
                    }
                    else
                    {
                        version.Value = assemblyVersion + "-" + PrereleaseSuffix;
                    }
                }
            }
        }

        public void SetNewVersionsToSdk(XDocument project, VersioningBaseConfiguration config,
            string description,
            string assemblyFileVersion, string assemblyVersion, string assemblyInformationalVersion, string PrereleaseSuffix = null)
        {
            List<XElement> vnodes = new List<XElement>();
            
            // Use either Version OR VersionPrefix+VersionSuffix, not both
            if (string.IsNullOrEmpty(PrereleaseSuffix))
            {
                // No suffix - use Version property
                if (config.AssemblyInfoVersionSet) vnodes.Add(new XElement("Version", assemblyInformationalVersion));
            }
            else
            {
                // Has suffix - use VersionPrefix + VersionSuffix instead of Version
                if (config.AssemblyVersionSet) vnodes.Add(new XElement("VersionPrefix", assemblyVersion));
                if (config.AssemblyVersionSet) vnodes.Add(new XElement("VersionSuffix", SanitizeVersionSuffix(PrereleaseSuffix)));
            }
            
            if (config.AssemblyVersionSet) vnodes.Add(new XElement("AssemblyVersion", assemblyVersion));
            if (config.AssemblyFileVersionSet) vnodes.Add(new XElement("FileVersion", assemblyFileVersion));
            if (config.AssemblyInfoVersionSet) vnodes.Add(new XElement("Description", description));

            project
                .Element("Project")
                .Add(new XElement("PropertyGroup", vnodes));
        }

        public void SetNewVersionsToProps(XDocument project, VersioningBaseConfiguration config,
            string description,
            string assemblyFileVersion, string assemblyVersion, string assemblyInformationalVersion, string PrereleaseSuffix = null)
        {
            List<XElement> vnodes = new List<XElement>();
            
            // Use either Version OR VersionPrefix+VersionSuffix, not both
            if (string.IsNullOrEmpty(PrereleaseSuffix))
            {
                // No suffix - use Version property
                if (config.AssemblyInfoVersionSet) vnodes.Add(new XElement("Version", assemblyInformationalVersion));
            }
            else
            {
                // Has suffix - use VersionPrefix + VersionSuffix instead of Version
                if (config.AssemblyVersionSet) vnodes.Add(new XElement("VersionPrefix", assemblyVersion));
                if (config.AssemblyVersionSet) vnodes.Add(new XElement("VersionSuffix", SanitizeVersionSuffix(PrereleaseSuffix)));
            }
            
            if (config.AssemblyVersionSet) vnodes.Add(new XElement("AssemblyVersion", assemblyVersion));
            if (config.AssemblyFileVersionSet) vnodes.Add(new XElement("FileVersion", assemblyFileVersion));
            if (config.AssemblyInfoVersionSet) vnodes.Add(new XElement("Description", description));

            project
                .Element("Project")
                .Add(new XElement("PropertyGroup", vnodes));
        }

        public void SetNewVersionsToAssemblyInfoFile(string assemblyInfoFilePath, string assemblyVersion,
            string assemblyInformationalVersion, string assemblyFileVersion, string PrereleaseSuffix = null)
        {
            var aifile = assemblyInfoFilePath;
            if (File.Exists(aifile))
            {
                string content = File.ReadAllText(aifile);

                content = rAV.ReplaceGroup(content, "version", assemblyVersion);
                if (string.IsNullOrEmpty(PrereleaseSuffix))
                {
                    content = rAIV.ReplaceGroup(content, "version", assemblyInformationalVersion);
                }
                else
                {
                    content = rAIV.ReplaceGroup(content, "version", assemblyInformationalVersion + "-" + PrereleaseSuffix);
                }

                content = rAFV.ReplaceGroup(content, "version", assemblyFileVersion);
                File.WriteAllText(aifile, content, Encoding.UTF8);
            }
        }

        public JObject SetNewVersionsToPackageJson(string filePath, string assemblyVersion, string PrereleaseSuffix = null)
        {
            var jObject = JObject.Parse(File.ReadAllText(filePath));
            if (string.IsNullOrEmpty(PrereleaseSuffix))
            {
                jObject["version"] = assemblyVersion;
            }
            else
            {
                jObject["version"] = assemblyVersion + "-" + PrereleaseSuffix;
            }

            return jObject;
        }

        /// <summary>
        /// Sanitizes version suffix by removing invalid characters for NuGet versioning.
        /// Replaces forward slashes with hyphens and removes other invalid characters.
        /// </summary>
        /// <param name="suffix">The version suffix to sanitize</param>
        /// <returns>Sanitized version suffix</returns>
        private static string SanitizeVersionSuffix(string suffix)
        {
            if (string.IsNullOrEmpty(suffix))
                return string.Empty;

            // Replace forward slashes with hyphens (common in branch names)
            // Remove other invalid characters for NuGet versioning
            return suffix
                .Replace("/", "-")
                .Replace("\\", "-")
                .Replace(" ", "-")
                .Replace(":", "-")
                .Replace("*", "-")
                .Replace("?", "-")
                .Replace("\"", "-")
                .Replace("<", "-")
                .Replace(">", "-")
                .Replace("|", "-")
                .Trim('-'); // Remove leading/trailing hyphens
        }
    }
} 