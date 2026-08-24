using WzComparerR2.WzLib.Utilities;

namespace WzComparerR2.WzLib.Compatibility
{
    /// <summary>
    /// Reads directory entry names in PKG2 files, encapsulating version-specific string encoding and key selection.
    /// </summary>
    public interface IPkg2DirStringReader
    {
        string ReadName(WzBinaryReader reader, bool isFirstEntry);
    }

    public enum Pkg2EntryNameVersion
    {
        KMST1196,
        KMST1198,
        KMST1199,
        KMST1202,
        KMST1204,
        KMST1205,
    }

    /// <summary>
    /// PKG2 legacy (KMST 1196-1197): all entries use ReadString with the same key.
    /// </summary>
    internal sealed class Pkg2LegacyDirStringReader : IPkg2DirStringReader
    {
        public Pkg2LegacyDirStringReader(IWzDecrypter keys)
        {
            this.keys = keys;
        }

        private readonly IWzDecrypter keys;

        public string ReadName(WzBinaryReader reader, bool isFirstEntry)
        {
            return reader.ReadString(keys);
        }
    }

    /// <summary>
    /// PKG2 KMST 1198+: first entry uses ReadPkg2DirString with pkg2 key, rest use ReadString with pkg1 key.
    /// </summary>
    internal sealed class Pkg2MixedKeyDirStringReader : IPkg2DirStringReader
    {
        public Pkg2MixedKeyDirStringReader(IWzDecrypter pkg2Keys, IWzDecrypter pkg1Keys)
        {
            this.pkg2Keys = pkg2Keys;
            this.pkg1Keys = pkg1Keys;
        }

        private readonly IWzDecrypter pkg2Keys;
        private readonly IWzDecrypter pkg1Keys;

        public string ReadName(WzBinaryReader reader, bool isFirstEntry)
        {
            return isFirstEntry ? reader.ReadPkg2DirString(pkg2Keys) : reader.ReadString(pkg1Keys);
        }
    }

    /// <summary>
    /// 64-bit PKG2: entries can use ReadPkg2DirStringV2 (16bit length) with pkg2 key, depending on the format version.
    /// </summary>
    internal sealed class Pkg2MixedKeyDirStringReader64 : IPkg2DirStringReader
    {
        public Pkg2MixedKeyDirStringReader64(IWzDecrypter firstNameKey, IWzDecrypter pkg1Keys, bool allNamesUseV2 = false)
        {
            this.firstNameKey = firstNameKey;
            this.pkg1Keys = pkg1Keys;
            this.allNamesUseV2 = allNamesUseV2;
        }

        private readonly IWzDecrypter firstNameKey;
        private readonly IWzDecrypter pkg1Keys;
        private readonly bool allNamesUseV2;

        public string ReadName(WzBinaryReader reader, bool isFirstEntry)
        {
            if (this.allNamesUseV2 || isFirstEntry)
            {
                 // The reader's base stream must start from dirStartOffset.
                if (this.firstNameKey is IWzStatefulDecrypter statefulDecrypter)
                    statefulDecrypter.ApplyState((ulong)reader.BaseStream.Position);

                return reader.ReadPkg2DirStringV2(firstNameKey);
            }

            return reader.ReadString(pkg1Keys);
        }
    }
}
