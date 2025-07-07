namespace AnubisWorks.Tools.Versioner
{
    public static class State
    {
        public static Configuration Config { get; set; }
        public static TimeFrameConfiguration TimeFrameConfiguration { get; set; }
        public static ProjGuidsConfiguration ProjGuidsConfiguration { get; set; } = new ProjGuidsConfiguration();
        public static bool MaintainProjectGuids { get; set; } = false;
        public static string ProjectGuidsConfigurationFile { get; set; }
    }
}