using System.Collections.Generic;
using AnubisWorks.Tools.Versioner.Model;
using AnubisWorks.Tools.Versioner.Interfaces;

namespace AnubisWorks.Tools.Versioner.Application.Commands
{
    public class ProcessMonoRepoProjectCommand : IRequest<bool>
    {
        public string FilePath { get; set; }
        public string WorkingFolder { get; set; }
        public List<string> TcOutputs { get; set; }
        public string BuildLabel { get; set; }
        public bool StoreVersionFile { get; set; }
        public CustomProjectSettings CustomProjectSettings { get; set; }
        public VersionedSetModel CalculatedModel { get; set; }

        public ProcessMonoRepoProjectCommand(
            string filePath,
            string workingFolder,
            List<string> tcOutputs,
            string buildLabel,
            bool storeVersionFile,
            CustomProjectSettings customProjectSettings,
            VersionedSetModel calculatedModel)
        {
            FilePath = filePath;
            WorkingFolder = workingFolder;
            TcOutputs = tcOutputs;
            BuildLabel = buildLabel;
            StoreVersionFile = storeVersionFile;
            CustomProjectSettings = customProjectSettings;
            CalculatedModel = calculatedModel;
        }
    }
} 