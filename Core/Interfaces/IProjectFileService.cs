using System.Xml.Linq;
using AnubisWorks.Tools.Versioner.Model;

namespace AnubisWorks.Tools.Versioner.Interfaces
{
    public interface IProjectFileService
    {
        void ProcessProjectFile(
            bool saveprops,
            bool savecsproj,
            bool saveNuSpec,
            XDocument project,
            string filePath,
            string assemblyVersion);

        void VersionTypeScriptFile(
            string filePath,
            string buildLabel,
            string shortHash,
            string assemblyVersion,
            string assemblyFileVersion,
            string assemblyInformationalVersion);

        void CreateVersionTextFile(
            bool storeVersionFile,
            string projDir,
            ProjectType projectType,
            string assemblyVersion,
            string prereleaseSuffix = null);
    }
} 