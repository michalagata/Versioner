using System;
using System.IO;
using AnubisWorks.Tools.Versioner.Helper;
using AnubisWorks.Tools.Versioner.Model;
using AnubisWorks.Tools.Versioner.Interfaces;
using Newtonsoft.Json;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Services
{
    public interface IOverrideVersioningService
    {
        (bool exists, string fileUrl, MajorMinorPatchHotfixModel model) LoadOverrideConfiguration(string workFolder, string definedPatch);
    }

    public class OverrideVersioningService : IOverrideVersioningService
    {
        private readonly ILogger _log;
        private readonly IFileOperations _fileOperations;

        public OverrideVersioningService(ILogger log, IFileOperations fileOperations)
        {
            _log = log;
            _fileOperations = fileOperations;
        }

        public (bool exists, string fileUrl, MajorMinorPatchHotfixModel model) LoadOverrideConfiguration(string workFolder, string definedPatch)
        {
            bool overrideExists = false;
            string overrideFileUrl = string.Empty;
            MajorMinorPatchHotfixModel overrideModel = new MajorMinorPatchHotfixModel { Major = 0, Minor = 0, Patch = 0, Hotfix = 0 };

            _log.Information("Looking for ProjectOverride file");
            if (_fileOperations.FileExists(Path.Combine(workFolder, "ProjectOverride.json")))
            {
                overrideExists = true;
                overrideFileUrl = Path.Combine(workFolder, "ProjectOverride.json");
                _log.Debug($"Found topper-structure ProjectOverride file under {overrideFileUrl}");
            }

            if (overrideExists)
            {
                _log.Debug($"Loading up ProjectOverride file: {overrideFileUrl}");
                overrideModel = JsonConvert.DeserializeObject<MajorMinorPatchHotfixModel>(_fileOperations.ReadFileContent(overrideFileUrl));
                if (!string.IsNullOrEmpty(definedPatch))
                {
                    overrideModel.Patch = int.Parse(definedPatch);
                }
            }

            return (overrideExists, overrideFileUrl, overrideModel);
        }
    }
} 