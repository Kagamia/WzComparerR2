using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using DevComponents.AdvTree;
using WzComparerR2.PluginBase;
using WzComparerR2.Common;
using WzComparerR2.WzLib;
using WzComparerR2.CharaSimControl;
using System.Text.RegularExpressions;

namespace WzComparerR2.MonsterCard.UI
{
    public partial class MonsterCardForm : DevComponents.DotNetBar.OfficeForm
    {
        public MonsterCardForm()
        {
            InitializeComponent();
            this.gifControl1.Origin = new Point(this.gifControl1.Width / 2, this.gifControl1.Height / 2);
            this.tooltipRender = new MobTooltipRender();
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

        private MobInfo mobInfo;
        private bool showTooltip = true;
        private MobTooltipRender tooltipRender;

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
            if (file == null || file.Type != Wz_Type.Mob)
            {
                return;
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
                    LoadMob(mobImg.Node);
                    loaded = true;
                }
                ShowTooltipWindow(mobImg.Node);
            }

            if (PluginEntry.Context.SelectedTab == PluginEntry.Tab)
            {
                if (!loaded)
                {
                    LoadMob(mobImg.Node);
                    loaded = true;
                }
                LoadAnimates(this.mobInfo, mobImg.Node);
                ShowMobInfo();
                ShowAnimes();
            }
        }

        public void OnWzClosing(object sender, WzStructureEventArgs e)
        {
        }

        #region 加载数据相关
        private void LoadMob(Wz_Node imgNode)
        {
            MobInfo mobInfo = new MobInfo();
            Wz_Node infoNode = imgNode.FindNodeByPath("info");

            Match m = Regex.Match(imgNode.Text, @"(\d{7})\.img");
            if (m.Success)
            {
                int id;
                if (Int32.TryParse(m.Result("$1"), out id))
                {
                    mobInfo.ID = id;
                }
            }
            //加载基础属性
            if (infoNode != null)
            {
                foreach (var node in infoNode.Nodes)
                {
                    switch (node.Text)
                    {
                        case "level": mobInfo.Level = node.GetValueEx<int>(0); break;
                        case "defaultHP": mobInfo.DefaultHP = node.GetValueEx<string>(null); break;
                        case "defaultMP": mobInfo.DefaultMP = node.GetValueEx<string>(null); break;
                        case "finalmaxHP": mobInfo.FinalMaxHP = node.GetValueEx<string>(null); break;
                        case "finalmaxMP": mobInfo.FinalMaxMP = node.GetValueEx<string>(null); break;
                        case "maxHP": mobInfo.MaxHP = node.GetValueEx<int>(0); break;
                        case "maxMP": mobInfo.MaxMP = node.GetValueEx<int>(0); break;
                        case "hpRecovery": mobInfo.HPRecovery = node.GetValueEx<int>(0); break;
                        case "mpRecovery": mobInfo.MPRecovery = node.GetValueEx<int>(0); break;
                        case "speed": mobInfo.Speed = node.GetValueEx<int>(0); break;
                        case "flySpeed": mobInfo.FlySpeed = node.GetValueEx<int>(0); break;

                        case "PADamage": mobInfo.PADamage = node.GetValueEx<int>(0); break;
                        case "MADamage": mobInfo.MADamage = node.GetValueEx<int>(0); break;
                        case "PDRate": mobInfo.PDRate = node.GetValueEx<int>(0); break;
                        case "MDRate": mobInfo.MDRate = node.GetValueEx<int>(0); break;
                        case "acc": mobInfo.Acc = node.GetValueEx<int>(0); break;
                        case "eva": mobInfo.Eva = node.GetValueEx<int>(0); break;
                        case "pushed": mobInfo.Pushed = node.GetValueEx<int>(0); break;
                        case "exp": mobInfo.Exp = node.GetValueEx<int>(0); break;

                        case "boss": mobInfo.Boss = node.GetValueEx<int>(0) != 0; break;
                        case "undead": mobInfo.Undead = node.GetValueEx<int>(0) != 0; break;
                        case "firstAttack": mobInfo.FirstAttack = node.GetValueEx<int>(0) != 0; break;
                        case "bodyAttack": mobInfo.BodyAttack = node.GetValueEx<int>(0) != 0; break;
                        case "category": mobInfo.Category = node.GetValueEx<int>(0); break;
                        case "removeAfter": mobInfo.RemoveAfter = node.GetValueEx<int>(0); break;
                        case "damagedByMob": mobInfo.DamagedByMob = node.GetValueEx<int>(0) != 0; break;
                        case "invincible": mobInfo.Invincible = node.GetValueEx<int>(0) != 0; break;
                        case "notAttack": mobInfo.NotAttack = node.GetValueEx<int>(0) != 0; break;
                        case "elemAttr": mobInfo.ElemAttr = new MobElemAttr(node.GetValueEx<string>(null)); break;

                        case "link": mobInfo.Link = node.GetValueEx<int>(0); break;

                        case "skill": LoadSkill(mobInfo, node); break;
                        case "attack": LoadAttack(mobInfo, node); break;
                        case "buff": LoadBuff(mobInfo, node); break;
                        case "revive": LoadRevive(mobInfo, node); break;

                        
                    }
                }
            }

            this.mobInfo = mobInfo;
        }

        private void LoadSkill(MobInfo mobInfo, Wz_Node propNode)
        {

        }

        private void LoadAttack(MobInfo mobInfo, Wz_Node propNode)
        {

        }

        private void LoadBuff(MobInfo mobInfo, Wz_Node propNode)
        {

        }

        private void LoadRevive(MobInfo mobInfo, Wz_Node propNode)
        {
            for (int i = 0; ; i++)
            {
                var reviveNode = propNode.FindNodeByPath(i.ToString());
                if (reviveNode == null)
                {
                    break;
                }
                mobInfo.Revive.Add(reviveNode.GetValue<int>());
            }
        }

        private Wz_Node GetLinkNode(int linkMobID)
        {
            return PluginManager.FindWz(string.Format("Mob\\{0:d7}.img", linkMobID));
        }

        private Bitmap GetTooltipImage(MobInfo mobInfo, Wz_Node imgNode)
        {
            Wz_Node linkNode = mobInfo.Link == null ? imgNode : GetLinkNode(mobInfo.Link.Value);

            if (linkNode == null)
            {
                return null;
            }

            GifFrame mobImgFrame = null;

            foreach (var action in new[] { "stand", "move", "fly" })
            {
                mobImgFrame = Gif.CreateFrameFromNode(linkNode.FindNodeByPath(action + "\\0"), PluginManager.FindWz);
                if (mobImgFrame != null)
                {
                    break;
                }
            }

            if (mobImgFrame == null || mobImgFrame.Bitmap == null)
            {
                return null;
            }

            return mobImgFrame.Bitmap;
        }

        private void LoadAnimates(MobInfo mobInfo, Wz_Node imgNode)
        {
            Wz_Node linkNode = mobInfo.Link == null ? imgNode : GetLinkNode(mobInfo.Link.Value);

            if (linkNode == null)
            {
                return;
            }

            var aniList = new[] { "stand", "regen", "move", "fly" };
            foreach (string aniName in aniList)
            {
                var ani = LoadAnimateByName(linkNode, aniName);
                if (ani != null)
                {
                    mobInfo.Animates.Add(ani);
                }
            }

            foreach (var ani in LoadAllAnimate(linkNode, "attack"))
            {
                mobInfo.Animates.Add(ani);
            }

            foreach (var ani in LoadAllAnimate(linkNode, "skill"))
            {
                mobInfo.Animates.Add(ani);
            }

            foreach (var ani in LoadAllAnimate(linkNode, "hit"))
            {
                mobInfo.Animates.Add(ani);
            }

            foreach (var ani in LoadAllAnimate(linkNode, "die"))
            {
                mobInfo.Animates.Add(ani);
            }
        }

        private IEnumerable<MobAnimate> LoadAllAnimate(Wz_Node imgNode, string aniPrefix)
        {
            for (int i = 0; ; i++)
            {
                string aniName = aniPrefix + (i > 0 ? i.ToString() : "");
                var ani = LoadAnimateByName(imgNode, aniName);
                if (ani != null)
                {
                    yield return ani;
                }
                else if (i >= 1)
                {
                    yield break;
                }
            }
        }

        private MobAnimate LoadAnimateByName(Wz_Node imgNode, string aniName)
        {
            var aniNode = imgNode.FindNodeByPath(aniName);
            if (aniNode == null)
            {
                return null;
            }
            var ani = new MobAnimate(aniName);
            ani.AnimateGif = Gif.CreateFromNode(aniNode, PluginManager.FindWz);
            return ani;
        }
        #endregion

        #region 显示相关
        private void ShowTooltipWindow(Wz_Node imgNode)
        {
            Bitmap mobImage = GetTooltipImage(this.mobInfo, imgNode);
            this.tooltipRender.StringLinker = this.PluginEntry.Context.DefaultStringLinker;
            Bitmap bmp = this.tooltipRender.Render(this.mobInfo, mobImage);
            if (bmp != null)
            {
                var tooltipWnd = this.PluginEntry.Context.DefaultTooltipWindow as AfrmTooltip;
                if (tooltipWnd != null)
                {
                    tooltipWnd.Bitmap = bmp;
                    tooltipWnd.TargetItem = null;
                    tooltipWnd.CaptionRectangle = new Rectangle(Point.Empty, bmp.Size);
                    tooltipWnd.Refresh();

                    tooltipWnd.TargetItem = this.mobInfo;
                    tooltipWnd.HideOnHover = false;
                    tooltipWnd.Show();
                }
            }
        }

        private void ShowMobInfo()
        {
            try
            {
                advTreeMobInfo.BeginUpdate();
               
                advTreeMobInfo.Nodes.Clear();
                
                if (this.mobInfo == null)
                {
                    return;
                }
                //基本信息
                advTreeMobInfo.Nodes.Add(CreateNode("level", this.mobInfo.Level.ToString()));
                if (!string.IsNullOrEmpty(this.mobInfo.DefaultHP))
                {
                    advTreeMobInfo.Nodes.Add(CreateNode("defaultHP", this.mobInfo.DefaultHP));
                }
                if (!string.IsNullOrEmpty(this.mobInfo.DefaultMP))
                {
                    advTreeMobInfo.Nodes.Add(CreateNode("defaultMP", this.mobInfo.DefaultMP));
                }
                if (!string.IsNullOrEmpty(this.mobInfo.FinalMaxHP))
                {
                    advTreeMobInfo.Nodes.Add(CreateNode("finalMaxHP", this.mobInfo.FinalMaxHP));
                }
                if (!string.IsNullOrEmpty(this.mobInfo.FinalMaxMP))
                {
                    advTreeMobInfo.Nodes.Add(CreateNode("finalMaxMP", this.mobInfo.FinalMaxMP));
                }
                advTreeMobInfo.Nodes.Add(CreateNode("maxHP", this.mobInfo.MaxHP.ToString()));
                advTreeMobInfo.Nodes.Add(CreateNode("maxMP", this.mobInfo.MaxMP.ToString()));
                advTreeMobInfo.Nodes.Add(CreateNode("HPRecovery", this.mobInfo.HPRecovery.ToString()));
                advTreeMobInfo.Nodes.Add(CreateNode("MPRecovery", this.mobInfo.MPRecovery.ToString()));
                if (this.mobInfo.Speed == null && this.mobInfo.FlySpeed == null
                    && !this.mobInfo.Animates.Contains("move") && !this.mobInfo.Animates.Contains("fly"))
                {
                    advTreeMobInfo.Nodes.Add(CreateNode("speed", "无法移动"));
                }
                else
                {
                    if (this.mobInfo.Speed != null || this.mobInfo.Animates.Contains("move"))
                    {
                        advTreeMobInfo.Nodes.Add(CreateNode("speed", (this.mobInfo.Speed ?? 0).ToString()));
                    }
                    if (this.mobInfo.FlySpeed != null || this.mobInfo.Animates.Contains("fly"))
                    {
                        advTreeMobInfo.Nodes.Add(CreateNode("flySpeed", (this.mobInfo.FlySpeed ?? 0).ToString()));
                    }
                }

                advTreeMobInfo.Nodes.Add(CreateNode("PADamage", this.mobInfo.PADamage.ToString()));
                advTreeMobInfo.Nodes.Add(CreateNode("MADamage", this.mobInfo.MADamage.ToString()));
                advTreeMobInfo.Nodes.Add(CreateNode("PDRate", this.mobInfo.PDRate + "%"));
                advTreeMobInfo.Nodes.Add(CreateNode("MDRate", this.mobInfo.MDRate + "%"));
                advTreeMobInfo.Nodes.Add(CreateNode("Acc", this.mobInfo.Acc.ToString()));
                advTreeMobInfo.Nodes.Add(CreateNode("Eva", this.mobInfo.Eva.ToString()));
                advTreeMobInfo.Nodes.Add(CreateNode("KnockBack", this.mobInfo.Pushed.ToString()));
                advTreeMobInfo.Nodes.Add(CreateNode("Exp", this.mobInfo.Exp.ToString()));

                Node treeNode;

                treeNode = CreateNode("特殊属性");
                treeNode.Nodes.Add(CreateNode("怪物类别(Category)", 
                    string.Format("{0}({1})", CharaSim.ItemStringHelper.GetMobCategoryName(this.mobInfo.Category), this.mobInfo.Category)));
                treeNode.Nodes.Add(CreateNodeWithValue("Boss", this.mobInfo.Boss));
                treeNode.Nodes.Add(CreateNodeWithValue("不死系(Undead)", this.mobInfo.Undead));
                treeNode.Nodes.Add(CreateNodeWithValue("主动攻击(FirstAttack)", this.mobInfo.FirstAttack));
                treeNode.Nodes.Add(CreateNodeWithValue("碰撞伤害(BodyAttack)", this.mobInfo.BodyAttack, true));
                treeNode.Nodes.Add(CreateNodeWithValue("只受怪物伤害(damagedByMob)", this.mobInfo.DamagedByMob));
                treeNode.Nodes.Add(CreateNodeWithValue("无敌(Invincible)", this.mobInfo.Invincible));
                treeNode.Nodes.Add(CreateNodeWithValue("无法攻击(NotAttack)", this.mobInfo.NotAttack));
                treeNode.Expand();
                advTreeMobInfo.Nodes.Add(treeNode);


                treeNode = CreateNode("属性抗性(ElemAttr)", this.mobInfo.ElemAttr.StringValue);
                treeNode.Nodes.Add(CreateNodeWithValue("冰(I)", this.mobInfo.ElemAttr.I));
                treeNode.Nodes.Add(CreateNodeWithValue("雷(L)", this.mobInfo.ElemAttr.L));
                treeNode.Nodes.Add(CreateNodeWithValue("火(F)", this.mobInfo.ElemAttr.F));
                treeNode.Nodes.Add(CreateNodeWithValue("毒(S)", this.mobInfo.ElemAttr.S));
                treeNode.Nodes.Add(CreateNodeWithValue("圣(H)", this.mobInfo.ElemAttr.H));
                treeNode.Nodes.Add(CreateNodeWithValue("暗(D)", this.mobInfo.ElemAttr.D));
                treeNode.Nodes.Add(CreateNodeWithValue("物理(P)", this.mobInfo.ElemAttr.P));
                treeNode.Expand();
                advTreeMobInfo.Nodes.Add(treeNode);

                if (this.mobInfo.RemoveAfter > 0)
                {
                    advTreeMobInfo.Nodes.Add(CreateNode("RemoveAfter", this.mobInfo.RemoveAfter.ToString()+"s"));
                }
                if (this.mobInfo.Revive.Count > 0)
                {
                    treeNode = CreateNode("死后召唤", "[" + this.mobInfo.Revive.Count + "]");
                    for (int i = 0; i < this.mobInfo.Revive.Count; i++)
                    {
                        int reviveID =  this.mobInfo.Revive[i];
                        StringResult sr;
                        PluginEntry.Context.DefaultStringLinker.StringMob.TryGetValue(reviveID, out sr);
                        string mobName = sr == null ? reviveID.ToString() : string.Format("{0}({1})", sr.Name, reviveID);
                        treeNode.Nodes.Add(CreateNode("[" + i + "]", mobName));
                    }
                    treeNode.Expand();
                    advTreeMobInfo.Nodes.Add(treeNode);
                }
            }
            finally
            {
                advTreeMobInfo.EndUpdate();
            }
        }

        private Node CreateNode(params string[] cells)
        {
            Node node = new Node(cells.Length > 0 ? cells[0] : null);
            for (int i = 1; i < cells.Length; i++)
            {
                node.Cells.Add(new Cell(cells[i]));
            }
            return node;
        }


        private Node CreateNodeWithValue(string propName, bool value)
        {
            return CreateNodeWithValue(propName, value, false);
        }

        private Node CreateNodeWithValue(string propName, bool value, bool defaultValue)
        {
            var node = CreateNode(propName, value ? "是(1)" : "否(0)");
            if (value == defaultValue)
            {
                node.Style = new ElementStyle(Color.Gray);
            }
            return node;
        }

        private Node CreateNodeWithValue(string propName, ElemResistance resist)
        {
            string resistStr;
            switch (resist)
            {
                case ElemResistance.Normal: resistStr = "普通"; break;
                case ElemResistance.Immune: resistStr = "免疫(1)"; break;
                case ElemResistance.Resist: resistStr = "耐性(2)"; break;
                case ElemResistance.Weak: resistStr = "弱属性(3)"; break;
                default: resistStr = "(" + (int)resist + ")"; break;
            }
            var node = CreateNode(propName, resistStr);
            if (resist == ElemResistance.Normal)
            {
                node.Style = new ElementStyle(Color.Gray);
            }
            return node;
        }

        private void ShowAnimes()
        {
            try
            {
                superTabStripAnimes.BeginUpdate();
                superTabStripAnimes.Tabs.Clear();
                if (this.mobInfo == null)
                {
                    return;
                }
                foreach (var ani in this.mobInfo.Animates)
                {
                    superTabStripAnimes.Tabs.Add(new SuperTabItem() { Text = ani.Name });
                }
            }
            finally
            {
                superTabStripAnimes.EndUpdate();
            }
        }

        #endregion

        private void superTabStripAnimes_SelectedTabChanged(object sender, SuperTabStripSelectedTabChangedEventArgs e)
        {
            SuperTabItem tab = e.NewValue as SuperTabItem;
            if (tab == null || this.mobInfo == null)
            {
                return;
            }

            if (!this.mobInfo.Animates.Contains(tab.Text))
            {
                return;
            }

            var ani = this.mobInfo.Animates[tab.Text];
            if (ani != null)
            {
                this.gifControl1.AnimateGif = ani.AnimateGif;
            }
            else
            {
                this.gifControl1.AnimateGif = null;
            }
        }


    }
}