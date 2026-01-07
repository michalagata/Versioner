using System.Collections.Generic;
using AnubisWorks.Tools.Versioner.Model;
using AnubisWorks.Tools.Versioner.Helper;

namespace AnubisWorks.Tools.Versioner.Interfaces
{
    /// <summary>
    /// Interface for versioning strategy plugins.
    /// Allows easy extension of versioning policies (Semantic, Calendar, Git Flow, Conventional Commits, etc.)
    /// </summary>
    public interface IVersioningStrategy
    {
        /// <summary>
        /// Name of the strategy (e.g., "Semantic", "Calendar", "GitFlow")
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Description of the strategy
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Calculates version components based on Git history and context.
        /// </summary>
        /// <param name="context">Context containing Git log entries, file path, configuration, etc.</param>
        /// <returns>Version calculation result with all components</returns>
        VersionCalculationResult CalculateVersion(VersioningContext context);

        /// <summary>
        /// Checks if this strategy supports the given format or artifact type.
        /// </summary>
        /// <param name="format">Format name (e.g., "dotnet", "npm", "docker")</param>
        /// <returns>True if strategy supports the format</returns>
        bool SupportsFormat(string format);

        /// <summary>
        /// Validates if the strategy can be used with the given configuration.
        /// </summary>
        /// <param name="context">Versioning context</param>
        /// <returns>Validation result with error message if invalid</returns>
        (bool IsValid, string? ErrorMessage) Validate(VersioningContext context);
    }

    /// <summary>
    /// Context for version calculation containing all necessary information.
    /// </summary>
    public class VersioningContext
    {
        public string GitPath { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string WorkingFolder { get; set; } = string.Empty;
        public List<GitLogEntry> GitLogEntries { get; set; } = new List<GitLogEntry>();
        public VersioningBaseConfiguration Configuration { get; set; } = new VersioningBaseConfiguration();
        public MajorMinorPatchHotfixModel OverrideModel { get; set; } = new MajorMinorPatchHotfixModel();
        public CustomProjectSettings CustomProjectSettings { get; set; } = new CustomProjectSettings();
        public string PrereleaseSuffix { get; set; } = string.Empty;
        public string DefinedPatch { get; set; } = string.Empty;
        public bool CalculateMonoMode { get; set; }
        public string BuildLabel { get; set; } = string.Empty;
        public TimeModel? TimeFrameModel { get; set; }
    }

    /// <summary>
    /// Result of version calculation containing all version components.
    /// </summary>
    public class VersionCalculationResult
    {
        public string AssemblyVersion { get; set; } = string.Empty;
        public string AssemblyFileVersion { get; set; } = string.Empty;
        public string AssemblyInformationalVersion { get; set; } = string.Empty;
        public string BuildLabel { get; set; } = string.Empty;
        public string GitHash { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        // Version components for pattern formatting
        public string Pos0 { get; set; } = string.Empty; // Major
        public string Pos1 { get; set; } = string.Empty; // Minor
        public string Pos2 { get; set; } = string.Empty; // Patch/Hotfix
        public string Pos3 { get; set; } = string.Empty; // ShortHash
        public string Pos4 { get; set; } = string.Empty; // LongHash
        public string Pos5 { get; set; } = string.Empty; // DayOfYear or Patch
    }
}

