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
        private ACtrlButton btnCoin;
        private ACtrlButton btnGather;
        private ACtrlButton btnSort;
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
                   -9 - 31 * i, -26);
                this.itemTabs[i].TabDisabled = new BitmapOrigin((Bitmap)Resource.ResourceManager.GetObject("Item_Tab_disabled_" + i),
                    -9 - 31 * i, -26);
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
            this.btnSmall.Location = new Point(147, 267);
            this.btnSmall.Size = new Size(16, 16);
            this.btnSmall.MouseClick += new MouseEventHandler(btnSmall_MouseClick);
            this.btnSmall.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnCoin = new ACtrlButton();
            this.btnCoin.Normal = new BitmapOrigin(Resource.Item_BtCoin_normal_0);
            this.btnCoin.Pressed = new BitmapOrigin(Resource.Item_BtCoin_pressed_0);
            this.btnCoin.MouseOver = new BitmapOrigin(Resource.Item_BtCoin_mouseOver_0);
            this.btnCoin.Disabled = new BitmapOrigin(Resource.Item_BtCoin_disabled_0);
            this.btnCoin.Location = new Point(9, 267);
            this.btnCoin.Size = new Size(40, 16);
            this.btnCoin.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);

            this.btnGather = new ACtrlButton();
            this.btnGather.Normal = new BitmapOrigin(Resource.Item_BtGather_normal_0);
            this.btnGather.Pressed = new BitmapOrigin(Resource.Item_BtGather_pressed_0);
            this.btnGather.MouseOver = new BitmapOrigin(Resource.Item_BtGather_mouseOver_0);
            this.btnGather.Disabled = new BitmapOrigin(Resource.Item_BtGather_disabled_0);
            this.btnGather.Location = new Point(129, 267);
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

            g.Dispose();
        }

        private void renderFull()
        {
            this.Bitmap = new Bitmap(Resource.Item_FullBackgrnd);
            Graphics g = Graphics.FromImage(this.Bitmap);
            g.DrawImage(Resource.Item_FullBackgrnd2, 6, 23);
            renderTabs(g);
            g.DrawImage(Resource.Item_FullBackgrnd3, 10, 51);
            foreach (AControl ctrl in this.aControls)
            {
                ctrl.Draw(g);
            }

            ItemBase[] itemArray = this.SelectedTab.Items;
            for (int i = 0; i < itemArray.Length; i++)
            {
                int idx = i % 24, group = i / 24;
                Point origin = getItemIconOrigin(i);
                origin.Offset(0, 32);
                renderItemBase(g, itemArray[i], origin);
            }

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
                /*
                int value;
                if (gear.Props.TryGetValue(GearPropType.royalSpecial, out value) && value > 0)
                    g.DrawImage(Resource.CashItem_label_0, origin.X + 20, origin.Y - 12);
                else if (gear.Props.TryGetValue(GearPropType.masterSpecial, out value) && value > 0)
                    g.DrawImage(Resource.CashItem_label_3, origin.X + 20, origin.Y - 12);
                else
                */
                g.DrawImage(Resource.CashItem_0, origin.X + 20, origin.Y - 12);
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
                if (item.Props.TryGetValue(ItemPropType.wonderGrade, out long value) && value > 0)
                {
                    Image label = Resource.ResourceManager.GetObject("CashItem_label_" + (value + 3)) as Image;
                    if (label != null)
                    {
                        g.DrawImage(new Bitmap(label), origin.X + 20, origin.Y - 12);
                    }
                }
                else
                {
                    g.DrawImage(Resource.CashItem_0, origin.X + 20, origin.Y - 12);
                }
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
            if (y >= 6 || (fullMode ? x >= 24 : x >= 4))
                return -1;
            int idx = y * 4 + x % 4 + x / 4 * 24;
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
                yield return this.btnCoin;
                yield return this.btnGather;
                yield return this.btnSort;
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

            public const int ItemCount = 96;
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
