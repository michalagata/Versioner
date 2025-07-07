using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace AnubisWorks.Tools.Versioner
{
    public class TimeFrameConfiguration
    {
        public List<TimeModel> TimeFrames { get; set; } = new List<TimeModel>();

        public TimeModel GetTimeFrameConfig(DateTime date)
        {
            var semVerBase = this.TimeFrames.Where(w => date.Date >= w.DateStart && date.Date <= w.DateEnd).OrderByDescending(o => o.DateStart).FirstOrDefault() ?? new TimeModel {Name = $"{date:yy}.{date:MM}", Version = new SemVerBase() {Major = int.Parse($"{date:yy}"), Minor = int.Parse($"{date:MM}"),}};

            return semVerBase;
        }
    }
}