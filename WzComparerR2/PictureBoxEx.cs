using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Xna.Framework;

using WzComparerR2.WzLib;
using WzComparerR2.Controls;
using WzComparerR2.Animation;
using WzComparerR2.Rendering;
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

        public bool SaveAsGif(AnimationItem aniItem, string fileName, ImageHandlerConfig config, bool options)
        {
            var rec = new AnimationRecoder(this.GraphicsDevice);

            rec.Items.Add(aniItem);
            int length = rec.GetMaxLength();
            int delay = Math.Max(10, config.MinDelay);
            var timeline = rec.GetGifTimeLine(delay, 655350);

            // calc available canvas area
            rec.ResetAll();
            Microsoft.Xna.Framework.Rectangle bounds = aniItem.Measure();
            if (length > 0)
            {
                IEnumerable<int> delays = timeline?.Take(timeline.Length - 1)
                    ?? Enumerable.Range(0, (int)Math.Ceiling(1.0 * length / delay) - 1);

                foreach (var frameDelay in delays)
                {
                    rec.Update(TimeSpan.FromMilliseconds(frameDelay));
                    var rect = aniItem.Measure();
                    bounds = Microsoft.Xna.Framework.Rectangle.Union(bounds, rect);
                }
            }
            bounds.Offset(aniItem.Position);

            // customize clip/scale options
            AnimationClipOptions clipOptions = new AnimationClipOptions()
            {
                StartTime = 0,
                StopTime = length,
                Left = bounds.Left,
                Top = bounds.Top,
                Right = bounds.Right,
                Bottom = bounds.Bottom,
                OutputWidth = bounds.Width,
                OutputHeight = bounds.Height,
            };

            if (options)
            {
                var frmOptions = new FrmGifClipOptions()
                {
                    ClipOptions = clipOptions,
                    ClipOptionsNew = clipOptions,
                };
                if (frmOptions.ShowDialog() == DialogResult.OK)
                {
                    var clipOptionsNew = frmOptions.ClipOptionsNew;
                    clipOptions.StartTime = clipOptionsNew.StartTime ?? clipOptions.StartTime;
                    clipOptions.StopTime = clipOptionsNew.StopTime ?? clipOptions.StopTime;

                    clipOptions.Left = clipOptionsNew.Left ?? clipOptions.Left;
                    clipOptions.Top = clipOptionsNew.Top ?? clipOptions.Top;
                    clipOptions.Right = clipOptionsNew.Right ?? clipOptions.Right;
                    clipOptions.Bottom = clipOptionsNew.Bottom ?? clipOptions.Bottom;

                    clipOptions.OutputWidth = clipOptionsNew.OutputWidth ?? (clipOptions.Right - clipOptions.Left);
                    clipOptions.OutputHeight = clipOptionsNew.OutputHeight ?? (clipOptions.Bottom - clipOptions.Top);
                }
                else
                {
                    return false;
                }
            }

            // validate params
            bounds = new Rectangle(
                clipOptions.Left.Value,
                clipOptions.Top.Value,
                clipOptions.Right.Value - clipOptions.Left.Value,
                clipOptions.Bottom.Value - clipOptions.Top.Value
                );
            var targetSize = new Point(clipOptions.OutputWidth.Value, clipOptions.OutputHeight.Value);
            var startTime = clipOptions.StartTime.Value;
            var stopTime = clipOptions.StopTime.Value;

            if (bounds.Width <= 0 || bounds.Height <= 0
                || targetSize.X <= 0 || targetSize.Y <= 0
                || startTime < 0 || stopTime > length
                || stopTime - startTime < 0)
            {
                return false;
            }
            length = stopTime - startTime;

            // create output dir
            string framesDirName = Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + ".frames");
            if (config.SavePngFramesEnabled && !Directory.Exists(framesDirName))
            {
                Directory.CreateDirectory(framesDirName);
            }

            // pre-render
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

            // select encoder
            GifEncoder enc = AnimateEncoderFactory.CreateEncoder(fileName, targetSize.X, targetSize.Y, config);
            var encParams = AnimateEncoderFactory.GetEncoderParams(config.GifEncoder.Value);

            // pipeline functions
            IEnumerable<Tuple<byte[], int>> MergeFrames(IEnumerable<Tuple<byte[], int>> frames)
            {
                byte[] prevFrame = null;
                int prevDelay = 0;

                foreach (var frame in frames)
                {
                    byte[] currentFrame = frame.Item1;
                    int currentDelay = frame.Item2;

                    if (prevFrame == null)
                    {
                        prevFrame = currentFrame;
                        prevDelay = currentDelay;
                    }
                    else if (memcmp(prevFrame, currentFrame, (IntPtr)prevFrame.Length) == 0)
                    {
                        prevDelay += currentDelay;
                    }
                    else
                    {
                        yield return Tuple.Create(prevFrame, prevDelay);
                        prevFrame = currentFrame;
                        prevDelay = currentDelay;
                    }
                }

                if (prevFrame != null)
                {
                    yield return Tuple.Create(prevFrame, prevDelay);
                }
            }

            IEnumerable<int> RenderDelay()
            {
                int t = 0;
                while (t < length)
                {
                    int frameDelay = Math.Min(length - t, delay);
                    t += frameDelay;
                    yield return frameDelay;
                }
            }

            IEnumerable<int> ClipTimeline(int[] _timeline)
            {
                int t = 0;
                for (int i = 0; i < timeline.Length; i++)
                {
                    var frameDelay = timeline[i];
                    if (t < startTime)
                    {
                        if (t + frameDelay > startTime)
                        {
                            frameDelay = t + frameDelay - startTime;
                            t = startTime;
                        }
                        else
                        {
                            t += frameDelay;
                            continue;
                        }
                    }

                    if (t + frameDelay < stopTime)
                    {
                        yield return frameDelay;
                        t += frameDelay;
                    }
                    else
                    {
                        frameDelay = stopTime - t;
                        yield return frameDelay;
                        break;
                    }
                }
            }

            int prevTime = 0;
            async Task<int> ApplyFrame(byte[] frameData, int frameDelay)
            {
                byte[] gifData = null;
                if (!encParams.SupportAlphaChannel && config.BackgroundType.Value == ImageBackgroundType.Transparent)
                {
                    using (var rt2 = rec.GetGifTexture(config.BackgroundColor.Value.ToXnaColor(), config.MinMixedAlpha))
                    {
                        if (gifData == null)
                        {
                            gifData = new byte[frameData.Length];
                        }
                        rt2.GetData(gifData);
                    }
                }
                else
                {
                    gifData = frameData;
                }

                var tasks = new List<Task>();

                // save each frame as png
                if (config.SavePngFramesEnabled)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        string pngFileName = Path.Combine(framesDirName, $"{prevTime}_{prevTime + frameDelay}.png");
                        unsafe
                        {
                            fixed (byte* pFrameBuffer = frameData)
                            {
                                using (var bmp = new System.Drawing.Bitmap(targetSize.X, targetSize.Y, targetSize.X * 4, System.Drawing.Imaging.PixelFormat.Format32bppArgb, new IntPtr(pFrameBuffer)))
                                {
                                    bmp.Save(pngFileName, System.Drawing.Imaging.ImageFormat.Png);
                                }
                            }
                        }
                    }));
                }

                // append frame data to gif stream
                tasks.Add(Task.Run(() =>
                {
                    // TODO: only for gif here?
                    frameDelay = Math.Max(10, (int)(Math.Round(frameDelay / 10.0) * 10));
                    unsafe
                    {
                        fixed (byte* pGifBuffer = gifData)
                        {
                            enc.AppendFrame(new IntPtr(pGifBuffer), frameDelay);
                        }
                    }
                }));

                await Task.WhenAll(tasks);
                prevTime += frameDelay;
                return prevTime;
            }

            async Task RenderJob(IProgressDialogContext context, CancellationToken cancellationToken)
            {
                bool isCompareAndMergeFrames = timeline == null;

                // build pipeline
                IEnumerable<int> delayEnumerator = timeline == null ? RenderDelay() : ClipTimeline(timeline);
                var step1 = delayEnumerator.TakeWhile(_ => !cancellationToken.IsCancellationRequested);
                var frameRenderEnumerator = step1.Select(frameDelay =>
                {
                    rec.Draw();
                    rec.Update(TimeSpan.FromMilliseconds(frameDelay));
                    return frameDelay;
                });
                var step2 = frameRenderEnumerator.TakeWhile(_ => !cancellationToken.IsCancellationRequested);
                var getFrameData = step2.Select(frameDelay =>
                {
                    using (var t2d = rec.GetPngTexture())
                    {
                        byte[] frameData = new byte[t2d.Width * t2d.Height * 4];
                        t2d.GetData(frameData);
                        return Tuple.Create(frameData, frameDelay);
                    }
                });
                var step3 = getFrameData.TakeWhile(_ => !cancellationToken.IsCancellationRequested);
                if (isCompareAndMergeFrames)
                {
                    var mergedFrameData = MergeFrames(step3);
                    step3 = mergedFrameData.TakeWhile(_ => !cancellationToken.IsCancellationRequested);
                }

                var step4 = step3.Select(item => ApplyFrame(item.Item1, item.Item2));

                // run pipeline
                bool isPlaying = this.IsPlaying;
                try
                {
                    this.IsPlaying = false;
                    rec.Begin(bounds, targetSize);
                    if (startTime > 0)
                    {
                        rec.Update(TimeSpan.FromMilliseconds(startTime));
                    }
                    context.ProgressMin = 0;
                    context.ProgressMax = length;
                    foreach (var task in step4)
                    {
                        int currentTime = await task;
                        context.Progress = currentTime;
                    }
                }
                catch (Exception ex)
                {
                    context.Message = $"Error: {ex.Message}";
                    throw;
                }
                finally
                {
                    rec.End();
                    enc.Dispose();
                    this.IsPlaying = isPlaying;
                }
            }

            var dialogResult = ProgressDialog.Show(this.FindForm(), "Exporting...", "Save animation file...", true, false, RenderJob);
            return dialogResult == DialogResult.OK;
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
                //this.SaveAsGif(e.Item, fileName, ImageHandlerConfig.Default);
                // this is too lag so we don't support dragging gifs!
                return;
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
