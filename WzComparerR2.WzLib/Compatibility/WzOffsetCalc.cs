using System;
using WzComparerR2.WzLib.Utilities;
using static WzComparerR2.WzLib.Utilities.MathHelper;

namespace WzComparerR2.WzLib.Compatibility
{
    /// <summary>
    /// Calculates the offset of a wz image/directory entry from its hashed value.
    /// </summary>
    public interface IWzImageOffsetCalc
    {
        uint CalcOffset(uint filePos, uint hashedOffset);
    }

    /// <summary>
    /// Extended offset calculator for PKG2.
    /// </summary>
    public interface IPkg2ImageOffsetCalc : IWzImageOffsetCalc
    {
    }

    /// <summary>
    /// Extended offset calculator for PKG2, also handles entry count decryption.
    /// </summary>
    public interface IPkg2ImageOffsetCalc<TEncryptedEntryCount> : IPkg2ImageOffsetCalc
    {
        int DecryptEntryCount(TEncryptedEntryCount encryptedEntryCount);
    }

    /// <summary>
    /// Optional PKG2 capability for formats that encrypt image length/checksum fields.
    /// </summary>
    public interface IPkg2ImageLengthCalc
    {
        int CalcLength(uint filePos, int encryptedValue);
    }

    internal static class Pkg2ImageOffsetCalcHelper
    {
        public static int DecryptEntryCount(IPkg2ImageOffsetCalc calc, long encryptedEntryCount)
        {
            if (calc is IPkg2ImageOffsetCalc<int> calc32)
                return calc32.DecryptEntryCount(checked((int)encryptedEntryCount));
            if (calc is IPkg2ImageOffsetCalc<long> calc64)
                return calc64.DecryptEntryCount(encryptedEntryCount);
            throw new NotSupportedException($"Unsupported PKG2 offset calculator type: {calc.GetType().FullName}");
        }
    }

    /// <summary>
    /// PKG2 offset calculation algorithm version.
    /// </summary>
    public enum Pkg2OffsetVersion
    {
        /// <summary>KMST 1196-1197</summary>
        KMST1196 = 1,
        /// <summary>KMST 1198</summary>
        KMST1198 = 2,
        /// <summary>KMST 1199-1201</summary>
        KMST1199 = 3,
        /// <summary>KMST 1202-1204</summary>
        KMST1202 = 4,
        /// <summary>KMST 1205</summary>
        KMST1205 = 5,
    }

    /// <summary>
    /// PKG1 offset calculation (original format).
    /// </summary>
    public sealed class Pkg1OffsetCalc : IWzImageOffsetCalc
    {
        public Pkg1OffsetCalc(uint headerLen, uint hashVersion)
        {
            this.headerLen = headerLen;
            this.hashVersion = hashVersion;
        }

        private readonly uint headerLen;
        private readonly uint hashVersion;

        public uint CalcOffset(uint filePos, uint hashedOffset)
        {
            uint offset = filePos - this.headerLen;
            offset = ~offset;
            offset *= this.hashVersion;
            offset -= 0x581C3F6D;
            int distance = (int)offset & 0x1F;
            offset = ROL(offset, distance);
            offset ^= hashedOffset;
            offset += this.headerLen * 2;
            return offset;
        }
    }

    /// <summary>
    /// PKG2 offset calculation for KMST 1196-1197 (V1).
    /// </summary>
    public sealed class Pkg2OffsetCalcV1 : IPkg2ImageOffsetCalc<int>
    {
        public Pkg2OffsetCalcV1(uint headerLen, uint hashVersion, uint hash1)
        {
            this.headerLen = headerLen;
            this.hashVersion = hashVersion;
            this.hash1 = hash1;
        }

        private readonly uint headerLen;
        private readonly uint hashVersion;
        private readonly uint hash1;

        public uint CalcOffset(uint filePos, uint hashedOffset)
        {
            uint offset = filePos - this.headerLen;
            offset = ~offset;
            offset *= this.hashVersion;
            offset -= 0x581C3F6D;
            offset ^= this.hash1 * 0x01010101;
            int distance = (byte)((this.hashVersion ^ this.hash1) & 0x1F);
            offset = ROL(offset, distance);
            offset ^= hashedOffset;
            offset += this.headerLen;
            return offset;
        }

        public int DecryptEntryCount(int encryptedEntryCount)
        {
            return (int)(encryptedEntryCount ^ ((this.hash1 << 24) + (0x7F4A7C15 * this.hashVersion)));
        }
    }

    /// <summary>
    /// PKG2 offset calculation for KMST 1198 (V2).
    /// </summary>
    public sealed class Pkg2OffsetCalcV2 : IPkg2ImageOffsetCalc<int>
    {
        public Pkg2OffsetCalcV2(uint headerLen, uint hashVersion, uint hash1)
        {
            this.headerLen = headerLen;
            this.hashVersion = hashVersion;
            this.hash1 = hash1;
        }

        private readonly uint headerLen;
        private readonly uint hashVersion;
        private readonly uint hash1;

        public uint CalcOffset(uint filePos, uint hashedOffset)
        {
            uint offset = filePos - this.headerLen;
            offset = ~offset;
            offset *= this.hashVersion ^ this.hash1;
            offset -= 0x581C3F6D;
            offset ^= this.hash1 * 0x01010101;
            int distance = (byte)((this.hashVersion ^ this.hash1) & 0x1F);
            offset = ROL(offset, distance);
            offset ^= ~hashedOffset;
            offset += this.headerLen;
            return offset;
        }

        public int DecryptEntryCount(int encryptedEntryCount)
        {
            return (int)(encryptedEntryCount ^ ((this.hash1 << 16) - (0x21524111 * this.hashVersion)));
        }
    }

    /// <summary>
    /// PKG2 offset calculation for KMST 1199 (V3).
    /// </summary>
    public sealed class Pkg2OffsetCalcV3 : IPkg2ImageOffsetCalc<int>
    {
        public Pkg2OffsetCalcV3(uint headerLen, uint hashVersion, uint hash1)
        {
            this.headerLen = headerLen;
            this.hashVersion = hashVersion;
            this.hash1 = hash1;

            uint preHash = hash1 ^ hashVersion;
            this.preHash = preHash;
            this.mixedHash = Mix(preHash ^ 0x6D4C3B2A) ^ 0x91E10DA5;
        }

        private readonly uint headerLen;
        private readonly uint hashVersion;
        private readonly uint hash1;
        private readonly uint preHash;
        private readonly uint mixedHash;

        public uint CalcOffset(uint filePos, uint hashedOffset)
        {
            uint offset = filePos - this.headerLen;
            offset = ~offset;
            offset *= this.preHash + (this.mixedHash ^ 0xA7E3C093);
            offset -= 0x581C3F6D;
            offset ^= this.hash1 * 0x01010101;
            offset ^= this.mixedHash * 0x9E3779B9;
            int distance = (byte)((this.preHash ^ this.mixedHash) & 0x1F);
            offset = ROL(offset, distance);
            offset ^= ~hashedOffset;
            offset += this.headerLen;
            return offset;
        }

        public int DecryptEntryCount(int encryptedEntryCount)
        {
            return (int)(encryptedEntryCount ^ ((this.hash1 << 16) + (this.mixedHash & 0x7fffffff) - (0x21524111 * this.hashVersion)));
        }
    }

    /// <summary>
    /// 64-bit PKG2 offset calculation for KMST 1202.
    /// </summary>
    public sealed class Pkg2OffsetCalc64V1 : IPkg2ImageOffsetCalc<long>, IPkg2ImageLengthCalc
    {
        public Pkg2OffsetCalc64V1(uint headerLen, ulong hash1, ulong hashVersion)
        {
            // client only use low 32bits.
            this.headerLen = headerLen;
            this.hash1 = hash1;
            this.hashVersion = hashVersion;
            this.preHash = (uint)hash1 ^ (uint)hashVersion;
            this.mixedHash = this.preHash ^ 0x33BBBB33;
        }

        private readonly uint headerLen;
        private readonly ulong hash1;
        private readonly ulong hashVersion;
        private readonly uint preHash;
        private readonly uint mixedHash;

        public uint CalcOffset(uint filePos, uint hashedOffset)
        {
            uint offset = this.CalcSharedKey(filePos);
            offset ^= ~hashedOffset;
            offset += this.headerLen;
            return offset;
        }

        public int CalcLength(uint filePos, int encryptedValue)
        {
            return encryptedValue ^ unchecked((int)this.CalcSharedKey(filePos));
        }

        private uint CalcSharedKey(uint filePos)
        {
            uint key = filePos - this.headerLen;
            key = ~key;
            key *= (this.preHash + (this.mixedHash ^ 0xA7E3C093));
            key -= 0x581C3F6D;
            key ^= (uint)this.hash1 * 0x01010101;
            key ^= this.mixedHash * 0x9E3779B9;
            return ROL(key, 19);
        }

        public int DecryptEntryCount(long encryptedEntryCount)
        {
            ulong dirCount = ((ulong)encryptedEntryCount ^ this.hash1 ^ this.hashVersion ^ 0x550EC4DD02C468ECUL) >> 16;
            if (dirCount > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(encryptedEntryCount), "64-bit PKG2 dir count exceeds supported range.");
            }
            return (int)dirCount;
        }
    }

    /// <summary>
    /// 64-bit PKG2 offset calculation for KMST 1205.
    /// </summary>
    public sealed class Pkg2OffsetCalc64V2 : IPkg2ImageOffsetCalc<long>, IPkg2ImageLengthCalc
    {
        public Pkg2OffsetCalc64V2(uint headerLen, ulong hash1, ulong hashVersion)
        {
            this.headerLen = headerLen;
            this.hash1 = hash1;
            this.hashVersion = hashVersion;
        }

        private readonly uint headerLen;
        private readonly ulong hash1;
        private readonly ulong hashVersion;

        public uint CalcOffset(uint filePos, uint hashedOffset)
        {
            return Pkg2Kmst1205Hash.ComputeImageOffset(this.hash1, this.hashVersion, this.headerLen, filePos, hashedOffset);
        }

        public int CalcLength(uint filePos, int encryptedValue)
        {
            ulong key = Pkg2Kmst1205Hash.ComputeEntryFieldKey(this.hash1, this.hashVersion, filePos - this.headerLen);
            return encryptedValue ^ unchecked((int)key);
        }

        public int DecryptEntryCount(long encryptedEntryCount)
        {
            ulong key = Pkg2Kmst1205Hash.ComputeDirectoryCountKey(this.hash1, this.hashVersion);
            ulong dirCount = ((ulong)encryptedEntryCount ^ key) >> 16;
            if (dirCount > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(encryptedEntryCount),
                    "64-bit PKG2 dir count exceeds supported range.");
            }
            return (int)dirCount;
        }
    }
}