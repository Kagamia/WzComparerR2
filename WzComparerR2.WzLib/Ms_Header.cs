using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.WzLib
{
    public class Ms_Header
    {
        public Ms_Header(string fullFileName, string keySalt, string fileNameWithSalt, int headerHash, int fileVer, int entryCount, long headerStartPos, long entryStartPos)
        {
            this.FullFileName = fullFileName;
            this.KeySalt = keySalt;
            this.FileNameWithSalt = fileNameWithSalt;
            this.HeaderHash = headerHash;
            this.Version = fileVer;
            this.EntryCount = entryCount;
            this.HeaderStartPosition = headerStartPos;
            this.EntryStartPosition = entryStartPos;
            this.DataStartPosition = -1;
        }

        public string FullFileName { get; private set; }
        public string KeySalt { get; private set; }
        public string FileNameWithSalt { get; private set; }
        public int HeaderHash { get; private set; }
        public int Version { get; private set; }
        public int EntryCount { get; private set; }
        public long HeaderStartPosition { get; private set; }
        public long EntryStartPosition { get; private set; }

        public long DataStartPosition { get; internal set; }
    }
}
