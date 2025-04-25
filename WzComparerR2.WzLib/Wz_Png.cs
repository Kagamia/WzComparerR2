using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
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
        [Obsolete]
        public int Form => (int)this.Format + this.Scale;

        public Wz_TextureFormat Format { get; set; }

        public int Scale { get; set; }

        /// <summary>
        /// The actual width and height sacale is pow(2, value).
        /// </summary>
        public int ActualScale => this.Scale > 0 ? (1 << this.Scale) : 1;

        public int Pages { get; set; }

        public int ActualPages => this.Pages > 0 ? this.Pages : 1;

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

        public int GetRawDataSize() => this.GetRawDataSizePerPage() * this.ActualPages;

        public int GetRawDataSizePerPage() => GetUncompressedDataSize(this.Format, this.ActualScale, this.Width, this.Height);

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
            return this.GetRawData(0, buffer);
        }

        public int GetRawData(int skipBytes, Span<byte> buffer)
        {
            lock (this.WzFile.ReadLock)
            {
                using (var zlib = this.UnsafeOpenRead())
                {
                    if (skipBytes > 0)
                    {
                        var pool = ArrayPool<byte>.Shared;
                        byte[] tempBuffer = pool.Rent(4096);
                        try
                        {
                            while (skipBytes > 0)
                            {
                                int len = zlib.Read(tempBuffer, 0, (int)Math.Min(skipBytes, tempBuffer.Length));
                                if (len == 0)
                                {
                                    break;
                                }
                                skipBytes -= len;
                            }
                        }
                        finally
                        {
                            pool.Return(tempBuffer);
                        }
                    }

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
            return this.ExtractPng(page: 0);
        }

        public Bitmap ExtractPng(int page)
        {
            if (this.Pages > 0)
            {
                if (page < 0 || page >= this.Pages)
                {
                    throw new ArgumentOutOfRangeException(nameof(page));
                }
            }
            else
            {
                // ignore it, always pick the first page.
                page = 0;
            }

            int dataSizePerPage = this.GetRawDataSizePerPage();
            byte[] pixel = new byte[dataSizePerPage];
            int actualBytes = this.GetRawData(page * dataSizePerPage, pixel);
            if (actualBytes != dataSizePerPage)
                throw new ArgumentException($"Not enough bytes have been read. (actual:{actualBytes}, expected:{dataSizePerPage})");
            Bitmap pngDecoded = null;
            BitmapData bmpdata;

            switch (this.Format)
            {
                case Wz_TextureFormat.ARGB4444 when this.ActualScale == 1:  // there's no form(3) any more.
                    pngDecoded = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
                    bmpdata = pngDecoded.LockBits(new Rectangle(Point.Empty, pngDecoded.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    unsafe
                    {
                        Span<byte> output = new Span<byte>(bmpdata.Scan0.ToPointer(), bmpdata.Stride * bmpdata.Height);
                        ImageCodec.BGRA4444ToBGRA32(pixel, output);
                    }
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                case Wz_TextureFormat.ARGB8888 when this.ActualScale == 1:
                    pngDecoded = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
                    bmpdata = pngDecoded.LockBits(new Rectangle(Point.Empty, pngDecoded.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    unsafe
                    {
                        Span<byte> output = new Span<byte>(bmpdata.Scan0.ToPointer(), bmpdata.Stride * bmpdata.Height);
                        pixel.CopyTo(output);
                    }
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                case Wz_TextureFormat.ARGB1555 when this.ActualScale == 1:
                    pngDecoded = new Bitmap(this.Width, this.Height, PixelFormat.Format16bppArgb1555);
                    bmpdata = pngDecoded.LockBits(new Rectangle(Point.Empty, pngDecoded.Size), ImageLockMode.WriteOnly, PixelFormat.Format16bppArgb1555);
                    CopyBmpDataWithStride(pixel, pngDecoded.Width * 2, bmpdata);
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                case Wz_TextureFormat.RGB565 when this.ActualScale == 1 || this.ActualScale == 16:
                    pngDecoded = new Bitmap(this.Width, this.Height, PixelFormat.Format16bppRgb565);
                    bmpdata = pngDecoded.LockBits(new Rectangle(Point.Empty, pngDecoded.Size), ImageLockMode.WriteOnly, PixelFormat.Format16bppRgb565);
                    if (this.ActualScale == 1) // old form(513)
                    {
                        CopyBmpDataWithStride(pixel, pngDecoded.Width * 2, bmpdata);
                    }
                    else if (this.ActualScale == 16) // old form(517)
                    {
                        unsafe
                        {
                            Span<byte> output = new Span<byte>(bmpdata.Scan0.ToPointer(), bmpdata.Stride * bmpdata.Height);
                            int rawDataWidth = this.Width / this.ActualScale;
                            int rawDataHeight = this.Height / this.ActualScale;
                            ImageCodec.ScalePixels(pixel, 2, rawDataWidth, rawDataWidth * 2, rawDataHeight, this.ActualScale, this.ActualScale, output, bmpdata.Stride);
                        }
                    }
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                case Wz_TextureFormat.DXT3:
                    if (this.ActualScale != 1)
                        throw new Exception("DXT3 does not support scale.");
                    pngDecoded = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
                    bmpdata = pngDecoded.LockBits(new Rectangle(new Point(), pngDecoded.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    unsafe
                    {
                        Span<byte> output = new Span<byte>(bmpdata.Scan0.ToPointer(), bmpdata.Stride * bmpdata.Height);
                        ImageCodec.DXT3ToBGRA32(pixel, output, this.Width, this.Width * 4, this.Height);
                    }
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                case Wz_TextureFormat.DXT5:
                    if (this.ActualScale != 1)
                        throw new Exception("DXT5 does not support scale.");
                    pngDecoded = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
                    bmpdata = pngDecoded.LockBits(new Rectangle(new Point(), pngDecoded.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    unsafe
                    {
                        Span<byte> output = new Span<byte>(bmpdata.Scan0.ToPointer(), bmpdata.Stride * bmpdata.Height);
                        ImageCodec.DXT5ToBGRA32(pixel, output, this.Width, this.Width * 4, this.Height);
                    }
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                case Wz_TextureFormat.RGBA1010102 when this.ActualScale == 1:
                    pngDecoded = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
                    bmpdata = pngDecoded.LockBits(new Rectangle(0, 0, this.Width, this.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    unsafe
                    {
                        int pageSize = this.Width * this.Height * 4;
                        Span<byte> outputPixels = new Span<byte>(bmpdata.Scan0.ToPointer(), bmpdata.Stride * bmpdata.Height);
                        ImageCodec.R10G10B10A2ToBGRA32(pixel, outputPixels);
                    }
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                case Wz_TextureFormat.BC7:
                    if (this.ActualScale != 1)
                        throw new Exception("BC7 does not support scale.");
                    pngDecoded = new Bitmap(this.Width, this.Height, PixelFormat.Format32bppArgb);
                    bmpdata = pngDecoded.LockBits(new Rectangle(0, 0, this.Width, this.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                    unsafe
                    {
                        Span<byte> outputPixels = new Span<byte>(bmpdata.Scan0.ToPointer(), bmpdata.Stride * bmpdata.Height);
                        ImageCodec.BC7ToRGBA32(pixel, outputPixels, bmpdata.Width, bmpdata.Stride, bmpdata.Height);
                        ImageCodec.RGBA32ToBGRA32(outputPixels, outputPixels);
                    }
                    pngDecoded.UnlockBits(bmpdata);
                    break;

                default:
                    throw new Exception($"Unsupported format ({this.Format}, scale={this.ActualScale}).");
            }

            return pngDecoded;
        }

        private static void CopyBmpDataWithStride(byte[] source, int stride, BitmapData bmpData)
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
            if (scale > 1)
            {
                if ((width % scale) != 0 || (height % scale) != 0)
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
        /* introduced in KMST 1186 */
        A8 = 2304,
        RGBA1010102 = 2562,
        DXT1 = 4097,
        BC7 = 4098,
        RGBA32Float = 4100,
    }
}
