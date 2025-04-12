using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using WzComparerR2.WzLib.Utilities;


#if NET6_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

namespace WzComparerR2.WzLib
{
    public class Wz_Png
    {
        public Wz_Png(int w, int h, int data_length, Wz_TextureFormat format, int scale, int pages, uint offs, Wz_Image wz_i)
        {
            this.Width = w;
            this.Height = h;
            this.DataLength = data_length;
            this.Format = format;
            this.Scale = scale;
            this.Pages = pages;
            this.Offset = offs;
            this.WzImage = wz_i;
        }

        /// <summary>
        /// 获取或设置图片的宽度。
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// 获取或设置图片的高度。
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// 获取或设置数据块的长度。
        /// </summary>
        public int DataLength { get; set; }

        /// <summary>
        /// 获取或设置数据块对于文件的偏移。
        /// </summary>
        public uint Offset { get; set; }

        /// <summary>
        /// 获取或设置图片的数据压缩方式。
        /// </summary>
        public int Form => (int)this.Format + this.Scale;

        public Wz_TextureFormat Format { get; set; }

        /// <summary>
        /// The actual width and height sacale is pow(2, value).
        /// </summary>
        public int Scale { get; set; }

        public int Pages { get; set; }

        /// <summary>
        /// 获取或设置图片所属的WzFile
        /// </summary>
        public IMapleStoryFile WzFile
        {
            get { return this.WzImage?.WzFile; }
        }

        /// <summary>
        /// 获取或设置图片所属的WzImage
        /// </summary>
        public Wz_Image WzImage { get; set; }

        public int GetRawDataSize() => GetUncompressedDataSize(this.Format, this.Scale > 0 ? (1 << this.Scale) : 0, this.Width, this.Height);

        public byte[] GetRawData()
        {
            int dataSize = this.GetRawDataSize();
            byte[] rawData = new byte[dataSize];
            int count = this.GetRawData(rawData);
            if (count != dataSize)
            {
                throw new Exception($"Data size mismatch. (expected: {dataSize}, actual: {count})");
            }
            return rawData;
        }

        public int GetRawData(Span<byte> buffer)
        {
            lock (this.WzFile.ReadLock)
            {
                using (var zlib = this.UnsafeOpenRead())
                {
                    return zlib.ReadAvailableBytes(buffer);
                }
            }
        }

        public Stream UnsafeOpenRead()
        {
            DeflateStream zlib;

            var stream = this.WzImage.OpenRead();
            long endPosition = this.Offset + this.DataLength;
            stream.Position = this.Offset + 1; // skip the first byte
            Span<byte> zlibHeader = stackalloc byte[2];
            stream.ReadExactly(zlibHeader);

            if (MemoryMarshal.Read<ushort>(zlibHeader) == 0x9C78) //TODO: more generic zlib header validation
            {
                Stream dataStream = new PartialStream(stream, stream.Position, endPosition - stream.Position, true);
                zlib = new DeflateStream(dataStream, CompressionMode.Decompress);
            }
            else
            {
                stream.Position -= 2;
                Stream dataStream = new PartialStream(stream, stream.Position, endPosition - stream.Position, true);
                Stream chunkStream = new ChunkedEncryptedInputStream(dataStream, this.WzImage.EncKeys);
                chunkStream.ReadExactly(zlibHeader);
                zlib = new DeflateStream(chunkStream, CompressionMode.Decompress);
            }

            return zlib;
        }

        public Bitmap ExtractPng()
        {
            byte[] pixel = this.GetRawData();
            Bitmap pngDecoded = null;
            BitmapData bmpdata;
            byte[] argb;

            switch (this.Form)
            {
                case 1: //16位argb4444
                    pngDecoded = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
                    bmpdata = pngDecoded.LockBits(new Rectangle(Point.Empty, pngDecoded.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    unsafe
                    {
                        Span<byte> output = new Span<byte>(bmpdata.Scan0.ToPointer(), bmpdata.Stride * bmpdata.Height);
                        GetPixelDataBgra4444(pixel, this.Width, this.Height, output);
                    }
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                case 2: //32位argb8888
                    pngDecoded = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
                    bmpdata = pngDecoded.LockBits(new Rectangle(Point.Empty, pngDecoded.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(pixel, 0, bmpdata.Scan0, pixel.Length);
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                case 3: //黑白缩略图
                    argb = GetPixelDataForm3(pixel, this.Width, this.Height);
                    pngDecoded = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
                    bmpdata = pngDecoded.LockBits(new Rectangle(Point.Empty, pngDecoded.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(argb, 0, bmpdata.Scan0, argb.Length);
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                case 257: //16位argb1555
                    pngDecoded = new Bitmap(this.Width, this.Height, PixelFormat.Format16bppArgb1555);
                    bmpdata = pngDecoded.LockBits(new Rectangle(Point.Empty, pngDecoded.Size), ImageLockMode.WriteOnly, PixelFormat.Format16bppArgb1555);
                    CopyBmpDataWithStride(pixel, pngDecoded.Width * 2, bmpdata);
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                case 513: //16位rgb565
                    pngDecoded = new Bitmap(this.Width, this.Height, PixelFormat.Format16bppRgb565);
                    bmpdata = pngDecoded.LockBits(new Rectangle(new Point(), pngDecoded.Size), ImageLockMode.WriteOnly, PixelFormat.Format16bppRgb565);
                    CopyBmpDataWithStride(pixel, pngDecoded.Width * 2, bmpdata);
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                case 517: //16位rgb565缩略图
                    argb = GetPixelDataForm517(pixel, this.Width, this.Height);
                    pngDecoded = new Bitmap(this.Width, this.Height, PixelFormat.Format16bppRgb565);
                    bmpdata = pngDecoded.LockBits(new Rectangle(0, 0, this.Width, this.Height), ImageLockMode.WriteOnly, PixelFormat.Format16bppRgb565);
                    Marshal.Copy(argb, 0, bmpdata.Scan0, argb.Length);
                    pngDecoded.UnlockBits(bmpdata);
                    break;
                /* pngDecoded = new Bitmap(this.w, this.h);
                 pngSize = this.w * this.h / 128;
                 plainData = new byte[pngSize];
                 zlib.Read(plainData, 0, pngSize);
                 byte iB = 0;
                 for (int i = 0; i < pngSize; i++)
                 {
                     for (byte j = 0; j < 8; j++)
                     {
                         iB = Convert.ToByte(((plainData[i] & (0x01 << (7 - j))) >> (7 - j)) * 0xFF);
                         for (int k = 0; k < 16; k++)
                         {
                             if (x == this.w) { x = 0; y++; }
                             pngDecoded.SetPixel(x, y, Color.FromArgb(0xFF, iB, iB, iB));
                             x++;
                         }
                     }
                 }
                 break;*/

                case 1026: //dxt3
                    argb = GetPixelDataDXT3(pixel, this.Width, this.Height);
                    pngDecoded = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
                    bmpdata = pngDecoded.LockBits(new Rectangle(new Point(), pngDecoded.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(argb, 0, bmpdata.Scan0, argb.Length);
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                case 2050: //dxt5
                    argb = GetPixelDataDXT5(pixel, this.Width, this.Height);
                    pngDecoded = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
                    bmpdata = pngDecoded.LockBits(new Rectangle(new Point(), pngDecoded.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(argb, 0, bmpdata.Scan0, argb.Length);
                    pngDecoded.UnlockBits(bmpdata);
                    break;
            }

            return pngDecoded;
        }

        public static byte[] GetPixelDataBgra4444(byte[] rawData, int width, int height)
        {
            byte[] argb = new byte[width * height * 4];
            GetPixelDataBgra4444(rawData, width, height, argb);
            return argb;
        }

        public static void GetPixelDataBgra4444(ReadOnlySpan<byte> rawData, int width, int height, Span<byte> output)
        {
            if (output.Length < width * height * 4)
            {
                throw new ArgumentException($"Output buffer requires at least {width * height * 4} bytes.", nameof(output));
            }
#if NET6_0_OR_GREATER
            /*
                      0        1        2        3
              data    ggggbbbb aaaarrrr -------- --------
              xmm0 = unpack_low(data, data)
                      ggggbbbb ggggbbbb aaaarrrr aaaarrrr
              xmm1 = (ushort[])xmm0 >> 4
                      bbbbgggg 0000gggg rrrraaaa 0000aaaa
              xmm0 &= 0F F0 0F F0
                      0000bbbb gggg0000 0000rrrr aaaa0000
              xmm1 &= F0 0F F0 0F
                      bbbb0000 0000gggg rrrr0000 0000aaaa
              xmm0 |= xmm1
                      bbbbbbbb gggggggg rrrrrrrr aaaaaaaa
            */
            if (rawData.Length >= 16 && Avx2.IsSupported)
            {
                var mask0 = Vector256.Create(
                        0x0f, 0xf0, 0x0f, 0xf0, 0x0f, 0xf0, 0x0f, 0xf0,
                        0x0f, 0xf0, 0x0f, 0xf0, 0x0f, 0xf0, 0x0f, 0xf0,
                        0x0f, 0xf0, 0x0f, 0xf0, 0x0f, 0xf0, 0x0f, 0xf0,
                        0x0f, 0xf0, 0x0f, 0xf0, 0x0f, 0xf0, 0x0f, 0xf0);
                var mask1 = Vector256.Create(
                        0xf0, 0x0f, 0xf0, 0x0f, 0xf0, 0x0f, 0xf0, 0x0f,
                        0xf0, 0x0f, 0xf0, 0x0f, 0xf0, 0x0f, 0xf0, 0x0f,
                        0xf0, 0x0f, 0xf0, 0x0f, 0xf0, 0x0f, 0xf0, 0x0f,
                        0xf0, 0x0f, 0xf0, 0x0f, 0xf0, 0x0f, 0xf0, 0x0f);
                Vector128<byte> xmm;

                unsafe
                {
                    while (rawData.Length >= 16)
                    {
                        fixed (byte* pRawData = rawData)
                            xmm = Sse2.LoadVector128(pRawData);
                        var ymm0 = Vector256.Create(Avx.UnpackLow(xmm, xmm), Avx.UnpackHigh(xmm, xmm));
                        var ymm1 = Avx2.ShiftRightLogical(ymm0.AsUInt16(), 4).AsByte();
                        var ymm2 = Avx2.Or(Avx2.And(ymm0, mask0), Avx2.And(ymm1, mask1));
                        fixed (byte* pOutput = output)
                            Avx.Store(pOutput, ymm2);
                        rawData = rawData.Slice(16);
                        output = output.Slice(32);
                    }
                }
            }
#endif
            int p;
            for (int i = 0; i < rawData.Length; i++)
            {
                p = rawData[i] & 0x0F; p |= (p << 4); output[i * 2] = (byte)p;
                p = rawData[i] & 0xF0; p |= (p >> 4); output[i * 2 + 1] = (byte)p;
            }
        }

        public static byte[] GetPixelDataDXT3(byte[] rawData, int width, int height)
        {
            byte[] pixel = new byte[width * height * 4];

            Color[] colorTable = new Color[4];
            int[] colorIdxTable = new int[16];
            byte[] alphaTable = new byte[16];
            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 4)
                {
                    int off = x * 4 + y * width;
                    ExpandAlphaTableDXT3(alphaTable, rawData, off);
                    ushort u0 = BitConverter.ToUInt16(rawData, off + 8);
                    ushort u1 = BitConverter.ToUInt16(rawData, off + 10);
                    ExpandColorTable(colorTable, u0, u1);
                    ExpandColorIndexTable(colorIdxTable, rawData, off + 12);

                    for (int j = 0; j < 4; j++)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            SetPixel(pixel,
                                x + i,
                                y + j,
                                width,
                                colorTable[colorIdxTable[j * 4 + i]],
                                alphaTable[j * 4 + i]);
                        }
                    }
                }
            }

            return pixel;
        }

        public static byte[] GetPixelDataDXT5(byte[] rawData, int width, int height)
        {
            byte[] pixel = new byte[width * height * 4];

            Color[] colorTable = new Color[4];
            int[] colorIdxTable = new int[16];
            byte[] alphaTable = new byte[8];
            int[] alphaIdxTable = new int[16];
            for (int y = 0; y < height; y += 4)
            {
                for (int x = 0; x < width; x += 4)
                {
                    int off = x * 4 + y * width;
                    ExpandAlphaTableDXT5(alphaTable, rawData[off + 0], rawData[off + 1]);
                    ExpandAlphaIndexTableDXT5(alphaIdxTable, rawData, off + 2);
                    ushort u0 = BitConverter.ToUInt16(rawData, off + 8);
                    ushort u1 = BitConverter.ToUInt16(rawData, off + 10);
                    ExpandColorTable(colorTable, u0, u1);
                    ExpandColorIndexTable(colorIdxTable, rawData, off + 12);

                    for (int j = 0; j < 4; j++)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            SetPixel(pixel,
                                x + i,
                                y + j,
                                width,
                                colorTable[colorIdxTable[j * 4 + i]],
                                alphaTable[alphaIdxTable[j * 4 + i]]);
                        }
                    }
                }
            }

            return pixel;
        }

        public static unsafe byte[] GetPixelDataForm3(byte[] rawData, int width, int height)
        {
            byte[] pixel = new byte[width * height * 4];
            fixed (byte* pArray = pixel)
            {
                int* argb2 = (int*)pArray;
                int w = ((int)Math.Ceiling(width / 4.0));
                int h = ((int)Math.Ceiling(height / 4.0));
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        var index = (x + y * w) * 2; //原像素索引
                        var index2 = x * 4 + y * width * 4; //目标像素索引
                        var p = (rawData[index] & 0x0F) | ((rawData[index] & 0x0F) << 4)
                            | ((rawData[index] & 0xF0) | ((rawData[index] & 0xF0) >> 4)) << 8
                            | ((rawData[index + 1] & 0x0F) | ((rawData[index + 1] & 0x0F) << 4)) << 16
                            | ((rawData[index + 1] & 0xF0) | ((rawData[index + 1] & 0xF0) >> 4)) << 24;

                        for (int i = 0; i < 4; i++)
                        {
                            if (x * 4 + i < width)
                            {
                                argb2[index2 + i] = p;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    //复制行
                    var srcIndex = y * width * 4 * 4;
                    var dstIndex = srcIndex + width * 4;
                    for (int j = 1; j < 4; j++)
                    {
                        if (y * 4 + j < height)
                        {
                            Array.Copy(pixel, srcIndex, pixel, dstIndex, width * 4);
                            dstIndex += width * 4;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            return pixel;
        }

        public static byte[] GetPixelDataForm517(byte[] rawData, int width, int height)
        {
            byte[] pixel = new byte[width * height * 2];
            int lineIndex = 0;
            for (int j0 = 0, j1 = height / 16; j0 < j1; j0++)
            {
                var dstIndex = lineIndex;
                for (int i0 = 0, i1 = width / 16; i0 < i1; i0++)
                {
                    int idx = (i0 + j0 * i1) * 2;
                    byte b0 = rawData[idx];
                    byte b1 = rawData[idx + 1];
                    for (int k = 0; k < 16; k++)
                    {
                        pixel[dstIndex++] = b0;
                        pixel[dstIndex++] = b1;
                    }
                }

                for (int k = 1; k < 16; k++)
                {
                    Array.Copy(pixel, lineIndex, pixel, dstIndex, width * 2);
                    dstIndex += width * 2;
                }

                lineIndex += width * 32;
            }
            return pixel;
        }

        private static void SetPixel(byte[] pixelData, int x, int y, int width, Color color, byte alpha)
        {
            int offset = (y * width + x) * 4;
            pixelData[offset + 0] = color.B;
            pixelData[offset + 1] = color.G;
            pixelData[offset + 2] = color.R;
            pixelData[offset + 3] = alpha;
        }

        #region DXT1 Color
        private static void ExpandColorTable(Color[] color, ushort c0, ushort c1)
        {
            color[0] = RGB565ToColor(c0);
            color[1] = RGB565ToColor(c1);
            if (c0 > c1)
            {
                color[2] = Color.FromArgb(0xff, (color[0].R * 2 + color[1].R + 1) / 3, (color[0].G * 2 + color[1].G + 1) / 3, (color[0].B * 2 + color[1].B + 1) / 3);
                color[3] = Color.FromArgb(0xff, (color[0].R + color[1].R * 2 + 1) / 3, (color[0].G + color[1].G * 2 + 1) / 3, (color[0].B + color[1].B * 2 + 1) / 3);
            }
            else
            {
                color[2] = Color.FromArgb(0xff, (color[0].R + color[1].R) / 2, (color[0].G + color[1].G) / 2, (color[0].B + color[1].B) / 2);
                color[3] = Color.FromArgb(0xff, Color.Black);
            }
        }

        private static void ExpandColorIndexTable(int[] colorIndex, byte[] rawData, int offset)
        {
            for (int i = 0; i < 16; i += 4, offset++)
            {
                colorIndex[i + 0] = (rawData[offset] & 0x03);
                colorIndex[i + 1] = (rawData[offset] & 0x0c) >> 2;
                colorIndex[i + 2] = (rawData[offset] & 0x30) >> 4;
                colorIndex[i + 3] = (rawData[offset] & 0xc0) >> 6;
            }
        }
        #endregion

        #region DXT3/DXT5 Alpha
        private static void ExpandAlphaTableDXT3(byte[] alpha, byte[] rawData, int offset)
        {
            for (int i = 0; i < 16; i += 2, offset++)
            {
                alpha[i + 0] = (byte)(rawData[offset] & 0x0f);
                alpha[i + 1] = (byte)((rawData[offset] & 0xf0) >> 4);
            }
            for (int i = 0; i < 16; i++)
            {
                alpha[i] = (byte)(alpha[i] | (alpha[i] << 4));
            }
        }

        private static void ExpandAlphaTableDXT5(byte[] alpha, byte a0, byte a1)
        {
            alpha[0] = a0;
            alpha[1] = a1;
            if (a0 > a1)
            {
                for (int i = 2; i < 8; i++)
                {
                    alpha[i] = (byte)(((8 - i) * a0 + (i - 1) * a1 + 3) / 7);
                }
            }
            else
            {
                for (int i = 2; i < 6; i++)
                {
                    alpha[i] = (byte)(((6 - i) * a0 + (i - 1) * a1 + 2) / 5);
                }
                alpha[6] = 0;
                alpha[7] = 255;
            }
        }

        private static void ExpandAlphaIndexTableDXT5(int[] alphaIndex, byte[] rawData, int offset)
        {
            for (int i = 0; i < 16; i += 8, offset += 3)
            {
                int flags = rawData[offset]
                    | (rawData[offset + 1] << 8)
                    | (rawData[offset + 2] << 16);
                for (int j = 0; j < 8; j++)
                {
                    int mask = 0x07 << (3 * j);
                    alphaIndex[i + j] = (flags & mask) >> (3 * j);
                }
            }
        }
        #endregion

        public static Color RGB565ToColor(ushort val)
        {
            const int rgb565_mask_r = 0xf800;
            const int rgb565_mask_g = 0x07e0;
            const int rgb565_mask_b = 0x001f;
            int r = (val & rgb565_mask_r) >> 11;
            int g = (val & rgb565_mask_g) >> 5;
            int b = (val & rgb565_mask_b);
            var c = Color.FromArgb(
                (r << 3) | (r >> 2),
                (g << 2) | (g >> 4),
                (b << 3) | (b >> 2));
            return c;
        }

        public static void CopyBmpDataWithStride(byte[] source, int stride, BitmapData bmpData)
        {
            if (bmpData.Stride == stride)
            {
                Marshal.Copy(source, 0, bmpData.Scan0, source.Length);
            }
            else
            {
                for (int y = 0; y < bmpData.Height; y++)
                {
                    Marshal.Copy(source, stride * y, bmpData.Scan0 + bmpData.Stride * y, stride);
                }
            }
        }

        public static int GetUncompressedDataSize(Wz_TextureFormat format, int scale, int width, int height)
        {
            if (scale > 0)
            {
                if ((width % scale) != 0 || (height & scale) != 0)
                {
                    throw new ArgumentException("Width or height cannot be divided by scale");
                }
                width /= scale;
                height /= scale;
            }
            return GetUncompressedDataSize(format, width, height);
        }

        public static int GetUncompressedDataSize(Wz_TextureFormat format, int width, int height)
        {
            return format switch
            {
                Wz_TextureFormat.ARGB4444 or
                Wz_TextureFormat.ARGB1555 or
                Wz_TextureFormat.RGB565 => width * height * 2,

                Wz_TextureFormat.ARGB8888 or
                Wz_TextureFormat.RGBA1010102 => width * height * 4,

                Wz_TextureFormat.DXT3 or
                Wz_TextureFormat.DXT5 or
                Wz_TextureFormat.BC7 => ((width + 3) / 4) * ((height + 3) / 4) * 16,

                Wz_TextureFormat.DXT1 => ((width + 3) / 4) * ((height + 3) / 4) * 8,

                Wz_TextureFormat.A8 => width * height,

                Wz_TextureFormat.RGBA32Float => width * height * 16,

                _ => throw new ArgumentException($"Unknown texture format {(int)format}.")
            };
        }
    }

    public enum Wz_TextureFormat
    {
        Unknown = 0,
        ARGB4444 = 1,
        ARGB8888 = 2,
        ARGB1555 = 257,
        RGB565 = 513,
        DXT3 = 1026,
        DXT5 = 2050,
        // introduced in KMST 1186
        A8 = 2304,
        RGBA1010102 = 2562,
        DXT1 = 4097,
        BC7 = 4098,
        RGBA32Float = 4100,
    }
}
