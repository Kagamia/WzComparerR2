using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;
using System.Reflection;

namespace WzComparerR2.MonsterCard.UI
{
    public partial class MonsterCardForm : DevComponents.DotNetBar.OfficeForm
    {
        public MonsterCardForm()
        {
            InitializeComponent();
            this.gifControl1.Origin = new Point(this.gifControl1.Width / 2, this.gifControl1.Height / 2);
            this.mobGage = new MobGage();
        }

        public SuperTabControlPanel GetTabPanel()
        {
            this.TopLevel = false;
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.DoubleBuffered = true;
            var pnl = new SuperTabControlPanel();
            pnl.Controls.Add(this);
            pnl.Padding = new System.Windows.Forms.Padding(1);
            this.Visible = true;
            return pnl;
        }

        public Entry PluginEntry { get; set; }

        private Handler handler;
        private MobGage mobGage;
        private MobHandler mobHandler;
        private NpcHandler npcHandler;
        
        private bool showTooltip = true;

        /// <summary>
        /// wz1节点选中事件。
        /// </summary>
        public void OnSelectedNode1Changed(object sender, WzNodeEventArgs e)
        {
            if (e.Node == null)
            {
                return;
            }

            //限定mob.wz
            Wz_File file = e.Node.GetNodeWzFile();
            if (file == null)
            {
                return;
            }

            switch (file.Type)
            {
                case Wz_Type.Mob:
                    if (this.mobHandler == null)
                    {
                        this.mobHandler = new MobHandler(this);
                    }
                    
                    this.handler = mobHandler;
                    this.mobGage.Visible = true;
                    break;

                case Wz_Type.Npc:
                    if (this.npcHandler == null)
                    {
                        this.npcHandler = new NpcHandler(this);
                    }
                    
                    this.handler = npcHandler;
                    this.mobGage.Visible = false;
                    break;

                default: return;
            }
            
            Wz_Image mobImg = e.Node.GetValue<Wz_Image>();
            if (mobImg == null || !mobImg.TryExtract())
            {
                return;
            }

            bool loaded = false;

            if (showTooltip)
            {
                if (!loaded)
                {
                    handler.OnLoad(mobImg.Node);
                    loaded = true;
                }
                handler.ShowTooltipWindow(mobImg.Node);
            }

            if (PluginEntry.Context.SelectedTab == PluginEntry.Tab)
            {
                if (!loaded)
                {
                    handler.OnLoad(mobImg.Node);
                    loaded = true;
                }
                if (this.mobGage.Visible)
                {
                    this.mobGage.Load(mobImg.Node);
                }
                handler.OnLoadAnimates(mobImg.Node); //读取动画
                this.PrepareTabs(); //更新动画
                this.DisplayInfo(); //更新信息
            }
        }

        public void OnWzClosing(object sender, WzStructureEventArgs e)
        {
        }

        #region 显示相关
        private void PrepareTabs()
        {
            try
            {
                superTabStripAnimes.BeginUpdate();
                superTabStripAnimes.Tabs.Clear();
                if (this.handler == null)
                {
                    return;
                }
                foreach (var aniName in this.handler.GetAnimateNames())
                {
                    superTabStripAnimes.Tabs.Add(new SuperTabItem() { Text = aniName });
                }
            }
            finally
            {
                superTabStripAnimes.EndUpdate();
            }
        }

        private void DisplayInfo()
        {
            try
            {
                int scrolling = 0;
                if (advTreeMobInfo.VScrollBarVisible && advTreeMobInfo.VScrollBar != null)
                {
                    scrolling = advTreeMobInfo.VScrollBar.Value;
                }

                advTreeMobInfo.BeginUpdate();
                advTreeMobInfo.Nodes.Clear();

                if (this.handler != null)
                {
                    this.handler.DisplayInfo(this.advTreeMobInfo);
                }

                if (scrolling > 0)
                {
                    InvokeScroll(scrolling);
                }
            }
            finally
            {
                advTreeMobInfo.EndUpdate();
            }
        }

        private  void InvokeScroll(int value)
        {
            if (advTreeMobInfo.VScrollBarVisible && advTreeMobInfo.VScrollBar != null)
            {
                var bar = advTreeMobInfo.VScrollBar;
                value = Math.Max(Math.Min(bar.Maximum, value), bar.Minimum);
                var methods = typeof(ScrollBarAdv).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var mInfo in methods)
                {
                    var pList = mInfo.GetParameters();
                    if (pList.Length == 2
                        && pList[0].ParameterType == typeof(int)
                        && pList[1].ParameterType == typeof(ScrollEventType))
                    {
                        mInfo.Invoke(bar, new object[] { value, ScrollEventType.ThumbPosition });
                        break;
                    }
                }
            }
        }
        #endregion

        private void superTabStripAnimes_SelectedTabChanged(object sender, SuperTabStripSelectedTabChangedEventArgs e)
        {
            SuperTabItem tab = e.NewValue as SuperTabItem;
            if (tab == null || this.handler == null)
            {
                return;
            }

            this.gifControl1.AnimateGif = handler.GetAnimate(tab.Text);
        }

        private void gifControl1_Paint(object sender, PaintEventArgs e)
        {
            if (this.mobGage.Visible)
            {
                this.mobGage.DrawGage(e.Graphics, gifControl1.Size);
            }
            //e.Graphics.DrawString(DateTime.Now.ToString("HH:mm:ss.fff"), this.Font, Brushes.Blue, PointF.Empty);
        }
    }
}