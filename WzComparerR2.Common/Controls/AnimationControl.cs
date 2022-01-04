using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WzComparerR2.Animation;
using WzComparerR2.Rendering;

namespace WzComparerR2.Controls
{
    public partial class AnimationControl : GraphicsDeviceControl
    {
        public AnimationControl()
        {
            InitializeComponent();
            this.MouseDown += AnimationControl_MouseDown;
            this.MouseUp += AnimationControl_MouseUp;
            this.MouseMove += AnimationControl_MouseMove;
            this.MouseWheel += AnimationControl_MouseWheel;

            this.Items = new List<AnimationItem>();
            this.MouseDragEnabled = true;
            this.GlobalScale = 1f;

            this.timer = new Timer();
            timer.Interval = 30;
            timer.Tick += Timer_Tick;
            timer.Enabled = true;
            this.sw = Stopwatch.StartNew();
        }

        public List<AnimationItem> Items { get; private set; }
        public bool MouseDragEnabled { get; set; }
        public bool MouseDragSaveEnabled { get; set; }
        public bool ShowPositionGridOnDrag { get; set; }

        public float GlobalScale
        {
            get { return this.globalScale; }
            set { this.globalScale = MathHelper.Clamp(value, 0.1f, 10f); }
        }

        public bool IsPlaying
        {
            get { return this.timer.Enabled; }
            set
            {
                if (value)
                {
                    this.lastUpdateTime = TimeSpan.Zero;
                    this.sw.Restart();
                }
                this.timer.Enabled = value;
            }
        }

        public int FrameInterval
        {
            get { return this.timer.Interval; }
            set { this.timer.Interval = value; }
        }

        private float globalScale;
        private Timer timer;
        private Stopwatch sw;
        private TimeSpan lastUpdateTime;

        private SpriteBatchEx sprite;
        private AnimationGraphics graphics;

        //拖拽相关
        private MouseDragContext mouseDragContext;

        //离屏绘制相关

        protected override void Initialize()
        {
            sprite = new SpriteBatchEx(this.GraphicsDevice);
            graphics = new AnimationGraphics(this.GraphicsDevice, sprite);
        }

        protected virtual void Update(TimeSpan elapsed)
        {
            foreach (var animation in this.Items)
            {
                if (animation != null)
                {
                    animation.Update(elapsed);
                }
            }
        }

        public virtual void DrawBackground()
        {
            this.GraphicsDevice.Clear(this.BackColor.ToXnaColor());
        }

        protected override void Draw()
        {
            //绘制背景色
            this.DrawBackground();
            //绘制场景
            foreach (var animation in this.Items)
            {
                if (animation != null)
                {
                    Matrix mt = Matrix.CreateRotationZ(MathHelper.PiOver2)
                        * Matrix.CreateTranslation(100, 100, 0);

                    mt = Matrix.CreateScale(GlobalScale, GlobalScale, 1);

                    if (animation is FrameAnimator)
                    {
                        graphics.Draw((FrameAnimator)animation, mt);
                    }
                    else if (animation is SpineAnimator)
                    {
                        graphics.Draw((SpineAnimator)animation, mt);
                    }
                    else if (animation is MultiFrameAnimator)
                    {
                        graphics.Draw((MultiFrameAnimator)animation, mt);
                    }
                }
            }

            //绘制辅助内容
            if (ShowPositionGridOnDrag && this.mouseDragContext.IsDragging && this.mouseDragContext.DraggingItem != null)
            {
                var pos = this.mouseDragContext.DraggingItem.Position;
                this.sprite.Begin();
                this.sprite.DrawLine(new Point(0, pos.Y), new Point(this.Width, pos.Y), 1, Color.Indigo);
                this.sprite.DrawLine(new Point(pos.X, 0), new Point(pos.X, this.Height), 1, Color.Indigo);
                this.sprite.End();
            }
        }

        public virtual AnimationItem GetItemAt(int x, int y)
        {
            for(int i = this.Items.Count - 1; i >= 0; i--)
            {
                var item = this.Items[i];
                var bound = item.Measure();
                var rect = new Rectangle(
                    (int)Math.Round(item.Position.X + bound.X* this.GlobalScale),
                    (int)Math.Round(item.Position.Y + bound.Y * this.GlobalScale),
                    (int)Math.Round(bound.Width * this.GlobalScale),
                    (int)Math.Round(bound.Height * this.GlobalScale));
                if (rect.Contains(x, y))
                {
                    return item;
                }
            }
            return null;
        }

        #region EVENTS
        protected virtual void OnItemDragSave(AnimationItemEventArgs e)
        {

        }


        private void AnimationControl_MouseDown(object sender, MouseEventArgs e)
        {
            this.Focus();

            if (this.MouseDragEnabled && e.Button == MouseButtons.Left)
            {
                var item = GetItemAt(e.X, e.Y);
                if (item != null)
                {
                    this.mouseDragContext.IsDragging = true;
                    this.mouseDragContext.MouseDownPoint = new Point(e.X, e.Y);
                    this.mouseDragContext.DraggingItem = item;
                    this.mouseDragContext.StartPosition = item.Position;
                }
            }
            if ((Control.ModifierKeys & Keys.Control) != 0 && e.Button == MouseButtons.Middle)
            {
                this.GlobalScale = 1f;
            }
        }

        private void AnimationControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (this.MouseDragEnabled && e.Button == MouseButtons.Left)
            {
                this.mouseDragContext.IsDragging = false;
            }
        }

        private void AnimationControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.MouseDragEnabled && this.mouseDragContext.IsDragging && this.mouseDragContext.DraggingItem != null)
            {
                this.mouseDragContext.DraggingItem.Position = new Point(
                    e.X - mouseDragContext.MouseDownPoint.X + mouseDragContext.StartPosition.X,
                    e.Y - mouseDragContext.MouseDownPoint.Y + mouseDragContext.StartPosition.Y);

                //处理拖拽保存
                if (this.MouseDragSaveEnabled && (Control.ModifierKeys & Keys.Control) != 0)
                {
                    var dragSize = SystemInformation.DragSize;
                    var dragBox = new Rectangle(mouseDragContext.MouseDownPoint, new Point(dragSize.Width, dragSize.Height));
                    if (!dragBox.Contains(new Point(e.X, e.Y)))
                    {
                        var e2 = new AnimationItemEventArgs(this.mouseDragContext.DraggingItem);
                        this.OnItemDragSave(e2);
                        if (e2.Handled)
                        {
                            this.mouseDragContext.IsDragging = false;
                        }
                    }
                }
            }
        }

        private void AnimationControl_MouseWheel(object sender, MouseEventArgs e)
        {
            if ((Control.ModifierKeys & Keys.Control) != 0)
            {
                this.GlobalScale += 0.1f * e.Delta / 120;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            var curTime = sw.Elapsed;
            var elapsed = curTime - lastUpdateTime;
            lastUpdateTime = curTime;

            if (this.Visible)
            {
                this.Update(elapsed);
                this.Invalidate();
            }
        }
        #endregion

        private struct MouseDragContext
        {
            public bool IsDragging;
            public Point MouseDownPoint;
            public Point StartPosition;
            public AnimationItem DraggingItem;
        }
    }
}
