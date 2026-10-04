using System;
using System.Collections.Generic;
using System.Linq;
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

        public bool Update(Wz_Node stringNode)
        {
            if (stringNode == null)
                return false;

            return Load(stringNode, update: true);
        }

        public bool Load(Wz_File stringWz)
        {
            this.Clear();

            return Load(stringWz?.Node);
        }

        public bool Load(Wz_Node stringNode, bool update = false)
        {
            if (update)
            {
                this.SourceStringUpdateNode = stringNode;
            }
            else
            {
                this.SourceStringNode = stringNode;
            }

            int id;
            foreach (Wz_Node node in stringNode.Nodes ?? Enumerable.Empty<Wz_Node>())
            {
                Wz_Image image = node.Value as Wz_Image;
                Wz_Node imgNode = node;
                switch (node.Text)
                {
                    case "Pet.img":
                    case "Cash.img":
                    case "Ins.img":
                    case "Consume.img":
                        if (image != null)
                        {
                            if (!image.TryExtract()) break;
                            imgNode = image.Node;
                        }
                        foreach (Wz_Node tree in imgNode.Nodes)
                        {
                            if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                            {
                                StringResult strResult = null;
                                if (update)
                                {
                                    try { strResult = stringItem[id]; }
                                    catch { }
                                }
                                if (strResult == null) strResult = new StringResult();

                                strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name ?? string.Empty;
                                strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                                strResult.AutoDesc = GetDefaultString(linkNode, "autodesc") ?? strResult.AutoDesc;
                                if (!update) strResult.FullPath = tree.FullPath; // always use the original node path

                                AddAllValue(strResult, linkNode);
                                stringItem[id] = strResult;
                            }
                        }
                        break;
                    case "Etc.img":
                        if (image != null)
                        {
                            if (!image.TryExtract()) break;
                            imgNode = image.Node;
                        }
                        foreach (Wz_Node tree0 in imgNode.Nodes)
                        {
                            foreach (Wz_Node tree in tree0.Nodes)
                            {
                                if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                                {
                                    StringResult strResult = null;
                                    if (update)
                                    {
                                        try { strResult = stringItem[id]; }
                                        catch { }
                                    }
                                    if (strResult == null) strResult = new StringResult();

                                    strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name ?? string.Empty;
                                    strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                                    if (!update) strResult.FullPath = tree.FullPath;

                                    AddAllValue(strResult, linkNode);
                                    stringItem[id] = strResult;
                                }
                            }
                        }
                        break;
                    case "Familiar.img":
                    case "FamiliarSkill.img":
                        if (image != null)
                        {
                            if (!image.TryExtract()) break;
                            imgNode = image.Node;
                        }
                        foreach (Wz_Node tree0 in imgNode.Nodes)
                        {
                            if (tree0.Text == "skill")
                            {
                                foreach (Wz_Node tree1 in tree0.Nodes)
                                {
                                    if (Int32.TryParse(tree1.Text, out id) && tree1.ResolveUol() is Wz_Node linkNode)
                                    {
                                        StringResult strResult = null;
                                        if (update)
                                        {
                                            try { strResult = stringFamiliarSkill[id]; }
                                            catch { }
                                        }
                                        if (strResult == null) strResult = new StringResult();

                                        strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name ?? string.Empty;
                                        strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                                        if (!update) strResult.FullPath = tree1.FullPath;

                                        AddAllValue(strResult, linkNode);
                                        stringFamiliarSkill[id] = strResult;
                                    }
                                }
                            }
                        }
                        break;
                    case "Mob.img":
                        if (image != null)
                        {
                            if (!image.TryExtract()) break;
                            imgNode = image.Node;
                        }
                        foreach (Wz_Node tree in imgNode.Nodes)
                        {
                            if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                            {
                                StringResult strResult = null;
                                if (update)
                                {
                                    try { strResult = stringMob[id]; }
                                    catch { }
                                }
                                if (strResult == null) strResult = new StringResult();

                                strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name ?? string.Empty;
                                if (!update) strResult.FullPath = tree.FullPath;

                                AddAllValue(strResult, linkNode);
                                stringMob[id] = strResult;
                            }
                        }
                        break;
                    case "Npc.img":
                        if (image != null)
                        {
                            if (!image.TryExtract()) break;
                            imgNode = image.Node;
                        }
                        foreach (Wz_Node tree in imgNode.Nodes)
                        {
                            if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                            {
                                StringResult strResult = null;
                                if (update)
                                {
                                    try { strResult = stringNpc[id]; }
                                    catch { }
                                }
                                if (strResult == null) strResult = new StringResult();

                                strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name ?? string.Empty;
                                strResult.Desc = GetDefaultString(linkNode, "func") ?? strResult.Desc;
                                if (!update) strResult.FullPath = tree.FullPath;

                                AddAllValue(strResult, linkNode);
                                stringNpc[id] = strResult;
                            }
                        }
                        break;
                    case "Map.img":
                        if (image != null)
                        {
                            if (!image.TryExtract()) break;
                            imgNode = image.Node;
                        }
                        foreach (Wz_Node tree0 in imgNode.Nodes)
                        {
                            foreach (Wz_Node tree in tree0.Nodes)
                            {
                                if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                                {
                                    StringResult strResult = null;
                                    if (update)
                                    {
                                        try { strResult = stringMap[id]; }
                                        catch { }
                                    }
                                    if (strResult == null) strResult = new StringResult();

                                    strResult.Name = string.Format("{0}：{1}",
                                        GetDefaultString(linkNode, "streetName"),
                                        GetDefaultString(linkNode, "mapName"))
                                        ?? strResult.Name ?? string.Empty;
                                    strResult.Desc = GetDefaultString(linkNode, "mapDesc") ?? strResult.Desc;
                                    if (!update) strResult.FullPath = tree.FullPath;

                                    AddAllValue(strResult, linkNode);
                                    stringMap[id] = strResult;
                                }
                            }
                        }
                        break;
                    case "Skill.img":
                        if (image != null)
                        {
                            if (!image.TryExtract()) break;
                            imgNode = image.Node;
                        }
                        foreach (Wz_Node tree in imgNode.Nodes)
                        {
                            if (tree.ResolveUol() is not Wz_Node linkNode)
                            {
                                continue;
                            }
                            StringResultSkill strResult = null;
                            if (update)
                            {
                                try
                                {
                                    if (tree.Text.Length >= 7 && Int32.TryParse(tree.Text, out id))
                                    {
                                        strResult = (StringResultSkill)stringSkill[id];
                                    }
                                    strResult = (StringResultSkill)stringSkill2[tree.Text];
                                }
                                catch { }
                            }
                            if (strResult == null) strResult = new StringResultSkill();

                            strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name ?? string.Empty;//?? GetDefaultString(tree, "bookName");
                            strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                            strResult.Pdesc = GetDefaultString(linkNode, "pdesc") ?? strResult.Pdesc;

                            var h = GetDefaultString(linkNode, "h");
                            if (update && h != null)
                            {
                                strResult.SkillH.Clear();
                            }
                            strResult.SkillH.Add(h);

                            h = GetDefaultString(linkNode, "ph");
                            if (update && h != null)
                            {
                                strResult.SkillpH.Clear();
                                strResult.SkillpH.Add(h);
                            }
                            else if (!update) strResult.SkillpH.Add(h);

                            h = GetDefaultString(linkNode, "hch");
                            if (update && h != null)
                            {
                                strResult.SkillhcH.Clear();
                                strResult.SkillhcH.Add(h);
                            }
                            else if (!update) strResult.SkillhcH.Add(h);

                            if (strResult.SkillH.Count > 0 && strResult.SkillH.Last() == null)
                            {
                                strResult.SkillH.RemoveAt(strResult.SkillH.Count - 1);
                                bool cleared = false;

                                for (int i = 1; ; i++)
                                {
                                    string hi = GetDefaultString(linkNode, "h" + i);
                                    if (string.IsNullOrEmpty(hi))
                                        break;
                                    else if (update && !cleared)
                                    {
                                        strResult.SkillH.Clear();
                                        cleared = true;
                                    }
                                    strResult.SkillH.Add(hi);
                                }
                            }
                            // KMST1196, add h_ prefix strings
                            foreach (Wz_Node child in linkNode.Nodes)
                            {
                                if (child.Text.StartsWith("h_") && int.TryParse(child.Text.Substring(2), out int level) && level > 0 && child.Value != null)
                                {
                                    strResult.SkillExtraH.RemoveAll(x => x.Key == level);
                                    strResult.SkillExtraH.Add(new KeyValuePair<int, string>(level, child.GetValue<string>()));
                                }
                            }
                            if (strResult.SkillExtraH.Count > 1)
                            {
                                strResult.SkillExtraH.Sort((left, right) => left.Key.CompareTo(right.Key));
                            }
                            strResult.SkillH.TrimExcess();
                            strResult.SkillpH.TrimExcess();
                            if (!update) strResult.FullPath = tree.FullPath;

                            AddAllValue(strResult, linkNode);
                            if (tree.Text.Length >= 7 && Int32.TryParse(tree.Text, out id))
                            {
                                stringSkill[id] = strResult;
                            }
                            stringSkill2[tree.Text] = strResult;
                        }
                        break;
                    case "Eqp.img":
                        if (image != null)
                        {
                            if (!image.TryExtract()) break;
                            imgNode = image.Node;
                        }
                        foreach (Wz_Node tree0 in imgNode.Nodes)
                        {
                            foreach (Wz_Node tree1 in tree0.Nodes)
                            {
                                foreach (Wz_Node tree in tree1.Nodes)
                                {
                                    if (Int32.TryParse(tree.Text, out id) && tree.ResolveUol() is Wz_Node linkNode)
                                    {
                                        StringResult strResult = null;
                                        if (update)
                                        {
                                            try { strResult = stringEqp[id]; }
                                            catch { }
                                        }
                                        if (strResult == null) strResult = new StringResult();

                                        strResult.Name = GetDefaultString(linkNode, "name") ?? strResult.Name ?? string.Empty;
                                        strResult.Desc = GetDefaultString(linkNode, "desc") ?? strResult.Desc;
                                        if (!update) strResult.FullPath = tree.FullPath;

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

            SourceStringUpdateNode = null;
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

        public Wz_Node SourceStringNode { get; private set; }
        public Wz_Node SourceStringUpdateNode { get; private set; }

        public Wz_Node FindNodeFromSource(string path, Wz_Type type)
        {
            switch (type)
            {
                case Wz_Type.String:
                    return SourceStringUpdateNode?.FindNodeByPath(path, true) ?? SourceStringNode?.FindNodeByPath(path, true);
                default:
                    return null;
            }
        }
    }
}
