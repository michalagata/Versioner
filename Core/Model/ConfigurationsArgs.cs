using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Serilog.Events;

namespace AnubisWorks.Tools.Versioner.Model
{
    public class ConfigurationsArgs
    {
        const string lVerbose = nameof(LogEventLevel.Verbose);
        const string lDebug = nameof(LogEventLevel.Debug);
        const string lInfo = nameof(LogEventLevel.Information);
        const string lWarning = nameof(LogEventLevel.Warning);
        const string lError = nameof(LogEventLevel.Error);
        const string lFatal = nameof(LogEventLevel.Fatal);
        public LogEventLevel logLevel = LogEventLevel.Debug;

        public bool StoreVersionFile { get; set; }
        public string WorkingFolder { get; set; }
        public bool UseDefaults { get; set; }
        // ConfigurationFile parameter removed - all configuration is now done via command line parameters and defaults
        public string CustomConfigurationFile { get; set; }

        public string LogLevel
        {
            get { return logLevel.ToString(); }
            set
            {
                switch (value)
                {
                    case lVerbose:
                    case "V":
                        logLevel = LogEventLevel.Verbose;

                        break;
                    case lDebug:
                    case "D":
                        logLevel = LogEventLevel.Debug;

                        break;
                    case lInfo:
                    case "I":
                        logLevel = LogEventLevel.Information;

                        break;
                    case lWarning:
                    case "W":
                        logLevel = LogEventLevel.Warning;

                        break;
                    case lError:
                    case "E":
                        logLevel = LogEventLevel.Error;

                        break;
                    case lFatal:
                    case "F":
                        logLevel = LogEventLevel.Fatal;

                        break;
                    default:
                        logLevel = LogEventLevel.Information;

                        break;
                }
            }
        }
        public bool AllSlnLocations { get; set; }
        public bool SetProjectGuid { get; set; }
        public string ProjectsGuidConfiguration { get; set; }
        public string ExactProjectFile { get; set; }
        public bool IsMonoRepo { get; set; } = false;
        public bool DebugMode { get; set; } = false;
        // VersionNuspecsEvenIfOtherFilesExist removed - use VersionItems="nuget" instead
        public string PreReleaseSuffix { get; set; } = string.Empty;
        public string DefinedPatch { get; set; } = string.Empty;
        public bool VersionOnlyProps { get; set; } = true;
        // EnforceGlobalVersioning is now always enabled by default (removed CLI parameter)
        // NpmOnly and NpmExclude removed - replaced by automatic artifact detection
        
        // Webhook support (Phase 3)
        public string WebhookUrl { get; set; } = string.Empty;
        public string WebhookToken { get; set; } = string.Empty;
        
        // Selective versioning (Phase 5)
        public string VersionItems { get; set; } = string.Empty;
    }
}
