using System.Collections.Generic;
using AnubisWorks.Tools.Versioner.Model;
using AnubisWorks.Tools.Versioner.Interfaces;

namespace AnubisWorks.Tools.Versioner.Application.Commands
{
    public class CalculateProjectVersionCommand : IRequest<VersionedSetModel>
    {
        public string GitPath { get; set; }
        public string FilePath { get; set; }
        public string WorkingFolder { get; set; }
        public List<string> ConsoleOutputs { get; set; }
        public string BuildLabel { get; set; }
        public bool StoreVersionFile { get; set; }
        public MajorMinorPatchHotfixModel PatchHotfixModel { get; set; }
        public CustomProjectSettings CustomProjectSettings { get; set; }
        public int SemVersion { get; set; }
        public string PrereleaseSuffix { get; set; }
        public string DefinedPatch { get; set; }
        public bool CalculateMonoMode { get; set; }

        public CalculateProjectVersionCommand(
            string gitPath,
            string filePath,
            string workingFolder,
            List<string> consoleOutputs,
            string buildLabel,
            bool storeVersionFile,
            MajorMinorPatchHotfixModel patchHotfixModel,
            CustomProjectSettings customProjectSettings,
            int semVersion,
            string prereleaseSuffix,
            string definedPatch,
            bool calculateMonoMode = false)
        {
            GitPath = gitPath;
            FilePath = filePath;
            WorkingFolder = workingFolder;
            ConsoleOutputs = consoleOutputs;
            BuildLabel = buildLabel;
            StoreVersionFile = storeVersionFile;
            PatchHotfixModel = patchHotfixModel;
            CustomProjectSettings = customProjectSettings;
            SemVersion = semVersion;
            PrereleaseSuffix = prereleaseSuffix;
            DefinedPatch = definedPatch;
            CalculateMonoMode = calculateMonoMode;
        }
    }
} 