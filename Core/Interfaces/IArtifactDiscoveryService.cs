using System.Collections.Generic;
using AnubisWorks.Tools.Versioner.Model;

namespace AnubisWorks.Tools.Versioner.Interfaces
{
    /// <summary>
    /// Service for discovering and categorizing versionable artifacts in a repository.
    /// </summary>
    public interface IArtifactDiscoveryService
    {
        /// <summary>
        /// Discovers all versionable artifacts in the repository.
        /// </summary>
        /// <param name="repositoryRoot">Root directory of the repository</param>
        /// <param name="recursive">Whether to search recursively in subdirectories</param>
        /// <param name="allowedTypes">Optional list of artifact types to include (e.g., ["dotnet", "npm", "docker"]). If null or empty, all types are included.</param>
        /// <returns>Discovery result containing categorized artifacts</returns>
        ArtifactDiscoveryResult DiscoverArtifacts(
            string repositoryRoot,
            bool recursive = true,
            List<string>? allowedTypes = null);
    }

    /// <summary>
    /// Result of artifact discovery operation.
    /// </summary>
    public class ArtifactDiscoveryResult
    {
        public List<ArtifactInfo> DotNetProjects { get; set; } = new List<ArtifactInfo>();
        public List<ArtifactInfo> PropsFiles { get; set; } = new List<ArtifactInfo>();
        public List<ArtifactInfo> NuGetPackages { get; set; } = new List<ArtifactInfo>();
        public List<ArtifactInfo> NpmPackages { get; set; } = new List<ArtifactInfo>();
        public List<ArtifactInfo> DockerArtifacts { get; set; } = new List<ArtifactInfo>();
        public List<ArtifactInfo> PythonProjects { get; set; } = new List<ArtifactInfo>();
        public List<ArtifactInfo> GoModules { get; set; } = new List<ArtifactInfo>();
        public List<ArtifactInfo> RustProjects { get; set; } = new List<ArtifactInfo>();
        public List<ArtifactInfo> JavaProjects { get; set; } = new List<ArtifactInfo>();
        public List<ArtifactInfo> HelmCharts { get; set; } = new List<ArtifactInfo>();
        public List<ArtifactInfo> YamlConfigs { get; set; } = new List<ArtifactInfo>();
        
        public List<string> ExcludedPaths { get; set; } = new List<string>();
        public int TotalArtifacts => 
            DotNetProjects.Count + PropsFiles.Count + NuGetPackages.Count + NpmPackages.Count +
            DockerArtifacts.Count + PythonProjects.Count + GoModules.Count + RustProjects.Count +
            JavaProjects.Count + HelmCharts.Count + YamlConfigs.Count;
    }

    /// <summary>
    /// Information about a discovered artifact.
    /// </summary>
    public class ArtifactInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public ArtifactType Type { get; set; }
        public string TypeName { get; set; } = string.Empty; // "dotnet", "npm", "python", etc.
        public string Directory { get; set; } = string.Empty;
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Types of artifacts that can be versioned.
    /// </summary>
    public enum ArtifactType
    {
        DotNet,
        Props,
        NuGet,
        Npm,
        Docker,
        Python,
        Go,
        Rust,
        Java,
        Helm,
        Yaml
    }
}

