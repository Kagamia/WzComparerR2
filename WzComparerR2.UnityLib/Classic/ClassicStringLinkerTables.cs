using System;
using System.Collections.Generic;
using System.Globalization;
using WzComparerR2.UnityLib.Unity;
using WzComparerR2.UnityLib.Wzjs;
using WzComparerR2.WzLib;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// Organizes the Unity <c>String/&lt;region&gt;/&lt;category&gt;</c> assets into the node shapes of the classic
    /// String.wz / Etc.wz / Quest.wz so that
    /// <c>WzComparerR2.Common.StringLinker.Load(Wz_Node, Wz_Node, Wz_Node, Wz_Node)</c>
    /// can use them directly.
    /// <para>
    /// The classic WZ <c>String.wz/Skill.img</c> corresponds to <c>String/&lt;region&gt;/Skill</c> here;
    /// in Classic, Cash / Consume / Eqp / Etc / Ins / Pet share a single
    /// <c>String/&lt;region&gt;/Item</c> (id → name/desc), so they are split back into per-img tables by id prefix.
    /// The imgs are Eqp / Consume / Ins / Etc / Pet / Cash and so on.
    /// </para>
    /// </summary>
    public sealed class ClassicStringLinkerTables
    {
        // Categories that StringLinker resolves directly with a single id level; Map / Eqp need extra grouping levels,
        // and GuildCastle / Roguelike / Redmoon need real Wz_Image instances and are not provided here.
        private static readonly string[] DirectStringCategories =
        {
            "Pet", "Cash", "Ins", "Consume", "Etc", "Familiar", "FamiliarSkill",
            "MonsterBook", "Mob", "Npc", "Skill",
        };

        private ClassicStringLinkerTables(
            Wz_Node stringNode, Wz_Node itemNode, Wz_Node etcNode, Wz_Node questNode)
        {
            this.StringNode = stringNode;
            this.ItemNode = itemNode;
            this.EtcNode = etcNode;
            this.QuestNode = questNode;
        }

        /// <summary>Node corresponding to String.wz (child nodes are named like <c>Skill.img</c>).</summary>
        public Wz_Node StringNode { get; private set; }

        /// <summary>Node corresponding to Item.wz; nothing needs to be filled in yet, so it stays null.</summary>
        public Wz_Node ItemNode { get; private set; }

        /// <summary>Node corresponding to Etc.wz (SetItemInfo, Achievement, worldArchive…).</summary>
        public Wz_Node EtcNode { get; private set; }

        /// <summary>Node corresponding to Quest.wz (QuestData/&lt;id&gt;.img/QuestInfo).</summary>
        public Wz_Node QuestNode { get; private set; }

        public bool HasValues
        {
            get
            {
                return (this.StringNode != null && this.StringNode.Nodes.Count > 0)
                    || (this.EtcNode != null && this.EtcNode.Nodes.Count > 0)
                    || (this.QuestNode != null && this.QuestNode.Nodes.Count > 0);
            }
        }

        public static ClassicStringLinkerTables Build(ClassicWzSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return new ClassicStringLinkerTables(
                BuildStringNode(source),
                null,
                BuildEtcNode(source),
                BuildQuestNode(source));
        }

        private static Wz_Node BuildStringNode(ClassicWzSource source)
        {
            string locale = source.Locale;
            Wz_Node root = new Wz_Node("String");
            Dictionary<string, Wz_Node> categories =
                new Dictionary<string, Wz_Node>(StringComparer.Ordinal);
            Dictionary<int, string> equipTypes = BuildEquipTypeMap(source);

            string prefix = "String/" + locale + "/";
            foreach (KeyValuePair<string, UnityEntryLocation> entry in source.Catalog.Entries)
            {
                if (!entry.Key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }
                string category = entry.Key.Substring(prefix.Length);
                if (category.Length == 0 || category.IndexOf('/') >= 0)
                {
                    continue;
                }
                if (string.Equals(category, "Item", StringComparison.Ordinal))
                {
                    continue; // single item table; split it by id range later
                }
                if (Array.IndexOf(DirectStringCategories, category) < 0)
                {
                    continue;
                }
                AddCategoryFromEntry(root, categories, source, entry.Key, category);
            }

            // Fill in categories missing from the current locale using COMMON.
            if (!string.Equals(locale, "COMMON", StringComparison.OrdinalIgnoreCase))
            {
                string commonPrefix = "String/COMMON/";
                foreach (KeyValuePair<string, UnityEntryLocation> entry in source.Catalog.Entries)
                {
                    if (!entry.Key.StartsWith(commonPrefix, StringComparison.Ordinal))
                    {
                        continue;
                    }
                    string category = entry.Key.Substring(commonPrefix.Length);
                    if (category.Length == 0 || category.IndexOf('/') >= 0
                        || string.Equals(category, "Item", StringComparison.Ordinal)
                        || Array.IndexOf(DirectStringCategories, category) < 0
                        || categories.ContainsKey(category))
                    {
                        continue;
                    }
                    AddCategoryFromEntry(root, categories, source, entry.Key, category);
                }
            }

            BuildItemCategories(root, categories, source, prefix, equipTypes);
            BuildEqpImage(root, categories, source, prefix, equipTypes);
            BuildMapImage(root, categories, source, prefix);
            BuildWorldMapImage(root, categories, source, prefix);
            return root;
        }

        private static void AddCategoryFromEntry(
            Wz_Node root, Dictionary<string, Wz_Node> categories,
            ClassicWzSource source, string key, string category)
        {
            if (categories.ContainsKey(category))
            {
                return;
            }
            Wz_Node node = new Wz_Node(WithImgSuffix(category));
            if (TryAppendEntryContents(node, source, key))
            {
                root.Nodes.Add(node);
            }
            categories[category] = node;
        }

        /// <summary>
        /// Swallows exceptions and continues so that a single failed asset decode does not lose the whole string table.
        /// </summary>
        private static bool TryAppendEntryContents(Wz_Node target, ClassicWzSource source, string key)
        {
            try
            {
                ClassicWzBridge.AppendEntryContents(target, source, key);
                return true;
            }
            catch (Exception ex) when (ex is UnityException || ex is ClassicAssetException || ex is WzjsException)
            {
                target.Nodes.Clear();
                return false;
            }
        }

        /// <summary>
        /// <c>String/&lt;region&gt;/Item</c> is a single id table merging Cash / Consume / Eqp / Etc / Ins / Pet;
        /// the individual <c>*.img</c> tables are restored from the id prefix.
        /// </summary>
        private static void BuildItemCategories(
            Wz_Node root, Dictionary<string, Wz_Node> categories,
            ClassicWzSource source, string prefix, Dictionary<int, string> equipTypes)
        {
            string itemKey = prefix + "Item";
            if (!source.Catalog.Entries.ContainsKey(itemKey))
            {
                return;
            }

            Wz_Node table = new Wz_Node("Item");
            TryAppendEntryContents(table, source, itemKey);

            List<Wz_Node> itemNodes = new List<Wz_Node>(table.Nodes);
            foreach (Wz_Node itemNode in itemNodes)
            {
                int id;
                if (!int.TryParse(itemNode.Text, NumberStyles.None,
                    CultureInfo.InvariantCulture, out id))
                {
                    continue;
                }

                string category = GetItemCategory(id);
                if (category == null)
                {
                    continue;
                }
                Wz_Node imgNode = GetOrCreateCategory(root, categories, category);
                if (string.Equals(category, "Eqp", StringComparison.Ordinal))
                {
                    // Eqp.img needs 3 levels: "category / subcategory / id".
                    AddEqpEntry(imgNode, itemNode, id, equipTypes);
                }
                else if (string.Equals(category, "Etc", StringComparison.Ordinal))
                {
                    // Etc.img needs 2 levels: "Etc / id".
                    GetOrAddChild(imgNode, "Etc").Nodes.Add(itemNode);
                }
                else
                {
                    imgNode.Nodes.Add(itemNode);
                }
            }
        }

        /// <summary>
        /// Determines which img in the classic String.wz an item id belongs to, based on its prefix.
        /// 0xxxxxx~1xxxxxx = Eqp, 2xxxxxx = Consume, 3xxxxxx = Ins, 4xxxxxx = Etc,
        /// 500xxxx = Pet, 501xxxx and above = Cash.
        /// </summary>
        internal static string GetItemCategory(int id)
        {
            if (id < 0)
            {
                return null;
            }
            if (id < 2000000)
            {
                return "Eqp";
            }
            if (id < 3000000)
            {
                return "Consume";
            }
            if (id < 4000000)
            {
                return "Ins";
            }
            if (id < 5000000)
            {
                return "Etc";
            }
            if (id < 5010000)
            {
                return "Pet";
            }
            return "Cash";
        }

        /// <summary>
        /// <c>String/&lt;region&gt;/Eqp</c> (when present) is also a single id table;
        /// StringLinker's Eqp.img needs three levels: "category / subcategory / id".
        /// </summary>
        private static void BuildEqpImage(
            Wz_Node root, Dictionary<string, Wz_Node> categories,
            ClassicWzSource source, string prefix, Dictionary<int, string> equipTypes)
        {
            string eqpKey = prefix + "Eqp";
            if (!source.Catalog.Entries.ContainsKey(eqpKey))
            {
                return;
            }

            Wz_Node table = new Wz_Node("Eqp");
            TryAppendEntryContents(table, source, eqpKey);
            Wz_Node eqpImg = GetOrCreateCategory(root, categories, "Eqp");
            foreach (Wz_Node node in new List<Wz_Node>(table.Nodes))
            {
                int id;
                if (int.TryParse(node.Text, NumberStyles.None, CultureInfo.InvariantCulture, out id))
                {
                    AddEqpEntry(eqpImg, node, id, equipTypes);
                }
            }
        }

        /// <summary>
        /// <c>String/&lt;region&gt;/Map</c> is a single id table;
        /// StringLinker's Map.img needs two levels: "group / id".
        /// </summary>
        private static void BuildMapImage(
            Wz_Node root, Dictionary<string, Wz_Node> categories,
            ClassicWzSource source, string prefix)
        {
            string mapKey = prefix + "Map";
            if (!source.Catalog.Entries.ContainsKey(mapKey))
            {
                return;
            }

            Wz_Node table = new Wz_Node("Map");
            TryAppendEntryContents(table, source, mapKey);
            Wz_Node mapImg = GetOrCreateCategory(root, categories, "Map");
            foreach (Wz_Node node in new List<Wz_Node>(table.Nodes))
            {
                int id;
                if (!int.TryParse(node.Text, NumberStyles.None, CultureInfo.InvariantCulture, out id))
                {
                    continue;
                }
                string groupName = (id / 10000000).ToString(CultureInfo.InvariantCulture);
                GetOrAddChild(mapImg, groupName).Nodes.Add(node);
            }
        }

        /// <summary>
        /// <c>String/&lt;region&gt;/WorldMap</c> is a single table keyed by world map id, and the values are
        /// <c>&lt;index&gt;_toolTip</c>, <c>&lt;index&gt;_title</c> and <c>&lt;index&gt;_desc</c>.
        /// It is normalized into the same shape as WZ's <c>String.wz/WorldMap.img</c> (root is <c>0</c>,
        /// the rest are zero-padded to three digits).
        /// </summary>
        private static void BuildWorldMapImage(
            Wz_Node root, Dictionary<string, Wz_Node> categories,
            ClassicWzSource source, string prefix)
        {
            string worldMapKey = prefix + "WorldMap";
            if (!source.Catalog.Entries.ContainsKey(worldMapKey))
            {
                return;
            }

            Wz_Node table = new Wz_Node("WorldMap");
            TryAppendEntryContents(table, source, worldMapKey);

            // WorldMap only has <id>/values and no WZ-style <id>/name.
            Wz_Node worldMapImg = GetOrCreateCategory(root, categories, "WorldMap");
            foreach (Wz_Node node in new List<Wz_Node>(table.Nodes))
            {
                int id;
                if (!int.TryParse(node.Text, NumberStyles.None, CultureInfo.InvariantCulture, out id))
                {
                    continue;
                }
                node.Text = id == 0
                    ? "0"
                    : id.ToString("000", CultureInfo.InvariantCulture);
                worldMapImg.Nodes.Add(node);
            }
        }

        private static void AddEqpEntry(
            Wz_Node eqpImg, Wz_Node entry, int id, Dictionary<int, string> equipTypes)
        {
            // StringLinker expects three levels (category / subcategory / id), so the conventional shape is kept too.
            Wz_Node group = GetOrAddChild(
                eqpImg, (id / 10000).ToString(CultureInfo.InvariantCulture));
            Wz_Node slot = GetOrAddChild(group, "0");
            slot.Nodes.Add(entry);

            // Also provide the WZ-shaped "Eqp.img/Eqp/<category name>/<id>" (referenced by DB2 and others).
            string typeName;
            if (equipTypes == null || !equipTypes.TryGetValue(id, out typeName))
            {
                return;
            }
            Wz_Node eqpRoot = GetOrAddChild(eqpImg, "Eqp");
            GetOrAddChild(eqpRoot, typeName).Nodes.Add(CloneNode(entry));
            if (string.Equals(typeName, "TamingMob", StringComparison.Ordinal))
            {
                // The category name on the WZ side is "Taming".
                GetOrAddChild(eqpRoot, "Taming").Nodes.Add(CloneNode(entry));
            }
        }

        private static Wz_Node CloneNode(Wz_Node node)
        {
            Wz_Node copy = new Wz_Node(node.Text);
            copy.Value = node.Value;
            foreach (Wz_Node child in node.Nodes)
            {
                copy.Nodes.Add(CloneNode(child));
            }
            return copy;
        }

        /// <summary>
        /// Mirrors <c>Etc/&lt;region&gt;/*</c> into the node shape of Etc.wz. Leaf nodes get a <c>.img</c> suffix
        /// (for example <c>Etc/SetItemInfo.img</c>, <c>Etc/CashPackage.img</c>, <c>Etc/Achievement/AchievementData/&lt;id&gt;.img</c>).
        /// </summary>
        private static Wz_Node BuildEtcNode(ClassicWzSource source)
        {
            string prefix = "Etc/" + source.Locale + "/";
            Wz_Node root = new Wz_Node("Etc");
            foreach (KeyValuePair<string, UnityEntryLocation> entry in source.Catalog.Entries)
            {
                if (!entry.Key.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }
                string rest = entry.Key.Substring(prefix.Length);
                if (rest.Length == 0)
                {
                    continue;
                }

                string[] segments = rest.Split('/');
                Wz_Node current = root;
                for (int i = 0; i < segments.Length; i++)
                {
                    bool isLeaf = i == segments.Length - 1;
                    string name = isLeaf ? WithImgSuffix(segments[i]) : segments[i];
                    Wz_Node next = GetOrAddChild(current, name);
                    if (isLeaf)
                    {
                        TryAppendEntryContents(next, source, entry.Key);
                    }
                    current = next;
                }
            }
            return root;
        }

        /// <summary>
        /// Builds the equipment id → category name mapping from <c>Character/&lt;type&gt;/&lt;id&gt;</c>
        /// (for example <c>Character/Cap/01002000</c>). It is used to assemble WZ's <c>String.wz/Eqp.img/Eqp/Cap/…</c>.
        /// </summary>
        private static Dictionary<int, string> BuildEquipTypeMap(ClassicWzSource source)
        {
            Dictionary<int, string> types = new Dictionary<int, string>();
            foreach (string key in source.Catalog.Entries.Keys)
            {
                if (!key.StartsWith("Character/", StringComparison.Ordinal))
                {
                    continue;
                }
                string[] parts = key.Split('/');
                if (parts.Length != 3)
                {
                    continue;
                }
                int id;
                if (!int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out id))
                {
                    continue;
                }
                types[id] = parts[1];
            }
            return types;
        }

        /// <summary>
        /// Builds the <c>QuestData/&lt;id&gt;.img/QuestInfo</c> shape of Quest.wz from
        /// <c>String/&lt;region&gt;/QuestData</c>.
        /// </summary>
        private static Wz_Node BuildQuestNode(ClassicWzSource source)
        {
            string key = "String/" + source.Locale + "/QuestData";
            if (!source.Catalog.Entries.ContainsKey(key))
            {
                key = "String/COMMON/QuestData";
                if (!source.Catalog.Entries.ContainsKey(key))
                {
                    return null;
                }
            }

            Wz_Node table = new Wz_Node("QuestData");
            TryAppendEntryContents(table, source, key);

            Wz_Node root = new Wz_Node("Quest");
            Wz_Node dataNode = root.Nodes.Add("QuestData");
            List<Wz_Node> quests = new List<Wz_Node>(table.Nodes);
            foreach (Wz_Node quest in quests)
            {
                Wz_Node questImg = dataNode.Nodes.Add(WithImgSuffix(quest.Text));
                Wz_Node infoNode = questImg.Nodes.Add("QuestInfo");
                List<Wz_Node> children = new List<Wz_Node>(quest.Nodes);
                foreach (Wz_Node child in children)
                {
                    infoNode.Nodes.Add(child);
                }
            }
            return root;
        }

        private static Wz_Node GetOrCreateCategory(
            Wz_Node root, Dictionary<string, Wz_Node> categories, string category)
        {
            Wz_Node node;
            if (categories.TryGetValue(category, out node))
            {
                return node;
            }
            node = GetOrAddChild(root, WithImgSuffix(category));
            categories[category] = node;
            return node;
        }

        private static Wz_Node GetOrAddChild(Wz_Node parent, string name)
        {
            Wz_Node existing = parent.Nodes[name];
            if (existing != null)
            {
                return existing;
            }
            return parent.Nodes.Add(name);
        }

        private static string WithImgSuffix(string name)
        {
            return name.EndsWith(".img", StringComparison.OrdinalIgnoreCase) ? name : name + ".img";
        }
    }
}
