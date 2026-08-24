using static WzComparerR2.WzLib.Utilities.MathHelper;

namespace WzComparerR2.WzLib.Compatibility
{
    internal static class Pkg2Kmst1205Hash
    {
        private const ulong Magic1 = 0x84CAA73B2BB70682UL;
        private const ulong Magic2 = 0x510E527FADE682D1UL;
        private const ulong Magic3 = 0xBF58476D1CE4E5B9UL;
        private const ulong Magic4 = 0x94D049BB133111EBUL;
        private const ulong Magic5 = 0x2545F4914F6CDD1DUL;
        private const ulong Magic6 = 0x6A09E667F3BCC908UL;
        private const uint Magic7 = 0x2545F491u;
        private const uint Magic8 = 0x85EBCA77u;

        private static ulong MultiplyXorRight(ulong value, ulong multiplier, int shift)
        {
            return multiplier * XorShiftRight(value, shift);
        }

        private static ulong RotateMultiplyXorRight30(ulong value)
        {
            return ROR(MultiplyXorRight(value, Magic3, 30), 27);
        }

        private static ulong MultiplyRotateXorRight25And47(ulong value)
        {
            return 0x9FB21C651E98DF25UL * (value ^ ROR(value, 25) ^ ROR(value, 47));
        }

        private static ulong MultiplyRotateXorLeft23And41(ulong value)
        {
            return Magic5 * (value ^ ROL(value, 23) ^ ROL(value, 41));
        }

        private static ulong MultiplyXorRight27(ulong value)
        {
            return MultiplyXorRight(value, Magic4, 27);
        }

        public static ulong ComputeSharedHash(ulong hash1, ulong hashVersion)
        {
            ulong tmp1 =ROL(hash1 ^ 0x81B4A01224AAB10CUL, 31);
            tmp1 = MultiplyXorRight(tmp1, 0xFF51AFD7ED558CCDUL, 33);
            tmp1 = MultiplyXorRight(tmp1, 0xC4CEB9FE1A85EC53UL, 29);
            tmp1 = XorShiftRight(tmp1, 32);

            ulong temp2 = RotateMultiplyXorRight30((hashVersion - 0x2E4AB5CD2E6D12FDUL) ^ Magic1);
            temp2 = XorShiftRight(MultiplyXorRight27(temp2), 31);

            ulong value = MultiplyRotateXorRight25And47(tmp1 + temp2 + Magic2);
            value = tmp1 ^ value ^ ROL(temp2, 17) ^ (value >> 28);
            value = MultiplyXorRight(value, Magic4, 29);
            value = XorShiftRight(value, 32);
            return value;
        }

        public static ulong ComputeHash2(ulong hash1, ulong hashVersion)
        {
            ulong sharedHash = ComputeSharedHash(hash1, hashVersion);
            ulong value = RotateMultiplyXorRight30(sharedHash ^ Magic1);
            value = MultiplyXorRight27(value);
            return XorShiftRight(value, 31);
        }

        public static ulong ComputeDirectoryCountKey(ulong hash1, ulong hashVersion)
        {
            ulong tmp1 = RotateMultiplyXorRight30(hash1 ^ Magic1);
            ulong tmp2 = MultiplyXorRight27(tmp1);
            tmp1 = MultiplyRotateXorRight25And47(hashVersion + Magic2);
            tmp1 = XorShiftRight(tmp1, 28) ^ (tmp2 >> 31);
            tmp1 = tmp1 ^ tmp2;

            ulong key = (hashVersion + hash1) ^ Magic6;
            key = MultiplyRotateXorLeft23And41(key);
            key = Magic3 * XorShiftRight(key, 32);
            key = XorShiftRight(key, 29) ^ (tmp1 + ROL(tmp1, 23));
            return XorShiftRight(key, 31);
        }

        public static ulong ComputeDirEntryNameKey(ulong hash1, ulong hashVersion, ulong filePos)
        {
            ulong sharedHash = ComputeSharedHash(hash1, hashVersion);
            ulong positionHash = 0xD1B54A32D192ED03UL * filePos;
            positionHash ^= ROL(positionHash, 32);

            ulong tmp1 = positionHash + sharedHash + Magic2;
            tmp1 = MultiplyRotateXorRight25And47(tmp1);
            ulong tmp2 = positionHash ^ sharedHash ^ Magic6;
            tmp2 = MultiplyRotateXorLeft23And41(tmp2);

            ulong key = Magic3 * XorShiftRight(tmp2, 32);
            key = XorShiftRight(tmp1, 28) ^ XorShiftRight(key, 29);
            key = ROL(key, (int)(positionHash & 0x3F));
            return XorShiftRight(key, 29);
        }

        public static uint ComputeEntryFieldKey(ulong hash1, ulong hashVersion, uint filePos)
        {
            uint mixed = XorFold64To32(ComputeSharedHash(hash1, hashVersion));
            uint hash1Low = (uint)hash1;
            uint hashVersionLow = (uint)hashVersion;
            uint key = mixed
                + Magic7 * XorShiftRight(filePos, 15)
                + (hash1Low ^ hashVersionLow);
            key = ROL(key, (int)((mixed ^ hash1Low) & 0x1F));
            key = XorShiftRight(Magic8 * key, 13);
            key = key - 0x3D4D51C3u * mixed;
            return key ^ ROL(key, 16);
        }

        public static uint ComputeImageOffset(ulong hash1, ulong hashVersion, uint headerLen, uint filePos, uint hashedOffset)
        {
            uint mixed = (uint)ComputeSharedHash(hash1, hashVersion);
            uint hash1Low = (uint)hash1;
            uint hashVersionLow = (uint)hashVersion;
            uint key = (mixed ^ Magic7) + (hash1Low ^ hashVersionLow);
            key = key * ~(filePos - headerLen) + Magic7;
            key = key ^ (0xC2B2AE3Du * mixed)
                ^ (Magic8 * hash1Low);
            key = ROL(key, (int)((mixed ^ hash1Low ^ hashVersionLow) & 0x1F));
            return (~hashedOffset ^ key) + headerLen;
        }
    }
}
