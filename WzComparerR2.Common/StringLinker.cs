using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.Common
{
    public class StringLinker
    {
        public StringLinker()
        {
            stringEqp = new Dictionary<int, StringResult>();
            stringFamiliarSkill = new Dictionary<int, StringResult>();
            stringItem = new Dictionary<int, StringResult>();
            stringMap = new Dictionary<int, StringResult>();
            stringMob = new Dictionary<int, StringResult>();
            stringNpc = new Dictionary<int, StringResult>();
            stringSkill = new Dictionary<int, StringResult>();
            stringSkill2 = new Dictionary<string, StringResult>();
        }

        public bool Load(Wz_File stringWz)
        {
            if (stringWz == null || stringWz.Node == null)
                return false;
            this.Clear();
            int id;
            foreach (Wz_Node node in stringWz.Node.Nodes)
            {
                Wz_Image image = node.Value as Wz_Image;
                if (image == null)
                    continue;
                switch (node.Text)
                {
                    case "Pet.img":
                    case "Cash.img":
                    case "Ins.img":
                    case "Consume.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {
                            if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                            {
                                StringResult strResult = new StringResult();
                                strResult.Name = GetDefaultString(linkNode, "name");
                                strResult.Desc = GetDefaultString(linkNode, "desc");
                                strResult.AutoDesc = GetDefaultString(linkNode, "autodesc");
                                strResult.FullPath = tree.FullPath; // always use the original node path

                                AddAllValue(strResult, linkNode);
                                stringItem[id] = strResult;
                            }
                        }
                        break;
                    case "Etc.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree0 in image.Node.Nodes)
                        {
                            foreach (Wz_Node tree in tree0.Nodes)
                            {
                                if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                                {
                                    StringResult strResult = new StringResult();
                                    strResult.Name = GetDefaultString(linkNode, "name");
                                    strResult.Desc = GetDefaultString(linkNode, "desc");
                                    strResult.FullPath = tree.FullPath;

                                    AddAllValue(strResult, linkNode);
                                    stringItem[id] = strResult;
                                }
                            }
                        }
                        break;
                    case "Familiar.img":
                    case "FamiliarSkill.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree0 in image.Node.Nodes)
                        {
                            if (tree0.Text == "skill")
                            {
                                foreach (Wz_Node tree1 in tree0.Nodes)
                                {
                                    if (Int32.TryParse(tree1.Text, out id) && tree1.ResolveUol() is Wz_Node linkNode)
                                    {
                                        StringResult strResult = null;
                                        if (strResult == null) strResult = new StringResult();

                                        strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name ?? string.Empty;
                                        strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                                        strResult.FullPath = tree1.FullPath;

                                        AddAllValue(strResult, linkNode);
                                        stringFamiliarSkill[id] = strResult;
                                    }
                                }
                            }
                        }
                        break;
                    case "Mob.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {
                            if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                            {
                                StringResult strResult = new StringResult();
                                strResult.Name = GetDefaultString(linkNode, "name");
                                strResult.FullPath = tree.FullPath;

                                AddAllValue(strResult, linkNode);
                                stringMob[id] = strResult;
                            }
                        }
                        break;
                    case "Npc.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {
                            if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                            {
                                StringResult strResult = new StringResult();
                                strResult.Name = GetDefaultString(linkNode, "name");
                                strResult.Desc = GetDefaultString(linkNode, "func");
                                strResult.FullPath = tree.FullPath;

                                AddAllValue(strResult, linkNode);
                                stringNpc[id] = strResult;
                            }
                        }
                        break;
                    case "Map.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree0 in image.Node.Nodes)
                        {
                            foreach (Wz_Node tree in tree0.Nodes)
                            {
                                if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                                {
                                    StringResult strResult = new StringResult();
                                    strResult.Name = string.Format("{0}：{1}",
                                        GetDefaultString(linkNode, "streetName"),
                                        GetDefaultString(linkNode, "mapName"));
                                    strResult.Desc = GetDefaultString(linkNode, "mapDesc");
                                    strResult.FullPath = tree.FullPath;

                                    AddAllValue(strResult, linkNode);
                                    stringMap[id] = strResult;
                                }
                            }
                        }
                        break;
                    case "Skill.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {
                            if (tree.ResolveUol() is not Wz_Node linkNode)
                            {
                                continue;
                            }
                            StringResultSkill strResult = new StringResultSkill();
                            strResult.Name = GetDefaultString(linkNode, "name");//?? GetDefaultString(tree, "bookName");
                            strResult.Desc = GetDefaultString(linkNode, "desc");
                            strResult.Pdesc = GetDefaultString(linkNode, "pdesc");
                            strResult.SkillH.Add(GetDefaultString(linkNode, "h"));
                            strResult.SkillpH.Add(GetDefaultString(linkNode, "ph"));
                            strResult.SkillhcH.Add(GetDefaultString(linkNode, "hch"));
                            if (strResult.SkillH[0] == null)
                            {
                                strResult.SkillH.RemoveAt(0);
                                for (int i = 1; ; i++)
                                {
                                    string hi = GetDefaultString(linkNode, "h" + i);
                                    if (string.IsNullOrEmpty(hi))
                                        break;
                                    strResult.SkillH.Add(hi);
                                }
                            }
                            // KMST1196, add h_ prefix strings
                            foreach (Wz_Node child in linkNode.Nodes)
                            {
                                if (child.Text.StartsWith("h_") && int.TryParse(child.Text.Substring(2), out int level) && level > 0 && child.Value != null)
                                {
                                    strResult.SkillExtraH.Add(new KeyValuePair<int, string>(level, child.GetValue<string>()));
                                }
                            }
                            if (strResult.SkillExtraH.Count > 1)
                            {
                                strResult.SkillExtraH.Sort((left, right) => left.Key.CompareTo(right.Key));
                            }
                            strResult.SkillH.TrimExcess();
                            strResult.SkillpH.TrimExcess();
                            strResult.FullPath = tree.FullPath;

                            AddAllValue(strResult, linkNode);
                            if (tree.Text.Length >= 7 && Int32.TryParse(tree.Text, out id))
                            {
                                stringSkill[id] = strResult;
                            }
                            stringSkill2[tree.Text] = strResult;
                        }
                        break;
                    case "Eqp.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree0 in image.Node.Nodes)
                        {
                            foreach (Wz_Node tree1 in tree0.Nodes)
                            {
                                foreach (Wz_Node tree in tree1.Nodes)
                                {
                                    if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                                    {
                                        StringResult strResult = new StringResult();
                                        strResult.Name = GetDefaultString(linkNode, "name");
                                        strResult.Desc = GetDefaultString(linkNode, "desc");
                                        strResult.FullPath = tree.FullPath;

                                        AddAllValue(strResult, linkNode);
                                        stringEqp[id] = strResult;
                                    }
                                }
                            }
                        }
                        break;
                }
            }

            return this.HasValues;
        }

        public void Clear()
        {
            stringEqp.Clear();
            stringFamiliarSkill.Clear();
            stringItem.Clear();
            stringMob.Clear();
            stringMap.Clear();
            stringNpc.Clear();
            stringSkill.Clear();
            stringSkill2.Clear();
        }

        public bool HasValues
        {
            get
            {
                return (stringEqp.Count + stringItem.Count + stringMap.Count +
                    stringMob.Count + stringNpc.Count + stringSkill.Count > 0);
            }
        }

        private Dictionary<int, StringResult> stringEqp;
        private Dictionary<int, StringResult> stringFamiliarSkill;
        private Dictionary<int, StringResult> stringItem;
        private Dictionary<int, StringResult> stringMap;
        private Dictionary<int, StringResult> stringMob;
        private Dictionary<int, StringResult> stringNpc;
        private Dictionary<int, StringResult> stringSkill;
        private Dictionary<string, StringResult> stringSkill2;

        private string GetDefaultString(Wz_Node node, string searchNodeText)
        {
            node = node.FindNodeByPath(searchNodeText);
            return node == null ? null : Convert.ToString(node.Value);
        }

        private void AddAllValue(StringResult sr, Wz_Node node)
        {
            foreach (Wz_Node child in node.Nodes)
            {
                if (child.Value != null)
                {
                    sr[child.Text] = child.GetValue<string>();
                }
            }
        }

        public Dictionary<int, StringResult> StringEqp
        {
            get { return stringEqp; }
        }

        public Dictionary<int, StringResult> StringFamiliarSkill
        {
            get { return stringFamiliarSkill; }
        }

        public Dictionary<int, StringResult> StringItem
        {
            get { return stringItem; }
        }

        public Dictionary<int, StringResult> StringMap
        {
            get { return stringMap; }
        }

        public Dictionary<int, StringResult> StringMob
        {
            get { return stringMob; }
        }

        public Dictionary<int, StringResult> StringNpc
        {
            get { return stringNpc; }
        }

        public Dictionary<int, StringResult> StringSkill
        {
            get { return stringSkill; }
        }

        public Dictionary<string, StringResult> StringSkill2
        {
            get { return stringSkill2; }
        }

    }
}
