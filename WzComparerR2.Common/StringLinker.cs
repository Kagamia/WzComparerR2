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
                            if (Int32.TryParse(tree.Text, out id))
                            {
                                StringResult strResult = new StringResult();
                                strResult.Name = GetDefaultString(tree, "name");
                                strResult.Desc = GetDefaultString(tree, "desc");
                                strResult.AutoDesc = GetDefaultString(tree, "autodesc");
                                strResult.FullPath = tree.FullPath;

                                AddAllValue(strResult, tree);
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
                                if (Int32.TryParse(tree.Text, out id))
                                {
                                    StringResult strResult = new StringResult();
                                    strResult.Name = GetDefaultString(tree, "name");
                                    strResult.Desc = GetDefaultString(tree, "desc");
                                    strResult.FullPath = tree.FullPath;

                                    AddAllValue(strResult, tree);
                                    stringItem[id] = strResult;
                                }
                            }
                        }
                        break;
                    case "Mob.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {
                            if (Int32.TryParse(tree.Text, out id))
                            {
                                StringResult strResult = new StringResult();
                                strResult.Name = GetDefaultString(tree, "name");
                                strResult.FullPath = tree.FullPath;

                                AddAllValue(strResult, tree);
                                stringMob[id] = strResult;
                            }
                        }
                        break;
                    case "Npc.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {
                            if (Int32.TryParse(tree.Text, out id))
                            {
                                StringResult strResult = new StringResult();
                                strResult.Name = GetDefaultString(tree, "name");
                                strResult.Desc = GetDefaultString(tree, "func");
                                strResult.FullPath = tree.FullPath;

                                AddAllValue(strResult, tree);
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
                                if (Int32.TryParse(tree.Text, out id))
                                {
                                    StringResult strResult = new StringResult();
                                    strResult.Name = string.Format("{0}：{1}",
                                        GetDefaultString(tree, "streetName"),
                                        GetDefaultString(tree, "mapName"));
                                    strResult.Desc = GetDefaultString(tree, "mapDesc");
                                    strResult.FullPath = tree.FullPath;

                                    AddAllValue(strResult, tree);
                                    stringMap[id] = strResult;
                                }
                            }
                        }
                        break;
                    case "Skill.img":
                        if (!image.TryExtract()) break;
                        foreach (Wz_Node tree in image.Node.Nodes)
                        {
                            StringResult strResult = new StringResult(true);
                            strResult.Name = GetDefaultString(tree, "name");//?? GetDefaultString(tree, "bookName");
                            strResult.Desc = GetDefaultString(tree, "desc");
                            strResult.Pdesc = GetDefaultString(tree, "pdesc");
                            strResult.SkillH.Add(GetDefaultString(tree, "h"));
                            strResult.SkillpH.Add(GetDefaultString(tree, "ph"));
                            strResult.SkillhcH.Add(GetDefaultString(tree, "hch"));
                            if (strResult.SkillH[0] == null)
                            {
                                strResult.SkillH.RemoveAt(0);
                                for (int i = 1; ; i++)
                                {
                                    string hi = GetDefaultString(tree, "h" + i);
                                    if (string.IsNullOrEmpty(hi))
                                        break;
                                    strResult.SkillH.Add(hi);
                                }
                            }
                            strResult.SkillH.TrimExcess();
                            strResult.SkillpH.TrimExcess();
                            strResult.FullPath = tree.FullPath;

                            AddAllValue(strResult, tree);
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
                                    if (Int32.TryParse(tree.Text, out id))
                                    {
                                        StringResult strResult = new StringResult();
                                        strResult.Name = GetDefaultString(tree, "name");
                                        strResult.Desc = GetDefaultString(tree, "desc");
                                        strResult.FullPath = tree.FullPath;

                                        AddAllValue(strResult, tree);
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
            stringItem.Clear();
            stringMob.Clear();
            stringMap.Clear();
            stringNpc.Clear();
            stringSkill.Clear();
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
                    sr.AllValues[child.Text] = child.GetValue<string>();
                }
            }
        }

        public Dictionary<int, StringResult> StringEqp
        {
            get { return stringEqp; }
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
