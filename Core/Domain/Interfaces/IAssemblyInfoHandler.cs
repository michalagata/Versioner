using System;
using System.IO;
using System.Text;

namespace AnubisWorks.Tools.Versioner.Domain.Interfaces
{
    public interface IAssemblyInfoHandler
    {
        void VersionTsFile(string filePath, string buildLabel, string shortHash,
            string assemblyVersion, string assemblyFileVersion, string assemblyInformationalVersion);

        string ExtractCurrentVersionFromAssemblyInfo(string cfgEntryAssemblyInfoFile, string filePath,
            ref string assemblyInfoFilePath, ref string assemblyFileVersion, ref string assemblyInformationalVersion,
            ref string assemblyVersion);

        void SetNewVersionsToAssemblyInfoFile(string assemblyInfoFilePath, string assemblyVersion,
            string assemblyInformationalVersion, string assemblyFileVersion, string PrereleaseSuffix = null);
    }
} 