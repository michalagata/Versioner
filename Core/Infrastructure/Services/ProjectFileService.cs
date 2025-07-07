using System.Xml.Linq;
using AnubisWorks.Tools.Versioner.Entity;
using AnubisWorks.Tools.Versioner.Interfaces;

namespace AnubisWorks.Tools.Versioner.Infrastructure.Services
{
    public class ProjectFileService : IProjectFileService
    {
        public void ProcessProjectFile(
            bool saveprops,
            bool savecsproj,
            bool saveNuSpec,
            XDocument project,
            string filePath,
            string assemblyVersion)
        {
            if (savecsproj)
            {
                ProjectEntityStatic.VersionCsProjFile(project, filePath);
            }

            if (saveprops)
            {
                ProjectEntityStatic.VersionPropsFile(project, filePath);
            }

            if (saveNuSpec)
            {
                ProjectEntityStatic.VersionNuspecFile(project, filePath, assemblyVersion);
            }
        }

        public void VersionTypeScriptFile(
            string filePath,
            string buildLabel,
            string shortHash,
            string assemblyVersion,
            string assemblyFileVersion,
            string assemblyInformationalVersion)
        {
            ProjectEntityStatic.VersionTsFile(
                null, // Logger będzie przekazany przez dependency injection
                filePath,
                buildLabel,
                shortHash,
                assemblyVersion,
                assemblyFileVersion,
                assemblyInformationalVersion);
        }

        public void CreateVersionTextFile(
            bool storeVersionFile,
            string projDir,
            ProjectType projectType,
            string assemblyVersion,
            string prereleaseSuffix = null)
        {
            ProjectEntityStatic.CreateVersionTextFile(
                storeVersionFile,
                projDir,
                projectType,
                assemblyVersion,
                null, // Logger będzie przekazany przez dependency injection
                prereleaseSuffix);
        }
    }
} 