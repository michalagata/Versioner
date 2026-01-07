using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AnubisWorks.Tools.Versioner.Helper
{
    /// <summary>
    /// Provides path validation utilities for cross-platform compatibility (Windows, Linux, macOS).
    /// </summary>
    public static class PathValidator
    {
        /// <summary>
        /// Validates a file path for cross-platform compatibility.
        /// Checks for invalid characters, empty paths, and provides platform-specific validation.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <param name="parameterName">Name of the parameter for error messages.</param>
        /// <param name="mustExist">If true, validates that the path exists (checks after normalization).</param>
        /// <returns>Validation result with error message if validation fails.</returns>
        public static (bool IsValid, string? ErrorMessage) ValidatePath(string path, string parameterName, bool mustExist = false)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return (IsValid: true, ErrorMessage: null); // Empty paths are allowed (optional parameters)
            }

            // First normalize the path
            string normalizedPath = FilePathHelper.NormalizePathForPlatform(path);

            // Check for invalid characters
            char[] invalidChars = Path.GetInvalidPathChars();
            int invalidCharIndex = normalizedPath.IndexOfAny(invalidChars);
            if (invalidCharIndex >= 0)
            {
                return (IsValid: false, ErrorMessage: $"{parameterName} path contains invalid character(s) at position {invalidCharIndex}: '{normalizedPath[invalidCharIndex]}'");
            }

            // Check for empty path after normalization
            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                return (IsValid: false, ErrorMessage: $"{parameterName} path is empty after normalization");
            }

            // Platform-specific validation - use PlatformDetector for consistency
            bool isUnix = PlatformDetector.IsUnixLike();

            if (isUnix)
            {
                // On Unix-like systems (Linux/macOS), validate path format
                // Absolute paths should start with /
                // Relative paths are allowed

                // Check for Windows-style absolute paths that weren't normalized
                if (normalizedPath.Length >= 2 && normalizedPath[1] == ':' && char.IsLetter(normalizedPath[0]))
                {
                    return (IsValid: false, ErrorMessage: $"{parameterName} path appears to be a Windows absolute path (e.g., 'C:\\...') which is not valid on Unix-like systems. Please use a Unix-style path or relative path.");
                }

                // Check for Windows UNC paths
                if (normalizedPath.StartsWith("\\\\"))
                {
                    return (IsValid: false, ErrorMessage: $"{parameterName} path appears to be a Windows UNC path (\\\\...), which is not valid on Unix-like systems.");
                }
            }
            else
            {
                // On Windows, validate path format
                // Check for Unix-style absolute paths that might cause issues
                // (though Windows can handle forward slashes, we should warn about mixing)
                if (normalizedPath.StartsWith("/") && !normalizedPath.StartsWith("\\"))
                {
                    // This might be a Unix path used on Windows - could work but warn
                    // Actually, Windows can handle forward slashes, so we'll allow it but note it's unusual
                }
            }

            // Check if path should exist
            if (mustExist)
            {
                if (!File.Exists(normalizedPath) && !Directory.Exists(normalizedPath))
                {
                    return (IsValid: false, ErrorMessage: $"{parameterName} path does not exist: {normalizedPath}");
                }
            }

            return (IsValid: true, ErrorMessage: null);
        }

        /// <summary>
        /// Gets a platform-appropriate default path for configuration files.
        /// </summary>
        /// <param name="fileName">The configuration file name (e.g., "projectguids.json")</param>
        /// <returns>Platform-appropriate default path</returns>
        public static string GetPlatformDefaultPath(string fileName)
        {
            bool isUnix = PlatformDetector.IsUnixLike();

            if (isUnix)
            {
                // On Unix-like systems (Linux/macOS), use home directory
                string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                return Path.Combine(homeDir, fileName);
            }
            else
            {
                // On Windows, use user's documents folder or fallback to C:\
                try
                {
                    string documentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    return Path.Combine(documentsDir, fileName);
                }
                catch
                {
                    return Path.Combine("C:\\", fileName);
                }
            }
        }
    }
}


