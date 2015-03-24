using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using ImageManipulation;

using WzComparerR2.WzLib;

namespace WzComparerR2
{
    public class Gif
    {
        public Gif()
        {
            this.frames = new List<GifFrame>();
            this.loop = true;
        }

        List<GifFrame> frames;
        bool loop;

        public List<GifFrame> Frames
        {
            get { return frames; }
        }

        public bool Loop
        {
            get { return loop; }
            set { loop = value; }
        }

        public Bitmap EncodeGif(Color backgrnd)
        {
            return EncodeGif(backgrnd, 0x00);
        }

        public Bitmap EncodeGif(Color backgrnd, int minAlpha)
        {
            return EncodeGif(backgrnd, minAlpha, 0, this.frames.Count);
        }

        public Bitmap EncodeGif(Color backgrnd, int minAlpha, int startIndex, int frameCount)
        {
            if (this.frames.Count == 0)
                return null;
            if (this.frames.Count == 1)
                return this.frames[0].Bitmap;

            Rectangle rect = this.GetRect();
            string tempFileName = Path.GetTempFileName();
            FileStream gifStream = new FileStream(tempFileName, FileMode.OpenOrCreate);//gif文件流
            MemoryStream mStream = new MemoryStream(); //存储frame的内存流
            BinaryWriter bWriter = new BinaryWriter(gifStream); //写入流

            //写入gif头信息
            bWriter.Write(gifHeader);
            bWriter.Write((ushort)rect.Width);
            bWriter.Write((ushort)rect.Height);
            bWriter.Write(logicalScreen);
            if (loop)
            {
                bWriter.Write(appExtension);
            }

            //写入帧信息
            for (int i = startIndex, j = startIndex + frameCount; i < j; i++)
            {
                if (i >= this.frames.Count)
                    break;
                GifFrame frame = this.frames[i];
                if (frame == null || frame.Bitmap == null)
                {
                    continue;
                }
                mStream.SetLength(0);
                int off_x = -rect.X - frame.Origin.X, off_y = -rect.Y - frame.Origin.Y;
                Quantize(frame.Bitmap, backgrnd, rect.Size, new Point(off_x, off_y), minAlpha).Save(mStream, ImageFormat.Gif);

                byte[] tempArray = mStream.ToArray();
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
                bWriter.Write(tempArray, 799, tempArray.Length - 800);
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
            return EncodeGif2(backgrnd, minAlpha, 0, this.frames.Count);
        }

        public Bitmap EncodeGif2(Color backgrnd, int minAlpha, int startIndex, int frameCount)
        {
            if (this.frames.Count == 0)
                return null;
            if (this.frames.Count == 1)
                return this.frames[0].Bitmap;

            Rectangle rect = this.GetRect();
            string tempFileName = Path.GetTempFileName();


            GifEncoder enc;
            byte a = backgrnd.A;
            backgrnd = Color.FromArgb(255, backgrnd);
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
                if (i >= this.frames.Count)
                    break;
                GifFrame frame = this.frames[i];
                if (frame == null || frame.Bitmap == null)
                {
                    continue;
                }
                int off_x = -rect.X - frame.Origin.X, off_y = -rect.Y - frame.Origin.Y;
                if (off_x == 0 && off_y == 0 && frame.Bitmap.Size == rect.Size
                    && (backgrnd.A == 0xff || backgrnd == Color.FromArgb(0x00ffffff) && minAlpha == 0)) //与画布等大
                {
                    enc.AppendFrame(frame.Bitmap, frame.Delay);
                }
                else //混合颜色
                {
                    Bitmap picFrame = PrepareFrame(frame.Bitmap, backgrnd, rect.Size, new Point(off_x, off_y), minAlpha);
                    enc.AppendFrame(picFrame, frame.Delay);
                    picFrame.Dispose();
                }
            }

            enc.Dispose();

            Bitmap gifBitmap = new Bitmap(tempFileName);
            return gifBitmap;
        }

        public Rectangle GetRect()
        {
            int left = short.MaxValue, top = short.MaxValue, right = short.MinValue, bottom = short.MinValue;
            foreach (GifFrame frame in frames)
            {
                if (frame == null || frame.Bitmap == null)
                {
                    continue;
                }

                left = Math.Min(left, -frame.Origin.X);
                top = Math.Min(top, -frame.Origin.Y);
                right = Math.Max(right, frame.Bitmap.Width - frame.Origin.X);
                bottom = Math.Max(bottom, frame.Bitmap.Height - frame.Origin.Y);
            }
            Rectangle rect = new Rectangle(left, top, right - left, bottom - top);
            return rect;
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
        public static Bitmap PrepareFrame(Bitmap bitmap, Color backgrnd, Size gifSize, Point offset, int minAlpha)
        {
            Bitmap gifFrame = new Bitmap(gifSize.Width, gifSize.Height);
            Graphics g = Graphics.FromImage(gifFrame);
            if (backgrnd.A == 0xff) //背景色
            {
                Brush brush = new SolidBrush(backgrnd);
                g.FillRectangle(brush, 0, 0, gifSize.Width, gifSize.Height);
                g.DrawImage(bitmap, offset);
                brush.Dispose();
            }
            else //透明混合色
            {
                g.DrawImage(bitmap, offset);
                BitmapData data = gifFrame.LockBits(new Rectangle(offset, bitmap.Size), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                byte[] buffer = new byte[data.Stride * data.Height];
                Marshal.Copy(data.Scan0, buffer, 0, buffer.Length);

                for (int y = 0; y < data.Height; y++)
                {
                    for (int x = 0; x < data.Width; x++)
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
        public static Bitmap Quantize(Bitmap bitmap, Color backgrnd, Size gifSize, Point offset, int minAlpha)
        {
            Bitmap gifFrame = PrepareFrame(bitmap, backgrnd, gifSize, offset, minAlpha);

            Quantizer quantizer = new OctreeQuantizer(255, 8);
            Bitmap quantBitmap = quantizer.Quantize(gifFrame);

            gifFrame.Dispose();
            return quantBitmap;
        }

        public static Gif CreateFromNode(Wz_Node node)
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
                    if (!string.IsNullOrEmpty(source))
                    {
                        png = PluginBase.PluginManager.FindWz(source).GetValueEx<Wz_Png>(null);
                    }
                    if (png == null)
                    {
                        png = frameNode.Value as Wz_Png;
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
                                gifFrame.Delay = Convert.ToInt32(propNode.Value);
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
            if (gif.frames.Count > 0)
                return gif;
            else
                return null;
        }
    }
}
