using System;
using System.Collections.Generic;
using System.Linq;
using AnubisWorks.Tools.Versioner.Model;
using Newtonsoft.Json.Linq;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Services
{
    /// <summary>
    /// Validates configuration files and provides detailed error messages.
    /// </summary>
    public class ConfigurationValidator
    {
        private readonly ILogger _logger;

        public ConfigurationValidator(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates a configuration JSON string.
        /// </summary>
        /// <param name="configurationJson">The configuration JSON to validate</param>
        /// <returns>Validation result with errors and warnings</returns>
        public ConfigurationValidationResult Validate(string configurationJson)
        {
            var result = new ConfigurationValidationResult();

            if (string.IsNullOrWhiteSpace(configurationJson))
            {
                result.IsValid = true; // Empty config is valid (will use defaults)
                return result;
            }

            try
            {
                var config = JObject.Parse(configurationJson);
                
                // Validate structure
                ValidateStructure(config, result);
                
                // Validate Default section
                if (config.ContainsKey("Default"))
                {
                    ValidateVersioningConfiguration(config["Default"], "Default", result);
                }

                // Validate Items array
                if (config.ContainsKey("Items"))
                {
                    var items = config["Items"] as JArray;
                    if (items != null)
                    {
                        for (int i = 0; i < items.Count; i++)
                        {
                            ValidateVersioningConfiguration(items[i], $"Items[{i}]", result);
                        }
                    }
                }

                result.IsValid = !result.Errors.Any();
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Invalid JSON format: {ex.Message}");
                result.IsValid = false;
            }

            return result;
        }

        private void ValidateStructure(JObject config, ConfigurationValidationResult result)
        {
            var allowedKeys = new HashSet<string> { "Default", "Items" };
            var configKeys = config.Properties().Select(p => p.Name).ToList();

            foreach (var key in configKeys)
            {
                if (!allowedKeys.Contains(key))
                {
                    result.Warnings.Add($"Unknown configuration key: '{key}'. Allowed keys: Default, Items");
                }
            }
        }

        private void ValidateVersioningConfiguration(JToken config, string path, ConfigurationValidationResult result)
        {
            if (config == null || config.Type != JTokenType.Object)
            {
                result.Errors.Add($"{path}: Expected object");
                return;
            }

            var obj = config as JObject;
            if (obj == null) return;

            // Validate format strings
            ValidateFormatString(obj, "AssemblyVersionFormat", path, result);
            ValidateFormatString(obj, "AssemblyFileVersionFormat", path, result);
            ValidateFormatString(obj, "AssemblyInfoVersionFormat", path, result);

            // Validate boolean values
            ValidateBoolean(obj, "AssemblyVersionSet", path, result);
            ValidateBoolean(obj, "AssemblyFileVersionSet", path, result);
            ValidateBoolean(obj, "AssemblyInfoVersionSet", path, result);
            ValidateBoolean(obj, "HashAsDescription", path, result);

            // Validate file paths
            if (obj.ContainsKey("AssemblyInfoFile"))
            {
                var filePath = obj["AssemblyInfoFile"]?.ToString();
                if (!string.IsNullOrWhiteSpace(filePath))
                {
                    try
                    {
                        // Try to validate path - this is platform-dependent
                        var invalidChars = System.IO.Path.GetInvalidPathChars();
                        if (invalidChars != null && invalidChars.Length > 0)
                        {
                            if (filePath.IndexOfAny(invalidChars) >= 0)
                            {
                                result.Errors.Add($"{path}.AssemblyInfoFile: Contains invalid path characters");
                            }
                        }
                        else
                        {
                            // On some platforms, invalid chars array may be empty
                            // Check for common invalid characters manually
                            var commonInvalidChars = new[] { '<', '>', '|', '?', '*' };
                            if (filePath.IndexOfAny(commonInvalidChars) >= 0)
                            {
                                result.Warnings.Add($"{path}.AssemblyInfoFile: Contains potentially invalid characters (<, >, |, ?, *)");
                            }
                        }
                    }
                    catch
                    {
                        // Path validation may fail on some platforms, just warn
                        result.Warnings.Add($"{path}.AssemblyInfoFile: Path validation skipped (platform-dependent)");
                    }
                }
            }
        }

        private void ValidateFormatString(JObject obj, string key, string path, ConfigurationValidationResult result)
        {
            if (!obj.ContainsKey(key)) return;

            var format = obj[key]?.ToString();
            if (string.IsNullOrWhiteSpace(format))
            {
                result.Warnings.Add($"{path}.{key}: Format string is empty");
                return;
            }

            // Check for common format string issues
            if (!format.Contains("{0}") && !format.Contains("{1}") && !format.Contains("{2}"))
            {
                result.Warnings.Add($"{path}.{key}: Format string may not contain version placeholders ({{0}}, {{1}}, {{2}}, etc.)");
            }
        }

        private void ValidateBoolean(JObject obj, string key, string path, ConfigurationValidationResult result)
        {
            if (!obj.ContainsKey(key)) return;

            var value = obj[key];
            if (value.Type != JTokenType.Boolean)
            {
                result.Errors.Add($"{path}.{key}: Expected boolean value, got {value.Type}");
            }
        }
    }

    /// <summary>
    /// Result of configuration validation.
    /// </summary>
    public class ConfigurationValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();

        public bool HasErrors => Errors.Any();
        public bool HasWarnings => Warnings.Any();
    }
}

