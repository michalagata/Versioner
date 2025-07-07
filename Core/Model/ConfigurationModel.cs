using System.Collections.Generic;

namespace AnubisWorks.Tools.Versioner
{
    public class Configuration
    {
        public VersioningBaseConfiguration Default { get; set; } = new VersioningBaseConfiguration();
        public List<VersioningBaseConfiguration> Items { get; set; } = new List<VersioningBaseConfiguration>();

        public Configuration()
        {
        }
    }
}