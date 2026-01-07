using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AnubisWorks.Tools.Versioner.Interfaces;
using AnubisWorks.Tools.Versioner.Model;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Services.Versioning
{
    public interface IJavaVersioningService
    {
        void VersionJavaProject(string filePath, string version, string buildLabel, string gitHash);
        string? GetCurrentVersion(string filePath);
    }

    public class JavaVersioningService : IJavaVersioningService
    {
        private readonly ILogger _logger;
        private readonly IFileOperations _fileOperations;

        public JavaVersioningService(ILogger logger, IFileOperations fileOperations)
        {
            _logger = logger;
            _fileOperations = fileOperations;
        }

        public void VersionJavaProject(string filePath, string version, string buildLabel, string gitHash)
        {
            _logger.Information("Versioning Java/Maven project: {file}", filePath);
            VersionPomXml(filePath, version);
        }

        public string? GetCurrentVersion(string filePath)
        {
            try
            {
                var content = _fileOperations.ReadFileContent(filePath);
                return ExtractVersionFromPomXml(content);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to get current version from {file}", filePath);
            }

            return null;
        }

        private void VersionPomXml(string filePath, string version)
        {
            try
            {
                var content = _fileOperations.ReadFileContent(filePath);
                var doc = XDocument.Parse(content);
                var ns = XNamespace.Get("http://maven.apache.org/POM/4.0.0");
                
                var project = doc.Element(ns + "project");
                if (project == null)
                {
                    // Try without namespace
                    project = doc.Element("project");
                    ns = XNamespace.None;
                }

                if (project != null)
                {
                    var versionElement = project.Element(ns + "version");
                    if (versionElement != null)
                    {
                        versionElement.Value = version;
                    }
                    else
                    {
                        // Add version element
                        project.Add(new XElement(ns + "version", version));
                    }

                    _fileOperations.WriteFileContent(filePath, doc.ToString());
                    _logger.Debug("Updated version in pom.xml to {version}", version);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to version pom.xml file: {file}", filePath);
                throw;
            }
        }

        private string? ExtractVersionFromPomXml(string content)
        {
            try
            {
                var doc = XDocument.Parse(content);
                var ns = XNamespace.Get("http://maven.apache.org/POM/4.0.0");
                
                var project = doc.Element(ns + "project");
                if (project == null)
                {
                    project = doc.Element("project");
                    ns = XNamespace.None;
                }

                var versionElement = project?.Element(ns + "version");
                return versionElement?.Value;
            }
            catch
            {
                // Fallback to regex if XML parsing fails
                var match = Regex.Match(content, @"<version>([^<]+)</version>");
                return match.Success ? match.Groups[1].Value.Trim() : null;
            }
        }
    }
}

