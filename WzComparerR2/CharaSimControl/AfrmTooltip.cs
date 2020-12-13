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
                using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
                {
                    var dataObj = new DataObject();
                    dataObj.SetData(DataFormats.Bitmap, this.Bitmap);
                    Byte[] dibData = ConvertToDib(this.Bitmap);
                    stream.Write(dibData, 0, dibData.Length);
                    dataObj.SetData(DataFormats.Dib, stream);
                    Clipboard.SetDataObject(dataObj, true);
                }
            }
        }

        private Byte[] ConvertToDib(Image image) // https://stackoverflow.com/a/46424800
        {
            Byte[] bm32bData;
            Int32 width = image.Width;
            Int32 height = image.Height;
            // Ensure image is 32bppARGB by painting it on a new 32bppARGB image.
            using (Bitmap bm32b = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                using (Graphics gr = Graphics.FromImage(bm32b))
                    gr.DrawImage(image, new Rectangle(0, 0, bm32b.Width, bm32b.Height));
                // Bitmap format has its lines reversed.
                bm32b.RotateFlip(RotateFlipType.Rotate180FlipX);
                Int32 stride;
                bm32bData = GetImageData(bm32b, out stride);
            }
            // BITMAPINFOHEADER struct for DIB.
            Int32 hdrSize = 0x28;
            Byte[] fullImage = new Byte[hdrSize + 12 + bm32bData.Length];
            //Int32 biSize;
            WriteIntToByteArray(fullImage, 0x00, 4, true, (UInt32)hdrSize);
            //Int32 biWidth;
            WriteIntToByteArray(fullImage, 0x04, 4, true, (UInt32)width);
            //Int32 biHeight;
            WriteIntToByteArray(fullImage, 0x08, 4, true, (UInt32)height);
            //Int16 biPlanes;
            WriteIntToByteArray(fullImage, 0x0C, 2, true, 1);
            //Int16 biBitCount;
            WriteIntToByteArray(fullImage, 0x0E, 2, true, 32);
            //BITMAPCOMPRESSION biCompression = BITMAPCOMPRESSION.BITFIELDS;
            WriteIntToByteArray(fullImage, 0x10, 4, true, 3);
            //Int32 biSizeImage;
            WriteIntToByteArray(fullImage, 0x14, 4, true, (UInt32)bm32bData.Length);
            // These are all 0. Since .net clears new arrays, don't bother writing them.
            //Int32 biXPelsPerMeter = 0;
            //Int32 biYPelsPerMeter = 0;
            //Int32 biClrUsed = 0;
            //Int32 biClrImportant = 0;

            // The aforementioned "BITFIELDS": colour masks applied to the Int32 pixel value to get the R, G and B values.
            WriteIntToByteArray(fullImage, hdrSize + 0, 4, true, 0x00FF0000);
            WriteIntToByteArray(fullImage, hdrSize + 4, 4, true, 0x0000FF00);
            WriteIntToByteArray(fullImage, hdrSize + 8, 4, true, 0x000000FF);
            Array.Copy(bm32bData, 0, fullImage, hdrSize + 12, bm32bData.Length);
            return fullImage;
        }

        private Byte[] GetImageData(Bitmap sourceImage, out Int32 stride) // https://stackoverflow.com/a/43706643
        {
            System.Drawing.Imaging.BitmapData sourceData = sourceImage.LockBits(new Rectangle(0, 0, sourceImage.Width, sourceImage.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, sourceImage.PixelFormat);
            stride = sourceData.Stride;
            Byte[] data = new Byte[stride * sourceImage.Height];
            System.Runtime.InteropServices.Marshal.Copy(sourceData.Scan0, data, 0, data.Length);
            sourceImage.UnlockBits(sourceData);
            return data;
        }

        private void WriteIntToByteArray(Byte[] data, Int32 startIndex, Int32 bytes, Boolean littleEndian, UInt32 value) // https://stackoverflow.com/a/46424800
        {
            Int32 lastByte = bytes - 1;
            if (data.Length < startIndex + bytes)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to write a " + bytes + "-byte value at offset " + startIndex + ".");
            for (Int32 index = 0; index < bytes; index++)
            {
                Int32 offs = startIndex + (littleEndian ? index : lastByte - index);
                data[offs] = (Byte)(value >> (8 * index) & 0xFF);
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
