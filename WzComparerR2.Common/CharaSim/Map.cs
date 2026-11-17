using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Map
    {
        public Map()
        {
            this.Mobs = new List<int>();
            this.Npcs = new List<int>();
            this.MiniMapNpcs = new List<MiniMapIcon>();
            this.MiniMapMobs = new List<MiniMapIcon>();
            this.MiniMapPortals = new List<MiniMapIcon>();
            this.MiniMapIlluminantClusters = new List<MiniMapIcon>();
        }

        public int MapID { get; set; }
        public int? Link { get; set; }
        public int Barrier { get; set; }
        public int BarrierArc { get; set; }
        public int BarrierAut { get; set; }
        public int MiniMapWidth { get; set; }
        public int MiniMapHeight { get; set; }
        public int MiniMapCenterX { get; set; }
        public int MiniMapCenterY { get; set; }
        public string MapMark { get; set; }
        public string Bgm { get; set; }
        public Wz_Node MiniMapNode { get; set; }
        public List<int> Mobs { get; set; }
        public List<int> Npcs { get; set; }
        public List<MiniMapIcon> MiniMapNpcs { get; set; }
        public List<MiniMapIcon> MiniMapMobs { get; set; }
        public List<MiniMapIcon> MiniMapPortals { get; set; }
        public List<MiniMapIcon> MiniMapIlluminantClusters { get; set; }

        public static Map CreateFromNode(Wz_Node node, GlobalFindNodeFunction findNode)
        {
            if (node == null) return null;

            Map map = new Map();
            int mapID;
            Match m = Regex.Match(node.Text, @"^(\d{9})\.img$");
            if (!(m.Success && Int32.TryParse(m.Result("$1"), out mapID)))
            {
                return null;
            }
            map.MapID = mapID;

            Wz_Node infoNode = node.FindNodeByPath("info").ResolveUol();
            if (infoNode != null)
            {
                foreach (var propNode in infoNode.Nodes)
                {
                    switch (propNode.Text)
                    {
                        case "mapMark": map.MapMark = propNode.GetValueEx<string>(null); break;
                        case "link": map.Link = propNode.GetValueEx<int?>(null); break;
                        case "barrier": map.Barrier = propNode.GetValueEx<int>(0); break;
                        case "barrierArc": map.BarrierArc = propNode.GetValueEx<int>(0); break;
                        case "barrierAut": map.BarrierAut = propNode.GetValueEx<int>(0); break;
                        case "bgm": map.Bgm = propNode.GetValueEx<string>(null); break;
                    }
                }

                int flyingMob = infoNode.FindNodeByPath(@"AFKmob\flyingMob").ResolveUol().GetValueEx<int>(0);
                if (flyingMob != 0)
                {
                    map.Mobs.Add(flyingMob);
                }
            }

            Wz_Node linkNode = null;
            if (map.Link != null && findNode != null)
            {
                linkNode = findNode(@$"Map\Map\Map{map.Link / 100000000}\{map.Link:d9}.img");
            }
            if (linkNode == null)
            {
                linkNode = node;
            }

            Wz_Node miniMapNode = linkNode.FindNodeByPath("miniMap").ResolveUol();
            map.MiniMapNode = miniMapNode;
            if (miniMapNode != null)
            {
                map.MiniMapWidth = miniMapNode.FindNodeByPath("width").GetValueEx<int>(0);
                map.MiniMapHeight = miniMapNode.FindNodeByPath("height").GetValueEx<int>(0);
                map.MiniMapCenterX = miniMapNode.FindNodeByPath("centerX").GetValueEx<int>(0);
                map.MiniMapCenterY = miniMapNode.FindNodeByPath("centerY").GetValueEx<int>(0);
            }

            var mapInfo = findNode?.Invoke($"Etc/MapObjectInfo.img/{map.MapID}");
            bool mapObjectInfoReadied = false;
            if (mapInfo != null)
            {
                var mobNode = mapInfo.Nodes["mob"];
                if (mobNode != null)
                {
                    foreach (var valNode in mobNode.Nodes)
                    {
                        map.Mobs.Add(valNode.GetValue<int>());
                    }
                }
                var npcNode = mapInfo.Nodes["npc"];
                if (npcNode != null)
                {
                    foreach (var valNode in npcNode.Nodes)
                    {
                        map.Npcs.Add(valNode.GetValue<int>());
                    }
                }
                mapObjectInfoReadied = true;
            }

            Wz_Node lifeNode = linkNode.FindNodeByPath("life").ResolveUol();
            Dictionary<int, bool> mobCanFlyDict = new Dictionary<int, bool>();
            if (lifeNode != null)
            {
                foreach (var life in lifeNode.Nodes)
                {
                    var lifeType = life.FindNodeByPath("type").GetValueEx<string>(null);
                    var lifeId = life.FindNodeByPath("id").GetValueEx<int>(0);
                    if (lifeId != 0)
                    {
                        switch (lifeType)
                        {
                            case "n":
                                if (!mapObjectInfoReadied && !map.Npcs.Contains(lifeId))
                                {
                                    map.Npcs.Add(lifeId);
                                }
                                MiniMapIcon npcItem = new MiniMapIcon();
                                npcItem.X = life.FindNodeByPath("x").GetValueEx<int>(0);
                                npcItem.Y = life.FindNodeByPath("y").GetValueEx<int>(0);

                                // 0 : npc
                                // 1 : shop
                                // 2 : event npc
                                // 3 : transport
                                // 4 : trunk
                                var npcNode = findNode?.Invoke($"Npc/{lifeId:D7}.img/info");
                                if (npcNode != null)
                                {
                                    if (npcNode.FindNodeByPath("shop").GetValueEx<int>(0) != 0 || npcNode.FindNodeByPath("miniMapType").GetValueEx<int>(0) == 1)
                                    {
                                        npcItem.Type = 1;
                                    }
                                    else if (npcNode.FindNodeByPath("miniMapType").GetValueEx<int>(0) == 2)
                                    {
                                        npcItem.Type = 2;
                                    }
                                    else if (npcNode.FindNodeByPath("miniMapType").GetValueEx<int>(0) == 3)
                                    {
                                        npcItem.Type = 3;
                                    }
                                    else if (npcNode.FindNodeByPath("trunkPut").GetValueEx<int>(-1) != -1)
                                    {
                                        npcItem.Type = 4;
                                    }
                                }

                                var hide = (life.FindNodeByPath("hide").GetValueEx<int>(0) != 0 || (npcNode?.FindNodeByPath("hide").GetValueEx<int>(0) ?? 0) != 0);
                                if (!hide)
                                    map.MiniMapNpcs.Add(npcItem);
                                break;

                            case "m":
                                if (!mapObjectInfoReadied && !map.Mobs.Contains(lifeId))
                                {
                                    map.Mobs.Add(lifeId);
                                }
                                MiniMapIcon mobItem = new MiniMapIcon();
                                bool canFly = false;
                                if (mobCanFlyDict.ContainsKey(lifeId))
                                {
                                    canFly = mobCanFlyDict[lifeId];
                                }
                                else
                                {
                                    var mobNode = findNode?.Invoke($"Mob\\{lifeId:d7}.img");
                                    if (mobNode != null && mobNode.FindNodeByPath("fly") != null)
                                    {
                                        canFly = true;
                                    }
                                    mobCanFlyDict.Add(lifeId, canFly);
                                }
                                mobItem.X = life.FindNodeByPath("x").GetValueEx<int>(0);
                                mobItem.Y = life.FindNodeByPath(canFly ? "y" : "cy").GetValueEx<int>(0);
                                map.MiniMapMobs.Add(mobItem);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            Wz_Node portalNode = linkNode.FindNodeByPath("portal").ResolveUol();
            if (portalNode != null)
            {
                foreach (var portal in portalNode.Nodes)
                {
                    MiniMapIcon portalItem = new MiniMapIcon();
                    portalItem.X = portal.FindNodeByPath("x").GetValueEx<int>(0);
                    portalItem.Y = portal.FindNodeByPath("y").GetValueEx<int>(0);
                    var pt = portal.FindNodeByPath("pt").GetValueEx<int>(-1);
                    switch (pt)
                    {
                        // 0 : portal
                        // 1 : enchant portal
                        // 2 : blink portal
                        // 3 : hidden portal
                        case 2:
                        case 7:
                            if (portal.FindNodeByPath("enchantPortal").GetValueEx<int>(0) != 0)
                                portalItem.Type = 1;
                            else
                                portalItem.Type = 0;
                            map.MiniMapPortals.Add(portalItem);
                            break;
                        case 10:
                            var toMap = portal.FindNodeByPath("tm").GetValueEx<int>(0);
                            var toName = portal.FindNodeByPath("tn").GetValueEx<string>(null);
                            if (toMap == map.MapID || (toMap == 999999999 && !string.IsNullOrEmpty(toName)))
                                portalItem.Type = 2;
                            else
                                portalItem.Type = 3;
                            map.MiniMapPortals.Add(portalItem);
                            break;
                        case 8:
                        case 11:
                            portalItem.Type = 3;
                            if (portal.FindNodeByPath("shownAtMinimap").GetValueEx<int>(0) != 0)
                                map.MiniMapPortals.Add(portalItem);
                            break;
                    }
                }
            }

            Wz_Node illuminantClusterNode = linkNode.FindNodeByPath("illuminantCluster").ResolveUol();
            if (illuminantClusterNode != null)
            {
                foreach (var illuminantCluster in illuminantClusterNode.Nodes)
                {
                    MiniMapIcon illuminantClusterItem = new MiniMapIcon();
                    var start = illuminantCluster.FindNodeByPath("start").GetValueEx<Wz_Vector>(null);
                    if (start != null)
                    {
                        illuminantClusterItem.X = start.X;
                        illuminantClusterItem.Y = start.Y;
                        map.MiniMapIlluminantClusters.Add(illuminantClusterItem);
                    }
                }
            }

            return map;
        }

        public class MiniMapIcon
        {
            public MiniMapIcon()
            {

            }

            public int X { get; set; }
            public int Y { get; set; }
            public int Type { get; set; }
        }
    }
}