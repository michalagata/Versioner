using System;
using System.Collections.Generic;
using System.Text;

namespace AnubisWorks.Tools.Versioner.Helper
{
    public static class GuidHelper
    {
        public static string ToVsMode(this Guid gid, bool removeBracelets = false)
        {
            if (!removeBracelets) return gid.ToString("B");
            else return gid.ToString("B").Replace("{", "").Replace("}", "");
        }
    }
}
