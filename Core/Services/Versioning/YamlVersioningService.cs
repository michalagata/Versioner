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
    public interface IYamlVersioningService
    {
        void VersionYamlFile(string filePath, string version, string buildLabel, string gitHash);
        string? GetCurrentVersion(string filePath);
    }

    public class YamlVersioningService : IYamlVersioningService
    {
        private readonly ILogger _logger;
        private readonly IFileOperations _fileOperations;

        public YamlVersioningService(ILogger logger, IFileOperations fileOperations)
        {
            _logger = logger;
            _fileOperations = fileOperations;
        }

        public void VersionYamlFile(string filePath, string version, string buildLabel, string gitHash)
        {
            _logger.Information("Versioning YAML file: {file}", filePath);

            var fileName = Path.GetFileName(filePath).ToLowerInvariant();
            
            if (fileName == "chart.yaml")
            {
                VersionHelmChart(filePath, version);
            }
            else if (fileName == "docker-compose.yml" || fileName == "compose.yml")
            {
                VersionDockerCompose(filePath, version);
            }
            else
            {
                // Generic YAML versioning
                VersionGenericYaml(filePath, version);
            }
        }

        public string? GetCurrentVersion(string filePath)
        {
            try
            {
                var content = _fileOperations.ReadFileContent(filePath);
                var fileName = Path.GetFileName(filePath).ToLowerInvariant();

                if (fileName == "chart.yaml")
                {
                    return ExtractVersionFromHelmChart(content);
                }
                else if (fileName == "docker-compose.yml" || fileName == "compose.yml")
                {
                    return ExtractVersionFromDockerCompose(content);
                }
                else
                {
                    return ExtractVersionFromGenericYaml(content);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to get current version from {file}", filePath);
            }

            return null;
        }

        private void VersionHelmChart(string filePath, string version)
        {
            var content = _fileOperations.ReadFileContent(filePath);
            
            // Update version in Chart.yaml
            var pattern = @"(^version:\s*)([^\r\n]+)";
            var replacement = $"$1{version}";
            
            if (Regex.IsMatch(content, pattern, RegexOptions.Multiline))
            {
                content = Regex.Replace(content, pattern, replacement, RegexOptions.Multiline);
            }
            else
            {
                // Add version if it doesn't exist
                content = $"version: {version}{Environment.NewLine}{content}";
            }

            _fileOperations.WriteFileContent(filePath, content);
            _logger.Debug("Updated version in Chart.yaml to {version}", version);
        }

        private void VersionDockerCompose(string filePath, string version)
        {
            var content = _fileOperations.ReadFileContent(filePath);
            
            // Update version in docker-compose.yml (usually in top-level version field or image tags)
            var pattern = @"(^version:\s*[""']?)([^""'\r\n]+)([""']?)";
            var replacement = $"$1{version}$3";
            
            if (Regex.IsMatch(content, pattern, RegexOptions.Multiline))
            {
                content = Regex.Replace(content, pattern, replacement, RegexOptions.Multiline);
            }

            _fileOperations.WriteFileContent(filePath, content);
            _logger.Debug("Updated version in docker-compose.yml to {version}", version);
        }

        private void VersionGenericYaml(string filePath, string version)
        {
            var content = _fileOperations.ReadFileContent(filePath);
            
            // Try to find and update version field
            var pattern = @"(^version:\s*[""']?)([^""'\r\n]+)([""']?)";
            var replacement = $"$1{version}$3";
            
            if (Regex.IsMatch(content, pattern, RegexOptions.Multiline))
            {
                content = Regex.Replace(content, pattern, replacement, RegexOptions.Multiline);
                _fileOperations.WriteFileContent(filePath, content);
                _logger.Debug("Updated version in YAML file to {version}", version);
            }
            else
            {
                _logger.Warning("No version field found in YAML file: {file}", filePath);
            }
        }

        private string? ExtractVersionFromHelmChart(string content)
        {
            var match = Regex.Match(content, @"^version:\s*([^\r\n]+)", RegexOptions.Multiline);
            return match.Success ? match.Groups[1].Value.Trim().Trim('"', '\'') : null;
        }

        private string? ExtractVersionFromDockerCompose(string content)
        {
            var match = Regex.Match(content, @"^version:\s*[""']?([^""'\r\n]+)[""']?", RegexOptions.Multiline);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private string? ExtractVersionFromGenericYaml(string content)
        {
            var match = Regex.Match(content, @"^version:\s*[""']?([^""'\r\n]+)[""']?", RegexOptions.Multiline);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }
    }
}

