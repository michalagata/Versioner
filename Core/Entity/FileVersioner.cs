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
    public class FileVersioner : IFileVersioner
    {
        private readonly ILogger _log;

        public FileVersioner(ILogger log)
        {
            _log = log;
        }

        public void VersionNuspecFile(XDocument project, string filePath, string assemblyVersion)
        {
            foreach (XElement child in project.Descendants().Reverse())
            {
                if (!child.HasElements && string.IsNullOrEmpty(child.Value) && !child.HasAttributes) child.Remove();
            }

            if (File.Exists($"{filePath}.bak")) File.Delete($"{filePath}.bak");
            File.Move(filePath, $"{filePath}.bak");

            File.WriteAllText($"{filePath}",
                project.ToString(SaveOptions.None).Replace("-&gt;", "->"));

            FilePathHelper.RenameFile(filePath, assemblyVersion);
        }

        public void VersionCsProjFile(XDocument project, string filePath)
        {
            foreach (XElement child in project.Descendants().Reverse())
            {
                if (!child.HasElements && string.IsNullOrEmpty(child.Value) && !child.HasAttributes) child.Remove();
            }

            if (File.Exists($"{filePath}.bak")) File.Delete($"{filePath}.bak");
            File.Move(filePath, $"{filePath}.bak");

            File.WriteAllText($"{filePath}",
                project.ToString(SaveOptions.None).Replace("-&gt;", "->")
                    .Replace("<ProjectGuid xmlns=\"\">", "<ProjectGuid>"));
        }

        public void VersionPropsFile(XDocument project, string filePath)
        {
            foreach (XElement child in project.Descendants().Reverse())
            {
                if (!child.HasElements && string.IsNullOrEmpty(child.Value) && !child.HasAttributes) child.Remove();
            }

            if (File.Exists($"{filePath}.bak")) File.Delete($"{filePath}.bak");
            File.Move(filePath, $"{filePath}.bak");

            File.WriteAllText($"{filePath}",
                project.ToString(SaveOptions.None).Replace("-&gt;", "->"));
        }

        public void VersionTsFile(string filePath, string buildLabel, string shortHash,
            string assemblyVersion, string assemblyFileVersion, string assemblyInformationalVersion)
        {
            string tsVersionFile = Path.Combine(Path.GetDirectoryName(filePath), "ClientApp", "src", "assemblyinfo.ts");
            if (File.Exists(tsVersionFile))
            {
                _log.Information("Generating TypeScript assemblyinfo.ts");
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

        public void CreateVersionTextFile(bool storeVersionFile, string projDir, ProjectType projectType, string assemblyVersion, ILogger log, string PrereleaseSuffix = null)
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
                    log.Warning("Problem creating version.txt file. {err}", e.Message);
                }
            }
        }
    }
} 