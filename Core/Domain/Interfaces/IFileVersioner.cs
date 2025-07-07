using System.Xml.Linq;
using AnubisWorks.Tools.Versioner.Model;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Domain.Interfaces
{
    public interface IFileVersioner
    {
        void VersionNuspecFile(XDocument project, string filePath, string assemblyVersion);
        void VersionCsProjFile(XDocument project, string filePath);
        void VersionPropsFile(XDocument project, string filePath);
        void VersionTsFile(string filePath, string buildLabel, string shortHash,
            string assemblyVersion, string assemblyFileVersion, string assemblyInformationalVersion);
        void CreateVersionTextFile(bool storeVersionFile, string projDir, ProjectType projectType, 
            string assemblyVersion, ILogger log, string PrereleaseSuffix = null);
    }
} 