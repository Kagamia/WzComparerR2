using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using WzComparerR2.Common;

namespace WzComparerR2.CharaSimControl
{
    public class AfrmTooltip : AlphaForm
    {
        public AfrmTooltip()
        {
            this.menu = new ContextMenuStrip();
            this.menu.Items.Add(new ToolStripMenuItem("复制(&C)", null, tsmiCopy_Click));
            this.menu.Items.Add(new ToolStripMenuItem("保存(&S)", null, tsmiSave_Click));
            this.ContextMenuStrip = this.menu;

            this.Size = new Size(1, 1);
            this.HideOnHover = true;
            this.GearRender = new GearTooltipRender2();
            this.ItemRender = new ItemTooltipRender2();
            this.SkillRender = new SkillTooltipRender2();
            this.RecipeRender = new RecipeTooltipRender();
            this.SizeChanged += AfrmTooltip_SizeChanged;

            this.MouseClick += AfrmTooltip_MouseClick;
        }

        private object item;

        private ContextMenuStrip menu;
        private bool showMenu;
        private bool showID;

        public Object TargetItem
        {
            get { return item; }
            set { item = value; }
        }

        public StringLinker StringLinker { get; set; }
        public Character Character { get; set; }

        public GearTooltipRender2 GearRender { get; private set; }
        public ItemTooltipRender2 ItemRender { get; private set; }
        public SkillTooltipRender2 SkillRender { get; private set; }
        public RecipeTooltipRender RecipeRender { get; private set; }

        public bool ShowID
        {
            get { return this.showID; }
            set
            {
                this.showID = value;
                this.GearRender.ShowObjectID = value;
                this.ItemRender.ShowObjectID = value;
                this.SkillRender.ShowObjectID = value;
                this.RecipeRender.ShowObjectID = value;
            }
        }

        public bool ShowMenu
        {
            get { return showMenu; }
            set { showMenu = value; }
        }

        public override void Refresh()
        {
            this.PreRender();
            if (this.Bitmap != null)
            {
                this.SetBitmap(Bitmap);
                this.CaptionRectangle = new Rectangle(0, 0, Bitmap.Width, Bitmap.Height);
                base.Refresh();
            }
        }

        public void PreRender()
        {
            if (this.item == null)
                return;

            TooltipRender renderer;
            if (item is Item)
            {
                renderer = ItemRender;
                ItemRender.Item = this.item as Item;
            }
            else if (item is Gear)
            {
                renderer = GearRender;
                GearRender.Gear = this.TargetItem as Gear;

                if (false)
                {
                    Gear g = GearRender.Gear;
                    if (this.StringLinker.StringEqp.ContainsKey(g.ItemID))
                    {
                        this.StringLinker.StringEqp[g.ItemID].Name = "米猪猪的胖次";
                        this.StringLinker.StringEqp[g.ItemID].Desc = @"传说中米猪猪穿过的胖次。\n\n#c“来♂一♂发”#——颠倒";
                    }
                    g.Star = 25;
                    g.Grade = GearGrade.SS;
                    g.AdditionGrade = GearGrade.B;
                    g.Props[GearPropType.reqLevel] = 180;
                    g.Props[GearPropType.reqSTR] = 0;
                    g.Props[GearPropType.reqDEX] = 0;
                    g.Props[GearPropType.reqINT] = 0;
                    g.Props[GearPropType.reqLUK] = 0;
                    g.Props[GearPropType.reqPOP] = 880;
                    g.Props[GearPropType.level] = 1;
                    g.Props[GearPropType.reqJob] = 0;
                    g.Props[GearPropType.incPAD] = 880;
                    g.Props[GearPropType.incMAD] = 880;
                    g.Props[GearPropType.tuc] = 8;
                    g.Props[GearPropType.superiorEqp] = 1;
                    g.Props[GearPropType.tradeAvailable] = 2;
                    g.Props[GearPropType.charismaEXP] = 88;
                    g.Props[GearPropType.willEXP] = 88;
                    g.Props[GearPropType.charmEXP] = 88;
                    g.Props[GearPropType.nActivatedSocket] = 1;
                    g.Options[0] = Potential.LoadFromWz(60001, 3);
                    g.Options[1] = Potential.LoadFromWz(60001, 3);
                    g.Options[2] = Potential.LoadFromWz(60001, 3);
                    g.AdditionalOptions[0] = Potential.LoadFromWz(32086, 10);
                    g.AdditionalOptions[1] = Potential.LoadFromWz(32086, 10);
                    g.AdditionalOptions[2] = Potential.LoadFromWz(32086, 10);
                }
            }
            else if (item is Skill)
            {
                renderer = SkillRender;
                SkillRender.Skill = this.item as Skill;
            }
            else if (item is Recipe)
            {
                renderer = RecipeRender;
                RecipeRender.Recipe = this.item as Recipe;
            }
            else
            {
                this.Bitmap = null;
                renderer = null;
                return;
            }
            renderer.StringLinker = StringLinker;
            this.Bitmap = renderer.Render();
        }

        void AfrmTooltip_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && this.showMenu)
            {
                this.menu.Show(this, e.Location);
            }
        }

        void tsmiCopy_Click(object sender, EventArgs e)
        {
            if (this.Bitmap != null)
            {
                Clipboard.SetImage(this.Bitmap);
            }
        }

        void tsmiSave_Click(object sender, EventArgs e)
        {
            if (this.Bitmap != null && this.item != null)
            {
                using (SaveFileDialog dlg = new SaveFileDialog())
                {
                    dlg.Filter = "*.png|*.png|*.*|*.*";
                    if (this.item is ItemBase)
                    {
                        dlg.FileName = ((ItemBase)this.item).ItemID.ToString("D7") + ".png";
                    }
                    else if (this.item is Skill)
                    {
                        dlg.FileName = ((Skill)this.item).SkillID.ToString("D7") + ".png";
                    }

                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        this.Bitmap.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }
            }
        }

        void AfrmTooltip_SizeChanged(object sender, EventArgs e)
        {
            if (this.Bitmap != null)
                this.SetClientSizeCore(this.Bitmap.Width, this.Bitmap.Height);
        }
    }
}
