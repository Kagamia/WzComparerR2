using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using WzComparerR2.WzLib.Cryptography;
using WzComparerR2.WzLib.Utilities;

namespace WzComparerR2.WzLib
{
    public class Ms_Image : Wz_Image
    {
        public Ms_Image(string name, Ms_Entry msEntry, IMapleStoryFile msFile)
            : base(name, msEntry.Size, msEntry.CheckSum, 0, 0, msFile)
        {
            this.MsEntry = msEntry;
            this.Offset = msEntry.StartPos;
        }

        public Ms_Entry MsEntry { get; private set; }

        public override int CalcCheckSum(Stream stream)
        {
            return this.MsEntry.CalculatedCheckSum;
        }

        public override Stream OpenRead()
        {
            // calc snow key for entry
            uint keyHash = 0x811C9DC5;
            foreach (var c in (this.WzFile as Ms_File)?.Header.KeySalt)
            {
                keyHash = (keyHash ^ c) * 0x1000193;
            }
            byte[] keyHashDigits = keyHash.ToString().Select(v => (byte)(v - '0')).ToArray();

            Span<byte> imgKey = stackalloc byte[16];
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

            using var ps = new PartialStream(this.WzFile.FileStream, this.MsEntry.StartPos, this.MsEntry.SizeAligned, true);
            var buffer = new byte[this.MsEntry.Size];
            Span<byte> span = buffer;
            ps.Position = 0;
            var cs = new CryptoStream(ps, new Snow2CryptoTransform(imgKey, null, false), CryptoStreamMode.Read);

            // decrypt initial 1024 bytes twice
            {
                var cs2 = new CryptoStream(cs, new Snow2CryptoTransform(imgKey, null, false), CryptoStreamMode.Read);
                int dataLen = Math.Min(span.Length, 1024);
                cs2.ReadExactly(span.Slice(0, dataLen));
                span = span.Slice(dataLen);
            }

            // decrypt subsequent bytes
            if (span.Length > 0)
            {
                cs.ReadExactly(span);
            }

            var ms = new MemoryStream(buffer);
            return ms;
        }
    }
}
