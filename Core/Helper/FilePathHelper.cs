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