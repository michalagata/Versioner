using System;
using System.Collections.Generic;
using System.Text;

namespace AnubisWorks.Tools.Versioner.Model
{
    public class VersionedSetModel
    {
        public string AssemblyVersion { get; }
        public string AssemblyInfoVersion { get; }
        public string AssemblyFileVersion { get; }
        public string BuildLabel { get; }
        public string Description { get; }
        public string ShortHash { get; }
        public bool IsSet { get; } = false;

        public VersionedSetModel(string assemblyVersion, string assemblyInfoVersion, string assemblyFileVersion, string buildLabel, string description, string hash)
        {
            AssemblyVersion = assemblyVersion;
            AssemblyInfoVersion = assemblyInfoVersion;
            AssemblyFileVersion = assemblyFileVersion;
            BuildLabel = buildLabel;
            ShortHash = hash;
            Description = description;
            IsSet = true;
        }

        public VersionedSetModel()
        {
            IsSet = false;
        }
    }
}
