#if MapRenderV1
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Resource = CharaSimResource.Resource;
using WzComparerR2.MapRender.Patches;
using WzComparerR2.Common;
using WzComparerR2.Rendering;
using static WzComparerR2.MapRender.UI.TooltipHelper;

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

                        blocks.Add(PrepareTextBlock(env.Fonts.TooltipTitleFont, sr == null ? "(null)" : sr.Name, ref current, Color.White));
                        current += new Vector2(4, 4);
                        blocks.Add(PrepareTextBlock(env.Fonts.TooltipContentFont, "id:" + p.LifeID.ToString("d7"), ref current, Color.White));
                        size.X = Math.Max(size.X, current.X);
                        current = new Vector2(0, current.Y + 16);

                        LifeInfo info = p.LifeInfo;
                        Vector2 size2;
                        var blocks2 = TooltipHelper.Prepare(info, env.Fonts, out size2);
                        for (int i = 0; i < blocks2.Length; i++)
                        {
                            blocks2[i].Position.Y += current.Y;
                            blocks.Add(blocks2[i]);
                        }
                        size.X = Math.Max(size.X, size2.X);
                        size.Y = current.Y + size2.Y;
                    }
                    break;
                case RenderObjectType.Npc:
                    {
                        LifePatch p = tooltipTarget as LifePatch;
                        stringLinker.StringNpc.TryGetValue(p.LifeID, out sr);
                        Vector2 current = Vector2.Zero;

                        blocks.Add(PrepareTextBlock(env.Fonts.TooltipTitleFont, sr == null ? "(null)" : sr.Name, ref current, Color.White));
                        current += new Vector2(4, 4);
                        blocks.Add(PrepareTextBlock(env.Fonts.TooltipContentFont, "id:" + p.LifeID.ToString("d7"), ref current, Color.White));
                        size.X = Math.Max(size.X, current.X);
                        current = new Vector2(0, current.Y + 16);

                        foreach (var kv in p.Actions)
                        {
                            if (kv.Value == p.Frames)
                            {
                                blocks.Add(PrepareTextLine(env.Fonts.TooltipContentFont, "action: " + kv.Key, ref current, Color.White, ref size.X));
                            }
                        }
                        size.Y = current.Y;
                    }
                    break;

                case RenderObjectType.Portal:
                    {
                        PortalPatch p = tooltipTarget as PortalPatch;
                        Vector2 current = Vector2.Zero;
                        blocks.Add(PrepareTextLine(env.Fonts.TooltipContentFont, "pName: " + p.PortalName, ref current, Color.White, ref size.X));
                        string pTypeName = GetPortalTypeString(p.PortalType);
                        blocks.Add(PrepareTextLine(env.Fonts.TooltipContentFont, "pType: " + p.PortalType + (pTypeName == null ? null : (" (" + pTypeName + ")")), ref current, Color.White, ref size.X));
                        stringLinker.StringMap.TryGetValue(p.ToMap, out sr);
                        blocks.Add(PrepareTextLine(env.Fonts.TooltipContentFont, "toMap: " + (sr == null ? "(null)" : sr.Name) + "(" + p.ToMap + ")", ref current, Color.White, ref size.X));
                        blocks.Add(PrepareTextLine(env.Fonts.TooltipContentFont, "toName: " + p.ToName, ref current, Color.White, ref size.X));
                        if (!string.IsNullOrEmpty(p.Script))
                            blocks.Add(PrepareTextLine(env.Fonts.TooltipContentFont, "script: " + p.Script, ref current, Color.White, ref size.X));
                        size.Y = current.Y;
                    }
                    break;
            }

            if (blocks.Count > 0)
            {
                size += new Vector2(26, 26);
                Vector2 origin = new Vector2(env.Input.MousePosition.X, env.Input.MousePosition.Y);
                origin.X = MathHelper.Clamp(origin.X, 0, Math.Max(0, env.Camera.Width - size.X));
                origin.Y = MathHelper.Clamp(origin.Y, 0, Math.Max(0, env.Camera.Height - size.Y));
                this.DrawFrame(env, origin, size);

                origin += new Vector2(13, 13);
                foreach (TextBlock block in blocks)
                {
                    env.Sprite.DrawStringEx(block.Font, block.Text, block.Position, block.ForeColor, -origin);
                }
            }
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

    }
}
#endif