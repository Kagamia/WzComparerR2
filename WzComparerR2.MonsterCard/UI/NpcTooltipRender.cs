using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using WzComparerR2.CharaSimControl;
using System.Drawing.Imaging;
using WzComparerR2.Common;
using static WzComparerR2.MonsterCard.UI.RenderHelper;

namespace WzComparerR2.MonsterCard.UI
{
    public class NpcTooltipRender
    {
        public NpcTooltipRender()
        {
        }

        public StringLinker StringLinker { get; set; }

        public Bitmap Render(NpcInfo npcInfo, Bitmap npcImg)
        {
            if (npcInfo == null)
            {
                return null;
            }
            Bitmap bmp = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmp);

            //预绘制
            List<TextBlock> titleBlocks = new List<TextBlock>();
            List<TextBlock> propBlocks = new List<TextBlock>();
            int picY = 0;

            if (npcInfo.ID != null)
            {
                string mobName = GetNpcName(npcInfo.ID.Value);
                var block = PrepareText(g, mobName ?? "(null)", GearGraphics.ItemNameFont2, Brushes.White, 0, 0);
                titleBlocks.Add(block);
                block = PrepareText(g, "ID:" + npcInfo.ID, GearGraphics.ItemDetailFont, Brushes.White, block.Size.Width + 4, 4);
                titleBlocks.Add(block);
            }

            propBlocks.Add(PrepareText(g, "出没地区：", GearGraphics.ItemDetailFont, GearGraphics.GearNameBrushG, 0, 0));
            if (npcInfo.ID != null)
            {
                var locNode = PluginBase.PluginManager.FindWz("Etc\\NpcLocation.img\\" + npcInfo.ID.ToString());
                if (locNode != null)
                {
                    foreach (var locMapNode in locNode.Nodes)
                    {
                        int mapID;
                        string mapName = null;
                        if (int.TryParse(locMapNode.Text, out mapID))
                        {
                            mapName = GetMapName(mapID);
                        }
                        string npcLoc = string.Format(" {0}({1})", mapName ?? "null", locMapNode.Text);

                        propBlocks.Add(PrepareText(g, npcLoc, GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
                    }
                }
            }

            if (propBlocks.Count == 1) //获取地区失败
            {
                propBlocks.Add(PrepareText(g, " 不明", GearGraphics.ItemDetailFont, Brushes.White, 0, picY += 16));
            }

            //计算大小
            Rectangle titleRect = Measure(titleBlocks);
            Rectangle imgRect = Rectangle.Empty;
            Rectangle textRect = Measure(propBlocks);
            if (npcImg != null)
            {
                if (npcImg.Width > 250 || npcImg.Height > 300) //进行缩放
                {
                    double scale = Math.Min((double)250 / npcImg.Width, (double)300 / npcImg.Height);
                    imgRect = new Rectangle(0, 0, (int)(npcImg.Width * scale), (int)(npcImg.Height * scale));
                }
                else
                {
                    imgRect = new Rectangle(0, 0, npcImg.Width, npcImg.Height);
                }
            }

            //布局 
            //水平排列
            int width = 0;
            if (!imgRect.IsEmpty)
            {
                textRect.X = imgRect.Width + 4;
            }
            width = Math.Max(titleRect.Width, Math.Max(imgRect.Right, textRect.Right));
            titleRect.X = (width - titleRect.Width) / 2;

            //垂直居中
            int height = Math.Max(imgRect.Height, textRect.Height);
            imgRect.Y = (height - imgRect.Height) / 2;
            textRect.Y = (height - textRect.Height) / 2;
            if (!titleRect.IsEmpty)
            {
                height += titleRect.Height + 4;
                imgRect.Y += titleRect.Bottom + 4;
                textRect.Y += titleRect.Bottom + 4;
            }

            //绘制
            bmp = new Bitmap(width + 20, height + 20);
            titleRect.Offset(10, 10);
            imgRect.Offset(10, 10);
            textRect.Offset(10, 10);
            g = Graphics.FromImage(bmp);
            //绘制背景
            GearGraphics.DrawNewTooltipBack(g, 0, 0, bmp.Width, bmp.Height);
            //绘制标题
            foreach (var item in titleBlocks)
            {
                DrawText(g, item, titleRect.Location);
            }
            //绘制图像
            if (npcImg != null && !imgRect.IsEmpty)
            {
                g.DrawImage(npcImg, imgRect);
            }
            //绘制文本
            foreach (var item in propBlocks)
            {
                DrawText(g, item, textRect.Location);
            }
            g.Dispose();
            return bmp;
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

        private string GetMapName(int mapID)
        {
            StringResult sr;
            if (this.StringLinker == null || !this.StringLinker.StringMap.TryGetValue(mapID, out sr))
            {
                return null;
            }
            return sr.Name;
        }
    }
}
