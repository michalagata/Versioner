using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using AnubisWorks.Tools.Versioner.Helper;
using AnubisWorks.Tools.Versioner.Domain.Interfaces;
using AnubisWorks.Tools.Versioner.Model;

namespace AnubisWorks.Tools.Versioner.Entity
{
    public class ProjectFileHandler : IProjectFileHandler
    {
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

            // Only rename file if it contains a version in its name
            string fileName = Path.GetFileName(filePath);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            // Check if filename contains a version pattern (e.g., "package.1.0.0" or "package-1.0.0")
            if (System.Text.RegularExpressions.Regex.IsMatch(fileNameWithoutExt, @"[\.\-]\d+\.\d+"))
            {
                FilePathHelper.RenameFile(filePath, assemblyVersion);
            }
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

        public void DecideProjectTypeFromFile(string filePath, ref ProjectType projectType,
            ref XDocument project)
        {
            if (filePath.EndsWith(".nuspec"))
            {
                projectType = ProjectType.NuSpec;
                project = XDocument.Parse(File.ReadAllText(filePath));
            }
            else if (filePath.EndsWith(".props"))
            {
                projectType = ProjectType.Props;
                project = XDocument.Parse(File.ReadAllText(filePath));
            }
            else if (filePath.EndsWith("package.json"))
            {
                projectType = ProjectType.PackageJson;
            }
            else if (filePath.EndsWith("Dockerfile") || filePath.EndsWith(".dockerfile") || 
                     filePath.EndsWith("docker-compose.yml") || filePath.EndsWith("compose.yml") ||
                     filePath.EndsWith(".yaml") || filePath.EndsWith(".yml"))
            {
                // These file types are handled by specialized services (DockerVersioningService, YamlVersioningService)
                // and should not be parsed as XML
                projectType = ProjectType.PackageJson; // Use a non-XML type to skip XML parsing
                project = null;
            }
            else
            {
                // Only try to parse as XML if it's likely an XML file (.csproj, etc.)
                try
                {
                    project = XDocument.Parse(File.ReadAllText(filePath));
                    projectType = (project.XPathSelectElement("Project")?.Attribute("Sdk") != null)
                        ? ProjectType.Sdk
                        : ProjectType.AssemblyInfo;
                }
                catch (System.Xml.XmlException)
                {
                    // File is not valid XML, skip it
                    projectType = ProjectType.PackageJson; // Use a non-XML type to skip XML parsing
                    project = null;
                }
            }
        }
    }
} 