using System;
using WzComparerR2.WzLib;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// Maps Classic dataset directories to <see cref="Wz_Type"/>.
    /// <para>
    /// A Unity client has no WZ files, but much of the existing code determines the node type from <c>Wz_File.Type</c> alone
    /// (item/equipment/map/quest tooltips, etc.). Here the first path segment of an asset path is mapped to
    /// the legacy WZ type so that this code can handle Classic nodes as well.
    /// </para>
    /// <para>
    /// Mappings: <c>Character/*</c> → Character, <c>Item/*</c> → Item, <c>Mob/*</c> → Mob,
    /// <c>Npc/*</c> → Npc, <c>Map/*</c> → Map, <c>Skill/*</c> → Skill, <c>Quest/*</c> → Quest,
    /// <c>String/*</c> → String, <c>Sound/*</c> → Sound, and so on.
    /// </para>
    /// </summary>
    public static class ClassicWzTypeMap
    {
        /// <summary>
        /// Gets the <see cref="Wz_Type"/> for an asset path (e.g. <c>Character/Cap/01002000</c> or <c>Character\Cap\01002000</c>)
        /// or a single directory name. Returns <see cref="Wz_Type.Unknown"/> when no mapping exists.
        /// </summary>
        public static Wz_Type GetWzType(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return Wz_Type.Unknown;
            }

            int slash = assetPath.IndexOfAny(new[] { '/', '\\' });
            string root = slash < 0 ? assetPath : assetPath.Substring(0, slash);
            switch (root.ToLowerInvariant())
            {
                case "character":
                    return Wz_Type.Character;
                case "datamap":
                    return Wz_Type.DataMap;
                case "effect":
                    return Wz_Type.Effect;
                case "etc":
                    return Wz_Type.Etc;
                case "item":
                    return Wz_Type.Item;
                case "language":
                    return Wz_Type.Language;
                case "map":
                    return Wz_Type.Map;
                case "mob":
                    return Wz_Type.Mob;
                case "morph":
                    return Wz_Type.Morph;
                case "npc":
                    return Wz_Type.Npc;
                case "quest":
                    return Wz_Type.Quest;
                case "reactor":
                    return Wz_Type.Reactor;
                case "skill":
                    return Wz_Type.Skill;
                case "sound":
                    return Wz_Type.Sound;
                case "string":
                    return Wz_Type.String;
                case "tamingmob":
                    return Wz_Type.TamingMob;
                case "ui":
                    return Wz_Type.UI;
                default:
                    return Wz_Type.Unknown;
            }
        }

        /// <summary>
        /// Nullable version of <see cref="GetWzType(string)"/>: returns null when no mapping exists (for external type resolution).
        /// </summary>
        public static Wz_Type? TryGetWzType(string assetPath)
        {
            Wz_Type type = GetWzType(assetPath);
            return type == Wz_Type.Unknown ? (Wz_Type?)null : type;
        }

        /// <summary>
        /// Same as <see cref="GetWzType(string)"/>, but directories with no corresponding legacy WZ type
        /// (e.g. <c>CashShopPreviewNew</c>, <c>Feature</c>, <c>GlobalMarketPreview</c>, <c>MapLogin</c>,
        /// <c>MonsterBook</c>, <c>StandardPDD</c>) are treated as <see cref="Wz_Type.Base"/>.
        /// Used to group root directories by type (e.g. the comparison page categories).
        /// </summary>
        public static Wz_Type GetWzTypeOrBase(string assetPath)
        {
            Wz_Type type = GetWzType(assetPath);
            return type == Wz_Type.Unknown ? Wz_Type.Base : type;
        }
    }
}
