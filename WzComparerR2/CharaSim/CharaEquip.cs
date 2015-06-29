using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.CharaSim
{
    public class CharaEquip
    {
        public CharaEquip()
        {
            int slotsCount = 66;
            gearSlots = new Gear[slotsCount];
            cashGearSlots = new Gear[slotsCount];
        }

        public const int RingCount = 6;
        public const int PendantCount = 2;

        private Gear[] gearSlots;
        private Gear[] cashGearSlots;


        public Gear[] GearSlots
        {
            get { return gearSlots; }
        }

        public Gear[] CashGearSlots
        {
            get { return cashGearSlots; }
        }

        public int GetGearSlot(GearType type, int index)
        {
            switch (type)
            {
                //line 0
                case GearType.badge: return 0;
                case GearType.cap: return 1;
                case GearType.ring:
                    switch (index)
                    {
                        case 0: return 8;
                        case 1: return 9;
                        case 2: return 23;
                        case 3: return 24;
                        case 4: return 2;
                        case 5: return 7;
                        default: return -1;
                    }
                case GearType.android: return 3;
                case GearType.machineHeart: return 4;
                //line 1
                case GearType.medal: return 5;
                case GearType.faceAccessory: return 6;
                //line 2
                case GearType.pocket: return 10;
                case GearType.eyeAccessory: return 11;
                case GearType.pendant:
                    switch (index)
                    {
                        case 0: return 17;
                        case 1: return 12;
                        default: return -1;
                    }
                case GearType.earrings: return 13;
                case GearType.shoulderPad: return 14;
                //line 3
                case GearType.cape: return 15;
                case GearType.coat:
                case GearType.longcoat: return 16;
                default:
                    if (Gear.IsLeftWeapon(type) || Gear.IsDoubleHandWeapon(type))
                        return 18;
                    else if (Gear.IsSubWeapon(type))
                        return 19;
                    else
                        return -1;
                //line 4
                case GearType.glove: return 20;
                case GearType.pants: return 21;
                case GearType.belt: return 22;
                //line 5
                case GearType.shoes: return 27;
                //dragon
                case GearType.dragonMask: return 35;
                case GearType.dragonPendant: return 36;
                case GearType.dragonWings: return 37;
                case GearType.dragonTail: return 38;
                //machine
                case GearType.machineTransistors: return 39;
                case GearType.machineEngine: return 40;
                case GearType.machineBody: return 41;
                case GearType.machineArms: return 42;
                case GearType.machineLegs: return 43;
                //totem
                case GearType.totem:
                    switch (index)
                    {
                        case 0: return 44;
                        case 1: return 45;
                        case 2: return 46;
                        default: return -1;
                    }
            }
        }

        public IEnumerable<Gear> GearsEquiped
        {
            get
            {
                foreach (Gear gear in gearSlots)
                {
                    if (gear != null)
                        yield return gear;
                }
                foreach (Gear gear in cashGearSlots)
                {
                    if (gear != null)
                        yield return gear;
                }
            }
        }

        public bool AddGear(Gear gear, out Gear[] removedGears)
        {
            if (gear == null)
            {
                removedGears = new Gear[0];
                return false;
            }
            int emptyIdx = GetEmptySlotIndex(gear.type, gear.Cash);
            return AddGear(gear, emptyIdx, out removedGears);
        }

        public bool AddGear(Gear gear, int index, out Gear[] removedGears)
        {
            if (gear == null)
            {
                removedGears = new Gear[0];
                return false;
            }
            int slotIdx = GetGearSlot(gear.type, index);
            if (slotIdx == -1)
            {
                removedGears = new Gear[0];
                return false;
            }
            List<Gear> removedGearList = new List<Gear>();

            Gear[] slotList = gear.Cash ? cashGearSlots : gearSlots;
            if (slotList[slotIdx] != null) //移除同槽
            {
                removedGearList.Add(slotList[slotIdx]);
            }
            slotList[slotIdx] = gear; //装备上
            Gear preRemove = getPreRemoveGears(gear.type, gear.Cash);
            if (preRemove != null) //移除冲突装备
            {
                removedGearList.Add(preRemove);
            }

            removedGears = removedGearList.ToArray();
            return true;
        }

        public int GetEmptySlotIndex(GearType gearType, bool cash)
        {
            Gear[] slotList = cash ? cashGearSlots : gearSlots;
            int max;
            switch (gearType)
            {
                case GearType.ring: max = RingCount; break;
                case GearType.pendant: max = PendantCount; break;
                default: return 0;
            }
            for (int i = 0; i < max; i++)
            {
                if (slotList[GetGearSlot(gearType, i)] == null)
                    return i;
            }
            return 0;
        }

        private Gear getPreRemoveGears(GearType newGearType, bool cash)
        {
            Gear[] slotList = cash ? cashGearSlots : gearSlots;

            if (Gear.IsDoubleHandWeapon(newGearType)) //双手 移除副手
            {
                Gear gear = slotList[GetGearSlot(GearType.shield, 0)];
                if (gear != null)
                    return gear;
            }
            else if (Gear.IsSubWeapon(newGearType)) //副手 移除双手
            {
                Gear gear = slotList[GetGearSlot(GearType.ohSword, 0)];
                if (gear != null
                    && (Gear.IsDoubleHandWeapon(gear.type)
                    || (newGearType != GearType.magicArrow && gear.type == GearType.dualBow))) //非魔法箭 移除双弓的主手
                    return gear;
            }
            else if (newGearType == GearType.dualBow) //双弩 移除非魔法箭的副手
            {
                Gear gear = slotList[GetGearSlot(GearType.magicArrow, 0)];
                if (gear != null && gear.type != GearType.magicArrow)
                {
                    return gear;
                }
            }
            else if (newGearType == GearType.pants) //下装 移除套服
            {
                Gear gear = slotList[GetGearSlot(GearType.longcoat, 0)];
                if (gear != null && gear.type == GearType.longcoat)
                {
                    return gear;
                }
            }
            else if (newGearType == GearType.pants) //套服 移除下装
            {
                Gear gear = slotList[GetGearSlot(GearType.pants, 0)];
                if (gear != null && gear.type == GearType.pants)
                {
                    return gear;
                }
            }

            return null;
        }
    }
}
