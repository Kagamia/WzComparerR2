using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.WzLib;
using WzComparerR2.MapRender.Patches2;
using WzComparerR2.PluginBase;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WzComparerR2.Animation;


namespace WzComparerR2.MapRender
{
    public class MapData
    {
        public MapData()
        {
            this.Scene = new MapScene();
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

        public MiniMap Minimap { get; set; }
        #endregion

        #region 绘图资源
        public Texture2D MapMarkIcon { get; set; }
        #endregion 

        public MapScene Scene { get; private set; }


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

            //加载地图元件
            Wz_Node node;
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

            }
            if ((node = mapImgNode.Nodes["portal"]) != null)
            {

            }
            if ((node = mapImgNode.Nodes["ladderRope"]) != null)
            {

            }
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
                foreach(var node in objNode.Nodes)
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
                foreach(var node in tileNode.Nodes)
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

            foreach(var group in fhLayerNode.Nodes)
            {
                foreach(var node in group.Nodes)
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
            foreach(var node in lifeNode.Nodes)
            {
                var item = LifeItem.LoadFromNode(node);
                item.Name = $"life_{item.Type}_{node.Text}";
                item.Index = int.Parse(node.Text);

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
                        PreloadItemResource(resLoader, item);
                    }
                }

                foreach (var child in node.Nodes)
                {
                    loadFunc(child);
                }
            };

            loadFunc(this.Scene);
        }

        private void PreloadItemResource(ResourceLoader resLoader, SceneItem item)
        {
            if (item is BackItem)
            {
                var back = (BackItem)item;
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
            else if (item is ObjItem)
            {
                var obj = (ObjItem)item;
                string path = $@"Map\Obj\{obj.OS}.img\{obj.L0}\{obj.L1}\{obj.L2}";
                var aniItem = resLoader.LoadAnimationData(path);
                obj.View = new ObjItem.ItemView()
                {
                    Animator = CreateAnimator(aniItem)
                };
            }
            else if (item is TileItem)
            {
                var tile = (TileItem)item;
                string path = $@"Map\Tile\{tile.TS}.img\{tile.U}\{tile.No}";
                var aniItem = resLoader.LoadAnimationData(path);
                tile.View = new TileItem.ItemView()
                {
                    Animator = CreateAnimator(aniItem)
                };
            }
            else if (item is LifeItem)
            {
                var life = (LifeItem)item;
                string path;
                switch (life.Type)
                {
                    case "m":
                        path = $@"Mob\{life.ID:D7}.img";
                        var mobNode = PluginManager.FindWz(path);
                        //TODO: 加载mob数据

                        int? mobLink = mobNode.FindNodeByPath(@"info\link")?.GetValueEx<int>();
                        if (mobLink != null)
                        {
                            path = $@"Mob\{mobLink.Value:D7}.img\stand";
                        }
                        else
                        {
                            path = $@"Mob\{life.ID:D7}.img\stand";
                        }
                       
                        //加载动画
                        {
                            var aniItem = resLoader.LoadAnimationData(path);
                            if (aniItem != null)
                            {
                                life.View = new LifeItem.ItemView()
                                {
                                    Animator = CreateAnimator(aniItem)
                                };
                            }
                        }
                        break;

                    case "n":
                        path = $@"Npc\{life.ID:D7}.img";
                        var npcNode = PluginManager.FindWz(path);
                        //TODO: 加载npc数据
                        int? npcLink = npcNode.FindNodeByPath(@"info\link")?.GetValueEx<int>();
                        if (npcLink != null)
                        {
                            path = $@"Npc\{npcLink.Value:D7}.img\stand";
                        }
                        else
                        {
                            path = $@"Npc\{life.ID:D7}.img\stand";
                        }

                        //加载动画
                        {
                            var aniItem = resLoader.LoadAnimationData(path);
                            if (aniItem != null)
                            {
                                life.View = new LifeItem.ItemView()
                                {
                                    Animator = CreateAnimator(aniItem)
                                };
                            }
                        }
                        break;
                }
            }
        }

        private object CreateFSMAnimator(Wz_Node node)
        {
            return null;
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
