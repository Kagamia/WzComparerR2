using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using ImageManipulation;

using WzComparerR2.WzLib;

namespace WzComparerR2.Common
{
    public class Gif
    {
        public Gif()
        {
            this.Frames = new List<IGifFrame>();
            this.Loop = true;
        }

        public List<IGifFrame> Frames { get; private set; }
        public bool Loop { get; set; }

        public Bitmap EncodeGif(Color backgrnd)
        {
            return EncodeGif(backgrnd, 0x00);
        }

        public Bitmap EncodeGif(Color backgrnd, int minAlpha)
        {
            return EncodeGif(backgrnd, minAlpha, 0, this.Frames.Count);
        }

        public Bitmap EncodeGif(Color backgrnd, int minAlpha, int startIndex, int frameCount)
        {
            if (frameCount <= 0)
            {
                return null;
            }

            Rectangle rect = this.GetRect();
            string tempFileName = Path.GetTempFileName();
            FileStream gifStream = new FileStream(tempFileName, FileMode.Create);//gif文件流
            MemoryStream mStream = new MemoryStream(); //存储frame的内存流
            BinaryWriter bWriter = new BinaryWriter(gifStream); //写入流

            //写入gif头信息
            bWriter.Write(gifHeader);
            bWriter.Write((ushort)rect.Width);
            bWriter.Write((ushort)rect.Height);
            bWriter.Write(logicalScreen);
            if (Loop)
            {
                bWriter.Write(appExtension);
            }

            //写入帧信息
            for (int i = startIndex, j = startIndex + frameCount; i < j; i++)
            {
                if (i >= this.Frames.Count)
                    break;
                IGifFrame frame = this.Frames[i];
                if (frame == null)
                {
                    continue;
                }
                mStream.SetLength(0);
                Bitmap tmpGif = Quantize(frame, rect, backgrnd, minAlpha);
                tmpGif.Save(mStream, ImageFormat.Gif);
                tmpGif.Dispose();

                byte[] tempArray = mStream.GetBuffer();
                // 781开始为Graphic Control Extension块 标志为21 F9 04 
                tempArray[784] = (byte)0x09; //图像刷新时屏幕返回初始帧 貌似不打会bug 意味不明 测试用
                int delay = frame.Delay / 10;
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
                tempArray = null;
            }
            bWriter.Write(gifEnd);
            mStream.Close();
            gifStream.Close();

            Bitmap gifBitmap = new Bitmap(tempFileName);
            return gifBitmap;
        }

        public Bitmap EncodeGif2(Color backgrnd, int minAlpha)
        {
            return EncodeGif2(backgrnd, minAlpha, 0, this.Frames.Count);
        }

        public Bitmap EncodeGif2(Color backgrnd, int minAlpha, int startIndex, int frameCount)
        {
            if (frameCount <= 0)
            {
                return null;
            }

            Rectangle rect = this.GetRect();
            string tempFileName = Path.GetTempFileName();

            GifEncoder enc;
            byte a = backgrnd.A;
            Color backgrndColor = Color.FromArgb(255, backgrnd);
            if (a == 0xff) //纯色
            {
                enc = new GifEncoder(tempFileName, rect.Width, rect.Height, 256, backgrnd);
            }
            else //透明混色
            {
                enc = new GifEncoder(tempFileName, rect.Width, rect.Height, 256, Color.Transparent);
            }

            for (int i = startIndex, j = startIndex + frameCount; i < j; i++)
            {
                if (i >= this.Frames.Count)
                    break;
                IGifFrame frame = this.Frames[i];
                if (frame == null)
                {
                    continue;
                }
                Bitmap picFrame = PrepareFrame(frame, rect, backgrnd, minAlpha);
                enc.AppendFrame(picFrame, frame.Delay);
                picFrame.Dispose();
            }
            enc.Dispose();

            Bitmap gifBitmap = new Bitmap(tempFileName);
            return gifBitmap;
        }

        public Rectangle GetRect()
        {
            Rectangle rect = Rectangle.Empty;
            foreach (var f in this.Frames)
            {
                var newRect = ((IGifFrame)f).Region;
                rect = rect.Size.IsEmpty ? newRect : Rectangle.Union(rect, newRect);
            }
            return rect.Size.IsEmpty ? Rectangle.Empty : rect;
        }

        public Bitmap GetFrame(int i)
        {
            var iFrame = this.Frames[i];
            var rect = iFrame.Region;
            return GetFrame(i, rect);
        }

        private Bitmap GetFrame(int i, Rectangle canvasRect)
        {
            var iFrame = this.Frames[i];
            Bitmap bmp = new Bitmap(canvasRect.Width, canvasRect.Height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmp);
            iFrame.Draw(g, canvasRect);
            g.Dispose();
            return bmp;
        }

        private static readonly byte[] gifHeader = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 };//GIF89a
        private static readonly byte[] logicalScreen = new byte[] { 0x70, 0x00, 0x00 };//无全局色彩表 无视背景色 无视像素纵横比
        private static readonly byte[] appExtension = new byte[] { 0x21,0xff,0x0b, //块标志
                0x4e,0x45,0x54,0x53,0x43,0x41,0x50,0x45,0x32,0x2e,0x30, //NETSCAPE2.0
                0x03,0x01,0x00,0x00,0x00};//循环信息 其他信息
        private static readonly byte[] gifEnd = new byte[] { 0x3b };//结束信息


        /// <summary>
        /// 预处理帧坐标，生成新的图片。
        /// </summary>
        private static Bitmap PrepareFrame(IGifFrame frame, Rectangle canvasRect, Color backgrnd, int minAlpha)
        {
            Bitmap gifFrame = new Bitmap(canvasRect.Width, canvasRect.Height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(gifFrame);

            if (backgrnd.A == 0xff) //背景色
            {
                g.Clear(backgrnd);
                frame.Draw(g, canvasRect);
            }
            else //透明混合色
            {
                Rectangle frameRect = frame.Region;
                frameRect.Offset(-canvasRect.X, -canvasRect.Y);
                frame.Draw(g, canvasRect);

                BitmapData data = gifFrame.LockBits(new Rectangle(Point.Empty, gifFrame.Size), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                byte[] buffer = new byte[data.Stride * data.Height];
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                for (int y = frameRect.Top; y < frameRect.Bottom; y++)
                {
                    for (int x = frameRect.Left; x < frameRect.Right; x++)
                    {
                        int i = 4 * x + y * data.Stride;

                        byte a = buffer[i + 3];
                        if (a <= minAlpha)
                        {
                            buffer[i] = buffer[i + 1] = buffer[i + 2] = buffer[i + 3] = 0;
                        }
                        else if (a < 0xff)
                        {
                            float al = a / 255f;
                            float be = (1 - al);
                            buffer[i] = (byte)(buffer[i] * al + backgrnd.B * be);
                            buffer[i + 1] = (byte)(buffer[i + 1] * al + backgrnd.G * be);
                            buffer[i + 2] = (byte)(buffer[i + 2] * al + backgrnd.R * be);
                            buffer[i + 3] = 0xff;
                        }
                    }
                }
                Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                gifFrame.UnlockBits(data);
            }

            return gifFrame;
        }

        /// <summary>
        /// 对32位图像进行八叉树量化处理，生成Gif格式图像。
        /// </summary>
        /// <param Name="Bitmap">要处理的Bitmap。</param>
        /// <param Name="backgrnd">若Alpha分量为0xff，则表示生成gif的全局背景色，否则取RGB视为透明混合背景色。</param>
        /// <param Name="gifSize">表示将要生成Gif图像的全局大小。</param>
        /// <param Name="Offset">表示bitmap在Gif图像的左上角坐标偏移。</param>
        /// <param Name="minAlpha">表示参与透明色混合像素的最小alpha分量，小于或等于这个值将不进行混合。</param>
        /// <returns></returns>
        private static Bitmap Quantize(IGifFrame frame, Rectangle canvasRect, Color backgrnd, int minAlpha)
        {
            Bitmap gifFrame = PrepareFrame(frame, canvasRect, backgrnd, minAlpha);

            Quantizer quantizer = new OctreeQuantizer(255, 8);
            Bitmap quantBitmap = quantizer.Quantize(gifFrame);

            gifFrame.Dispose();
            return quantBitmap;
        }


        public static Gif CreateFromNode(Wz_Node node, GlobalFindNodeFunction findNode)
        {
            if (node == null)
                return null;
            Gif gif = new Gif();
            for (int i = 0; ; i++)
            {
                GifFrame gifFrame = null;
                Wz_Node frameNode = node.FindNodeByPath(i.ToString());

                if (frameNode == null || frameNode.Value == null)
                    break;

                if (frameNode.Value is Wz_Uol)
                {
                    Wz_Uol uol = frameNode.Value as Wz_Uol;
                    Wz_Node uolNode = uol.HandleUol(frameNode);
                    if (uolNode != null)
                        frameNode = uolNode;
                }
                if (frameNode.Value is Wz_Png)
                {
                    string source = frameNode.Nodes["source"].GetValueEx<string>(null);
                    Wz_Png png = null;
                    if (!string.IsNullOrEmpty(source) && findNode != null)
                    {
                        png = findNode(source).GetValueEx<Wz_Png>(null);
                    }
                    if (png == null)
                    {
                        png = (Wz_Png)frameNode.Value;
                    }

                    gifFrame = new GifFrame(png.ExtractPng());
                    foreach (Wz_Node propNode in frameNode.Nodes)
                    {
                        switch (propNode.Text)
                        {
                            case "origin":
                                gifFrame.Origin = (propNode.Value as Wz_Vector);
                                break;
                            case "delay":
                                gifFrame.Delay = propNode.GetValue<int>();
                                break;
                            case "a0":
                                gifFrame.A0 = propNode.GetValue<int>();
                                break;
                            case "a1":
                                gifFrame.A1 = propNode.GetValue<int>();
                                break;
                        }
                    }
                    if (gifFrame.Delay == 0)
                        gifFrame.Delay = 100;//给予默认delay
                }
                if (gifFrame == null)
                    break;
                gif.Frames.Add(gifFrame);
            }
            if (gif.Frames.Count > 0)
                return gif;
            else
                return null;
        }
    }
}
