using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AnubisWorks.Tools.Versioner.Interfaces;
using AnubisWorks.Tools.Versioner.Model;
using Serilog;

namespace AnubisWorks.Tools.Versioner.Services
{
    /// <summary>
    /// Service for discovering and categorizing versionable artifacts in a repository.
    /// </summary>
    public class ArtifactDiscoveryService : IArtifactDiscoveryService
    {
        private readonly IFileOperations _fileOperations;
        private readonly ILogger _logger;
        private readonly HashSet<string> _excludedPatterns;

        public ArtifactDiscoveryService(IFileOperations fileOperations, ILogger logger)
        {
            _fileOperations = fileOperations;
            _logger = logger;
            _excludedPatterns = LoadDefaultExclusions();
        }

        public ArtifactDiscoveryResult DiscoverArtifacts(
            string repositoryRoot,
            bool recursive = true,
            List<string>? allowedTypes = null)
        {
            _logger.Information("Starting artifact discovery in: {root}", repositoryRoot);

            var result = new ArtifactDiscoveryResult();
            var normalizedAllowedTypes = allowedTypes?.Select(t => t.ToLowerInvariant()).ToHashSet();

            // Load .versionerignore patterns if file exists
            var ignorePatterns = LoadIgnorePatterns(repositoryRoot);
            _excludedPatterns.UnionWith(ignorePatterns);

            // Discover .NET projects
            if (ShouldIncludeType("dotnet", normalizedAllowedTypes))
            {
                result.DotNetProjects = DiscoverDotNetProjects(repositoryRoot, recursive);
                _logger.Debug("Found {count} .NET projects", result.DotNetProjects.Count);
            }

            // Discover .props files (skip in monorepo mode - we only handle Directory.Build.props there)
            // Note: In monorepo mode, Directory.Build.props is handled separately in GlobalRepoVersioningService
            if (ShouldIncludeType("props", normalizedAllowedTypes))
            {
                result.PropsFiles = DiscoverPropsFiles(repositoryRoot, recursive);
                _logger.Debug("Found {count} .props files", result.PropsFiles.Count);
            }

            // Discover NuGet packages
            if (ShouldIncludeType("nuget", normalizedAllowedTypes))
            {
                result.NuGetPackages = DiscoverNuGetPackages(repositoryRoot, recursive);
                _logger.Debug("Found {count} NuGet packages", result.NuGetPackages.Count);
            }

            // Discover NPM packages
            if (ShouldIncludeType("npm", normalizedAllowedTypes))
            {
                result.NpmPackages = DiscoverNpmPackages(repositoryRoot, recursive);
                _logger.Debug("Found {count} NPM packages", result.NpmPackages.Count);
            }

            // Discover Docker artifacts
            if (ShouldIncludeType("docker", normalizedAllowedTypes))
            {
                result.DockerArtifacts = DiscoverDockerArtifacts(repositoryRoot, recursive);
                _logger.Debug("Found {count} Docker artifacts", result.DockerArtifacts.Count);
            }

            // Discover Python projects (Phase 2)
            if (ShouldIncludeType("python", normalizedAllowedTypes))
            {
                result.PythonProjects = DiscoverPythonProjects(repositoryRoot, recursive);
                _logger.Debug("Found {count} Python projects", result.PythonProjects.Count);
            }

            // Discover Go modules (Phase 2)
            if (ShouldIncludeType("go", normalizedAllowedTypes))
            {
                result.GoModules = DiscoverGoModules(repositoryRoot, recursive);
                _logger.Debug("Found {count} Go modules", result.GoModules.Count);
            }

            // Discover Rust projects (Phase 2)
            if (ShouldIncludeType("rust", normalizedAllowedTypes))
            {
                result.RustProjects = DiscoverRustProjects(repositoryRoot, recursive);
                _logger.Debug("Found {count} Rust projects", result.RustProjects.Count);
            }

            // Discover Java/Maven projects (Phase 2)
            if (ShouldIncludeType("java", normalizedAllowedTypes))
            {
                result.JavaProjects = DiscoverJavaProjects(repositoryRoot, recursive);
                _logger.Debug("Found {count} Java projects", result.JavaProjects.Count);
            }

            // Discover Helm charts (Phase 2)
            if (ShouldIncludeType("helm", normalizedAllowedTypes))
            {
                result.HelmCharts = DiscoverHelmCharts(repositoryRoot, recursive);
                _logger.Debug("Found {count} Helm charts", result.HelmCharts.Count);
            }

            // Discover YAML configs (Phase 2)
            if (ShouldIncludeType("yaml", normalizedAllowedTypes))
            {
                result.YamlConfigs = DiscoverYamlConfigs(repositoryRoot, recursive);
                _logger.Debug("Found {count} YAML configs", result.YamlConfigs.Count);
            }

            _logger.Information("Artifact discovery completed. Total artifacts: {total}", result.TotalArtifacts);
            return result;
        }

        private bool ShouldIncludeType(string typeName, HashSet<string>? allowedTypes)
        {
            if (allowedTypes == null || allowedTypes.Count == 0)
                return true;
            return allowedTypes.Contains(typeName.ToLowerInvariant());
        }

        private List<ArtifactInfo> DiscoverDotNetProjects(string repositoryRoot, bool recursive)
        {
            var artifacts = new List<ArtifactInfo>();
            var files = _fileOperations.FindFiles("*.csproj", repositoryRoot) ?? new List<string>();
            
            foreach (var file in files)
            {
                // Convert absolute path to relative path for pattern matching
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                if (ShouldExclude(relativePath))
                {
                    _logger.Debug("Excluding file: {file} (matched pattern)", file);
                    continue;
                }

                artifacts.Add(new ArtifactInfo
                {
                    FilePath = file,
                    Type = ArtifactType.DotNet,
                    TypeName = "dotnet",
                    Directory = Path.GetDirectoryName(file) ?? string.Empty,
                    Metadata = new Dictionary<string, string> { { "Extension", ".csproj" } }
                });
            }

            return artifacts;
        }

        private List<ArtifactInfo> DiscoverPropsFiles(string repositoryRoot, bool recursive)
        {
            var artifacts = new List<ArtifactInfo>();
            var files = _fileOperations.FindFiles("*.props", repositoryRoot) ?? new List<string>();
            
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                if (ShouldExclude(relativePath))
                {
                    continue;
                }

                artifacts.Add(new ArtifactInfo
                {
                    FilePath = file,
                    Type = ArtifactType.Props,
                    TypeName = "props",
                    Directory = Path.GetDirectoryName(file) ?? string.Empty,
                    Metadata = new Dictionary<string, string> { { "Extension", ".props" } }
                });
            }

            return artifacts;
        }

        private List<ArtifactInfo> DiscoverNuGetPackages(string repositoryRoot, bool recursive)
        {
            var artifacts = new List<ArtifactInfo>();
            var files = _fileOperations.FindFiles("*.nuspec", repositoryRoot) ?? new List<string>();
            
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                if (ShouldExclude(relativePath))
                {
                    continue;
                }

                artifacts.Add(new ArtifactInfo
                {
                    FilePath = file,
                    Type = ArtifactType.NuGet,
                    TypeName = "nuget",
                    Directory = Path.GetDirectoryName(file) ?? string.Empty,
                    Metadata = new Dictionary<string, string> { { "Extension", ".nuspec" } }
                });
            }

            return artifacts;
        }

        private List<ArtifactInfo> DiscoverNpmPackages(string repositoryRoot, bool recursive)
        {
            var artifacts = new List<ArtifactInfo>();
            var files = _fileOperations.FindFiles("package.json", repositoryRoot) ?? new List<string>();
            
            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                if (ShouldExclude(relativePath) || file.Contains("node_modules"))
                {
                    continue;
                }

                artifacts.Add(new ArtifactInfo
                {
                    FilePath = file,
                    Type = ArtifactType.Npm,
                    TypeName = "npm",
                    Directory = Path.GetDirectoryName(file) ?? string.Empty,
                    Metadata = new Dictionary<string, string> { { "Extension", "package.json" } }
                });
            }

            return artifacts;
        }

        private List<ArtifactInfo> DiscoverDockerArtifacts(string repositoryRoot, bool recursive)
        {
            var artifacts = new List<ArtifactInfo>();
            var dockerfiles = _fileOperations.FindDockerfiles(repositoryRoot) ?? new List<string>();
            
            foreach (var file in dockerfiles)
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                if (ShouldExclude(relativePath))
                {
                    continue;
                }

                artifacts.Add(new ArtifactInfo
                {
                    FilePath = file,
                    Type = ArtifactType.Docker,
                    TypeName = "docker",
                    Directory = Path.GetDirectoryName(file) ?? string.Empty,
                    Metadata = new Dictionary<string, string> { { "Type", "Dockerfile" } }
                });
            }

            // Also check for docker-compose.yml and compose.yml
            var composeYml = _fileOperations.FindFiles("docker-compose.yml", repositoryRoot) ?? new List<string>();
            var composeYmlAlt = _fileOperations.FindFiles("compose.yml", repositoryRoot) ?? new List<string>();
            var composeFiles = composeYml.Concat(composeYmlAlt);
            
            foreach (var file in composeFiles)
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                if (ShouldExclude(relativePath))
                {
                    continue;
                }

                artifacts.Add(new ArtifactInfo
                {
                    FilePath = file,
                    Type = ArtifactType.Docker,
                    TypeName = "docker",
                    Directory = Path.GetDirectoryName(file) ?? string.Empty,
                    Metadata = new Dictionary<string, string> { { "Type", "DockerCompose" } }
                });
            }

            return artifacts;
        }

        // Phase 2 implementations
        private List<ArtifactInfo> DiscoverPythonProjects(string repositoryRoot, bool recursive)
        {
            var artifacts = new List<ArtifactInfo>();
            
            // Discover pyproject.toml
            var pyprojectFiles = _fileOperations.FindFiles("pyproject.toml", repositoryRoot) ?? new List<string>();
            foreach (var file in pyprojectFiles)
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                if (ShouldExclude(relativePath)) continue;
                artifacts.Add(new ArtifactInfo
                {
                    FilePath = file,
                    Type = ArtifactType.Python,
                    TypeName = "python",
                    Directory = Path.GetDirectoryName(file) ?? string.Empty,
                    Metadata = new Dictionary<string, string> { { "Format", "pyproject.toml" } }
                });
            }

            // Discover setup.py
            var setupPyFiles = _fileOperations.FindFiles("setup.py", repositoryRoot) ?? new List<string>();
            foreach (var file in setupPyFiles)
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                if (ShouldExclude(relativePath)) continue;
                artifacts.Add(new ArtifactInfo
                {
                    FilePath = file,
                    Type = ArtifactType.Python,
                    TypeName = "python",
                    Directory = Path.GetDirectoryName(file) ?? string.Empty,
                    Metadata = new Dictionary<string, string> { { "Format", "setup.py" } }
                });
            }

            // Discover setup.cfg
            var setupCfgFiles = _fileOperations.FindFiles("setup.cfg", repositoryRoot) ?? new List<string>();
            foreach (var file in setupCfgFiles)
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                if (ShouldExclude(relativePath)) continue;
                artifacts.Add(new ArtifactInfo
                {
                    FilePath = file,
                    Type = ArtifactType.Python,
                    TypeName = "python",
                    Directory = Path.GetDirectoryName(file) ?? string.Empty,
                    Metadata = new Dictionary<string, string> { { "Format", "setup.cfg" } }
                });
            }

            // Discover __version__.py
            var versionPyFiles = _fileOperations.FindFiles("__version__.py", repositoryRoot) ?? new List<string>();
            foreach (var file in versionPyFiles)
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                if (ShouldExclude(relativePath)) continue;
                artifacts.Add(new ArtifactInfo
                {
                    FilePath = file,
                    Type = ArtifactType.Python,
                    TypeName = "python",
                    Directory = Path.GetDirectoryName(file) ?? string.Empty,
                    Metadata = new Dictionary<string, string> { { "Format", "__version__.py" } }
                });
            }

            return artifacts;
        }

        private List<ArtifactInfo> DiscoverGoModules(string repositoryRoot, bool recursive)
        {
            var artifacts = new List<ArtifactInfo>();
            
            // Discover go.mod
            var goModFiles = _fileOperations.FindFiles("go.mod", repositoryRoot) ?? new List<string>();
            foreach (var file in goModFiles)
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                if (ShouldExclude(relativePath)) continue;
                artifacts.Add(new ArtifactInfo
                {
                    FilePath = file,
                    Type = ArtifactType.Go,
                    TypeName = "go",
                    Directory = Path.GetDirectoryName(file) ?? string.Empty,
                    Metadata = new Dictionary<string, string> { { "Format", "go.mod" } }
                });
            }

            // Discover version.go
            var versionGoFiles = _fileOperations.FindFiles("version.go", repositoryRoot) ?? new List<string>();
            foreach (var file in versionGoFiles)
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                if (ShouldExclude(relativePath)) continue;
                artifacts.Add(new ArtifactInfo
                {
                    FilePath = file,
                    Type = ArtifactType.Go,
                    TypeName = "go",
                    Directory = Path.GetDirectoryName(file) ?? string.Empty,
                    Metadata = new Dictionary<string, string> { { "Format", "version.go" } }
                });
            }

            return artifacts;
        }

        private List<ArtifactInfo> DiscoverRustProjects(string repositoryRoot, bool recursive)
        {
            var artifacts = new List<ArtifactInfo>();
            
            // Discover Cargo.toml
            var cargoFiles = _fileOperations.FindFiles("Cargo.toml", repositoryRoot) ?? new List<string>();
            foreach (var file in cargoFiles)
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                if (ShouldExclude(relativePath)) continue;
                artifacts.Add(new ArtifactInfo
                {
                    FilePath = file,
                    Type = ArtifactType.Rust,
                    TypeName = "rust",
                    Directory = Path.GetDirectoryName(file) ?? string.Empty,
                    Metadata = new Dictionary<string, string> { { "Format", "Cargo.toml" } }
                });
            }

            return artifacts;
        }

        private List<ArtifactInfo> DiscoverJavaProjects(string repositoryRoot, bool recursive)
        {
            var artifacts = new List<ArtifactInfo>();
            
            // Discover pom.xml
            var pomFiles = _fileOperations.FindFiles("pom.xml", repositoryRoot) ?? new List<string>();
            foreach (var file in pomFiles)
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                if (ShouldExclude(relativePath)) continue;
                artifacts.Add(new ArtifactInfo
                {
                    FilePath = file,
                    Type = ArtifactType.Java,
                    TypeName = "java",
                    Directory = Path.GetDirectoryName(file) ?? string.Empty,
                    Metadata = new Dictionary<string, string> { { "Format", "pom.xml" } }
                });
            }

            return artifacts;
        }

        private List<ArtifactInfo> DiscoverHelmCharts(string repositoryRoot, bool recursive)
        {
            var artifacts = new List<ArtifactInfo>();
            
            // Discover Chart.yaml
            var chartFiles = _fileOperations.FindFiles("Chart.yaml", repositoryRoot) ?? new List<string>();
            foreach (var file in chartFiles)
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                if (ShouldExclude(relativePath)) continue;
                artifacts.Add(new ArtifactInfo
                {
                    FilePath = file,
                    Type = ArtifactType.Helm,
                    TypeName = "helm",
                    Directory = Path.GetDirectoryName(file) ?? string.Empty,
                    Metadata = new Dictionary<string, string> { { "Format", "Chart.yaml" } }
                });
            }

            return artifacts;
        }

        private List<ArtifactInfo> DiscoverYamlConfigs(string repositoryRoot, bool recursive)
        {
            var artifacts = new List<ArtifactInfo>();
            
            // Discover compose.yml (already handled in Docker, but also as YAML)
            var composeYml = _fileOperations.FindFiles("compose.yml", repositoryRoot) ?? new List<string>();
            var dockerComposeYml = _fileOperations.FindFiles("docker-compose.yml", repositoryRoot) ?? new List<string>();
            var composeFiles = composeYml.Concat(dockerComposeYml);
            
            foreach (var file in composeFiles)
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                if (ShouldExclude(relativePath)) continue;
                // Only add if not already added as Docker artifact
                artifacts.Add(new ArtifactInfo
                {
                    FilePath = file,
                    Type = ArtifactType.Yaml,
                    TypeName = "yaml",
                    Directory = Path.GetDirectoryName(file) ?? string.Empty,
                    Metadata = new Dictionary<string, string> { { "Format", "DockerCompose" } }
                });
            }

            return artifacts;
        }

        private HashSet<string> LoadDefaultExclusions()
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "node_modules",
                "bin",
                "obj",
                ".git",
                "venv",
                "target",
                ".vs",
                ".idea"
            };
        }

        private HashSet<string> LoadIgnorePatterns(string repositoryRoot)
        {
            var patterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ignoreFile = Path.Combine(repositoryRoot, ".versionerignore");
            
            if (_fileOperations.FileExists(ignoreFile))
            {
                var content = _fileOperations.ReadFileContent(ignoreFile);
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#"))
                    {
                        patterns.Add(trimmed);
                    }
                }
                
                _logger.Debug("Loaded {count} patterns from .versionerignore: {patterns}", patterns.Count, string.Join(", ", patterns));
            }

            return patterns;
        }

        private bool ShouldExclude(string filePath)
        {
            // filePath should already be relative to repository root
            var relativePath = filePath.Replace('\\', '/');
            
            foreach (var pattern in _excludedPatterns)
            {
                // Simple pattern matching: support ** for any directory
                var normalizedPattern = pattern.Replace('\\', '/').Trim();
                
                // If pattern ends with /**, check if path is inside the directory
                if (normalizedPattern.EndsWith("/**", StringComparison.OrdinalIgnoreCase))
                {
                    var prefix = normalizedPattern.Substring(0, normalizedPattern.Length - 3);
                    // Check if path is inside the directory (not just contains the directory name)
                    // Example: pattern "test/**" should match "/test/repo/test/file.csproj" but not "/test/repo/Project.csproj"
                    // We need to check if the path contains "/prefix/" as a directory segment
                    // For pattern "test/**", prefix is "test", so we check for "/test/" in the path
                    var searchPattern = "/" + prefix + "/";
                    var index = relativePath.IndexOf(searchPattern, StringComparison.OrdinalIgnoreCase);
                    if (index >= 0)
                    {
                        // Found "/prefix/" in the path, so this file is inside the directory
                        return true;
                    }
                    // Also check if path starts with "prefix/" (for files directly in the directory)
                    if (relativePath.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                // If pattern ends with *, check if path starts with the prefix
                else if (normalizedPattern.EndsWith("*", StringComparison.OrdinalIgnoreCase) && !normalizedPattern.EndsWith("/**", StringComparison.OrdinalIgnoreCase))
                {
                    var prefix = normalizedPattern.Substring(0, normalizedPattern.Length - 1);
                    if (relativePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                        relativePath.Contains("/" + prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                // For exact directory names (e.g., "node_modules"), check if it's a directory segment
                else if (normalizedPattern.Contains("/"))
                {
                    // Directory pattern - must match as directory segment
                    if (relativePath.Contains("/" + normalizedPattern + "/", StringComparison.OrdinalIgnoreCase) ||
                        relativePath.StartsWith(normalizedPattern + "/", StringComparison.OrdinalIgnoreCase) ||
                        relativePath.EndsWith("/" + normalizedPattern, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                // Simple contains check for exact matches (e.g., "node_modules", "bin", "obj")
                else
                {
                    // Check if it's a directory segment (not part of filename)
                    // Example: "node_modules" should match "/path/node_modules/file.json" but not "/path/node_modules_file.json"
                    if (relativePath.Contains("/" + normalizedPattern + "/", StringComparison.OrdinalIgnoreCase) ||
                        relativePath.StartsWith(normalizedPattern + "/", StringComparison.OrdinalIgnoreCase) ||
                        relativePath.EndsWith("/" + normalizedPattern, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

