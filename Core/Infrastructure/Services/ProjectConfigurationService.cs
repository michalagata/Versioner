using System.IO;
using System.Linq;
using AnubisWorks.Tools.Versioner.Helper;
using AnubisWorks.Tools.Versioner.Interfaces;
using AnubisWorks.Tools.Versioner.Model;

namespace AnubisWorks.Tools.Versioner.Infrastructure.Services
{
    public class ProjectConfigurationService : IProjectConfigurationService
    {
        public VersioningBaseConfiguration GetProjectConfiguration(string relativePath)
        {
            if (State.Config == null)
            {
                throw new InvalidOperationException("State.Config is null. Configuration not initialized.");
            }
            
            var config = State.Config.Items?.FirstOrDefault(f => f.ProjectFile?.AreEqual(relativePath) ?? false) ??
                         State.Config.Default;
            config.ProjectFile = relativePath;
            return config;
        }

        public void ApplyCustomProjectSettings(
            VersioningBaseConfiguration config,
            string projectFileName,
            CustomProjectSettings customProjectSettings)
        {
            config.Directories = customProjectSettings.ProjectSettings?
                .FirstOrDefault(s => s.ProjectName?.AreEqual(Path.GetFileNameWithoutExtension(projectFileName)) ?? false)
                ?.Directories;
        }
    }
} 