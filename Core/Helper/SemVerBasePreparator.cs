using System;
using System.Collections.Generic;
using System.Text;

namespace AnubisWorks.Tools.Versioner.Helper
{
    public static class SemVerBasePreparator
    {

        public static List<TimeModel> ReturnSprintList()
        {
            List<TimeModel> SemVers = new List<TimeModel>();
            List<int> monthsTillNow = GetMonthsTillNow();
            foreach(int month in monthsTillNow)
            {
                int minor = new DateTime(DateTime.Now.Year, month, 1).Month;
                int major = int.Parse(new DateTime(DateTime.Now.Year, month, 1).Year.ToString().Substring(2, 2));
                TimeModel sp = new TimeModel {Name = $"{new DateTime(DateTime.Now.Year, month, 1).Year}-{minor}", Version = new SemVerBase {Major = major, Minor = minor}, DateStart = new DateTime(DateTime.Now.Year, month, 1), DateEnd = LastDayOfMonth(new DateTime(DateTime.Now.Year, month, 1))};
                SemVers.Add(sp);
            }

            return SemVers;
        }

        private static List<int> GetMonthsTillNow()
        {
            List<int> ll = new List<int>();
            DateTime now = DateTime.Today;
            for (int i = 1; i <= 12; i++)
            {
                if(i>now.Month) break;
                ll.Add(i);
            }

            return ll;
        }

        private static DateTime LastDayOfMonth(DateTime dt)
        {
            DateTime ss = new DateTime(dt.Year, dt.Month, 1).AddMonths(1).AddDays(-1);
            return ss;
        }
    }
}
