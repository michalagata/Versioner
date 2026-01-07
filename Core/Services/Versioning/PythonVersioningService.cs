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
    public interface IPythonVersioningService
    {
        void VersionPythonProject(string filePath, string version, string buildLabel, string gitHash);
        string? GetCurrentVersion(string filePath);
    }

    public class PythonVersioningService : IPythonVersioningService
    {
        private readonly ILogger _logger;
        private readonly IFileOperations _fileOperations;

        public PythonVersioningService(ILogger logger, IFileOperations fileOperations)
        {
            _logger = logger;
            _fileOperations = fileOperations;
        }

        public void VersionPythonProject(string filePath, string version, string buildLabel, string gitHash)
        {
            _logger.Information("Versioning Python project: {file}", filePath);

            var fileName = Path.GetFileName(filePath).ToLowerInvariant();
            
            if (fileName == "pyproject.toml")
            {
                VersionPyProjectToml(filePath, version);
            }
            else if (fileName == "setup.py")
            {
                VersionSetupPy(filePath, version);
            }
            else if (fileName == "setup.cfg")
            {
                VersionSetupCfg(filePath, version);
            }
            else if (fileName == "__version__.py")
            {
                VersionVersionPy(filePath, version);
            }
        }

        public string? GetCurrentVersion(string filePath)
        {
            try
            {
                var content = _fileOperations.ReadFileContent(filePath);
                var fileName = Path.GetFileName(filePath).ToLowerInvariant();

                if (fileName == "pyproject.toml")
                {
                    return ExtractVersionFromToml(content);
                }
                else if (fileName == "setup.py")
                {
                    return ExtractVersionFromSetupPy(content);
                }
                else if (fileName == "setup.cfg")
                {
                    return ExtractVersionFromSetupCfg(content);
                }
                else if (fileName == "__version__.py")
                {
                    return ExtractVersionFromVersionPy(content);
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to get current version from {file}", filePath);
            }

            return null;
        }

        private void VersionPyProjectToml(string filePath, string version)
        {
            var content = _fileOperations.ReadFileContent(filePath);
            
            // Update version in [project] section
            var pattern = @"(\[project\]\s+.*?version\s*=\s*[""'])([^""']+)([""'])";
            var replacement = $"$1{version}$3";
            
            if (Regex.IsMatch(content, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                content = Regex.Replace(content, pattern, replacement, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            }
            else
            {
                // Add version if [project] section exists but no version
                if (content.Contains("[project]"))
                {
                    content = Regex.Replace(content, @"(\[project\])", $@"$1{Environment.NewLine}version = ""{version}""", RegexOptions.IgnoreCase);
                }
            }

            _fileOperations.WriteFileContent(filePath, content);
            _logger.Debug("Updated version in pyproject.toml to {version}", version);
        }

        private void VersionSetupPy(string filePath, string version)
        {
            var content = _fileOperations.ReadFileContent(filePath);
            
            // Update version in setup() call
            var pattern = @"(version\s*=\s*[""'])([^""']+)([""'])";
            var replacement = $"$1{version}$3";
            
            if (Regex.IsMatch(content, pattern))
            {
                content = Regex.Replace(content, pattern, replacement);
            }
            else
            {
                // Add version parameter if setup() exists but no version
                if (content.Contains("setup("))
                {
                    content = Regex.Replace(content, @"(setup\()", $@"$1{Environment.NewLine}    version=""{version}"",", RegexOptions.Multiline);
                }
            }

            _fileOperations.WriteFileContent(filePath, content);
            _logger.Debug("Updated version in setup.py to {version}", version);
        }

        private void VersionSetupCfg(string filePath, string version)
        {
            var content = _fileOperations.ReadFileContent(filePath);
            
            // Update version in [metadata] section
            var pattern = @"(\[metadata\]\s+.*?version\s*=\s*)([^\r\n]+)";
            var replacement = $"$1{version}";
            
            if (Regex.IsMatch(content, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase))
            {
                content = Regex.Replace(content, pattern, replacement, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            }
            else
            {
                // Add version if [metadata] section exists but no version
                if (content.Contains("[metadata]"))
                {
                    content = Regex.Replace(content, @"(\[metadata\])", $@"$1{Environment.NewLine}version = {version}", RegexOptions.IgnoreCase);
                }
            }

            _fileOperations.WriteFileContent(filePath, content);
            _logger.Debug("Updated version in setup.cfg to {version}", version);
        }

        private void VersionVersionPy(string filePath, string version)
        {
            var content = _fileOperations.ReadFileContent(filePath);
            
            // Update __version__ variable
            var pattern = @"(__version__\s*=\s*[""'])([^""']+)([""'])";
            var replacement = $"$1{version}$3";
            
            if (Regex.IsMatch(content, pattern))
            {
                content = Regex.Replace(content, pattern, replacement);
            }
            else
            {
                // Add __version__ if it doesn't exist
                content = $@"__version__ = ""{version}""{Environment.NewLine}{content}";
            }

            _fileOperations.WriteFileContent(filePath, content);
            _logger.Debug("Updated version in __version__.py to {version}", version);
        }

        private string? ExtractVersionFromToml(string content)
        {
            var match = Regex.Match(content, @"\[project\]\s+.*?version\s*=\s*[""']([^""']+)[""']", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : null;
        }

        private string? ExtractVersionFromSetupPy(string content)
        {
            var match = Regex.Match(content, @"version\s*=\s*[""']([^""']+)[""']");
            return match.Success ? match.Groups[1].Value : null;
        }

        private string? ExtractVersionFromSetupCfg(string content)
        {
            var match = Regex.Match(content, @"\[metadata\]\s+.*?version\s*=\s*([^\r\n]+)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private string? ExtractVersionFromVersionPy(string content)
        {
            var match = Regex.Match(content, @"__version__\s*=\s*[""']([^""']+)[""']");
            return match.Success ? match.Groups[1].Value : null;
        }
    }
}

