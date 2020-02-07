using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using WzComparerR2.WzLib;
using WzComparerR2.Controls;
using WzComparerR2.Animation;
using WzComparerR2.Rendering;
using Microsoft.Xna.Framework;
using WzComparerR2.Config;
using WzComparerR2.Common;


namespace WzComparerR2
{
    public class PictureBoxEx : AnimationControl
    {
        public PictureBoxEx() : base()
        {
            this.AutoAdjustPosition = true;
            this.sbInfo = new StringBuilder();
        }

        public bool AutoAdjustPosition { get; set; }
        public string PictureName { get; set; }
        public bool ShowInfo { get; set; }

        public override System.Drawing.Font Font
        {
            get { return base.Font; }
            set
            {
                base.Font = value;
                this.xnaFont?.Dispose();
                this.xnaFont = new XnaFont(this.GraphicsDevice, value);
            }
        }

        public XnaFont XnaFont
        {
            get
            {
                if (xnaFont == null && this.Font != null)
                {
                    this.xnaFont = new XnaFont(this.GraphicsDevice, this.Font);
                }
                return this.xnaFont;
            }
        }

        private XnaFont xnaFont;
        private SpriteBatchEx sprite;
        private StringBuilder sbInfo;

        public void ShowImage(Wz_Png png)
        {
            //添加到动画控件
            var frame = new Animation.Frame()
            {
                Texture = png.ToTexture(this.GraphicsDevice),
                Png = png,
                Delay = 0,
                Origin = Point.Zero,
            };

            var frameData = new Animation.FrameAnimationData();
            frameData.Frames.Add(frame);

            this.ShowAnimation(frameData);
        }

        public FrameAnimationData LoadFrameAnimation(Wz_Node node)
        {
            return FrameAnimationData.CreateFromNode(node, this.GraphicsDevice, PluginBase.PluginManager.FindWz);
        }

        public SpineAnimationData LoadSpineAnimation(Wz_Node node)
        {
            return SpineAnimationData.CreateFromNode(node, null, this.GraphicsDevice, PluginBase.PluginManager.FindWz);
        }

        public MultiFrameAnimationData LoadMultiFrameAnimation(Wz_Node node)
        {
            return MultiFrameAnimationData.CreateFromNode(node, this.GraphicsDevice, PluginBase.PluginManager.FindWz);
        }

        public void ShowAnimation(FrameAnimationData data)
        {
            this.ShowAnimation(new FrameAnimator(data));
        }

        public void ShowAnimation(SpineAnimationData data)
        {
            this.ShowAnimation(new SpineAnimator(data));
        }

        public void ShowAnimation(MultiFrameAnimationData data)
        {
            this.ShowAnimation(new MultiFrameAnimator(data));
        }

        public void ShowAnimation(AnimationItem animator)
        {
            if (this.Items.Count > 0)
            {
                this.Items.Clear();
            }

            this.Items.Add(animator);

            if (this.AutoAdjustPosition)
            {
                this.AdjustPosition();
            }

            this.Invalidate();
        }

        public void AdjustPosition()
        {
            if (this.Items.Count <= 0)
                return;

            var animator = this.Items[0];

            if (animator is FrameAnimator)
            {
                var aniItem = (FrameAnimator)animator;
                var rect = aniItem.Data.GetBound();
                aniItem.Position = new Point(-rect.Left, -rect.Top);
            }
            else if (animator is SpineAnimator)
            {
                var aniItem = (SpineAnimator)animator;
                var rect = aniItem.Measure();
                aniItem.Position = new Point(-rect.Left, -rect.Top);
            }
            else if (animator is MultiFrameAnimator)
            {
                var aniItem = (MultiFrameAnimator)animator;
                var rect = aniItem.Data.GetBound(aniItem.SelectedAnimationName);
                aniItem.Position = new Point(-rect.Left, -rect.Top);
            }
        }

        public void SaveAsGif(AnimationItem aniItem, string fileName, ImageHandlerConfig config)
        {
            //保存动画 天坑代码
            var rec = new AnimationRecoder(this.GraphicsDevice);

            rec.Items.Add(aniItem);
            int length = rec.GetMaxLength();
            //rec.GetGifTimeLine(30); 放弃获取时间轴

            //预判大小阶段
            rec.ResetAll();
            Microsoft.Xna.Framework.Rectangle? bounds = null;

            int delay = Math.Max(10, config.MinDelay);
            for (int d = 0; d <= length; d += delay)
            {
                rec.Update(TimeSpan.FromMilliseconds(delay));
                var rect = aniItem.Measure();
                rect.Offset(aniItem.Position);
                bounds = (bounds == null || bounds.Value.IsEmpty) ? rect
                    : Microsoft.Xna.Framework.Rectangle.Union(rect, bounds.Value);
            }

            //绘制阶段
            rec.ResetAll();
            switch (config.BackgroundType.Value)
            {
                default:
                case ImageBackgroundType.Transparent:
                    rec.BackgroundColor = Color.TransparentBlack;
                    break;

                case ImageBackgroundType.Color:
                    rec.BackgroundColor = System.Drawing.Color.FromArgb(255, config.BackgroundColor.Value).ToXnaColor();
                    break;

                case ImageBackgroundType.Mosaic:
                    rec.BackgroundImage = MonogameUtils.CreateMosaic(GraphicsDevice,
                        config.MosaicInfo.Color0.ToXnaColor(),
                        config.MosaicInfo.Color1.ToXnaColor(),
                        Math.Max(1, config.MosaicInfo.BlockSize));
                    break;
            }

            byte[] buffer = new byte[bounds.Value.Width * bounds.Value.Height * 4];
            byte[] bufferPrev = null, bufferGif = null;
            int gifTime = 0, prevTime = 0;

            string dirName = Path.Combine(Path.GetDirectoryName(fileName),
                Path.GetFileNameWithoutExtension(fileName) + ".frames");


            if (config.SavePngFramesEnabled && !Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }

            //选择encoder
            GifEncoder enc = AnimateEncoderFactory.CreateEncoder(fileName, bounds.Value.Width, bounds.Value.Height, config);
            var encParams = AnimateEncoderFactory.GetEncoderParams(config.GifEncoder.Value);

            Action<int> writeFrame = (curTime) =>
            {
                unsafe
                {
                    string pngFileName = Path.Combine(dirName, prevTime + ".png");
                    fixed (byte* pBuffer = bufferPrev, pBufferGif = bufferGif)
                    {
                        using (var bmp = new System.Drawing.Bitmap(bounds.Value.Width, bounds.Value.Height, bounds.Value.Width * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, new IntPtr(pBuffer)))
                        {
                            var tasks = new List<Task>();
                            if (config.SavePngFramesEnabled) //保存单帧图像
                            {
                                tasks.Add(Task.Factory.StartNew(
                                    () => bmp.Save(pngFileName, System.Drawing.Imaging.ImageFormat.Png)
                                ));
                            }

                            //保存gif帧
                            int frameDelay = curTime - gifTime;
                            frameDelay = (int)(Math.Round(frameDelay / 10.0) * 10);
                            if (frameDelay > 0)
                            {
                                gifTime += frameDelay;
                                var pData = new IntPtr(pBufferGif);
                                tasks.Add(Task.Factory.StartNew(() =>
                                    enc.AppendFrame(pData, frameDelay)
                                ));
                            }

                            Task.WaitAll(tasks.ToArray());
                        }
                    }
                }
            };

            rec.Begin(bounds.Value);
            //0长度补丁
            for (int d = 0; d <= length; d += delay, rec.Update(TimeSpan.FromMilliseconds(delay)))
            {
                rec.Draw();
                using (var rt = rec.GetPngTexture())
                {
                    rt.GetData(buffer);
                    //比较上一帧
                    if (bufferPrev != null)
                    {
                        //跳过当前帧
                        if (memcmp(buffer, bufferPrev, (IntPtr)buffer.Length) == 0)
                        {
                            continue;
                        }
                        else //计算时间 输出
                        {
                            writeFrame(d);
                        }
                    }
                    else
                    {
                        bufferPrev = new byte[bounds.Value.Width * bounds.Value.Height * 4];
                    }

                    //交换缓冲区
                    var temp = buffer;
                    buffer = bufferPrev;
                    bufferPrev = temp;
                    prevTime = d;

                    //透明 额外导出一份gif
                    if (!encParams.SupportAlphaChannel && config.BackgroundType.Value == ImageBackgroundType.Transparent)
                    {
                        using (var rt2 = rec.GetGifTexture(config.BackgroundColor.Value.ToXnaColor(), config.MinMixedAlpha))
                        {
                            if (bufferGif == null)
                            {
                                bufferGif = new byte[bufferPrev.Length];
                            }
                            rt2.GetData(bufferGif);
                        }
                    }
                    else
                    {
                        bufferGif = bufferPrev;
                    }
                }
            }

            //输出最后一帧
            if (prevTime < length)
            {
                writeFrame(length);
            }
            else if (length <= 0) //0长度补丁
            {
                writeFrame(100);
            }
            //保存动画长度
            if (config.SavePngFramesEnabled)
            {
                File.WriteAllText(Path.Combine(dirName, "delay.txt"), length.ToString());
            }
            rec.End();
            enc.Dispose();
        }

        public override AnimationItem GetItemAt(int x, int y)
        {
            //固定获取当前显示的物件 无论鼠标在哪
            return this.Items.Count > 0 ? this.Items[0] : null;
        }

        protected override void Initialize()
        {
            base.Initialize();
            this.sprite = new SpriteBatchEx(this.GraphicsDevice);
        }

        protected override void Update(TimeSpan elapsed)
        {
            base.Update(elapsed);
        }

        protected override void Draw()
        {
            base.Draw();

            if (this.ShowInfo && this.XnaFont != null)
            {
                UpdateInfoText();
                sprite.Begin();
                sprite.DrawStringEx(this.XnaFont, this.sbInfo, Vector2.Zero, Color.Black);
                sprite.End();
            }
        }

        protected override void OnItemDragSave(AnimationItemEventArgs e)
        {
            var fileName = Path.GetTempFileName();

            if ((e.Item as FrameAnimator)?.Data.Frames.Count == 1)
            {
                using (var bmp = (e.Item as FrameAnimator).Data.Frames[0].Png.ExtractPng())
                {
                    bmp.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
            else
            {
                this.SaveAsGif(e.Item, fileName, ImageHandlerConfig.Default);
            }
            
            var imgObj = new ImageDataObject(null, fileName);
            this.DoDragDrop(imgObj, System.Windows.Forms.DragDropEffects.Copy);
            e.Handled = true;
        }

        private void UpdateInfoText()
        {
            this.sbInfo.Clear();
            if (this.Items.Count > 0)
            {
                var aniItem = this.Items[0];
                int time = 0;
                if (aniItem is FrameAnimator)
                {
                    time = ((FrameAnimator)aniItem).CurrentTime;
                }
                else if (aniItem is SpineAnimator)
                {
                    time = ((SpineAnimator)aniItem).CurrentTime;
                }
                else if (aniItem is MultiFrameAnimator)
                {
                    time = ((MultiFrameAnimator)aniItem).CurrentTime;
                }
                this.sbInfo.AppendFormat("pos: {0}, scale: {1:p0}, play: {2} / {3}",
                    aniItem.Position,
                    base.GlobalScale,
                    aniItem.Length <= 0 ? 0 : (time % aniItem.Length),
                    aniItem.Length);
            }
        }


        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int memcmp(byte[] b1, byte[] b2, IntPtr count);

    }
}
