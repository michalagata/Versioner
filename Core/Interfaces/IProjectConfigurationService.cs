using AnubisWorks.Tools.Versioner.Model;

namespace AnubisWorks.Tools.Versioner.Interfaces
{
    public interface IProjectConfigurationService
    {
        VersioningBaseConfiguration GetProjectConfiguration(string relativePath);
        
        void ApplyCustomProjectSettings(
            VersioningBaseConfiguration config, 
            string projectFileName, 
            CustomProjectSettings customProjectSettings);
    }
} 