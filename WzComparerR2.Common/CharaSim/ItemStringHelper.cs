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
                case 0: return "无形态";
                case 1: return "动物型";
                case 2: return "植物型";
                case 3: return "鱼类型";
                case 4: return "爬虫类型";
                case 5: return "精灵型";
                case 6: return "恶魔型";
                case 7: return "不死型";
                case 8: return "无机物型";
                default: return null;
            }
        }

        public static string GetGearPropString(GearPropType propType, long value)
        {
            return GetGearPropString(propType, value, 0);
        }

        /// <summary>
        /// 获取GearPropType所对应的文字说明。
        /// </summary>
        /// <param Name="propType">表示装备属性枚举GearPropType。</param>
        /// <param Name="Value">表示propType属性所对应的值。</param>
        /// <returns></returns>
        public static string GetGearPropString(GearPropType propType, long value, int signFlag)
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
                case GearPropType.incSTR: return "力量 : " + sign + value;
                case GearPropType.incSTRr: return "力量 : " + sign + value + "%";
                case GearPropType.incDEX: return "敏捷 : " + sign + value;
                case GearPropType.incDEXr: return "敏捷 : " + sign + value + "%";
                case GearPropType.incINT: return "智力 : " + sign + value;
                case GearPropType.incINTr: return "智力 : " + sign + value + "%";
                case GearPropType.incLUK: return "运气 : " + sign + value;
                case GearPropType.incLUKr: return "运气 : " + sign + value + "%";
                case GearPropType.incAllStat: return "所有属性 : " + sign + value;
                case GearPropType.incMHP: return "最大血量： " + sign + value;
                case GearPropType.incMHPr: return "最大血量： " + sign + value + "%";
                case GearPropType.incMMP: return "最大魔量： " + sign + value;
                case GearPropType.incMMPr: return "最大魔量： " + sign + value + "%";
                case GearPropType.incMDF: return "MaxDF : " + sign + value;
                case GearPropType.incPAD: return "攻击力 : " + sign + value;
                case GearPropType.incPADr: return "攻击力 : " + sign + value + "%";
                case GearPropType.incMAD: return "魔法力 : " + sign + value;
                case GearPropType.incMADr: return "魔法力 : " + sign + value + "%";
                case GearPropType.incPDD: return "防御力 : " + sign + value;
                case GearPropType.incPDDr: return "物理防御力 : " + sign + value + "%";
                case GearPropType.incMDD: return "魔法防御力 : " + sign + value;
                case GearPropType.incMDDr: return "魔法防御力 : " + sign + value + "%";
                case GearPropType.incACC: return "命中值 : " + sign + value;
                case GearPropType.incACCr: return "命中值 : " + sign + value + "%";
                case GearPropType.incEVA: return "回避值 : " + sign + value;
                case GearPropType.incEVAr: return "回避值 : " + sign + value + "%";
                case GearPropType.incSpeed: return "移动速度 : " + sign + value;
                case GearPropType.incJump: return "跳跃力 : " + sign + value;
                case GearPropType.incCraft: return "手技 : " + sign + value;
                case GearPropType.damR:
                case GearPropType.incDAMr: return "总伤害 : " + sign + value + "%";
                case GearPropType.incCr: return "爆击率 : " + sign + value + "%";
                case GearPropType.incCDr: return "爆击伤害 : " + sign + value + "%";
                case GearPropType.knockback: return "直接攻击时" + value + "的比率发生后退现象。";
                case GearPropType.incPVPDamage: return "大乱斗时追加攻击力" + sign + value;
                case GearPropType.incPQEXPr: return "组队任务经验值增加" + value + "%";
                case GearPropType.incEXPr: return "经验值增加" + value + "%";
                case GearPropType.incBDR:
                case GearPropType.bdR: return "攻击首领怪时，伤害+" + value + "%";
                case GearPropType.incIMDR:
                case GearPropType.imdR: return "无视怪物防御率：+" + value + "%";
                case GearPropType.limitBreak:return "伤害上限突破至" + ToChineseNumberExpr(value) + "。";
                case GearPropType.reduceReq: return "装备等级降低：- " + value;
                case GearPropType.nbdR: return "攻击普通怪物时，伤害+" + value + "%";

                case GearPropType.only: return value == 0 ? null : "固有道具";
                case GearPropType.tradeBlock: return value == 0 ? null : "不可交换";
                case GearPropType.equipTradeBlock: return value == 0 ? null : "装备后无法交换";
                case GearPropType.accountSharable: return value == 0 ? null : "服务器内只有我的角色之间可以移动";
                case GearPropType.onlyEquip: return value == 0 ? null : "固有装备物品";
                case GearPropType.notExtend: return value == 0 ? null : "无法延长有效时间。";
                case GearPropType.accountSharableAfterExchange: return value == 0 ? null : "可交换1次\n（交易后只能在世界内我的角色之间移动）";
                case GearPropType.mintable: return value == 0 ? null : "可铸造";
                case GearPropType.tradeAvailable:
                    switch (value)
                    {
                        case 1: return " #c使用宿命剪刀，可以使物品交易1次。#";
                        case 2: return " #c使用白金宿命剪刀，可以使物品交易1次。#";
                        default: return null;
                    }
                case GearPropType.accountShareTag:
                    switch (value)
                    {
                        case 1: return " #c使用物品共享牌，可以在同一账号内的角色间移动1次。#";
                        default: return null;
                    }
                case GearPropType.noPotential: return value == 0 ? null : "无法设置潜能。";
                case GearPropType.fixedPotential: return value == 0 ? null : "无法重设潜能";
                case GearPropType.superiorEqp: return value == 0 ? null : "道具强化成功时，可以获得更高的效果。";
                case GearPropType.nActivatedSocket: return value == 0 ? null : "#c可以镶嵌星岩#";
                case GearPropType.jokerToSetItem: return value == 0 ? null : " #c当前装备3个以上的所有套装道具中包含的幸运物品！#";
                case GearPropType.abilityTimeLimited: return value == 0 ? null : "限期能力值";
                case GearPropType.blockGoldHammer: return value == 0 ? null : "无法使用黄金锤";
                case GearPropType.colorvar: return value == 0 ? null : "#c该装备可通过染色颜料来变更颜色.#";

                case GearPropType.incMHP_incMMP: return "最大血量/最大魔量：" + sign + value;
                case GearPropType.incMHPr_incMMPr: return "最大血量/最大魔量：" + sign + value + "%";
                case GearPropType.incPAD_incMAD: return "攻击力/魔力：" + sign + value;
                case GearPropType.incPDD_incMDD: return "物理/魔法防御力：" + sign + value;
                case GearPropType.incACC_incEVA: return "命中值/回避值：" + sign + value;

                case GearPropType.incARC: return "神秘之力 : " + sign + value;
                case GearPropType.incAUT: return "原初之力 : " + sign + value;

                case GearPropType.Etuc: return "可进行卓越强化。（最多：" + value + "次）";
                case GearPropType.CuttableCount: return "可使用剪刀：" + value + "次";
                default: return null;
            }
        }


        public static string GetGearPropDiffString(GearPropType propType, int value, int standardValue)
        {
            var propStr = GetGearPropString(propType, value);
            if (value > standardValue)
            {
                string suffix = null;
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
                    case GearPropType.incAUT:
                    case GearPropType.incPAD:
                    case GearPropType.incMAD:
                    case GearPropType.incPDD:
                    case GearPropType.incMDD:
                    case GearPropType.incSpeed:
                    case GearPropType.incJump:
                        suffix = $"({standardValue} #$e+{value - standardValue}#)"; break;
                    case GearPropType.bdR:
                    case GearPropType.incBDR:
                    case GearPropType.imdR:
                    case GearPropType.incIMDR:
                    case GearPropType.damR:
                    case GearPropType.incDAMr:
                        suffix = $"({standardValue}% #$y+{value - standardValue}%#)"; break;
                }
                propStr = "#$y" + propStr + "# " + suffix;
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
                case GearGrade.C: return "C级(一般物品)";
                case GearGrade.B: return "B级(高级物品)";
                case GearGrade.A: return "A级(史诗物品)";
                case GearGrade.S: return "S级(传说物品)";
                case GearGrade.SS: return "SS级(传说极品)";
                case GearGrade.Special: return "(特殊物品)";
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
                case GearType.body: return "纸娃娃(身体)";
                case GearType.head: return "纸娃娃(头部)";
                case GearType.face:
                case GearType.face2: return "纸娃娃(脸型)";
                case GearType.hair:
                case GearType.hair2:
                case GearType.hair3: return "纸娃娃(发型)";
                case GearType.faceAccessory: return "脸饰";
                case GearType.eyeAccessory: return "眼饰";
                case GearType.earrings: return "耳环";
                case GearType.pendant: return "坠子";
                case GearType.belt: return "腰带";
                case GearType.medal: return "勋章";
                case GearType.shoulderPad: return "肩饰";
                case GearType.cap: return "帽子";
                case GearType.cape: return "披风";
                case GearType.coat: return "上衣";
                case GearType.dragonMask: return "龙神帽子";
                case GearType.dragonPendant: return "龙神吊坠";
                case GearType.dragonWings: return "龙神翅膀";
                case GearType.dragonTail: return "龙神尾巴";
                case GearType.glove: return "手套";
                case GearType.longcoat: return "套服";
                case GearType.machineEngine: return "机甲引擎";
                case GearType.machineArms: return "机甲机械臂";
                case GearType.machineLegs: return "机甲机械腿";
                case GearType.machineBody: return "机甲机身材质";
                case GearType.machineTransistors: return "机甲晶体管";
                case GearType.pants: return "裤/裙";
                case GearType.ring: return "戒指";
                case GearType.shield: return "盾牌";
                case GearType.shoes: return "鞋子";
                case GearType.shiningRod: return "双头杖";
                case GearType.soulShooter: return "灵魂手铳";
                case GearType.ohSword: return "单手剑";
                case GearType.ohAxe: return "单手斧";
                case GearType.ohBlunt: return "单手钝器";
                case GearType.dagger: return "短刀";
                case GearType.katara: return "刀";
                case GearType.magicArrow: return "魔法箭矢";
                case GearType.card: return "卡片";
                case GearType.box: return "宝盒";
                case GearType.orb: return "宝珠";
                case GearType.novaMarrow: return "龙之精髓";
                case GearType.soulBangle: return "灵魂手镯";
                case GearType.mailin: return "麦林";
                case GearType.cane: return "手杖";
                case GearType.wand: return "短杖";
                case GearType.staff: return "长杖";
                case GearType.thSword: return "双手剑";
                case GearType.thAxe: return "双手斧";
                case GearType.thBlunt: return "双手钝器";
                case GearType.spear: return "枪";
                case GearType.polearm: return "矛";
                case GearType.bow: return "弓";
                case GearType.crossbow: return "弩";
                case GearType.throwingGlove: return "拳套";
                case GearType.knuckle: return "指节";
                case GearType.gun: return "短枪";
                case GearType.android: return "智能机器人";
                case GearType.machineHeart: return "机械心脏";
                case GearType.pickaxe: return "采矿工具";
                case GearType.shovel: return "采药工具";
                case GearType.pocket: return "口袋物品";
                case GearType.dualBow: return "双弩枪";
                case GearType.handCannon: return "手持火炮";
                case GearType.badge: return "徽章";
                case GearType.emblem: return "纹章";
                case GearType.soulShield: return "灵魂盾";
                case GearType.demonShield: return "精气盾";
                case GearType.totem: return "图腾";
                case GearType.petEquip: return "宠物装备";
                case GearType.taming:
                case GearType.taming2:
                case GearType.taming3: 
                case GearType.tamingChair: return "骑兽";
                case GearType.saddle: return "鞍子";
                case GearType.katana: return "武士刀";
                case GearType.fan: return "折扇";
                case GearType.swordZB: return "大剑";
                case GearType.swordZL: return "太刀";
                case GearType.weapon: return "武器";
                case GearType.subWeapon: return "辅助武器";
                case GearType.heroMedal: return "吊坠";
                case GearType.rosario: return "念珠";
                case GearType.chain: return "铁链";
                case GearType.book1:
                case GearType.book2:
                case GearType.book3: return "魔导书";
                case GearType.bowMasterFeather: return "箭羽";
                case GearType.crossBowThimble: return "扳指";
                case GearType.shadowerSheath: return "短剑剑鞘";
                case GearType.nightLordPoutch: return "护身符";
                case GearType.viperWristband: return "手腕护带";
                case GearType.captainSight: return "瞄准器";
                case GearType.connonGunPowder: 
                case GearType.connonGunPowder2: return "火药桶";
                case GearType.aranPendulum: return "砝码";
                case GearType.evanPaper: return "文件";
                case GearType.battlemageBall: return "魔法球";
                case GearType.wildHunterArrowHead: return "箭轴";
                case GearType.cygnusGem: return "宝石";
                case GearType.controller: return "控制器";
                case GearType.foxPearl: return "狐狸珠";
                case GearType.chess: return "棋子";
                case GearType.powerSource: return "能源";

                case GearType.energySword: return "能量剑";
                case GearType.desperado: return "亡命剑";
                case GearType.magicStick: return "记忆长杖";
                case GearType.whistle: return "飞越";
                case GearType.boxingClaw: return "拳爪";
                case GearType.katana2: return "小太刀";
                case GearType.espLimiter: return "ESP限制器";

                case GearType.GauntletBuster: return "机甲手枪";
                case GearType.ExplosivePill: return "装弹";

                case GearType.chain2: return "锁链";
                case GearType.magicGauntlet: return "魔力手套";
                case GearType.transmitter: return "武器传送装置";
                case GearType.magicWing: return "魔法之翼";
                case GearType.pathOfAbyss: return "深渊精气珠";

                case GearType.relic: return "遗物";
                case GearType.ancientBow: return "远古弓";

                case GearType.handFan: return "扇子";
                case GearType.fanTassel: return "扇坠";

                case GearType.tuner: return "调谐器";
                case GearType.bracelet: return "手链";

                case GearType.boxingCannon: return "拳封";
                case GearType.boxingSky: return "拳天";

                case GearType.breathShooter: return "龙息臂箭";
                case GearType.weaponBelt: return "武器腰带";

                case GearType.ornament: return "饰品";

                case GearType.chakram: return "环刃";
                case GearType.hexSeeker: return "索魂器";

                case GearType.jewel: return "珠宝";

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
                case 3: return "极快";
                case 4:
                case 5: return "快";
                case 6: return "普通";
                case 7:
                case 8: return "缓慢";
                case 9: return "较慢";
                default:
                    return attackSpeed.ToString();
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
                case GearType.heroMedal: return "英雄职业群可穿戴装备";
                case GearType.rosario: return "圣骑士职业群可穿戴装备";
                case GearType.chain: return "黑骑士职业群可穿戴装备";
                case GearType.book1: return "火毒系列魔法师可穿戴装备";
                case GearType.book2: return "冰雷系列魔法师可穿戴装备";
                case GearType.book3: return "主教系列魔法师可穿戴装备";
                case GearType.bowMasterFeather: return "神射手职业群可穿戴装备";
                case GearType.crossBowThimble: return "箭神职业群可穿戴装备";
                case GearType.shadowerSheath: return "侠盗职业群可穿戴装备";
                case GearType.nightLordPoutch: return "隐士职业群可穿戴装备";
                case GearType.katara: return "暗影双刀可穿戴装备";
                case GearType.viperWristband: return "冲锋队长职业群可穿戴装备";
                case GearType.captainSight: return "船长职业群可穿戴装备";
                case GearType.connonGunPowder: 
                case GearType.connonGunPowder2: return "火炮手职业群可穿戴装备";
                case GearType.box:
                case GearType.boxingClaw: return "龙的传人可穿戴装备";
                case GearType.relic: return "古迹猎人职业群可穿戴装备";

                //1xxx
                case GearType.cygnusGem: return "冒险骑士团可穿戴装备";

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
                case GearType.desperado: return "恶魔复仇者可穿戴装备";
                case GearType.battlemageBall: return "唤灵斗师职业群可穿戴装备";
                case GearType.wildHunterArrowHead: return "豹弩游侠职业群可穿戴装备";
                case GearType.mailin: return "机械师可穿戴装备";
                case GearType.controller:
                case GearType.energySword: return GetExtraJobReqString(36);
                case GearType.GauntletBuster:
                case GearType.ExplosivePill: return GetExtraJobReqString(37);

                //4xxx
                case GearType.katana:
                case GearType.katana2: return "剑豪可穿戴装备";
                case GearType.fan: return "阴阳师可穿戴装备";

                //5xxx
                case GearType.soulShield: return "米哈尔可穿戴装备";

                //6xxx
                case GearType.novaMarrow: return GetExtraJobReqString(61);
                case GearType.weaponBelt:
                case GearType.breathShooter: return GetExtraJobReqString(63);
                case GearType.chain2:
                case GearType.transmitter: return GetExtraJobReqString(64);
                case GearType.soulBangle:
                case GearType.soulShooter: return GetExtraJobReqString(65);

                //10xxx
                case GearType.swordZB:
                case GearType.swordZL: return GetExtraJobReqString(101);

                case GearType.whistle:
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

                case GearType.ornament: return GetExtraJobReqString(162);

                case GearType.chakram:
                case GearType.hexSeeker: return GetExtraJobReqString(154);
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
                case 21: return "战神可穿戴装备";
                case 22: return "龙神职业群可穿戴装备";
                case 23: return "双弩精灵可穿戴装备";
                case 24: return "幻影可穿戴装备";
                case 25: return "隐月可穿戴装备";
                case 27: return "夜光法师可穿戴装备";
                case 31: return "恶魔猎手可穿戴装备";
                case 36: return "尖兵可穿戴装备";
                case 37: return "爆破手可使用";
                case 41: return "剑豪可穿戴装备";
                case 42: return "阴阳师可穿戴装备";
                case 51: return "米哈尔可穿戴装备";
                case 61: return "狂龙战士可穿戴装备";
                case 63: return "炼狱黑客可穿戴装备";
                case 64: return "魔链影士可穿戴装备";
                case 65: return "爆莉萌天使可穿戴装备";
                case 101: return "神之子可穿戴装备";
                case 112: return "琳可穿戴装备";
                case 142: return "超能力者可穿戴装备";
                case 151: return "御剑骑士可穿戴装备";
                case 152: return "圣晶使徒可穿戴装备";
                case 154: return "飞刃沙士可穿戴装备";
                case 155: return "影魂异人可穿戴装备";
                case 162: return "元素师可穿戴装备";
                case 164: return "虎影可穿戴装备";
                case 175: return "墨玄可穿戴装备";
                default: return null;
            }
        }

        public static string GetExtraJobReqString(IEnumerable<int> specJobs)
        {
            List<string> extraJobNames = new List<string>();
            foreach (int specJob in specJobs)
            {
                switch (specJob)
                {
                    case 1: extraJobNames.AddRange(new[] { "英雄", "圣骑士" }); break;
                    case 2: extraJobNames.AddRange(new[] { "冰雷魔导师", "火毒魔导师", "主教" }); break;
                    case 4: extraJobNames.Add("侠盗"); break;
                    case 11: extraJobNames.Add("魂骑士"); break;
                    case 12: extraJobNames.Add("炎术士"); break;
                    case 22: extraJobNames.Add("龙神"); break;
                    case 32: extraJobNames.Add("唤灵斗师"); break;
                    case 172: extraJobNames.Add("森林小主"); break;
                    default: extraJobNames.Add(specJob.ToString()); break;
                }
            }
            if (extraJobNames.Count == 0)
            {
                return null;
            }
            return string.Join("、", extraJobNames) + "可穿戴装备";
        }

        public static string GetItemPropString(ItemPropType propType, long value)
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
                case ItemPropType.accountSharableAfterExchange:
                    return GetGearPropString(GearPropType.accountSharableAfterExchange, value);
                case ItemPropType.quest:
                    return value == 0 ? null : "任务道具";
                case ItemPropType.pquest:
                    return value == 0 ? null : "组队任务道具";
                case ItemPropType.permanent:
                    return value == 0 ? null : "可以一直使用魔法的神奇宠物。";
                case ItemPropType.mintable:
                    return GetGearPropString(GearPropType.mintable, value);
                default:
                    return null;
            }
        }

        public static string GetSkillReqAmount(int skillID, int reqAmount)
        {
            switch (skillID / 10000)
            {
                case 11200: return "[需要巨熊技能点: " + reqAmount + "]";
                case 11210: return "[需要雪豹技能点: " + reqAmount + "]";
                case 11211: return "[需要猛禽技能点: " + reqAmount + "]";
                case 11212: return "[需要猫咪技能点: " + reqAmount + "]";
                default: return "[需要？？技能点: " + reqAmount + "]";
            }
        }

        public static string GetJobName(int jobCode)
        {
            switch (jobCode)
            {
                case 0: return "新手";
                case 100: return "战士";
                case 110: return "剑客";
                case 111: return "勇士";
                case 112: return "英雄";
                case 120: return "准骑士";
                case 121: return "骑士";
                case 122: return "圣骑士";
                case 130: return "枪战士";
                case 131: return "龙骑士";
                case 132: return "黑骑士";
                case 200: return "魔法师";
                case 210: return "法师（火，毒）";
                case 211: return "巫师（火，毒）";
                case 212: return "魔导师（火，毒）";
                case 220: return "法师（冰，雷）";
                case 221: return "巫师（冰，雷）";
                case 222: return "魔导师（冰，雷）";
                case 230: return "牧师";
                case 231: return "祭司";
                case 232: return "主教";
                case 300: return "弓箭手";
                case 310: return "猎人";
                case 311: return "射手";
                case 312: return "神射手";
                case 320: return "弩弓手";
                case 321: return "游侠";
                case 322: return "箭神";
                case 400: return "飞侠";
                case 410: return "刺客";
                case 411: return "无影人";
                case 412: return "隐士";
                case 420: return "侠客";
                case 421: return "独行客";
                case 422: return "侠盗";
                case 430: return "见习刀客";
                case 431: return "双刀客";
                case 432: return "双刀侠";
                case 433: return "血刀";
                case 434: return "暗影双刀";
                case 500: return "海盗";
                case 501: return "海盗(炮手)";
                case 510: return "拳手";
                case 511: return "斗士";
                case 512: return "冲锋队长";
                case 520: return "火枪手";
                case 521: return "大副";
                case 522: return "船长";
                case 530: return "火炮手";
                case 531: return "毁灭炮手";
                case 532: return "神炮王";

                case 1000: return "初心者";
                case 1100:
                case 1110:
                case 1111:
                case 1112: return "魂骑士";
                case 1200:
                case 1210:
                case 1211:
                case 1212: return "炎术士";
                case 1300:
                case 1310:
                case 1311:
                case 1312: return "风灵使者";
                case 1400:
                case 1410:
                case 1411:
                case 1412: return "夜行者";
                case 1500:
                case 1510:
                case 1511:
                case 1512: return "奇袭者";

                case 2000: return "战童";
                case 2001: return "小不点";
                case 2002: return "双弩精灵";
                case 2100: return "战神(1次)";
                case 2110: return "战神(2次)";
                case 2111: return "战神(3次)";
                case 2112: return "战神(4次)";
                case 2200: return "龙神(1次)";
                case 2210: return "龙神(2次)";
                case 2211: return "龙神(3次)";
                case 2212: return "龙神(4次)";
                case 2213: return "龙神(5次)";
                case 2214: return "龙神(6次)";
                case 2215: return "龙神(7次)";
                case 2216: return "龙神(8次)";
                case 2217: return "龙神(9次)";
                case 2218: return "龙神(10次)";
                case 2300: return "双弩精灵(1次)";
                case 2310: return "双弩精灵(2次)";
                case 2311: return "双弩精灵(3次)";
                case 2312: return "双弩精灵(4次)";
                case 2400: return "幻影(1次)";
                case 2410: return "幻影(2次)";
                case 2411: return "幻影(3次)";
                case 2412: return "幻影(4次)";
                case 2700: return "夜光(1次)";
                case 2710: return "夜光(2次)";
                case 2711: return "夜光(3次)";
                case 2712: return "夜光(4次)";


                case 3000: return "预备兵";
                case 3001:
                case 3100: return "恶魔猎手(1次)";
                case 3110: return "恶魔猎手(2次)";
                case 3111: return "恶魔猎手(3次)";
                case 3112: return "恶魔猎手(4次)";
                case 3101: return "恶魔复仇者(1次)";
                case 3120: return "恶魔复仇者(2次)";
                case 3121: return "恶魔复仇者(3次)";
                case 3122: return "恶魔复仇者(4次)";
                case 3200: return "唤灵斗师(1次)";
                case 3210: return "唤灵斗师(2次)";
                case 3211: return "唤灵斗师(3次)";
                case 3212: return "唤灵斗师(4次)";
                case 3300: return "豹弩游侠(1次)";
                case 3310: return "豹弩游侠(2次)";
                case 3311: return "豹弩游侠(3次)";
                case 3312: return "豹弩游侠(4次)";
                case 3500: return "机械师(1次)";
                case 3510: return "机械师(2次)";
                case 3511: return "机械师(3次)";
                case 3512: return "机械师(4次)";
                case 3002: return "尖兵";
                case 3600: return "尖兵(1次)";
                case 3610: return "尖兵(2次)";
                case 3611: return "尖兵(3次)";
                case 3612: return "尖兵(4次)";

                case 4001: return "剑豪";
                case 4002: return "阴阳师";
                case 4100: return "剑豪(1次)";
                case 4110: return "剑豪(2次)";
                case 4111: return "剑豪(3次)";
                case 4112: return "剑豪(4次)";
                case 4200: return "阴阳师(1次)";
                case 4210: return "阴阳师(2次)";
                case 4211: return "阴阳师(3次)";
                case 4212: return "阴阳师(4次)";


                case 5000: return "无名少年";
                case 5100: return "米哈尔(1次)";
                case 5110: return "米哈尔(2次)";
                case 5111: return "米哈尔(3次)";
                case 5112: return "米哈尔(4次)";


                case 6000: return "狂龙战士";
                case 6100: return "狂龙战士(1次)";
                case 6110: return "狂龙战士(2次)";
                case 6111: return "狂龙战士(3次)";
                case 6112: return "狂龙战士(4次)";
                case 6001: return "爆莉萌天使";
                case 6500: return "爆莉萌天使(1次)";
                case 6510: return "爆莉萌天使(2次)";
                case 6511: return "爆莉萌天使(3次)";
                case 6512: return "爆莉萌天使(4次)";

                case 10000: return "神之子";
                case 10100: return "神之子(1次)";
                case 10110: return "神之子(2次)";
                case 10111: return "神之子(3次)";
                case 10112: return "神之子(4次)";

                case 11000: return "林之灵";
                case 11200: return "林之灵(1次)";
                case 11210: return "林之灵(2次)";
                case 11211: return "林之灵(3次)";
                case 11212: return "林之灵(4次)";

                case 14000: return "超能力者";
                case 14200: return "超能力者(1次)";
                case 14210: return "超能力者(2次)";
                case 14211: return "超能力者(3次)";
                case 14212: return "超能力者(4次)";
            }
            return null;
        }

        private static string ToChineseNumberExpr(long value)
        {
            var sb = new StringBuilder(16);
            bool firstPart = true;
            if (value < 0)
            {
                sb.Append("-");
                value = -value; // just ignore the exception -2147483648
            }
            if (value >= 1_0000_0000)
            {
                long part = value / 1_0000_0000;
                sb.AppendFormat("{0}亿", part);
                value -= part * 1_0000_0000;
                firstPart = false;
            }
            if (value >= 1_0000)
            {
                long part = value / 1_0000;
                sb.Append(firstPart ? null : " ");
                sb.AppendFormat("{0}万", part);
                value -= part * 1_0000;
                firstPart = false;
            }
            if (value > 0)
            {
                sb.Append(firstPart ? null : " ");
                sb.AppendFormat("{0}", value);
            }

            return sb.Length > 0 ? sb.ToString() : "0";
        }
    }
}
