using System.Collections.Generic;

namespace AnubisWorks.Tools.Versioner
{
    public class VersioningBaseConfiguration
    {
        //timeModel.Version.Major, timeModel.Version.Minor, logEntries.Count, last.ShortHash, last.LongHash, dayOfTheYear
        public string AssemblyInfoFile { get; set; } = "Properties/AssemblyInfo.cs";

        public bool AssemblyFileVersionSet { get; set; } = true;
        public string AssemblyFileVersionFormat { get; set; } = "*.{0:00}{1:00}.{5}.{2}";
        public string AssemblyFileVersionFormatOverride { get; set; } = "{0:00}.{1:00}.{5}.{2}";
        public string AssemblyFileVersionFormatV2 { get; set; } = "*.{0:00}{1:00}.{2}";
        public string AssemblyFileVersionFormatOverrideV2 { get; set; } = "{0:00}.{1:00}.{2}";

        public bool AssemblyVersionSet { get; set; } = true;
        public string AssemblyVersionFormat { get; set; } = "*.{0:00}{1:00}.{5}.{2}";
        public string AssemblyVersionFormatOverride { get; set; } = "{0:00}.{1:00}.{5}.{2}";
        public string AssemblyVersionFormatV2 { get; set; } = "*.{0:00}{1:00}.{2}";
        public string AssemblyVersionFormatOverrideV2 { get; set; } = "{0:00}.{1:00}.{2}";

        public string AssemblyVersionFormatJS { get; set; } = "*.{0:00}{1:00}.{2}";
        public string AssemblyVersionFormatJSOverride { get; set; } = "{0:00}.{1:00}.{2}";
        
        public bool AssemblyInfoVersionSet { get; set; } = true;
        public string AssemblyInfoVersionFormat { get; set; } = "*.{0:00}{1:00}.{5}.{2}+{3}";
        public string AssemblyInfoVersionFormatOverride { get; set; } = "{0:00}.{1:00}.{5}.{2}+{3}";
        public string AssemblyInfoVersionFormatV2 { get; set; } = "*.{0:00}{1:00}.{2}+{3}";
        public string AssemblyInfoVersionFormatOverrideV2 { get; set; } = "{0:00}.{1:00}.{2}+{3}";

        public bool HashAsDescription { get; set; } = true;

        public string ProjectFile { get; set; }

        public List<string> Directories { get; set; } = new List<string>();
    }
}