using System.Xml.Linq;
using AnubisWorks.Tools.Versioner.Model;

namespace AnubisWorks.Tools.Versioner.Domain.Interfaces
{
    public interface IProjectFileHandler
    {
        void VersionNuspecFile(XDocument project, string filePath, string assemblyVersion);
        void VersionCsProjFile(XDocument project, string filePath);
        void VersionPropsFile(XDocument project, string filePath);
        void DecideProjectTypeFromFile(string filePath, ref ProjectType projectType, ref XDocument project);
    }
} 