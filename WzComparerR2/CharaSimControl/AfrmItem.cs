using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using CharaSimResource;
using WzComparerR2.CharaSim;
using WzComparerR2.Common;
using WzComparerR2.Controls;

namespace WzComparerR2.CharaSimControl
{
    public class AfrmItem : AlphaForm
    {
        public AfrmItem()
        {
            this.AllowDrop = true;
            initCtrl();
        }

        private ItemTab[] itemTabs;
        private bool fullMode;
        private bool selectedIndexChanging;
        private ACtrlVScroll vScroll;
        private Character character;

        private ACtrlButton btnFull;
        private ACtrlButton btnSmall;
        private ACtrlButton btnCoin3;
        private ACtrlButton btnCoin4;
        private ACtrlButton btnPoint;
        private ACtrlButton btnGather;
        private ACtrlButton btnSort;
        private ACtrlButton btnDisassemble3;
        private ACtrlButton btnDisassemble4;
        private ACtrlButton btnExtract3;
        private ACtrlButton btnExtract4;
        private ACtrlButton btnAppraise3;
        private ACtrlButton btnAppraise4;
        private ACtrlButton btnBits3;
        private ACtrlButton btnBits4;
        private ACtrlButton btnPot3;
        private ACtrlButton btnPot4;
        private ACtrlButton btnUpgrade3;
        private ACtrlButton btnUpgrade4;
        private ACtrlButton btnToad3;
        private ACtrlButton btnToad4;
        private ACtrlButton btnCashshop;
        private ACtrlButton btnClose;
        private bool waitForRefresh;

        public event ItemMouseEventHandler ItemMouseDown;
        public event ItemMouseEventHandler ItemMouseUp;
        public event ItemMouseEventHandler ItemMouseClick;
        public event ItemMouseEventHandler ItemMouseMove;
        public event EventHandler ItemMouseLeave;

        public Character Character
        {
            get { return character; }
            set
            {
                character = value;

                for (int i = 0; i < this.itemTabs.Length; i++)
                {
                    if (this.character == null || !this.itemTabs[i].SetItemSource(character.ItemSlots[i]))
                    {
                        this.itemTabs[i].ClearItems();
                    }
                }
            }
        }

        private void initCtrl()
        {
            this.itemTabs = new ItemTab[5];
            for (int i = 0; i < itemTabs.Length; i++)
            {
                this.itemTabs[i] = new ItemTab(this);
                this.itemTabs[i].TabEnabled = new BitmapOrigin((Bitmap)Resource.ResourceManager.GetObject("Item_Tab_enabled_" + i),
                   -9 - 31 * i, -28);
                this.itemTabs[i].TabDisabled = new BitmapOrigin((Bitmap)Resource.ResourceManager.GetObject("Item_Tab_disabled_" + i),
                    -9 - 31 * i, -28);
            }
            this.itemTabs[0].Selected = true;

            this.vScroll = new ACtrlVScroll();

            this.vScroll.PicBase.Normal = new BitmapOrigin(Resource.VScr9_enabled_base);
            this.vScroll.PicBase.Disabled = new BitmapOrigin(Resource.VScr9_disabled_base);

            this.vScroll.BtnPrev.Normal = new BitmapOrigin(Resource.VScr9_enabled_prev0);
            this.vScroll.BtnPrev.Pressed = new BitmapOrigin(Resource.VScr9_enabled_prev1);
            this.vScroll.BtnPrev.MouseOver = new BitmapOrigin(Resource.VScr9_enabled_prev2);
            this.vScroll.BtnPrev.Disabled = new BitmapOrigin(Resource.VScr9_disabled_prev);
            this.vScroll.BtnPrev.Size = this.vScroll.BtnPrev.Normal.Bitmap.Size;
            this.vScroll.BtnPrev.Location = new Point(0, 0);

            this.vScroll.BtnNext.Normal = new BitmapOrigin(Resource.VScr9_enabled_next0);
            this.vScroll.BtnNext.Pressed = new BitmapOrigin(Resource.VScr9_enabled_next1);
            this.vScroll.BtnNext.MouseOver = new BitmapOrigin(Resource.VScr9_enabled_next2);
            this.vScroll.BtnNext.Disabled = new BitmapOrigin(Resource.VScr9_disabled_next);
            this.vScroll.BtnNext.Size = this.vScroll.BtnNext.Normal.Bitmap.Size;
            this.vScroll.BtnNext.Location = new Point(0, 195);

            this.vScroll.BtnThumb.Normal = new BitmapOrigin(Resource.VScr9_enabled_thumb0);
            this.vScroll.BtnThumb.Pressed = new BitmapOrigin(Resource.VScr9_enabled_thumb1);
            this.vScroll.BtnThumb.MouseOver = new BitmapOrigin(Resource.VScr9_enabled_thumb2);
            this.vScroll.BtnThumb.Size = this.vScroll.BtnThumb.Normal.Bitmap.Size;

            this.vScroll.Location = new Point(152, 51);
            this.vScroll.Size = new Size(11, 207);
            this.vScroll.ScrollableLocation = new Point(10, 51);
            this.vScroll.ScrollableSize = new Size(153, 207);
            this.vScroll.ValueChanged += new EventHandler(vScroll_ValueChanged);
            this.vScroll.ChildButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnFull = new ACtrlButton();
            this.btnFull.Normal = new BitmapOrigin(Resource.Item_BtFull_normal_0);
            this.btnFull.Pressed = new BitmapOrigin(Resource.Item_BtFull_pressed_0);
            this.btnFull.MouseOver = new BitmapOrigin(Resource.Item_BtFull_mouseOver_0);
            this.btnFull.Disabled = new BitmapOrigin(Resource.Item_BtFull_disabled_0);
            this.btnFull.Location = new Point(147, 267);
            this.btnFull.Size = new Size(16, 16);
            this.btnFull.MouseClick += new MouseEventHandler(btnFull_MouseClick);
            this.btnFull.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnSmall = new ACtrlButton();
            this.btnSmall.Normal = new BitmapOrigin(Resource.Item_BtSmall_normal_0);
            this.btnSmall.Pressed = new BitmapOrigin(Resource.Item_BtSmall_pressed_0);
            this.btnSmall.MouseOver = new BitmapOrigin(Resource.Item_BtSmall_mouseOver_0);
            this.btnSmall.Disabled = new BitmapOrigin(Resource.Item_BtSmall_disabled_0);
            this.btnSmall.Location = new Point(153, 337);
            this.btnSmall.Size = new Size(16, 16);
            this.btnSmall.MouseClick += new MouseEventHandler(btnSmall_MouseClick);
            this.btnSmall.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnCoin3 = new ACtrlButton();
            this.btnCoin3.Normal = new BitmapOrigin(Resource.Item_BtCoin3_normal_0);
            this.btnCoin3.Pressed = new BitmapOrigin(Resource.Item_BtCoin3_pressed_0);
            this.btnCoin3.MouseOver = new BitmapOrigin(Resource.Item_BtCoin3_mouseOver_0);
            this.btnCoin3.Disabled = new BitmapOrigin(Resource.Item_BtCoin3_disabled_0);
            this.btnCoin3.Location = new Point(9, 267);
            this.btnCoin3.Size = new Size(38, 16);
            this.btnCoin3.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnCoin4 = new ACtrlButton();
            this.btnCoin4.Normal = new BitmapOrigin(Resource.Item_BtCoin4_normal_0);
            this.btnCoin4.Pressed = new BitmapOrigin(Resource.Item_BtCoin4_pressed_0);
            this.btnCoin4.MouseOver = new BitmapOrigin(Resource.Item_BtCoin4_mouseOver_0);
            this.btnCoin4.Disabled = new BitmapOrigin(Resource.Item_BtCoin4_disabled_0);
            this.btnCoin4.Location = new Point(9, 337);
            this.btnCoin4.Size = new Size(40, 16);
            this.btnCoin4.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnPoint = new ACtrlButton();
            this.btnPoint.Normal = new BitmapOrigin(Resource.Item_BtPoint_normal_0);
            this.btnPoint.Pressed = new BitmapOrigin(Resource.Item_BtPoint_pressed_0);
            this.btnPoint.MouseOver = new BitmapOrigin(Resource.Item_BtPoint_mouseOver_0);
            this.btnPoint.Disabled = new BitmapOrigin(Resource.Item_BtPoint_disabled_0);
            this.btnPoint.Location = new Point(9, 285);
            this.btnPoint.Size = new Size(82, 16);
            this.btnPoint.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnGather = new ACtrlButton();
            this.btnGather.Normal = new BitmapOrigin(Resource.Item_BtGather_normal_0);
            this.btnGather.Pressed = new BitmapOrigin(Resource.Item_BtGather_pressed_0);
            this.btnGather.MouseOver = new BitmapOrigin(Resource.Item_BtGather_mouseOver_0);
            this.btnGather.Disabled = new BitmapOrigin(Resource.Item_BtGather_disabled_0);
            this.btnGather.Location = new Point(130, 267);
            this.btnGather.Size = new Size(16, 16);
            this.btnGather.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);
            this.btnGather.MouseClick += new MouseEventHandler(btnGather_MouseClick);

            this.btnSort = new ACtrlButton();
            this.btnSort.Normal = new BitmapOrigin(Resource.Item_BtSort_normal_0);
            this.btnSort.Pressed = new BitmapOrigin(Resource.Item_BtSort_pressed_0);
            this.btnSort.MouseOver = new BitmapOrigin(Resource.Item_BtSort_mouseOver_0);
            this.btnSort.Disabled = new BitmapOrigin(Resource.Item_BtSort_disabled_0);
            this.btnSort.Location = new Point(129, 267);
            this.btnSort.Size = new Size(16, 16);
            this.btnSort.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);
            this.btnSort.MouseClick += new MouseEventHandler(btnSort_MouseClick);

            this.btnDisassemble3 = new ACtrlButton();
            this.btnDisassemble3.Normal = new BitmapOrigin(Resource.Item_BtDisassemble3_normal_0);
            this.btnDisassemble3.Pressed = new BitmapOrigin(Resource.Item_BtDisassemble3_pressed_0);
            this.btnDisassemble3.MouseOver = new BitmapOrigin(Resource.Item_BtDisassemble3_mouseOver_0);
            this.btnDisassemble3.Disabled = new BitmapOrigin(Resource.Item_BtDisassemble3_disabled_0);
            this.btnDisassemble3.Location = new Point(9, 303);
            this.btnDisassemble3.Size = new Size(24, 24);
            this.btnDisassemble3.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnDisassemble4 = new ACtrlButton();
            this.btnDisassemble4.Normal = new BitmapOrigin(Resource.Item_BtDisassemble4_normal_0);
            this.btnDisassemble4.Pressed = new BitmapOrigin(Resource.Item_BtDisassemble4_pressed_0);
            this.btnDisassemble4.MouseOver = new BitmapOrigin(Resource.Item_BtDisassemble4_mouseOver_0);
            this.btnDisassemble4.Disabled = new BitmapOrigin(Resource.Item_BtDisassemble4_disabled_0);
            this.btnDisassemble4.Location = new Point(412, 337);
            this.btnDisassemble4.Size = new Size(16, 16);
            this.btnDisassemble4.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnExtract3 = new ACtrlButton();
            this.btnExtract3.Normal = new BitmapOrigin(Resource.Item_BtExtract3_normal_0);
            this.btnExtract3.Pressed = new BitmapOrigin(Resource.Item_BtExtract3_pressed_0);
            this.btnExtract3.MouseOver = new BitmapOrigin(Resource.Item_BtExtract3_mouseOver_0);
            this.btnExtract3.Disabled = new BitmapOrigin(Resource.Item_BtExtract3_disabled_0);
            this.btnExtract3.Location = new Point(35, 303);
            this.btnExtract3.Size = new Size(24, 24);
            this.btnExtract3.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnExtract4 = new ACtrlButton();
            this.btnExtract4.Normal = new BitmapOrigin(Resource.Item_BtExtract4_normal_0);
            this.btnExtract4.Pressed = new BitmapOrigin(Resource.Item_BtExtract4_pressed_0);
            this.btnExtract4.MouseOver = new BitmapOrigin(Resource.Item_BtExtract4_mouseOver_0);
            this.btnExtract4.Disabled = new BitmapOrigin(Resource.Item_BtExtract4_disabled_0);
            this.btnExtract4.Location = new Point(430, 337);
            this.btnExtract4.Size = new Size(16, 16);
            this.btnExtract4.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnAppraise3 = new ACtrlButton();
            this.btnAppraise3.Normal = new BitmapOrigin(Resource.Item_BtAppraise3_normal_0);
            this.btnAppraise3.Pressed = new BitmapOrigin(Resource.Item_BtAppraise3_pressed_0);
            this.btnAppraise3.MouseOver = new BitmapOrigin(Resource.Item_BtAppraise3_mouseOver_0);
            this.btnAppraise3.Disabled = new BitmapOrigin(Resource.Item_BtAppraise3_disabled_0);
            this.btnAppraise3.Location = new Point(61, 303);
            this.btnAppraise3.Size = new Size(24, 24);
            this.btnAppraise3.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnAppraise4 = new ACtrlButton();
            this.btnAppraise4.Normal = new BitmapOrigin(Resource.Item_BtAppraise4_normal_0);
            this.btnAppraise4.Pressed = new BitmapOrigin(Resource.Item_BtAppraise4_pressed_0);
            this.btnAppraise4.MouseOver = new BitmapOrigin(Resource.Item_BtAppraise4_mouseOver_0);
            this.btnAppraise4.Disabled = new BitmapOrigin(Resource.Item_BtAppraise4_disabled_0);
            this.btnAppraise4.Location = new Point(448, 337);
            this.btnAppraise4.Size = new Size(16, 16);
            this.btnAppraise4.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnBits3 = new ACtrlButton();
            this.btnBits3.Normal = new BitmapOrigin(Resource.Item_BtBits3_normal_0);
            this.btnBits3.Pressed = new BitmapOrigin(Resource.Item_BtBits3_pressed_0);
            this.btnBits3.MouseOver = new BitmapOrigin(Resource.Item_BtBits3_mouseOver_0);
            this.btnBits3.Disabled = new BitmapOrigin(Resource.Item_BtBits3_disabled_0);
            this.btnBits3.Location = new Point(113, 303);
            this.btnBits3.Size = new Size(24, 24);
            this.btnBits3.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnBits4 = new ACtrlButton();
            this.btnBits4.Normal = new BitmapOrigin(Resource.Item_BtBits4_normal_0);
            this.btnBits4.Pressed = new BitmapOrigin(Resource.Item_BtBits4_pressed_0);
            this.btnBits4.MouseOver = new BitmapOrigin(Resource.Item_BtBits4_mouseOver_0);
            this.btnBits4.Disabled = new BitmapOrigin(Resource.Item_BtBits4_disabled_0);
            this.btnBits4.Location = new Point(484, 337);
            this.btnBits4.Size = new Size(16, 16);
            this.btnBits4.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnPot3 = new ACtrlButton();
            this.btnPot3.Normal = new BitmapOrigin(Resource.Item_BtPot3_normal_0);
            this.btnPot3.Pressed = new BitmapOrigin(Resource.Item_BtPot3_pressed_0);
            this.btnPot3.MouseOver = new BitmapOrigin(Resource.Item_BtPot3_mouseOver_0);
            this.btnPot3.Disabled = new BitmapOrigin(Resource.Item_BtPot3_disabled_0);
            this.btnPot3.Location = new Point(87, 303);
            this.btnPot3.Size = new Size(24, 24);
            this.btnPot3.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnPot4 = new ACtrlButton();
            this.btnPot4.Normal = new BitmapOrigin(Resource.Item_BtPot4_normal_0);
            this.btnPot4.Pressed = new BitmapOrigin(Resource.Item_BtPot4_pressed_0);
            this.btnPot4.MouseOver = new BitmapOrigin(Resource.Item_BtPot4_mouseOver_0);
            this.btnPot4.Disabled = new BitmapOrigin(Resource.Item_BtPot4_disabled_0);
            this.btnPot4.Location = new Point(466, 337);
            this.btnPot4.Size = new Size(16, 16);
            this.btnPot4.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnUpgrade3 = new ACtrlButton();
            this.btnUpgrade3.Normal = new BitmapOrigin(Resource.Item_BtUpgrade3_normal_0);
            this.btnUpgrade3.Pressed = new BitmapOrigin(Resource.Item_BtUpgrade3_pressed_0);
            this.btnUpgrade3.MouseOver = new BitmapOrigin(Resource.Item_BtUpgrade3_mouseOver_0);
            this.btnUpgrade3.Disabled = new BitmapOrigin(Resource.Item_BtUpgrade3_disabled_0);
            this.btnUpgrade3.Location = new Point(139, 303);
            this.btnUpgrade3.Size = new Size(24, 24);
            this.btnUpgrade3.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnUpgrade4 = new ACtrlButton();
            this.btnUpgrade4.Normal = new BitmapOrigin(Resource.Item_BtUpgrade4_normal_0);
            this.btnUpgrade4.Pressed = new BitmapOrigin(Resource.Item_BtUpgrade4_pressed_0);
            this.btnUpgrade4.MouseOver = new BitmapOrigin(Resource.Item_BtUpgrade4_mouseOver_0);
            this.btnUpgrade4.Disabled = new BitmapOrigin(Resource.Item_BtUpgrade4_disabled_0);
            this.btnUpgrade4.Location = new Point(392, 337);
            this.btnUpgrade4.Size = new Size(16, 16);
            this.btnUpgrade4.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnToad3 = new ACtrlButton();
            this.btnToad3.Normal = new BitmapOrigin(Resource.Item_BtToad3_normal_0);
            this.btnToad3.Pressed = new BitmapOrigin(Resource.Item_BtToad3_pressed_0);
            this.btnToad3.MouseOver = new BitmapOrigin(Resource.Item_BtToad3_mouseOver_0);
            this.btnToad3.Disabled = new BitmapOrigin(Resource.Item_BtToad3_disabled_0);
            this.btnToad3.Location = new Point(113, 303);
            this.btnToad3.Size = new Size(24, 24);
            this.btnToad3.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnToad4 = new ACtrlButton();
            this.btnToad4.Normal = new BitmapOrigin(Resource.Item_BtToad4_normal_0);
            this.btnToad4.Pressed = new BitmapOrigin(Resource.Item_BtToad4_pressed_0);
            this.btnToad4.MouseOver = new BitmapOrigin(Resource.Item_BtToad4_mouseOver_0);
            this.btnToad4.Disabled = new BitmapOrigin(Resource.Item_BtToad4_disabled_0);
            this.btnToad4.Location = new Point(484, 337);
            this.btnToad4.Size = new Size(16, 16);
            this.btnToad4.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnCashshop = new ACtrlButton();
            this.btnCashshop.Normal = new BitmapOrigin(Resource.Item_BtCashshop_normal_0);
            this.btnCashshop.Pressed = new BitmapOrigin(Resource.Item_BtCashshop_pressed_0);
            this.btnCashshop.MouseOver = new BitmapOrigin(Resource.Item_BtCashshop_mouseOver_0);
            this.btnCashshop.Disabled = new BitmapOrigin(Resource.Item_BtCashshop_disabled_0);
            this.btnCashshop.Location = new Point(502, 337);
            this.btnCashshop.Size = new Size(82, 16);
            this.btnCashshop.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnClose = new ACtrlButton();
            this.btnClose.Normal = new BitmapOrigin(Resource.BtClose3_normal_0);
            this.btnClose.Pressed = new BitmapOrigin(Resource.BtClose3_pressed_0);
            this.btnClose.MouseOver = new BitmapOrigin(Resource.BtClose3_mouseOver_0);
            this.btnClose.Disabled = new BitmapOrigin(Resource.BtClose3_disabled_0);
            this.btnClose.Location = new Point(150, 6);
            this.btnClose.Size = new Size(13, 13);
            this.btnClose.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);
            this.btnClose.MouseClick += new MouseEventHandler(btnClose_MouseClick);
        }

        public override void Refresh()
        {
            this.preRender();
            this.SetBitmap(this.Bitmap);
            this.CaptionRectangle = new Rectangle(0, 0, this.Bitmap.Width, 24);
            base.Refresh();
        }

        protected override bool captionHitTest(Point point)
        {
            if (this.btnClose.Rectangle.Contains(point))
                return false;
            return base.captionHitTest(point);
        }

        private void preRender()
        {
            if (Bitmap != null)
                Bitmap.Dispose();

            if (this.btnGather.Visible)
            {
                this.btnSort.Visible = false;
            }
            else
            {
                this.btnSort.Visible = true;
            }

            if (this.fullMode)
            {
                this.btnFull.Visible = false;
                this.btnSmall.Visible = true;
                this.vScroll.Visible = false;
                this.btnCoin3.Visible = false;
                this.btnCoin4.Visible = true;
                this.btnPoint.Location = new Point(190, 337);
                this.btnGather.Location = new Point(135, 337);
                this.btnSort.Location = new Point(135, 337);
                this.btnDisassemble3.Visible = false;
                this.btnDisassemble4.Visible = true;
                this.btnExtract3.Visible = false;
                this.btnExtract4.Visible = true;
                this.btnAppraise3.Visible = false;
                this.btnAppraise4.Visible = true;
                this.btnBits3.Visible = false;
                this.btnBits4.Visible = true;
                this.btnPot3.Visible = false;
                this.btnPot4.Visible = true;
                this.btnUpgrade3.Visible = false;
                this.btnUpgrade4.Visible = true;
                this.btnToad3.Visible = false;
                this.btnToad4.Visible = true;
                this.btnCashshop.Visible = true;
                this.btnClose.Location = new Point(574, 6);
                renderFull();
            }
            else
            {
                this.btnFull.Visible = true;
                this.btnSmall.Visible = false;
                this.vScroll.Visible = true;
                this.vScroll.Maximum = this.SelectedTab.ScrollMaxValue - 6;
                this.vScroll.Value = this.SelectedTab.ScrollValue;
                this.btnCoin3.Visible = true;
                this.btnCoin4.Visible = false;
                this.btnPoint.Location = new Point(9, 285);
                this.btnGather.Location = new Point(130, 267);
                this.btnSort.Location = new Point(130, 267);
                this.btnDisassemble3.Visible = true;
                this.btnDisassemble4.Visible = false;
                this.btnExtract3.Visible = true;
                this.btnExtract4.Visible = false;
                this.btnAppraise3.Visible = true;
                this.btnAppraise4.Visible = false;
                this.btnBits3.Visible = true;
                this.btnBits4.Visible = false;
                this.btnPot3.Visible = true;
                this.btnPot4.Visible = false;
                this.btnUpgrade3.Visible = true;
                this.btnUpgrade4.Visible = false;
                this.btnToad3.Visible = true;
                this.btnToad4.Visible = false;
                this.btnCashshop.Visible = false;
                this.btnClose.Location = new Point(150, 6);
                renderSmall();
            }
        }

        private void renderSmall()
        {
            this.Bitmap = new Bitmap(Resource.Item_backgrnd);
            Graphics g = Graphics.FromImage(this.Bitmap);
            g.DrawImage(Resource.Item_backgrnd2, 6, 23);
            renderTabs(g);
            g.DrawImage(Resource.Item_backgrnd3, 7, 45);
            foreach (AControl ctrl in this.aControls)
            {
                ctrl.Draw(g);
            }

            ItemBase[] itemArray = this.SelectedTab.Items;
            int idxOffset = 4 * this.SelectedTab.ScrollValue;
            for (int i = 0; i < 24; i++)
            {
                Point origin = getItemIconOrigin(i);
                origin.Offset(0, 32);
                renderItemBase(g, itemArray[i + idxOffset], origin);
            }

            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Far;
            g.DrawString("0", GearGraphics.TahomaFont, Brushes.Black, 128f, 270f, format);
            g.DrawString("0", GearGraphics.TahomaFont, Brushes.Black, 160f, 287f, format);

            g.Dispose();
        }

        private void renderFull()
        {
            this.Bitmap = new Bitmap(Resource.Item_FullBackgrnd);
            Graphics g = Graphics.FromImage(this.Bitmap);
            g.DrawImage(Resource.Item_FullBackgrnd2, 6, 23);
            renderTabs(g);
            g.DrawImage(Resource.Item_FullBackgrnd3, 7, 46);
            foreach (AControl ctrl in this.aControls)
            {
                ctrl.Draw(g);
            }

            ItemBase[] itemArray = this.SelectedTab.Items;
            for (int i = 0; i < itemArray.Length; i++)
            {
                int idx = i % 32, group = i / 32;
                Point origin = getItemIconOrigin(i);
                origin.Offset(0, 32);
                renderItemBase(g, itemArray[i], origin);
            }

            StringFormat format = new StringFormat();
            format.Alignment = StringAlignment.Far;
            g.DrawString("0", GearGraphics.TahomaFont, Brushes.Black, 131f, 340f, format);
            g.DrawString("0", GearGraphics.TahomaFont, Brushes.Black, 341f, 340f, format);

            g.Dispose();
        }

        private void renderTabs(Graphics g)
        {
            for (int i = 0; i < this.itemTabs.Length; i++)
            {
                if (this.itemTabs[i].Selected)
                {
                    Point pos = this.itemTabs[i].TabEnabled.OpOrigin;
                    // if (this.fullMode)
                    pos.Offset(0, -2);
                    g.DrawImage(this.itemTabs[i].TabEnabled.Bitmap, pos);
                }
                else
                {
                    g.DrawImage(this.itemTabs[i].TabDisabled.Bitmap, this.itemTabs[i].TabEnabled.OpOrigin);
                }
            }
        }

        private void renderItemBase(Graphics g, ItemBase itemBase, Point origin)
        {
            if (itemBase is Gear)
                renderGear(g, itemBase as Gear, origin);
            else if (itemBase is Item)
                renderItem(g, itemBase as Item, origin);
        }

        private void renderGear(Graphics g, Gear gear, Point origin)
        {
            if (g == null || gear == null)
                return;
            Pen pen = GearGraphics.GetGearItemBorderPen(gear.Grade);
            if (pen != null)
            {
                Point[] path = GearGraphics.GetIconBorderPath(origin.X, origin.Y - 32);
                g.DrawLines(pen, path);
            }
            g.DrawImage(Resource.Item_shadow, origin.X + 3, origin.Y - 6);
            if (gear.IconRaw.Bitmap != null)
            {
                g.DrawImage(gear.IconRaw.Bitmap, origin.X - gear.IconRaw.Origin.X, origin.Y - gear.IconRaw.Origin.Y);
            }
            if (gear.Cash)
            {
                Bitmap cashImg = null;

                int value;
                if (gear.Props.TryGetValue(GearPropType.royalSpecial, out value) && value > 0)
                {
                    string resKey = $"CashShop_img_CashItem_label_{value - 1}";
                    cashImg = Resource.ResourceManager.GetObject(resKey) as Bitmap;
                }
                else if (gear.Props.TryGetValue(GearPropType.masterSpecial, out value) && value > 0)
                {
                    cashImg = Resource.CashItem_label_3;
                }
                if (cashImg == null) //default cashImg
                {
                    cashImg = Resource.CashItem_0;
                }

                g.DrawImage(cashImg, origin.X + 20, origin.Y - 12);
            }
            if (gear.TimeLimited)
            {
                g.DrawImage(Resource.Item_timeLimit_0, origin.X, origin.Y - 32);
            }
        }

        private void renderItem(Graphics g, Item item, Point origin)
        {
            if (g == null || item == null)
                return;
            g.DrawImage(Resource.Item_shadow, origin.X + 3, origin.Y - 6);
            if (item.IconRaw.Bitmap != null)
            {
                g.DrawImage(item.IconRaw.Bitmap, origin.X - item.IconRaw.Origin.X, origin.Y - item.IconRaw.Origin.Y);
            }
            if (item.Cash)
            {
                Bitmap cashImg = null;

                int value;
                if (item.Props.TryGetValue(ItemPropType.wonderGrade, out value) && value > 0)
                {
                    string resKey = $"CashShop_img_CashItem_label_{value + 3}";
                    cashImg = Resource.ResourceManager.GetObject(resKey) as Bitmap;
                }
                if (cashImg == null) //default cashImg
                {
                    cashImg = Resource.CashItem_0;
                }

                g.DrawImage(cashImg, origin.X + 20, origin.Y - 12);
            }
            if (item.TimeLimited)
            {
                g.DrawImage(Resource.Item_timeLimit_0, origin.X, origin.Y - 32);
            }
            if (item.ItemID / 1000 == 3017)
            {
                g.DrawImage(Resource.Item_monsterCollection_0, origin.X, origin.Y - 32);
            }
        }

        private Point getItemIconOrigin(int index)
        {
            int idx = index % 24, group = index / 24;
            Point p = new Point((idx % 4 + group * 4) * 36, idx / 4 * 35);
            p.Offset(10, 51);
            return p;
        }

        public int GetSlotIndexByPoint(Point point)
        {
            Point p = point;
            p.Offset(-10, -51);
            if (p.X < 0 || p.Y < 0)
                return -1;
            int x = p.X / 36, y = p.Y / 35;
            if ((fullMode ? y >= 8 : y >= 6) || (fullMode ? x >= 16 : x >= 4))
                return -1;
            int idx = y * 4 + x % 4 + x / 4 * 32;
            if (new Rectangle(getItemIconOrigin(idx), new Size(33, 33)).Contains(point))
                return idx;
            else
                return -1;
        }

        public int GetItemIndexByPoint(Point point)
        {
            int slotIdx = GetSlotIndexByPoint(point);
            if (slotIdx != -1 && !this.fullMode)
            {
                slotIdx += 4 * this.SelectedTab.ScrollValue;
            }
            return slotIdx;
        }

        public ItemBase GetItemByPoint(Point point)
        {
            int itemIdx = GetItemIndexByPoint(point);
            if (itemIdx > -1 && itemIdx < this.SelectedTab.Items.Length)
                return this.SelectedTab.Items[itemIdx];
            else
                return null;
        }

        #region 重写和响应事件
        protected override void OnMouseMove(MouseEventArgs e)
        {
            foreach (AControl ctrl in this.aControls)
            {
                ctrl.OnMouseMove(e);
            }

            if (this.waitForRefresh)
            {
                this.Refresh();
                waitForRefresh = false;
            }

            base.OnMouseMove(e);

            ItemBase item = GetItemByPoint(e.Location);
            if (item != null)
                this.OnItemMouseMove(new ItemMouseEventArgs(e, item));
            else
                this.OnItemMouseLeave(EventArgs.Empty);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            foreach (AControl ctrl in this.aControls)
            {
                ctrl.OnMouseDown(e);
            }

            if (this.waitForRefresh)
            {
                this.Refresh();
                waitForRefresh = false;
            }

            base.OnMouseDown(e);

            ItemBase item = GetItemByPoint(e.Location);
            if (item != null)
                this.OnItemMouseDown(new ItemMouseEventArgs(e, item));
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            foreach (AControl ctrl in this.aControls)
            {
                ctrl.OnMouseUp(e);
            }

            if (this.waitForRefresh)
            {
                this.Refresh();
                waitForRefresh = false;
            }

            base.OnMouseUp(e);

            ItemBase item = GetItemByPoint(e.Location);
            if (item != null)
                this.OnItemMouseUp(new ItemMouseEventArgs(e, item));
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            //处理选择选项卡
            tab_OnMouseClick(e);

            foreach (AControl ctrl in this.aControls)
            {
                ctrl.OnMouseClick(e);
            }

            if (this.waitForRefresh)
            {
                this.Refresh();
                waitForRefresh = false;
            }

            base.OnMouseClick(e);

            ItemBase item = GetItemByPoint(e.Location);
            if (item != null)
                this.OnItemMouseClick(new ItemMouseEventArgs(e, item));
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            foreach (AControl ctrl in this.aControls)
            {
                ctrl.OnMouseWheel(e);
            }

            if (this.waitForRefresh)
            {
                this.Refresh();
                waitForRefresh = false;
            }

            base.OnMouseWheel(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Tab)
            {
                this.SelectedIndex = (this.SelectedIndex + 1) % this.itemTabs.Length;
                this.Refresh();
            }

            base.OnKeyDown(e);
        }

        protected virtual void OnItemMouseDown(ItemMouseEventArgs e)
        {
            if (this.ItemMouseDown != null)
                this.ItemMouseDown(this, e);
        }

        protected virtual void OnItemMouseUp(ItemMouseEventArgs e)
        {
            if (this.ItemMouseUp != null)
                this.ItemMouseUp(this, e);
        }

        protected virtual void OnItemMouseClick(ItemMouseEventArgs e)
        {
            if (this.ItemMouseClick != null)
                this.ItemMouseClick(this, e);
        }

        protected virtual void OnItemMouseMove(ItemMouseEventArgs e)
        {
            if (this.ItemMouseMove != null)
                this.ItemMouseMove(this, e);
        }

        protected virtual void OnItemMouseLeave(EventArgs e)
        {
            if (this.ItemMouseLeave != null)
                this.ItemMouseLeave(this, e);
        }
        #endregion

        private void tab_OnMouseClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                for (int i = 0; i < this.itemTabs.Length; i++)
                {
                    Rectangle rect;
                    if (this.itemTabs[i].Selected)
                    {
                        rect = this.itemTabs[i].TabEnabled.Rectangle;
                    }
                    else
                    {
                        rect = this.itemTabs[i].TabDisabled.Rectangle;
                    }
                    if (rect.Contains(e.Location))
                    {
                        if (this.SelectedIndex != i)
                        {
                            this.SelectedIndex = i;
                            this.btnGather.Visible = true; //切换tab时重置btnGather状态
                            this.waitForRefresh = true;
                        }
                        break;
                    }
                }
            }
        }

        private void btnSmall_MouseClick(object sender, MouseEventArgs e)
        {
            this.fullMode = false;
            this.waitForRefresh = true;
        }

        private void btnFull_MouseClick(object sender, MouseEventArgs e)
        {
            this.fullMode = true;
            this.waitForRefresh = true;
        }

        private void vScroll_ValueChanged(object sender, EventArgs e)
        {
            this.SelectedTab.ScrollValue = this.vScroll.Value;
            this.waitForRefresh = true;
        }

        private void btnGather_MouseClick(object sender, MouseEventArgs e)
        {
            this.btnGather.Visible = !this.btnGather.Visible;
            this.gather();
            this.waitForRefresh = true;
        }

        private void btnSort_MouseClick(object sender, MouseEventArgs e)
        {
            this.btnGather.Visible = !this.btnGather.Visible;
            this.sort();
            this.waitForRefresh = true;
        }

        private void btnClose_MouseClick(object sender, MouseEventArgs e)
        {
            this.Visible = false;
        }

        private void aCtrl_RefreshCall(object sender, EventArgs e)
        {
            this.waitForRefresh = true;
        }

        private void gather()
        {
            ItemBase[] itemArray = this.SelectedTab.Items;
            Queue<int> nullQueue = new Queue<int>();
            for (int i = 0; i < itemArray.Length; i++)
            {
                if (itemArray[i] == null)
                {
                    nullQueue.Enqueue(i);
                }
                else if (nullQueue.Count > 0)
                {
                    int nullIdx = nullQueue.Dequeue();
                    itemArray[nullIdx] = itemArray[i];
                    itemArray[i] = null;
                    nullQueue.Enqueue(i);
                }
            }
        }

        private void sort()
        {
            ItemBase[] itemArray = this.SelectedTab.Items;
            Array.Sort<ItemBase>(itemArray, (a, b) =>
            {
                if (a == null) return 1;
                if (b == null) return -1;
                return a.ItemID - b.ItemID;
            });
        }

        private IEnumerable<AControl> aControls
        {
            get
            {
                yield return this.vScroll;
                yield return this.btnFull;
                yield return this.btnSmall;
                yield return this.btnCoin3;
                yield return this.btnCoin4;
                yield return this.btnPoint;
                yield return this.btnGather;
                yield return this.btnSort;
                yield return this.btnDisassemble3;
                yield return this.btnDisassemble4;
                yield return this.btnExtract3;
                yield return this.btnExtract4;
                yield return this.btnAppraise3;
                yield return this.btnAppraise4;
                yield return this.btnBits3;
                yield return this.btnBits4;
                yield return this.btnPot3;
                yield return this.btnPot4;
                yield return this.btnUpgrade3;
                yield return this.btnUpgrade4;
                yield return this.btnToad3;
                yield return this.btnToad4;
                yield return this.btnCashshop;
                yield return this.btnClose;
            }
        }

        public ItemTab[] ItemTabs
        {
            get { return this.itemTabs; }
        }

        /// <summary>
        /// 获取或设置一个bool值，它表示是否当前背包显示为大背包模式。
        /// </summary>
        public bool FullMode
        {
            get { return fullMode; }
            set { fullMode = value; }
        }

        /// <summary>
        /// 获取或设置正在选中的背包选项卡的索引。
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                for (int i = 0; i < this.itemTabs.Length; i++)
                {
                    if (this.itemTabs[i].Selected)
                        return i;
                }
                this.itemTabs[0].Selected = true;
                return 0;
            }
            set
            {
                value = Math.Min(Math.Max(value, 0), this.itemTabs.Length - 1);
                this.selectedIndexChanging = true;
                for (int i = 0; i < this.itemTabs.Length; i++)
                {
                    this.itemTabs[i].Selected = (i == value);
                }
                this.selectedIndexChanging = false;
            }
        }

        public bool AddItem(ItemBase item)
        {
            if (item == null)
                return false;

            int idx;
            switch (item.Type)
            {
                case ItemBaseType.Equip: idx = 0; break;
                case ItemBaseType.Consume: idx = 1; break;
                case ItemBaseType.Install: idx = 3; break;
                case ItemBaseType.Etc: idx = 2; break;
                case ItemBaseType.Cash: idx = 4; break;
                default: return false;
            }

            ItemBase[] itemArray = this.itemTabs[idx].Items;

            for (int i = 0; i < itemArray.Length; i++)
            {
                if (itemArray[i] == null)
                {
                    itemArray[i] = (ItemBase)item.Clone();
                    this.SelectedIndex = idx;
                    this.itemTabs[idx].ScrollValue = (i - 20) / 4;
                    this.Refresh();
                    return true;
                }
            }
            return false;
        }

        public void RemoveItem(int index)
        {
            ItemBase[] itemArray = this.SelectedTab.Items;
            if (index >= 0 && index < itemArray.Length && itemArray[index] != null)
            {
                itemArray[index] = null;
                this.Refresh();
            }
        }

        /// <summary>
        /// 获取或设置正在选中的背包选项卡。
        /// </summary>
        public ItemTab SelectedTab
        {
            get { return this.itemTabs[this.SelectedIndex]; }
            set { this.SelectedIndex = Array.IndexOf(this.itemTabs, value); }
        }

        public class ItemTab
        {
            public ItemTab(AfrmItem owner)
            {
                this.owner = owner;
                this.items = new ItemBase[ItemCount];
            }

            public const int ItemCount = 128;
            private AfrmItem owner;
            private ItemBase[] items;
            private bool selected;
            private BitmapOrigin tabEnabled;
            private BitmapOrigin tabDisabled;
            private int scrollValue;

            public ItemBase[] Items
            {
                get { return this.items; }
            }

            public bool Selected
            {
                get { return selected; }
                set
                {
                    if (selected != value)
                    {
                        if (!this.owner.selectedIndexChanging)
                            this.owner.SelectedIndex = Array.IndexOf(this.owner.itemTabs, this);
                        this.selected = value;
                    }
                }
            }

            public bool SetItemSource(ItemBase[] items)
            {
                if (items == null || items.Length != ItemCount)
                {
                    return false;
                }
                else
                {
                    this.items = items;
                    return true;
                }
            }

            public void ClearItems()
            {
                for (int i = 0; i < this.items.Length; i++)
                {
                    this.items[i] = null;
                }
            }

            public BitmapOrigin TabEnabled
            {
                get { return tabEnabled; }
                set { tabEnabled = value; }
            }

            public BitmapOrigin TabDisabled
            {
                get { return tabDisabled; }
                set { tabDisabled = value; }
            }

            public int ScrollMaxValue
            {
                get { return this.items.Length / 4; }
            }

            public int ScrollValue
            {
                get { return scrollValue; }
                set
                {
                    value = Math.Min(Math.Max(0, value), ScrollMaxValue);
                    scrollValue = value;
                }
            }
        }
    }
}
