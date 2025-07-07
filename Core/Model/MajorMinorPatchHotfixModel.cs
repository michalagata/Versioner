using System;
using System.Collections.Generic;
using System.Text;

namespace AnubisWorks.Tools.Versioner.Model
{
    public class MajorMinorPatchHotfixModel
    {
        public int Major { get; set; } = 1;
        public int Minor { get; set; } = 0;
        public int Patch { get; set; } = 0;
        public int Hotfix { get; set; } = 0;
    }

    public class MajorMinorPatchDockerModel
    {
        public string Major { get; set; } = "1";
        public string Minor { get; set; } = "0";
        public string Patch { get; set; } = "0";
    }
}
