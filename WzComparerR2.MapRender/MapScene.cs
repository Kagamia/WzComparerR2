using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;
using WzComparerR2.MapRender.Patches;
using WzComparerR2.Common;
using WzComparerR2.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WzComparerR2.MapRender
{
    public class MapScene
    {
        public MapScene(GraphicsDevice device)
        {
            this.device = device;
            this.texLoader = new TextureLoader(device);
            this.Back = new MapSceneNode();
            this.Layer0 = new MapSceneNode();
            this.Layer1 = new MapSceneNode();
            this.Layer2 = new MapSceneNode();
            this.Layer3 = new MapSceneNode();
            this.Layer4 = new MapSceneNode();
            this.Layer5 = new MapSceneNode();
            this.Layer6 = new MapSceneNode();
            this.Layer7 = new MapSceneNode();
            this.Front = new MapSceneNode();

            this.Base = new MapSceneNode();
            this.Base.Nodes.AddRange(new[] { Back, Layer0, Layer1, Layer2, Layer3, Layer4, Layer5, Layer6, Layer7, Front });
        }

        public MapSceneNode Back { get; private set; }
        public MapSceneNode Layer0 { get; private set; }
        public MapSceneNode Layer1 { get; private set; }
        public MapSceneNode Layer2 { get; private set; }
        public MapSceneNode Layer3 { get; private set; }
        public MapSceneNode Layer4 { get; private set; }
        public MapSceneNode Layer5 { get; private set; }
        public MapSceneNode Layer6 { get; private set; }
        public MapSceneNode Layer7 { get; private set; }
        public MapSceneNode Front { get; private set; }

        public MapSceneNode Base { get; private set; }

        public void LoadMap(Wz_Image mapImg)
        {
            this.mapImg = mapImg;

            LoadBack();
        }

        private Wz_Image mapImg;
        private TextureLoader texLoader;
        private GraphicsDevice device;

        public void Update(GameTime gameTime, RenderEnv renderEnv)
        {
            UpdateNode(Base, gameTime, renderEnv);
        }

        public void Draw(GameTime gameTime, RenderEnv renderEnv)
        {
            renderEnv.Sprite.Begin(SpriteSortMode.Deferred, StateEx.NonPremultipled_Hidef());
            DrawNode(Base, gameTime, renderEnv);
            renderEnv.Sprite.End();
        }

        private void UpdateNode(MapSceneNode node, GameTime gameTime, RenderEnv renderEnv)
        {
            if (node.Patch != null)
            {
                node.Patch.Update(gameTime, renderEnv);
            }
            foreach (var child in node.Nodes)
            {
                UpdateNode(child, gameTime, renderEnv);
            }
        }

        private void DrawNode(MapSceneNode node, GameTime gameTime, RenderEnv renderEnv)
        {
            if (node.Patch != null)
            {
                node.Patch.Draw(gameTime, renderEnv);
            }
            foreach (var child in node.Nodes)
            {
                DrawNode(child, gameTime, renderEnv);
            }
        }

        private void LoadBack()
        {
            Wz_Node mapWz = PluginManager.FindWz(Wz_Type.Map);
            Dictionary<string, RenderFrame> loadedBackRes = new Dictionary<string, RenderFrame>();
            Dictionary<string, RenderFrame[]> loadedFrames = new Dictionary<string, RenderFrame[]>();
            if (mapWz == null)
                return;
            Wz_Node backLstNode = mapImg.Node.FindNodeByPath("back");
            if (backLstNode != null)
            {
                string[] path = new string[4];

                foreach (Wz_Node node in backLstNode.Nodes)
                {
                    Wz_Node x = node.FindNodeByPath("x"),
                        y = node.FindNodeByPath("y"),
                        bs = node.FindNodeByPath("bS"),
                        ani = node.FindNodeByPath("ani"),
                        no = node.FindNodeByPath("no"),
                        f = node.FindNodeByPath("f"),
                        front = node.FindNodeByPath("front"),
                        type = node.FindNodeByPath("type"),
                        cx = node.FindNodeByPath("cx"),
                        cy = node.FindNodeByPath("cy"),
                        rx = node.FindNodeByPath("rx"),
                        ry = node.FindNodeByPath("ry"),
                        a = node.FindNodeByPath("a"),
                        screenMode = node.FindNodeByPath("screenMode");

                    if (bs != null && no != null)
                    {
                        bool _ani = ani.GetValueEx<int>(0) != 0;
                        int _type = type.GetValueEx<int>(0);

                        path[0] = "Back";
                        path[1] = bs.GetValue<string>() + ".img";
                        path[2] = _ani ? "ani" : "back";
                        path[3] = no.GetValue<string>();

                        string key = string.Join("\\", path);

                        RenderFrame[] frames;
                        if (!loadedFrames.TryGetValue(key, out frames))
                        {
                            Wz_Node objResNode = mapWz.FindNodeByPath(true, path);
                            if (objResNode == null)
                                continue;
                            frames = LoadFrames(objResNode, loadedBackRes);
                            loadedFrames[key] = frames;
                        }

                        BackPatch patch = new BackPatch();
                        patch.ObjectType = front.GetValueEx<int>(0) != 0 ? RenderObjectType.Front : RenderObjectType.Back;
                        patch.Position = new Vector2(x.GetValueEx<int>(0), y.GetValueEx<int>(0));
                        patch.Cx = cx.GetValueEx<int>(0);
                        patch.Cy = cy.GetValueEx<int>(0);
                        patch.Rx = rx.GetValueEx<int>(0);
                        patch.Ry = ry.GetValueEx<int>(0);
                        patch.Frames = new RenderAnimate(frames);
                        patch.Flip = f.GetValueEx<int>(0) != 0;
                        patch.TileMode = GetBackTileMode(_type);
                        patch.Alpha = a.GetValueEx<int>(255);
                        patch.ScreenMode = screenMode.GetValueEx<int>(0);

                        patch.ZIndex[0] = (int)patch.ObjectType;
                        Int32.TryParse(node.Text, out patch.ZIndex[1]);

                        patch.Name = string.Format("back_{0}", node.Text);

                        if (patch.ObjectType == RenderObjectType.Back)
                        {
                            this.Back.Nodes.Add(patch);
                        }
                        else if (patch.ObjectType == RenderObjectType.Front)
                        {
                            this.Front.Nodes.Add(patch);
                        }
                    }
                } // end foreach

                this.Back.Nodes.Sort();
                this.Front.Nodes.Sort();
            }
        }

        private RenderFrame[] LoadFrames(Wz_Node resNode, Dictionary<string, RenderFrame> loadedRes)
        {
            if (resNode.Value is Wz_Png)
            {
                RenderFrame frame;
                if (!loadedRes.TryGetValue(resNode.FullPath, out frame))
                {
                    frame = this.texLoader.CreateFrame(resNode);
                    loadedRes[resNode.FullPath] = frame;
                }
                return new RenderFrame[1] { frame };
            }

            List<RenderFrame> frames = new List<RenderFrame>();
            Wz_Uol uol;

            for (int i = 0; ; i++)
            {
                Wz_Node frameNode = resNode.FindNodeByPath(i.ToString());
                if (frameNode == null)
                    break;
                while ((uol = frameNode.Value as Wz_Uol) != null)
                {
                    frameNode = uol.HandleUol(frameNode);
                }
                RenderFrame frame;
                if (!loadedRes.TryGetValue(frameNode.FullPath, out frame))
                {
                    frame = this.texLoader.CreateFrame(frameNode);
                    loadedRes[frameNode.FullPath] = frame;
                }
                frames.Add(frame);
            }

            return frames.ToArray();
        }

        private TileMode GetBackTileMode(int type)
        {
            switch (type)
            {
                case 0: return TileMode.None;
                case 1: return TileMode.Horizontal;
                case 2: return TileMode.Vertical;
                case 3: return TileMode.BothTile;
                case 4: return TileMode.Horizontal | TileMode.ScrollHorizontial;
                case 5: return TileMode.Vertical | TileMode.ScrollVertical;
                case 6: return TileMode.BothTile | TileMode.ScrollHorizontial;
                case 7: return TileMode.BothTile | TileMode.ScrollVertical;
                default: return TileMode.None;
            }
        }
    }

    public class MapSceneNode
    {
        public MapSceneNode()
        {
            this.Nodes = new MapSceneNodeCollection(this);
        }

        public MapSceneNodeCollection Nodes { get; private set; }
        public MapSceneNode Parent { get; internal set; }
        public RenderPatch Patch { get; set; }
    }

    public class MapSceneNodeCollection : IList<MapSceneNode>
    {
        internal MapSceneNodeCollection(MapSceneNode owner)
        {
            this.owner = owner;
            this.innerList = new List<MapSceneNode>();
        }
        private MapSceneNode owner;
        private List<MapSceneNode> innerList;

        public int IndexOf(MapSceneNode item)
        {
            return this.innerList.IndexOf(item);
        }

        public void Insert(int index, MapSceneNode item)
        {
            if (item.Parent != null)
            {
                throw new ArgumentException("item already has parent.");
            }
            item.Parent = this.owner;
            this.innerList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            this[index].Parent = null;
            this.innerList.RemoveAt(index);
        }

        public MapSceneNode this[int index]
        {
            get
            {
                return this.innerList[index];
            }
            set
            {
                this.innerList[index] = value;
            }
        }

        public void Add(MapSceneNode item)
        {
            if (item.Parent != null)
            {
                throw new ArgumentException("item already has parent.");
            }
            item.Parent = this.owner;
            this.innerList.Add(item);
        }

        public void Add(RenderPatch patch)
        {
            MapSceneNode node = new MapSceneNode() { Patch = patch };
            this.Add(node);
        }

        public void AddRange(IEnumerable<MapSceneNode> nodes)
        {
            foreach (var node in nodes)
            {
                this.Add(node);
            }
        }

        public void Sort()
        {
            this.innerList.Sort((a, b) => {
                if (a.Patch != null && b.Patch !=null)
                {
                    return RenderPatchComarison(a.Patch, b.Patch);
                }
                else if (a.Patch == null)
                {
                    return b.Patch == null ? 0 : -1;
                }
                else
                {
                    return 1;
                }
            });
        }

        private int RenderPatchComarison(RenderPatch a, RenderPatch b)
        {
            for (int i = 0; i < a.ZIndex.Length; i++)
            {
                int dz = a.ZIndex[i].CompareTo(b.ZIndex[i]);
                if (dz != 0)
                    return dz;
            }
            return ((int)a.ObjectType).CompareTo((int)b.ObjectType);
        }

        public void Clear()
        {
            this.innerList.ForEach(item => item.Parent = null);
            this.innerList.Clear();
        }

        public bool Contains(MapSceneNode item)
        {
            return this.innerList.Contains(item);
        }

        public int Count
        {
            get { return this.innerList.Count; }
        }

        public bool Remove(MapSceneNode item)
        {
            bool success = this.innerList.Remove(item);
            if (success)
            {
                item.Parent = null;
            }
            return success;
        }

        public IEnumerator<MapSceneNode> GetEnumerator()
        {
            return this.innerList.GetEnumerator();
        }

        bool ICollection<MapSceneNode>.IsReadOnly
        {
            get { return false; }
        }

        void ICollection<MapSceneNode>.CopyTo(MapSceneNode[] array, int arrayIndex)
        {
            this.innerList.CopyTo(array, arrayIndex);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.innerList.GetEnumerator();
        }
    }
}
