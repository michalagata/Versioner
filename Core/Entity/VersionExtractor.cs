using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using AnubisWorks.Tools.Versioner.Helper;
using Newtonsoft.Json.Linq;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Entity
{
    public class VersionExtractor
    {
        private readonly ILogger _logger;
        private const string pat = @"(^\s*\[\s*assembly\s*:\s*((System\s*\.)?\s*Reflection\s*\.)?\s*TYPE(Attribute)?\s*\(\s*@?\"")(?<version>(([0-9\*])+\.?)+(\W(\w)+)?)(?'smiec'.+)?(\""\s*\)\s*\])";
        private const RegexOptions opts = RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Compiled;
        private static readonly Regex rAFV = new Regex(pat.Replace("TYPE", "AssemblyFileVersion"), opts);
        private static readonly Regex rAIV = new Regex(pat.Replace("TYPE", "AssemblyInformationalVersion"), opts);
        private static readonly Regex rAV = new Regex(pat.Replace("TYPE", "AssemblyVersion"), opts);

        public VersionExtractor(ILogger logger)
        {
            _logger = logger;
        }

        public void ExtractCurrentVersionsFromSdk(XDocument csproj, VersioningBaseConfiguration config,
            ref string description, ref string assemblyFileVersion, ref string assemblyVersion,
            ref string assemblyInformationalVersion, string defaultVersion)
        {
            string assemblyInformationalVersion_ = null;
            string assemblyVersion_ = null;
            string assemblyFileVersion_ = null;
            string description_ = null;

            csproj.Element("Project").XPathSelectElements("PropertyGroup").ToList().ForEach(xnPG =>
            {
                if (xnPG.Attribute("Condition") != null)
                {
                    xnPG.Element("Version")?.Remove();
                    xnPG.Element("FileVersion")?.Remove();
                    xnPG.Element("AssemblyVersion")?.Remove();
                    xnPG.Element("Description")?.Remove();
                }

                if (xnPG.Element("Version") != null)
                {
                    assemblyInformationalVersion_ = xnPG.Element("Version")?.Value ?? defaultVersion;
                    if (config.AssemblyInfoVersionSet) xnPG.Element("Version")?.Remove();
                }

                if (xnPG.Element("AssemblyVersion") != null)
                {
                    assemblyVersion_ = xnPG.Element("AssemblyVersion")?.Value ?? defaultVersion;
                    if (config.AssemblyVersionSet) xnPG.Element("AssemblyVersion")?.Remove();
                }

                if (xnPG.Element("FileVersion") != null)
                {
                    assemblyFileVersion_ = xnPG.Element("FileVersion")?.Value ?? defaultVersion;
                    if (config.AssemblyFileVersionSet) xnPG.Element("FileVersion")?.Remove();
                }

                if (xnPG.Element("Description") != null)
                {
                    description_ = xnPG.Element("Description")?.Value ?? defaultVersion;
                    if (config.HashAsDescription) xnPG.Element("Description")?.Remove();
                }

                if (xnPG.Element("PackageVersion") != null)
                {
                    xnPG.Element("PackageVersion")?.Remove();
                }
            });

            assemblyInformationalVersion = assemblyInformationalVersion_;
            assemblyVersion = assemblyVersion_;
            assemblyFileVersion = assemblyFileVersion_;
            description = description_;
        }

        public void ExtractCurrentVersionsFromProps(XDocument csproj, VersioningBaseConfiguration config,
            ref string description, ref string assemblyFileVersion, ref string assemblyVersion,
            ref string assemblyInformationalVersion, string defaultVersion)
        {
            string assemblyInformationalVersion_ = null;
            string assemblyVersion_ = null;
            string assemblyFileVersion_ = null;
            string description_ = null;

            csproj.Element("Project").XPathSelectElements("PropertyGroup").ToList().ForEach(xnPG =>
            {
                if (xnPG.Attribute("Condition") != null)
                {
                    xnPG.Element("Version")?.Remove();
                    xnPG.Element("FileVersion")?.Remove();
                    xnPG.Element("AssemblyVersion")?.Remove();
                    xnPG.Element("Description")?.Remove();
                }

                if (xnPG.Element("Version") != null)
                {
                    assemblyInformationalVersion_ = xnPG.Element("Version")?.Value ?? defaultVersion;
                    if (config.AssemblyInfoVersionSet) xnPG.Element("Version")?.Remove();
                }

                if (xnPG.Element("AssemblyVersion") != null)
                {
                    assemblyVersion_ = xnPG.Element("AssemblyVersion")?.Value ?? defaultVersion;
                    if (config.AssemblyVersionSet) xnPG.Element("AssemblyVersion")?.Remove();
                }

                if (xnPG.Element("FileVersion") != null)
                {
                    assemblyFileVersion_ = xnPG.Element("FileVersion")?.Value ?? defaultVersion;
                    if (config.AssemblyFileVersionSet) xnPG.Element("FileVersion")?.Remove();
                }

                if (xnPG.Element("Description") != null)
                {
                    description_ = xnPG.Element("Description")?.Value ?? defaultVersion;
                    if (config.HashAsDescription) xnPG.Element("Description")?.Remove();
                }

                if (xnPG.Element("PackageVersion") != null)
                {
                    xnPG.Element("PackageVersion")?.Remove();
                }
            });

            assemblyInformationalVersion = assemblyInformationalVersion_;
            assemblyVersion = assemblyVersion_;
            assemblyFileVersion = assemblyFileVersion_;
            description = description_;
        }

        public void ExtractCurrentVersionsFromNuSpec(XDocument nuSpec, ref string assemblyVersion,
            string defaultVersion)
        {
            XElement serial = nuSpec.Root.Elements().Where(o => o.Name.LocalName == "metadata").SingleOrDefault();

            foreach (XElement xElement in serial.Elements().Where(o => o.Name.LocalName == "version").ToList())
            {
                if (xElement != null)
                {
                    assemblyVersion = xElement.Value ?? defaultVersion;
                }
            }
        }

        public void ExtractCurrentVersionsFromPackageJson(string filePath, ref string assemblyFileVersion,
            ref string assemblyInformationalVersion, ref string assemblyVersion)
        {
            var jObject = JObject.Parse(File.ReadAllText(filePath));
            if (jObject.TryGetValue("version", StringComparison.InvariantCultureIgnoreCase, out JToken jtVersion))
            {
                assemblyFileVersion = assemblyInformationalVersion = assemblyVersion = jtVersion.Value<string>();
            }
        }

        public string ExtractCurrentVersionFromAssemblyInfo(string cfgEntryAssemblyInfoFile, string filePath,
            ref string assemblyInfoFilePath, ref string assemblyFileVersion, ref string assemblyInformationalVersion,
            ref string assemblyVersion)
        {
            string aifile = string.Empty;
            if(PlatformDetector.GetOperatingSystem() == OSPlatform.Windows) aifile = assemblyInfoFilePath =
                Path.Combine(Path.GetDirectoryName(filePath), cfgEntryAssemblyInfoFile.ToWindowsPath());
            if(PlatformDetector.GetOperatingSystem() == OSPlatform.Linux || PlatformDetector.GetOperatingSystem() == OSPlatform.OSX) aifile = assemblyInfoFilePath =
                Path.Combine(Path.GetDirectoryName(filePath), cfgEntryAssemblyInfoFile.ToLinuxPath());
            if (File.Exists(aifile))
            {
                string content = File.ReadAllText(aifile);
                {
                    var mAFV = rAFV.Match(content);
                    if (mAFV.Success)
                    {
                        assemblyFileVersion = mAFV.Groups["version"].Value;
                    }
                }

                {
                    var mAIV = rAIV.Match(content);
                    if (mAIV.Success)
                    {
                        assemblyInformationalVersion = mAIV.Groups["version"].Value;
                    }
                }

                {
                    var mAV = rAV.Match(content);
                    if (mAV.Success)
                    {
                        assemblyVersion = mAV.Groups["version"].Value;
                    }
                }
            }
            else
            {
                return string.Format("AssemblyInfo File not found. {file}", aifile);
            }

            return string.Empty;
        }
    }
} 