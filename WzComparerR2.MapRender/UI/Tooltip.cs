using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Resource = CharaSimResource.Resource;
using WzComparerR2.MapRender.Patches;
using WzComparerR2.Common;
using WzComparerR2.Rendering;

namespace WzComparerR2.MapRender.UI
{
    public class Tooltip
    {
        public Tooltip(GraphicsDevice graphicsDevice)
        {
            this.frame = new Dictionary<string, Texture2D>();
            this.frame["n"] = Resource.UIToolTip_img_Item_Frame2_n.ToTexture(graphicsDevice);
            this.frame["ne"] = Resource.UIToolTip_img_Item_Frame2_ne.ToTexture(graphicsDevice);
            this.frame["e"] = Resource.UIToolTip_img_Item_Frame2_e.ToTexture(graphicsDevice);
            this.frame["se"] = Resource.UIToolTip_img_Item_Frame2_se.ToTexture(graphicsDevice);
            this.frame["s"] = Resource.UIToolTip_img_Item_Frame2_s.ToTexture(graphicsDevice);
            this.frame["sw"] = Resource.UIToolTip_img_Item_Frame2_sw.ToTexture(graphicsDevice);
            this.frame["w"] = Resource.UIToolTip_img_Item_Frame2_w.ToTexture(graphicsDevice);
            this.frame["nw"] = Resource.UIToolTip_img_Item_Frame2_nw.ToTexture(graphicsDevice);
            this.frame["c"] = Resource.UIToolTip_img_Item_Frame2_c.ToTexture(graphicsDevice);
            this.frame["cover"] = Resource.UIToolTip_img_Item_Frame2_cover.ToTexture(graphicsDevice);
        }

        private Dictionary<string, Texture2D> frame;
        private RenderPatch tooltipTarget;

        public RenderPatch TooltipTarget
        {
            get { return tooltipTarget; }
            set { tooltipTarget = value; }
        }

        public void DrawTooltip(GameTime gameTime, RenderEnv env, StringLinker stringLinker)
        {
            if (tooltipTarget == null)
                return;

            StringResult sr;
            List<TextBlock> blocks = new List<TextBlock>();
            Vector2 size = Vector2.Zero;
            switch (tooltipTarget.ObjectType)
            {
                case RenderObjectType.Mob:
                    {
                        LifePatch p = tooltipTarget as LifePatch;
                        stringLinker.StringMob.TryGetValue(p.LifeID, out sr);
                        Vector2 current = Vector2.Zero;

                        PrepareTextBlock(blocks, env.Fonts.TooltipTitleFont, sr == null ? "(null)" : sr.Name, ref current, Color.White);
                        current += new Vector2(4, 4);
                        PrepareTextBlock(blocks, env.Fonts.TooltipContentFont, "id:" + p.LifeID.ToString("d7"), ref current, Color.White);
                        size.X = Math.Max(size.X, current.X);
                        current = new Vector2(0, current.Y + 16);

                        LifeInfo info = p.LifeInfo;

                        PrepareTextLine(blocks, env.Fonts.TooltipContentFont, "Level: " + info.level + (info.boss ? " (Boss)" : null), ref current, Color.White, ref size.X);
                        PrepareTextLine(blocks, env.Fonts.TooltipContentFont, "HP/MP: " + info.maxHP + " / " + info.maxMP, ref current, Color.White, ref size.X);
                        PrepareTextLine(blocks, env.Fonts.TooltipContentFont, "PAD/MAD: " + info.PADamage + " / " + info.MADamage, ref current, Color.White, ref size.X);
                        PrepareTextLine(blocks, env.Fonts.TooltipContentFont, "PDr/MDr: " + info.PDRate + "% / " + info.MDRate + "%", ref current, Color.White, ref size.X);
                        PrepareTextLine(blocks, env.Fonts.TooltipContentFont, "Acc/Eva: " + info.acc + " / " + info.eva, ref current, Color.White, ref size.X);
                        PrepareTextLine(blocks, env.Fonts.TooltipContentFont, "KB: " + info.pushed, ref current, Color.White, ref size.X);
                        PrepareTextLine(blocks, env.Fonts.TooltipContentFont, "Exp: " + info.exp, ref current, Color.White, ref size.X);
                        if (info.undead) PrepareTextLine(blocks, env.Fonts.TooltipContentFont, "undead: 1", ref current, Color.White, ref size.X);
                        StringBuilder sb;
                        if ((sb = GetLifeElemAttrString(ref info.elemAttr)).Length > 0)
                            PrepareTextLine(blocks, env.Fonts.TooltipContentFont, "elem: " + sb.ToString(), ref current, Color.White, ref size.X);
                        size.Y = current.Y;
                    }
                    break;
                case RenderObjectType.Npc:
                    {
                        LifePatch p = tooltipTarget as LifePatch;
                        stringLinker.StringNpc.TryGetValue(p.LifeID, out sr);
                        Vector2 current = Vector2.Zero;

                        PrepareTextBlock(blocks, env.Fonts.TooltipTitleFont, sr == null ? "(null)" : sr.Name, ref current, Color.White);
                        current += new Vector2(4, 4);
                        PrepareTextBlock(blocks, env.Fonts.TooltipContentFont, "id:" + p.LifeID.ToString("d7"), ref current, Color.White);
                        size.X = Math.Max(size.X, current.X);
                        current = new Vector2(0, current.Y + 16);

                        foreach (var kv in p.Actions)
                        {
                            if (kv.Value == p.Frames)
                            {
                                PrepareTextLine(blocks, env.Fonts.TooltipContentFont, "action: " + kv.Key, ref current, Color.White, ref size.X);
                            }
                        }
                        size.Y = current.Y;
                    }
                    break;

                case RenderObjectType.Portal:
                    {
                        PortalPatch p = tooltipTarget as PortalPatch;
                        Vector2 current = Vector2.Zero;
                        PrepareTextLine(blocks, env.Fonts.TooltipContentFont, "pName: " + p.PortalName, ref current, Color.White, ref size.X);
                        string pTypeName = GetPortalTypeString(p.PortalType);
                        PrepareTextLine(blocks, env.Fonts.TooltipContentFont, "pType: " + p.PortalType + (pTypeName == null ? null : (" (" + pTypeName + ")")), ref current, Color.White, ref size.X);
                        stringLinker.StringMap.TryGetValue(p.ToMap, out sr);
                        PrepareTextLine(blocks, env.Fonts.TooltipContentFont, "toMap: " + (sr == null ? "(null)" : sr.Name) + "(" + p.ToMap + ")", ref current, Color.White, ref size.X);
                        PrepareTextLine(blocks, env.Fonts.TooltipContentFont, "toName: " + p.ToName, ref current, Color.White, ref size.X);
                        if (!string.IsNullOrEmpty(p.Script))
                            PrepareTextLine(blocks, env.Fonts.TooltipContentFont, "script: " + p.Script, ref current, Color.White, ref size.X);
                        size.Y = current.Y;
                    }
                    break;
            }

            if (blocks.Count > 0)
            {
                size += new Vector2(26, 26);
                Vector2 origin = new Vector2(env.Input.MousePosition.X, env.Input.MousePosition.Y);
                origin.X = MathHelper.Clamp(origin.X, 0, Math.Max(0, env.Camera.Width - size.X));
                origin.Y = MathHelper.Clamp(origin.Y, 0, Math.Max(0, env.Camera.Height- size.Y));
                this.DrawFrame(env, origin, size);

                origin += new Vector2(13, 13);
                foreach (TextBlock block in blocks)
                {
                    env.Sprite.DrawStringEx(block.Font, block.Text, block.Position, block.ForeColor, -origin);
                }
            }
        }

        private StringBuilder GetLifeElemAttrString(ref LifeInfo.ElemAttr elemAttr)
        {
            StringBuilder sb = new StringBuilder(14);
            sb.Append(GetElemResistanceString("冰", elemAttr.I));
            sb.Append(GetElemResistanceString("雷", elemAttr.L));
            sb.Append(GetElemResistanceString("火", elemAttr.F));
            sb.Append(GetElemResistanceString("毒", elemAttr.S));
            sb.Append(GetElemResistanceString("圣", elemAttr.H));
            sb.Append(GetElemResistanceString("暗", elemAttr.D));
            sb.Append(GetElemResistanceString("物", elemAttr.P));
            return sb;
        }

        private string GetPortalTypeString(int pType)
        {
            switch (pType)
            {
                case 0: return "地图出生点";
                case 1: return "一般传送门(隐藏)";
                case 2: return "一般传送门";
                case 3: return "一般传送门(接触)";
                case 6: return "时空门入口点";
                case 7: return "脚本传送门";
                case 8: return "脚本传送门(隐藏)";
                case 9: return "脚本传送门(接触)";
                case 10: return "地图内传送门";
                case 12: return "弹力装置";
                default: return null;
            }
        }

        private string GetElemResistanceString(string elemName, LifeInfo.ElemResistance resist)
        {
            string e = null;
            switch (resist)
            {
                case LifeInfo.ElemResistance.Immune: e = "× "; break;
                case LifeInfo.ElemResistance.Resist: e = "△ "; break;
                case LifeInfo.ElemResistance.Normal: e = null; break;
                case LifeInfo.ElemResistance.Weak: e = "◎ "; break;
            }
            return e != null ? (elemName + e) : null;
        }

        private void PrepareTextBlock(IList<TextBlock> list, XnaFont font, string text, ref Vector2 pos, Color color)
        {
            Vector2 size = font.MeasureString(text);

            TextBlock block = new TextBlock();
            block.Font = font;
            block.Text = text;
            block.Position = pos;
            block.ForeColor = color;
            list.Add(block);

            pos.X += size.X;
        }

        private void PrepareTextLine(IList<TextBlock> list, XnaFont font, string text, ref Vector2 pos, Color color, ref float maxWidth)
        {
            Vector2 size = font.MeasureString(text);

            TextBlock block = new TextBlock();
            block.Font = font;
            block.Text = text;
            block.Position = pos;
            block.ForeColor = color;
            list.Add(block);

            maxWidth = Math.Max(pos.X + size.X, maxWidth);
            pos.X = 0;
            pos.Y += size.Y;
        }

        private void DrawFrame(RenderEnv env, Vector2 position, Vector2 size)
        {
            SpriteBatchEx sprite = env.Sprite;
            sprite.Draw(this.frame["nw"], position, Color.White);
            sprite.Draw(this.frame["ne"], position + new Vector2(size.X - 13, 0), Color.White);
            sprite.Draw(this.frame["sw"], position + new Vector2(0, size.Y - 13), Color.White);
            sprite.Draw(this.frame["se"], position + new Vector2(size.X - 13, size.Y - 13), Color.White);
            if (size.X > 26)
            {
                sprite.Draw(this.frame["n"], new Rectangle((int)position.X + 13, (int)position.Y, (int)size.X - 26, 13), Color.White);
                sprite.Draw(this.frame["s"], new Rectangle((int)position.X + 13, (int)(position.Y + size.Y) - 13, (int)size.X - 26, 13), Color.White);
            }
            if (size.Y > 26)
            {
                sprite.Draw(this.frame["e"], new Rectangle((int)(position.X + size.X) - 13, (int)position.Y + 13, 13, (int)size.Y - 26), Color.White);
                sprite.Draw(this.frame["w"], new Rectangle((int)position.X, (int)position.Y + 13, 13, (int)size.Y - 26), Color.White);
            }
            if (size.X > 26 && size.Y > 26)
            {
                sprite.Draw(this.frame["c"], new Rectangle((int)position.X + 13, (int)position.Y + 13, (int)size.X - 26, (int)size.Y - 26), Color.White);
            }
            sprite.Draw(this.frame["cover"], position, Color.White);
        }

        public void DrawNameTooltip(GameTime gameTime, RenderEnv env, RenderPatch patch, StringLinker stringLinker)
        {
            StringResult sr;
            switch (patch.ObjectType)
            {
                case RenderObjectType.Mob:
                    {
                        LifePatch p = patch as LifePatch;
                        string name = "lv." + p.LifeInfo.level + " ";
                        if (stringLinker != null && stringLinker.StringMob.TryGetValue(p.LifeID, out sr))
                            name += sr.Name;
                        else
                            name += p.LifeID.ToString();
                        DrawNameTooltip(env, name, env.Fonts.MobNameFont, p.Position, Color.White);
                    }
                    break;
                case RenderObjectType.Npc:
                    {
                        LifePatch p = patch as LifePatch;
                        string name;
                        if (stringLinker != null && stringLinker.StringNpc.TryGetValue(p.LifeID, out sr))
                            name = sr.Name;
                        else
                            name = p.LifeID.ToString();
                        DrawNameTooltip(env, name, env.Fonts.NpcNameFont, p.Position, Color.Yellow);
                    }
                    break;
            }
        }

        private void DrawNameTooltip(RenderEnv env, string name, XnaFont font, Vector2 mapPosition, Color color)
        {
            SpriteBatchEx sprite = env.Sprite;
            Vector2 size = font.MeasureString(name);
            Rectangle rect = new Rectangle((int)(mapPosition.X - size.X / 2 - 2), (int)(mapPosition.Y + 2), (int)(size.X + 4), (int)(size.Y + 3));
            sprite.FillRectangle(rect, new Color(Color.Black, 0.7f), env.Camera.Origin);
            sprite.DrawStringEx(
                font,
                name,
                new Vector2(rect.X + 2, rect.Y + 2),
                color,
                env.Camera.Origin);
        }

        private struct TextBlock
        {
            public Vector2 Position;
            public Color ForeColor;
            public XnaFont Font;
            public string Text;
        }
    }
}
