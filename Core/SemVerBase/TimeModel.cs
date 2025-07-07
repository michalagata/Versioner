using System;

namespace AnubisWorks.Tools.Versioner
{
    public class TimeModel
    {
        public string Name { get; set; }
        public DateTime DateStart { get; set; }
        public DateTime DateEnd { get; set; }
        public SemVerBase Version { get; set; }
        public bool OverrideWithLocalFile { get; set; } = false;
    }
}