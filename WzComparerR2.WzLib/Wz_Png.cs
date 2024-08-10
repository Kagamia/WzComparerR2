using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WzComparerR2.WzLib
{
    public class Wz_Png
    {
        public Wz_Png(int w, int h, int data_length, int form, uint offs, Wz_Image wz_i)
        {
            this.w = w;
            this.h = h;
            this.data_length = data_length;
            this.form = form;
            this.offs = offs;
            this.wz_i = wz_i;
        }

        private int w;
        private int h;
        private int data_length;
        private int form;
        private uint offs;
        private Wz_Image wz_i;

        /// <summary>
        /// 获取或设置图片的宽度。
        /// </summary>
        public int Width
        {
            get { return w; }
            set { w = value; }
        }

        /// <summary>
        /// 获取或设置图片的高度。
        /// </summary>
        public int Height
        {
            get { return h; }
            set { h = value; }
        }

        /// <summary>
        /// 获取或设置数据块的长度。
        /// </summary>
        public int DataLength
        {
            get { return data_length; }
            set { data_length = value; }
        }

        /// <summary>
        /// 获取或设置数据块对于文件的偏移。
        /// </summary>
        public uint Offset
        {
            get { return offs; }
            set { offs = value; }
        }

        /// <summary>
        /// 获取或设置图片的数据压缩方式。
        /// </summary>
        public int Form
        {
            get { return form; }
            set { form = value; }
        }

        /// <summary>
        /// 获取或设置图片所属的WzFile
        /// </summary>
        public IMapleStoryFile WzFile
        {
            get { return wz_i?.WzFile; }
        }

        /// <summary>
        /// 获取或设置图片所属的WzImage
        /// </summary>
        public Wz_Image WzImage
        {
            get { return wz_i; }
            set { wz_i = value; }
        }

        public byte[] GetRawData()
        {
            lock (this.WzFile.ReadLock)
            {
                DeflateStream zlib;
                byte[] plainData = null;

                var stream = this.WzImage.OpenRead();
                long endPosition = this.Offset + this.DataLength;
                stream.Position = this.Offset + 1; // skip the first byte
                var bReader = new BinaryReader(stream);

                if (bReader.ReadUInt16() == 0x9C78)
                {
                    byte[] buffer = bReader.ReadBytes((int)(endPosition - stream.Position));
                    MemoryStream dataStream = new MemoryStream(buffer, false);

                    zlib = new DeflateStream(dataStream, CompressionMode.Decompress);
                }
                else
                {
                    stream.Position -= 2;
                    byte[] buffer = new byte[this.DataLength];
                    int dataEndOffset = 0;
                    
                    var encKeys = this.WzImage.EncKeys;

                    while (stream.Position < endPosition)
                    {
                        int blockSize = bReader.ReadInt32();
                        if (stream.Position + blockSize > endPosition)
                        {
                            throw new Exception($"Wz_Png exceeds the declared data size. (data length: {this.DataLength}, readed bytes: {dataEndOffset}, next block: {blockSize})");
                        }
                        bReader.Read(buffer, dataEndOffset, blockSize);
                        encKeys.Decrypt(buffer, dataEndOffset, blockSize);

                        dataEndOffset += blockSize;
                    }
                    var dataStream = new MemoryStream(buffer, 0, dataEndOffset, false);
                    dataStream.Position = 2;
                    zlib = new DeflateStream(dataStream, CompressionMode.Decompress);
                }

                switch (this.Form)
                {
                    case 1:
                    case 257:
                    case 513:
                        plainData = new byte[this.w * this.h * 2];
                        ReadAvailableBytes(zlib, plainData, 0, plainData.Length);
                        break;

                    case 2:
                        plainData = new byte[this.w * this.h * 4];
                        ReadAvailableBytes(zlib, plainData, 0, plainData.Length);
                        break;

                    case 3:
                        plainData = new byte[((int)Math.Ceiling(this.w / 4.0)) * 4 * ((int)Math.Ceiling(this.h / 4.0)) * 4 / 8];
                        ReadAvailableBytes(zlib, plainData, 0, plainData.Length);
                        break;

                    case 517:
                        plainData = new byte[this.w * this.h / 128];
                        ReadAvailableBytes(zlib, plainData, 0, plainData.Length);
                        break;

                    case 1026:
                    case 2050:
                        plainData = new byte[this.w * this.h];
                        ReadAvailableBytes(zlib, plainData, 0, plainData.Length);
                        break;

                    default:
                        var msOut = new MemoryStream();
                        zlib.CopyTo(msOut);
                        plainData = msOut.ToArray();
                        break;
                }
                if (zlib != null)
                {
                    zlib.Close();
                }
                return plainData;
            }
        }

        public Bitmap ExtractPng()
        {
            byte[] pixel = this.GetRawData();
            if (pixel == null)
            {
                return null;
            }
            Bitmap pngDecoded = null;
            BitmapData bmpdata;
            byte[] argb;

            switch (this.form)
            {
                case 1: //16位argb4444
                    argb = GetPixelDataBgra4444(pixel, this.w, this.h);
                    pngDecoded = new Bitmap(this.w, this.h, PixelFormat.Format32bppArgb);
                    bmpdata = pngDecoded.LockBits(new Rectangle(Point.Empty, pngDecoded.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(argb, 0, bmpdata.Scan0, argb.Length);
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                case 2: //32位argb8888
                    pngDecoded = new Bitmap(this.w, this.h, PixelFormat.Format32bppArgb);
                    bmpdata = pngDecoded.LockBits(new Rectangle(Point.Empty, pngDecoded.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(pixel, 0, bmpdata.Scan0, pixel.Length);
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                case 3: //黑白缩略图
                    argb = GetPixelDataForm3(pixel, this.w, this.h);
                    pngDecoded = new Bitmap(this.w, this.h, PixelFormat.Format32bppArgb);
                    bmpdata = pngDecoded.LockBits(new Rectangle(Point.Empty, pngDecoded.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(argb, 0, bmpdata.Scan0, argb.Length);
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                case 257: //16位argb1555
                    pngDecoded = new Bitmap(this.w, this.h, PixelFormat.Format16bppArgb1555);
                    bmpdata = pngDecoded.LockBits(new Rectangle(Point.Empty, pngDecoded.Size), ImageLockMode.WriteOnly, PixelFormat.Format16bppArgb1555);
                    CopyBmpDataWithStride(pixel, pngDecoded.Width * 2, bmpdata);
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                case 513: //16位rgb565
                    pngDecoded = new Bitmap(this.w, this.h, PixelFormat.Format16bppRgb565);
                    bmpdata = pngDecoded.LockBits(new Rectangle(new Point(), pngDecoded.Size), ImageLockMode.WriteOnly, PixelFormat.Format16bppRgb565);
                    CopyBmpDataWithStride(pixel, pngDecoded.Width * 2, bmpdata);
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                case 517: //16位rgb565缩略图
                    argb = GetPixelDataForm517(pixel, this.w, this.h);
                    pngDecoded = new Bitmap(this.w, this.h, PixelFormat.Format16bppRgb565);
                    bmpdata = pngDecoded.LockBits(new Rectangle(0, 0, this.w, this.h), ImageLockMode.WriteOnly, PixelFormat.Format16bppRgb565);
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
                    argb = GetPixelDataDXT3(pixel, this.w, this.h);
                    pngDecoded = new Bitmap(this.w, this.h, PixelFormat.Format32bppArgb);
                    bmpdata = pngDecoded.LockBits(new Rectangle(new Point(), pngDecoded.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    Marshal.Copy(argb, 0, bmpdata.Scan0, argb.Length);
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                case 2050: //dxt5
                    argb = GetPixelDataDXT5(pixel, this.w, this.h);
                    pngDecoded = new Bitmap(this.w, this.h, PixelFormat.Format32bppArgb);
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
            {
                int p;
                for (int i = 0; i < rawData.Length; i++)
                {
                    p = rawData[i] & 0x0F; p |= (p << 4); argb[i * 2] = (byte)p;
                    p = rawData[i] & 0xF0; p |= (p >> 4); argb[i * 2 + 1] = (byte)p;
                }
            }
            return argb;
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
                for(int i = 2; i < 8; i++)
                {
                    alpha[i] = (byte)(((8 - i) * a0 + (i - 1) * a1 + 3) / 7);
                }
            }
            else
            {
                for(int i = 2; i < 6; i++)
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
                for(int j = 0; j < 8; j++)
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

        private static int ReadAvailableBytes(Stream inputStream, byte[] array, int offset, int count)
        {
            // this is a wrapper function that to make sure always reading as much as requested from zlib stream;
            // https://github.com/Kagamia/WzComparerR2/issues/195
            int totalRead = 0;
            while (count > 0)
            {
                int bytesRead = inputStream.Read(array, offset, count);
                if (bytesRead == 0) break;
                totalRead += bytesRead;
                offset += bytesRead;
                count -= bytesRead;
            }
            return totalRead;
        }
    }
}
