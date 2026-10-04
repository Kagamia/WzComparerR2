using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using WzComparerR2.CharaSim;
using WzComparerR2.Common;
using WzComparerR2.WzLib;
using static WzComparerR2.CharaSimControl.RenderHelper;
using CharaSimResource;
using System.Linq;

namespace WzComparerR2.CharaSimControl
{
    public class MapTooltipRenderer : TooltipRender
    {
        public MapTooltipRenderer()
        {
        }

        public Map Map { get; set; }
        public bool ShowMiniMap { get; set; }
        public bool ShowMiniMapMob { get; set; }
        public bool ShowMiniMapNpc { get; set; }
        public bool ShowMiniMapPortal { get; set; }

        public override object TargetItem
        {
            get
            {
                return this.Map;
            }
            set
            {
                this.Map = value as Map;
            }
        }

        public override Bitmap Render()
        {
            if (this.Map == null)
            {
                return null;
            }
            using Bitmap bmp = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            using Graphics g = Graphics.FromImage(bmp);

            List<TextBlock> titleBlocks = new List<TextBlock>();
            List<TextBlock> mobBlocks = new List<TextBlock>();
            List<TextBlock> npcBlocks = new List<TextBlock>();
            TextBlock barrierBlock = null;
            TextBlock descBlock = null;
            TextBlock bgmBlock = null;
            int[] barrierResourceWidth = [17, 18, 18];
            int barrierType = 0;
            int picY = 0;

            if (Map.MapID > -1)
            {
                string[] mapName = GetMapName(Map.MapID, Map.Link);
                var block = PrepareText(g, mapName[0], GearGraphics.ItemDetailFont, Brushes.White, 0, picY);
                titleBlocks.Add(block);
                int tw = block.Size.Width;

                block = PrepareText(g, mapName[1], GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 18);
                titleBlocks.Add(block);
                tw = Math.Max(block.Size.Width, tw);

                block = PrepareText(g, $"ID:{Map.MapID}", GearGraphics.ItemDetailFont, Brushes.White, tw + 8, picY);
                titleBlocks.Add(block);
            }

            if (Map.Barrier > 0)
            {
                barrierType = 1;
                barrierBlock = PrepareText(g, Map.Barrier.ToString(), GearGraphics.ItemReqLevelFont, GearGraphics.Equip22BrushEmphasis, 0, 0);
            }
            else if (Map.BarrierArc > 0)
            {
                barrierType = 2;
                barrierBlock = PrepareText(g, Map.BarrierArc.ToString(), GearGraphics.ItemReqLevelFont, GearGraphics.BarrierArcBrush, 0, 0);
            }
            else if (Map.BarrierAut > 0)
            {
                barrierType = 3;
                barrierBlock = PrepareText(g, Map.BarrierAut.ToString(), GearGraphics.ItemReqLevelFont, GearGraphics.BarrierAutBrush, 0, 0);
            }

            var mapDesc = GetMapDesc(Map.MapID, Map.Link);
            if (!string.IsNullOrEmpty(mapDesc))
            {
                var block = PrepareText(g, mapDesc, GearGraphics.ItemDetailFont, Brushes.White, 0, 0);
                descBlock = block;
            }

            if (!string.IsNullOrEmpty(Map.Bgm))
            {
                bgmBlock = PrepareText(g, Map.Bgm, GearGraphics.ItemDetailFont, GearGraphics.MapBgmBrush, 0, 0);
            }

            if (Map.Mobs.Count > 0)
            {
                picY = 0;
                foreach (var mob in Map.Mobs)
                {
                    string mobName = GetMobName(mob);
                    var mobLevel = PluginBase.PluginManager.FindWz(@$"Mob\{mob:D7}.img\info\level").GetValueEx<int?>(null);
                    var block = PrepareText(g, (mobName ?? "(null)") + ((mobLevel != null) ? $"(Lv.{mobLevel})" : ""), GearGraphics.ItemDetailFont, GearGraphics.BlockRedBrush, 0, picY);
                    mobBlocks.Add(block);
                    picY += 18;
                }
            }

            if (Map.Npcs.Count > 0)
            {
                picY = 0;
                foreach (var npc in Map.Npcs)
                {
                    string npcName = GetNpcName(npc);
                    var block = PrepareText(g, npcName ?? "(null)", GearGraphics.ItemDetailFont, GearGraphics.Equip22BrushRare, 0, picY);
                    npcBlocks.Add(block);
                    picY += 18;
                }
            }

            Bitmap mapMark = null;
            if (!string.IsNullOrEmpty(Map.MapMark))
            {
                var mapMarkNode = PluginBase.PluginManager.FindWz(@$"Map\MapHelper.img\mark\{Map.MapMark}");
                if (mapMarkNode != null)
                {
                    mapMark = BitmapOrigin.CreateFromNode(mapMarkNode, PluginBase.PluginManager.FindWz).Bitmap;
                }
            }

            Bitmap miniMap = null;
            if (ShowMiniMap && Map.MiniMapNode != null)
            {
                miniMap = BitmapOrigin.CreateFromNode(Map.MiniMapNode.FindNodeByPath("canvas"), PluginBase.PluginManager.FindWz).Bitmap;
            }

            Rectangle barrierRect = barrierBlock?.Rectangle ?? new Rectangle();
            Rectangle markRect = new Rectangle(0, 0, mapMark?.Width ?? 0, mapMark?.Height ?? 0);
            Rectangle titleRect = Measure(titleBlocks);
            Rectangle miniMapRect = new Rectangle(0, 0, miniMap?.Width ?? 0, miniMap?.Height ?? 0);
            Rectangle descRect = descBlock?.Rectangle ?? new Rectangle();
            Rectangle bgmRect = bgmBlock?.Rectangle ?? new Rectangle();
            Rectangle mobRect = Measure(mobBlocks);
            Rectangle npcRect = Measure(npcBlocks);

            int width = 0;
            width = (new[] { miniMapRect.Width, markRect.Width + 5 + titleRect.Width, bgmRect.Width + 21, mobRect.Width + 21, npcRect.Width + 21 }).Max();
            if (!descRect.IsEmpty)
                width = Math.Max(width, 300);
            if (!markRect.IsEmpty)
                titleRect.X = markRect.Width + 5;
            miniMapRect.X = (width - miniMapRect.Width) / 2;

            int barrierResourceX = 0;
            if (!barrierRect.IsEmpty)
            {
                barrierResourceX = (width - (barrierResourceWidth[barrierType - 1] + 2 + barrierRect.Width)) / 2;
                barrierRect.X = barrierResourceX + barrierResourceWidth[barrierType - 1] + 2;
            }

            int titleHeight = Math.Max(markRect.Height, titleRect.Height);
            markRect.Y = barrierRect.Height + (titleHeight - markRect.Height) / 2;
            titleRect.Y = barrierRect.Height + (titleHeight - titleRect.Height) / 2;

            if (titleHeight != 0)
                titleHeight += 6;
            if (!miniMapRect.IsEmpty)
                miniMapRect.Height += 6;
            if (!descRect.IsEmpty)
                descRect.Height += 6;
            if (!bgmRect.IsEmpty)
                bgmRect.Height = 14 + 6;
            if (!mobRect.IsEmpty)
                mobRect.Height = mobBlocks.Count * 18 - 4 + 6;
            if (!npcRect.IsEmpty)
                npcRect.Height = npcBlocks.Count * 18 - 4 + 6;

            miniMapRect.Y = barrierRect.Height + titleHeight;
            descRect.Y = miniMapRect.Y + miniMapRect.Height;
            bgmRect.Y = descRect.Y + descRect.Height;
            mobRect.Y = bgmRect.Y + bgmRect.Height;
            npcRect.Y = mobRect.Y + mobRect.Height;

            int height = barrierRect.Height + titleHeight + miniMapRect.Height + descRect.Height + bgmRect.Height + mobRect.Height + npcRect.Height;

            Bitmap bmp2 = new Bitmap(width + 20, height + 20);
            Graphics g2 = Graphics.FromImage(bmp2);
            barrierRect.Offset(10, 10);
            markRect.Offset(10, 10);
            titleRect.Offset(10, 10);
            miniMapRect.Offset(10, 10);
            descRect.Offset(10, 10);
            bgmRect.Offset(31, 12);
            mobRect.Offset(31, 12);
            npcRect.Offset(31, 12);

            if (barrierBlock != null)
            {
                switch (barrierType)
                {
                    case 1:
                        g2.DrawImage(Resource.UIWindow_img_ToolTip_WorldMap_StarForce, barrierResourceX + 10, 10);
                        break;
                    case 2:
                        g2.DrawImage(Resource.UIWindow_img_ToolTip_WorldMap_ArcaneForce, barrierResourceX + 10, 10);
                        break;
                    case 3:
                        g2.DrawImage(Resource.UIWindow_img_ToolTip_WorldMap_AuthenticForce, barrierResourceX + 10, 10);
                        break;
                }
                DrawText(g2, barrierBlock, barrierRect.Location);
            }
            if (mapMark != null)
            {
                g2.DrawImage(mapMark, markRect.X, markRect.Y);
                mapMark.Dispose();
            }
            foreach (var item in titleBlocks)
            {
                DrawText(g2, item, titleRect.Location);
            }

            if (miniMap != null)
            {
                var dx = (bmp2.Width - miniMap.Width - 2) / 2;
                miniMap = DrawMinimapIcons(miniMap, dx);
                g2.DrawImage(miniMap, miniMapRect.X - dx, miniMapRect.Y);
                miniMap.Dispose();
            }

            if (descBlock != null)
            {
                DrawMultilineText(g2, descBlock, descRect.Location, bmp2.Width - 15, 18, out int offsetY);

                // 비트맵 크기 조절
                if (offsetY > 0)
                {
                    Bitmap bmp3 = new Bitmap(bmp2.Width, bmp2.Height + offsetY);
                    Graphics g3 = Graphics.FromImage(bmp3);
                    g3.DrawImage(bmp2, 0, 0);
                    DrawMultilineText(g3, descBlock, descRect.Location, bmp2.Width - 15, 18, out offsetY); // redraw

                    bmp2.Dispose();
                    g2.Dispose();
                    bmp2 = bmp3;
                    g2 = g3;
                }

                bgmRect.Offset(0, offsetY);
                mobRect.Offset(0, offsetY);
                npcRect.Offset(0, offsetY);
            }

            if (bgmBlock != null)
            {
                g2.DrawImage(Resource.UIWindow_img_ToolTip_WorldMap_Custom0, bgmRect.X - 21, bgmRect.Y - 2);
                DrawText(g2, bgmBlock, bgmRect.Location);
            }

            if (mobBlocks.Count > 0)
            {
                g2.DrawImage(Resource.UIWindow_img_ToolTip_WorldMap_Mob, mobRect.X - 21, mobRect.Y - 2);
            }
            foreach (var item in mobBlocks)
            {
                DrawText(g2, item, mobRect.Location);
            }

            if (npcBlocks.Count > 0)
            {
                g2.DrawImage(Resource.UIWindow_img_ToolTip_WorldMap_Npc, npcRect.X - 21, npcRect.Y - 2);
            }
            foreach (var item in npcBlocks)
            {
                DrawText(g2, item, npcRect.Location);
            }

            // 배경 채워넣기
            Bitmap bmpResult = new Bitmap(bmp2.Width, bmp2.Height);
            using Graphics gResult = Graphics.FromImage(bmpResult);
            GearGraphics.DrawNewTooltipBack(gResult, 0, 0, bmpResult.Width, bmpResult.Height);
            gResult.DrawImage(bmp2, 0, 0);

            bmp2.Dispose();
            g2.Dispose();

            return bmpResult;
        }

        private Bitmap DrawMinimapIcons(Bitmap miniMap, int dx)
        {
            Bitmap bmp = new Bitmap(miniMap.Width + dx * 2, miniMap.Height);
            using Graphics g = Graphics.FromImage(bmp);
            g.DrawImage(miniMap, dx, 0);
            Image image = null;

            if (ShowMiniMapMob)
            {
                foreach (var mob in this.Map.MiniMapMobs)
                {
                    image = Resource.MapHelper_img_minimap_mob_1;
                    g.DrawImage(image, TranslateMiniMapIcon(new Point(mob.X, mob.Y), miniMap.Size, image.Size, dx));
                }
            }

            if (ShowMiniMapNpc)
            {
                // 0 : npc
                // 1 : shop
                // 2 : event npc
                // 3 : transport
                // 4 : trunk
                foreach (var npc in this.Map.MiniMapNpcs)
                {
                    switch (npc.Type)
                    {
                        case 0:
                            image = Resource.MapHelper_img_minimap_npc;
                            break;
                        case 1:
                            image = Resource.MapHelper_img_minimap_shop;
                            break;
                        case 2:
                            image = Resource.MapHelper_img_minimap_eventnpc;
                            break;
                        case 3:
                            image = Resource.MapHelper_img_minimap_transport;
                            break;
                        case 4:
                            image = Resource.MapHelper_img_minimap_trunk;
                            break;
                    }
                    g.DrawImage(image, TranslateMiniMapIcon(new Point(npc.X, npc.Y), miniMap.Size, image.Size, dx));
                }
            }

            if (ShowMiniMapPortal)
            {
                // 0 : portal
                // 1 : enchant portal
                // 2 : blink portal
                // 3 : hidden portal
                foreach (var portal in this.Map.MiniMapPortals)
                {
                    switch (portal.Type)
                    {
                        case 0:
                            image = Resource.MapHelper_img_minimap_portal;
                            break;
                        case 1:
                            image = Resource.MapHelper_img_minimap_enchantportal;
                            break;
                        case 2:
                            image = Resource.MapHelper_img_minimap_arrowup;
                            break;
                        case 3:
                            image = Resource.MapHelper_img_minimap_hiddenportal;
                            break;
                    }
                    g.DrawImage(image, TranslateMiniMapIcon(new Point(portal.X, portal.Y), miniMap.Size, image.Size, dx));
                }

                foreach (var illuminantCluster in this.Map.MiniMapIlluminantClusters)
                {
                    image = Resource.MapHelper_img_minimap_cluster;
                    g.DrawImage(image, TranslateMiniMapIcon(new Point(illuminantCluster.X, illuminantCluster.Y), miniMap.Size, image.Size, dx));
                }
            }

            miniMap.Dispose();
            return bmp;
        }

        private Point TranslateMiniMapIcon(Point item, Size minimap, Size texture, int offset)
        {
            if (this.Map.MiniMapWidth != 0 && this.Map.MiniMapHeight != 0)
            {
                var x = (float)minimap.Width / this.Map.MiniMapWidth * (item.X + this.Map.MiniMapCenterX) - (texture.Width / 2);
                var y = (float)minimap.Height / this.Map.MiniMapHeight * (item.Y + this.Map.MiniMapCenterY) - (texture.Height / 2) - 5;

                return new Point((int)x + offset, (int)y);
            }
            else return (new Point(-100, -100));
        }

        private string[] GetMapName(int mapID, int? linkID)
        {
            string[] ret = ["(null)", "(null)"];
            StringResult sr;
            if (this.StringLinker == null || !this.StringLinker.StringMap.TryGetValue(mapID, out sr))
            {
                if (!this.StringLinker.StringMap.TryGetValue(linkID ?? -1, out sr))
                {
                    return ret;
                }
            }

            ret[0] = sr["streetName"] ?? "(null)";
            ret[1] = sr["mapName"] ?? "(null)";
            return ret;
        }

        private string GetMapDesc(int mapID, int? linkID)
        {
            string ret = null;
            StringResult sr;
            if (this.StringLinker == null || !this.StringLinker.StringMap.TryGetValue(mapID, out sr))
            {
                if (!this.StringLinker.StringMap.TryGetValue(linkID ?? -1, out sr))
                {
                    return ret;
                }
            }

            ret = sr["mapDesc"];
            return ret;
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

        private string GetNpcName(int npcID)
        {
            StringResult sr;
            if (this.StringLinker == null || !this.StringLinker.StringNpc.TryGetValue(npcID, out sr))
            {
                return null;
            }
            return sr.Name;
        }
    }
}