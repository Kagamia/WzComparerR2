using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using WzComparerR2.WzLib;
using System.Text.RegularExpressions;

namespace WzComparerR2.Avatar
{
    public class AvatarPart
    {
        public AvatarPart(Wz_Node node)
        {
            this.Node = node;
            this.Visible = true;
            this.LoadInfo();
        }

        public Wz_Node Node { get; private set; }
        public string ISlot { get; private set; }
        public BitmapOrigin Icon { get; private set; }
        public bool Visible { get; set; }
        public int? ID { get; private set; }

        private void LoadInfo()
        {
            var m = Regex.Match(Node.Text, @"^(\d+)\.img$");
            if (m.Success)
            {
                this.ID = Convert.ToInt32(m.Result("$1"));
            }

            Wz_Node infoNode = this.Node.FindNodeByPath("info");
            if (infoNode == null)
            {
                return;
            }

            foreach (var node in infoNode.Nodes)
            {
                switch (node.Text)
                {
                    case "islot":
                        this.ISlot = node.GetValue<string>();
                        break;

                    case "icon":
                        this.Icon = BitmapOrigin.CreateFromNode(node, PluginBase.PluginManager.FindWz);
                        break;
                }
            }
        }
    }
}
