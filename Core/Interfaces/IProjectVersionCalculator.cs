using System.Collections.Generic;
using AnubisWorks.Tools.Versioner.Model;

namespace AnubisWorks.Tools.Versioner.Interfaces
{
    public interface IProjectVersionCalculator
    {
        VersionedSetModel CalculateVersion(
            string gitPath,
            string filePath,
            string workingFolder,
            ref List<string> consoleOutputs,
            string buildLabel,
            bool storeVersionFile,
            MajorMinorPatchHotfixModel patchHotfixModel,
            CustomProjectSettings customProjectSettings,
            string prereleaseSuffix,
            string definedPatch,
            bool calculateMonoMode = false);

        VersionedSetModel ProcessMonoRepoProject(
            string filePath,
            string workingFolder,
            ref List<string> tcOutputs,
            string buildLabel,
            bool storeVersionFile,
            CustomProjectSettings customProjectSettings,
            VersionedSetModel calculatedModel);
    }
} 