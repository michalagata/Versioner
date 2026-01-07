using System;
using System.Collections.Generic;
using System.Linq;
using AnubisWorks.Tools.Versioner.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Services
{
    /// <summary>
    /// Merges multiple configuration sources in priority order.
    /// </summary>
    public class ConfigurationMerger
    {
        private readonly ILogger _logger;

        public ConfigurationMerger(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Merges configurations in priority order (higher priority overrides lower).
        /// Priority order: 1. ConfigurationFile (highest), 2. ProjectOverride, 3. CustomConfigurationFile, 4. Defaults (lowest)
        /// </summary>
        public string MergeConfigurations(
            string configurationFileJson,
            string projectOverrideJson,
            string customConfigurationJson,
            string defaultJson)
        {
            var merged = new JObject();

            // Start with defaults (lowest priority)
            if (!string.IsNullOrWhiteSpace(defaultJson))
            {
                try
                {
                    var defaults = JObject.Parse(defaultJson);
                    MergeInto(merged, defaults, "defaults");
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to parse default configuration");
                }
            }

            // Apply custom configuration (medium priority)
            if (!string.IsNullOrWhiteSpace(customConfigurationJson))
            {
                try
                {
                    var custom = JObject.Parse(customConfigurationJson);
                    MergeInto(merged, custom, "custom");
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to parse custom configuration");
                }
            }

            // Apply project override (higher priority - only version components)
            if (!string.IsNullOrWhiteSpace(projectOverrideJson))
            {
                try
                {
                    var projectOverride = JObject.Parse(projectOverrideJson);
                    // ProjectOverride only affects version components, not format settings
                    // This is handled separately in version calculation
                    _logger.Debug("ProjectOverride loaded (affects version components only)");
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to parse project override");
                }
            }

            // Apply configuration file (highest priority)
            if (!string.IsNullOrWhiteSpace(configurationFileJson))
            {
                try
                {
                    var configFile = JObject.Parse(configurationFileJson);
                    MergeInto(merged, configFile, "configurationFile");
                    _logger.Information("ConfigurationFile merged (highest priority)");
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "Failed to parse configuration file");
                }
            }

            return merged.ToString(Formatting.Indented);
        }

        private void MergeInto(JObject target, JObject source, string sourceName)
        {
            foreach (var property in source.Properties())
            {
                if (target.ContainsKey(property.Name))
                {
                    // Merge nested objects
                    if (property.Value.Type == JTokenType.Object && target[property.Name].Type == JTokenType.Object)
                    {
                        var targetObj = target[property.Name] as JObject;
                        var sourceObj = property.Value as JObject;
                        if (targetObj != null && sourceObj != null)
                        {
                            MergeInto(targetObj, sourceObj, sourceName);
                            continue;
                        }
                    }

                    // Replace arrays
                    if (property.Value.Type == JTokenType.Array)
                    {
                        target[property.Name] = property.Value.DeepClone();
                        _logger.Debug("Merged {key} from {source} (replaced array)", property.Name, sourceName);
                        continue;
                    }
                }

                // Add or replace property
                target[property.Name] = property.Value.DeepClone();
                _logger.Debug("Merged {key} from {source}", property.Name, sourceName);
            }
        }

        /// <summary>
        /// Creates a default configuration JSON.
        /// </summary>
        public string CreateDefaultConfiguration()
        {
            var defaultConfig = new VersioningBaseConfiguration
            {
                AssemblyVersionSet = true,
                AssemblyFileVersionSet = true,
                AssemblyInfoVersionSet = true,
                AssemblyVersionFormat = "*.{0:00}{1:00}.{5}.{2}",
                AssemblyFileVersionFormat = "*.{0:00}{1:00}.{5}.{2}",
                AssemblyInfoVersionFormat = "*.{0:00}{1:00}.{5}.{2}+{3}",
                HashAsDescription = false
            };

            var config = new Configuration
            {
                Default = defaultConfig,
                Items = new List<VersioningBaseConfiguration>()
            };

            return JsonConvert.SerializeObject(config, Formatting.Indented);
        }
    }
}

