using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.CharaSim
{
    public static class ItemStringHelper
    {
        /// <summary>
        /// 获取怪物category属性对应的类型说明。
        /// </summary>
        /// <param Name="category">怪物的category属性的值。</param>
        /// <returns></returns>
        public static string GetMobCategoryName(int category)
        {
            switch (category)
            {
                case 0: return "None";
                case 1: return "Mammal";
                case 2: return "Plant";
                case 3: return "Fish";
                case 4: return "Reptile";
                case 5: return "Spirit";
                case 6: return "Devil";
                case 7: return "Undead";
                case 8: return "Enchanted";
                default: return null;
            }
        }

        public static string GetGearPropString(GearPropType propType, int value)
        {
            return GetGearPropString(propType, value, 0);
        }

        /// <summary>
        /// 获取GearPropType所对应的文字说明。
        /// </summary>
        /// <param Name="propType">表示装备属性枚举GearPropType。</param>
        /// <param Name="Value">表示propType属性所对应的值。</param>
        /// <returns></returns>
        public static string GetGearPropString(GearPropType propType, int value, int signFlag)
        {

            string sign;
            switch (signFlag)
            {
                default:
                case 0: //默认处理符号
                    sign = value > 0 ? "+" : null;
                    break;

                case 1: //固定加号
                    sign = "+";
                    break;

                case 2: //无特别符号
                    sign = "";
                    break;
            }
            switch (propType)
            {
                case GearPropType.incSTR: return "STR : " + sign + value;
                case GearPropType.incSTRr: return "STR : " + sign + value + "%";
                case GearPropType.incDEX: return "DEX : " + sign + value;
                case GearPropType.incDEXr: return "DEX : " + sign + value + "%";
                case GearPropType.incINT: return "INT : " + sign + value;
                case GearPropType.incINTr: return "INT : " + sign + value + "%";
                case GearPropType.incLUK: return "LUK : " + sign + value;
                case GearPropType.incLUKr: return "LUK : " + sign + value + "%";
                case GearPropType.incAllStat: return "All Stats : " + sign + value;
                case GearPropType.incMHP: return "MaxHP : " + sign + value;
                case GearPropType.incMHPr: return "MaxHP : " + sign + value + "%";
                case GearPropType.incMMP: return "MaxMP : " + sign + value;
                case GearPropType.incMMPr: return "MaxMP : " + sign + value + "%";
                case GearPropType.incMDF: return "MaxDF : " + sign + value;
                case GearPropType.incPAD: return "Attack Power: " + sign + value;
                case GearPropType.incPADr: return "Attack Power: " + sign + value + "%";
                case GearPropType.incMAD: return "Magic Attack: " + sign + value;
                case GearPropType.incMADr: return "Magic Attack: " + sign + value + "%";
                case GearPropType.incPDD: return "Defense : " + sign + value;
                case GearPropType.incPDDr: return "Defense : " + sign + value + "%";
                case GearPropType.incSpeed: return "Speed: " + sign + value;
                case GearPropType.incJump: return "Jump: " + sign + value;
                case GearPropType.incCraft: return "Diligence: " + sign + value;
                case GearPropType.damR:
                case GearPropType.incDAMr: return "Damage: " + sign + value + "%";
                case GearPropType.incCr: return "Critical Rate: " + sign + value + "%";
                case GearPropType.knockback: return "Knockback Chance: " + value + "%";
                case GearPropType.incPQEXPr: return "Party Quest EXP: +" + value + "%";
                case GearPropType.incEXPr: return "EXP: " + value + "%";
                case GearPropType.incBDR:
                case GearPropType.bdR: return "Boss Damage: +" + value + "%";
                case GearPropType.incIMDR:
                case GearPropType.imdR: return "Ignored Enemy DEF: +" + value + "%";
                case GearPropType.limitBreak: return "Max Damage: " + value;
                case GearPropType.reduceReq: return "Required Level: -" + value;
                case GearPropType.nbdR: return "Damage Against Normal Monsters: +" + value + "%";

                case GearPropType.only: return value == 0 ? null : "One-of-a-kind item";
                case GearPropType.tradeBlock: return value == 0 ? null : "Untradable";
                case GearPropType.equipTradeBlock: return value == 0 ? null : "Cannot be Traded when equipped";
                case GearPropType.accountSharable: return value == 0 ? null : "Account-bound. Transferable within world.";
                case GearPropType.sharableOnce: return value == 0 ? null : "Tradable once within the same world.\n(Cannot be traded after transfer)";
                case GearPropType.blockGoldHammer: return value == 0 ? null : "Golden Hammer cannot be used.";
                case GearPropType.onlyEquip: return value == 0 ? null : "Unique Equipped Item";
                case GearPropType.notExtend: return value == 0 ? null : "Duration cannot be extended.";
                case GearPropType.tradeAvailable:
                    switch (value)
                    {
                        case 1: return "#cUse the Scissors of Karma to enable this item to be traded one time.#";
                        case 2: return "#cUse the Platinum Scissors of Karma to enable this item to be traded one time.#";
                        default: return null;
                    }
                case GearPropType.accountShareTag:
                    switch (value)
                    {
                        case 1: return "#cUse the Sharing Tag to move an item to another character on the same account once.#";
                        default: return null;
                    }
                case GearPropType.noPotential: return value == 0 ? null : "This item cannot gain Potential.";
                case GearPropType.fixedPotential: return value == 0 ? null : "Potential Reset Not Allowed";
                case GearPropType.superiorEqp: return value == 0 ? null : "Allows you to gain even higher stats with successful item enhancement.";
                case GearPropType.nActivatedSocket: return value == 0 ? null : "#cYou can mount a Nebulite on this item.#";
                case GearPropType.jokerToSetItem: return value == 0 ? null : "#cThis lucky item counts toward any set, so long as you have at least 3 set pieces equipped!#";
                case GearPropType.plusToSetItem: return value == 0 ? null : "#cWhen equipped, the item set will count as having equipped two.#";
                case GearPropType.abilityTimeLimited: return value == 0 ? null : "Time Limited Stats";
                // case GearPropType.colorvar: return value == 0 ? null : "#cè¯¥è£…å¤‡å¯é€šè¿‡æŸ“è‰²é¢œæ–™æ¥å˜æ›´é¢œè‰².#";

                case GearPropType.incMHP_incMMP: return "MaxHP / MaxMP : " + sign + value;
                case GearPropType.incMHPr_incMMPr: return "MaxHP / MaxMP : " + sign + value + "%";
                case GearPropType.incPAD_incMAD: return "Attack Power & Magic ATT: " + sign + value;
                case GearPropType.incPDD_incMDD: return "Defense: " + sign + value;
                case GearPropType.incARC: return "ARC : " + sign + value;
                case GearPropType.incAUT: return "SAC : " + sign + value;
                default: return null;
            }
        }


        public static string GetGearPropDiffString(GearPropType propType, int value, int standardValue)
        {
            var propStr = GetGearPropString(propType, value);
            if (value > standardValue)
            {
                string subfix = null;
                switch (propType)
                {
                    case GearPropType.incSTR:
                    case GearPropType.incDEX:
                    case GearPropType.incINT:
                    case GearPropType.incLUK:
                    case GearPropType.incMHP:
                    case GearPropType.incMMP:
                    case GearPropType.incMDF:
                    case GearPropType.incARC:
                    case GearPropType.incPAD:
                    case GearPropType.incMAD:
                    case GearPropType.incPDD:
                    case GearPropType.incMDD:
                        subfix = $"({standardValue} #$+{value - standardValue}#)"; break;

                    case GearPropType.bdR:
                    case GearPropType.incBDR:
                    case GearPropType.imdR:
                    case GearPropType.incIMDR:
                        subfix = $"({standardValue}% #$+{value - standardValue}%#)"; break;
                }
                propStr = "#$" + propStr + "# " + subfix;
            }
            return propStr;
        }

        /// <summary>
        /// 获取gearGrade所对应的字符串。
        /// </summary>
        /// <param Name="rank">表示装备的潜能等级GearGrade。</param>
        /// <returns></returns>
        public static string GetGearGradeString(GearGrade rank)
        {
            switch (rank)
            {
                case GearGrade.B: return "(Rare Item)";
                case GearGrade.A: return "(Epic Item)";
                case GearGrade.S: return "(Unique Item)";
                case GearGrade.SS: return "(Legendary Item)";
                case GearGrade.Special: return "(Special Item)";
                default: return null;
            }
        }

        /// <summary>
        /// 获取gearType所对应的字符串。
        /// </summary>
        /// <param Name="Type">表示装备类型GearType。</param>
        /// <returns></returns>
        public static string GetGearTypeString(GearType type)
        {
            switch (type)
            {
                case GearType.body: return "Body";
                case GearType.head: return "Head";
                case GearType.face:
                case GearType.face2: return "Face";
                case GearType.hair:
                case GearType.hair2:
                case GearType.hair3: return "Hair";
                case GearType.faceAccessory: return "FACE ACCESSORY";
                case GearType.eyeAccessory: return "EYE ACCESSORY";
                case GearType.earrings: return "EARRINGS";
                case GearType.pendant: return "PENDANT";
                case GearType.belt: return "BELT";
                case GearType.medal: return "MEDAL";
                case GearType.shoulderPad: return "SHOULDER";
                case GearType.cap: return "HAT";
                case GearType.cape: return "CAPE";
                case GearType.coat: return "TOP";
                case GearType.dragonMask: return "Dragon Hat";
                case GearType.dragonPendant: return "Dragon Pendant";
                case GearType.dragonWings: return "Dragon Wing Accessory";
                case GearType.dragonTail: return "Dragon Tail Accessory";
                case GearType.glove: return "GLOVES";
                case GearType.longcoat: return "OVERALL";
                case GearType.machineEngine: return "Mechanic Engine";
                case GearType.machineArms: return "Mechanic Arm";
                case GearType.machineLegs: return "Mechanic Leg";
                case GearType.machineBody: return "Mechanic Frame";
                case GearType.machineTransistors: return "Mechanic Transistor";
                case GearType.pants: return "BOTTOM";
                case GearType.ring: return "RING";
                case GearType.shield: return "SHIELD";
                case GearType.shoes: return "SHOES";
                case GearType.shiningRod: return "Shining Rod";
                case GearType.soulShooter: return "Soul Shooter";
                case GearType.ohSword: return "ONE-HANDED SWORD";
                case GearType.ohAxe: return "ONE-HANDED AXE";
                case GearType.ohBlunt: return "ONE-HANDED MACE";
                case GearType.dagger: return "DAGGER";
                case GearType.katara: return "KATARA";
                case GearType.magicArrow: return "Magic Arrow";
                case GearType.card: return "Card";
                case GearType.box: return "Core";
                case GearType.orb: return "Orb";
                case GearType.novaMarrow: return "Dragon Essence";
                case GearType.soulBangle: return "Soul Ring";
                case GearType.mailin: return "Magnum";
                case GearType.cane: return "Cane";
                case GearType.wand: return "WAND";
                case GearType.staff: return "STAFF";
                case GearType.thSword: return "TWO-HANDED SWORD";
                case GearType.thAxe: return "TWO-HANDED AXE";
                case GearType.thBlunt: return "TWO-HANDED MACE";
                case GearType.spear: return "SPEAR";
                case GearType.polearm: return "POLE ARM";
                case GearType.bow: return "BOW";
                case GearType.crossbow: return "CROSSBOW";
                case GearType.throwingGlove: return "CLAW";
                case GearType.knuckle: return "KNUCKLE";
                case GearType.gun: return "GUN";
                case GearType.android: return "ANDROID";
                case GearType.machineHeart: return "MECHANICAL HEART";
                case GearType.pickaxe: return "Mining Tool";
                case GearType.shovel: return "Herbalism Tool";
                case GearType.pocket: return "POCKET ITEM";
                case GearType.dualBow: return "Dual Bowguns";
                case GearType.handCannon: return "Hand Cannon";
                case GearType.badge: return "BADGE";
                case GearType.emblem: return "EMBLEM";
                case GearType.soulShield: return "Soul Shield";
                case GearType.demonShield: return "Demon Aegis";
                case GearType.totem: return "Totem";
                case GearType.petEquip: return "PET EQUIPMENT";
                case GearType.taming:
                case GearType.taming2:
                case GearType.taming3: 
                case GearType.tamingChair: return "TAMED MONSTER";
                case GearType.saddle: return "SADDLE";
                case GearType.katana: return "KATANA";
                case GearType.fan: return "Fan";
                case GearType.swordZB: return "Heavy Sword";
                case GearType.swordZL: return "Long Sword";
                case GearType.weapon: return "Weapon";
                case GearType.subWeapon: return "Secondary Weapon";
                case GearType.heroMedal: return "Medallions";
                case GearType.rosario: return "Rosary";
                case GearType.chain: return "Iron Chain";
                case GearType.book1:
                case GearType.book2:
                case GearType.book3: return "Magic Book";
                case GearType.bowMasterFeather: return "Arrow Fletching";
                case GearType.crossBowThimble: return "Bow Thimble";
                case GearType.shadowerSheath: return "Dagger Scabbard";
                case GearType.nightLordPoutch: return "Charm";
                case GearType.viperWristband: return "Wrist Band";
                case GearType.captainSight: return "Far Sight";
                case GearType.connonGunPowder:
                case GearType.connonGunPowder2: return "Powder Keg";
                case GearType.aranPendulum: return "Mass";
                case GearType.evanPaper: return "Document";
                case GearType.battlemageBall: return "Magic Marble";
                case GearType.wildHunterArrowHead: return "Arrowhead";
                case GearType.cygnusGem: return "Jewel";
                case GearType.controller: return "Controller";
                case GearType.foxPearl: return "Fox Marble";
                case GearType.chess: return "Chess Piece";
                case GearType.powerSource: return "Power Source";

                case GearType.energySword: return "Whip Blade";
                case GearType.desperado: return "Desperado";
                case GearType.magicStick: return "Beast Tamer Scepter";
                case GearType.whistle:
                case GearType.whistle2: return "Whistle";
                case GearType.boxingClaw: return "Fist";
                case GearType.kodachi:
                case GearType.kodachi2: return "Kodachi";
                case GearType.espLimiter: return "Psy-limiter";

                case GearType.GauntletBuster: return "Arm Cannon";
                case GearType.ExplosivePill: return "Charge";

                case GearType.chain2: return "Chain";
                case GearType.transmitter: return "Warp Forge";
                case GearType.magicGauntlet: return "Lucent Gauntlet";
                case GearType.magicWing: return "Lucent Wings";
                case GearType.pathOfAbyss: return "Abyssal Path";

                case GearType.relic: return "Relic";
                case GearType.ancientBow: return "Ancient Bow";

                case GearType.handFan: return "Ritual Fan";
                case GearType.fanTassel: return "Fan Tassel";

                case GearType.tuner: return "Bladecaster";
                case GearType.bracelet: return "Bladebinder";

                case GearType.breathShooter: return "Whispershot";
                case GearType.weaponBelt: return "Weapon Belt";

                case GearType.boxingCannon: return "拳炮";
                case GearType.boxingSky: return "拳天";

                default: return null;
            }
        }

        /// <summary>
        /// 获取武器攻击速度所对应的字符串。
        /// </summary>
        /// <param Name="attackSpeed">表示武器的攻击速度，通常为2~9的数字。</param>
        /// <returns></returns>
        public static string GetAttackSpeedString(int attackSpeed)
        {
            switch (attackSpeed)
            {
                case 2:
                case 3: return "FASTER";
                case 4:
                case 5: return "FAST";
                case 6: return "NORMAL";
                case 7:
                case 8: return "SLOW";
                case 9: return "SLOW";
                default:
                    if (attackSpeed < 2) return "吃屎一样快";
                    else if (attackSpeed > 9) return "吃屎一样慢";
                    else return attackSpeed.ToString();
            }
        }

        /// <summary>
        /// 获取套装装备类型的字符串。
        /// </summary>
        /// <param Name="Type">表示套装装备类型的GearType。</param>
        /// <returns></returns>
        public static string GetSetItemGearTypeString(GearType type)
        {
            return GetGearTypeString(type);
        }

        /// <summary>
        /// 获取装备额外职业要求说明的字符串。
        /// </summary>
        /// <param Name="Type">表示装备类型的GearType。</param>
        /// <returns></returns>
        public static string GetExtraJobReqString(GearType type)
        {
            switch (type)
            {
                //0xxx
                case GearType.heroMedal: return "Hero only";
                case GearType.rosario: return "Paladin only";
                case GearType.chain: return "Dark Knight only";
                case GearType.book1: return "Fire/Poison Magician branch only";
                case GearType.book2: return "Ice/Lightning Magician branch only";
                case GearType.book3: return "Bishop Magician only";
                case GearType.bowMasterFeather: return "Bow Master only";
                case GearType.crossBowThimble: return "Marksman only";
                case GearType.shadowerSheath: return "Shadower only";
                case GearType.nightLordPoutch: return "Night Lord only";
                case GearType.katara: return "Dual Blade only";
                case GearType.viperWristband: return "Buccaneer only";
                case GearType.captainSight: return "Corsair only";
                case GearType.connonGunPowder:
                case GearType.connonGunPowder2: return "Cannoneer only";
                case GearType.box:
                case GearType.boxingClaw: return "Jett Only";
                case GearType.relic: return "Pathfinder only";

                //1xxx
                case GearType.cygnusGem: return "Cygnus Knights only";

                //2xxx
                case GearType.aranPendulum: return GetExtraJobReqString(21);
                case GearType.evanPaper: return GetExtraJobReqString(22);
                case GearType.magicArrow: return GetExtraJobReqString(23);
                case GearType.card: return GetExtraJobReqString(24);
                case GearType.foxPearl: return GetExtraJobReqString(25);
                case GearType.orb:
                case GearType.shiningRod: return GetExtraJobReqString(27);

                //3xxx
                case GearType.demonShield: return GetExtraJobReqString(31);
                case GearType.desperado: return "Demon Avenger only";
                case GearType.battlemageBall: return "Battle Mage only";
                case GearType.wildHunterArrowHead: return "Wild Hunter only";
                case GearType.mailin: return "Mechanic only";
                case GearType.controller:
                case GearType.energySword: return GetExtraJobReqString(36);
                case GearType.GauntletBuster:
                case GearType.ExplosivePill: return GetExtraJobReqString(37);

                //4xxx
                case GearType.katana:
                case GearType.kodachi:
                case GearType.kodachi2: return GetExtraJobReqString(41);
                case GearType.fan: return GetExtraJobReqString(42);

                //5xxx
                case GearType.soulShield: return GetExtraJobReqString(51);

                //6xxx
                case GearType.novaMarrow: return GetExtraJobReqString(61);
                case GearType.breathShooter:
                case GearType.weaponBelt: return GetExtraJobReqString(63);
                case GearType.chain2:
                case GearType.transmitter: return GetExtraJobReqString(64);
                case GearType.soulBangle:
                case GearType.soulShooter: return GetExtraJobReqString(65);

                //10xxx
                case GearType.swordZB:
                case GearType.swordZL: return GetExtraJobReqString(101);

                case GearType.whistle:
                case GearType.whistle2:
                case GearType.magicStick: return GetExtraJobReqString(112);

                case GearType.espLimiter:
                case GearType.chess: return GetExtraJobReqString(142);

                case GearType.magicGauntlet: 
                case GearType.magicWing: return GetExtraJobReqString(152);

                case GearType.pathOfAbyss: return GetExtraJobReqString(155);

                case GearType.handFan:
                case GearType.fanTassel: return GetExtraJobReqString(164);

                case GearType.tuner:
                case GearType.bracelet: return GetExtraJobReqString(151);

                case GearType.boxingCannon:
                case GearType.boxingSky: return GetExtraJobReqString(175);
                default: return null;
            }
        }

        /// <summary>
        /// 获取装备额外职业要求说明的字符串。
        /// </summary>
        /// <param Name="specJob">表示装备属性的reqSpecJob的值。</param>
        /// <returns></returns>
        public static string GetExtraJobReqString(int specJob)
        {
            switch (specJob)
            {
                case 21: return "Aran only";
                case 22: return "Evan only";
                case 23: return "Mercedes only";
                case 24: return "Phantom only";
                case 25: return "Shade only";
                case 27: return "Luminous only";
                case 31: return "Demon only";
                case 36: return "Xenon only";
                case 37: return "Blaster only";
                case 41: return "Hayato only";
                case 42: return "Kanna only";
                case 51: return "Mihile only";
                case 61: return "Kaiser only";
                case 63: return "Kain only";
                case 64: return "Cadena only";
                case 65: return "Angelic Buster only";
                case 101: return "Zero only";
                case 112: return "Beast Tamer only";
                case 142: return "Kinesis only";
                case 151: return "Adele only";
                case 152: return "Illium only";
                case 155: return "Ark only";
                case 162: return "Lara only";
                case 164: return "Hoyoung only";
                case 175: return "Mo Xuan only";
                default: return null;
            }
        }

        public static string GetItemPropString(ItemPropType propType, int value)
        {
            switch (propType)
            {
                case ItemPropType.tradeBlock:
                    return GetGearPropString(GearPropType.tradeBlock, value);
                case ItemPropType.tradeAvailable:
                    return GetGearPropString(GearPropType.tradeAvailable, value);
                case ItemPropType.only:
                    return GetGearPropString(GearPropType.only, value);
                case ItemPropType.accountSharable:
                    return GetGearPropString(GearPropType.accountSharable, value);
                case ItemPropType.quest:
                    return value == 0 ? null : "Quest Item";
                case ItemPropType.pquest:
                    return value == 0 ? null : "Party Quest Item";
                default:
                    return null;
            }
        }

        public static string GetSkillReqAmount(int skillID, int reqAmount)
        {
            switch (skillID / 10000)
            {
                case 11200: return "[Required Bear Skill Point(s): " + reqAmount + "]";
                case 11210: return "[Required Leopard Skill Point(s): " + reqAmount + "]";
                case 11211: return "[Required Hawk Skill Point(s): " + reqAmount + "]";
                case 11212: return "[Required Cat Skill Point(s): " + reqAmount + "]";
                default: return "[Required Skill Point(s): " + reqAmount + "]";
            }
        }

        public static string GetJobName(int jobCode)
        {
            switch (jobCode)
            {
                case 0: return "Beginner";
                case 100: return "Swordman";
                case 110: return "Fighter";
                case 111: return "Crusader";
                case 112: return "Hero";
                case 113: return "Hero (5th)";
                case 120: return "Page";
                case 121: return "White Knight";
                case 122: return "Paladin";
                case 123: return "Paladin (5th)";
                case 130: return "Spearman";
                case 131: return "Berserker";
                case 132: return "Dark Knight";
                case 133: return "Dark Knight (5th)";
                case 200: return "Magician";
                case 210: return "Wizard (Fire, Poison)";
                case 211: return "Mage (Fire, Poison)";
                case 212: return "Arch Mage (Fire, Poison)";
                case 213: return "Arch Mage (Fire, Poison) (5th)";
                case 220: return "Wizard (Ice, Lightning)";
                case 221: return "Mage (Ice, Lightning)";
                case 222: return "Arch Mage (Ice, Lightning)";
                case 223: return "Arch Mage (Ice, Lightning) (5th)";
                case 230: return "Cleric";
                case 231: return "Priest";
                case 232: return "Bishop";
                case 233: return "Bishop (5th)";
                case 300: return "Archer";
                case 301: return "Archer (Pathfinder)";
                case 310: return "Hunter";
                case 311: return "Ranger";
                case 312: return "Bowmaster";
                case 313: return "Bowmaster (5th)";
                case 320: return "Crossbowman";
                case 321: return "Sniper";
                case 322: return "Marksman";
                case 323: return "Marksman (5th)";
                case 330: return "Ancient Archer";
                case 331: return "Soulchaser";
                case 332: return "Pathfinder";
                case 333: return "Pathfinder (5th)";
                case 400: return "Rogue";
                case 410: return "Assassin";
                case 411: return "Hermit";
                case 412: return "Night Lord";
                case 413: return "Night Lord (5th)";
                case 420: return "Bandit";
                case 421: return "Chief Bandit";
                case 422: return "Shadower";
                case 423: return "Shadower (5th)";
                case 430: return "Blade Recruit";
                case 431: return "Blade Acolyte";
                case 432: return "Blade Specialist";
                case 433: return "Blade Lord";
                case 434: return "Blade Master";
                case 435: return "Blade Master (5th)";
                case 500: return "Pirate";
                case 501: return "Pirate (Cannoneer)";
                case 510: return "Brawler";
                case 511: return "Marauder";
                case 512: return "Buccaneer";
                case 513: return "Buccaneer (5th)";
                case 520: return "Gunslinger";
                case 521: return "Outlaw";
                case 522: return "Corsair";
                case 523: return "Corsair (5th)";
                case 530: return "Cannoneer";
                case 531: return "Cannon Trooper";
                case 532: return "Cannon Master";
                case 533: return "Cannon Master (5th)";

                case 1000: return "Noblesse";
                case 1100: return "Dawn Warrior (1st)";
                case 1110: return "Dawn Warrior (2nd)";
                case 1111: return "Dawn Warrior (3rd)";
                case 1112: return "Dawn Warrior (4th)";
                case 1113: return "Dawn Warrior (5th)";
                case 1200: return "Blaze Wizard (1st)";
                case 1210: return "Blaze Wizard (2nd)";
                case 1211: return "Blaze Wizard (3rd)";
                case 1212: return "Blaze Wizard (4th)";
                case 1213: return "Blaze Wizard (5th)";
                case 1300: return "Wind Archer (1st)";
                case 1310: return "Wind Archer (2nd)";
                case 1311: return "Wind Archer (3rd)";
                case 1312: return "Wind Archer (4th)";
                case 1313: return "Wind Archer (5th)";
                case 1400: return "Night Walker (1st)";
                case 1410: return "Night Walker (2nd)";
                case 1411: return "Night Walker (3rd)";
                case 1412: return "Night Walker (4th)";
                case 1413: return "Night Walker (5th)";
                case 1500: return "Thunder Breaker (1st)";
                case 1510: return "Thunder Breaker (2nd)";
                case 1511: return "Thunder Breaker (3rd)";
                case 1512: return "Thunder Breaker (4th)";
                case 1513: return "Thunder Breaker (5th)";

                case 2000: return "Legend";
                case 2001: return "Evan";
                case 2002: return "Mercedes";
                case 2100: return "Aran (1st)";
                case 2110: return "Aran (2nd)";
                case 2111: return "Aran (3rd)";
                case 2112: return "Aran (4th)";
                case 2113: return "Aran (5th)";
                case 2200: return "Evan (1st)";
                case 2210: return "Evan (2nd, old)";
                case 2211: return "Evan (2nd)";
                case 2212: return "Evan (4th, old)";
                case 2213: return "Evan (5th, old)";
                case 2214: return "Evan (3rd)";
                case 2215: return "Evan (7th, old)";
                case 2216: return "Evan (8th, old)";
                case 2217: return "Evan (4th)";
                case 2218: return "Evan (10th, old)";
                case 2219: return "Evan (5th)";
                case 2300: return "Mercedes (1st)";
                case 2310: return "Mercedes (2nd)";
                case 2311: return "Mercedes (3rd)";
                case 2312: return "Mercedes (4th)";
                case 2313: return "Mercedes (5th)";
                case 2400: return "Phantom (1st)";
                case 2410: return "Phantom (2nd)";
                case 2411: return "Phantom (3rd)";
                case 2412: return "Phantom (4th)";
                case 2413: return "Phantom (5th)";
                case 2500: return "Shade (1st)";
                case 2510: return "Shade (2nd)";
                case 2511: return "Shade (3rd)";
                case 2512: return "Shade (4th)";
                case 2513: return "Shade (5th)";
                case 2700: return "Luminous (1st)";
                case 2710: return "Luminous (2nd)";
                case 2711: return "Luminous (3rd)";
                case 2712: return "Luminous (4th)";
                case 2713: return "Luminous (5th)";


                case 3000: return "Citizen";
                case 3001: return "Demon";
                case 3100: return "Demon Slayer (1st)";
                case 3110: return "Demon Slayer (2nd)";
                case 3111: return "Demon Slayer (3rd)";
                case 3112: return "Demon Slayer (4th)";
                case 3113: return "Demon Slayer (5th)";
                case 3101: return "Demon Avenger (1st)";
                case 3120: return "Demon Avenger (2nd)";
                case 3121: return "Demon Avenger (3rd)";
                case 3122: return "Demon Avenger (4th)";
                case 3123: return "Demon Avenger (5th)";
                case 3200: return "Battle Mage (1st)";
                case 3210: return "Battle Mage (2nd)";
                case 3211: return "Battle Mage (3rd)";
                case 3212: return "Battle Mage (4th)";
                case 3213: return "Battle Mage (5th)";
                case 3300: return "Wild Hunter (1st)";
                case 3310: return "Wild Hunter (2nd)";
                case 3311: return "Wild Hunter (3rd)";
                case 3312: return "Wild Hunter (4th)";
                case 3313: return "Wild Hunter (5th)";
                case 3500: return "Mechanic (1st)";
                case 3510: return "Mechanic (2nd)";
                case 3511: return "Mechanic (3rd)";
                case 3512: return "Mechanic (4th)";
                case 3513: return "Mechanic (5th)";
                case 3700: return "Blaster (1st)";
                case 3710: return "Blaster (2nd)";
                case 3711: return "Blaster (3rd)";
                case 3712: return "Blaster (4th)";
                case 3713: return "Blaster (5th)";
                case 3002: return "Xenon";
                case 3600: return "Xenon (1st)";
                case 3610: return "Xenon (2nd)";
                case 3611: return "Xenon (3rd)";
                case 3612: return "Xenon (4th)";
                case 3613: return "Xenon (5th)";

                case 4001: return "Hayato";
                case 4002: return "Kanna";
                case 4100: return "Hayato (1st)";
                case 4110: return "Hayato (2nd)";
                case 4111: return "Hayato (3rd)";
                case 4112: return "Hayato (4th)";
                case 4113: return "Hayato (5th)";
                case 4200: return "Kanna (1st)";
                case 4210: return "Kanna (2nd)";
                case 4211: return "Kanna (3rd)";
                case 4212: return "Kanna (4th)";
                case 4213: return "Kanna (5th)";


                case 5000: return "Mihile";
                case 5100: return "Mihile (1st)";
                case 5110: return "Mihile (2nd)";
                case 5111: return "Mihile (3rd)";
                case 5112: return "Mihile (4th)";
                case 5113: return "Mihile (5th)";


                case 6000: return "Kaiser";
                case 6100: return "Kaiser (1st)";
                case 6110: return "Kaiser (2nd)";
                case 6111: return "Kaiser (3rd)";
                case 6112: return "Kaiser (4th)";
                case 6113: return "Kaiser (5th)";
                case 6003: return "Kain";
                case 6300: return "Kain (1st)";
                case 6310: return "Kain (2nd)";
                case 6311: return "Kain (3rd)";
                case 6312: return "Kain (4th)";
                case 6313: return "Kain (5th)";
                case 6002: return "Cadena";
                case 6400: return "Cadena (1st)";
                case 6410: return "Cadena (2nd)";
                case 6411: return "Cadena (3rd)";
                case 6412: return "Cadena (4th)";
                case 6413: return "Cadena (5th)";
                case 6001: return "Angelic Buster";
                case 6500: return "Angelic Buster (1st)";
                case 6510: return "Angelic Buster (2nd)";
                case 6511: return "Angelic Buster (3rd)";
                case 6512: return "Angelic Buster (4th)";
                case 6513: return "Angelic Buster (5th)";

                case 10000: return "Zero";
                case 10100: return "Zero";
                case 10110: return "Zero";
                case 10111: return "Zero";
                case 10112: return "Zero";

                case 11000: return "Beast Tamer";
                case 11200: return "Bear Skills";
                case 11210: return "Leopard Skills";
                case 11211: return "Hawk Skills";
                case 11212: return "Cat Skills";

                case 14000: return "Kinesis";
                case 14200: return "Kinesis (1st)";
                case 14210: return "Kinesis (2nd)";
                case 14211: return "Kinesis (3rd)";
                case 14212: return "Kinesis (4th)";
                case 14213: return "Kinesis (5th)";

                case 15000: return "Illium";
                case 15001: return "Ark";
                case 15002: return "Adele";
                case 15100: return "Adele (1st)";
                case 15110: return "Adele (2nd)";
                case 15111: return "Adele (3rd)";
                case 15112: return "Adele (4th)";
                case 15113: return "Adele (5th)";
                case 15200: return "Illium (1st)";
                case 15210: return "Illium (2nd)";
                case 15211: return "Illium (3rd)";
                case 15212: return "Illium (4th)";
                case 15213: return "Illium (5th)";
                case 15500: return "Ark (1st)";
                case 15510: return "Ark (2nd)";
                case 15511: return "Ark (3rd)";
                case 15512: return "Ark (4th)";
                case 15513: return "Ark (5th)";

                case 16001: return "Lara";
                case 16200: return "Lara (1st)";
                case 16210: return "Lara (2nd)";
                case 16211: return "Lara (3rd)";
                case 16212: return "Lara (4th)";
                case 16213: return "Lara (5th)";
                case 16000: return "Hoyoung";
                case 16400: return "Hoyoung (1st)";
                case 16410: return "Hoyoung (2nd)";
                case 16411: return "Hoyoung (3rd)";
                case 16412: return "Hoyoung (4th)";
                case 16413: return "Hoyoung (5th)";
            }
            return null;
        }
    }
}