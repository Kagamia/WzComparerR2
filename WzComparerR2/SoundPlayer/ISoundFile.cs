using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2
{
    public interface ISoundFile
    {
        string FileName { get; }
        int StartPosition { get; }
        int Length { get; }
    }
}
