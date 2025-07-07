using System;
using System.Collections.Generic;
using System.Text;

namespace AnubisWorks.Tools.Versioner.Model
{

    public class CustomProjectSettings
    {
        public List<CustomSingleProjectSettings> ProjectSettings { get; set; } = new List<CustomSingleProjectSettings>();
    }

    public class CustomSingleProjectSettings
    {
        public string ProjectName { get; set; }

        public List<string> Directories { get; set; } = new List<string>();
    }
}
