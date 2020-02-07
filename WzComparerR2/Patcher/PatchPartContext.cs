using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.WzLib;

namespace WzComparerR2.Patcher
{
    public class PatchPartContext
    {
        public PatchPartContext(string fileName, long offset, int type)
        {
            this.offset = offset;
            this.fileName = fileName;
            this.type = type;

            Match m = Regex.Match(fileName, @"^([A-Za-z]+)\d*(?:\.wz)?$");
            if (m.Success)
            {
                fileName = m.Result("$1");
            }

            try
            {
                this.wzType = (Wz_Type)Enum.Parse(typeof(Wz_Type), fileName, true);
            }
            catch
            {
                this.wzType = Wz_Type.Unknown;
            }
        }

        private long offset;
        private string fileName;
        private int type;
        private Wz_Type wzType;
        private int newFileLength;
        private uint oldChecksum;
        private uint newChecksum;
        private string tempFilePath;
        private string oldFilePath;

        private int action0;
        private int action1;
        private int action2;

        public long Offset
        {
            get { return offset; }
            set { offset = value; }
        }

        public string FileName
        {
            get { return fileName; }
        }

        public int Type
        {
            get { return type; }
        }

        public Wz_Type WzType
        {
            get { return wzType; }
        }

        public int NewFileLength
        {
            get { return newFileLength; }
            set { newFileLength = value; }
        }

        public uint OldChecksum
        {
            get { return oldChecksum; }
            set { oldChecksum = value; }
        }

        public uint NewChecksum
        {
            get { return newChecksum; }
            set { newChecksum = value; }
        }

        public string TempFilePath
        {
            get { return tempFilePath; }
            set { tempFilePath = value; }
        }

        public string OldFilePath
        {
            get { return oldFilePath; }
            set { oldFilePath = value; }
        }

        public int Action0
        {
            get { return action0; }
            set { action0 = value; }
        }

        public int Action1
        {
            get { return action1; }
            set { action1 = value; }
        }

        public int Action2
        {
            get { return action2; }
            set { action2 = value; }
        }
    }
}
