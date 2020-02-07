using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using WzComparerR2.Common;
using WzComparerR2.CharaSim;
using WzComparerR2.Controls;

namespace WzComparerR2.CharaSimControl
{
    public class AfrmTooltip : AlphaForm
    {
        public AfrmTooltip()
        {
            this.menu = new ContextMenuStrip();
            this.menu.Items.Add(new ToolStripMenuItem("複製(&C)", null, tsmiCopy_Click));
            this.menu.Items.Add(new ToolStripMenuItem("儲存(&S)", null, tsmiSave_Click));
            this.ContextMenuStrip = this.menu;

            this.Size = new Size(1, 1);
            this.HideOnHover = true;
            this.GearRender = new GearTooltipRender2();
            this.ItemRender = new ItemTooltipRender2();
            this.SkillRender = new SkillTooltipRender2();
            this.RecipeRender = new RecipeTooltipRender();
            this.MobRender = new MobTooltipRenderer();
            this.NpcRender = new NpcTooltipRenderer();
            this.SetItemRender = new SetItemTooltipRender();
            this.HelpRender = new HelpTooltipRender();
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
        public MobTooltipRenderer MobRender { get; private set; }
        public NpcTooltipRenderer NpcRender { get; private set; }
        public HelpTooltipRender HelpRender { get; private set; }
        public SetItemTooltipRender SetItemRender { get; private set; }

        public string ImageFileName { get; set; }

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
            else if (item is Mob)
            {
                renderer = MobRender;
                MobRender.MobInfo = this.item as Mob;
            }
            else if (item is Npc)
            {
                renderer = NpcRender;
                NpcRender.NpcInfo = this.item as Npc;
            }
            else if (item is TooltipHelp)
            {
                renderer = HelpRender;
                HelpRender.Pair = this.item as TooltipHelp;
            }
            else if (item is SetItem)
            {
                renderer = SetItemRender;
                SetItemRender.SetItem = this.item as SetItem;
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
                var dataObj = new ImageDataObject(this.Bitmap, this.ImageFileName);
                Clipboard.SetDataObject(dataObj, false);
            }
        }

        void tsmiSave_Click(object sender, EventArgs e)
        {
            if (this.Bitmap != null && this.item != null)
            {
                using (SaveFileDialog dlg = new SaveFileDialog())
                {
                    dlg.Filter = "PNG (*.png)|*.png|*.*|*.*";
                    dlg.FileName = this.ImageFileName;

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
