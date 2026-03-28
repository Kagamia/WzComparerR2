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
    internal sealed class Pkg2KmstDirStringReader : IPkg2DirStringReader
    {
        public Pkg2KmstDirStringReader(IWzDecrypter pkg2Keys, IWzDecrypter pkg1Keys)
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
}
