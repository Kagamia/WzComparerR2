using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using WzComparerR2.WzLib;

namespace WzComparerR2
{
    public class CustomSoundFile : ISoundFile
    {
        public CustomSoundFile(string fileName)
            : this(fileName, 0, 0)
        {
        }

        public CustomSoundFile(string fileName, int startPos, int length)
        {
            this.fileName = fileName;
            this.startPosition = startPos;
            this.length = length;
        }

        private string fileName;
        private int startPosition;
        private int length;

        public string FileName
        {
            get { return this.fileName; }
        }

        public int StartPosition
        {
            get { return this.startPosition; }
        }

        public int Length
        {
            get { return this.length; }
        }
    }
}
