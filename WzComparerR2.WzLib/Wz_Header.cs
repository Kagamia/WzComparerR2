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
    public interface IWzPkg2HashHeader<THash>
    {
        THash Hash1 { get; }
        THash Hash2 { get; }
    }

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
                    this.Capabilities |= Wz_Capabilities.EncverMissing;
                }
            }

            public int EncryptedVersion { get; }
            public bool IsEncverMissing { get; }
        }

        public abstract class WzPkg2HeaderBase<THash> : Wz_Header, IWzPkg2HashHeader<THash>
        {
            protected WzPkg2HeaderBase(string signature, string copyright, string fileName, int headerSize, long dataSize, long fileSize, long dataStartPosition, THash hash1, THash hash2)
                : base(signature, copyright, fileName, headerSize, dataSize, fileSize, dataStartPosition)
            {
                this.Hash1 = hash1;
                this.Hash2 = hash2;
            }

            public THash Hash1 { get; }
            public THash Hash2 { get; }
        }

        public class WzPkg2Header : WzPkg2HeaderBase<uint>
        {
            public WzPkg2Header(string signature, string copyright, string fileName, int headerSize, long dataSize, long fileSize, long dataStartPosition, uint hash1, uint hash2)
                : base(signature, copyright, fileName, headerSize, dataSize, fileSize, dataStartPosition, hash1, hash2)
            {
            }
        }

        public class WzPkg2Header64 : WzPkg2HeaderBase<ulong>
        {
            public WzPkg2Header64(string signature, string copyright, string fileName, int headerSize, long dataSize, long fileSize, long dataStartPosition, ulong hash1, ulong hash2)
                : base(signature, copyright, fileName, headerSize, dataSize, fileSize, dataStartPosition, hash1, hash2)
            {
                this.Capabilities |= Wz_Capabilities.Pkg2RandomHeader64;
            }
        }

        public string Signature { get; private set; }
        public string Copyright { get; private set; }
        public string FileName { get; private set; }

        public int HeaderSize { get; private set; }
        public long DataSize { get; private set; }
        public long FileSize { get; private set; }
        public long DirStartPosition { get; private set; }
        public int WzVersion { get; internal set; }

        public bool IsPkg1 => this.Signature == PKG1;
        public bool IsPkg2 => this.Signature == PKG2;

        public Wz_Capabilities Capabilities { get; internal set; }

        public bool HasCapabilities(Wz_Capabilities cap)
        {
            return cap == (this.Capabilities & cap);
        }
    }
}
