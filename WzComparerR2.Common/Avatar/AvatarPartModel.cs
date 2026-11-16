using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using WzComparerR2.WzLib;

namespace WzComparerR2.Avatar
{
    public enum AvatarPartSlot
    {
        // Body, head and face are separate resources, outside the native equipment array.
        Body = -3,
        Head = -2,
        Face = -1,

        Hair = 0,
        Cap = 1,
        FaceAccessory = 2,
        EyeAccessory = 3,
        Earrings = 4,
        Coat = 5,
        Pants = 6,
        Shoes = 7,
        Glove = 8,
        Cape = 9,
        SubWeapon = 10,
        Weapon = 11,
        Ring1 = 12,
        Ring2 = 13,
        PetEquipment1 = 14,
        Ring3 = 15,
        Ring4 = 16,
        Pendant1 = 17,
        Taming = 18,
        Saddle = 19,
        TamingEquipment = 20,
        Medal = 21,
        Belt = 22,
        Shoulder = 23,
        PetEquipment2 = 24,
        PetEquipment3 = 25,
        Pocket = 26,
        Android = 27,
        Heart = 28,
        Badge = 29,
        Emblem = 30,
        Pendant2 = 31
    }

    public enum AvatarEarType
    {
        Human = 0,
        Elf = 1,
        Lef = 2,
        HighLef = 3
    }

    public sealed class AvatarPartInput
    {
        public AvatarPartInput(int itemId)
        {
            if (itemId < 0)
                throw new ArgumentOutOfRangeException(nameof(itemId));

            this.ItemId = itemId;
        }

        public AvatarPartInput(Wz_Node imageNode)
        {
            this.ImageNode = imageNode ?? throw new ArgumentNullException(nameof(imageNode));
        }

        public int? ItemId { get; }
        public Wz_Node ImageNode { get; }
    }

    public enum AvatarPartType
    {
        Unknown,
        Body,
        Head,
        Face,
        Hair,
        Cap,
        Coat,
        Longcoat,
        Pants,
        Shoes,
        Glove,
        Shield,
        Cape,
        Weapon,
        CashWeapon,
        FaceAccessory,
        EyeAccessory,
        Earrings,
        Taming,
        Saddle,
        Ring,
        Pendant,
        PetEquipment,
        Medal,
        Belt,
        Shoulder,
        Pocket,
        Android,
        Heart,
        Badge,
        Emblem
    }

    public enum AvatarLayerGroup
    {
        Character,
        Taming
    }

    public static class AvatarSlotTokens
    {
        public static IReadOnlyList<string> Parse(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Array.Empty<string>();
            }

            // Slots are consecutive two-character tokens; an unmatched trailing character is ignored.
            string[] tokens = new string[value.Length / 2];
            for (int i = 0; i < tokens.Length; i++)
            {
                tokens[i] = value.Substring(i * 2, 2);
            }

            return new ReadOnlyCollection<string>(tokens);
        }

        public static bool Intersects(IEnumerable<string> left, IEnumerable<string> right)
        {
            HashSet<string> values = new HashSet<string>(left, StringComparer.Ordinal);
            foreach (string value in right)
            {
                if (values.Contains(value))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class AvatarPartContext
    {
        public AvatarPartContext(
            Wz_Node sourceNode,
            int? id,
            AvatarPartType type,
            AvatarLayerGroup group,
            string islot,
            string vslot)
        {
            this.SourceNode = sourceNode;
            this.Id = id;
            this.Type = type;
            this.Group = group;

            this.ISlot = islot ?? string.Empty;
            this.VSlot = vslot ?? string.Empty;
            this.ISlotTokens = AvatarSlotTokens.Parse(this.ISlot);
            this.VSlotTokens = AvatarSlotTokens.Parse(this.VSlot);
        }

        public Wz_Node SourceNode { get; private set; }
        public int? Id { get; private set; }
        public AvatarPartType Type { get; private set; }
        public AvatarLayerGroup Group { get; private set; }

        public string ISlot { get; private set; }
        public string VSlot { get; private set; }
        public IReadOnlyList<string> ISlotTokens { get; private set; }
        public IReadOnlyList<string> VSlotTokens { get; private set; }

        public static AvatarPartContext FromNode(
            Wz_Node sourceNode,
            AvatarPartType type = AvatarPartType.Unknown,
            AvatarLayerGroup group = AvatarLayerGroup.Character)
        {
            sourceNode = sourceNode.ExtractImageNode();
            int? id = ParseId(sourceNode?.Text);

            Wz_Node info = sourceNode?.FindNodeByPath("info");
            string islot = (info?.FindNodeByPath("islot")).ReadString();
            string vslot = (info?.FindNodeByPath("vslot")).ReadString();

            return new AvatarPartContext(sourceNode, id, type, group, islot, vslot);
        }

        private static int? ParseId(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (name.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - 4);
            }

            int id;
            return Int32.TryParse(name, out id) ? (int?)id : null;
        }
    }

    public sealed class AvatarFramePart
    {
        public AvatarFramePart(AvatarPartContext part, Wz_Node frameNode)
        {
            if (part == null)
            {
                throw new ArgumentNullException("part");
            }
            if (frameNode == null)
            {
                throw new ArgumentNullException("frameNode");
            }

            this.Part = part;
            this.FrameNode = frameNode;
        }

        public AvatarPartContext Part { get; private set; }
        public Wz_Node FrameNode { get; private set; }
    }
}
