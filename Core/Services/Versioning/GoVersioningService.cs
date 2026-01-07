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
    public interface IGoVersioningService
    {
        void VersionGoModule(string filePath, string version, string buildLabel, string gitHash);
        string? GetCurrentVersion(string filePath);
    }

    public class GoVersioningService : IGoVersioningService
    {
        private readonly ILogger _logger;
        private readonly IFileOperations _fileOperations;

        public GoVersioningService(ILogger logger, IFileOperations fileOperations)
        {
            _logger = logger;
            _fileOperations = fileOperations;
        }

        public void VersionGoModule(string filePath, string version, string buildLabel, string gitHash)
        {
            _logger.Information("Versioning Go module: {file}", filePath);

            var fileName = Path.GetFileName(filePath).ToLowerInvariant();
            
            if (fileName == "go.mod")
            {
                VersionGoMod(filePath, version);
            }
            else if (fileName == "version.go")
            {
                VersionVersionGo(filePath, version);
            }
        }

        public string? GetCurrentVersion(string filePath)
        {
            try
            {
                var content = _fileOperations.ReadFileContent(filePath);
                var fileName = Path.GetFileName(filePath).ToLowerInvariant();

                if (fileName == "go.mod")
                {
                    return ExtractVersionFromGoMod(content);
                }
                else if (fileName == "version.go")
                {
                    return ExtractVersionFromVersionGo(content);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to get current version from {file}", filePath);
            }

            return null;
        }

        private void VersionGoMod(string filePath, string version)
        {
            var content = _fileOperations.ReadFileContent(filePath);
            
            // Go modules don't typically store version in go.mod, but we can add a comment
            // Or update module path if it contains version
            var pattern = @"(module\s+[^\s]+)(\s+//\s+version\s+)([^\r\n]+)";
            var replacement = $"$1$2{version}";
            
            if (Regex.IsMatch(content, pattern))
            {
                content = Regex.Replace(content, pattern, replacement);
            }
            else
            {
                // Add version comment after module declaration
                var moduleMatch = Regex.Match(content, @"(module\s+[^\r\n]+)");
                if (moduleMatch.Success)
                {
                    content = content.Replace(moduleMatch.Value, $"{moduleMatch.Value} // version {version}");
                }
            }

            _fileOperations.WriteFileContent(filePath, content);
            _logger.Debug("Updated version comment in go.mod to {version}", version);
        }

        private void VersionVersionGo(string filePath, string version)
        {
            var content = _fileOperations.ReadFileContent(filePath);
            
            // Update Version variable
            var pattern = @"(Version\s*=\s*[""'])([^""']+)([""'])";
            var replacement = $"$1{version}$3";
            
            if (Regex.IsMatch(content, pattern))
            {
                content = Regex.Replace(content, pattern, replacement);
            }
            else
            {
                // Add Version variable if it doesn't exist
                if (!content.Contains("package"))
                {
                    content = $"package main{Environment.NewLine}{content}";
                }
                content = $@"var Version = ""{version}""{Environment.NewLine}{content}";
            }

            _fileOperations.WriteFileContent(filePath, content);
            _logger.Debug("Updated version in version.go to {version}", version);
        }

        private string? ExtractVersionFromGoMod(string content)
        {
            var match = Regex.Match(content, @"module\s+[^\s]+\s+//\s+version\s+([^\r\n]+)");
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private string? ExtractVersionFromVersionGo(string content)
        {
            var match = Regex.Match(content, @"Version\s*=\s*[""']([^""']+)[""']");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}

