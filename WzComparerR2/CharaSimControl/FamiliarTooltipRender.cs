using System.Collections.Generic;
using System.Drawing;
using WzComparerR2.CharaSim;
using WzComparerR2.Common;
using WzComparerR2.PluginBase;
using static WzComparerR2.CharaSimControl.RenderHelper;
using Resource = CharaSimResource.Resource;

namespace WzComparerR2.CharaSimControl
{
    public class FamiliarTooltipRender : TooltipRender
    {
        public FamiliarTooltipRender()
        {
        }

        private Familiar familiar;

        public Familiar Familiar
        {
            get { return familiar; }
            set { familiar = value; }
        }

        public override object TargetItem
        {
            get { return this.familiar; }
            set { this.familiar = value as Familiar; }
        }

        public int? ItemID { get; set; }
        public int FamiliarTier { get; set; }
        public bool AllowOutOfBounds { get; set; }
        public bool UseAssembleUI { get; set; }

        public override Bitmap Render()
        {
            if (this.familiar == null)
            {
                return null;
            }
            return UseAssembleUI ? GeneratePostAssembleFamiliarCard() : GeneratePreAssembleFamiliarCard();
        }

        private Bitmap GeneratePreAssembleFamiliarCard()
        {
            Bitmap baseTooltip = Resource.UIFamiliar_img_familiarCard_backgrnd;

            // Get Mob image and name
            Mob mob = Mob.CreateFromNode(PluginManager.FindWz($@"Mob\{familiar.MobID.ToString().PadLeft(7, '0')}.img"), PluginManager.FindWz);
            Point alignOrigin = new Point(161, 200);
            Point mobOrigin = new Point(0, 0);
            int mobXoffset = 0;
            int mobYoffset = 0;
            int tDelta = 0;
            int bDelta = 0;
            int lDelta = 0;
            int rDelta = 0;
            if (familiar.FamiliarCover.Bitmap != null)
            {
                mobOrigin = familiar.FamiliarCover.Origin;
            }
            else
            {
                mobOrigin = mob.Default.Origin;
            }
            Bitmap mobImg = Crop(familiar.FamiliarCover.Bitmap ?? mob.Default.Bitmap, alignOrigin, mobOrigin, out mobXoffset, out mobYoffset, out tDelta, out bDelta, out lDelta, out rDelta);

            Bitmap tooltip = new Bitmap(baseTooltip.Width + lDelta + rDelta, baseTooltip.Height + tDelta + bDelta);

            using (Graphics g = Graphics.FromImage(tooltip))
            {
                g.Clear(Color.Transparent);
                g.DrawImage(baseTooltip, lDelta, tDelta, new Rectangle(0, 0, baseTooltip.Width, baseTooltip.Height), GraphicsUnit.Pixel);

                // Draw Familiar Card basic background
                g.DrawImage(Resource.UIFamiliar_img_familiarCard_base, 45 + lDelta, 37 + tDelta, new Rectangle(0, 0, Resource.UIFamiliar_img_familiarCard_base.Width, Resource.UIFamiliar_img_familiarCard_base.Height), GraphicsUnit.Pixel);
                g.DrawImage(Resource.UIFamiliar_img_familiarCard_name, 31 + lDelta, 222 + tDelta, new Rectangle(0, 0, Resource.UIFamiliar_img_familiarCard_name.Width, Resource.UIFamiliar_img_familiarCard_name.Height), GraphicsUnit.Pixel);

                g.DrawImage(mobImg, mobXoffset + lDelta, mobYoffset + tDelta, new Rectangle(0, 0, mobImg.Width, mobImg.Height), GraphicsUnit.Pixel);

                if (this.FamiliarTier == 4)
                {
                    // draw Legendary bezel
                    g.DrawImage(Resource.UIFamiliar_img_familiarCard_legendary, 35 + lDelta, 24 + tDelta, new Rectangle(0, 0, Resource.UIFamiliar_img_familiarCard_legendary.Width, Resource.UIFamiliar_img_familiarCard_legendary.Height), GraphicsUnit.Pixel);
                }

                g.DrawImage(Resource.UIFamiliar_img_jewel_backgrnd, 25 + lDelta, 21 + tDelta, new Rectangle(0, 0, Resource.UIFamiliar_img_jewel_backgrnd.Width, Resource.UIFamiliar_img_jewel_backgrnd.Height), GraphicsUnit.Pixel);

                switch (this.FamiliarTier)
                {
                    default:
                    case 0:
                        g.DrawImage(Resource.UIFamiliar_img_jewel_normal_5, 30 + lDelta, 27 + tDelta, new Rectangle(0, 0, Resource.UIFamiliar_img_jewel_normal_5.Width, Resource.UIFamiliar_img_jewel_normal_5.Height), GraphicsUnit.Pixel);
                        g.DrawImage(Resource.UIFamiliar_img_jewel_normal_0, 38 + lDelta, 25 + tDelta, new Rectangle(0, 0, Resource.UIFamiliar_img_jewel_normal_0.Width, Resource.UIFamiliar_img_jewel_normal_0.Height), GraphicsUnit.Pixel);
                        break;
                    case 1:
                        g.DrawImage(Resource.UIFamiliar_img_jewel_rare_5, 30 + lDelta, 27 + tDelta, new Rectangle(0, 0, Resource.UIFamiliar_img_jewel_rare_5.Width, Resource.UIFamiliar_img_jewel_rare_5.Height), GraphicsUnit.Pixel);
                        g.DrawImage(Resource.UIFamiliar_img_jewel_rare_0, 38 + lDelta, 25 + tDelta, new Rectangle(0, 0, Resource.UIFamiliar_img_jewel_rare_0.Width, Resource.UIFamiliar_img_jewel_rare_0.Height), GraphicsUnit.Pixel);
                        break;
                    case 2:
                        g.DrawImage(Resource.UIFamiliar_img_jewel_epic_5, 30 + lDelta, 27 + tDelta, new Rectangle(0, 0, Resource.UIFamiliar_img_jewel_epic_5.Width, Resource.UIFamiliar_img_jewel_epic_5.Height), GraphicsUnit.Pixel);
                        g.DrawImage(Resource.UIFamiliar_img_jewel_epic_0, 38 + lDelta, 25 + tDelta, new Rectangle(0, 0, Resource.UIFamiliar_img_jewel_epic_0.Width, Resource.UIFamiliar_img_jewel_epic_0.Height), GraphicsUnit.Pixel);
                        break;
                    case 3:
                        g.DrawImage(Resource.UIFamiliar_img_jewel_unique_5, 30 + lDelta, 27 + tDelta, new Rectangle(0, 0, Resource.UIFamiliar_img_jewel_unique_5.Width, Resource.UIFamiliar_img_jewel_unique_5.Height), GraphicsUnit.Pixel);
                        g.DrawImage(Resource.UIFamiliar_img_jewel_unique_0, 38 + lDelta, 25 + tDelta, new Rectangle(0, 0, Resource.UIFamiliar_img_jewel_unique_0.Width, Resource.UIFamiliar_img_jewel_unique_0.Height), GraphicsUnit.Pixel);
                        break;
                    case 4:
                        g.DrawImage(Resource.UIFamiliar_img_jewel_legendary_5, 30 + lDelta, 27 + tDelta, new Rectangle(0, 0, Resource.UIFamiliar_img_jewel_legendary_5.Width, Resource.UIFamiliar_img_jewel_legendary_5.Height), GraphicsUnit.Pixel);
                        g.DrawImage(Resource.UIFamiliar_img_jewel_legendary_0, 38 + lDelta, 25 + tDelta, new Rectangle(0, 0, Resource.UIFamiliar_img_jewel_legendary_0.Width, Resource.UIFamiliar_img_jewel_legendary_0.Height), GraphicsUnit.Pixel);
                        break;
                }

                // Pre-Drawing
                List<TextBlock> titleBlocks = new List<TextBlock>();
                string mobName = GetMobName(mob.ID);
                var block = PrepareText(g, mobName ?? "(null)", GearGraphics.EpicGearDetailFont, Brushes.White, 0, 0);
                titleBlocks.Add(block);

                Rectangle titleRect = Measure(titleBlocks);

                int titleXoffset = Resource.UIFamiliar_img_familiarCard_name.Width >= Resource.UIFamiliar_img_familiarCard_name.Width ? (Resource.UIFamiliar_img_familiarCard_name.Width - titleRect.Width) / 2 : 0;
                int titleYoffset = (Resource.UIFamiliar_img_familiarCard_name.Height - titleRect.Height) / 2;

                foreach (var item in titleBlocks)
                {
                    DrawText(g, item, new Point(31 + lDelta + titleXoffset, 222 + tDelta + titleYoffset));
                }

                // Draw Skill
                string skillName = GetFamiliarSkillName(this.familiar.SkillID);
                if (!string.IsNullOrEmpty(skillName))
                {
                    List<TextBlock> skillBlocks = new List<TextBlock>();
                    block = PrepareText(g, skillName, GearGraphics.EpicGearDetailFont, Brushes.White, 0, 0);
                    skillBlocks.Add(block);

                    Rectangle skillRect = Measure(skillBlocks);

                    int skillXoffset = Resource.UIFamiliar_img_familiarCard_name.Width >= Resource.UIFamiliar_img_familiarCard_name.Width ? (Resource.UIFamiliar_img_familiarCard_name.Width - skillRect.Width) / 2 : 0;
                    int skillYoffset = Resource.UIFamiliar_img_familiarCard_name.Height + 1;

                    foreach (var item in skillBlocks)
                    {
                        DrawText(g, item, new Point(31 + lDelta + skillXoffset, 222 + tDelta + skillYoffset));
                    }

                }


                // Layout
                if (this.ShowObjectID)
                {
                    GearGraphics.DrawGearDetailNumber(g, 24 + lDelta, 24 + tDelta, this.ItemID != null ? $"{((int)this.ItemID).ToString("d8")}" : $"{this.familiar.FamiliarID.ToString()}", true);
                }
            }
            return tooltip;
        }

        private Bitmap GeneratePostAssembleFamiliarCard()
        {
            // To be implemented
            return GeneratePreAssembleFamiliarCard();
        }

        private string GetMobName(int mobID)
        {
            StringResult sr;
            if (this.StringLinker == null || !this.StringLinker.StringMob.TryGetValue(mobID, out sr))
            {
                return null;
            }
            else
            {
                return sr.Name;
            }
        }

        private string GetFamiliarSkillName(int skillID)
        {
            StringResult sr;
            if (this.StringLinker == null || !this.StringLinker.StringFamiliarSkill.TryGetValue(skillID, out sr))
            {
                return null;
            }
            else
            {
                return sr.Name;
            }
        }

        private Bitmap Crop(Bitmap sourceBmp, Point alignOrigin, Point mobOrigin, out int xOffset, out int yOffset, out int tDelta, out int bDelta, out int lDelta, out int rDelta)
        {
            tDelta = 0;
            bDelta = 0;
            lDelta = 0;
            rDelta = 0;
            xOffset = 0;
            yOffset = 0;

            if (sourceBmp == null)
            {
                return null;
            }

            xOffset = alignOrigin.X - mobOrigin.X;
            yOffset = alignOrigin.Y - mobOrigin.Y;

            if (this.AllowOutOfBounds)
            {
                lDelta = mobOrigin.X > alignOrigin.X ? mobOrigin.X - alignOrigin.X : 0;
                rDelta = sourceBmp.Width - mobOrigin.X > 154 ? sourceBmp.Width - mobOrigin.X - 154 : 0;
                tDelta = mobOrigin.Y > alignOrigin.Y ? mobOrigin.Y - alignOrigin.Y : 0;
                bDelta = sourceBmp.Height - mobOrigin.Y > 228 ? sourceBmp.Height - mobOrigin.Y - 228 : 0;
                return sourceBmp;
            }

            int rectangXoffset = 0;
            int rectangYoffset = 0;
            int rectangWidth = 0;
            int rectangHeight = 0;

            if (mobOrigin.X > 108) // Define Left Border
            {
                rectangXoffset = mobOrigin.X - 108;
                rectangWidth += 108;
            }
            else
            {
                rectangWidth += mobOrigin.X;
            }

            if (sourceBmp.Width - mobOrigin.X > 104) // Define Right Border
            {
                rectangWidth += 104;
            }
            else
            {
                rectangWidth += sourceBmp.Width - mobOrigin.X;
            }

            if (mobOrigin.Y > 154) // Define Top Border
            {
                rectangYoffset = mobOrigin.Y - 154;
                rectangHeight += 154;
            }
            else
            {
                rectangHeight += mobOrigin.Y;
            }

            if (sourceBmp.Height - mobOrigin.Y > 18)
            {
                rectangHeight += 18;
            }
            else
            {
                rectangHeight += sourceBmp.Height - mobOrigin.Y;
            }

            xOffset = rectangXoffset == 0 ? xOffset : 53;
            yOffset = rectangYoffset == 0 ? yOffset : 46;

            return sourceBmp.Clone(new Rectangle(rectangXoffset, rectangYoffset, rectangWidth, rectangHeight), sourceBmp.PixelFormat);
        }
    }
}
