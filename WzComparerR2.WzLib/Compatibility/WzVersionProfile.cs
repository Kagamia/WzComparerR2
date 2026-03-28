using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using WzComparerR2.WzLib.Utilities;

namespace WzComparerR2.WzLib.Compatibility
{
    public abstract class WzVersionProfile
    {
        protected WzVersionProfile(WzFileFormat format, Wz_CryptoKeyType cryptoKeyType)
        {
            this.Format = format;
            this.CryptoKeyType = cryptoKeyType;
        }

        public WzFileFormat Format { get; }
        public abstract string Name { get; }
        public Wz_CryptoKeyType CryptoKeyType { get; }
        public abstract IWzVersionIterator CreateIterator(Wz_File wzFile);
        public abstract bool TryDetect(Wz_File wzFile, WzPreReadResult preReadResult, IWzVersionIterator iterator);
        public abstract IWzImageOffsetCalc CreateOffsetCalc(Wz_File wzFile, uint hashVersion);

        public virtual IWzImageOffsetCalc CreateOffsetCalc(Wz_File wzFile)
        {
            return CreateOffsetCalc(wzFile, wzFile.Header.HashVersion);
        }

        public abstract void DetectCryptoKeyType(Wz_File wzFile, Wz_Crypto crypto, WzPreReadResult preReadResult, out Wz_CryptoKeyType pkg1KeyType, out Wz_CryptoKeyType pkg2KeyType);

        #region Shared crypto detection helpers

        private static readonly Wz_CryptoKeyType[] Pkg1LegacyCandidates = { Wz_CryptoKeyType.BMS, Wz_CryptoKeyType.KMS, Wz_CryptoKeyType.GMS };

        /// <summary>
        /// Apply wire mask (0xAA+i for ASCII, 0xAAAA+i for UTF16) then try each candidate key.
        /// Called by both profile detection and Wz_Crypto fallback.
        /// </summary>
        internal static Wz_CryptoKeyType DetectPkg1CryptoKeyType(ReadOnlySpan<byte> rawBytes, WzStringEncoding encoding, Wz_Crypto crypto)
        {
            Span<byte> masked = rawBytes.Length <= 256 ? stackalloc byte[rawBytes.Length] : new byte[rawBytes.Length];
            ApplyWireMask(rawBytes, masked, encoding);
            return TryMatchKeys(masked, encoding, crypto, Pkg1LegacyCandidates);
        }

        /// <summary>
        /// Try a single decrypter on raw bytes (no wire mask).
        /// </summary>
        protected static bool TryMatchKey(ReadOnlySpan<byte> sourceBytes, WzStringEncoding encoding, IWzDecrypter decrypter)
        {
            Span<byte> buf = sourceBytes.Length <= 256 ? stackalloc byte[sourceBytes.Length] : new byte[sourceBytes.Length];
            decrypter.Decrypt(sourceBytes, buf);
            return IsDecryptedStringLegal(buf, encoding);
        }

        private static Wz_CryptoKeyType TryMatchKeys(ReadOnlySpan<byte> sourceBytes, WzStringEncoding encoding, Wz_Crypto crypto, Wz_CryptoKeyType[] candidates)
        {
            Span<byte> buf = sourceBytes.Length <= 256 ? stackalloc byte[sourceBytes.Length] : new byte[sourceBytes.Length];
            foreach (var keyType in candidates)
            {
                crypto.GetKeys(keyType).Decrypt(sourceBytes, buf);
                if (IsDecryptedStringLegal(buf, encoding))
                    return keyType;
            }
            return Wz_CryptoKeyType.Unknown;
        }

        private static bool IsDecryptedStringLegal(ReadOnlySpan<byte> bytes, WzStringEncoding encoding)
        {
            if (encoding == WzStringEncoding.ASCII)
            {
                int len = bytes.Length;
                Span<char> chars = len <= 256 ? stackalloc char[len] : new char[len];
                for (int i = 0; i < len; i++) chars[i] = (char)bytes[i];
                return WzPreReadHelper.IsLegalNodeName(chars);
            }
            else
            {
                return WzPreReadHelper.IsLegalNodeName(MemoryMarshal.Cast<byte, char>(bytes));
            }
        }

        private static void ApplyWireMask(ReadOnlySpan<byte> rawBytes, Span<byte> output, WzStringEncoding encoding)
        {
            if (encoding == WzStringEncoding.ASCII)
            {
                MathHelper.XorBytes(rawBytes, output);
            }
            else
            {
                MathHelper.XorChars(MemoryMarshal.Cast<byte, char>(rawBytes), MemoryMarshal.Cast<byte, char>(output));
            }
        }

        #endregion
    }

    /// <summary>
    /// Static registry of all known version profiles.
    /// Profiles are ordered so that the most likely match for a given WzFileFormat is tried first.
    /// </summary>
    public static class WzVersionProfiles
    {
        private static readonly WzVersionProfile[] allProfiles = new WzVersionProfile[]
        {
            new Pkg1Profile(),
            new Pkg2Profile(1199, WzFileFormat.Pkg2Kmst1198, Pkg2OffsetVersion.KMST1199, Wz_CryptoKeyType.KMST1199, new Pkg2HashVersionCalcV4()),
            new Pkg2Profile(1198, WzFileFormat.Pkg2Kmst1198, Pkg2OffsetVersion.KMST1198, Wz_CryptoKeyType.KMST1198, new Pkg2HashVersionCalcV3()),
            new Pkg2Profile(1197, WzFileFormat.Pkg2Kmst1196, Pkg2OffsetVersion.KMST1196, Wz_CryptoKeyType.BMS, new Pkg2HashVersionCalcV2()),  // BMS/KMS/GMS detected separately
            new Pkg2Profile(1196, WzFileFormat.Pkg2Kmst1196, Pkg2OffsetVersion.KMST1196, Wz_CryptoKeyType.BMS, new Pkg2HashVersionCalcV1()),
        };

        public static IEnumerable<WzVersionProfile> GetCandidates(WzFileFormat format)
        {
            foreach (var p in allProfiles)
            {
                if (p.Format == format)
                    yield return p;
            }
        }

        public static WzVersionProfile GetByName(string name)
        {
            foreach (var p in allProfiles)
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                    return p;
            }
            return null;
        }
    }

    #region PKG1 profile

    public sealed class Pkg1Profile : WzVersionProfile
    {
        public Pkg1Profile() : base(WzFileFormat.Pkg1, Wz_CryptoKeyType.Unknown) { }

        public override string Name => "pkg1";

        public override IWzVersionIterator CreateIterator(Wz_File wzFile)
        {
            var pkg1 = (Wz_Header.WzPkg1Header)wzFile.Header;
            if (pkg1.IsEncverMissing)
                return Pkg1VersionIterator.CreateFixed(777);
            return new Pkg1VersionIterator(pkg1.EncryptedVersion);
        }

        public override bool TryDetect(Wz_File wzFile, WzPreReadResult preReadResult, IWzVersionIterator iterator)
        {
            if (preReadResult.Format != WzFileFormat.Pkg1)
                return false;

            return WzVersionDetectHelper.FastDetectWithPreReadNodes(wzFile, preReadResult, iterator,
                (f, hv) => this.CreateOffsetCalc(f, hv));
        }

        public override IWzImageOffsetCalc CreateOffsetCalc(Wz_File wzFile, uint hashVersion)
        {
            return new Pkg1OffsetCalc((uint)wzFile.Header.HeaderSize, hashVersion);
        }

        public override void DetectCryptoKeyType(Wz_File wzFile, Wz_Crypto crypto, WzPreReadResult preReadResult, out Wz_CryptoKeyType pkg1KeyType, out Wz_CryptoKeyType pkg2KeyType)
        {
            pkg2KeyType = Wz_CryptoKeyType.Unknown;

            byte[] rawBytes = preReadResult.FirstStringRawBytes;
            if (rawBytes == null || rawBytes.Length == 0)
            {
                pkg1KeyType = Wz_CryptoKeyType.Unknown;
                return;
            }

            pkg1KeyType = DetectPkg1CryptoKeyType(rawBytes, preReadResult.FirstStringEncoding, crypto);
        }
    }

    #endregion

    #region PKG2 profiles

    public sealed class Pkg2Profile : WzVersionProfile
    {
        public Pkg2Profile(
            int wzVersion,
            WzFileFormat format,
            Pkg2OffsetVersion offsetVersion,
            Wz_CryptoKeyType cryptoKeyType,
            IPkg2HashVersionCalc hashVersionCalc)
            : base(format, cryptoKeyType)
        {
            this.WzVersion = wzVersion;
            this.OffsetVersion = offsetVersion;
            this.HashVersionCalc = hashVersionCalc;
        }

        public int WzVersion { get; }
        public Pkg2OffsetVersion OffsetVersion { get; }
        public IPkg2HashVersionCalc HashVersionCalc { get; }

        public override string Name => $"pkg2_kmst{this.WzVersion}";

        public override IWzVersionIterator CreateIterator(Wz_File wzFile)
        {
            var pkg2 = (Wz_Header.WzPkg2Header)wzFile.Header;
            uint hash1 = pkg2.Hash1, hash2 = pkg2.Hash2;
            var calc = this.HashVersionCalc;
            return new Pkg2VersionIterator(
                this.WzVersion,
                () => calc.CalcCandidates(hash1, hash2),
                hv => calc.Verify(hash1, hash2, hv));
        }

        public override bool TryDetect(Wz_File wzFile, WzPreReadResult preReadResult, IWzVersionIterator iterator)
        {
            if (preReadResult.Format != this.Format)
                return false;

            return WzVersionDetectHelper.FastDetectWithPreReadNodes(wzFile, preReadResult, iterator,
                (f, hv) => this.CreateOffsetCalc(f, hv));
        }

        public override IWzImageOffsetCalc CreateOffsetCalc(Wz_File wzFile, uint hashVersion)
        {
            uint headerLen = (uint)wzFile.Header.HeaderSize;
            uint hash1 = ((Wz_Header.WzPkg2Header)wzFile.Header).Hash1;
            return this.OffsetVersion switch
            {
                Pkg2OffsetVersion.KMST1196 => new Pkg2OffsetCalcV1(headerLen, hashVersion, hash1),
                Pkg2OffsetVersion.KMST1198 => new Pkg2OffsetCalcV2(headerLen, hashVersion, hash1),
                Pkg2OffsetVersion.KMST1199 => new Pkg2OffsetCalcV3(headerLen, hashVersion, hash1),
                _ => throw new ArgumentOutOfRangeException(nameof(OffsetVersion)),
            };
        }

        public override void DetectCryptoKeyType(Wz_File wzFile, Wz_Crypto crypto, WzPreReadResult preReadResult, out Wz_CryptoKeyType pkg1KeyType, out Wz_CryptoKeyType pkg2KeyType)
        {
            pkg1KeyType = Wz_CryptoKeyType.Unknown;
            pkg2KeyType = Wz_CryptoKeyType.Unknown;

            byte[] rawBytes = preReadResult.FirstStringRawBytes;
            if (rawBytes == null || rawBytes.Length == 0)
                return;

            if (this.Format == WzFileFormat.Pkg2Kmst1196)
            {
                // Legacy PKG2: same wire-mask + BMS/KMS/GMS probing as PKG1
                var keyType = DetectPkg1CryptoKeyType(rawBytes, preReadResult.FirstStringEncoding, crypto);
                pkg1KeyType = keyType;
                pkg2KeyType = keyType;
                return;
            }

            // KMST 1198+: detect pkg2 key from first string
            if (this.WzVersion == 1198)
            {
                if (TryMatchKey(rawBytes, WzStringEncoding.UTF16, crypto.GetKeys(Wz_CryptoKeyType.KMST1198)))
                    pkg2KeyType = Wz_CryptoKeyType.KMST1198;
            }
            else if (this.WzVersion >= 1199 && wzFile.Header.VersionChecked)
            {
                uint hash1 = (wzFile.Header as Wz_Header.WzPkg2Header)?.Hash1 ?? 0;
                uint hashVersion = wzFile.Header.HashVersion;
                if (TryMatchKey(rawBytes, WzStringEncoding.UTF16, new Wz_Crypto.Pkg2DirStringKeyV2(hash1, hashVersion)))
                    pkg2KeyType = Wz_CryptoKeyType.KMST1199;
            }

            // Detect pkg1 key from second string (standard encoding)
            byte[] secondBytes = preReadResult.SecondStringRawBytes;
            if (secondBytes != null && secondBytes.Length > 0)
            {
                pkg1KeyType = DetectPkg1CryptoKeyType(secondBytes, preReadResult.SecondStringEncoding, crypto);
            }
        }

        /// <summary>
        /// Create a directory string reader for this profile, used by ReadDirTreePkg2.
        /// Must be called after crypto detection has completed.
        /// </summary>
        public IPkg2DirStringReader CreateDirStringReader(Wz_File wzFile, Wz_Crypto crypto)
        {
            if (this.Format == WzFileFormat.Pkg2Kmst1196)
            {
                return new Pkg2LegacyDirStringReader(crypto.Pkg2Keys);
            }

            IWzDecrypter pkg2Keys;
            if (this.CryptoKeyType == Wz_CryptoKeyType.KMST1199)
            {
                var pkg2 = (Wz_Header.WzPkg2Header)wzFile.Header;
                pkg2Keys = new Wz_Crypto.Pkg2DirStringKeyV2(pkg2.Hash1, wzFile.Header.HashVersion);
            }
            else
            {
                pkg2Keys = crypto.Pkg2Keys;
            }
            var pkg1Keys = crypto.Pkg1Keys ?? crypto.GetKeys(Wz_CryptoKeyType.BMS);
            return new Pkg2KmstDirStringReader(pkg2Keys, pkg1Keys);
        }
    }

    #endregion
}
