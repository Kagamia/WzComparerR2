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
            if ((img = charact.Node.FindNodeByPath("00002000.img").GetValueEx<Wz_Image>(null)) != null
                && img.TryExtract())
            {
                AddPart(img, "stand1");
            }

            if ((img = charact.Node.FindNodeByPath("00012000.img").GetValueEx<Wz_Image>(null)) != null
                && img.TryExtract())
            {
                AddPart(img, "stand1");
            }

            if ((img = charact.Node.FindNodeByPath("Face\\00020000.img").GetValueEx<Wz_Image>(null)) != null
                && img.TryExtract())
            {
                AddPart(img, "troubled");
            }
        }

        public void AddEquip(Wz_Image img, string action)
        {
            if (img != null && img.TryExtract())
            {
                AddPart(img, action);
            }
        }

        private void AddPart(Wz_Image img, string action)
        {
            Wz_Node frameNode = img.Node.FindNodeByPath(false, action);
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

        public Bitmap Draw()
        {
            Bitmap backTexture = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(backTexture);
            g.FillRectangle(Brushes.White, new Rectangle(0, 0, 8, 8));
            g.FillRectangle(Brushes.LightGray, new Rectangle(8, 0, 8, 8));
            g.FillRectangle(Brushes.LightGray, new Rectangle(0, 8, 8, 8));
            g.FillRectangle(Brushes.White, new Rectangle(8, 8, 8, 8));
            g.Dispose();

            Bitmap bmp = new Bitmap(300, 300, PixelFormat.Format32bppArgb);
            g = Graphics.FromImage(bmp);

            TextureBrush backBrush = new TextureBrush(backTexture);
            backBrush.WrapMode = WrapMode.Tile;
            g.FillRectangle(backBrush, 0, 0, bmp.Width, bmp.Height);
            backTexture.Dispose();

            //正式绘制
            g.TranslateTransform(bmp.Width / 2, bmp.Height / 2);
            g.DrawLine(Pens.Black, -50, 0, 50, 0);

            Dictionary<string, Point> mapOffset = new Dictionary<string, Point>();
            List<PreDrawPart> preDrawParts = new List<PreDrawPart>();
            foreach (FramePart part in this.Parts)
            {
                Point origin = new Point();
                int tryCount = 0;
                foreach (var kv in part.Map)
                {
                    Point offs;
                    if (tryCount == 0 && mapOffset.TryGetValue(kv.Key, out offs))
                    {
                        origin.X -= offs.X - kv.Value.X;
                        origin.Y -= offs.Y - kv.Value.Y;
                        tryCount++;
                    }
                    else
                    {
                        mapOffset[kv.Key] = new Point(-origin.X + kv.Value.X, -origin.Y + kv.Value.Y);
                    }
                }

                origin.X += part.Origin.X;
                origin.Y += part.Origin.Y;

                preDrawParts.Add(new PreDrawPart() { Image = part.Image, Origin = origin, Z = part.Z });
            }

            var sorter = new PreDrawPartComprer(this.Zmap);

            preDrawParts.Sort(sorter);
            foreach (var part in preDrawParts)
            {
                g.DrawImage(part.Image, -part.Origin.X, -part.Origin.Y);
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
            public Image Image;
            public Point Origin;
            public string Z;
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
