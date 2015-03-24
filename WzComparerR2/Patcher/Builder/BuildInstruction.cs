using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace WzComparerR2.Patcher.Builder
{
    /// <summary>
    /// 表示一条重构文件的指令。
    /// </summary>
    public class BuildInstruction
    {
        public BuildInstruction()
            : this(BuildType.Unknown)
        {
        }

        public BuildInstruction(BuildType type)
        {
            this.Type = type;
            this.Length = 0;
            this.FillByte = 0;
            this.OldFilePosition = 0;
        }

        public BuildType Type { get; set; }

        /// <summary>
        /// 将要更新到新文件的字节长度。
        /// </summary>
        public int Length { get; set; }

        /// <summary>
        /// 填充固定字节的值，只用于RebuildType.FillBytes。
        /// </summary>
        public byte FillByte { get; set; }

        /// <summary>
        /// 从原文件中读取的起始偏移，只用于RebuildType.FromOldFile。
        /// </summary>
        public int OldFilePosition { get; set; }
    }
}
