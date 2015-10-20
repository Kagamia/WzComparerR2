using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Drawing.Drawing2D;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;


namespace WzComparerR2.MonsterCard.UI
{
    public class MobGage
    {
        public MobGage()
        {
        }

        public void Load(Wz_Node imgNode)
        {
            this.mob = null;
            this.gage0 = null;
            this.gage1 = null;

            Wz_Node uiNode = PluginManager.FindWz(@"UI\UIWindow2.img\MobGage");
            if (uiNode != null)
            {
                if (backgrnd == null)
                {
                    backgrnd = uiNode.FindNodeByPath("backgrnd").GetValueEx<Wz_Png>(null)?.ExtractPng();
                }
                if (backgrnd2 == null)
                {
                    backgrnd2 = uiNode.FindNodeByPath("backgrnd2").GetValueEx<Wz_Png>(null)?.ExtractPng();
                }
                if (backgrnd3 == null)
                {
                    backgrnd3 = uiNode.FindNodeByPath("backgrnd3").GetValueEx<Wz_Png>(null)?.ExtractPng();
                }
                if (backgrnd4 == null)
                {
                    backgrnd4 = uiNode.FindNodeByPath("backgrnd4").GetValueEx<Wz_Png>(null)?.ExtractPng();
                }

                var m = Regex.Match(imgNode.Text, @"(\d+)\.img");
                if (m.Success)
                {
                    var node = uiNode.FindNodeByPath(@"Mob\" + m.Result("$1"));
                    if (node != null)
                    {
                        BitmapOrigin mobPng = BitmapOrigin.CreateFromNode(node, null);
                        this.mob = mobPng.Bitmap;
                    }
                }

                var infoNode = imgNode.FindNodeByPath("info");
                var hpTagColor = infoNode?.FindNodeByPath("hpTagColor").GetValueEx(0);
                var hpTagBgcolor = infoNode?.FindNodeByPath("hpTagBgcolor").GetValueEx(0);

                if (hpTagColor > 0)
                {
                    string path = @"Gage\" + hpTagColor.ToString() + @"\1";
                    this.gage0 = uiNode.FindNodeByPath(path).GetValueEx<Wz_Png>(null)?.ExtractPng();
                }

                if (hpTagBgcolor > 0)
                {
                    string path = @"Gage\" + hpTagBgcolor.ToString() + @"\1";
                    this.gage1 = uiNode.FindNodeByPath(path).GetValueEx<Wz_Png>(null)?.ExtractPng();
                }
            }
        }

        public bool Visible { get; set; }

        private Bitmap backgrnd;
        private Bitmap backgrnd2;
        private Bitmap backgrnd3;
        private Bitmap backgrnd4;
        private Bitmap mob;
        private Bitmap gage0;
        private Bitmap gage1;

        public void DrawGage(Graphics g, Size size)
        {
            Point gageOrigin = Point.Empty;
            if (backgrnd != null) //绘制怪物框
            {
                g.DrawImage(backgrnd, 0, 0);
                gageOrigin.X = backgrnd.Width;

                if (mob != null) //绘制怪物头像
                {
                    g.DrawImage(mob, (backgrnd.Width - mob.Width) / 2, (backgrnd.Height - mob.Height) / 2);
                }
            }
            if (backgrnd2 != null && backgrnd3 != null && backgrnd4 != null) //绘制血条
            {
                int gagueWidth = Math.Max(0, size.Width - gageOrigin.X - backgrnd2.Width - backgrnd4.Width);
                int x = gageOrigin.X;
                g.DrawImage(backgrnd2, x, 0);
                x += backgrnd2.Width;
                if (gagueWidth > 0) //血条背景
                {
                    //底层
                    TextureBrush brush = new TextureBrush(backgrnd3);
                    brush.WrapMode = WrapMode.Tile;
                    brush.TranslateTransform(x, 0);
                    g.FillRectangle(brush, x, 0, gagueWidth, backgrnd3.Height);
                    brush.Dispose();

                    //背景色
                    if (this.gage1 != null)
                    {
                        Point offset = new Point(x, (backgrnd3.Height - gage1.Height) / 2);
                        brush = new TextureBrush(this.gage1);
                        brush.WrapMode = WrapMode.Tile;
                        brush.TranslateTransform(offset.X, offset.Y);
                        g.FillRectangle(brush, offset.X, offset.Y, gagueWidth, this.gage1.Height);
                        brush.Dispose();
                    }

                    //前景色
                    if (this.gage0 != null)
                    {
                        Point offset = new Point(x, (backgrnd3.Height - gage0.Height) / 2);
                        brush = new TextureBrush(this.gage0);
                        brush.WrapMode = WrapMode.Tile;
                        brush.TranslateTransform(offset.X, offset.Y);
                        g.FillRectangle(brush, offset.X, offset.Y, gagueWidth / 2, this.gage0.Height);
                        brush.Dispose();
                    }

                    x += gagueWidth;
                }
                g.DrawImage(backgrnd4, x, 0);
            }
        }
    }
}
