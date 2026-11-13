using System;
using System.IO;
using WzComparerR2.WzLib.Utilities;

namespace WzComparerR2.WzLib.Compatibility
{
    public abstract class WzFileReadContext
    {
        protected WzFileReadContext(uint compatibilityHashVersion, IWzImageOffsetCalc offsetCalc, IPkg2ImageLengthCalc lengthCalc, IPkg2DirTreeReadRule pkg2DirTreeRule)
        {
            this.CompatibilityHashVersion = compatibilityHashVersion;
            this.OffsetCalc = offsetCalc;
            this.LengthCalc = lengthCalc;
            this.Pkg2DirTreeRule = pkg2DirTreeRule;
        }

        public uint CompatibilityHashVersion { get; }
        public IWzImageOffsetCalc OffsetCalc { get; }
        public IPkg2ImageLengthCalc LengthCalc { get; }
        public IPkg2DirTreeReadRule Pkg2DirTreeRule { get; }
        public IPkg2DirStringReader DirStringReader { get; set; }
    }

    public sealed class WzFileReadContext<THash> : WzFileReadContext
    {
        public WzFileReadContext(THash hashVersion, uint compatibilityHashVersion, IWzImageOffsetCalc offsetCalc, IPkg2DirTreeReadRule pkg2DirTreeRule)
            : this(hashVersion, compatibilityHashVersion, offsetCalc, null, pkg2DirTreeRule)
        {
        }

        public WzFileReadContext(THash hashVersion, uint compatibilityHashVersion, IWzImageOffsetCalc offsetCalc, IPkg2ImageLengthCalc lengthCalc, IPkg2DirTreeReadRule pkg2DirTreeRule)
            : base(compatibilityHashVersion, offsetCalc, lengthCalc, pkg2DirTreeRule)
        {
            this.HashVersion = hashVersion;
        }

        public THash HashVersion { get; }
    }

    public interface IPkg2DirTreeReadRule
    {
        Pkg2EntryFieldOrder EntryFieldOrder { get; }
        bool ReverseOffsetOrder { get; }
        bool IsNodeTypeEncrypted { get; }
        int ReadEntryCount(WzBinaryReader reader, IWzImageOffsetCalc offsetCalc);
        bool ShouldReadOffsets(WzBinaryReader reader, IWzImageOffsetCalc offsetCalc, int actualEntryCount);
    }

    public enum Pkg2EntryFieldOrder
    {
        TypeNameSizeChecksum,
        TypeSizeChecksumName,
        TypeChecksumNameSize,
    }

    internal sealed class Pkg2DirTreeReadRule : IPkg2DirTreeReadRule
    {
        public static readonly Pkg2DirTreeReadRule Instance = new Pkg2DirTreeReadRule();

        private Pkg2DirTreeReadRule()
        {
        }

        public Pkg2EntryFieldOrder EntryFieldOrder => Pkg2EntryFieldOrder.TypeNameSizeChecksum;
        public bool ReverseOffsetOrder => false;
        public bool IsNodeTypeEncrypted => false;

        public int ReadEntryCount(WzBinaryReader reader, IWzImageOffsetCalc offsetCalc)
        {
            int encryptedEntryCount = reader.ReadCompressedInt32();
            var pkg2Calc = GetOffsetCalc(offsetCalc);
            return pkg2Calc.DecryptEntryCount(encryptedEntryCount);
        }

        public bool ShouldReadOffsets(WzBinaryReader reader, IWzImageOffsetCalc offsetCalc, int actualEntryCount)
        {
            int encryptedOffsetCount = reader.ReadCompressedInt32();
            var pkg2Calc = GetOffsetCalc(offsetCalc);
            return actualEntryCount > 0
                && pkg2Calc.DecryptEntryCount(encryptedOffsetCount) == actualEntryCount;
        }

        private static IPkg2ImageOffsetCalc<int> GetOffsetCalc(IWzImageOffsetCalc offsetCalc)
        {
            if (offsetCalc is IPkg2ImageOffsetCalc<int> pkg2Calc)
                return pkg2Calc;
            throw new InvalidOperationException("32-bit PKG2 dir tree reading requires a 32-bit PKG2 offset calculator.");
        }
    }

    internal sealed class Pkg2DirTreeReadRule64 : IPkg2DirTreeReadRule
    {
        public static readonly Pkg2DirTreeReadRule64 Instance = new Pkg2DirTreeReadRule64();
        public static readonly Pkg2DirTreeReadRule64 NameLastInstance = new Pkg2DirTreeReadRule64(Pkg2EntryFieldOrder.TypeSizeChecksumName);

        private Pkg2DirTreeReadRule64(Pkg2EntryFieldOrder entryFieldOrder = Pkg2EntryFieldOrder.TypeNameSizeChecksum)
        {
            this.EntryFieldOrder = entryFieldOrder;
        }

        public Pkg2EntryFieldOrder EntryFieldOrder { get; }
        public bool ReverseOffsetOrder => false;
        public bool IsNodeTypeEncrypted => false;

        public int ReadEntryCount(WzBinaryReader reader, IWzImageOffsetCalc offsetCalc)
        {
            long encryptedEntryCount = reader.ReadCompressedInt64();
            var pkg2Calc = offsetCalc as IPkg2ImageOffsetCalc<long>
                ?? throw new InvalidOperationException("64-bit PKG2 dir tree reading requires a 64-bit PKG2 offset calculator.");
            return pkg2Calc.DecryptEntryCount(encryptedEntryCount);
        }

        public bool ShouldReadOffsets(WzBinaryReader reader, IWzImageOffsetCalc offsetCalc, int actualEntryCount)
        {
            return true;
        }
    }

    internal sealed class Pkg2DirTreeReadRule1208 : IPkg2DirTreeReadRule
    {
        public static readonly Pkg2DirTreeReadRule1208 Instance = new Pkg2DirTreeReadRule1208();

        private Pkg2DirTreeReadRule1208()
        {
        }

        public Pkg2EntryFieldOrder EntryFieldOrder => Pkg2EntryFieldOrder.TypeChecksumNameSize;
        public bool ReverseOffsetOrder => true;
        public bool IsNodeTypeEncrypted => true;

        public int ReadEntryCount(WzBinaryReader reader, IWzImageOffsetCalc offsetCalc)
        {
            return Pkg2DirTreeReadRule64.Instance.ReadEntryCount(reader, offsetCalc);
        }

        public bool ShouldReadOffsets(WzBinaryReader reader, IWzImageOffsetCalc offsetCalc, int actualEntryCount)
        {
            return true;
        }
    }
}
