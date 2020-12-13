using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using WzComparerR2.MapRender.Patches2;
using WzComparerR2.PluginBase;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WzComparerR2.Animation;


namespace WzComparerR2.MapRender
{
    public class MapData
    {
        public MapData(IRandom random)
        {
            this.Scene = new MapScene();
            this.MiniMap = new MiniMap();
            this.Tooltips = new List<TooltipItem>();
            this.Date = DateTime.Now;

            this.random = random;
        }

        #region 基本信息
        public int? ID { get; set; }
        public string Name { get; set; }
        public int? Link { get; set; }
        public Rectangle VRect { get; set; }
        public string MapMark { get; set; }
        public string Bgm { get; set; }

        public bool IsTown { get; set; }
        public bool CanFly { get; set; }
        public bool CanSwim { get; set; }
        public int? ReturnMap { get; set; }
        public bool HideMinimap { get; set; }
        public int FieldLimit { get; set; }

        public MiniMap MiniMap { get; private set; }
        #endregion

        public MapScene Scene { get; private set; }
        public IList<TooltipItem> Tooltips { get; private set; }
        public DateTime Date { get; set; }

        private readonly IRandom random;

        public void Load(Wz_Node mapImgNode, ResourceLoader resLoader)
        {
            var infoNode = mapImgNode.Nodes["info"];
            if (infoNode == null)
            {
                throw new Exception("Cannot find map info node.");
            }

            //试图读取ID
            LoadIDOrName(mapImgNode);
            //加载基本信息
            LoadInfo(infoNode);
            //读取link
            if (this.Link != null && !FindMapByID(this.Link.Value, out mapImgNode))
            {
                throw new Exception("Cannot find or extract map link node.");
            }

            //加载小地图
            Wz_Node node;
            if (!string.IsNullOrEmpty(this.MapMark))
            {
                node = PluginManager.FindWz("Map\\MapHelper.img\\mark\\" + this.MapMark)?.GetLinkedSourceNode(PluginManager.FindWz);
                if (node != null)
                {
                    node = node.GetLinkedSourceNode(PluginManager.FindWz);
                    this.MiniMap.MapMark = resLoader.Load<Texture2D>(node);
                }
            }
            if ((node = mapImgNode.Nodes["miniMap"]) != null)
            {
                LoadMinimap(node, resLoader);
            }

            //加载地图元件
            if ((node = mapImgNode.Nodes["back"]) != null)
            {
                LoadBack(node);
            }
            for (int i = 0; i <= 7; i++)
            {
                if ((node = mapImgNode.Nodes[i.ToString()]) != null)
                {
                    LoadLayer(node, i);
                }
            }
            if ((node = mapImgNode.Nodes["foothold"]) != null)
            {
                for (int i = 0; i <= 7; i++)
                {
                    var fhLevel = node.Nodes[i.ToString()];
                    if (fhLevel != null)
                    {
                        LoadFoothold(fhLevel, i);
                    }
                }
            }
            if ((node = mapImgNode.Nodes["life"]) != null)
            {
                LoadLife(node);
            }
            if ((node = mapImgNode.Nodes["reactor"]) != null)
            {
                LoadReactor(node);
            }
            if ((node = mapImgNode.Nodes["portal"]) != null)
            {
                LoadPortal(node);
            }
            if ((node = mapImgNode.Nodes["ladderRope"]) != null)
            {
                LoadLadderRope(node);
            }
            if ((node = mapImgNode.Nodes["skyWhale"]) != null)
            {
                LoadSkyWhale(node);
            }
            if ((node = mapImgNode.Nodes["ToolTip"]) != null)
            {
                LoadTooltip(node);
            }
            if ((node = mapImgNode.Nodes["particle"]) != null)
            {
                LoadParticle(node);
            }

            //计算地图大小
            CalcMapSize();
        }

        private void LoadIDOrName(Wz_Node mapImgNode)
        {
            var m = Regex.Match(mapImgNode.Text, @"(\d{9})\.img");
            if (m.Success)
            {
                this.ID = int.Parse(m.Result("$1"));
            }
            else
            {
                this.Name = mapImgNode.Text;
            }
        }

        private void LoadInfo(Wz_Node infoNode)
        {
            int l = infoNode.Nodes["VRLeft"].GetValueEx(0),
                t = infoNode.Nodes["VRTop"].GetValueEx(0),
                r = infoNode.Nodes["VRRight"].GetValueEx(0),
                b = infoNode.Nodes["VRBottom"].GetValueEx(0);
            this.VRect = new Rectangle(l, t, r - l, b - t);
            this.Bgm = infoNode.Nodes["bgm"].GetValueEx<string>(null);
            this.Link = infoNode.Nodes["link"].GetValueEx<int>();
            this.MapMark = infoNode.Nodes["mapMark"].GetValueEx<string>(null);

            this.IsTown = infoNode.Nodes["town"].GetValueEx(false);
            this.CanFly = infoNode.Nodes["fly"].GetValueEx(false);
            this.CanSwim = infoNode.Nodes["swim"].GetValueEx(false);
            this.ReturnMap = infoNode.Nodes["returnMap"].GetValueEx<int>();
            this.HideMinimap = infoNode.Nodes["hideMinimap"].GetValueEx(false);
            this.FieldLimit = infoNode.Nodes["fieldLimit"].GetValueEx(0);
        }

        private void LoadMinimap(Wz_Node miniMapNode, ResourceLoader resLoader)
        {
            Wz_Node canvas = miniMapNode.FindNodeByPath("canvas"),
                   width = miniMapNode.FindNodeByPath("width"),
                   height = miniMapNode.FindNodeByPath("height"),
                   centerX = miniMapNode.FindNodeByPath("centerX"),
                   centerY = miniMapNode.FindNodeByPath("centerY"),
                   mag = miniMapNode.FindNodeByPath("mag");

            canvas = canvas.GetLinkedSourceNode(PluginManager.FindWz);

            if (canvas != null)
            {
                this.MiniMap.Canvas = resLoader.Load<Texture2D>(canvas);
            }
            this.MiniMap.Width = width.GetValueEx(0);
            this.MiniMap.Height = height.GetValueEx(0);
            this.MiniMap.CenterX = centerX.GetValueEx(0);
            this.MiniMap.CenterY = centerY.GetValueEx(0);
            this.MiniMap.Mag = mag.GetValueEx(0);
        }

        private void LoadBack(Wz_Node backNode)
        {
            foreach (var node in backNode.Nodes)
            {
                var item = BackItem.LoadFromNode(node);
                item.Name = $"back_{node.Text}";
                item.Index = int.Parse(node.Text);

                (item.IsFront ? this.Scene.Front : this.Scene.Back).Slots.Add(item);
            }
        }

        private void LoadLayer(Wz_Node layerNode, int level)
        {
            var layerSceneNode = (LayerNode)this.Scene.Layers.Nodes[level];

            //读取obj
            var objNode = layerNode.Nodes["obj"];
            if (objNode != null)
            {
                foreach (var node in objNode.Nodes)
                {
                    var item = ObjItem.LoadFromNode(node);
                    item.Name = $"obj_{level}_{node.Text}";
                    item.Index = int.Parse(node.Text);

                    layerSceneNode.Obj.Slots.Add(item);
                }
            }

            //读取tile
            string tS = layerNode.Nodes["info"]?.Nodes["tS"].GetValueEx<string>(null);
            var tileNode = layerNode.Nodes["tile"];
            if (tS != null && tileNode != null)
            {
                foreach (var node in tileNode.Nodes)
                {
                    var item = TileItem.LoadFromNode(node);
                    item.TS = tS;
                    item.Name = $"tile_{level}_{node.Text}";
                    item.Index = int.Parse(node.Text);

                    layerSceneNode.Tile.Slots.Add(item);
                }
            }
        }

        private void LoadFoothold(Wz_Node fhLayerNode, int level)
        {
            var layerSceneNode = (LayerNode)this.Scene.Layers.Nodes[level];

            foreach (var group in fhLayerNode.Nodes)
            {
                foreach (var node in group.Nodes)
                {
                    var item = FootholdItem.LoadFromNode(node);
                    item.ID = int.Parse(node.Text);
                    item.Name = $"fh_{level}_{group.Text}_{node.Text}";

                    var fhSceneNode = new ContainerNode<FootholdItem>() { Item = item };
                    layerSceneNode.Foothold.Nodes.Add(fhSceneNode);
                }
            }
        }

        private void LoadLife(Wz_Node lifeNode)
        {
            bool isCategory = lifeNode.Nodes["isCategory"].GetValueEx<int>(0) != 0;
            var lifeNodeList = !isCategory ? lifeNode.Nodes : lifeNode.Nodes.SelectMany(n => n.Nodes);

            int i = 0;
            foreach (var node in lifeNodeList)
            {
                var item = LifeItem.LoadFromNode(node);
                if (isCategory)
                {
                    item.Name = $"life_{item.Type}_{node.ParentNode.Text}_{node.Text}";
                    item.Index = i++;
                }
                else
                {
                    item.Name = $"life_{item.Type}_{node.Text}";
                    item.Index = int.Parse(node.Text);
                }

                if (item.Type == LifeItem.LifeType.Npc)
                {
                    var npcNode = PluginManager.FindWz(string.Format("Npc/{0:D7}.img/info", item.ID));
                    if ((npcNode?.Nodes["hide"].GetValueEx(0) ?? 0) != 0)
                    {
                        continue;
                    }
                }

                //直接绑定foothold
                ContainerNode<FootholdItem> fhNode;
                if (item.Fh != 0 && (fhNode = FindFootholdByID(item.Fh)) != null)
                {
                    fhNode.Slots.Add(item);
                }
                else
                {
                    Scene.Fly.Sky.Slots.Add(item);
                }
            }
        }

        private void LoadPortal(Wz_Node portalNode)
        {
            var portalTooltipNode = PluginManager.FindWz("String/ToolTipHelp.img/PortalTooltip/" + this.ID);

            foreach (var node in portalNode.Nodes)
            {
                var item = PortalItem.LoadFromNode(node);
                item.Name = $"portal_{node.Text}";
                item.Index = int.Parse(node.Text);

                //加载tooltip
                if (portalTooltipNode != null && !string.IsNullOrEmpty(item.PName))
                {
                    var tooltipNode = portalTooltipNode.Nodes[item.PName];
                    if (tooltipNode != null)
                    {
                        var tooltip = new PortalItem.ItemTooltip();

                        if (tooltipNode.Nodes.Count > 0)
                        {
                            tooltip.Title = tooltipNode.Nodes["Title"].GetValueEx<string>(null);
                        }
                        else
                        {
                            tooltip.Title = tooltipNode.GetValue<String>();
                        }

                        item.Tooltip = tooltip;
                    }
                }

                Scene.Fly.Portal.Slots.Add(item);
            }
        }

        private void LoadReactor(Wz_Node reactorNode)
        {
            //计算reactor所在层
            var layer = Scene.Layers.Nodes.OfType<LayerNode>()
                .FirstOrDefault(l => l.Foothold.Nodes.Count > 0)
                ?? (Scene.Layers.Nodes[0] as LayerNode);

            foreach (var node in reactorNode.Nodes)
            {
                var item = ReactorItem.LoadFromNode(node);
                item.Name = $"reactor_{node.Text}";
                item.Index = int.Parse(node.Text);

                layer.Reactor.Slots.Add(item);
            }
        }

        private void LoadLadderRope(Wz_Node ladderRopeNode)
        {
            foreach (var node in ladderRopeNode.Nodes)
            {
                var item = LadderRopeItem.LoadFromNode(node);
                item.Name = $"ladderRope_{node.Text}";
                item.Index = int.Parse(node.Text);

                Scene.Fly.LadderRope.Slots.Add(item);
            }
        }

        private void LoadSkyWhale(Wz_Node skyWhaleNode)
        {
            foreach (var node in skyWhaleNode.Nodes)
            {
                var item = SkyWhaleItem.LoadFromNode(node);
                item.Name = node.Text;
                Scene.Fly.SkyWhale.Slots.Add(item);
            }
        }

        private void LoadTooltip(Wz_Node tooltipNode)
        {
            Func<Wz_Node, Rectangle> getRect = (node) =>
            {
                int x1 = node.Nodes["x1"].GetValueEx<int>(0);
                int x2 = node.Nodes["x2"].GetValueEx<int>(0);
                int y1 = node.Nodes["y1"].GetValueEx<int>(0);
                int y2 = node.Nodes["y2"].GetValueEx<int>(0);
                return new Rectangle(x1, y1, x2 - x1, y2 - y1);
            };

            var tooltipDescNode = PluginManager.FindWz("String/ToolTipHelp.img/Mapobject/" + this.ID);

            for (int i = 0; ; i++)
            {
                var rectNode = tooltipNode.Nodes[i.ToString()];
                var charNode = tooltipNode.Nodes[i + "char"];
                if (rectNode != null)
                {
                    var item = new TooltipItem();
                    item.Name = i.ToString();
                    item.Index = i;
                    item.Rect = getRect(rectNode);
                    if (charNode != null)
                    {
                        item.CharRect = getRect(charNode);
                    }

                    var descNode = tooltipDescNode?.Nodes[i.ToString()];
                    if (descNode != null)
                    {
                        item.Title = descNode.Nodes["Title"].GetValueEx<string>(null);
                        item.Desc = descNode.Nodes["Desc"].GetValueEx<string>(null);
                        item.ItemEU = descNode.Nodes["ItemEU"].GetValueEx<string>(null);
                    }

                    this.Tooltips.Add(item);
                }
                else
                {
                    break;
                }
            }
        }

        private void LoadParticle(Wz_Node node)
        {
            foreach (var particleNode in node.Nodes)
            {
                var item = ParticleItem.LoadFromNode(particleNode);
                item.Name = node.Text;
                Scene.Effect.Slots.Add(item);
            }
        }

        private void CalcMapSize()
        {
            if (!this.VRect.IsEmpty)
            {
                return;
            }

            var rect = Rectangle.Empty;

            int xMAX = int.MinValue;
            foreach (LayerNode layer in this.Scene.Layers.Nodes)
            {
                foreach (ContainerNode<FootholdItem> item in layer.Foothold.Nodes)
                {
                    var fh = item.Item;
                    var fhRect = new Rectangle(
                        Math.Min(fh.X1, fh.X2),
                        Math.Min(fh.Y1, fh.Y2),
                        Math.Abs(fh.X2 - fh.X1),
                        Math.Abs(fh.Y2 - fh.Y1));
                    xMAX = Math.Max(fhRect.Right, xMAX);
                    var oldrec = rect;
                    if (rect.IsEmpty)
                    {
                        rect = fhRect;
                    }
                    else
                    {
                        Rectangle newRect;
                        Rectangle.Union(ref rect, ref fhRect, out newRect);
                        rect = newRect;
                    }
                }
            }

            rect.Y -= 250;
            rect.Height += 450;

            foreach (LadderRopeItem item in this.Scene.Fly.LadderRope.Slots)
            {
                var lrRect = new Rectangle(item.X, Math.Min(item.Y1, item.Y2), 1, Math.Abs(item.Y2 - item.Y1));
                if (rect.IsEmpty)
                {
                    rect = lrRect;
                }
                else
                {
                    Rectangle newRect;
                    Rectangle.Union(ref rect, ref lrRect, out newRect);
                    rect = newRect;
                }
            }

            this.VRect = rect;
        }

        private ContainerNode<FootholdItem> FindFootholdByID(int fhID)
        {
            return this.Scene.Layers.Nodes.OfType<LayerNode>()
                .SelectMany(layerNode => layerNode.Foothold.Nodes).OfType<ContainerNode<FootholdItem>>()
                .FirstOrDefault(fhNode => fhNode.Item.ID == fhID);
        }

        /// <summary>
        /// 对场景中所有的物件预加载动画资源。
        /// </summary>
        /// <param name="resLoader"></param>
        public void PreloadResource(ResourceLoader resLoader)
        {
            Action<SceneNode> loadFunc = null;
            loadFunc = (node) =>
            {
                var container = node as ContainerNode;
                if (container != null)
                {
                    foreach (var item in container.Slots)
                    {
                        if (item is BackItem)
                        {
                            PreloadResource(resLoader, (BackItem)item);
                        }
                        else if (item is ObjItem)
                        {
                            PreloadResource(resLoader, (ObjItem)item);
                        }
                        else if (item is TileItem)
                        {
                            PreloadResource(resLoader, (TileItem)item);
                        }
                        else if (item is LifeItem)
                        {
                            PreloadResource(resLoader, (LifeItem)item);
                        }
                        else if (item is PortalItem)
                        {
                            PreloadResource(resLoader, (PortalItem)item);
                        }
                        else if (item is ReactorItem)
                        {
                            PreloadResource(resLoader, (ReactorItem)item);
                        }
                        else if (item is ParticleItem)
                        {
                            PreloadResource(resLoader, (ParticleItem)item);
                        }
                    }
                }

                foreach (var child in node.Nodes)
                {
                    loadFunc(child);
                }
            };

            loadFunc(this.Scene);
        }

        private void PreloadResource(ResourceLoader resLoader, BackItem back)
        {
            string aniDir;
            switch (back.Ani)
            {
                case 0: aniDir = "back"; break;
                case 1: aniDir = "ani"; break;
                case 2: aniDir = "spine"; break;
                default: throw new Exception($"Unknown back ani value: {back.Ani}.");
            }
            string path = $@"Map\Back\{back.BS}.img\{aniDir}\{back.No}";
            var aniItem = resLoader.LoadAnimationData(path);

            back.View = new BackItem.ItemView()
            {
                Animator = CreateAnimator(aniItem, back.SpineAni)
            };
        }

        private void PreloadResource(ResourceLoader resLoader, ObjItem obj)
        {
            string path = $@"Map\Obj\{obj.OS}.img\{obj.L0}\{obj.L1}\{obj.L2}";
            var aniItem = resLoader.LoadAnimationData(path);
            obj.View = new ObjItem.ItemView()
            {
                Animator = CreateAnimator(aniItem)
            };
        }

        private void PreloadResource(ResourceLoader resLoader, TileItem tile)
        {
            string path = $@"Map\Tile\{tile.TS}.img\{tile.U}\{tile.No}";
            var aniItem = resLoader.LoadAnimationData(path);
            tile.View = new TileItem.ItemView()
            {
                Animator = CreateAnimator(aniItem)
            };
        }

        private void PreloadResource(ResourceLoader resLoader, LifeItem life)
        {
            string path;
            if (life.Hide)
            {
                life.View = new LifeItem.ItemView();
                return;
            }
            switch (life.Type)
            {
                case LifeItem.LifeType.Mob:
                    path = $@"Mob\{life.ID:D7}.img";
                    var mobNode = PluginManager.FindWz(path);

                    //加载mob数据
                    if (mobNode != null)
                    {
                        life.LifeInfo = LifeInfo.CreateFromNode(mobNode);
                    }

                    //获取link
                    int? mobLink = mobNode?.FindNodeByPath(@"info\link").GetValueEx<int>();
                    if (mobLink != null)
                    {
                        path = $@"Mob\{mobLink.Value:D7}.img";
                        mobNode = PluginManager.FindWz(path);
                    }

                    //加载动画
                    if (mobNode != null)
                    {
                        var aniItem = this.CreateSMAnimator(mobNode, resLoader);
                        if (aniItem != null)
                        {
                            AddMobAI(aniItem);
                            life.View = new LifeItem.ItemView()
                            {
                                Animator = aniItem
                            };
                        }
                    }
                    break;

                case LifeItem.LifeType.Npc:
                    path = $@"Npc\{life.ID:D7}.img";
                    var npcNode = PluginManager.FindWz(path);

                    //TODO: 加载npc数据
                    int? npcLink = npcNode?.FindNodeByPath(@"info\link").GetValueEx<int>();
                    if (npcLink != null)
                    {
                        path = $@"Npc\{npcLink.Value:D7}.img";
                        npcNode = PluginManager.FindWz(path);
                    }

                    //加载动画
                    if (npcNode != null)
                    {
                        var aniItem = this.CreateSMAnimator(npcNode, resLoader);
                        if (aniItem != null)
                        {
                            AddNpcAI(aniItem);
                            life.View = new LifeItem.ItemView()
                            {
                                Animator = aniItem
                            };
                        }
                    }
                    break;
            }

            if (life.View == null) //空动画
            {
                life.View = new LifeItem.ItemView();
            }
        }

        private void PreloadResource(ResourceLoader resLoader, PortalItem portal)
        {
            string path;

            var view = new PortalItem.ItemView();
            //加载editor
            {
                var typeName = PortalItem.PortalTypes[portal.Type];
                path = $@"Map\MapHelper.img\portal\editor\{typeName}";
                var aniData = resLoader.LoadAnimationData(path);
                if (aniData != null)
                {
                    view.EditorAnimator = CreateAnimator(aniData);
                }

            }
            //加载动画
            {
                string typeName, imgName;
                switch (portal.Type)
                {
                    case 7:
                        typeName = PortalItem.PortalTypes[2]; break;
                    default:
                        typeName = PortalItem.PortalTypes[portal.Type]; break;
                }

                switch (portal.Image)
                {
                    case 0:
                        imgName = "default"; break;
                    default:
                        imgName = portal.Image.ToString(); break;
                }
                path = $@"Map\MapHelper.img\portal\game\{typeName}\{imgName}";

                var aniNode = PluginManager.FindWz(path);
                if (aniNode != null)
                {
                    bool useParts = new[] { "portalStart", "portalContinue", "portalExit" }
                        .Any(aniName => aniNode.Nodes[aniName] != null);

                    if (useParts) //加载动作动画
                    {
                        var animator = CreateSMAnimator(aniNode, resLoader);
                        view.Animator = animator;
                        view.Controller = new PortalItem.Controller(view);
                    }
                    else //加载普通动画
                    {
                        var aniData = resLoader.LoadAnimationData(aniNode);
                        if (aniData != null)
                        {
                            view.Animator = CreateAnimator(aniData);
                        }
                    }
                }
            }

            portal.View = view;
        }

        private void PreloadResource(ResourceLoader resLoader, ReactorItem reactor)
        {
            string path = $@"Reactor\{reactor.ID:D7}.img";
            var reactorNode = PluginManager.FindWz(path);

            int? reactorLink = reactorNode?.FindNodeByPath(@"info\link").GetValueEx<int>();
            if (reactorLink != null)
            {
                path = $@"Reactor\{reactorLink.Value:D7}.img";
                reactorNode = PluginManager.FindWz(path);
            }

            //加载动画
            var aniData = new Dictionary<string, RepeatableFrameAnimationData>();

            Wz_Node frameNode;
            for (int i = 0; (frameNode = reactorNode.Nodes[i.ToString()]) != null; i++)
            {
                //加载循环动画
                var ani = resLoader.LoadAnimationData(frameNode) as RepeatableFrameAnimationData;
                if (ani != null)
                {
                    var ani2 = new RepeatableFrameAnimationData(ani.Frames);
                    ani2.Repeat = ani.Repeat ?? true; //默认循环
                    aniData.Add(i.ToString(), ani2);
                }

                //加载跳转动画
                var hitNode = frameNode.Nodes["hit"];
                Wz_Uol uol;
                if ((uol = hitNode?.GetValue<Wz_Uol>()) != null)
                {
                    hitNode = uol.HandleUol(hitNode);
                }
                if (hitNode != null)
                {
                    var aniHit = resLoader.LoadAnimationData(hitNode) as RepeatableFrameAnimationData;
                    aniData.Add($@"{i}/hit", aniHit);
                }
            }

            var view = new ReactorItem.ItemView();
            view.Animator = new StateMachineAnimator(aniData);
            view.Controller = new ReactorItem.Controller(view);

            reactor.View = view;
        }

        private void PreloadResource(ResourceLoader resLoader, ParticleItem particle)
        {
            string path = $@"Effect\particle.img\{particle.ParticleName}";
            var particleNode = PluginManager.FindWz(path);

            if (particleNode == null)
            {
                return;
            }

            var desc = resLoader.LoadParticleDesc(particleNode);
            var pSystem = new ParticleSystem(this.random);
            pSystem.LoadDescription(desc);

            for (int i = 0; i < particle.SubItems.Length; i++)
            {
                var subItem = particle.SubItems[i];
                if (subItem.Quest.Exists(quest => !resLoader.PatchVisibility.IsVisible(quest.Item1, quest.Item2)))
                {
                    continue;
                }
                var pGroup = pSystem.CreateGroup(i.ToString());
                pGroup.Position = new Vector2(subItem.X, subItem.Y);
                pGroup.Active();
                pSystem.Groups.Add(pGroup);
            }

            if (pSystem.Groups.Count == 0)
            {
                pSystem = new ParticleSystem(this.random);
            }

            particle.View = new ParticleItem.ItemView()
            {
                ParticleSystem = pSystem
            };
        }

        private StateMachineAnimator CreateSMAnimator(Wz_Node node, ResourceLoader resLoader)
        {
            var aniData = new Dictionary<string, RepeatableFrameAnimationData>();
            foreach (var actionNode in node.Nodes)
            {
                var actName = actionNode.Text;
                if (actName != "info" && !actName.StartsWith("condition"))
                {
                    var ani = resLoader.LoadAnimationData(actionNode) as RepeatableFrameAnimationData;
                    if (ani != null)
                    {
                        aniData.Add(actName, ani);
                    }
                }
            }
            long date = Int64.Parse(Date.ToString("yyyyMMddHHmm"));
            foreach (var conditionNode in node.Nodes.Where(n => n.Text.StartsWith("condition")))
            {
                if ((conditionNode.Nodes.Any(n => n.Text.All(char.IsDigit)) && conditionNode.Nodes.Where(n => n.Text.All(char.IsDigit)).All(n => resLoader.PatchVisibility.IsVisibleExact(int.Parse(n.Text), Convert.ToInt32(n.Value)))) || (conditionNode.Nodes["dateStart"].GetValueEx<long>(0) <= date && date <= conditionNode.Nodes["dateEnd"].GetValueEx<long>(0)))
                {
                    aniData.Clear();
                    foreach (var conditionedActionNode in conditionNode.Nodes)
                    {
                        var conditionedActName = conditionedActionNode.Text;
                        if (conditionedActName != "dateStart" && conditionedActName != "dateEnd")
                        {
                            var ani = resLoader.LoadAnimationData(conditionedActionNode) as RepeatableFrameAnimationData;
                            if (ani != null)
                            {
                                aniData.Add(conditionNode.Text + "/" + conditionedActName, ani);
                            }
                        }
                    }
                    break;
                }
            }
            if (aniData.Count > 0)
            {
                return new StateMachineAnimator(aniData);
            }
            else
            {
                return null;
            }
        }

        private object CreateAnimator(object animationData, string aniName = null)
        {
            if (animationData is RepeatableFrameAnimationData)
            {
                var aniData = (RepeatableFrameAnimationData)animationData;
                var animator = new RepeatableFrameAnimator(aniData);
                return animator;
            }
            else if (animationData is FrameAnimationData)
            {
                var aniData = (FrameAnimationData)animationData;
                var animator = new FrameAnimator(aniData);
                return animator;
            }
            else if (animationData is SpineAnimationData)
            {
                var aniData = (SpineAnimationData)animationData;
                var animator = new SpineAnimator(aniData);
                if (aniName != null)
                {
                    animator.SelectedAnimationName = aniName;
                }
                return animator;
            }
            return null;
        }

        private void AddMobAI(StateMachineAnimator ani)
        {
            ani.AnimationEnd += (o, e) =>
            {
                if (e.CurrentState == "regen" && ani.Data.States.Contains("stand"))
                {
                    e.NextState = "stand";
                }
            };
        }

        private void AddNpcAI(StateMachineAnimator ani)
        {
            var actions = new[] { "stand", "say", "mouse", "move", "hand", "laugh", "eye" };
            var availActions = ani.Data.States.Where(act => !act.EndsWith("_old") && Array.Exists(actions, acts => act.Contains(acts))).ToArray();
            if (availActions.Length > 0)
            {
                ani.AnimationEnd += (o, e) =>
                {
                    e.NextState = availActions[this.random.Next(availActions.Length)];
                };
            }
        }

        public static bool FindMapByID(int mapID, out Wz_Node mapImgNode)
        {
            string fullPath = string.Format(@"Map\Map\Map{0}\{1:D9}.img", (mapID / 100000000), mapID);
            mapImgNode = PluginManager.FindWz(fullPath);
            Wz_Image mapImg;
            if (mapImgNode != null
                && (mapImg = mapImgNode.GetValueEx<Wz_Image>(null)) != null
                && mapImg.TryExtract())
            {
                mapImgNode = mapImg.Node;
                return true;
            }
            else
            {
                mapImgNode = null;
                return false;
            }
        }
    }
}
