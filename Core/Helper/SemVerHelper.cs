using AnubisWorks.Tools.Versioner.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace AnubisWorks.Tools.Versioner.Helper
{
    public static class SemVerHelper
    {
        public static string ToNpmSemver(this string version)
        {
            string[] pieces = version.Split('.');

            if(pieces.Length<4) throw new ArgumentOutOfRangeException("Version must be in net SemVer format");

            return pieces[0] + "." + pieces[1] + "." + pieces[3];
        }

        public static string ReplaceLastUnit(this string version, string repl)
        {
            string retVal = string.Empty;
            string[] pieces = version.Split('.');

            if(pieces.Length>4 && pieces.Length<3) throw new ArgumentOutOfRangeException("Version must be in net SemVer format");

            if(pieces.Length == 3)
            {
                retVal = pieces[0] + "." + pieces[1] + "." + repl;
            }

            if(pieces.Length == 4)
            {
                retVal = pieces[0] + "." + pieces[1] + "." + pieces[2] + repl;
            }

            return retVal;
        }

        public static string ReturnArtifactVersionBase(List<GitLogEntry> logEntries)
        {
            DateTime baseTime = DateTime.Now;
            return ReturnArtifactVersionBase(logEntries.Count, baseTime);
        }

        public static MajorMinorPatchDockerModel ReturnDockerVersionBase(List<GitLogEntry> logEntries)
        {
            DateTime baseTime = DateTime.Now;
            return ReturnDockerVersionBase(logEntries.Count, baseTime);
        }

        public static string ReturnArtifactVersionBase(int logEntries)
        {
            DateTime baseTime = DateTime.Now;

            return ReturnArtifactVersionBase(logEntries, baseTime);
        }

        public static string ReturnArtifactVersionBase(int logEntries, DateTime baseTime)
        {
            return $"{baseTime.Year.ToString().Substring(2, 2):00}.{baseTime.Month:00}.{logEntries}";
        }

        public static MajorMinorPatchDockerModel ReturnDockerVersionBase(int logEntries, DateTime baseTime)
        {
            MajorMinorPatchDockerModel returnMdl = new MajorMinorPatchDockerModel();
            returnMdl.Major = "1";
            returnMdl.Minor = $"{baseTime.Year.ToString().Substring(2, 2):00}{baseTime.Month:00}";
            returnMdl.Patch = $"{logEntries}";

            return returnMdl;
        }

        public static string ReturnMonoRepoArtifactVersion(string artifactVersion)
        {
            DateTime baseTime = DateTime.Now;
            return $"{(int.Parse(baseTime.Year.ToString().Substring(0, 1)) + int.Parse(baseTime.Year.ToString().Substring(1, 1)) + int.Parse(baseTime.Year.ToString().Substring(0, 4).Substring(2, 1)) + int.Parse(baseTime.Year.ToString().Substring(0, 4).Substring(3, 1))).ToString():00}" + "." + artifactVersion;
        }

        public static string EnsurePropperMonths(this string semver)
        {
            string[] joins = semver.Split(".");

            if (joins.Length < 3 || joins.Length > 4)
            {
                throw new Exception("SemVer Format Incorrect");
            }

            if(joins[1].Length != 3) return semver;

            string year = joins[1].Substring(0, 2);
            string partialMonth = joins[1].Substring(2, joins[1].Length - 2);

            if (joins[1].Length == 3) joins[1] = year + $"0{partialMonth}";

            return string.Join(".", joins);

        }
    }
}