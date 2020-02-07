using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using WzComparerR2.CharaSim;
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
            this.LoadEffectInfo();
            if((this.ID.Value >= 30000) && (this.ID.Value < 50000))
            {
                this.LoadMixHairs();
            }
        }

        public Wz_Node Node { get; private set; }
        public string ISlot { get; private set; }
        public BitmapOrigin Icon { get; private set; }
        public bool Visible { get; set; }
        public int? ID { get; private set; }
        public Wz_Node ItemEff { get; private set; } //Effects Node from Effect.wz/ItemEff.img/(itemcode)/effect
        public Wz_Node[] MixedNodes; //Nodes from Mix Hair
        public bool MixHair { get; set; }
        public int MixOpacity { get; set; } //混合染色透明度

        private void LoadInfo()
        {
            var m = Regex.Match(Node.Text, @"^(\d+)(\.img)?$");
            if (m.Success)
            {
                this.ID = Convert.ToInt32(m.Result("$1"));
                GearType type = Gear.GetGearType(this.ID.Value);
                if (type == GearType.face || type == GearType.face2)
                {
                    Icon = BitmapOrigin.CreateFromNode(PluginBase.PluginManager.FindWz(@"Item\Install\0380.img\03801284\info\icon"), PluginBase.PluginManager.FindWz);
                }
                if (type == GearType.hair || type == GearType.hair2)
                {
                    Icon = BitmapOrigin.CreateFromNode(PluginBase.PluginManager.FindWz(@"Item\Install\0380.img\03801283\info\icon"), PluginBase.PluginManager.FindWz);
                }
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
        private void LoadEffectInfo()
        {
            if (this.Node.Nodes["effect"] != null)
            {
                ItemEff = this.Node.Nodes["effect"];
                return;
            }
            Wz_Node itemEff = PluginBase.PluginManager.FindWz("Effect\\ItemEff.img");
            if(itemEff == null)
            {
                return;
            }
            Wz_Node effectNode = itemEff.FindNodeByPath(this.ID.ToString()+"\\effect");
            ItemEff = effectNode;
        }
        private void LoadMixHairs()
        {
            string hairDir;
            int baseBlackHair = this.ID.Value;
            baseBlackHair = baseBlackHair - (baseBlackHair % 10);
            int[] HairCodes = { baseBlackHair, baseBlackHair + 1, baseBlackHair + 2, baseBlackHair + 3, baseBlackHair + 4, baseBlackHair + 5, baseBlackHair + 6, baseBlackHair + 7 };
            MixedNodes = new Wz_Node[] { null, null, null, null, null, null, null, null };
            for(int i=0;i<HairCodes.Length;i++)
            {
                hairDir = "Character\\Hair\\" + HairCodes[i].ToString("d8") + ".img";
                MixedNodes[i] = PluginBase.PluginManager.FindWz(hairDir);
            }
        }
    }
}
