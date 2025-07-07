using System.Collections.Generic;
using System.Xml.Linq;
using AnubisWorks.Tools.Versioner.Model;

namespace AnubisWorks.Tools.Versioner.Domain.Interfaces
{
    public interface IVersionManager
    {
        void SetVersion(ref bool saveprops, ref bool savecsproj, ref bool saveNuSpec, XDocument project,
            VersioningBaseConfiguration config, ProjectType projectType, string description, string assemblyFileVersion,
            string assemblyVersion, string assemblyInformationalVersion, string filePath, string assemblyInfoFilePath,
            string PrereleaseSuffix = null);

        void ExtractVersion(ProjectType projectType, XDocument project,
            VersioningBaseConfiguration config, string filePath, ref string description, ref string assemblyFileVersion,
            ref string assemblyVersion, ref string assemblyInformationalVersion, ref string assemblyInfoFilePath,
            string defaultVersion);

        void CreateVersionTextFile(bool storeVersionFile, string projDir, ProjectType projectType,
            string assemblyVersion, string PrereleaseSuffix = null);

        void ConsoleOutPutCalculatedVersions(ref List<string> consoleOutputs, string workingFolder, string filePath,
            ProjectType projectType, string assemblyVersion, string assemblyInformationalVersion,
            string assemblyFileVersion);
    }
} 