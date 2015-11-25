using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using System.Reflection;

namespace WzComparerR2.MonsterCard.UI
{
    public partial class MonsterCardForm : DevComponents.DotNetBar.OfficeForm
    {
        public MonsterCardForm()
        {
            InitializeComponent();
            this.mobGage = new MobGage();

            this.aniArgs = new AnimationDrawArgs();
            this.aniArgs.OriginX = gifControl1.Width / 2;
            this.aniArgs.OriginY = (int)(gifControl1.Height * 0.6);
            this.aniArgs.RegisterEvents(this.gifControl1);
            this.gifControl1.AniDrawArgs = this.aniArgs;
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

        internal AnimationDrawArgs aniArgs;

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
        public void DisplayGif(Gif gif)
        {
            HideSpineControl();
            this.gifControl1.Visible = true;
            this.lblMode.Text = "mode-gif";
            this.gifControl1.AnimateGif = gif;
        }

        public void TryDisplaySpine(Wz_Node imgNode, string aniName, bool? useJson)
        {
            if (CanShowSpine())
            {
                this.gifControl1.Visible = false;
                LoadSpineAndShow(imgNode, aniName, useJson);
            }
        }

        public bool CanShowSpine()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Any(asm =>
                string.Equals("Microsoft.Xna.Framework.dll",
                    asm.ManifestModule?.Name,
                    StringComparison.CurrentCultureIgnoreCase));
        }

        public void LoadSpineAndShow(Wz_Node imgNode, string aniName, bool? useJson)
        {
            SpineControl spineControl = this.panelEx1.Controls.OfType<SpineControl>().FirstOrDefault();
            if (spineControl == null)
            {
                spineControl = new SpineControl();
                spineControl.Dock = System.Windows.Forms.DockStyle.Fill;
                this.panelEx1.Controls.Add(spineControl);
                this.aniArgs.RegisterEvents(spineControl);
                spineControl.AniDrawArgs = this.aniArgs;
                spineControl.ShowBoundingBox = this.chkShowBoundingBox.Checked;
                spineControl.ShowDrawingArea = this.chkShowDrawingArea.Checked;
                spineControl.EnableEffect = this.chkShowEffect.Checked;
            }

            spineControl.Visible = true;
            this.itemContainerSpine.Visible = true;
            this.lblMode.Text = "mode-xna";
            spineControl.LoadSkeleton(imgNode, aniName, useJson);
        }

        public void HideSpineControl()
        {
            SpineControl spineControl = this.panelEx1.Controls.OfType<SpineControl>().FirstOrDefault();
            if (spineControl != null)
            {
                spineControl.Visible = false;
            }
            this.itemContainerSpine.Visible = false;
        }

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

            if (superTabStripAnimes.SelectedTab != null)
            {
                this.handler.OnShowAnimate(superTabStripAnimes.SelectedTab.Text);
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

        private void InvokeScroll(int value)
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
            if (tab == null || this.handler == null || superTabStripAnimes.IsUpdateSuspended)
            {
                return;
            }

            this.handler.OnShowAnimate(tab.Text);
        }

        private void gifControl1_Paint(object sender, PaintEventArgs e)
        {
            if (this.mobGage.Visible)
            {
                this.mobGage.DrawGage(e.Graphics, gifControl1.Size);
            }
        }

        public void OnResize()
        {
            this.PerformLayout(this.superTabStripAnimes, null);
        }

        private void btnSpineSave_Click(object sender, EventArgs e)
        {
            SpineControl spineControl = this.panelEx1.Controls.OfType<SpineControl>().FirstOrDefault();
            if (spineControl != null)
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.OverwritePrompt = true;
                dlg.Filter = "*.gif|*.gif|*.*|*.*";
                dlg.FileName = spineControl.CurrentAniName + ".gif";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    int delay;
                    if (!Int32.TryParse(txtDelay.Text, out delay)
                        || (delay = (delay / 10 * 10)) <= 0)
                    {
                        delay = 100;
                    }
                    var gif = spineControl.SaveAsGif(delay);
                    if (gif != null)
                    {
                        gif.Save(dlg.FileName);
                        gif.Dispose();
                        gif = null;
                        GC.Collect();
                    }
                    else
                    {
                        MessageBox.Show("没有动画可以保存。");
                    }
                }
            }
        }

        private void chkShowEffect_Click(object sender, EventArgs e)
        {
            SpineControl spineControl = this.panelEx1.Controls.OfType<SpineControl>().FirstOrDefault();
            if (spineControl != null)
            {
                spineControl.EnableEffect = chkShowEffect.Checked;
            }
        }

        private void chkShowBoundingBox_Click(object sender, EventArgs e)
        {
            SpineControl spineControl = this.panelEx1.Controls.OfType<SpineControl>().FirstOrDefault();
            if (spineControl != null)
            {
                spineControl.ShowBoundingBox = chkShowBoundingBox.Checked;
            }
        }

        private void chkShowDrawingArea_Click(object sender, EventArgs e)
        {
            SpineControl spineControl = this.panelEx1.Controls.OfType<SpineControl>().FirstOrDefault();
            if (spineControl != null)
            {
                spineControl.ShowDrawingArea = chkShowDrawingArea.Checked;
            }
        }

        private void txtDelay_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                int delay;
                if (Int32.TryParse(txtDelay.Text, out delay))
                {
                    btnSpineSave.ClosePopup();
                }
            }
        }
    }
}