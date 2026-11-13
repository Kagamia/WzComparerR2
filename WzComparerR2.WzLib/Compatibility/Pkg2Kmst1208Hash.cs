using static WzComparerR2.WzLib.Utilities.MathHelper;

namespace WzComparerR2.WzLib.Compatibility
{
    internal static class Pkg2Kmst1208Hash
    {
        public const int HeaderSize = 383;

        private const ulong Magic1 = 0x348E9CFE0D833B4BUL;
        private const ulong Magic2 = 0x03D871A903B6DBB7UL;
        private const ulong Magic3 = 0xA5521FD3FCB4057BUL;
        private const ulong Magic4 = 0x5C8E961F7B7BA523UL;
        private const ulong Magic5 = 0x8B5ADDDCC28F4A1FUL;
        private const ulong Magic6 = 0xBF2F2B4EFECC7BE1UL;
        private const ulong Magic7 = 0x0D64EE103D36E75BUL;
        private const ulong Magic8 = 0x35EEB10256AA6BD7UL;
        private const ulong Magic9 = 0xDADB824F046C2EFFUL;
        private const ulong Magic10 = 0x00207B38132A02ECUL;

        private static ulong MultiplyXorRight(ulong value, ulong multiplier, int shift)
        {
            return multiplier * XorShiftRight(value, shift);
        }

        private static ulong MultiplyRotateXorRight27And44(ulong value)
        {
            return Magic1 * (value ^ ROR(value, 27) ^ ROR(value, 44));
        }

        private static ulong MultiplyXorRight34And32(ulong value)
        {
            value = MultiplyXorRight(value, Magic2, 34);
            return MultiplyXorRight(value, Magic3, 32);
        }

        private static ulong MixXorRight31AndRotateRight27(ulong value)
        {
            value = ROR(MultiplyXorRight(value, Magic4, 31), 27);
            return MultiplyXorRight(value, Magic5, 31);
        }

        private static ulong MixRotateXorLeft18And41(ulong value)
        {
            value = Magic6 * (value ^ ROL(value, 18) ^ ROL(value, 41));
            return MultiplyXorRight(value, Magic7, 34);
        }

        public static ulong ComputeSharedHash(ulong hash1, ulong hashVersion)
        {
            ulong headerHash = MultiplyRotateXorRight27And44(hash1 - 0x716D61B446224A6DUL);
            headerHash = XorShiftRight(headerHash, 28);
            ulong versionValue = ROL(hashVersion ^ 0x1E1F8E71B938A7E1UL, 28);
            ulong versionHash = XorShiftRight(MultiplyXorRight34And32(versionValue), 31);
            ulong mixed = MixXorRight31AndRotateRight27(headerHash ^ versionHash ^ Magic8);
            ulong value = versionHash ^ mixed ^ ROR(headerHash, 19) ^ (mixed >> 30);
            value = MultiplyXorRight(value, 0x71E6EFA7890B49D7UL, 28);
            return XorShiftRight(value, 34);
        }

        public static ulong ComputeHash2(ulong hash1, ulong hashVersion)
        {
            ulong sharedHash = ComputeSharedHash(hash1, hashVersion);
            ulong mixed = MixRotateXorLeft18And41(sharedHash ^ 0xC565BDCD03A98872UL);
            ulong rotated = ROL(sharedHash ^ Magic10, 28);
            rotated = XorShiftRight(MultiplyXorRight34And32(rotated), 31);
            return mixed ^ ROL(rotated, 29) ^ (mixed >> 27);
        }

        public static ulong ComputeDirectoryCountKey(ulong hashVersion, ulong hash1)
        {
            ulong headerHash = MixRotateXorLeft18And41(hash1 ^ Magic9);
            ulong versionValue = ROL(hashVersion ^ Magic10, 28);
            ulong versionHash = MultiplyXorRight34And32(versionValue);
            ulong mixed = headerHash ^ versionHash ^ ((headerHash ^ (versionHash >> 4)) >> 27);
            ulong value = (hash1 ^ hashVersion ^ 0x067F5742CA97D7F9UL) + 0x77979B713DBB074AUL;
            value = MultiplyRotateXorRight27And44(value);
            value = XorShiftRight(value, 28) ^ (mixed + ROR(mixed, 28));
            return XorShiftRight(value, 28);
        }

        public static ulong ComputeDirEntryNameKey(ulong sharedHash, uint filePos)
        {
            ulong positionHash = 0xE4150772966F4E81UL * filePos;
            positionHash ^= ROR(positionHash, 25);
            ulong mixed = MixXorRight31AndRotateRight27((positionHash + sharedHash) ^ Magic8);
            ulong rotated = MixRotateXorLeft18And41(sharedHash ^ ROL(positionHash, 32) ^ Magic9);
            ulong value = 0x56E95F804A4CD699UL * (mixed ^ rotated ^ ((rotated ^ (mixed >> 3)) >> 27));
            return XorShiftRight(value, 31);
        }

        public static uint ComputeEntryFieldKey(ulong hashVersion, ulong hash1, ulong sharedHash, int filePos)
        {
            uint mixed = (uint)((sharedHash >> 7) ^ (sharedHash >> 32));
            uint key = mixed + (uint)(hash1 ^ (hashVersion >> 32)) + XorShiftRight((uint)(294617447 * filePos), 12);
            key = ROR(key, (int)(((uint)(hash1 >> 32) ^ (mixed >> 5)) & 0x1F));
            key = 1021519063u * key;
            uint value = 277150939u * mixed + XorShiftRight(key, 13);
            return value ^ ROL(value, 16);
        }

        public static byte DecryptNodeType(ulong hashVersion, ulong hash1, ulong sharedHash, int filePos, byte encryptedNodeType)
        {
            uint key = ComputeEntryFieldKey(hashVersion, hash1, sharedHash, filePos);
            return (byte)(encryptedNodeType ^ ((key >> 11) & 0xFF));
        }

        public static uint ComputeImageOffset(uint headerLen, ulong hashVersion, ulong hash1, ulong sharedHash, int filePos, uint hashedOffset)
        {
            uint mixed = (uint)(sharedHash >> 17);
            uint key = mixed + (uint)(hashVersion >> 16) ^ (uint)(-1918831151 * filePos);
            key = ROL(key, (int)((mixed ^ (uint)((hash1 >> 40) & 0xFF)) & 0x1F));
            key = 1442009253u * key;
            uint offset = ROR(hashedOffset, 11) ^ XorShiftRight(key, 15) ^ (uint)(1030949161UL * hash1);
            return headerLen + offset;
        }
    }
}