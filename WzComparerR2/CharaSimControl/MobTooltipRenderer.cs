using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using WzComparerR2.CharaSim;
using WzComparerR2.Common;
using static WzComparerR2.CharaSimControl.RenderHelper;

namespace WzComparerR2.CharaSimControl
{
    public class MobTooltipRenderer : TooltipRender
    {

        public MobTooltipRenderer()
        {
        }

        public override object TargetItem
        {
            get { return this.MobInfo; }
            set { this.MobInfo = value as Mob; }
        }

        public Mob MobInfo { get; set; }

        public override Bitmap Render()
        {
            if (MobInfo == null)
            {
                return null;
            }

            Bitmap bmp = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmp);

            //预绘制
            List<TextBlock> titleBlocks = new List<TextBlock>();

            if (MobInfo.ID > -1)
            {
                string mobName = GetMobName(MobInfo.ID);
                var block = PrepareText(g, mobName ?? "(null)", GearGraphics.ItemNameFont2, Brushes.White, 0, 0);
                titleBlocks.Add(block);
                block = PrepareText(g, "ID:" + MobInfo.ID, GearGraphics.ItemDetailFont, Brushes.White, block.Size.Width + 4, 4);
                titleBlocks.Add(block);
            }

            List<TextBlock> propBlocks = new List<TextBlock>();
            int picY = 0;

            StringBuilder sbExt = new StringBuilder();
            if (MobInfo.Boss && MobInfo.PartyBonusMob)
            {
                sbExt.Append("[Mini-Boss] ");
            }
            if (MobInfo.Boss && !MobInfo.PartyBonusMob)
            {
                sbExt.Append("[Boss] ");
            }
            if (MobInfo.Undead)
            {
                sbExt.Append("[Undead] ");
            }
            if (MobInfo.FirstAttack)
            {
                sbExt.Append("[Auto-aggressive] ");
            }
            if (!MobInfo.BodyAttack)
            {
                sbExt.Append("[No Touch Damage] ");
            }
            if (MobInfo.DamagedByMob)
            {
                sbExt.Append("[Ally Mob] ");
            }
            if (MobInfo.Invincible)
            {
                sbExt.Append("[Invincible] ");
            }
            if (MobInfo.NotAttack)
            {
                sbExt.Append("[Cannot Attack] ");
            }
            if (MobInfo.IgnoreMoveImpact)
            {
                sbExt.Append("[Cannot Be Rushed] ");
            }
            if (MobInfo.IgnoreMovable)
            {
                sbExt.Append("[Cannot Be Stunned/Binded] ");
            }
            if (MobInfo.FixedDamage > 0)
            {
                sbExt.Append("[Fixed Damage: " + MobInfo.FixedDamage + "] ");
            }

            if (sbExt.Length > 1)
            {
                sbExt.Remove(sbExt.Length - 1, 1);
                propBlocks.Add(PrepareText(g, sbExt.ToString(), GearGraphics.ItemDetailFont, Brushes.GreenYellow, 0, picY));
                picY += 16;
            }

            if (MobInfo.RemoveAfter > 0)
            {
                propBlocks.Add(PrepareText(g, "[After " + MobInfo.RemoveAfter + " seconds, dies automatically] ", GearGraphics.ItemDetailFont, Brushes.GreenYellow, 0, picY));
                picY += 16;
            }
            
            propBlocks.Add(PrepareText(g, "LEVEL: " + MobInfo.Level, GearGraphics.ItemDetailFont, Brushes.White, 0, picY));
            string hpNum = !string.IsNullOrEmpty(MobInfo.FinalMaxHP) ? this.AddCommaSeparators(MobInfo.FinalMaxHP) : MobInfo.MaxHP.ToString("N0");
            propBlocks.Add(PrepareText(g, "HP: " + hpNum, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            string mpNum = !string.IsNullOrEmpty(MobInfo.FinalMaxMP) ? this.AddCommaSeparators(MobInfo.FinalMaxMP) : MobInfo.MaxMP.ToString("N0");
            propBlocks.Add(PrepareText(g, "MP: " + mpNum, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            string padNum = this.AddCommaSeparators(MobInfo.PADamage.ToString("N0"));
            propBlocks.Add(PrepareText(g, "PAD: " + padNum, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            string madNum = this.AddCommaSeparators(MobInfo.MADamage.ToString("N0"));
            propBlocks.Add(PrepareText(g, "MAD: " + madNum, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            string pddNum = this.AddCommaSeparators(MobInfo.MDDamage.ToString("N0"));
            propBlocks.Add(PrepareText(g, "PDEF: " + pddNum, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            string mddNum = this.AddCommaSeparators(MobInfo.MDDamage.ToString("N0"));
            propBlocks.Add(PrepareText(g, "MDEF: " + mddNum, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            propBlocks.Add(PrepareText(g, "PDR: " + MobInfo.PDRate + "%", GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            propBlocks.Add(PrepareText(g, "MDR: " + MobInfo.MDRate + "%", GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            string accNum = this.AddCommaSeparators(MobInfo.Acc.ToString("N0"));
            propBlocks.Add(PrepareText(g, "ACC: " + accNum, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            string expNum = this.AddCommaSeparators(MobInfo.Exp.ToString("N0"));
            propBlocks.Add(PrepareText(g, "EXP: " + expNum, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            propBlocks.Add(PrepareText(g, GetElemAttrString(MobInfo.ElemAttr), GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            picY += 28;

            if (MobInfo.Revive.Count > 0)
            {
                Dictionary<int, int> reviveCounts = new Dictionary<int, int>();
                foreach (var reviveID in MobInfo.Revive)
                {
                    int count = 0;
                    reviveCounts.TryGetValue(reviveID, out count);
                    reviveCounts[reviveID] = count + 1;
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("Summons upon death: ");
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
                        sb.Append(" * " + kv.Value);
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
            Bitmap mobImg = MobInfo.Default.Bitmap;
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
            if (mobImg != null && !imgRect.IsEmpty)
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

            sb1.Append("ILFSHDP");
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
                case ElemResistance.Immune: e = "1"; break;
                case ElemResistance.Resist: e = "2"; break;
                case ElemResistance.Normal: e = "0"; break;
                case ElemResistance.Weak: e = "3"; break;
            }
            return e ?? "  ";
        }

        private string AddCommaSeparators(string number)
        {
            return Regex.Replace(number, @"^(\d+?)(\d{3})+$", m =>
            {
                var sb = new StringBuilder();
                sb.Append(m.Result("$1"));
                foreach (Capture cap in m.Groups[2].Captures)
                {
                    sb.Append(",");
                    sb.Append(cap.ToString());
                }
                return sb.ToString();
            });
        }
    }
}