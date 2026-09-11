using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WzComparerR2.WzLib.Utilities;

namespace WzComparerR2.WzLib.Compatibility
{
    #region Format profile base

    public interface IWzFormatProfile
    {
        WzFileFormat Format { get; }
        string Name { get; }
        Wz_CryptoKeyType CryptoKeyType { get; }
        bool TryDetect(Wz_File wzFile, WzPreReadResult preReadResult);
        bool TryDetectCached(Wz_File wzFile, WzPreReadResult preReadResult, WzProfileCacheEntry cached);
        WzProfileCacheEntry CreateCacheEntry(Wz_File wzFile);
        void DetectCryptoKeyType(Wz_File wzFile, Wz_Crypto crypto, WzPreReadResult preReadResult, out Wz_CryptoKeyType pkg1KeyType, out Wz_CryptoKeyType pkg2KeyType);
        void AssignDirStringReader(Wz_File wzFile, Wz_Crypto crypto);
    }

    public interface IWzFormatProfile<THeader, THash> : IWzFormatProfile
        where THeader : Wz_Header
    {
        bool CanHandle(Wz_File wzFile, out THeader header);
        IWzVersionIterator<THash> CreateVersionIterator(THeader header);
        bool TryResolveCache(THeader header, WzProfileCacheEntry cache, out int wzVersion, out THash hashVersion);
        WzProfileCacheEntry CreateCacheEntry(int wzVersion, THash hashVersion);
        IWzImageOffsetCalc CreateOffsetCalc(THeader header, THash hashVersion);
        IPkg2ImageLengthCalc CreateLengthCalc(THeader header, THash hashVersion);
        WzFileReadContext CreateReadContext(THeader header, THash hashVersion, IWzImageOffsetCalc offsetCalc, IPkg2ImageLengthCalc imageLengthCalc);
        void DetectCryptoKeyType(Wz_File wzFile, THeader header, Wz_Crypto crypto, WzPreReadResult preReadResult, out Wz_CryptoKeyType pkg1KeyType, out Wz_CryptoKeyType pkg2KeyType);
        IPkg2DirStringReader CreateDirStringReader(THeader header, Wz_Crypto crypto, THash hashVersion);
    }

    public abstract class WzFormatProfile<THeader, THash> : IWzFormatProfile<THeader, THash>
        where THeader : Wz_Header
    {
        protected WzFormatProfile(WzFileFormat format, Wz_CryptoKeyType cryptoKeyType)
        {
            this.Format = format;
            this.CryptoKeyType = cryptoKeyType;
        }

        public WzFileFormat Format { get; }
        public abstract string Name { get; }
        public Wz_CryptoKeyType CryptoKeyType { get; }

        public abstract bool CanHandle(Wz_File wzFile, out THeader header);
        public abstract IWzVersionIterator<THash> CreateVersionIterator(THeader header);
        public abstract bool TryResolveCache(THeader header, WzProfileCacheEntry cache, out int wzVersion, out THash hashVersion);
        public abstract WzProfileCacheEntry CreateCacheEntry(int wzVersion, THash hashVersion);
        public abstract IWzImageOffsetCalc CreateOffsetCalc(THeader header, THash hashVersion);
        public abstract WzFileReadContext CreateReadContext(THeader header, THash hashVersion, IWzImageOffsetCalc offsetCalc, IPkg2ImageLengthCalc imageLengthCalc);
        protected abstract THash GetDetectedHashVersion(Wz_File wzFile, THeader header);

        public virtual IPkg2ImageLengthCalc CreateLengthCalc(THeader header, THash hashVersion)
        {
            return null;
        }

        public virtual bool TryDetect(Wz_File wzFile, WzPreReadResult preReadResult)
        {
            return WzFormatDetector.TryDetect(wzFile, preReadResult, this);
        }

        public bool TryDetectCached(Wz_File wzFile, WzPreReadResult preReadResult, WzProfileCacheEntry cached)
        {
            return WzFormatDetector.TryDetectCached(wzFile, preReadResult, this, cached);
        }

        public WzProfileCacheEntry CreateCacheEntry(Wz_File wzFile)
        {
            if (!this.CanHandle(wzFile, out var header))
                throw new InvalidOperationException($"{this.Name} cannot create a cache entry for this file.");
            return this.CreateCacheEntry(header.WzVersion, this.GetDetectedHashVersion(wzFile, header));
        }

        public void DetectCryptoKeyType(Wz_File wzFile, Wz_Crypto crypto, WzPreReadResult preReadResult, out Wz_CryptoKeyType pkg1KeyType, out Wz_CryptoKeyType pkg2KeyType)
        {
            if (!this.CanHandle(wzFile, out var header))
            {
                pkg1KeyType = Wz_CryptoKeyType.Unknown;
                pkg2KeyType = Wz_CryptoKeyType.Unknown;
                return;
            }

            this.DetectCryptoKeyType(wzFile, header, crypto, preReadResult, out pkg1KeyType, out pkg2KeyType);
        }

        public virtual void DetectCryptoKeyType(Wz_File wzFile, THeader header, Wz_Crypto crypto, WzPreReadResult preReadResult, out Wz_CryptoKeyType pkg1KeyType, out Wz_CryptoKeyType pkg2KeyType)
        {
            pkg1KeyType = Wz_CryptoKeyType.Unknown;
            pkg2KeyType = Wz_CryptoKeyType.Unknown;
        }

        public virtual IPkg2DirStringReader CreateDirStringReader(THeader header, Wz_Crypto crypto, THash hashVersion)
        {
            return null;
        }

        public virtual void AssignDirStringReader(Wz_File wzFile, Wz_Crypto crypto)
        {
        }

        #region Shared crypto detection helpers

        private static readonly Wz_CryptoKeyType[] Pkg1LegacyCandidates = { Wz_CryptoKeyType.BMS, Wz_CryptoKeyType.KMS, Wz_CryptoKeyType.GMS };

        internal static Wz_CryptoKeyType DetectPkg1CryptoKeyType(ReadOnlySpan<byte> rawBytes, WzStringEncoding encoding, Wz_Crypto crypto)
        {
            Span<byte> masked = rawBytes.Length <= 256 ? stackalloc byte[rawBytes.Length] : new byte[rawBytes.Length];
            ApplyWireMask(rawBytes, masked, encoding);
            return TryMatchKeys(masked, encoding, crypto, Pkg1LegacyCandidates);
        }

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
            return WzPreReadHelper.IsLegalNodeName(MemoryMarshal.Cast<byte, char>(bytes));
        }

        private static void ApplyWireMask(ReadOnlySpan<byte> rawBytes, Span<byte> output, WzStringEncoding encoding)
        {
            if (encoding == WzStringEncoding.ASCII)
            {
                MathHelper.ApplyWzStringByteMask(rawBytes, output);
            }
            else
            {
                MathHelper.ApplyWzStringCharMask(MemoryMarshal.Cast<byte, char>(rawBytes), MemoryMarshal.Cast<byte, char>(output));
            }
        }

        #endregion
    }

    #endregion

    /// <summary>
    /// Static registry of all known format profiles.
    /// Profiles are ordered so that the most likely match for a given WzFileFormat is tried first.
    /// </summary>
    public static class WzFormatProfiles
    {
        private static readonly IWzFormatProfile[] allProfiles = new IWzFormatProfile[]
        {
            new Pkg1Profile(),
            new Pkg2Profile64(1206, WzFileFormat.Pkg2Kmst1206, Pkg2OffsetVersion.KMST1205, Pkg2EntryNameVersion.KMST1206, Wz_CryptoKeyType.KMST1199, new Pkg2HashVersionCalc64V3(), Pkg2EntryNamePosition.AfterData),
            new Pkg2Profile64(1205, WzFileFormat.Pkg2Kmst1205, Pkg2OffsetVersion.KMST1205, Pkg2EntryNameVersion.KMST1205, Wz_CryptoKeyType.KMST1199, new Pkg2HashVersionCalc64V2(), Pkg2EntryNamePosition.AfterData),
            new Pkg2Profile64(1204, WzFileFormat.Pkg2Kmst1204, Pkg2OffsetVersion.KMST1202, Pkg2EntryNameVersion.KMST1204, Wz_CryptoKeyType.KMST1199, new Pkg2HashVersionCalc64V1()),
            new Pkg2Profile64(1202, WzFileFormat.Pkg2Kmst1202, Pkg2OffsetVersion.KMST1202, Pkg2EntryNameVersion.KMST1202, Wz_CryptoKeyType.KMST1199, new Pkg2HashVersionCalc64V1()),
            new Pkg2Profile(1201, WzFileFormat.Pkg2Kmst1201, Pkg2OffsetVersion.KMST1199, Pkg2EntryNameVersion.KMST1199, Wz_CryptoKeyType.KMST1199, new Pkg2HashVersionCalcV4()),
            new Pkg2Profile(1200, WzFileFormat.Pkg2Kmst1198, Pkg2OffsetVersion.KMST1199, Pkg2EntryNameVersion.KMST1199, Wz_CryptoKeyType.KMST1199, new Pkg2HashVersionCalcV5()),
            new Pkg2Profile(1199, WzFileFormat.Pkg2Kmst1198, Pkg2OffsetVersion.KMST1199, Pkg2EntryNameVersion.KMST1199, Wz_CryptoKeyType.KMST1199, new Pkg2HashVersionCalcV4()),
            new Pkg2Profile(1198, WzFileFormat.Pkg2Kmst1198, Pkg2OffsetVersion.KMST1198, Pkg2EntryNameVersion.KMST1198, Wz_CryptoKeyType.KMST1198, new Pkg2HashVersionCalcV3()),
            new Pkg2Profile(1197, WzFileFormat.Pkg2Kmst1196, Pkg2OffsetVersion.KMST1196, Pkg2EntryNameVersion.KMST1196, Wz_CryptoKeyType.BMS, new Pkg2HashVersionCalcV2()),
            new Pkg2Profile(1196, WzFileFormat.Pkg2Kmst1196, Pkg2OffsetVersion.KMST1196, Pkg2EntryNameVersion.KMST1196, Wz_CryptoKeyType.BMS, new Pkg2HashVersionCalcV1()),
        };

        public static IEnumerable<IWzFormatProfile> GetCandidates(WzFileFormat format)
        {
            foreach (var profile in allProfiles)
            {
                if (profile.Format == format)
                    yield return profile;
            }
        }

        public static IWzFormatProfile GetByName(string name)
        {
            foreach (var profile in allProfiles)
            {
                if (string.Equals(profile.Name, name, StringComparison.OrdinalIgnoreCase))
                    return profile;
            }
            return null;
        }
    }

    #region PKG1 profile

    public sealed class Pkg1Profile : WzFormatProfile<Wz_Header.WzPkg1Header, uint>
    {
        public Pkg1Profile() : base(WzFileFormat.Pkg1, Wz_CryptoKeyType.Unknown) { }

        public override string Name => "pkg1";

        public override bool CanHandle(Wz_File wzFile, out Wz_Header.WzPkg1Header header)
        {
            header = wzFile.Header as Wz_Header.WzPkg1Header;
            return header != null;
        }

        public override IWzVersionIterator<uint> CreateVersionIterator(Wz_Header.WzPkg1Header header)
        {
            if (header.IsEncverMissing)
                return Pkg1VersionIterator.CreateFixed(777);
            return new Pkg1VersionIterator(header.EncryptedVersion);
        }

        public override bool TryResolveCache(Wz_Header.WzPkg1Header header, WzProfileCacheEntry cache, out int wzVersion, out uint hashVersion)
        {
            wzVersion = cache.WzVersion;
            hashVersion = unchecked((uint)cache.HashKey);
            if (!string.Equals(cache.ProfileName, this.Name, StringComparison.OrdinalIgnoreCase))
                return false;

            var iterator = this.CreateVersionIterator(header);
            while (iterator.TryGetNextVersion())
            {
                if (iterator.WzVersion == wzVersion && iterator.HashVersion == hashVersion)
                    return true;
            }
            return false;
        }

        public override WzProfileCacheEntry CreateCacheEntry(int wzVersion, uint hashVersion)
        {
            return new WzProfileCacheEntry(this.Name, wzVersion, hashVersion);
        }

        public override IWzImageOffsetCalc CreateOffsetCalc(Wz_Header.WzPkg1Header header, uint hashVersion)
        {
            return new Pkg1OffsetCalc((uint)header.HeaderSize, hashVersion);
        }

        public override WzFileReadContext CreateReadContext(Wz_Header.WzPkg1Header header, uint hashVersion, IWzImageOffsetCalc offsetCalc, IPkg2ImageLengthCalc imageLengthCalc)
        {
            return new WzFileReadContext<uint>(hashVersion, hashVersion, offsetCalc, imageLengthCalc, null);
        }

        protected override uint GetDetectedHashVersion(Wz_File wzFile, Wz_Header.WzPkg1Header header)
        {
            return ((WzFileReadContext<uint>)wzFile.ReadContext).HashVersion;
        }

        public override void DetectCryptoKeyType(Wz_File wzFile, Wz_Header.WzPkg1Header header, Wz_Crypto crypto, WzPreReadResult preReadResult, out Wz_CryptoKeyType pkg1KeyType, out Wz_CryptoKeyType pkg2KeyType)
        {
            pkg2KeyType = Wz_CryptoKeyType.Unknown;
            byte[] rawBytes = preReadResult.FirstStringRawBytes;
            pkg1KeyType = rawBytes == null || rawBytes.Length == 0
                ? Wz_CryptoKeyType.Unknown
                : DetectPkg1CryptoKeyType(rawBytes, preReadResult.FirstStringEncoding, crypto);
        }
    }

    #endregion

    #region PKG2 profiles

    public sealed class Pkg2Profile : WzFormatProfile<Wz_Header.WzPkg2Header, uint>
    {
        public Pkg2Profile(int wzVersion, WzFileFormat format, Pkg2OffsetVersion offsetVersion, Pkg2EntryNameVersion entryNameVersion, Wz_CryptoKeyType cryptoKeyType, IPkg2HashVersionCalc<uint> hashVersionCalc)
            : base(format, cryptoKeyType)
        {
            this.WzVersion = wzVersion;
            this.OffsetVersion = offsetVersion;
            this.EntryNameVersion = entryNameVersion;
            this.HashVersionCalc = hashVersionCalc;
        }

        public int WzVersion { get; }
        public Pkg2OffsetVersion OffsetVersion { get; }
        public Pkg2EntryNameVersion EntryNameVersion { get; }
        public IPkg2HashVersionCalc<uint> HashVersionCalc { get; }
        public override string Name => $"pkg2_kmst{this.WzVersion}";

        public override bool CanHandle(Wz_File wzFile, out Wz_Header.WzPkg2Header header)
        {
            header = wzFile.Header as Wz_Header.WzPkg2Header;
            return header != null;
        }

        public override IWzVersionIterator<uint> CreateVersionIterator(Wz_Header.WzPkg2Header header)
        {
            uint hash1 = header.Hash1, hash2 = header.Hash2;
            var calc = this.HashVersionCalc;
            return new Pkg2VersionIterator(
                this.WzVersion,
                () => calc.CalcCandidates(hash1, hash2));
        }

        public override bool TryResolveCache(Wz_Header.WzPkg2Header header, WzProfileCacheEntry cache, out int wzVersion, out uint hashVersion)
        {
            wzVersion = cache.WzVersion;
            hashVersion = unchecked((uint)cache.HashKey);
            return string.Equals(cache.ProfileName, this.Name, StringComparison.OrdinalIgnoreCase)
                && wzVersion == this.WzVersion
                && this.HashVersionCalc.Verify(header.Hash1, header.Hash2, hashVersion);
        }

        public override WzProfileCacheEntry CreateCacheEntry(int wzVersion, uint hashVersion)
        {
            return new WzProfileCacheEntry(this.Name, wzVersion, hashVersion);
        }

        public override IWzImageOffsetCalc CreateOffsetCalc(Wz_Header.WzPkg2Header header, uint hashVersion)
        {
            uint headerSize = (uint)header.HeaderSize;
            uint hash1 = header.Hash1;
            return this.OffsetVersion switch
            {
                Pkg2OffsetVersion.KMST1196 => new Pkg2OffsetCalcV1(headerSize, hashVersion, hash1),
                Pkg2OffsetVersion.KMST1198 => new Pkg2OffsetCalcV2(headerSize, hashVersion, hash1),
                Pkg2OffsetVersion.KMST1199 => new Pkg2OffsetCalcV3(headerSize, hashVersion, hash1),
                _ => throw new ArgumentOutOfRangeException(nameof(OffsetVersion)),
            };
        }

        public override WzFileReadContext CreateReadContext(Wz_Header.WzPkg2Header header, uint hashVersion, IWzImageOffsetCalc offsetCalc, IPkg2ImageLengthCalc imageLengthCalc)
        {
            return new WzFileReadContext<uint>(hashVersion, hashVersion, offsetCalc, imageLengthCalc, Pkg2DirTreeReadRule.Instance);
        }

        protected override uint GetDetectedHashVersion(Wz_File wzFile, Wz_Header.WzPkg2Header header)
        {
            return ((WzFileReadContext<uint>)wzFile.ReadContext).HashVersion;
        }

        public override void DetectCryptoKeyType(Wz_File wzFile, Wz_Header.WzPkg2Header header, Wz_Crypto crypto, WzPreReadResult preReadResult, out Wz_CryptoKeyType pkg1KeyType, out Wz_CryptoKeyType pkg2KeyType)
        {
            pkg1KeyType = Wz_CryptoKeyType.Unknown;
            pkg2KeyType = Wz_CryptoKeyType.Unknown;

            byte[] rawBytes = preReadResult.FirstStringRawBytes;
            if (rawBytes == null || rawBytes.Length == 0)
                return;

            if (this.Format == WzFileFormat.Pkg2Kmst1196)
            {
                var keyType = DetectPkg1CryptoKeyType(rawBytes, preReadResult.FirstStringEncoding, crypto);
                pkg1KeyType = keyType;
                pkg2KeyType = keyType;
                return;
            }

            if (this.WzVersion == 1198)
            {
                if (TryMatchKey(rawBytes, WzStringEncoding.UTF16, crypto.GetKeys(Wz_CryptoKeyType.KMST1198)))
                    pkg2KeyType = Wz_CryptoKeyType.KMST1198;
            }
            else if (this.WzVersion >= 1199)
            {
                if (TryMatchKey(rawBytes, WzStringEncoding.UTF16, new Wz_Crypto.Pkg2DirStringKeyV2(header.Hash1, ((WzFileReadContext<uint>)wzFile.ReadContext).HashVersion)))
                    pkg2KeyType = Wz_CryptoKeyType.KMST1199;
            }

            byte[] secondBytes = preReadResult.SecondStringRawBytes;
            if (secondBytes != null && secondBytes.Length > 0)
            {
                pkg1KeyType = DetectPkg1CryptoKeyType(secondBytes, preReadResult.SecondStringEncoding, crypto);
            }
        }

        public override IPkg2DirStringReader CreateDirStringReader(Wz_Header.WzPkg2Header header, Wz_Crypto crypto, uint hashVersion)
        {
            IWzDecrypter pkg1Keys = crypto.Pkg1Keys ?? crypto.GetKeys(Wz_CryptoKeyType.BMS);
            return this.EntryNameVersion switch
            {
                Pkg2EntryNameVersion.KMST1196 => new Pkg2LegacyDirStringReader(crypto.Pkg2Keys),
                Pkg2EntryNameVersion.KMST1198 => new Pkg2MixedKeyDirStringReader(crypto.GetKeys(Wz_CryptoKeyType.KMST1198), pkg1Keys),
                Pkg2EntryNameVersion.KMST1199 => new Pkg2MixedKeyDirStringReader(new Wz_Crypto.Pkg2DirStringKeyV2(header.Hash1, hashVersion), pkg1Keys),
                _ => throw new ArgumentOutOfRangeException(nameof(EntryNameVersion)),
            };
        }

        public override void AssignDirStringReader(Wz_File wzFile, Wz_Crypto crypto)
        {
            if (this.CanHandle(wzFile, out var header))
            {
                wzFile.ReadContext.DirStringReader = this.CreateDirStringReader(header, crypto, ((WzFileReadContext<uint>)wzFile.ReadContext).HashVersion);
            }
        }
    }

    #endregion

    #region PKG2 64-bit profile

    /// <summary>
    /// 64-bit PKG2 profile. Currently used by KMST1202+.
    /// </summary>
    public sealed class Pkg2Profile64 : WzFormatProfile<Wz_Header.WzPkg2Header64, ulong>
    {
        public Pkg2Profile64(int wzVersion, WzFileFormat format, Pkg2OffsetVersion offsetVersion, Pkg2EntryNameVersion entryNameVersion, Wz_CryptoKeyType cryptoKeyType, IPkg2HashVersionCalc<ulong> hashVersionCalc, Pkg2EntryNamePosition entryNamePosition = Pkg2EntryNamePosition.BeforeData)
            : base(format, cryptoKeyType)
        {
            this.WzVersion = wzVersion;
            this.OffsetVersion = offsetVersion;
            this.EntryNameVersion = entryNameVersion;
            this.HashVersionCalc = hashVersionCalc;
            this.EntryNamePosition = entryNamePosition;
        }

        public int WzVersion { get; }
        public Pkg2OffsetVersion OffsetVersion { get; }
        public Pkg2EntryNameVersion EntryNameVersion { get; }
        public IPkg2HashVersionCalc<ulong> HashVersionCalc { get; }
        public Pkg2EntryNamePosition EntryNamePosition { get; }
        public override string Name => $"pkg2_kmst{this.WzVersion}";

        public override bool CanHandle(Wz_File wzFile, out Wz_Header.WzPkg2Header64 header)
        {
            header = wzFile.Header as Wz_Header.WzPkg2Header64;
            return header != null;
        }

        public override IWzVersionIterator<ulong> CreateVersionIterator(Wz_Header.WzPkg2Header64 header)
        {
            return new Pkg2VersionIterator64(this.WzVersion, header, this.HashVersionCalc);
        }

        public override bool TryResolveCache(Wz_Header.WzPkg2Header64 header, WzProfileCacheEntry cache, out int wzVersion, out ulong hashVersion)
        {
            wzVersion = cache.WzVersion;
            hashVersion = cache.HashKey;
            return string.Equals(cache.ProfileName, this.Name, StringComparison.OrdinalIgnoreCase)
                && wzVersion == this.WzVersion
                && this.HashVersionCalc.Verify(header.Hash1, header.Hash2, hashVersion);
        }

        public override WzProfileCacheEntry CreateCacheEntry(int wzVersion, ulong hashVersion)
        {
            return new WzProfileCacheEntry(this.Name, wzVersion, hashVersion);
        }

        public override IWzImageOffsetCalc CreateOffsetCalc(Wz_Header.WzPkg2Header64 header, ulong hashVersion)
        {
            return this.OffsetVersion switch
            {
                Pkg2OffsetVersion.KMST1202 => new Pkg2OffsetCalc64V1((uint)header.HeaderSize, header.Hash1, hashVersion),
                Pkg2OffsetVersion.KMST1205 => new Pkg2OffsetCalc64V2((uint)header.HeaderSize, header.Hash1, hashVersion),
                _ => throw new ArgumentOutOfRangeException(nameof(OffsetVersion)),
            };
        }

        public override IPkg2ImageLengthCalc CreateLengthCalc(Wz_Header.WzPkg2Header64 header, ulong hashVersion)
        {
            return this.Format switch
            {
                WzFileFormat.Pkg2Kmst1204 => new Pkg2OffsetCalc64V1((uint)header.HeaderSize, header.Hash1, hashVersion),
                WzFileFormat.Pkg2Kmst1205 or
                WzFileFormat.Pkg2Kmst1206 => new Pkg2OffsetCalc64V2((uint)header.HeaderSize, header.Hash1, hashVersion),
                _ => null,
            };
        }

        public override WzFileReadContext CreateReadContext(Wz_Header.WzPkg2Header64 header, ulong hashVersion, IWzImageOffsetCalc offsetCalc, IPkg2ImageLengthCalc imageLengthCalc)
        {
            IPkg2DirTreeReadRule rule = this.EntryNamePosition == Pkg2EntryNamePosition.AfterData
                ? Pkg2DirTreeReadRule64.AfterDataInstance
                : Pkg2DirTreeReadRule64.Instance;
            return new WzFileReadContext<ulong>(hashVersion, unchecked((uint)hashVersion), offsetCalc, imageLengthCalc, rule);
        }

        protected override ulong GetDetectedHashVersion(Wz_File wzFile, Wz_Header.WzPkg2Header64 header)
        {
            return ((WzFileReadContext<ulong>)wzFile.ReadContext).HashVersion;
        }

        public override void DetectCryptoKeyType(Wz_File wzFile, Wz_Header.WzPkg2Header64 header, Wz_Crypto crypto, WzPreReadResult preReadResult, out Wz_CryptoKeyType pkg1KeyType, out Wz_CryptoKeyType pkg2KeyType)
        {
            pkg1KeyType = Wz_CryptoKeyType.Unknown;
            pkg2KeyType = this.CryptoKeyType;
        }

        public override IPkg2DirStringReader CreateDirStringReader(Wz_Header.WzPkg2Header64 header, Wz_Crypto crypto, ulong hashVersion)
        {
            IWzDecrypter pkg1Keys = crypto.GetKeys(Wz_CryptoKeyType.BMS);
            return this.EntryNameVersion switch
            {
                Pkg2EntryNameVersion.KMST1202 => new Pkg2MixedKeyDirStringReader64(new Wz_Crypto.Pkg2DirStringKeyV3(header.Hash1, hashVersion), pkg1Keys),
                Pkg2EntryNameVersion.KMST1204 => new Pkg2MixedKeyDirStringReader64(new Wz_Crypto.Pkg2DirStringKeyV4(header.Hash1, hashVersion), pkg1Keys, true),
                Pkg2EntryNameVersion.KMST1205 => new Pkg2MixedKeyDirStringReader64(new Wz_Crypto.Pkg2DirStringKeyV5(header.Hash1, hashVersion), pkg1Keys, true),
                Pkg2EntryNameVersion.KMST1206 => new Pkg2MixedKeyDirStringReader64(new Wz_Crypto.Pkg2DirStringKeyV6(header.Hash1, hashVersion), pkg1Keys, true),
                _ => throw new ArgumentOutOfRangeException(nameof(EntryNameVersion)),
            };
        }

        public override void AssignDirStringReader(Wz_File wzFile, Wz_Crypto crypto)
        {
            if (this.CanHandle(wzFile, out var header))
            {
                wzFile.ReadContext.DirStringReader = this.CreateDirStringReader(header, crypto, ((WzFileReadContext<ulong>)wzFile.ReadContext).HashVersion);
            }
        }

        /// <summary>
        /// Iterator for 64-bit PKG2 hash version detection.
        /// </summary>
        private sealed class Pkg2VersionIterator64 : IWzVersionIterator<ulong>
        {
            private readonly Wz_Header.WzPkg2Header64 header;
            private readonly IPkg2HashVersionCalc<ulong> calc;
            private readonly int wzVersion;
            private IReadOnlyList<ulong> candidates;
            private int index;

            public Pkg2VersionIterator64(int wzVersion, Wz_Header.WzPkg2Header64 header, IPkg2HashVersionCalc<ulong> calc)
            {
                this.wzVersion = wzVersion;
                this.header = header;
                this.calc = calc;
                this.index = -1;
            }

            public int WzVersion => this.wzVersion;
            public ulong HashVersion { get; private set; }

            public bool TryGetNextVersion()
            {
                this.candidates ??= this.calc.CalcCandidates(header.Hash1, header.Hash2);
                if (++this.index < this.candidates.Count)
                {
                    this.HashVersion = this.candidates[this.index];
                    return true;
                }
                return false;
            }
        }
    }
    #endregion
}
