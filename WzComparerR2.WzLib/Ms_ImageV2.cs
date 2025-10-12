using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using WzComparerR2.WzLib.Cryptography;
using WzComparerR2.WzLib.Utilities;

namespace WzComparerR2.WzLib
{
    public class Ms_ImageV2 : Wz_Image
    {
        public Ms_ImageV2(string name, Ms_Entry msEntry, IMapleStoryFile msFile)
            : base(name, msEntry.Size, msEntry.CheckSum, 0, 0, msFile)
        {
            this.MsEntry = msEntry;
            this.Offset = msEntry.StartPos;
            this.IsChecksumChecked = true; // disable checksum check
        }

        public Ms_Entry MsEntry { get; private set; }

        public override int CalcCheckSum(Stream stream)
        {
            return this.MsEntry.CalculatedCheckSum;
        }

        public override Stream OpenRead()
        {
            // calc chacha20 key for entry
            uint keyHash = 0x811C9DC5;
            foreach (var c in (this.WzFile as Ms_FileV2)?.Header.KeySalt)
            {
                keyHash = (keyHash ^ c) * 0x1000193;
            }
            uint keyHash2 = keyHash >> 1;
            uint keyHash3 = keyHash2 ^ 0x6C;
            uint keyHash4 = keyHash3 << 2; // not used
            byte[] keyHashDigits = keyHash.ToString().Select(v => (byte)(v - '0')).ToArray();

            // key
            Span<byte> imgKey = stackalloc byte[32];
            string entryName = this.MsEntry.Name;
            ReadOnlySpan<byte> entryKey = this.MsEntry.Key;
            for (int i = 0; i < imgKey.Length; i++)
            {
                imgKey[i] = (byte)(i + entryName[i % entryName.Length] * (
                    keyHashDigits[i % keyHashDigits.Length] % 2
                    + entryKey[(keyHashDigits[(i + 2) % keyHashDigits.Length] + i) % entryKey.Length]
                    + (keyHashDigits[(i + 1) % keyHashDigits.Length] + i) % 5
                    ));
            }
            for (int i = 0; i < imgKey.Length; ++i)
            {
                imgKey[i] ^= Ms_FileV2.chacha20KeyObscure[i];
            }

            // nonce and counter
            Span<byte> keyHashData = stackalloc byte[12];
            Span<uint> keyHashDataUInt32 = MemoryMarshal.Cast<byte, uint>(keyHashData);
            keyHashDataUInt32[0] = keyHash;
            keyHashDataUInt32[1] = keyHash2;
            keyHashDataUInt32[2] = keyHash3;
            for (int i = 0, a = 0, b = 0, c = 90, d = 0; i < 12; ++i)
            {
                keyHashData[i] ^= (byte)(d + 11 * ((uint)i / 11) + (c ^ ((uint)i >> 2)) + (a ^ b));
                --d;
                a += 8;
                b += 17;
                c += 43;
            }
            Span<byte> nonce = stackalloc byte[ChaCha20CryptoTransform.AllowedNonceLength];
            keyHashData.Slice(0, 8).CopyTo(nonce.Slice(4));
            uint counter = MemoryMarshal.Read<uint>(keyHashData.Slice(8, 4));

            using var ps = new PartialStream(this.WzFile.FileStream, this.MsEntry.StartPos, this.MsEntry.SizeAligned, true);
            int cryptedSize = Math.Min(this.MsEntry.Size, 1024);
            var buffer = new byte[cryptedSize];
            var part1 = new MemoryStream(buffer);
            ps.Position = 0;

            // decrypt initial 1024 bytes
            {
                var cs = new CryptoStream(ps, new ChaCha20CryptoTransform(imgKey, nonce, counter), CryptoStreamMode.Read);
                cs.ReadExactly(buffer);
            }

            if (this.MsEntry.Size <= 1024)
            {
                return part1;
            }
            else
            {
                var part2 = new PartialStream(this.WzFile.FileStream, this.MsEntry.StartPos + 1024, this.MsEntry.SizeAligned - 1024, true);
                return new ConcatenatedStream(part1, part2);
            }
        }
    }
}
