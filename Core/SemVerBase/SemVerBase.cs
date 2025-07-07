using System;

namespace AnubisWorks.Tools.Versioner
{
    public class SemVerBase
    {
        public Int32 Major { get; set; }
        public Int32 Minor { get; set; }
        public Int32 Patch { get; set; }
        public Int32 Hotfix { get; set; }
    }
}