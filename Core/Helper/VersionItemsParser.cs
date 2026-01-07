using System;
using System.Collections.Generic;
using System.Linq;

namespace AnubisWorks.Tools.Versioner.Helper
{
    /// <summary>
    /// Parser for --versionItems parameter.
    /// Validates and parses comma-separated list of artifact types.
    /// </summary>
    public static class VersionItemsParser
    {
        private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "dotnet", "props", "nuget", "npm", "docker",
            "python", "go", "rust", "java", "yaml", "helm"
        };

        /// <summary>
        /// Parses and validates versionItems parameter.
        /// </summary>
        /// <param name="versionItems">Comma-separated list of artifact types</param>
        /// <returns>Tuple with validation result, list of types, and error message if invalid</returns>
        public static (bool IsValid, List<string> Types, string? ErrorMessage) Parse(string? versionItems)
        {
            if (string.IsNullOrWhiteSpace(versionItems))
            {
                return (IsValid: true, Types: new List<string>(), ErrorMessage: null);
            }

            var items = versionItems.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var types = new List<string>();
            var invalidTypes = new List<string>();

            foreach (var item in items)
            {
                var normalized = item.ToLowerInvariant();
                if (ValidTypes.Contains(normalized))
                {
                    types.Add(normalized);
                }
                else
                {
                    invalidTypes.Add(item);
                }
            }

            if (invalidTypes.Count > 0)
            {
                return (IsValid: false, Types: new List<string>(),
                    ErrorMessage: $"Invalid artifact types: {string.Join(", ", invalidTypes)}. Valid types: {string.Join(", ", ValidTypes.OrderBy(x => x))}");
            }

            return (IsValid: true, Types: types, ErrorMessage: null);
        }

        /// <summary>
        /// Gets all valid artifact type names.
        /// </summary>
        public static IEnumerable<string> GetValidTypes()
        {
            return ValidTypes.OrderBy(x => x);
        }
    }
}

