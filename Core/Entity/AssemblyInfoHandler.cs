using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using AnubisWorks.Tools.Versioner.Helper;
using AnubisWorks.Tools.Versioner.Domain.Interfaces;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Entity
{
    public class AssemblyInfoHandler : IAssemblyInfoHandler
    {
        private readonly ILogger _logger;
        private const string pat =
            @"(^\s*\[\s*assembly\s*:\s*((System\s*\.)?\s*Reflection\s*\.)?\s*TYPE(Attribute)?\s*\(\s*@?\"")(?<version>(([0-9\*])+\.?)+(\W(\w)+)?)(?'smiec'.+)?(\""\s*\)\s*\])";

        private const RegexOptions
            opts = RegexOptions.CultureInvariant | RegexOptions.Multiline | RegexOptions.Compiled;

        private readonly Regex rAFV = new Regex(pat.Replace("TYPE", "AssemblyFileVersion"), opts);
        private readonly Regex rAIV = new Regex(pat.Replace("TYPE", "AssemblyInformationalVersion"), opts);
        private readonly Regex rAV = new Regex(pat.Replace("TYPE", "AssemblyVersion"), opts);

        public AssemblyInfoHandler(ILogger logger)
        {
            _logger = logger;
        }

        public void VersionTsFile(string filePath, string buildLabel, string shortHash,
            string assemblyVersion, string assemblyFileVersion, string assemblyInformationalVersion)
        {
            string tsVersionFile = Path.Combine(Path.GetDirectoryName(filePath), "ClientApp\\src\\assemblyinfo.ts");
            if (File.Exists(tsVersionFile))
            {
                _logger.Information("Generating TypeScript assemblyinfo.ts");
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("export class CommitInformation {");
                sb.AppendLine($"    Commit: string = \"{shortHash}\";");
                sb.AppendLine($"    BuildLabel: string = \"{buildLabel}\";");
                sb.AppendLine($"    Description: string = \"\";");
                sb.AppendLine($"    AssemblyVersion: string = \"{assemblyVersion}\";");
                sb.AppendLine($"    FileVersion: string = \"{assemblyFileVersion}\";");
                sb.AppendLine($"    InformationalVersion: string = \"{assemblyInformationalVersion}\";");
                sb.Append("}");
                File.WriteAllText(tsVersionFile, sb.ToString());
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
    }
} 