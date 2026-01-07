using System;
using System.Runtime.InteropServices;

namespace AnubisWorks.Tools.Versioner.Helper
{
    /// <summary>
    /// Provides cross-platform operating system detection utilities.
    /// </summary>
    internal static class PlatformDetector
    {
        /// <summary>
        /// Gets the current operating system platform.
        /// </summary>
        /// <returns>The current OS platform.</returns>
        /// <exception cref="NotSupportedException">Thrown when the operating system is not supported.</exception>
        internal static OSPlatform GetOperatingSystem()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OSPlatform.OSX;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return OSPlatform.Linux;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return OSPlatform.Windows;
            }

            throw new NotSupportedException($"Unsupported operating system: {RuntimeInformation.OSDescription}");
        }

        /// <summary>
        /// Checks if the current platform is Unix-like (Linux or macOS).
        /// </summary>
        /// <returns>True if the platform is Unix-like, false otherwise.</returns>
        internal static bool IsUnixLike()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || 
                   RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }

        /// <summary>
        /// Gets the appropriate path separator for the current platform.
        /// </summary>
        /// <returns>The path separator character.</returns>
        internal static char GetPathSeparator()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? '\\' : '/';
        }
    }
}
