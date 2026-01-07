using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using AnubisWorks.Tools.Versioner.Interfaces;
using AnubisWorks.Tools.Versioner.Model;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Services.Versioning
{
    public interface IRustVersioningService
    {
        void VersionRustProject(string filePath, string version, string buildLabel, string gitHash);
        string? GetCurrentVersion(string filePath);
    }

    public class RustVersioningService : IRustVersioningService
    {
        private readonly ILogger _logger;
        private readonly IFileOperations _fileOperations;

        public RustVersioningService(ILogger logger, IFileOperations fileOperations)
        {
            _logger = logger;
            _fileOperations = fileOperations;
        }

        public void VersionRustProject(string filePath, string version, string buildLabel, string gitHash)
        {
            _logger.Information("Versioning Rust project: {file}", filePath);
            VersionCargoToml(filePath, version);
        }

        public string? GetCurrentVersion(string filePath)
        {
            try
            {
                var content = _fileOperations.ReadFileContent(filePath);
                return ExtractVersionFromCargoToml(content);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to get current version from {file}", filePath);
            }

            return null;
        }

        private void VersionCargoToml(string filePath, string version)
        {
            var content = _fileOperations.ReadFileContent(filePath);
            
            // Update version in [package] section
            var pattern = @"(\[package\]\s+.*?version\s*=\s*[""'])([^""']+)([""'])";
            var replacement = $"$1{version}$3";
            
            if (Regex.IsMatch(content, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                content = Regex.Replace(content, pattern, replacement, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            }
            else
            {
                // Add version if [package] section exists but no version
                if (content.Contains("[package]"))
                {
                    content = Regex.Replace(content, @"(\[package\])", $@"$1{Environment.NewLine}version = ""{version}""", RegexOptions.IgnoreCase);
                }
            }

            _fileOperations.WriteFileContent(filePath, content);
            _logger.Debug("Updated version in Cargo.toml to {version}", version);
        }

        private string? ExtractVersionFromCargoToml(string content)
        {
            var match = Regex.Match(content, @"\[package\]\s+.*?version\s*=\s*[""']([^""']+)[""']", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}

