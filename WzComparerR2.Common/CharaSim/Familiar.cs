using System;
using System.Text.RegularExpressions;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Familiar
    {
        public Familiar()
        {
            FamiliarAttribute = "N";
            FamiliarCategory = 1;
        }

        public int FamiliarID { get; set; }
        public int MobID { get; set; }
        public int MonsterCardID { get; set; }
        public int FamiliarCategory { get; set; }
        public int SkillID { get; set; }
        public int SkillEffectAfter { get; set; }
        public int Range { get; set; }
        public string FamiliarAttribute { get; set; }
        public BitmapOrigin FamiliarCover { get; set; }

        public static Familiar CreateFromNode(Wz_Node node, GlobalFindNodeFunction findNode)
        {
            if (node == null)
                return null;
            int familiarID;
            Match m = Regex.Match(node.Text, @"^(\d+)\.img$");
            if (!(m.Success && Int32.TryParse(m.Result("$1"), out familiarID)))
            {
                return null;
            }

            Familiar familiar = new Familiar();

            familiar.FamiliarID = familiarID;

            Wz_Node standNode = node.FindNodeByPath("stand\\0").ResolveUol();
            if (standNode != null)
            {
                familiar.FamiliarCover = BitmapOrigin.CreateFromNode(standNode, findNode);
            }

            Wz_Node infoNode = node.FindNodeByPath("info").ResolveUol();

            if (infoNode != null)
            {
                foreach (Wz_Node subNode in infoNode.Nodes)
                {
                    switch (subNode.Text)
                    {
                        case "FAttribute":
                            familiar.FamiliarAttribute = subNode.GetValue<string>();
                            break;
                        case "FCategory":
                            familiar.FamiliarCategory = subNode.GetValue<int>();
                            break;
                        case "MobID":
                            familiar.MobID = subNode.GetValue<int>();
                            break;
                        case "monsterCardID":
                            familiar.MonsterCardID = subNode.GetValue<int>();
                            break;
                        case "range":
                            familiar.Range = subNode.GetValue<int>();
                            break;
                        case "skill":
                            foreach (Wz_Node skillNode in subNode.Nodes)
                            {
                                switch (skillNode.Text)
                                {
                                    case "id":
                                        familiar.SkillID = skillNode.GetValue<int>();
                                        break;
                                    case "effectAfter":
                                        familiar.SkillEffectAfter = skillNode.GetValue<int>();
                                        break;
                                }
                            }
                            break;
                        case "portrait":
                            familiar.FamiliarCover = BitmapOrigin.CreateFromNode(subNode, findNode);
                            break;
                    }
                }
            }

            return familiar;
        }
    }
}

