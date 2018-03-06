using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Res = CharaSimResource.Resource;
using WzComparerR2.MapRender.Patches2;
using WzComparerR2.Common;
using WzComparerR2.Rendering;
using WzComparerR2.Animation;
using static WzComparerR2.MapRender.UI.TooltipHelper;

namespace WzComparerR2.MapRender.UI
{
    class Tooltip2
    {
        public Tooltip2(GraphicsDevice graphicsDevice)
        {
            this.LoadContent(graphicsDevice);
        }

        public NineFormResource Resource { get; private set; }
        public StringLinker StringLinker { get; set; }
        public object TooltipTarget { get; set; }

        public void Draw(GameTime gameTime, RenderEnv env)
        {
            if (this.TooltipTarget == null)
            {
                return;
            }

            var content = Draw(gameTime, env, this.TooltipTarget);
            if (content.blocks != null)
            {
                var pos = env.Input.MousePosition;
                DrawContent(env, content, new Vector2(pos.X + 16, pos.Y + 16), true);
            }
        }

        public void Draw(GameTime gameTime, RenderEnv env, object item, Vector2 centerPosition)
        {
            if (item == null)
            {
                return;
            }

            var content = Draw(gameTime, env, item);
            if (content.blocks != null)
            {
                var pos = new Vector2(centerPosition.X - content.size.X / 2, centerPosition.Y - content.size.Y / 2);
                DrawContent(env, content, pos, false);
            }
        }

        private void LoadContent(GraphicsDevice graphicsDevice)
        {
            var res = new NineFormResource();
            res.N = Res.UIToolTip_img_Item_Frame2_n.ToTexture(graphicsDevice);
            res.NE = Res.UIToolTip_img_Item_Frame2_ne.ToTexture(graphicsDevice);
            res.E = Res.UIToolTip_img_Item_Frame2_e.ToTexture(graphicsDevice);
            res.SE = Res.UIToolTip_img_Item_Frame2_se.ToTexture(graphicsDevice);
            res.S = Res.UIToolTip_img_Item_Frame2_s.ToTexture(graphicsDevice);
            res.SW = Res.UIToolTip_img_Item_Frame2_sw.ToTexture(graphicsDevice);
            res.W = Res.UIToolTip_img_Item_Frame2_w.ToTexture(graphicsDevice);
            res.NW = Res.UIToolTip_img_Item_Frame2_nw.ToTexture(graphicsDevice);
            res.C = Res.UIToolTip_img_Item_Frame2_c.ToTexture(graphicsDevice);
            this.Resource = res;
        }

        private TooltipContent Draw(GameTime gameTime, RenderEnv env, object target)
        {
            if (target is LifeItem)
            {
                return DrawItem(gameTime, env, (LifeItem)target);
            }
            else if (target is PortalItem)
            {
                return DrawItem(gameTime, env, (PortalItem)target);
            }
            else if (target is ReactorItem)
            {
                return DrawItem(gameTime, env, (ReactorItem)target);
            }
            else if (target is TooltipItem)
            {
                return DrawItem(gameTime, env, (TooltipItem)target);
            }
            else if (target is PortalItem.ItemTooltip)
            {
                return DrawString(gameTime, env, ((PortalItem.ItemTooltip)target).Title);
            }
            else if (target is UIWorldMap.Tooltip)
            {
                return DrawItem(gameTime, env, (UIWorldMap.Tooltip)target);
            }
            else if (target is string)
            {
                return DrawString(gameTime, env, (string)target);
            }
            return new TooltipContent();
        }

        private TooltipContent DrawItem(GameTime gameTime, RenderEnv env, LifeItem item)
        {
            var blocks = new List<TextBlock>();
            Vector2 size = Vector2.Zero;

            blocks = new List<TextBlock>();
            StringResult sr = null;
            Vector2 current = Vector2.Zero;

            switch (item.Type)
            {
                case LifeItem.LifeType.Mob:
                    {
                        this.StringLinker?.StringMob.TryGetValue(item.ID, out sr);
                        blocks.Add(PrepareTextBlock(env.Fonts.TooltipTitleFont, sr == null ? "(null)" : sr.Name, ref current, Color.LightYellow));
                        current += new Vector2(4, 4);
                        blocks.Add(PrepareTextBlock(env.Fonts.TooltipContentFont, "id:" + item.ID.ToString("d7"), ref current, Color.White));
                        size.X = Math.Max(size.X, current.X);
                        current = new Vector2(0, current.Y + 16);

                        Vector2 size2;
                        var blocks2 = TooltipHelper.Prepare(item.LifeInfo, env.Fonts, out size2);
                        for (int i = 0; i < blocks2.Length; i++)
                        {
                            blocks2[i].Position.Y += current.Y;
                            blocks.Add(blocks2[i]);
                        }
                        size.X = Math.Max(size.X, size2.X);
                        size.Y = current.Y + size2.Y;
                    }
                    break;

                case LifeItem.LifeType.Npc:
                    {
                        this.StringLinker?.StringNpc.TryGetValue(item.ID, out sr);
                        blocks.Add(PrepareTextBlock(env.Fonts.TooltipTitleFont, sr == null ? "(null)" : sr.Name, ref current, Color.LightYellow));
                        current += new Vector2(4, 4);
                        blocks.Add(PrepareTextBlock(env.Fonts.TooltipContentFont, "id:" + item.ID.ToString("d7"), ref current, Color.White));
                        size.X = Math.Max(size.X, current.X);
                        current = new Vector2(0, current.Y + 16);

                        var aniName = (item.View?.Animator as StateMachineAnimator)?.GetCurrent();
                        if (aniName != null)
                        {
                            blocks.Add(PrepareTextLine(env.Fonts.TooltipContentFont, "action: " + aniName, ref current, Color.White, ref size.X));
                        }

                        size.Y = current.Y;
                    }
                    break;
            }

            return new TooltipContent() { blocks = blocks, size = size };
        }

        private TooltipContent DrawItem(GameTime gameTime, RenderEnv env, PortalItem item)
        {
            var blocks = new List<TextBlock>();
            Vector2 size = Vector2.Zero;
            StringResult sr = null;
            Vector2 current = Vector2.Zero;

            var sb = new StringBuilder();
            sb.Append("pName: ").AppendLine(item.PName);

            string pTypeName = GetPortalTypeString(item.Type);
            sb.Append("pType: ").Append(item.Type);
            if (pTypeName != null)
            {
                sb.Append("(").Append(pTypeName).Append(")");
            }
            sb.AppendLine();

            sb.Append("toMap: ").Append(item.ToMap);
            if (item.ToMap != null)
            {
                this.StringLinker?.StringMap.TryGetValue(item.ToMap.Value, out sr);
                string toMapName = sr?.Name;
                sb.Append("(").Append(sr?.Name ?? "null").Append(")");
            }
            sb.AppendLine();

            sb.Append("toName: ").AppendLine(item.ToName);

            if (!string.IsNullOrEmpty(item.Script))
            {
                sb.Append("script: ").AppendLine(item.Script);
            }

            sb.Length -= 2;

            blocks.Add(PrepareTextLine(env.Fonts.TooltipContentFont, sb.ToString(), ref current, Color.White, ref size.X));
            size.Y = current.Y;
            return new TooltipContent() { blocks = blocks, size = size };
        }

        private TooltipContent DrawItem(GameTime gameTime, RenderEnv env, ReactorItem item)
        {
            var blocks = new List<TextBlock>();
            Vector2 size = Vector2.Zero;
            Vector2 current = Vector2.Zero;

            var sb = new StringBuilder();
            sb.Append("ID: ").Append(item.ID).AppendLine();
            sb.Append("rName: ").AppendLine(item.ReactorName);
            sb.Append("rTime: ").Append(item.ReactorTime).AppendLine();

            sb.Append("state: ").Append(item.View.Stage);
            var ani = item.View.Animator as StateMachineAnimator;
            if (ani != null)
            {
                sb.Append(" (").Append(ani.Data.SelectedState).Append(")");
            }
            sb.AppendLine();

            sb.Length -= 2;
            blocks.Add(PrepareTextLine(env.Fonts.TooltipContentFont, sb.ToString(), ref current, Color.White, ref size.X));
            size.Y = current.Y;
            return new TooltipContent() { blocks = blocks, size = size };
        }

        private TooltipContent DrawItem(GameTime gameTime, RenderEnv env, TooltipItem item)
        {
            var blocks = new List<TextBlock>();
            Vector2 size = Vector2.Zero;
            Vector2 current = Vector2.Zero;

            if (!string.IsNullOrEmpty(item.Title))
            {
                bool hasDesc = !string.IsNullOrEmpty(item.Desc) || !string.IsNullOrEmpty(item.ItemEU);
                var titleFont = hasDesc ? env.Fonts.TooltipTitleFont : env.Fonts.TooltipContentFont;
                blocks.Add(PrepareTextLine(titleFont, item.Title, ref current, Color.White, ref size.X));
            }
            if (!string.IsNullOrEmpty(item.Desc))
            {
                blocks.Add(PrepareTextLine(env.Fonts.TooltipContentFont, item.Desc, ref current, Color.White, ref size.X));
            }
            if (!string.IsNullOrEmpty(item.ItemEU))
            {
                blocks.Add(PrepareTextLine(env.Fonts.TooltipContentFont, item.ItemEU, ref current, Color.White, ref size.X));
            }

            size.Y = current.Y;
            return new TooltipContent() { blocks = blocks, size = size };
        }

        private TooltipContent DrawItem(GameTime gameTime, RenderEnv env, UIWorldMap.Tooltip item)
        {
            var blocks = new List<TextBlock>();
            Vector2 size = Vector2.Zero;
            Vector2 current = Vector2.Zero;
            StringResult sr = null;

            if (item.MapID != null)
            {
                this.StringLinker?.StringMap.TryGetValue(item.MapID.Value, out sr);
                string title = sr != null ? string.Format("{0} : {1}", sr["streetName"], sr["mapName"]) : item.MapID.ToString();
                string desc = sr?["mapDesc"];
                var titleFont = string.IsNullOrEmpty(desc) ? env.Fonts.TooltipContentFont : env.Fonts.TooltipTitleFont;
                blocks.Add(PrepareTextLine(titleFont, title, ref current, Color.White, ref size.X));
                if (!string.IsNullOrEmpty(desc))
                {
                    blocks.AddRange(PrepareFormatText(env.Fonts.TooltipContentFont, desc, ref current, 280, ref size.X));
                }
            }

            size.Y = current.Y;
            return new TooltipContent() { blocks = blocks, size = size };
        }

        private TooltipContent DrawString(GameTime gameTime, RenderEnv env, string text)
        {
            var blocks = new List<TextBlock>();
            Vector2 size = Vector2.Zero;
            Vector2 current = Vector2.Zero;
            blocks.Add(PrepareTextLine(env.Fonts.TooltipContentFont, text, ref current, Color.White, ref size.X));
            size.Y = current.Y;
            return new TooltipContent() { blocks = blocks, size = size };
        }

        private void DrawContent(RenderEnv env, TooltipContent content, Vector2 position, bool adjustToWindow)
        {
            Vector2 padding = new Vector2(10, 8);
            Vector2 preferSize = new Vector2(
                Math.Max(content.size.X + padding.X * 2, 26),
                Math.Max(content.size.Y + padding.Y * 2, 26));

            if (adjustToWindow)
            {
                position.X = Math.Max(0, Math.Min(position.X, env.Camera.Width - preferSize.X));
                position.Y = Math.Max(0, Math.Min(position.Y, env.Camera.Height - preferSize.Y));
            }

            env.Sprite.Begin();
            var background = UIGraphics.LayoutNinePatch(this.Resource, new Point((int)preferSize.X, (int)preferSize.Y));
            foreach (var block in background)
            {
                if (block.Rectangle.Width > 0 && block.Rectangle.Height > 0 && block.Texture != null)
                {
                    var rect = new Rectangle((int)position.X + block.Rectangle.X,
                        (int)position.Y + block.Rectangle.Y,
                        block.Rectangle.Width,
                        block.Rectangle.Height);
                    env.Sprite.Draw(block.Texture, rect, Color.White);
                }
            }
            env.Sprite.Flush();

            foreach (var block in content.blocks)
            {
                var pos = new Vector2(position.X + padding.X + block.Position.X,
                    position.Y + padding.Y + block.Position.Y);

                var baseFont = block.Font.BaseFont;
          
                if (baseFont is XnaFont)
                {
                    env.Sprite.DrawStringEx((XnaFont)baseFont, block.Text, pos, block.ForeColor);
                    env.Sprite.Flush();
                }
                else if (baseFont is D2DFont)
                {
                    env.D2DRenderer.Begin();
                    env.D2DRenderer.DrawString((D2DFont)baseFont, block.Text, pos, block.ForeColor);
                    env.D2DRenderer.End();
                }
            }
            env.Sprite.End();
        }

        private struct TooltipContent
        {
            public List<TextBlock> blocks;
            public Vector2 size;
        }
    }
}
