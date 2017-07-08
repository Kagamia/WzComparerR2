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
        }

        public List<IGifFrame> Frames { get; private set; }

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

            return EncodeGif<BuildInGifEncoder>(backgrnd, minAlpha, startIndex, frameCount);
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

            return EncodeGif<IndexGifEncoder>(backgrnd, minAlpha, startIndex, frameCount);
        }

        private Bitmap EncodeGif<T>(Color backgrnd, int minAlpha, int startIndex, int frameCount)
            where T : GifEncoder
        {
            //预判大小
            Rectangle rect = this.GetRect();
            Bitmap canvas = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb);
            string tempFileName = Path.GetTempFileName();
            GifEncoder enc = (GifEncoder)Activator.CreateInstance(typeof(T), tempFileName, rect.Width, rect.Height);

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

                PrepareFrame(canvas, frame, rect, backgrnd, minAlpha);
                enc.AppendFrame(canvas, frame.Delay);
            }

            enc.Dispose();
            return Image.FromFile(tempFileName) as Bitmap;
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

        private static Bitmap PrepareFrame(IGifFrame frame, Rectangle canvasRect, Color backgrnd, int minAlpha)
        {
            Bitmap gifFrame = new Bitmap(canvasRect.Width, canvasRect.Height, PixelFormat.Format32bppArgb);
            PrepareFrame(gifFrame, frame, canvasRect, backgrnd, minAlpha);
            return gifFrame;
        }

        /// <summary>
        /// 预处理帧坐标，生成新的图片。
        /// </summary>
        private static void PrepareFrame(Bitmap canvas, IGifFrame frame, Rectangle canvasRect, Color backgrnd, int minAlpha)
        {
            Graphics g = Graphics.FromImage(canvas);

            if (backgrnd.A == 0xff) //背景色
            {
                g.Clear(backgrnd);
                frame.Draw(g, canvasRect);
            }
            else //透明混合色
            {
                g.Clear(Color.Transparent);
                Rectangle frameRect = frame.Region;
                frameRect.Offset(-canvasRect.X, -canvasRect.Y);
                frame.Draw(g, canvasRect);

                BitmapData data = canvas.LockBits(new Rectangle(Point.Empty, canvas.Size), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                unsafe
                {
                    byte* buffer = (byte*)data.Scan0.ToPointer();

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
                }
                canvas.UnlockBits(data);
            }
        }

        public static Gif CreateFromNode(Wz_Node node, GlobalFindNodeFunction findNode)
        {
            if (node == null)
                return null;
            Gif gif = new Gif();
            for (int i = 0; ; i++)
            {
                Wz_Node frameNode = node.FindNodeByPath(i.ToString());

                if (frameNode == null || frameNode.Value == null)
                    break;
                GifFrame gifFrame = CreateFrameFromNode(frameNode, findNode);

                if (gifFrame == null)
                    break;
                gif.Frames.Add(gifFrame);
            }
            if (gif.Frames.Count > 0)
                return gif;
            else
                return null;
        }

        public static GifFrame CreateFrameFromNode(Wz_Node frameNode, GlobalFindNodeFunction findNode)
        {
            if (frameNode == null || frameNode.Value == null)
            {
                return null;
            }

            while (frameNode.Value is Wz_Uol)
            {
                Wz_Uol uol = frameNode.Value as Wz_Uol;
                Wz_Node uolNode = uol.HandleUol(frameNode);
                if (uolNode != null)
                {
                    frameNode = uolNode;
                }
            }
            if (frameNode.Value is Wz_Png)
            {
                var linkNode = frameNode.GetLinkedSourceNode(findNode);
                Wz_Png png = linkNode?.GetValue<Wz_Png>() ?? (Wz_Png)frameNode.Value;

                var gifFrame = new GifFrame(png.ExtractPng());
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
                {
                    gifFrame.Delay = 100;//给予默认delay
                }
                return gifFrame;
            }
            return null;
        }
    }
}
