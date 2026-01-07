using System;
using System.Linq;
using System.Xml.Linq;
using AnubisWorks.Tools.Versioner.Domain.Interfaces;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Infrastructure.Services
{
    /// <summary>
    /// Service for injecting version properties into project files when they are missing.
    /// </summary>
    public class VersionPropertyInjector : IVersionPropertyInjector
    {
        private readonly ILogger _logger;

        public VersionPropertyInjector(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Ensures that version properties exist in the project file.
        /// If properties are missing, they are added with default values.
        /// </summary>
        public bool EnsureVersionPropertiesExist(XDocument project, ProjectType projectType, string defaultVersion = "1.0.0.0")
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            if (HasVersionProperties(project, projectType))
            {
                _logger.Debug("Version properties already exist in {ProjectType} file", projectType);
                return false;
            }

            _logger.Information("Adding missing version properties to {ProjectType} file with default version {DefaultVersion}", 
                projectType, defaultVersion);

            var projectElement = project.Element("Project");
            if (projectElement == null)
            {
                _logger.Warning("Project element not found in XML document");
                return false;
            }

            // Find or create the first PropertyGroup without conditions
            var propertyGroup = projectElement
                .Elements("PropertyGroup")
                .FirstOrDefault(pg => pg.Attribute("Condition") == null);

            if (propertyGroup == null)
            {
                // Create a new PropertyGroup if none exists
                propertyGroup = new XElement("PropertyGroup");
                projectElement.Add(propertyGroup);
                _logger.Debug("Created new PropertyGroup for version properties");
            }

            // Add version properties based on project type
            switch (projectType)
            {
                case ProjectType.Sdk:
                    AddSdkVersionProperties(propertyGroup, defaultVersion);
                    break;
                case ProjectType.Props:
                    AddPropsVersionProperties(propertyGroup, defaultVersion);
                    break;
                default:
                    _logger.Warning("Unsupported project type for version property injection: {ProjectType}", projectType);
                    return false;
            }

            _logger.Information("Successfully added version properties to {ProjectType} file", projectType);
            return true;
        }

        /// <summary>
        /// Checks if version properties exist in the project file.
        /// </summary>
        public bool HasVersionProperties(XDocument project, ProjectType projectType)
        {
            if (project == null)
                return false;

            var projectElement = project.Element("Project");
            if (projectElement == null)
                return false;

            var propertyGroups = projectElement.Elements("PropertyGroup");
            
            switch (projectType)
            {
                case ProjectType.Sdk:
                    return HasSdkVersionProperties(propertyGroups);
                case ProjectType.Props:
                    return HasPropsVersionProperties(propertyGroups);
                default:
                    return false;
            }
        }

        private void AddSdkVersionProperties(XElement propertyGroup, string defaultVersion)
        {
            // Add Version property (for NuGet package version)
            if (propertyGroup.Element("Version") == null)
            {
                propertyGroup.Add(new XElement("Version", defaultVersion));
                _logger.Debug("Added Version property: {Version}", defaultVersion);
            }

            // Add AssemblyVersion property
            if (propertyGroup.Element("AssemblyVersion") == null)
            {
                propertyGroup.Add(new XElement("AssemblyVersion", defaultVersion));
                _logger.Debug("Added AssemblyVersion property: {AssemblyVersion}", defaultVersion);
            }

            // Add FileVersion property
            if (propertyGroup.Element("FileVersion") == null)
            {
                propertyGroup.Add(new XElement("FileVersion", defaultVersion));
                _logger.Debug("Added FileVersion property: {FileVersion}", defaultVersion);
            }

            // Add AssemblyInformationalVersion property
            if (propertyGroup.Element("AssemblyInformationalVersion") == null)
            {
                propertyGroup.Add(new XElement("AssemblyInformationalVersion", defaultVersion));
                _logger.Debug("Added AssemblyInformationalVersion property: {AssemblyInformationalVersion}", defaultVersion);
            }
        }

        private void AddPropsVersionProperties(XElement propertyGroup, string defaultVersion)
        {
            // Add Version property (for NuGet package version)
            if (propertyGroup.Element("Version") == null)
            {
                propertyGroup.Add(new XElement("Version", defaultVersion));
                _logger.Debug("Added Version property: {Version}", defaultVersion);
            }

            // Add AssemblyVersion property
            if (propertyGroup.Element("AssemblyVersion") == null)
            {
                propertyGroup.Add(new XElement("AssemblyVersion", defaultVersion));
                _logger.Debug("Added AssemblyVersion property: {AssemblyVersion}", defaultVersion);
            }

            // Add FileVersion property
            if (propertyGroup.Element("FileVersion") == null)
            {
                propertyGroup.Add(new XElement("FileVersion", defaultVersion));
                _logger.Debug("Added FileVersion property: {FileVersion}", defaultVersion);
            }

            // Add AssemblyInformationalVersion property
            if (propertyGroup.Element("AssemblyInformationalVersion") == null)
            {
                propertyGroup.Add(new XElement("AssemblyInformationalVersion", defaultVersion));
                _logger.Debug("Added AssemblyInformationalVersion property: {AssemblyInformationalVersion}", defaultVersion);
            }
        }

        private bool HasSdkVersionProperties(System.Collections.Generic.IEnumerable<XElement> propertyGroups)
        {
            foreach (var pg in propertyGroups)
            {
                // Skip conditional PropertyGroups
                if (pg.Attribute("Condition") != null)
                    continue;

                // Check for at least one version property
                if (pg.Element("Version") != null ||
                    pg.Element("AssemblyVersion") != null ||
                    pg.Element("FileVersion") != null ||
                    pg.Element("AssemblyInformationalVersion") != null ||
                    (pg.Element("VersionPrefix") != null && pg.Element("VersionSuffix") != null))
                {
                    return true;
                }
            }
            return false;
        }

        private bool HasPropsVersionProperties(System.Collections.Generic.IEnumerable<XElement> propertyGroups)
        {
            foreach (var pg in propertyGroups)
            {
                // Skip conditional PropertyGroups
                if (pg.Attribute("Condition") != null)
                    continue;

                // Check for at least one version property
                if (pg.Element("Version") != null ||
                    pg.Element("AssemblyVersion") != null ||
                    pg.Element("FileVersion") != null ||
                    pg.Element("AssemblyInformationalVersion") != null ||
                    (pg.Element("VersionPrefix") != null && pg.Element("VersionSuffix") != null))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
