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
                project.ToString(SaveOptions.None).Replace("-&gt;", "->")
                    .Replace("<ProjectGuid xmlns=\"\">", "<ProjectGuid>"));
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
            }
            else if (filePath.EndsWith("package.json"))
            {
                projectType = ProjectType.PackageJson;
            }
            else
            {
                project = XDocument.Parse(File.ReadAllText(filePath));
                projectType = (project.XPathSelectElement("Project")?.Attribute("Sdk") != null)
                    ? ProjectType.Sdk
                    : ProjectType.AssemblyInfo;
            }
        }
    }
} 