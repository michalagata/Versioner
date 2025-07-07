using System.Collections.Generic;
using System.Threading.Tasks;
using AnubisWorks.Tools.Versioner.Application.Commands;
using AnubisWorks.Tools.Versioner.Interfaces;
using AnubisWorks.Tools.Versioner.Model;

namespace AnubisWorks.Tools.Versioner.Application.Handlers
{
    public class ProcessMonoRepoProjectHandler : IRequestHandler<ProcessMonoRepoProjectCommand, bool>
    {
        private readonly IProjectVersionCalculator _projectVersionCalculator;

        public ProcessMonoRepoProjectHandler(IProjectVersionCalculator projectVersionCalculator)
        {
            _projectVersionCalculator = projectVersionCalculator;
        }

        public async Task<bool> Handle(ProcessMonoRepoProjectCommand request)
        {
            var tcOutputs = request.TcOutputs;
            
            _projectVersionCalculator.ProcessMonoRepoProject(
                request.FilePath,
                request.WorkingFolder,
                ref tcOutputs,
                request.BuildLabel,
                request.StoreVersionFile,
                request.CustomProjectSettings,
                request.CalculatedModel);

            // Aktualizacja listy wyjściowej
            request.TcOutputs.Clear();
            request.TcOutputs.AddRange(tcOutputs);

            return await Task.FromResult(true);
        }
    }
} 