using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using WzComparerR2.WzLib;

namespace WzComparerR2
{
    class Drawer
    {
        public Drawer()
        {
        }

        public void LoadZ(Wz_File baseWz)
        {
            Wz_Image img;
            if ((img = baseWz.Node.FindNodeByPath("zmap.img").GetValueEx<Wz_Image>(null)) != null
                && img.TryExtract())
            {
                foreach (Wz_Node node in img.Node.Nodes)
                {
                    this.Zmap.Add(node.Text);
                }
            }
        }

        public void Init(Wz_File charact)
        {
            Wz_Image img;
            
            if ((img = charact.Node.FindNodeByPath("TamingMob\\01983055.img").GetValueEx<Wz_Image>(null)) != null
                && img.TryExtract())
            {
                AddPart(img.Node, "sit");
            }

            if ((img = charact.Node.FindNodeByPath("TamingMob\\01912000.img").GetValueEx<Wz_Image>(null)) != null
                && img.TryExtract())
            {
                //AddPart(img.Node.FindNodeByPath("1902000"), "stand1");
            }
            

            if ((img = charact.Node.FindNodeByPath("00002000.img").GetValueEx<Wz_Image>(null)) != null
                && img.TryExtract())
            {
                AddPart(img.Node, "sit");
            }
            
            if ((img = charact.Node.FindNodeByPath("00012000.img").GetValueEx<Wz_Image>(null)) != null
                && img.TryExtract())
            {
                AddPart(img.Node, "sit");
            }

            if ((img = charact.Node.FindNodeByPath("Face\\00020000.img").GetValueEx<Wz_Image>(null)) != null
                && img.TryExtract())
            {
                AddPart(img.Node, "dam");
            }
            
        }

        public void AddEquip(Wz_Image img, string action)
        {
            if (img != null && img.TryExtract())
            {
                AddPart(img.Node, action);
            }
        }

        public void AddEquip(Wz_Node imgNode, string action)
        {
            AddPart(imgNode, action);
        }

        private void AddPart(Wz_Node imgNode, string action)
        {
            Wz_Node frameNode = imgNode.FindNodeByPath(false, action);
            if (frameNode == null) return;
            if (frameNode.Nodes.Contains("0"))
            {
                frameNode = frameNode.Nodes["0"];
            }
            foreach (Wz_Node partNode in frameNode.Nodes)
            {
                Wz_Node linkNode = partNode;
                if (partNode.Value is Wz_Uol)
                {
                    linkNode = ((Wz_Uol)partNode.Value).HandleUol(linkNode);
                }
                if (linkNode.Value is Wz_Png)
                {
                    FramePart part = ReadPart(linkNode);
                    this.Parts.Add(part);
                }
                else
                {
                    switch (partNode.Text)
                    {
                        case "face":
                        case "delay":
                            break;
                    }
                }
            }
        }

        private FramePart ReadPart(Wz_Node linkNode)
        {
            FramePart part = new FramePart();
            part.Name = linkNode.Text;
            part.Image = linkNode.GetValue<Wz_Png>().ExtractPng();
            Wz_Vector origin = linkNode.FindNodeByPath("origin").GetValueEx<Wz_Vector>(null);
            part.Origin = new Point(origin.X, origin.Y);
            part.Z = linkNode.FindNodeByPath("z").GetValueEx<string>(null);
            part.Group = linkNode.FindNodeByPath("group").GetValueEx<string>(null);

            Wz_Node mapNode = linkNode.FindNodeByPath("map");
            if (mapNode != null)
            {
                foreach (Wz_Node map in mapNode.Nodes)
                {
                    Wz_Vector vec = map.GetValue<Wz_Vector>();
                    part.Map.Add(map.Text, new Point(vec.X, vec.Y));
                }
            }
            return part;
        }

        public Bitmap Draw(Size bmpSize)
        {
            Bitmap backTexture = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(backTexture);
            g.FillRectangle(Brushes.White, new Rectangle(0, 0, 8, 8));
            g.FillRectangle(Brushes.LightGray, new Rectangle(8, 0, 8, 8));
            g.FillRectangle(Brushes.LightGray, new Rectangle(0, 8, 8, 8));
            g.FillRectangle(Brushes.White, new Rectangle(8, 8, 8, 8));
            g.Dispose();

            Bitmap bmp = new Bitmap(bmpSize.Width, bmpSize.Height, PixelFormat.Format32bppArgb);
            g = Graphics.FromImage(bmp);

            TextureBrush backBrush = new TextureBrush(backTexture);
            backBrush.WrapMode = WrapMode.Tile;
            g.FillRectangle(backBrush, 0, 0, bmp.Width, bmp.Height);
            backTexture.Dispose();

            //正式绘制
            g.TranslateTransform(bmp.Width / 2, (int)(bmp.Height * 0.8));
            g.DrawLine(Pens.Indigo, -100, 0, 100, 0);

            Bone root = new Bone("root");
            root.Position = Point.Empty;

            List<PreDrawPart> preDrawParts = new List<PreDrawPart>();
            foreach (FramePart part in this.Parts)
            {
                Point offset = new Point();
                Bone parentBone = null;
                int i = 0;
                
                //计算骨骼
                foreach (var kv in part.Map)
                {
                    Bone bone = root.FindChild(kv.Key);
                    bool exists;
                    if (bone == null) //创建骨骼
                    {
                        exists = false;
                        bone = new Bone(kv.Key);
                        bone.Position = kv.Value;
                    }
                    else
                    {
                        exists = true;
                    }

                    if (i == 0) //主骨骼
                    {
                        PreDrawPart drawItem = new PreDrawPart();
                        drawItem.Image = new BitmapOrigin((Bitmap)part.Image, part.Origin);
                        drawItem.Z = part.Z;
                        preDrawParts.Add(drawItem);

                        if (!exists) //基准骨骼不存在 加到root
                        {
                            parentBone = root;
                            bone.Parent = parentBone;
                            drawItem.Bone = bone;
                            drawItem.Offset = new Point(-kv.Value.X, -kv.Value.Y);
                        }
                        else //如果已存在 创建一个关节
                        {
                            Bone bone0 = new Bone("@" + bone.Name + "_" + part.Name);
                            bone0.Position = new Point(-kv.Value.X, -kv.Value.Y);
                            bone0.Parent = bone;
                            parentBone = bone0;
                            drawItem.Bone = bone0;
                            drawItem.Offset = Point.Empty;
                        }
                    }
                    else //级联骨骼
                    {
                        if (!exists)
                        {
                            bone.Parent = parentBone;
                            bone.Position = kv.Value;
                        }
                        else //如果已存在
                        {
                            if (parentBone == root) //翻转
                            {
                                Bone bone0 = new Bone("@" + bone.Name + "_" + part.Name); //创建虚关节
                                bone0.Position = new Point(- kv.Value.X, - kv.Value.Y); //偏移差值
                                for (int j = root.Children.Count - 1; j >= 0; j--) //对root所有子骨骼进行重定位
                                {
                                    Bone child = root.Children[j];
                                    if (child != bone)
                                    {
                                        child.Parent = bone0;
                                    }
                                }
                                bone0.Parent = bone;
                            }
                            else //替换
                            {
                                bone.Parent = parentBone;
                                bone.Position = kv.Value;
                            }
                        }
                    }

                    i++;
                }
            }

            var sorter = new PreDrawPartComprer(this.Zmap);

            preDrawParts.Sort(sorter);

            //绘制
            root.DrawOffset = Point.Empty;
            root.UpdateDrawOffset();

            foreach (var part in preDrawParts)
            {
                Point offset = part.Bone.DrawOffset;
                offset.X += part.Offset.X - part.Image.Origin.X;
                offset.Y += part.Offset.Y - part.Image.Origin.Y;
                g.DrawImage(part.Image.Bitmap, offset);
            }

            g.ResetTransform();
            g.Dispose();

            return bmp;
        }

        public List<FramePart> Parts = new List<FramePart>();
        public List<string> Zmap = new List<string>();

        public class FramePart
        {
            public string Name;
            public Image Image;
            public Point Origin;
            public string Z;
            public string Group;
            public Dictionary<string, Point> Map = new Dictionary<string, Point>();
        }

        private class PreDrawPart
        {
            public BitmapOrigin Image;
            public Point Offset;
            public string Z;

            public Bone Bone;
        }

        private class Bone
        {
            public Bone(string name)
            {
                this.Name = name;
            }

            public string Name { get; set; }
            public Point Position { get; set; }
            private Bone parent;
            public Bone Parent
            {
                get { return this.parent; }
                set
                {
                    Bone oldParent = this.parent;
                    if (oldParent != value)
                    {
                        if (oldParent != null)
                        {
                            oldParent.Children.Remove(this);
                        }
                        if (value != null)
                        {
                            value.Children.Add(this);
                        }

                        this.parent = value;
                    }
                }
            }

            public List<Bone> Children = new List<Bone>();

            public Point DrawOffset;

            public Bone FindChild(string name)
            {
                foreach (Bone bone in Children)
                {
                    if (bone.Name == name) return bone;
                    if (bone.Children.Count > 0)
                    {
                        Bone c = bone.FindChild(name);
                        if (c != null) return c;
                    }
                }
                return null;
            }

            public void UpdateDrawOffset()
            {
                foreach (Bone bone in Children)
                {
                    Point offset = this.DrawOffset;
                    offset.Offset(bone.Position);
                    bone.DrawOffset = offset;
                    bone.UpdateDrawOffset();
                }
            }
        }

        private class PreDrawPartComprer : Comparer<PreDrawPart>
        {
            public PreDrawPartComprer(List<string> zmap)
            {
                this.zmap = zmap;
            }

            public List<string> zmap;

            public override int Compare(PreDrawPart x, PreDrawPart y)
            {
                int zx = this.zmap.IndexOf(x.Z);
                int zy = this.zmap.IndexOf(y.Z);
                return -zx.CompareTo(zy);
            }
        }
    }
}
