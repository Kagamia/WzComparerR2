using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using Resource = CharaSimResource.Resource;
using WzComparerR2.Common;
using WzComparerR2.CharaSim;

namespace WzComparerR2.CharaSimControl
{
    public class RecipeTooltipRender : TooltipRender
    {
        public RecipeTooltipRender()
        {
        }

        public Recipe Recipe { get; set; }

        public override object TargetItem
        {
            get
            {
                return this.Recipe;
            }
            set
            {
                this.Recipe = value as Recipe;
            }
        }

        public override Bitmap Render()
        {
            if (this.Recipe == null)
            {
                return null;
            }
            int picHeight;
            Bitmap originBmp = RenderRecipe(out picHeight);
            Bitmap tooltip = new Bitmap(originBmp.Width, picHeight);
            Graphics g = Graphics.FromImage(tooltip);

            //绘制背景区域
            GearGraphics.DrawNewTooltipBack(g, 0, 0, tooltip.Width, tooltip.Height);

            //复制图像
            g.DrawImage(originBmp, 0, 0, new Rectangle(0, 0, originBmp.Width, picHeight), GraphicsUnit.Pixel);

            //左上角
            g.DrawImage(Resource.UIToolTip_img_Item_Frame2_cover, 3, 3);

            if (this.ShowObjectID)
            {
                GearGraphics.DrawGearDetailNumber(g, 3, 3, Recipe.RecipeID.ToString("d8"), true);
            }

            if (originBmp != null)
                originBmp.Dispose();

            g.Dispose();
            return tooltip;
        }

        private Bitmap RenderRecipe(out int picH)
        {
            Bitmap tooltip = new Bitmap(290, DefaultPicHeight);
            Graphics g = Graphics.FromImage(tooltip);
            StringFormat fmt = (StringFormat)StringFormat.GenericTypographic.Clone();
            fmt.Alignment = StringAlignment.Center;

            picH = 10;
            StringResult sr;
            string title = "製作配方";
            if (this.Recipe.MainTargetItemID != 0)
            {
                sr = GetSRByItemID(this.Recipe.MainTargetItemID);
                if (sr == null)
                {
                    title += " - " + this.Recipe.MainTargetItemID;
                }
                else
                {
                    title += " - " + sr.Name;
                }
            }
            g.DrawString(title, GearGraphics.ItemDetailFont, GearGraphics.GreenBrush2, 145, picH, fmt);
            picH += 16;

            g.DrawString("生產物品", GearGraphics.ItemDetailFont, GearGraphics.GreenBrush2, 13, picH);
            picH += 16;

            fmt.Alignment = StringAlignment.Far;
            foreach (RecipeItemInfo itemInfo in this.Recipe.TargetItems)
            {
                sr = GetSRByItemID(itemInfo.ItemID);
                string text = sr != null ? sr.Name : itemInfo.ItemID.ToString();
                text += " x " + itemInfo.Count;
                g.DrawString(text, GearGraphics.ItemDetailFont2, Brushes.White, 13, picH, StringFormat.GenericTypographic);
                g.DrawString(itemInfo.ProbWeight + "%", GearGraphics.ItemDetailFont2, Brushes.White, 278, picH, fmt);
                picH += 16;
            }

            picH += 4;

            g.DrawString("消耗物品", GearGraphics.ItemDetailFont, GearGraphics.GreenBrush2, 13, picH);
            picH += 16;
            foreach (RecipeItemInfo itemInfo in this.Recipe.RecipeItems)
            {
                sr = GetSRByItemID(itemInfo.ItemID);
                string text = sr != null ? sr.Name : itemInfo.ItemID.ToString();
                text += " x " + itemInfo.Count;
                g.DrawString(text, GearGraphics.ItemDetailFont2, Brushes.White, 13, picH, StringFormat.GenericTypographic);
                picH += 16;
            }

            picH += 5;
            fmt.Dispose();
            g.Dispose();
            return tooltip;
        }

        private StringResult GetSRByItemID(int itemID)
        {
            if (StringLinker == null)
            {
                return null;
            }

            StringResult sr = null;

            int itemIDClass = itemID / 1000000;
            if (itemIDClass == 1)
            {
                StringLinker.StringEqp.TryGetValue(itemID, out sr);
            }
            else if (itemIDClass >= 2 && itemIDClass <= 5)
            {
                StringLinker.StringItem.TryGetValue(itemID, out sr);
            }

            return sr;
        }
    }
}
