using System;
using System.Runtime.InteropServices;
using System.Text;
using WzComparerR2.WzLib.Compatibility;

#if NET6_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

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
            this.DirStartPosition = dataStartPosition;
            this.VersionChecked = false;
        }

        public class WzPkg1Header : Wz_Header
        {
            public WzPkg1Header(string signature, string copyright, string fileName, int headerSize, long dataSize, long fileSize, long dataStartPosition, bool encverMissing, int encryptedVersion)
                : base(signature, copyright, fileName, headerSize, dataSize, fileSize, dataStartPosition)
            {
                this.EncryptedVersion = encryptedVersion;
                this.IsEncverMissing = encverMissing;

                if (encverMissing)
                {
                    this.WzVersion = 777;
                    this.HashVersion = CalcHashVersion(777);
                    this.VersionChecked = true;
                    this.Capabilities |= Wz_Capabilities.EncverMissing;
                }
            }

            public int EncryptedVersion { get; }
            public bool IsEncverMissing { get; }
        }

        public class WzPkg2Header : Wz_Header
        {
            public WzPkg2Header(string signature, string copyright, string fileName, int headerSize, long dataSize, long fileSize, long dataStartPosition, uint hash1, uint hash2)
                : base(signature, copyright, fileName, headerSize, dataSize, fileSize, dataStartPosition)
            {
                this.Hash1 = hash1;
                this.Hash2 = hash2;
            }

            public uint Hash1 { get; }
            public uint Hash2 { get; }

            /// <summary>
            /// The PKG2 directory string reader assigned during crypto detection, used for dir tree reading.
            /// </summary>
            internal IPkg2DirStringReader DirStringReader { get; set; }
        }

        public string Signature { get; private set; }
        public string Copyright { get; private set; }
        public string FileName { get; private set; }

        public int HeaderSize { get; private set; }
        public long DataSize { get; private set; }
        public long FileSize { get; private set; }
        public long DirStartPosition { get; private set; }

        public bool IsPkg1 => this.Signature == PKG1;
        public bool IsPkg2 => this.Signature == PKG2;

        public bool VersionChecked { get; set; }
        public Wz_Capabilities Capabilities { get; internal set; }

        public int WzVersion { get; internal set; }
        public uint HashVersion { get; internal set; }

        public bool HasCapabilities(Wz_Capabilities cap)
        {
            return cap == (this.Capabilities & cap);
        }

        public static uint CalcHashVersion(int wzVersion)
        {
            uint sum = 0;
#if NET6_0_OR_GREATER
            Span<char> versionStr = stackalloc char[11];
            wzVersion.TryFormat(versionStr, out int charsWritten, provider: System.Globalization.CultureInfo.InvariantCulture);
            versionStr = versionStr.Slice(0, charsWritten);
#else
            string versionStr = wzVersion.ToString(System.Globalization.CultureInfo.InvariantCulture);
#endif
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
    }
}
