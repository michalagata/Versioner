using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace AnubisWorks.Tools.Versioner
{
    public class ProjGuidsConfiguration
    {
        [JsonIgnore]
        public bool StateChanged { get; set; } = false;
        public List<ProjGuidPair> Entities { get; set; } = new List<ProjGuidPair>();

    }

    public class ProjGuidPair
    {
        public string ProjectHash { get; set; }
        public string ProjectGuid { get; set; } = new Guid().ToString("B");
    }
}
