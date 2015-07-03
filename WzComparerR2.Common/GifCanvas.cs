using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace WzComparerR2.Common
{
    public class GifCanvas
    {
        public GifCanvas()
        {
            this.Layers = new List<GifLayer>();
            this.AlphaGradientDelay = 30;
        }

        public List<GifLayer> Layers { get; private set; }

        public int AlphaGradientDelay { get; set; }

        public Gif Combine()
        {
            //获取全部关键帧延时
            List<int> delays = new List<int>();
            delays.Add(0);
            foreach (var layer in this.Layers)
            {
                int delay = 0;
                foreach (var frame in layer.Frames)
                {
                    delay += frame.Delay;
                    int idx = delays.BinarySearch(delay);
                    if (idx < 0)
                    {
                        delays.Insert(~idx, delay);
                    }
                }
            }

            //构建关键帧
            LinkedList<KeyFrame> keyFrames = new LinkedList<KeyFrame>();
            for (int i = 1; i < delays.Count; i++)
            {
                keyFrames.AddLast(new KeyFrame() { Delay = delays[i] - delays[i - 1] });
            }

            //开始填充
            foreach (var layer in this.Layers)
            {
                var node = keyFrames.First;
                foreach (var frame in layer.Frames) //把图层按关键帧拆分
                {
                    var frame0 = frame;
                    int delay = frame.Delay;
                    while (delay > 0)
                    {
                        if (frame.Bitmap != null)
                        {
                            if (node.Value.Delay == frame0.Delay) //直接加入
                            {
                                node.Value.Frames.Add(frame0);
                            }
                            else if (node.Value.Delay < frame0.Delay) //拆分
                            {
                                GifFrame f1, f2;
                                SplitGifFrame(frame0, node.Value.Delay, out f1, out f2);
                                node.Value.Frames.Add(f1);
                                frame0 = f2;
                            }
                            else
                            {
                                throw new Exception("key frame delay error.");
                            }
                        }

                        delay -= node.Value.Delay;
                        node = node.Next;
                    }
                }
            }

            //开始合并
            Gif gif = new Gif();
            {
                var node = keyFrames.First;
                while (node != null)
                {
                    if (AlphaGradientDelay > 0 && node.Value.HasAlphaGradient && AlphaGradientDelay < node.Value.Delay) //分离渐变帧
                    {
                        KeyFrame f1, f2;
                        node.Value.Split(AlphaGradientDelay, out f1, out f2);
                        node.Value = f1;
                        keyFrames.AddAfter(node, f2);
                    }

                    gif.Frames.Add(node.Value);
                    node = node.Next;
                }
            }
            return gif;
        }

        private static void SplitGifFrame(GifFrame frame, int time, out GifFrame frame1, out GifFrame frame2)
        {
            double p = (double)time / frame.Delay;
            int a = frame.A0 == frame.A1 ? frame.A0 : (int)Math.Round(frame.A0 * (1 - p) + frame.A1 * p);
            frame1 = new GifFrame(frame.Bitmap, frame.Origin, time) { A0 = frame.A0, A1 = a };
            frame2 = new GifFrame(frame.Bitmap, frame.Origin, frame.Delay - time) { A0 = a, A1 = frame.A1 };
        }

        private class KeyFrame : IGifFrame
        {
            public KeyFrame()
            {
                this.Frames = new List<GifFrame>();
            }

            public List<GifFrame> Frames { get; private set; }
            public int Delay { get; set; }

            public bool HasAlphaGradient
            {
                get
                {
                    return !this.Frames.TrueForAll(f => f.A0 == f.A1);
                }
            }

            public void Split(int time, out KeyFrame keyFrame1, out KeyFrame keyFrame2)
            {
                keyFrame1 = new KeyFrame();
                keyFrame2 = new KeyFrame();
                double p = (double)time / this.Delay;
                foreach (var f in this.Frames)
                {
                    GifFrame f1, f2;
                    SplitGifFrame(f, time, out f1, out f2);
                    keyFrame1.Frames.Add(f1);
                    keyFrame2.Frames.Add(f2);
                }
                keyFrame1.Delay = time;
                keyFrame2.Delay = this.Delay - time;
            }

            int IGifFrame.Delay
            {
                get { return this.Delay; }
            }

            Rectangle IGifFrame.Region
            {
                get
                {
                    Rectangle rect = Rectangle.Empty;
                    foreach (var f in this.Frames)
                    {
                        var newRect = ((IGifFrame)f).Region;
                        rect = rect.Size.IsEmpty ? newRect : Rectangle.Union(rect, newRect);
                    }
                    return rect.Size.IsEmpty ? Rectangle.Empty : rect;
                }
            }

            void IGifFrame.Draw(Graphics g, Rectangle canvasRect)
            {
                foreach (var f in this.Frames)
                {
                    ((IGifFrame)f).Draw(g, canvasRect);
                }
            }
        }
    }
}
