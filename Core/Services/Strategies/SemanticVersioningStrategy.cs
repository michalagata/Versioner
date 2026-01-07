using System;
using System.Collections.Generic;
using System.Linq;
using AnubisWorks.Tools.Versioner.Helper;
using AnubisWorks.Tools.Versioner.Infrastructure.Services;
using AnubisWorks.Tools.Versioner.Interfaces;
using AnubisWorks.Tools.Versioner.Model;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Services.Strategies
{
    /// <summary>
    /// Default Semantic Versioning strategy (current implementation).
    /// Calculates version based on date (year, month) and Git commit history.
    /// </summary>
    public class SemanticVersioningStrategy : IVersioningStrategy
    {
        private readonly ILogger _logger;
        private readonly IGitLogService _gitLogService;

        public string Name => "Semantic";
        public string Description => "Semantic versioning based on date (year, month) and Git commit history";

        public SemanticVersioningStrategy(ILogger logger, IGitLogService gitLogService)
        {
            _logger = logger;
            _gitLogService = gitLogService;
        }

        public VersionCalculationResult CalculateVersion(VersioningContext context)
        {
            _logger.Debug("Calculating version using Semantic Versioning strategy");

            if (context.GitLogEntries == null || context.GitLogEntries.Count == 0)
            {
                _logger.Error("No Git log entries available for version calculation");
                throw new InvalidOperationException("Git log entries are required for version calculation");
            }

            var lastEntry = _gitLogService.GetLatestLogEntry(context.GitLogEntries);
            var timeModel = context.TimeFrameModel ?? 
                State.TimeFrameConfiguration.GetTimeFrameConfig(lastEntry.Date.Date);

            // Apply overrides
            if (context.OverrideModel.Major != 0)
            {
                if (context.OverrideModel.Major != 0) timeModel.Version.Major = context.OverrideModel.Major;
                if (context.OverrideModel.Minor != 0) timeModel.Version.Minor = context.OverrideModel.Minor;
                if (context.OverrideModel.Patch != 0) timeModel.Version.Patch = context.OverrideModel.Patch;
                if (context.OverrideModel.Hotfix != 0) timeModel.Version.Hotfix = context.OverrideModel.Hotfix;
                timeModel.OverrideWithLocalFile = true;
            }

            // Prepare version components
            string dayOfTheYear = lastEntry.Date.DayOfYear.ToString();
            string pos0 = timeModel.Version.Major.ToString();
            string pos1 = timeModel.OverrideWithLocalFile
                ? timeModel.Version.Minor.ToString()
                : timeModel.Version.Minor.ToString().EnsurePropperDateMonthsFormat();
            string pos2 = context.GitLogEntries.Count.ToString();

            if (!string.IsNullOrEmpty(context.DefinedPatch))
            {
                pos2 = int.Parse(context.DefinedPatch).ToString();
            }

            string pos3 = lastEntry.ShortHash;
            string pos4 = lastEntry.LongHash;
            string pos5 = dayOfTheYear;

            if (pos0 == "0") pos0 = "1";

            if (timeModel.OverrideWithLocalFile)
            {
                if (timeModel.Version.Patch != 0) pos5 = timeModel.Version.Patch.ToString();
                if (timeModel.Version.Hotfix != 0) pos2 = timeModel.Version.Hotfix.ToString();
            }

            // Generate version strings using patterns
            var versionPatternService = new VersionPatternService();
            string assemblyVersionPattern = versionPatternService.GenerateAssemblyVersionPattern(
                context.Configuration, timeModel, pos0, pos1, pos2, pos3, pos4, pos5);
            string assemblyInfoVersionPattern = versionPatternService.GenerateAssemblyInfoVersionPattern(
                context.Configuration, timeModel, pos0, pos1, pos2, pos3, pos4, pos5);
            string assemblyFileVersionPattern = versionPatternService.GenerateAssemblyFileVersionPattern(
                context.Configuration, timeModel, pos0, pos1, pos2, pos3, pos4, pos5);

            return new VersionCalculationResult
            {
                AssemblyVersion = assemblyVersionPattern,
                AssemblyFileVersion = assemblyFileVersionPattern,
                AssemblyInformationalVersion = assemblyInfoVersionPattern,
                BuildLabel = context.BuildLabel,
                GitHash = lastEntry.LongHash,
                Description = $"Version: {context.BuildLabel}",
                Pos0 = pos0,
                Pos1 = pos1,
                Pos2 = pos2,
                Pos3 = pos3,
                Pos4 = pos4,
                Pos5 = pos5
            };
        }

        public bool SupportsFormat(string format)
        {
            // Semantic versioning supports all formats
            return true;
        }

        public (bool IsValid, string? ErrorMessage) Validate(VersioningContext context)
        {
            if (context == null)
            {
                return (false, "VersioningContext cannot be null");
            }

            if (context.GitLogEntries == null || context.GitLogEntries.Count == 0)
            {
                return (false, "Git log entries are required for Semantic Versioning strategy");
            }

            return (true, null);
        }
    }
}

