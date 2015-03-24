using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.Patcher.Builder
{
    public enum BuildType
    {
        Unknown = 0,
        FromPatcher = 1,
        FillBytes = 2,
        FromOldFile = 3,
        Ending = 4
    }
}
