using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using ImageManipulation;

namespace WzComparerR2.Common
{
    public class BuildInGifEncoder : GifEncoder
    {
        public BuildInGifEncoder(string fileName, int width, int height)
            : base(fileName, width, height)
        {
            bWriter = new BinaryWriter(File.Create(fileName));
            mStream = new MemoryStream();
            quantizer = new OctreeQuantizer(255, 8);

            WriteHeader();
        }

        private BinaryWriter bWriter;
        private MemoryStream mStream;
        private Quantizer quantizer;

        private static readonly byte[] gifHeader = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 };//GIF89a
        private static readonly byte[] logicalScreen = new byte[] { 0x70, 0x00, 0x00 };//无全局色彩表 无视背景色 无视像素纵横比
        private static readonly byte[] appExtension = new byte[] { 0x21,0xff,0x0b, //块标志
                0x4e,0x45,0x54,0x53,0x43,0x41,0x50,0x45,0x32,0x2e,0x30, //NETSCAPE2.0
                0x03,0x01,0x00,0x00,0x00};//循环信息 其他信息
        private static readonly byte[] gifEnd = new byte[] { 0x3b };//结束信息

        public override void AppendFrame(Bitmap image, int delay)
        {
            mStream.SetLength(0);
            mStream.Position = 0;
            using (var tempGif = quantizer.Quantize(image))
            {
                tempGif.Save(mStream, ImageFormat.Gif);
            }

            byte[] tempArray = mStream.GetBuffer();
            // 781开始为Graphic Control Extension块 标志为21 F9 04 
            tempArray[784] = (byte)0x09; //图像刷新时屏幕返回初始帧 貌似不打会bug 意味不明 测试用
            delay = delay / 10;
            tempArray[785] = (byte)(delay & 0xff);
            tempArray[786] = (byte)(delay >> 8 & 0xff); //写入2字节的帧delay 
                                                        // 787为透明色索引  788为块结尾0x00
            tempArray[787] = (byte)0xff;
            // 789开始为Image Descriptor块 标志位2C
            // 790~793为帧偏移大小 默认为0
            // 794~797为帧图像大小 默认他
            tempArray[798] = (byte)(tempArray[798] | 0X87); //本地色彩表标志

            //写入到gif文件
            bWriter.Write(tempArray, 781, 18);
            bWriter.Write(tempArray, 13, 768);
            bWriter.Write(tempArray, 799, (int)mStream.Length - 800);
        }

        public override void AppendFrame(IntPtr pBuffer, int delay)
        {
            using(var bmp = new Bitmap(Width, Height, Width *4, PixelFormat.Format32bppArgb, pBuffer))
            {
                this.AppendFrame(bmp, delay);
            }
        }

        private void WriteHeader()
        {
            //写入gif头信息
            bWriter.Write(gifHeader);
            bWriter.Write((ushort)Width);
            bWriter.Write((ushort)Height);
            bWriter.Write(logicalScreen);
            //写入循环标记
            bWriter.Write(appExtension);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                bWriter.Write(gifEnd);
                bWriter.Close();
            }

            if (mStream != null)
            {
                mStream.Dispose();
                mStream = null;
            }
        }
    }
}
