using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WzComparerR2.WzLib
{
    public class Wz_Header
    {
        public const string PKG1 = "PKG1";
        public const string PKG2 = "PKG2";

        public Wz_Header(string signature, string copyright, string file_name, int head_size, long data_size, long file_size, long dataStartPosition)
        {
            this.Signature = signature;
            this.Copyright = copyright;
            this.FileName = file_name;
            this.HeaderSize = head_size;
            this.DataSize = data_size;
            this.FileSize = file_size;
            this.DataStartPosition = dataStartPosition;
            this.VersionChecked = false;
        }

        public string Signature { get; private set; }
        public string Copyright { get; private set; }
        public string FileName { get; private set; }

        public int HeaderSize { get; private set; }
        public long DataSize { get; private set; }
        public long FileSize { get; private set; }
        public long DataStartPosition { get; private set; }
        public long DirEndPosition { get; set; }

        public bool VersionChecked { get; set; }
        public Wz_Capabilities Capabilities { get; internal set; }

        public int WzVersion => this.versionDetector?.WzVersion ?? 0;
        public uint HashVersion => this.versionDetector?.HashVersion ?? 0;
        public bool TryGetNextVersion() => this.versionDetector?.TryGetNextVersion() ?? false;

        public uint Pkg2Hash1 => this.versionDetector is FixedVersionPkg2 pkg2 ? pkg2.Hash1 : throw new NotSupportedException();

        private IWzVersionDetector versionDetector;

        public void SetWzVersion(int wzVersion)
        {
            this.versionDetector = new FixedVersion(wzVersion);
        }

        public void SetOrdinalVersionDetector(int encryptedVersion)
        {
            this.versionDetector = new OrdinalVersionDetector(encryptedVersion);
        }

        public void SetWzVersionPkg2(uint hash1, uint hash2)
        {
            this.versionDetector = new FixedVersionPkg2(hash1, hash2);
        }

        public bool HasCapabilities(Wz_Capabilities cap)
        {
            return cap == (this.Capabilities & cap);
        }

        public static uint CalcHashVersion(int wzVersion)
        {
            uint sum = 0;
            string versionStr = wzVersion.ToString(System.Globalization.CultureInfo.InvariantCulture);
            for (int j = 0; j < versionStr.Length; j++)
            {
                sum <<= 5;
                sum += (uint)versionStr[j] + 1;
            }
            
            return sum;
        }

        // For pkg2 wz files, the version is a string that stored in MapleStory.exe, we can't find it without disassembling.
        public static uint CalcHashVersionPkg2(string wzVersion)
        {
            ReadOnlySpan<byte> strBytes = MemoryMarshal.Cast<char, byte>(wzVersion.AsSpan());
            uint hash = 0x811C9DC5;
            foreach (var c in strBytes)
            {
                hash = (hash ^ c) * 0x1000193;
            }
            hash = 0x85EBCA6B * (hash ^ (hash >> 13));
            return hash ^ (hash >> 16);
        }

        public interface IWzVersionDetector
        {
            bool TryGetNextVersion();
            int WzVersion { get; }
            uint HashVersion { get; }
        }

        public class FixedVersion : IWzVersionDetector
        {
            public FixedVersion(int wzVersion)
            {
                this.WzVersion = wzVersion;
                this.HashVersion = CalcHashVersion(wzVersion);
            }

            private bool hasReturned;


            public int WzVersion { get; private set; }

            public uint HashVersion { get; private set; }

            public bool TryGetNextVersion()
            {
                if (!hasReturned)
                {
                    hasReturned = true;
                    return true;
                }

                return false;
            }
        }

        public class OrdinalVersionDetector : IWzVersionDetector
        {
            public OrdinalVersionDetector(int encryptVersion)
            {
                this.EncryptedVersion = encryptVersion;
                this.versionTest = new List<int>();
                this.hasVersionTest = new List<uint>();
                this.startVersion = -1;
            }

            public int EncryptedVersion { get; private set; }

            private int startVersion;
            private List<int> versionTest;
            private List<uint> hasVersionTest;

            public int WzVersion
            {
                get
                {
                    int idx = this.versionTest.Count - 1;
                    return idx > -1 ? this.versionTest[idx] : 0;
                }
            }

            public uint HashVersion
            {
                get
                {
                    int idx = this.hasVersionTest.Count - 1;
                    return idx > -1 ? this.hasVersionTest[idx] : 0;
                }
            }

            public bool TryGetNextVersion()
            {
                for (int i = startVersion + 1; i < Int16.MaxValue; i++)
                {
                    uint sum = CalcHashVersion(i);
                    uint enc = 0xff
                        ^ ((sum >> 24) & 0xFF)
                        ^ ((sum >> 16) & 0xFF)
                        ^ ((sum >> 8) & 0xFF)
                        ^ ((sum) & 0xFF);

                    // if encver does not passed, try every version one by one
                    if (enc == this.EncryptedVersion)
                    {
                        this.versionTest.Add(i);
                        this.hasVersionTest.Add(sum);
                        startVersion = i;
                        return true;
                    }
                }

                return false;
            }
        }

        public class FixedVersionPkg2 : IWzVersionDetector
        {
            public FixedVersionPkg2(uint hash1, uint hash2)
            {
                this.Hash1 = hash1;
                this.Hash2 = hash2;
                this.WzVersion = 0;
                this.HashVersion = (hash1 << 7 | hash1 >> 25) ^ hash2;
            }

            public uint Hash1 { get; private set; }
            public uint Hash2 { get; private set; }
            public int WzVersion { get; private set; }
            public uint HashVersion { get; private set; }

            public bool TryGetNextVersion()
            {
                return false;
            }
        }
    }
}
