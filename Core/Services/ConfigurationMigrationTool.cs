using System;
using System.IO;
using AnubisWorks.Tools.Versioner.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Services
{
    /// <summary>
    /// Tool for migrating configuration files between versions.
    /// </summary>
    public class ConfigurationMigrationTool
    {
        private readonly ILogger _logger;
        private readonly IFileOperations _fileOperations;

        public ConfigurationMigrationTool(ILogger logger, IFileOperations fileOperations)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileOperations = fileOperations ?? throw new ArgumentNullException(nameof(fileOperations));
        }

        /// <summary>
        /// Migrates a configuration file to the latest format.
        /// </summary>
        /// <param name="configFilePath">Path to the configuration file to migrate</param>
        /// <param name="backupOriginal">Whether to create a backup of the original file</param>
        /// <returns>True if migration was successful</returns>
        public bool MigrateConfiguration(string configFilePath, bool backupOriginal = true)
        {
            if (!_fileOperations.FileExists(configFilePath))
            {
                _logger.Error("Configuration file not found: {path}", configFilePath);
                return false;
            }

            try
            {
                var originalContent = _fileOperations.ReadFileContent(configFilePath);
                var migratedContent = MigrateConfigurationContent(originalContent);

                // Create backup if requested
                if (backupOriginal)
                {
                    var backupPath = $"{configFilePath}.backup.{DateTime.Now:yyyyMMddHHmmss}";
                    _fileOperations.WriteFileContent(backupPath, originalContent);
                    _logger.Information("Backup created: {backup}", backupPath);
                }

                // Write migrated content
                _fileOperations.WriteFileContent(configFilePath, migratedContent);
                _logger.Information("Configuration migrated successfully: {path}", configFilePath);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to migrate configuration file: {path}", configFilePath);
                return false;
            }
        }

        /// <summary>
        /// Migrates configuration JSON content to the latest format.
        /// </summary>
        private string MigrateConfigurationContent(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent) || jsonContent.Trim() == "{}")
            {
                return jsonContent;
            }

            try
            {
                var config = JObject.Parse(jsonContent);
                var migrated = new JObject();

                // Migrate Default section
                if (config.ContainsKey("Default"))
                {
                    migrated["Default"] = MigrateVersioningConfiguration(config["Default"]);
                }

                // Migrate Items array
                if (config.ContainsKey("Items"))
                {
                    var items = config["Items"] as JArray;
                    if (items != null)
                    {
                        var migratedItems = new JArray();
                        foreach (var item in items)
                        {
                            migratedItems.Add(MigrateVersioningConfiguration(item));
                        }
                        migrated["Items"] = migratedItems;
                    }
                }

                return migrated.ToString(Formatting.Indented);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to parse configuration during migration, returning original");
                return jsonContent;
            }
        }

        /// <summary>
        /// Migrates a single versioning configuration object.
        /// </summary>
        private JObject MigrateVersioningConfiguration(JToken config)
        {
            var migrated = new JObject();

            if (config == null || config.Type != JTokenType.Object)
            {
                return migrated;
            }

            var obj = config as JObject;
            if (obj == null) return migrated;

            // Copy all existing properties
            foreach (var property in obj.Properties())
            {
                migrated[property.Name] = property.Value.DeepClone();
            }

            // Add missing default properties if needed
            var defaults = new
            {
                AssemblyVersionSet = true,
                AssemblyFileVersionSet = true,
                AssemblyInfoVersionSet = true,
                AssemblyVersionFormat = "*.{0:00}{1:00}.{5}.{2}",
                AssemblyFileVersionFormat = "*.{0:00}{1:00}.{5}.{2}",
                AssemblyInfoVersionFormat = "*.{0:00}{1:00}.{5}.{2}+{3}",
                HashAsDescription = false
            };

            if (!migrated.ContainsKey("AssemblyVersionSet"))
                migrated["AssemblyVersionSet"] = defaults.AssemblyVersionSet;
            if (!migrated.ContainsKey("AssemblyFileVersionSet"))
                migrated["AssemblyFileVersionSet"] = defaults.AssemblyFileVersionSet;
            if (!migrated.ContainsKey("AssemblyInfoVersionSet"))
                migrated["AssemblyInfoVersionSet"] = defaults.AssemblyInfoVersionSet;
            if (!migrated.ContainsKey("AssemblyVersionFormat"))
                migrated["AssemblyVersionFormat"] = defaults.AssemblyVersionFormat;
            if (!migrated.ContainsKey("AssemblyFileVersionFormat"))
                migrated["AssemblyFileVersionFormat"] = defaults.AssemblyFileVersionFormat;
            if (!migrated.ContainsKey("AssemblyInfoVersionFormat"))
                migrated["AssemblyInfoVersionFormat"] = defaults.AssemblyInfoVersionFormat;
            if (!migrated.ContainsKey("HashAsDescription"))
                migrated["HashAsDescription"] = defaults.HashAsDescription;

            return migrated;
        }

        /// <summary>
        /// Validates if a configuration file needs migration.
        /// </summary>
        public bool NeedsMigration(string configFilePath)
        {
            if (!_fileOperations.FileExists(configFilePath))
            {
                return false;
            }

            try
            {
                var content = _fileOperations.ReadFileContent(configFilePath);
                var config = JObject.Parse(content);

                // Check for old format indicators
                // Add checks for deprecated properties or missing required properties
                return false; // For now, assume no migration needed
            }
            catch
            {
                return true; // If we can't parse it, it might need migration
            }
        }
    }
}

