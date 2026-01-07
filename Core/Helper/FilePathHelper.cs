using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AnubisWorks.Tools.Versioner.Helper
{
    public static class FilePathHelper
    {
        public static string ParseSimpleInput(string orgpath)
        {
            if(string.IsNullOrEmpty(orgpath) || string.IsNullOrWhiteSpace(orgpath) || orgpath.Length<2)
            {
                return orgpath;
            }

            if (orgpath.Left(2) == @"\\") return orgpath;

            if (orgpath.StartsWith(@"\")) return orgpath.Right(orgpath.Length - 1);
            else return orgpath;
        }

        public static string ParsePathInput(string originalPath)
        {
            return originalPath.NormalizeEscapings();
        }

        /// <summary>
        /// Normalizes file paths for cross-platform compatibility.
        /// Converts Windows-style backslashes to forward slashes on Unix-like systems (Linux/macOS).
        /// Preserves UNC paths and Windows absolute paths when on Windows.
        /// </summary>
        /// <param name="path">The path to normalize</param>
        /// <returns>Normalized path with appropriate path separators for the current platform</returns>
        public static string NormalizePathForPlatform(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            // Remove quotes first (ArgsInterceptor may preserve quotes from command line)
            // Trim quotes from both ends if present
            string cleaned = path.Trim('"', '\'');

            // Detect if we're on Unix-like system (Linux/macOS)
            bool isUnix = Environment.OSVersion.Platform == PlatformID.Unix ||
                         Environment.OSVersion.Platform == PlatformID.MacOSX ||
                         Path.DirectorySeparatorChar == '/';

            if (!isUnix)
            {
                // On Windows, return as-is (but still normalize escapings)
                return cleaned.NormalizeEscapings();
            }

            // On Unix-like systems, normalize backslashes to forward slashes
            // Note: On Unix, \\ at start is NOT a UNC path (UNC paths don't exist on Unix)
            // So we treat \ as a potential absolute path indicator
            
            // Regular path - convert all backslashes to forward slashes
            string normalized = cleaned.Replace('\\', '/');
            
            // Normalize double slashes (but keep single leading slash for absolute paths)
            if (normalized.StartsWith("/"))
            {
                // Absolute path - keep single leading slash, normalize any double slashes after
                normalized = "/" + normalized.TrimStart('/').Replace("//", "/");
            }
            else
            {
                // Relative path - normalize any double slashes
                normalized = normalized.Replace("//", "/");
            }

            // Remove trailing slashes (consistent with NormalizeEscapings behavior)
            normalized = normalized.TrimEnd('/', '\\');

            return normalized;
        }

        private static string NormalizeEscapings(this string source)
        {
            string rr = source;

            if (rr.EndsWith(@"\\"))
            {
                rr = rr.Left(rr.Length - 2);
            }

            if (rr.EndsWith(@"\") || rr.EndsWith(@"/"))
            {
                rr = rr.Left(rr.Length - 1);
            }

            if (rr.StartsWith(@"\\"))
            {
                return @"\\" + rr.Right(rr.Length - 2).Replace(@"\\", @"\");
            }

            if (rr.StartsWith(@"\") || rr.StartsWith(@"/"))
            {
                rr = rr.Right(rr.Length - 1);
            }
            
            return rr.Replace(@"\\", @"\");
        }

        public static void RenameFile(string path, string newver)
        {
            string destName = Path.GetFileName(path).ReplaceSemver(newver);

            RenameFileInternal(path, destName);
        }

        private static void RenameFileInternal(string oldFilenameWithPathWithExtension, string newFilenameWithoutPathWithExtension)
        {
            try
            {
                string directoryPath = Path.GetDirectoryName(oldFilenameWithPathWithExtension);
                if (directoryPath == null)
                {
                    throw new Exception($"Directory not found in given path value:{oldFilenameWithPathWithExtension}");
                }

                var newFilenameWithPath = Path.Combine(directoryPath, newFilenameWithoutPathWithExtension);

                if(File.Exists(newFilenameWithPath)) File.Delete(newFilenameWithPath);

                FileInfo fileInfo = new FileInfo(oldFilenameWithPathWithExtension);
                fileInfo.MoveTo(newFilenameWithPath);
            }
            catch (Exception e)
            {
                throw;
            }
        }

        public static string Right(this string input, int count)
        {
            return input.Substring(Math.Max(input.Length - count, 0), Math.Min(count, input.Length));
        }

        public static string Left(this string input, int count)
        {
            return input.Substring(0, Math.Min(input.Length, count));
        }

        public static string Mid(this string input, int start)
        {
            return input.Substring(Math.Min(start, input.Length));
        }

        public static string Mid(this string input, int start, int count)
        {
            return input.Substring(Math.Min(start, input.Length), Math.Min(count, Math.Max(input.Length - start, 0)));
        }

        enum StringVector
        {
            Beginning,
            End
        }
    }
}