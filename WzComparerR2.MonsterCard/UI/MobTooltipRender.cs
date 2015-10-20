using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using WzComparerR2.CharaSimControl;
using System.Drawing.Imaging;
using WzComparerR2.Common;
using static WzComparerR2.MonsterCard.UI.RenderHelper;

namespace WzComparerR2.MonsterCard.UI
{
    public class MobTooltipRender
    {
        public MobTooltipRender()
        {
        }

        public StringLinker StringLinker { get; set; }

        public Bitmap Render(MobInfo mobInfo, Bitmap mobImg)
        {
            if (mobInfo == null)
            {
                return null;
            }
            Bitmap bmp = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmp);

            //预绘制
            List<TextBlock> titleBlocks = new List<TextBlock>();

            if (mobInfo.ID != null)
            {
                string mobName = GetMobName(mobInfo.ID.Value);
                var block = PrepareText(g, mobName ?? "(null)", GearGraphics.ItemNameFont2, Brushes.White, 0, 0);
                titleBlocks.Add(block);
                block = PrepareText(g, "ID:" + mobInfo.ID, GearGraphics.ItemDetailFont, Brushes.White, block.Size.Width + 4, 4);
                titleBlocks.Add(block);
            }

            List<TextBlock> propBlocks = new List<TextBlock>();
            int picY = 0;

            StringBuilder sbExt = new StringBuilder();
            if (mobInfo.Boss)
            {
                sbExt.Append("Boss ");
            }
            if (mobInfo.Undead)
            {
                sbExt.Append("不死系 ");
            }
            if (mobInfo.FirstAttack)
            {
                sbExt.Append("主动攻击 ");
            }
            if (!mobInfo.BodyAttack)
            {
                sbExt.Append("无触碰伤害 ");
            }
            if (mobInfo.DamagedByMob)
            {
                sbExt.Append("只受怪物伤害 ");
            }
            if (mobInfo.Invincible)
            {
                sbExt.Append("无敌 ");
            }
            if (mobInfo.NotAttack)
            {
                sbExt.Append("无法攻击 ");
            }
            if (mobInfo.FixedDamage > 0)
            {
                sbExt.Append("固定伤害" + mobInfo.FixedDamage + " ");
            }

            if (sbExt.Length > 1)
            {
                sbExt.Remove(sbExt.Length - 1, 1);
                propBlocks.Add(PrepareText(g, sbExt.ToString(), GearGraphics.ItemDetailFont, Brushes.GreenYellow, 0, picY));
                picY += 16;
            }

            if (mobInfo.RemoveAfter > 0)
            {
                propBlocks.Add(PrepareText(g, "出生" + mobInfo.RemoveAfter + "秒后自动消失", GearGraphics.ItemDetailFont, Brushes.GreenYellow, 0, picY));
                picY += 16;
            }

            propBlocks.Add(PrepareText(g, "Level: " + mobInfo.Level, GearGraphics.ItemDetailFont, Brushes.White, 0, picY));
            propBlocks.Add(PrepareText(g, "HP: " + (!string.IsNullOrEmpty(mobInfo.FinalMaxHP)? mobInfo.FinalMaxHP : mobInfo.MaxHP.ToString()),
                GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            propBlocks.Add(PrepareText(g, "MP: " + (!string.IsNullOrEmpty(mobInfo.FinalMaxMP) ? mobInfo.FinalMaxMP : mobInfo.MaxMP.ToString()),
                GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            propBlocks.Add(PrepareText(g, "PAD: " + mobInfo.PADamage, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            propBlocks.Add(PrepareText(g, "MAD: " + mobInfo.MADamage, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            propBlocks.Add(PrepareText(g, "PDr: " + mobInfo.PDRate + "%", GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            propBlocks.Add(PrepareText(g, "MDr: " + mobInfo.MDRate + "%", GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            propBlocks.Add(PrepareText(g, "Acc: " + mobInfo.Acc, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            propBlocks.Add(PrepareText(g, "Eva: " + mobInfo.Eva, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            propBlocks.Add(PrepareText(g, "KB: " + mobInfo.Pushed, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            propBlocks.Add(PrepareText(g, "Exp: " + mobInfo.Exp, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            propBlocks.Add(PrepareText(g, GetElemAttrString(mobInfo.ElemAttr), GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            picY += 28;

            if (mobInfo.Revive.Count > 0)
            {
                Dictionary<int, int> reviveCounts = new Dictionary<int, int>();
                foreach (var reviveID in mobInfo.Revive)
                {
                    int count = 0;
                    reviveCounts.TryGetValue(reviveID, out count);
                    reviveCounts[reviveID] = count + 1;
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("死后召唤 ");
                int rowCount = 0;
                foreach (var kv in reviveCounts)
                {
                    if (rowCount++ > 0)
                    {
                        sb.AppendLine().Append("    ");
                    }
                    string mobName = GetMobName(kv.Key);
                    sb.AppendFormat("{0}({1:D7})", mobName, kv.Key);
                    if (kv.Value > 1)
                    {
                        sb.Append("*" + kv.Value);
                    }
                }

                propBlocks.Add(PrepareText(g, sb.ToString(), GearGraphics.ItemDetailFont, Brushes.GreenYellow, 0, picY));
            }
            g.Dispose();
            bmp.Dispose();

            //计算大小
            Rectangle titleRect = Measure(titleBlocks);
            Rectangle imgRect = Rectangle.Empty;
            Rectangle textRect = Measure(propBlocks);
            if (mobImg != null)
            {
                if (mobImg.Width > 250 || mobImg.Height > 300) //进行缩放
                {
                    double scale = Math.Min((double)250 / mobImg.Width, (double)300 / mobImg.Height);
                    imgRect = new Rectangle(0, 0, (int)(mobImg.Width * scale), (int)(mobImg.Height * scale));
                }
                else
                {
                    imgRect = new Rectangle(0, 0, mobImg.Width, mobImg.Height);
                }
            }


            //布局 
            //水平排列
            int width = 0;
            if (!imgRect.IsEmpty)
            {
                textRect.X = imgRect.Width + 4;
            }
            width = Math.Max(titleRect.Width, Math.Max(imgRect.Right, textRect.Right));
            titleRect.X = (width - titleRect.Width) / 2;

            //垂直居中
            int height = Math.Max(imgRect.Height, textRect.Height);
            imgRect.Y = (height - imgRect.Height) / 2;
            textRect.Y = (height - textRect.Height) / 2;
            if (!titleRect.IsEmpty)
            {
                height += titleRect.Height + 4;
                imgRect.Y += titleRect.Bottom + 4;
                textRect.Y += titleRect.Bottom + 4;
            }

            //绘制
            bmp = new Bitmap(width + 20, height + 20);
            titleRect.Offset(10, 10);
            imgRect.Offset(10, 10);
            textRect.Offset(10, 10);
            g = Graphics.FromImage(bmp);
            //绘制背景
            GearGraphics.DrawNewTooltipBack(g, 0, 0, bmp.Width, bmp.Height);
            //绘制标题
            foreach (var item in titleBlocks)
            {
                DrawText(g, item, titleRect.Location);
            }
            //绘制图像
            if (mobImg!= null && !imgRect.IsEmpty)
            {
                g.DrawImage(mobImg, imgRect);
            }
            //绘制文本
            foreach (var item in propBlocks)
            {
                DrawText(g, item, textRect.Location);
            }
            g.Dispose();
            return bmp;
        }

        private string GetMobName(int mobID)
        {
            StringResult sr;
            if (this.StringLinker == null || !this.StringLinker.StringMob.TryGetValue(mobID, out sr))
            {
                return null;
            }
            return sr.Name;
        }

        private string GetElemAttrString(MobElemAttr elemAttr)
        {
            StringBuilder sb1 = new StringBuilder(),
                sb2 = new StringBuilder();

            sb1.Append("冰雷火毒圣暗物");
            sb2.Append(GetElemAttrResistString(elemAttr.I));
            sb2.Append(GetElemAttrResistString(elemAttr.L));
            sb2.Append(GetElemAttrResistString(elemAttr.F));
            sb2.Append(GetElemAttrResistString(elemAttr.S));
            sb2.Append(GetElemAttrResistString(elemAttr.H));
            sb2.Append(GetElemAttrResistString(elemAttr.D));
            sb2.Append(GetElemAttrResistString(elemAttr.P));
            sb1.AppendLine().Append(sb2.ToString());
            return sb1.ToString();
        }

        private string GetElemAttrResistString(ElemResistance resist)
        {
            string e = null;
            switch (resist)
            {
                case ElemResistance.Immune: e = "×"; break;
                case ElemResistance.Resist: e = "△"; break;
                case ElemResistance.Normal: e = "○"; break;
                case ElemResistance.Weak: e = "◎"; break;
            }
            return e ?? "  ";
        }
    }
}
