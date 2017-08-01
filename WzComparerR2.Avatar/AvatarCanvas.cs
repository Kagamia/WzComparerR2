using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Drawing;
using WzComparerR2.WzLib;
using WzComparerR2.CharaSim;

namespace WzComparerR2.Avatar
{
    public class AvatarCanvas
    {
        public AvatarCanvas()
        {
            this.ZMap = new List<string>();
            this.Actions = new List<Action>();
            this.Emotions = new List<string>();
            this.TamingActions = new List<string>();
            this.Parts = new AvatarPart[18];
            this.EarType = 0;
            this.WeaponIndex = 0;
        }

        public List<string> ZMap { get; private set; }
        public List<Action> Actions { get; private set; }
        public List<string> Emotions { get; private set; }
        public List<string> TamingActions { get; private set; }

        public AvatarPart[] Parts { get; private set; }
        public string ActionName { get; set; }
        public string EmotionName { get; set; }
        public string TamingActionName { get; set; }

        public bool HairCover { get; set; }
        public bool ShowHairShade { get; set; }
        
        public int WeaponIndex { get; set; }
        public int WeaponType { get; set; }
        public int EarType { get; set; }

        public bool LoadZ()
        {
            return LoadZ(PluginBase.PluginManager.FindWz("Base\\zmap.img"));
        }

        public bool LoadZ(Wz_Node zMapNode)
        {
            if (zMapNode == null)
            {
                return false;
            }

            this.ZMap.Clear();
            this.ZMap.Capacity = zMapNode.Nodes.Count;

            //读取z层顺序
            foreach (Wz_Node node in zMapNode.Nodes)
            {
                this.ZMap.Add(node.Text);
            }

            return true;
        }

        /// <summary>
        /// 从00002000.img中加载全部动作名称。
        /// </summary>
        /// <returns></returns>
        public bool LoadActions()
        {
            Wz_Node bodyNode = PluginBase.PluginManager.FindWz("Character\\00002000.img");
            if (bodyNode == null)
            {
                return false;
            }

            this.Actions.Clear();

            foreach (Wz_Node actionNode in bodyNode.Nodes)
            {
                if (actionNode.Text != "info")
                {
                    var action = LoadAction(actionNode);
                    this.Actions.AddRange(action);
                }
            }

            for (int i = 0; i < this.Actions.Count; i++)
            {
                this.Actions[i].Order = i;
            }

            this.Actions.Sort((a0, a1) =>
            {
                int comp = a0.Level.CompareTo(a1.Level);
                if (comp == 0)
                {
                    if (a0.Level == 0) //基础动作
                    {
                        int idx0 = Array.IndexOf(baseActions, a0.Name),
                            idx1 = Array.IndexOf(baseActions, a1.Name);
                        comp = idx0.CompareTo(idx1);
                    }
                    else
                    {
                        comp = a0.Order.CompareTo(a1.Order);
                    }
                }
                return comp;
            });

            return true;
        }

        /// <summary>
        /// 从00020000.img中加载表情名称。
        /// </summary>
        /// <returns></returns>
        public bool LoadEmotions()
        {
            Wz_Node faceNode = this.Face != null? this.Face.Node : PluginBase.PluginManager.FindWz("Character\\Face\\00020000.img");
            if (faceNode == null)
            {
                return false;
            }

            this.Emotions.Clear();

            foreach (Wz_Node emotionNode in faceNode.Nodes)
            {
                if (emotionNode.Text != "info")
                {
                    this.Emotions.Add(emotionNode.Text);
                }
            }

            return true;
        }

        public bool LoadTamingActions()
        {
            this.TamingActions.Clear();

            Wz_Node tamingNode = this.Taming == null ? null : this.Taming.Node;

            if (tamingNode == null)
            {
                return false;
            }

            foreach (Wz_Node actionNode in tamingNode.Nodes)
            {
                if (actionNode.Text != "info")
                {
                    this.TamingActions.Add(actionNode.Text);
                }
            }

            return true;
        }

        public List<int> GetCashWeaponTypes()
        {
            List<int> types = new List<int>();
            if (this.Weapon != null && this.Weapon.ID != null && Gear.GetGearType(this.Weapon.ID.Value) == GearType.cashWeapon)
            {
                foreach (var node in this.Weapon.Node.Nodes)
                {
                    int typeID;
                    if (Int32.TryParse(node.Text, out typeID))
                    {
                        types.Add(typeID);
                    }
                }
            }
            types.Sort();
            return types;
        }

        private IEnumerable<Action> LoadAction(Wz_Node actionNode)
        {
            if (actionNode.FindNodeByPath("0") != null)
            {
                var action = LoadActionFromNode(actionNode, actionNode.Text);
                if (action != null)
                {
                    action.Name = actionNode.Text;
                    yield return action;
                }
            }
            else
            {
                for (int i = 1; ; i++)
                {
                    var subActionNode = actionNode.FindNodeByPath(i.ToString());
                    if (subActionNode == null)
                    {
                        break;
                    }

                    var action = LoadActionFromNode(subActionNode, actionNode.Text);
                    if (action != null)
                    {
                        action.Name = actionNode.Text + "\\" + i;
                        yield return action;
                    }
                }
            }
        }

        private Action LoadActionFromNode(Wz_Node actionNode, string actionName)
        {
            Action act = new Action();
            act.Name = actionName;

            if (BaseActions.Contains(actionName)) //基础动作
            {
                act.Level = 0;
            }
            else
            {
                Wz_Node frameNode = actionNode.FindNodeByPath("0");
                if (frameNode == null) //有鬼
                {
                    return null;
                }
                if (frameNode.FindNodeByPath("action") != null
                    && frameNode.FindNodeByPath("frame") != null) //引用动作
                {
                    act.Level = 2;
                }
                else //当成扩展动作
                {
                    act.Level = 1;
                }
            }

            return act;
        }

        public AvatarPart AddPart(Wz_Node imgNode)
        {
            Wz_Node infoNode = imgNode.FindNodeByPath("info");
            if (infoNode == null)
            {
                return null;
            }
            AvatarPart part = new AvatarPart(imgNode);

            var gearType = Gear.GetGearType(part.ID.Value);
            switch (gearType)
            {
                case GearType.body: this.Body = part; break;
                case GearType.head: this.Head = part; break;
                case GearType.face: this.Face = part; break;
                case GearType.hair:
                case GearType.hair2: this.Hair = part; break;
                case GearType.cap: this.Cap = part; break;
                case GearType.coat: this.Coat = part; break;
                case GearType.longcoat: this.Longcoat = part; break;
                case GearType.pants: this.Pants = part; break;
                case GearType.shoes: this.Shoes = part; break;
                case GearType.glove: this.Glove = part; break;
                case GearType.shield:
                case GearType.demonShield:
                case GearType.soulShield:
                case GearType.katara: this.SubWeapon = part; break;
                case GearType.cape: this.Cape = part; break;
                case GearType.shovel:
                case GearType.pickaxe:
                case GearType.cashWeapon: this.Weapon = part; break;
                case GearType.earrings: this.Earrings = part; break;
                case GearType.faceAccessory: this.FaceAccessory = part; break;
                case GearType.eyeAccessory: this.EyeAccessory = part; break;
                case GearType.taming:
                case GearType.taming2:
                case GearType.taming3:
                case GearType.tamingChair: this.Taming = part; break;
                case GearType.saddle: this.Saddle = part; break;
                default:
                    if (Gear.IsWeapon(gearType))
                    {
                        this.Weapon = part;
                    }
                    break;
            }

            return part;
        }

        /// <summary>
        /// 获取角色动作的动画帧。
        /// </summary>
        public ActionFrame[] GetActionFrames(string actionName)
        {
            Action action = this.Actions.Find(act => act.Name == actionName);
            Wz_Node bodyNode = PluginBase.PluginManager.FindWz("Character\\00002000.img");
            if (action == null)
            {
                return new ActionFrame[0];
            }
            else if (action.Level == 2)
            {
                var actionNode = bodyNode.FindNodeByPath(action.Name);
                if (actionNode == null)
                {
                    return new ActionFrame[0];
                }

                List<ActionFrame> frames = new List<ActionFrame>();
                for (int i = 0; ; i++)
                {
                    var frameNode = actionNode.FindNodeByPath(i.ToString());
                    if (frameNode == null)
                    {
                        break;
                    }
                    ActionFrame frame = new ActionFrame();
                    frame.Action = frameNode.FindNodeByPath("action").GetValueEx<string>(null);
                    frame.Frame = frameNode.FindNodeByPath("frame").GetValueEx<int>(0);
                    LoadActionFrameDesc(frameNode, frame);
                    frames.Add(frame);
                }
                return frames.ToArray();
            }
            else
            {
                Wz_Node actionNode = null;
                if (this.Body != null)
                {
                    actionNode = this.Body.Node.FindNodeByPath(action.Name);
                }
                if (actionNode == null)
                {
                    actionNode = bodyNode.FindNodeByPath(action.Name);
                }

                List<ActionFrame> frames = new List<ActionFrame>();
                frames.AddRange(LoadStandardFrames(actionNode, action.Name));
                return frames.ToArray();
            }
        }

        public ActionFrame[] GetFaceFrames(string action)
        {
            List<ActionFrame> frames = new List<ActionFrame>();
            if (this.Face != null)
            {
                var actionNode = this.Face.Node.FindNodeByPath(action);
                if (action == "default")
                {
                    frames.Add(new ActionFrame() { Action = action });
                }
                else
                {
                    frames.AddRange(LoadStandardFrames(actionNode, action));
                }
            }
            return frames.ToArray();
        }

        public ActionFrame[] GetTamingFrames(string action)
        {
            List<ActionFrame> frames = new List<ActionFrame>();
            if (this.Taming != null)
            {
                var actionNode = this.Taming.Node.FindNodeByPath(action);
                frames.AddRange(LoadStandardFrames(actionNode, action));
            }
            return frames.ToArray();
        }

        /// <summary>
        /// 读取扩展属性。
        /// </summary>
        private void LoadActionFrameDesc(Wz_Node frameNode, ActionFrame actionFrame)
        {
            actionFrame.Delay = frameNode.FindNodeByPath("delay").GetValueEx<int>(100);
            actionFrame.Flip = frameNode.FindNodeByPath("flip").GetValueEx<int>(0) != 0;
            var faceNode = frameNode.FindNodeByPath("face");
            if (faceNode != null)
            {
                actionFrame.Face = faceNode.GetValue<int>() != 0;
            }
            var move = frameNode.FindNodeByPath("move").GetValueEx<Wz_Vector>(null);
            if (move != null)
            {
                actionFrame.Move = move;
            }
            actionFrame.RotateProp = frameNode.FindNodeByPath("rotateProp").GetValueEx<int>(0);
            actionFrame.Rotate = frameNode.FindNodeByPath("rotate").GetValueEx<int>(0);
        }

        private IEnumerable<ActionFrame> LoadStandardFrames(Wz_Node actionNode, string action)
        {
            if (actionNode == null)
            {
                yield break;
            }

            for (int i = 0; ; i++)
            {
                var frameNode = actionNode.FindNodeByPath(i.ToString());
                if (frameNode == null)
                {
                    yield break;
                }
                ActionFrame frame = new ActionFrame();
                frame.Action = action;
                frame.Frame = i;
                while (frameNode.Value is Wz_Uol)
                {
                    frameNode = frameNode.GetValue<Wz_Uol>().HandleUol(frameNode);
                }
                LoadActionFrameDesc(frameNode, frame);
                yield return frame;
            }
        }

        /// <summary>
        /// 计算角色骨骼层次结构。
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public Bone CreateFrame(int bodyFrame, int faceFrame, int tamingFrame)
        {
            ActionFrame[] frames;
            frames = GetActionFrames(this.ActionName);
            ActionFrame bodyAction = (bodyFrame > -1 && bodyFrame < frames.Length) ? frames[bodyFrame] : null;

            frames = GetFaceFrames(this.EmotionName);
            ActionFrame faceAction = (faceFrame > -1 && faceFrame < frames.Length) ? frames[faceFrame] : null;

            frames = GetTamingFrames(this.TamingActionName);
            ActionFrame tamingAction = (tamingFrame > -1 && tamingFrame < frames.Length) ? frames[tamingFrame] : null;

            return CreateFrame(bodyAction, faceAction, tamingAction);
        }

        public Bone CreateFrame(ActionFrame bodyAction, ActionFrame faceAction, ActionFrame tamingAction)
        {
            //获取所有部件
            Wz_Node[] playerNodes = LinkPlayerParts(bodyAction, faceAction);
            Wz_Node[] tamingNodes = LinkTamingParts(tamingAction);

            //根骨骼 作为角色原点
            Bone bodyRoot = new Bone("@root");
            bodyRoot.Position = Point.Empty;
            CreateBone(bodyRoot, playerNodes, bodyAction.Face);

            
            if (tamingNodes != null && tamingNodes.Length > 0)
            {
                //骑宠骨骼
                Bone tamingRoot = new Bone("@root2");
                tamingRoot.Position = Point.Empty;
                CreateBone(tamingRoot, tamingNodes);

                //合并骨骼
                foreach (var childBone in tamingRoot.Children)
                {
                    Bone bone = bodyRoot.FindChild(childBone.Name);
                    if (bone != null) //翻转骨骼
                    {
                        RotateBone(bodyRoot, bone);
                        bone.Position = childBone.Position;
                        bone.Skins.AddRange(childBone.Skins);
                        for (int i = childBone.Children.Count - 1; i >= 0; i--)
                        {
                            childBone.Children[i].Parent = bone;
                        }
                    }
                    else //直接添加
                    {
                        bodyRoot.Children.Add(childBone);
                    }
                }
                bodyRoot.Skins.AddRange(tamingRoot.Skins);
            }

            return bodyRoot;
        }

        private void RotateBone(Bone root, Bone childBone)
        {
            List<Bone> parents = new List<Bone>();
            Bone bone = childBone;
            while (bone != null)
            {
                parents.Add(bone);
                if (bone == root) break;
                bone = bone.Parent;
            }

            for (int i = parents.Count - 1; i > 0; i--)
            {
                var b0 = parents[i];
                var b1 = parents[i - 1];

                if (b0 == root)
                { //转接到新root里

                    Bone vRoot = new Bone("@" + root.Name);
                    vRoot.Position = new Point(-b1.Position.X, -b1.Position.Y);
                    for (int j = b0.Children.Count - 1; j >= 0; j--)
                    {
                        if (b0.Children[j] != b1)
                        {
                            b0.Children[j].Parent = vRoot;
                        }
                    }
                    if (vRoot.Children.Count > 0)
                    {
                        vRoot.Parent = b1;
                    }
                }
                else
                {
                    b0.Position = new Point(-b1.Position.X, -b1.Position.Y);
                    b1.Parent = null;
                    b0.Parent = b1;
                }
            }
            childBone.Parent = root;
        }

        private void CreateBone(Bone root, Wz_Node[] frameNodes, bool? bodyFace = null)
        {
            bool face = true;

            foreach (Wz_Node partNode in frameNodes)
            {
                Wz_Node linkPartNode = partNode;
                while (linkPartNode.Value is Wz_Uol)
                {
                    linkPartNode = linkPartNode.GetValue<Wz_Uol>().HandleUol(linkPartNode);
                }

                foreach (Wz_Node childNode in linkPartNode.Nodes) //分析部件
                {
                    Wz_Node linkNode = childNode;
                    while (linkNode?.Value is Wz_Uol)
                    {
                        linkNode = ((Wz_Uol)childNode.Value).HandleUol(linkNode);
                    }
                    if (linkNode == null)
                    {
                        continue;
                    }
                    if (childNode.Text == "hairShade")
                    {
                        linkNode = childNode.FindNodeByPath("0");
                        if (linkNode == null)
                        {
                            continue;
                        }
                    }
                    if (linkNode.Value is Wz_Png)
                    {
                        //过滤纹理
                        switch (childNode.Text)
                        {
                            case "face": if (!(bodyFace ?? face)) continue; break;
                            case "ear": if (this.EarType != 1) continue; break;
                            case "lefEar": if (this.EarType != 2) continue; break;
                            case "hairOverHead":
                            case "backHairOverCape":
                            case "backHair": if (HairCover) continue; break;
                            case "hair":
                            case "backHairBelowCap": if (!HairCover) continue; break;
                            case "hairShade": if (!ShowHairShade) continue; break;
                            default:
                                if (childNode.Text.StartsWith("weapon"))
                                {
                                    //检查是否多武器颜色
                                    if (linkNode.ParentNode.FindNodeByPath("weapon1") != null)
                                    {
                                        //只追加限定武器
                                        string weaponName = "weapon" + (this.WeaponIndex == 0 ? "" : this.WeaponIndex.ToString());
                                        if (childNode.Text != weaponName)
                                        {
                                            continue;
                                        }
                                    }
                                }
                                break;
                        }

                        //读取纹理
                        Skin skin = new Skin();
                        skin.Name = childNode.Text;
                        skin.Image = BitmapOrigin.CreateFromNode(linkNode, PluginBase.PluginManager.FindWz);
                        skin.Z = linkNode.FindNodeByPath("z").GetValueEx<string>(null);

                        //读取骨骼
                        Wz_Node mapNode = linkNode.FindNodeByPath("map");
                        if (mapNode != null)
                        {
                            Bone parentBone = null;
                            foreach (var map in mapNode.Nodes)
                            {
                                string mapName = map.Text;
                                Point mapOrigin = map.GetValue<Wz_Vector>();

                                if (mapName == "muzzle") //特殊处理 忽略
                                {
                                    continue;
                                }

                                if (parentBone == null) //主骨骼
                                {
                                    parentBone = AppendBone(root, null, skin, mapName, mapOrigin);
                                }
                                else //级联骨骼
                                {
                                    AppendBone(root, parentBone, skin, mapName, mapOrigin);
                                }
                            }
                        }
                        else
                        {
                            root.Skins.Add(skin);
                        }
                    }
                    else
                    {
                        switch (childNode.Text)
                        {
                            case "face":
                                face = Convert.ToInt32(childNode.Value) != 0;
                                break;
                        }
                    }
                }
            }
        }

        private Bone AppendBone(Bone root, Bone parentBone, Skin skin, string mapName, Point mapOrigin)
        {
            Bone bone = root.FindChild(mapName);
            bool exists;
            if (bone == null) //创建骨骼
            {
                exists = false;
                bone = new Bone(mapName);
                bone.Position = mapOrigin;
            }
            else
            {
                exists = true;
            }

            if (parentBone == null) //主骨骼
            {
                if (!exists) //基准骨骼不存在 加到root
                {
                    parentBone = root;
                    bone.Parent = parentBone;
                    bone.Skins.Add(skin);
                    skin.Offset = new Point(-mapOrigin.X, -mapOrigin.Y);
                }
                else //如果已存在 创建一个关节
                {
                    Bone bone0 = new Bone("@" + bone.Name + "_" + skin.Name);
                    bone0.Position = new Point(-mapOrigin.X, -mapOrigin.Y);
                    bone0.Parent = bone;
                    parentBone = bone0;
                    bone0.Skins.Add(skin);
                    skin.Offset = Point.Empty;
                }
                return parentBone;
            }
            else //级联骨骼
            {
                if (!exists)
                {
                    bone.Parent = parentBone;
                    bone.Position = mapOrigin;
                }
                else //如果已存在 替换
                {
                    bone.Parent = parentBone;
                    bone.Position = mapOrigin;
                }

                return null;
            }
        }

        public BitmapOrigin DrawFrame(Bone bone)
        {
            return DrawFrame(bone, 0, Point.Empty, false);
        }

        public BitmapOrigin DrawFrame(Bone bone, ActionFrame frame)
        {
            if (frame == null)
            {
                return DrawFrame(bone);
            }
            return DrawFrame(bone, frame.RotateProp == 0 ? frame.Rotate : 0, frame.Move, frame.Flip);
        }

        public BitmapOrigin DrawFrame(Bone bone, int rotate, Point move, bool flip)
        {
            List<AvatarLayer> layers = new List<AvatarLayer>(GenerateLayer(bone, Point.Empty));
            layers.Sort((l0, l1) => l1.ZIndex.CompareTo(l0.ZIndex));
            //计算最大图像范围
            Rectangle rect = Rectangle.Empty;
            foreach (var layer in layers)
            {
                var newRect = new Rectangle(layer.Position, layer.Bitmap.Size);
                rect = rect.Size.IsEmpty ? newRect : Rectangle.Union(rect, newRect);
            }
            rect = rect.Size.IsEmpty ? Rectangle.Empty : rect;

            if (rect.IsEmpty)
            {
                return new BitmapOrigin();
            }

            //绘制图像
            Bitmap bmp = new Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmp);
            layers.ForEach(layer =>
            {
                g.DrawImage(layer.Bitmap, layer.Position.X - rect.X, layer.Position.Y - rect.Y);
            });
            
            g.Dispose();

            if (flip || rotate != 0) //重新绘制 旋转和镜像
            {
                Matrix mt;
                switch (rotate)
                {
                    case 0:
                        mt = Matrix.Identity;
                        break;
                    case 90:
                        mt = new Matrix(0, 1, -1, 0, bmp.Height - 1, 0);
                        rect = new Rectangle(-rect.Bottom, rect.X, bmp.Height, bmp.Width);
                        break;
                    case 180:
                        mt = new Matrix(-1, 0, 0, -1, bmp.Width - 1, bmp.Height - 1);
                        rect = new Rectangle(-rect.Right, -rect.Bottom, bmp.Width, bmp.Height);
                        break;
                    case 270:
                        mt = new Matrix(0, -1, 1, 0, 0, bmp.Width - 1);
                        rect = new Rectangle(rect.Y, -rect.Right, bmp.Height, bmp.Width);
                        break;
                    default:
                        goto case 0;
                }

                if (flip)
                {
                    mt *= new Matrix(-1, 0, 0, 1, rect.Width - 1, 0);
                    rect.X = -rect.Right;
                }

                Bitmap bmpFlip = new Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                TransformPixel(bmp, bmpFlip, mt);
                bmp.Dispose();
                bmp = bmpFlip;
            }

            if (move != Point.Empty)
            {
                rect.Offset(flip && rotate != 0 ? -move.X : move.X, move.Y);
            }

            return new BitmapOrigin(bmp, -rect.X, -rect.Y);
        }

        private unsafe void TransformPixel(Bitmap src, Bitmap dst, Matrix mt)
        {
            var pxData1 = src.LockBits(new Rectangle(0, 0, src.Width, src.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var pxData2 = dst.LockBits(new Rectangle(0, 0, dst.Width, dst.Height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            for (int y = 0; y < pxData1.Height; y++)
            {
                int* pSrc = (int*)((byte*)pxData1.Scan0 + y * pxData1.Stride);
                for (int x = 0; x < pxData1.Width; x++, pSrc++)
                {
                    Point newPoint = new Point(
                        x * mt.m11 + y * mt.m21 + mt.m31,
                        x * mt.m12 + y * mt.m22 + mt.m32
                    );
                    int* pDst = (int*)((byte*)pxData2.Scan0 + newPoint.Y * pxData2.Stride + newPoint.X * 4);
                    *pDst = *pSrc;
                }
            }

            dst.UnlockBits(pxData2);
            src.UnlockBits(pxData1);
        }

        private IEnumerable<AvatarLayer> GenerateLayer(Bone bone, Point position)
        {
            position.Offset(bone.Position);
            foreach (Skin skin in bone.Skins)
            {
                var layer = new AvatarLayer();
                layer.Bitmap = skin.Image.Bitmap;
                layer.Position = new Point(position.X + skin.Offset.X - skin.Image.Origin.X,
                    position.Y + skin.Offset.Y - skin.Image.Origin.Y);
                layer.ZIndex = this.ZMap.IndexOf(skin.Z);
                if (layer.ZIndex < 0)
                {
                    layer.ZIndex = this.ZMap.Count;
                }
                yield return layer;
            }

            foreach (var child in bone.Children)
            {
                foreach (var layer in GenerateLayer(child, position))
                {
                    yield return layer;
                }
            }
        }

        private Wz_Node[] LinkPlayerParts(ActionFrame bodyAction, ActionFrame faceAction)
        {
            //寻找所有部件
            List<Wz_Node> partNode = new List<Wz_Node>();

            //链接人
            if (this.Body != null && this.Head != null && bodyAction != null
                && this.Body.Visible && this.Head.Visible)
            {
                //身体
                Wz_Node bodyNode = FindBodyActionNode(bodyAction);
                partNode.Add(bodyNode);

                //头部
                bool? face = bodyAction.Face; //扩展动作规定头部
                if (face == null && bodyNode != null) //链接的body内规定
                {
                    Wz_Node propNode = bodyNode.FindNodeByPath("face");
                    if(propNode != null)
                    {
                        face = propNode.GetValue<int>(0) != 0;
                    }
                }

                if (face ?? false)
                {
                    ActionFrame headAction = new ActionFrame() { Action = "front" };
                    partNode.Add(FindActionFrameNode(this.Head.Node, headAction));
                }
                else
                {
                    partNode.Add(FindActionFrameNode(this.Head.Node, bodyAction));
                }
                
                //脸
                if (this.Face != null && this.Face.Visible && faceAction != null)
                {
                    partNode.Add(FindActionFrameNode(this.Face.Node, faceAction));
                }
                //毛
                if (this.Hair != null && this.Hair.Visible)
                {
                    if (face ?? false)
                    {
                        ActionFrame headAction = new ActionFrame() { Action = "default" };
                        partNode.Add(FindActionFrameNode(this.Hair.Node, headAction));
                    }
                    else
                    {
                        partNode.Add(FindActionFrameNode(this.Hair.Node, bodyAction));
                    }
                }
                //其他部件
                for (int i = 4; i < 16; i++)
                {
                    var part = this.Parts[i];
                    if (part != null && part.Visible)
                    {
                        if (i == 12 && Gear.GetGearType(part.ID.Value) == GearType.cashWeapon) //点装武器
                        {
                            var wpNode = part.Node.FindNodeByPath(this.WeaponType.ToString());
                            partNode.Add(FindActionFrameNode(wpNode, bodyAction));
                        }
                        else if (i == 14) //脸
                        {
                            partNode.Add(FindActionFrameNode(part.Node, faceAction));
                        }
                        else //其他部件
                        {
                            partNode.Add(FindActionFrameNode(part.Node, bodyAction));
                        }
                    }
                }
            }

            partNode.RemoveAll(node => node == null);

            return partNode.ToArray();
        }

        private Wz_Node[] LinkTamingParts(ActionFrame tamingAction)
        {
            List<Wz_Node> partNode = new List<Wz_Node>();

            //链接马
            if (this.Taming != null && this.Taming.Visible && tamingAction != null)
            {
                partNode.Add(FindActionFrameNode(this.Taming.Node, tamingAction));
                if (this.Saddle != null && this.Saddle.Visible)
                {
                    var saddleNode = this.Saddle.Node.FindNodeByPath(false, this.Taming.ID.ToString());
                    partNode.Add(FindActionFrameNode(saddleNode, tamingAction));
                }
            }

            partNode.RemoveAll(node => node == null);

            return partNode.ToArray();
        }

        private Wz_Node FindBodyActionNode(ActionFrame actionFrame)
        {
            Wz_Node actionNode = null;
            if (this.Body != null)
            {
                actionNode = this.Body.Node.FindNodeByPath(actionFrame.Action);
            }
            if (actionNode == null)
            {
                Wz_Node bodyNode = PluginBase.PluginManager.FindWz("Character\\00002000.img");
                actionNode = bodyNode.FindNodeByPath(actionFrame.Action);
            }
            if (actionNode != null)
            {
                actionNode = actionNode.FindNodeByPath(actionFrame.Frame.ToString());
            }
            return actionNode;
        }

        private Wz_Node FindActionFrameNode(Wz_Node parent, ActionFrame actionFrame)
        {
            if (parent == null || actionFrame == null)
            {
                return null;
            }
            var actionNode = parent;
            foreach (var path in new[] { actionFrame.Action, actionFrame.Frame.ToString() })
            {
                if (actionNode != null && !string.IsNullOrEmpty(path))
                {
                    actionNode = actionNode.FindNodeByPath(path);

                    //处理uol
                    Wz_Uol uol;
                    while ((uol = actionNode.GetValueEx<Wz_Uol>(null)) != null)
                    {
                        actionNode = uol.HandleUol(actionNode);
                    }
                }
            }

            return actionNode;
        }

        #region parts
        /// <summary>
        /// 身体
        /// </summary>
        public AvatarPart Body
        {
            get { return this.Parts[0]; }
            set { this.Parts[0] = value; }
        }

        /// <summary>
        /// 头部
        /// </summary>
        public AvatarPart Head
        {
            get { return this.Parts[1]; }
            set { this.Parts[1] = value; }
        }

        /// <summary>
        /// 脸部
        /// </summary>
        public AvatarPart Face
        {
            get { return this.Parts[2]; }
            set { this.Parts[2] = value; }
        }

        /// <summary>
        /// 头发
        /// </summary>
        public AvatarPart Hair
        {
            get { return this.Parts[3]; }
            set { this.Parts[3] = value; }
        }

        /// <summary>
        /// 帽子
        /// </summary>
        public AvatarPart Cap
        {
            get { return this.Parts[4]; }
            set { this.Parts[4] = value; }
        }

        /// <summary>
        /// 上衣
        /// </summary>
        public AvatarPart Coat
        {
            get { return this.Parts[5]; }
            set { this.Parts[5] = value; }
        }

        /// <summary>
        /// 套装
        /// </summary>
        public AvatarPart Longcoat
        {
            get { return this.Parts[6]; }
            set { this.Parts[6] = value; }
        }

        /// <summary>
        /// 胖次
        /// </summary>
        public AvatarPart Pants
        {
            get { return this.Parts[7]; }
            set { this.Parts[7] = value; }
        }

        /// <summary>
        /// 鞋子
        /// </summary>
        public AvatarPart Shoes
        {
            get { return this.Parts[8]; }
            set { this.Parts[8] = value; }
        }

        /// <summary>
        /// 手套
        /// </summary>
        public AvatarPart Glove
        {
            get { return this.Parts[9]; }
            set { this.Parts[9] = value; }
        }

        /// <summary>
        /// 盾牌
        /// </summary>
        public AvatarPart SubWeapon
        {
            get { return this.Parts[10]; }
            set { this.Parts[10] = value; }
        }

        /// <summary>
        /// 披风
        /// </summary>
        public AvatarPart Cape
        {
            get { return this.Parts[11]; }
            set { this.Parts[11] = value; }
        }

        /// <summary>
        /// 武器
        /// </summary>
        public AvatarPart Weapon
        {
            get { return this.Parts[12]; }
            set { this.Parts[12] = value; }
        }

        /// <summary>
        /// 耳环
        /// </summary>
        public AvatarPart Earrings
        {
            get { return this.Parts[13]; }
            set { this.Parts[13] = value; }
        }

        /// <summary>
        /// 脸饰
        /// </summary>
        public AvatarPart FaceAccessory
        {
            get { return this.Parts[14]; }
            set { this.Parts[14] = value; }
        }

        /// <summary>
        /// 眼饰
        /// </summary>
        public AvatarPart EyeAccessory
        {
            get { return this.Parts[15]; }
            set { this.Parts[15] = value; }
        }

        /// <summary>
        /// 骑宠
        /// </summary>
        public AvatarPart Taming
        {
            get { return this.Parts[16]; }
            set { this.Parts[16] = value; }
        }

        /// <summary>
        /// 鞍子
        /// </summary>
        public AvatarPart Saddle
        {
            get { return this.Parts[17]; }
            set { this.Parts[17] = value; }
        }

        #endregion

        #region statics

        private static readonly string[] baseActions = new[]{
            "walk1", "walk2", "stand1", "stand2", "alert",
            "swingO1", "swingO2", "swingO3", "swingOF",
            "swingT1", "swingT2", "swingT3", "swingTF",
            "swingP1", "swingP2", "swingPF", 
            "stabO1", "stabO2", "stabOF", "stabT1", "stabT2", "stabTF",
            "shoot1", "shoot2", "shootF",
            "proneStab", "prone", 
            "heal", "fly", "jump", "sit", "ladder", "rope"
        };

        public static readonly ReadOnlyCollection<string> BaseActions = new ReadOnlyCollection<string>(baseActions);

        #endregion

        private class AvatarLayer
        {
            public Bitmap Bitmap { get; set; }
            public Point Position { get; set; }
            public int ZIndex { get; set; }
        }

        private struct Matrix
        {
            public Matrix(int m11, int m12, int m21, int m22, int m31, int m32)
            {
                this.m11 = m11;
                this.m12 = m12;
                this.m21 = m21;
                this.m22 = m22;
                this.m31 = m31;
                this.m32 = m32;
            }
            public int m11, m12, m21, m22, m31, m32;

            public static Matrix Identity
            {
                get { return new Matrix(1, 0, 0, 1, 0, 0); }
            }

            public static Matrix operator *(Matrix mt1, Matrix mt2)
            {
                return new Matrix(
                    mt1.m11 * mt2.m11 + mt1.m12 * mt2.m21,
                    mt1.m11 * mt2.m12 + mt1.m12 * mt2.m22,
                    mt1.m21 * mt2.m11 + mt1.m22 * mt2.m21,
                    mt1.m21 * mt2.m12 + mt1.m22 * mt2.m22,
                    mt1.m31 * mt2.m11 + mt1.m32 * mt2.m21 + mt2.m31,
                    mt1.m31 * mt2.m12 + mt1.m32 * mt2.m22 + mt2.m32);
            }
        }
    }
}
