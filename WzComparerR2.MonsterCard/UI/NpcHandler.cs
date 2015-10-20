using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Drawing;
using DevComponents.DotNetBar;
using DevComponents.AdvTree;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using WzComparerR2.CharaSimControl;

namespace WzComparerR2.MonsterCard.UI
{
    class NpcHandler : Handler
    {
        public NpcHandler(MonsterCardForm form) : base(form)
        {
            this.tooltipRender = new NpcTooltipRender();
        }

        private NpcTooltipRender tooltipRender;
        private NpcInfo npcInfo;

        public override void OnLoad(Wz_Node imgNode)
        {
            NpcInfo npcInfo = new NpcInfo();
            Wz_Node infoNode = imgNode.FindNodeByPath("info");

            Match m = Regex.Match(imgNode.Text, @"(\d{7})\.img");
            if (m.Success)
            {
                int id;
                if (Int32.TryParse(m.Result("$1"), out id))
                {
                    npcInfo.ID = id;
                }
            }

            //加载基础属性
            if (infoNode != null)
            {
                foreach (var node in infoNode.Nodes)
                {
                    switch (node.Text)
                    {
                        case "shop": npcInfo.Shop = node.GetValueEx<int>(0) != 0; break;
                        case "link": npcInfo.Link = node.GetValueEx<int>(0); break;
                        case "default": npcInfo.Default = BitmapOrigin.CreateFromNode(node, null); break;
                    }
                }
            }

            this.npcInfo = npcInfo;
        }

        private Wz_Node GetLinkNode(int linkNpcID)
        {
            return PluginManager.FindWz(string.Format("Npc\\{0:d7}.img", linkNpcID));
        }

        private Bitmap GetTooltipImage(NpcInfo npcInfo, Wz_Node imgNode)
        {
            if (npcInfo.Default.Bitmap != null)
            {
                return npcInfo.Default.Bitmap;
            }

            Wz_Node linkNode = npcInfo.Link == null ? imgNode : GetLinkNode(npcInfo.Link.Value);

            if (linkNode == null)
            {
                return null;
            }

            BitmapOrigin imageFrame = new BitmapOrigin();

            foreach (var action in new[] { "stand", "move", "say" })
            {
                var actNode = linkNode.FindNodeByPath(action + "\\0");
                if (actNode != null)
                {
                    imageFrame = BitmapOrigin.CreateFromNode(actNode, PluginManager.FindWz);
                    if (imageFrame.Bitmap != null)
                    {
                        break;
                    }
                }
            }

            return imageFrame.Bitmap;
        }

        public override Gif GetAnimate(string aniName)
        {
            if (this.npcInfo == null || !this.npcInfo.Animates.Contains(aniName))
            {
                return null;
            }

            return this.npcInfo.Animates[aniName]?.AnimateGif;
        }

        public override IEnumerable<string> GetAnimateNames()
        {
            if (this.npcInfo == null)
            {
                yield break;
            }
            foreach (var ani in this.npcInfo.Animates)
            {
                yield return ani.Name;
            }
        }

        public override void OnLoadAnimates(Wz_Node imgNode)
        {
            Wz_Node linkNode = npcInfo.Link == null ? imgNode : GetLinkNode(npcInfo.Link.Value);

            if (linkNode == null)
            {
                return;
            }

            foreach (var node in linkNode.Nodes)
            {
                if (node.Text != "info" && !node.Text.StartsWith("condition"))
                {
                    var ani = new LifeAnimate(node.Text);
                    ani.AnimateGif = Gif.CreateFromNode(node, PluginManager.FindWz);
                    this.npcInfo.Animates.Add(ani);
                }
            }
        }

        public override void ShowTooltipWindow(Wz_Node imgNode)
        {
            Bitmap mobImage = GetTooltipImage(this.npcInfo, imgNode);
            this.tooltipRender.StringLinker = this.PluginEntry.Context.DefaultStringLinker;
            Bitmap bmp = this.tooltipRender.Render(this.npcInfo, mobImage);
            if (bmp != null)
            {
                var tooltipWnd = this.PluginEntry.Context.DefaultTooltipWindow as AfrmTooltip;
                if (tooltipWnd != null)
                {
                    tooltipWnd.Bitmap = bmp;
                    tooltipWnd.TargetItem = null;
                    tooltipWnd.CaptionRectangle = new Rectangle(Point.Empty, bmp.Size);
                    tooltipWnd.Refresh();

                    tooltipWnd.TargetItem = this.npcInfo;
                    tooltipWnd.HideOnHover = false;
                    tooltipWnd.ImageFileName = "npc_" + this.npcInfo.ID.ToString() + ".png";
                    tooltipWnd.Show();
                }
            }
        }
    }
}
