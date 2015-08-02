using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.WzLib
{
    public class Wz_Header
    {
        public Wz_Header(string signature, string copyright, string file_name, int head_size, long data_size, long file_size, int encVersion)
        {
            this.Signature = signature;
            this.Copyright = copyright;
            this.FileName = file_name;
            this.HeaderSize = head_size;
            this.DataSize = data_size;
            this.FileSize = file_size;
            this.EncryptedVersion = encVersion;
            this.VersionChecked = false;

            this.c_version_test = new List<int>();
            this.hash_version_test = new List<uint>();
            this.startVersion = -1;
        }

        public string Signature { get; set; }
        public string Copyright { get; set; }
        public string FileName { get; set; }

        public int HeaderSize { get; set; }
        public long DataSize { get; set; }
        public long FileSize { get; set; }
        public int EncryptedVersion { get; set; }

        public bool VersionChecked { get; set; }

        private List<int> c_version_test;
        private List<uint> hash_version_test;
        private int startVersion;

        public int WzVersion
        {
            get
            {
                int idx = this.c_version_test.Count - 1;
                return idx > -1 ? this.c_version_test[idx] : 0;
            }
        }

        public uint HashVersion
        {
            get
            {
                int idx = this.hash_version_test.Count - 1;
                return idx > -1 ? this.hash_version_test[idx] : 0;
            }
        }

        public bool TryGetNextVersion()
        {
            for (int i = startVersion + 1; i < Int16.MaxValue; i++)
            {
                int sum = 0;
                string versionStr = i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                for (int j = 0; j < versionStr.Length; j++)
                {
                    sum <<= 5;
                    sum += (int)versionStr[j] + 1;
                }
                int enc = 0xff
                    ^ ((sum >> 24) & 0xFF)
                    ^ ((sum >> 16) & 0xFF)
                    ^ ((sum >> 8) & 0xFF)
                    ^ ((sum) & 0xFF);

                if (enc == EncryptedVersion)
                {
                    this.c_version_test.Add(i);
                    this.hash_version_test.Add((uint)sum);
                    startVersion = i;
                    return true;
                }
            }

            return false;
        }
    }
}
