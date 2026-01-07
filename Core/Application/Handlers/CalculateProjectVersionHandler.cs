using System.Collections.Generic;
using System.Threading.Tasks;
using AnubisWorks.Tools.Versioner.Application.Commands;
using AnubisWorks.Tools.Versioner.Interfaces;
using AnubisWorks.Tools.Versioner.Model;

namespace AnubisWorks.Tools.Versioner.Application.Handlers
{
    public class CalculateProjectVersionHandler : IRequestHandler<CalculateProjectVersionCommand, VersionedSetModel>
    {
        private readonly IProjectVersionCalculator _projectVersionCalculator;

        public CalculateProjectVersionHandler(IProjectVersionCalculator projectVersionCalculator)
        {
            _projectVersionCalculator = projectVersionCalculator;
        }

        public async Task<VersionedSetModel> Handle(CalculateProjectVersionCommand request)
        {
            var consoleOutputs = request.ConsoleOutputs;
            
            var result = _projectVersionCalculator.CalculateVersion(
                request.GitPath,
                request.FilePath,
                request.WorkingFolder,
                ref consoleOutputs,
                request.BuildLabel,
                request.StoreVersionFile,
                request.PatchHotfixModel,
                request.CustomProjectSettings,
                request.PrereleaseSuffix,
                request.DefinedPatch,
                request.CalculateMonoMode);

            // Aktualizacja listy wyjściowej
            request.ConsoleOutputs.Clear();
            request.ConsoleOutputs.AddRange(consoleOutputs);

            return await Task.FromResult(result);
        }
    }
} 