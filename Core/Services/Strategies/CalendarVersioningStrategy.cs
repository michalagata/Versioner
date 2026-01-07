using System;
using System.Collections.Generic;
using System.Linq;
using AnubisWorks.Tools.Versioner.Helper;
using AnubisWorks.Tools.Versioner.Interfaces;
using AnubisWorks.Tools.Versioner.Model;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Services.Strategies
{
    /// <summary>
    /// Calendar Versioning strategy.
    /// Uses YYYY.MM.DD format based on current date.
    /// </summary>
    public class CalendarVersioningStrategy : IVersioningStrategy
    {
        private readonly ILogger _logger;
        private readonly IGitLogService _gitLogService;

        public string Name => "Calendar";
        public string Description => "Calendar versioning using YYYY.MM.DD format based on current date";

        public CalendarVersioningStrategy(ILogger logger, IGitLogService gitLogService)
        {
            _logger = logger;
            _gitLogService = gitLogService;
        }

        public VersionCalculationResult CalculateVersion(VersioningContext context)
        {
            _logger.Debug("Calculating version using Calendar Versioning strategy");

            var now = DateTime.Now;
            var lastEntry = context.GitLogEntries?.FirstOrDefault();
            if (lastEntry != null)
            {
                now = lastEntry.Date;
            }

            // Calendar versioning: YYYY.MM.DD.BuildNumber
            string pos0 = now.Year.ToString();
            string pos1 = now.Month.ToString("00");
            string pos2 = now.Day.ToString("00");
            string pos3 = lastEntry?.ShortHash ?? "0000000";
            string pos4 = lastEntry?.LongHash ?? "0000000000000000000000000000000000000000";
            string pos5 = context.GitLogEntries?.Count.ToString() ?? "0";

            // Apply overrides if provided
            if (context.OverrideModel.Major != 0)
            {
                pos0 = context.OverrideModel.Major.ToString();
            }
            if (context.OverrideModel.Minor != 0)
            {
                pos1 = context.OverrideModel.Minor.ToString("00");
            }
            if (context.OverrideModel.Patch != 0)
            {
                pos2 = context.OverrideModel.Patch.ToString("00");
            }
            if (!string.IsNullOrEmpty(context.DefinedPatch))
            {
                pos5 = context.DefinedPatch;
            }

            // Format: YYYY.MM.DD.BuildNumber
            string version = $"{pos0}.{pos1}.{pos2}.{pos5}";
            string versionWithHash = $"{version}+{pos3}";

            return new VersionCalculationResult
            {
                AssemblyVersion = version,
                AssemblyFileVersion = version,
                AssemblyInformationalVersion = versionWithHash,
                BuildLabel = context.BuildLabel,
                GitHash = pos4,
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
            // Calendar versioning supports all formats
            return true;
        }

        public (bool IsValid, string? ErrorMessage) Validate(VersioningContext context)
        {
            // Calendar versioning doesn't require Git log entries, but it's better with them
            return (true, null);
        }
    }
}

