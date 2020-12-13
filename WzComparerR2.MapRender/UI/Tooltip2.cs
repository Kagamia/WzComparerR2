using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

using WzComparerR2.WzLib;
using WzComparerR2.Common;
using WzComparerR2.Rendering;
using WzComparerR2.Animation;
using WzComparerR2.MapRender.Patches2;
using WzComparerR2.PluginBase;

using Res = CharaSimResource.Resource;
using MRes = WzComparerR2.MapRender.Properties.Resources;
using static WzComparerR2.MapRender.UI.TooltipHelper;
using TextureBlock = WzComparerR2.MapRender.UI.UIGraphics.RenderBlock<Microsoft.Xna.Framework.Graphics.Texture2D>;

namespace WzComparerR2.MapRender.UI
{
    class Tooltip2
    {
        public Tooltip2(ContentManager content)
        {
            this.Content = content;

            this.LoadContent(content);
        }

        public ContentManager Content { get; private set; }
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

        private void LoadContent(ContentManager content)
        {
            var res = new NineFormResource();
            res.N = content.Load<Texture2D>(nameof(Res.UIToolTip_img_Item_Frame2_n));
            res.NE = content.Load<Texture2D>(nameof(Res.UIToolTip_img_Item_Frame2_ne));
            res.E = content.Load<Texture2D>(nameof(Res.UIToolTip_img_Item_Frame2_e));
            res.SE = content.Load<Texture2D>(nameof(Res.UIToolTip_img_Item_Frame2_se));
            res.S = content.Load<Texture2D>(nameof(Res.UIToolTip_img_Item_Frame2_s));
            res.SW = content.Load<Texture2D>(nameof(Res.UIToolTip_img_Item_Frame2_sw));
            res.W = content.Load<Texture2D>(nameof(Res.UIToolTip_img_Item_Frame2_w));
            res.NW = content.Load<Texture2D>(nameof(Res.UIToolTip_img_Item_Frame2_nw));
            res.C = content.Load<Texture2D>(nameof(Res.UIToolTip_img_Item_Frame2_c));
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
            var textures = new List<TextureBlock>();
            Vector2 size = Vector2.Zero;
            Vector2 current = Vector2.Zero;
            StringResult sr = null;

            var spot = item.Spot;
            if (spot != null)
            {
                //计算属性要求 获取怪物列表和npc列表
                int spotBarrier = 0, spotBarrierArc = 0, spotBarrierAut = 0;
                var mobNames = new List<string>();
                var npcNames = new List<string>();
                int minLevel = 0, maxLevel = 0;

                if (!spot.NoTooltip)
                {
                    HashSet<int> mobs = new HashSet<int>();
                    HashSet<int> npcs = new HashSet<int>();
                    //TODO: caching mobs level.
                    foreach (var mapNo in spot.MapNo)
                    {
                        var mapNode = PluginManager.FindWz(string.Format("Map/Map/Map{0}/{1:D9}.img/info", mapNo / 100000000, mapNo));
                        if (mapNode != null)
                        {
                            int barrier = mapNode?.Nodes["barrier"].GetValueEx(0) ?? 0;
                            int barrierArc = mapNode?.Nodes["barrierArc"].GetValueEx(0) ?? 0;
                            int barrierAut = mapNode?.Nodes["barrierAut"].GetValueEx(0) ?? 0;
                            spotBarrier = Math.Max(spotBarrier, barrier);
                            spotBarrierArc = Math.Max(spotBarrierArc, barrierArc);
                            spotBarrierAut = Math.Max(spotBarrierAut, barrierAut);
                        }

                        var mapInfo = PluginManager.FindWz(string.Format("Etc/MapObjectInfo.img/{0}", mapNo));
                        if (mapInfo != null)
                        {
                            var mobNode = mapInfo.Nodes["mob"];
                            if (mobNode != null)
                            {
                                foreach (var valNode in mobNode.Nodes)
                                {
                                    mobs.Add(valNode.GetValue<int>());
                                }
                            }
                            var npcNode = mapInfo.Nodes["npc"];
                            if (npcNode != null)
                            {
                                foreach (var valNode in npcNode.Nodes)
                                {
                                    npcs.Add(valNode.GetValue<int>());
                                }
                            }
                        }
                    }

                    if (mobs.Count > 0)
                    {
                        foreach (var mobID in mobs)
                        {
                            this.StringLinker?.StringMob.TryGetValue(mobID, out sr);
                            var mobLevel = PluginManager.FindWz(string.Format("Mob/{0:D7}.img/info/level", mobID)).GetValueEx<int>(0);
                            string mobText = sr != null ? string.Format("{0}(Lv.{1})", sr.Name, mobLevel) : mobID.ToString();
                            mobNames.Add(mobText);
                            if (mobLevel > 0)
                            {
                                if (minLevel > 0) minLevel = Math.Min(minLevel, mobLevel);
                                else minLevel = mobLevel;
                                if (maxLevel > 0) maxLevel = Math.Max(maxLevel, mobLevel);
                                else maxLevel = mobLevel;
                            }
                        }
                        minLevel = Math.Max(10, minLevel - 3);
                        maxLevel = Math.Max(10, maxLevel - 2);
                    }
                    if (npcs.Count > 0)
                    {
                        foreach (var npcID in npcs)
                        {
                            this.StringLinker?.StringNpc.TryGetValue(npcID, out sr);
                            string npcText = sr?.Name ?? npcID.ToString();
                            npcNames.Add(npcText);
                        }
                    }
                }

                //预计算宽度
                int partWidth = 0;
                int? drawNpcColumnWidth = null;
                var font = env.Fonts.TooltipContentFont;
                if (mobNames.Count > 0 || npcNames.Count > 0)
                {
                    float mobWidth = mobNames.Count <= 0 ? 0 : mobNames.Max(text => font.MeasureString(text).X);
                    float npcWidth = npcNames.Count <= 0 ? 0 : npcNames.Max(text => font.MeasureString(text).X);
                    if (npcNames.Count > 0 && mobNames.Count + npcNames.Count > 18)
                    {
                        partWidth = (int)Math.Max(mobWidth, npcWidth * 2 + 10);
                        drawNpcColumnWidth = (int)npcWidth;
                    }
                    else
                    {
                        partWidth = (int)Math.Max(mobWidth, npcWidth);
                    }
                    partWidth += 15;
                }

                //开始绘制
                //属性要求
                List<object> part1 = null;
                float part1Width = 0;
                if (spotBarrier > 0 || spotBarrierArc > 0 || spotBarrierAut > 0)
                {
                    part1 = new List<object>();
                    Action<int, Texture2D, Color> addBarrier = (barrier, icon, foreColor) =>
                    {
                        if (icon != null)
                        {
                            var rect = new Rectangle((int)current.X, (int)current.Y + 1, icon.Width, icon.Height);
                            part1.Add(new TextureBlock(icon, rect));
                            current.X += rect.Width + 1;
                        }

                        var textBlock = PrepareTextBlock(env.Fonts.DefaultFont, barrier.ToString(), ref current, foreColor);
                        part1.Add(textBlock);
                    };

                    if (spotBarrier > 0)
                    {
                        var icon = Content.Load<Texture2D>(nameof(MRes.UIWindow_img_ToolTip_WorldMap_StarForce));
                        addBarrier(spotBarrier, icon, new Color(255, 204, 0));
                    }
                    else if (spotBarrierArc > 0)
                    {
                        var icon = Content.Load<Texture2D>(nameof(MRes.UIWindow_img_ToolTip_WorldMap_ArcaneForce));
                        addBarrier(spotBarrierArc, icon, new Color(221, 170, 255));
                    }
                    else if (spotBarrierAut > 0)
                    {
                        var icon = Content.Load<Texture2D>(nameof(MRes.UIWindow_img_ToolTip_WorldMap_AuthenticForce));
                        addBarrier(spotBarrierAut, icon, new Color(221, 170, 255));
                    }

                    part1Width = current.X;
                    size.X = Math.Max(size.X, current.X);
                    current.X = 0;
                    current.Y += 15;
                }

                //地图名称
                List<TextBlock> part2 = new List<TextBlock>();
                List<TextBlock> part2_1 = null;
                float part2Width = 0;
                {
                    int mapID = spot.MapNo[0];
                    this.StringLinker?.StringMap.TryGetValue(mapID, out sr);
                    string title = spot.Title ?? (sr != null ? string.Format("{0} : {1}", sr["streetName"], sr["mapName"]) : mapID.ToString());
                    string desc = spot.Desc ?? sr?["mapDesc"];
                    var titleFont = string.IsNullOrEmpty(desc) ? env.Fonts.TooltipContentFont : env.Fonts.TooltipTitleFont;
                    part2.Add(PrepareTextLine(titleFont, title, ref current, Color.White, ref part2Width));
                    size.X = Math.Max(size.X, part2Width);

                    if (!string.IsNullOrEmpty(desc))
                    {
                        current.Y += 2;
                        part2_1 = new List<TextBlock>();
                        int width = (int)MathHelper2.Max(280, part2Width, size.X, partWidth);
                        part2_1.AddRange(PrepareFormatText(env.Fonts.TooltipContentFont, desc, ref current, width, ref size.X));
                    }

                    current.Y += 4;
                }

                //准备分割线
                List<TextureBlock> lines = new List<TextureBlock>();
                var line = Content.Load<Texture2D>(nameof(MRes.UIWindow_img_ToolTip_WorldMap_Line));

                //绘制怪物
                List<object> part3 = null;
                if (mobNames.Count > 0)
                {
                    part3 = new List<object>();

                    //绘制分割线
                    lines.Add(new TextureBlock(line, new Rectangle(current.ToPoint(), Point.Zero)));
                    current.Y += 8;

                    //推荐等级
                    current.X = 15;
                    part3.Add(PrepareTextBlock(font,
                        string.Format("推荐等级 : Lv.{0} ~ Lv.{1}", minLevel, maxLevel),
                        ref current, new Color(119, 204, 255)));
                    size.X = Math.Max(size.X, current.X);
                    current.X = 0;
                    current.Y += 18;

                    //绘制分割线
                    lines.Add(new TextureBlock(line, new Rectangle(current.ToPoint(), Point.Zero)));
                    current.Y += 8;

                    //怪物列表
                    Texture2D icon;
                    Color color;
                    if (spotBarrier > 0 || spotBarrierArc > 0 || spotBarrierAut > 0)
                    {
                        icon = Content.Load<Texture2D>(nameof(MRes.UIWindow_img_ToolTip_WorldMap_enchantMob));
                        color = new Color(255, 0, 102);
                    }
                    else
                    {
                        icon = Content.Load<Texture2D>(nameof(MRes.UIWindow_img_ToolTip_WorldMap_Mob));
                        color = new Color(119, 255, 0);
                    }
                    part3.Add(new TextureBlock(icon, new Rectangle(0, (int)current.Y + 1, 0, 0)));
                    foreach (var mobName in mobNames)
                    {
                        part3.Add(new TextBlock()
                        {
                            Font = font,
                            Text = mobName,
                            Position = new Vector2(15, current.Y),
                            ForeColor = color
                        });
                        current.Y += 18;
                    }
                }

                //绘制npc
                List<object> part4 = null;
                if (npcNames.Count > 0)
                {
                    part4 = new List<object>();
                    //绘制分割线
                    lines.Add(new TextureBlock(line, new Rectangle(current.ToPoint(), Point.Zero)));
                    current.Y += 8;

                    //npc列表
                    Texture2D icon = Content.Load<Texture2D>(nameof(MRes.UIWindow_img_ToolTip_WorldMap_Npc));
                    Color color = new Color(119, 204, 255);
                    part4.Add(new TextureBlock(icon, new Rectangle(0, (int)current.Y + 1, 0, 0)));
                    for (int i = 0; i < npcNames.Count; i++)
                    {
                        var pos = new Vector2(15, current.Y);
                        if (i % 2 == 1 && drawNpcColumnWidth != null)
                        {
                            pos.X += 10 + drawNpcColumnWidth.Value;
                        }

                        part4.Add(new TextBlock()
                        {
                            Font = font,
                            Text = npcNames[i],
                            Position = pos,
                            ForeColor = color
                        });

                        if (i == npcNames.Count - 1 || drawNpcColumnWidth == null || i % 2 == 1)
                        {
                            current.X = 0;
                            current.Y += 18;
                        }
                    }
                }

                size.X = Math.Max(size.X, partWidth);
                current.Y -= 4;

                //合并parts
                //对part1 part2居中
                if (part1 != null)
                {
                    int offset = (int)((size.X - part1Width) / 2);
                    foreach (object obj in part1)
                    {
                        if (obj is TextBlock)
                        {
                            var tb = (TextBlock)obj;
                            tb.Position.X += offset;
                            blocks.Add(tb);
                        }
                        else if (obj is TextureBlock)
                        {
                            var tex = (TextureBlock)obj;
                            tex.Rectangle.X += offset;
                            textures.Add(tex);
                        }
                    }
                }
                if (part2 != null)
                {
                    int offset = (int)((size.X - part2Width) / 2);
                    for (int i = 0; i < part2.Count; i++)
                    {
                        var tb = part2[i];
                        tb.Position.X += offset;
                        blocks.Add(tb);
                    }
                }
                if (part2_1 != null)
                {
                    foreach (var tb in part2_1)
                    {
                        blocks.Add(tb);
                    }
                }
                if (lines != null)
                {
                    for (int i = 0; i < lines.Count; i++)
                    {
                        var tex = lines[i];
                        tex.Rectangle.Width = (int)size.X;
                        tex.Rectangle.Height = 1;
                        textures.Add(tex);
                    }
                }
                foreach (var _part in new[] { part3, part4 })
                {
                    if (_part != null)
                    {
                        foreach (object obj in _part)
                        {
                            if (obj is TextBlock)
                            {
                                var tb = (TextBlock)obj;
                                blocks.Add(tb);
                            }
                            else if (obj is TextureBlock)
                            {
                                var tex = (TextureBlock)obj;
                                textures.Add(tex);
                            }
                        }
                    }
                }
            }
            size.Y = current.Y;
            return new TooltipContent() { blocks = blocks, textures = textures, size = size };
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

            if (content.textures != null)
            {
                foreach (var block in content.textures)
                {
                    if (block.Texture != null)
                    {
                        var rect = block.Rectangle;
                        rect.X += (int)(position.X + padding.X);
                        rect.Y += (int)(position.Y + padding.Y);
                        if (rect.Width == 0) rect.Width = block.Texture.Width;
                        if (rect.Height == 0) rect.Height = block.Texture.Height;
                        env.Sprite.Draw(block.Texture, rect, Color.White);
                    }
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
            public List<TextureBlock> textures;
            public Vector2 size;
        }
    }
}
