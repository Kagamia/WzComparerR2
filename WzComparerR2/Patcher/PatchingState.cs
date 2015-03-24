using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.Patcher
{
    public enum PatchingState
    {
        PatchStart,
        VerifyOldChecksumBegin,
        VerifyOldChecksumEnd,
        VerifyNewChecksumBegin,
        VerifyNewChecksumEnd,
        TempFileCreated,
        TempFileBuildProcessChanged,
        TempFileClosed,
    }
}
