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
                case 0: return "無形态";
                case 1: return "動物型";
                case 2: return "植物型";
                case 3: return "魚類型";
                case 4: return "爬蟲類型";
                case 5: return "精靈型";
                case 6: return "惡魔型";
                case 7: return "不死型";
                case 8: return "無機物型";
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
                case GearPropType.incSTR: return "力量 : " + sign + value;
                case GearPropType.incSTRr: return "力量 : " + sign + value + "%";
                case GearPropType.incDEX: return "敏捷 : " + sign + value;
                case GearPropType.incDEXr: return "敏捷 : " + sign + value + "%";
                case GearPropType.incINT: return "智力 : " + sign + value;
                case GearPropType.incINTr: return "智力 : " + sign + value + "%";
                case GearPropType.incLUK: return "幸運 : " + sign + value;
                case GearPropType.incLUKr: return "幸運 : " + sign + value + "%";
                case GearPropType.incAllStat: return "全部属性 : " + sign + value;
                case GearPropType.statR: return "全部属性 : " + sign + value + "%";
                case GearPropType.incMHP: return "MaxHP : " + sign + value;
                case GearPropType.incMHPr: return "MaxHP : " + sign + value + "%";
                case GearPropType.incMMP: return "MaxMP : " + sign + value;
                case GearPropType.incMMPr: return "MaxMP : " + sign + value + "%";
                case GearPropType.incMDF: return "MaxDF : " + sign + value;
                case GearPropType.incPAD: return "攻擊力 : " + sign + value;
                case GearPropType.incPADr: return "攻擊力 : " + sign + value + "%";
                case GearPropType.incMAD: return "魔法攻擊力 : " + sign + value;
                case GearPropType.incMADr: return "魔法攻擊力 : " + sign + value + "%";
                case GearPropType.incPDD: return "物理防禦力 : " + sign + value;
                case GearPropType.incPDDr: return "物理防禦力 : " + sign + value + "%";
                //case GearPropType.incMDD: return "魔法防禦力 : " + sign + value;
                //case GearPropType.incMDDr: return "魔法防禦力 : " + sign + value + "%";
                //case GearPropType.incACC: return "命中值 : " + sign + value;
                //case GearPropType.incACCr: return "命中值 : " + sign + value + "%";
                //case GearPropType.incEVA: return "回避值 : " + sign + value;
                //case GearPropType.incEVAr: return "回避值 : " + sign + value + "%";
                case GearPropType.incSpeed: return "移動速度 : " + sign + value;
                case GearPropType.incJump: return "跳躍力 : " + sign + value;
                case GearPropType.incCraft: return "手藝 : " + sign + value;
                case GearPropType.damR:
                case GearPropType.incDAMr: return "總傷害 : " + sign + value + "%";
                case GearPropType.incCr: return "爆擊率 : " + sign + value + "%";
                case GearPropType.knockback: return "直接攻擊時,以 " + value + "%的機率強弓";
                case GearPropType.incPVPDamage: return "大亂鬥時追加攻擊力" + sign + value;
                case GearPropType.incPQEXPr: return "组隊任務經驗值增加" + value + "%";
                case GearPropType.incEXPr: return "經驗值增加" + value + "%";
                case GearPropType.incBDR:
                case GearPropType.bdR: return "攻擊BOSS怪物時傷害 +" + value + "%";
                case GearPropType.incIMDR:
                case GearPropType.imdR: return "無視怪物防禦率：+" + value + "%";
                //case GearPropType.limitBreak: return "傷害上限突破至" + value + "。";
                case GearPropType.reduceReq: return "裝備等级降低：- " + value;
                case GearPropType.nbdR: return "攻擊一般怪物時傷害+" + value + "%";

                case GearPropType.only: return value == 0 ? null : "專屬道具";
                case GearPropType.tradeBlock: return value == 0 ? null : "無法交換";
                case GearPropType.equipTradeBlock: return value == 0 ? null : "裝備後無法交換";
                case GearPropType.accountSharable: return value == 0 ? null : "只能在同帳號內移動";
                case GearPropType.sharableOnce: return value == 0 ? null : "可以在帳號內移動一次(移動後無法交換)";
                case GearPropType.onlyEquip: return value == 0 ? null : "只能單獨使用";
                case GearPropType.notExtend: return value == 0 ? null : "無法延長有效時間。";
                case GearPropType.tradeAvailable:
                    switch (value)
                    {
                        case 1: return "若使用 #c宿命剪刀，該道具可進行一次交易！#";
                        case 2: return "若使用 #c白金神奇剪刀，該道具可進行一次交易！#";
                        default: return null;
                    }
                case GearPropType.accountShareTag:
                    switch (value)
                    {
                        case 1: return " #c若使用分享名牌技術，可以相同帳號內的角色進行移動一次。#";
                        default: return null;
                    }
                case GearPropType.noPotential: return value == 0 ? null : "無法設置潛能。";
                case GearPropType.fixedPotential: return value == 0 ? null : "無法重設潛能";
                case GearPropType.superiorEqp: return value == 0 ? null : "道具強化成功時, 可以獲得更高效果。";
                case GearPropType.nActivatedSocket: return value == 0 ? null : "#c可以鑲嵌星岩#";
                case GearPropType.jokerToSetItem: return value == 0 ? null : " #c當前裝備3個以上的所有套装道具中包含的幸運物品！#";
                case GearPropType.abilityTimeLimited: return value == 0 ? null : "期間限定能力值";
                case GearPropType.blockGoldHammer: return value == 0 ? null : "無法使用黄金鐵鎚";
                case GearPropType.colorvar: return value == 0 ? null : "#c此裝備可以通過染色顏料進行染色。#";

                case GearPropType.incMHP_incMMP: return "MaxHP/MaxMP：" + sign + value;
                case GearPropType.incMHPr_incMMPr: return "MaxHP/MaxMP：" + sign + value + "%";
                case GearPropType.incPAD_incMAD: 
                case GearPropType.incAD: return "攻擊/魔法攻擊力：" + sign + value;
                case GearPropType.incPDD_incMDD: return "物理防禦力：" + sign + value;
                //case GearPropType.incACC_incEVA: return "命中值/回避值：" + sign + value;
                case GearPropType.incARC: return "ARC : " + sign + value;
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
                    case GearPropType.incPAD:
                    case GearPropType.incMAD:
                    case GearPropType.incPDD:
                    case GearPropType.incMDD:
                    case GearPropType.incSpeed:
                    case GearPropType.incJump:
                    case GearPropType.incARC:
                        subfix = $"({standardValue} #$+{value - standardValue}#)"; break;

                    case GearPropType.bdR:
                    case GearPropType.incBDR:
                    case GearPropType.imdR:
                    case GearPropType.incIMDR:
                    case GearPropType.damR:
                    case GearPropType.incDAMr:
                    case GearPropType.statR:
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
                //case GearGrade.C: return "(一般物品)";
                case GearGrade.B: return "(特殊道具)";
                case GearGrade.A: return "(稀有道具)";
                case GearGrade.S: return "(罕見道具)";
                case GearGrade.SS: return "(傳說道具)";
                case GearGrade.Special: return "(特殊道具)";
                default: return null;
            }
        }

        /// <summary>
        /// 获取gearType所对应的字符串。
        /// </summary>
        /// <param Name="Type">表示裝備类型GearType。</param>
        /// <returns></returns>
        public static string GetGearTypeString(GearType type)
        {
            switch (type)
            {
                //case GearType.body: return "纸娃娃(身體)";
                //case GearType.head: return "纸娃娃(頭部)";
                //case GearType.face: return "纸娃娃(臉型)";
                //case GearType.hair:
                //case GearType.hair2: return "紙娃娃(髮型)";
                case GearType.faceAccessory: return "臉飾";
                case GearType.eyeAccessory: return "眼飾";
                case GearType.earrings: return "耳環";
                case GearType.pendant: return "墜飾";
                case GearType.belt: return "腰帶";
                case GearType.medal: return "勳章";
                case GearType.shoulderPad: return "肩膀裝飾";
                case GearType.cap: return "帽子";
                case GearType.cape: return "披風";
                case GearType.coat: return "上衣";
                case GearType.dragonMask: return "龍魔頭盔";
                case GearType.dragonPendant: return "龍魔項鍊";
                case GearType.dragonWings: return "龍魔翅膀";
                case GearType.dragonTail: return "龍魔尾巴";
                case GearType.glove: return "手套";
                case GearType.longcoat: return "套服";
                case GearType.machineEngine: return "戰神引擎";
                case GearType.machineArms: return "戰神手臂";
                case GearType.machineLegs: return "戰神腿部";
                case GearType.machineBody: return "戰神身軀";
                case GearType.machineTransistors: return "戰神電晶體";
                case GearType.pants: return "褲/裙";
                case GearType.ring: return "戒指";
                case GearType.shield: return "盾牌";
                case GearType.shoes: return "鞋子";
                case GearType.shiningRod: return "閃亮克魯";
                case GearType.soulShooter: return "靈魂射手";
                case GearType.ohSword: return "單手劍";
                case GearType.ohAxe: return "單手斧";
                case GearType.ohBlunt: return "單手棍";
                case GearType.dagger: return "短劍";
                case GearType.katara: return "雙刀";
                case GearType.magicArrow: return "魔法箭";
                case GearType.card: return "卡牌";
                case GearType.box: return "寶盒";
                case GearType.orb: return "夜光彈";
                case GearType.novaMarrow: return "龍之精水";
                case GearType.soulBangle: return "靈魂之環";
                case GearType.mailin: return "連發槍";
                case GearType.cane: return "手杖";
                case GearType.wand: return "短杖";
                case GearType.staff: return "長杖";
                case GearType.thSword: return "雙手剑";
                case GearType.thAxe: return "雙手斧";
                case GearType.thBlunt: return "雙手棍";
                case GearType.spear: return "槍";
                case GearType.polearm: return "矛";
                case GearType.bow: return "弓";
                case GearType.crossbow: return "弩";
                case GearType.throwingGlove: return "拳套";
                case GearType.knuckle: return "指虎";
                case GearType.gun: return "火槍";
                case GearType.android: return "機器人";
                case GearType.machineHeart: return "心臟";
                case GearType.pickaxe: return "採礦";
                case GearType.shovel: return "採藥";
                case GearType.pocket: return "口袋道具";
                case GearType.dualBow: return "雙弩槍";
                case GearType.handCannon: return "加農砲";
                case GearType.badge: return "胸章";
                case GearType.emblem: return "象徽";
                case GearType.soulShield: return "靈魂盾牌";
                case GearType.demonShield: return "力量之盾";
                case GearType.totem: return "圖騰";
                case GearType.petEquip: return "寵物裝備";
                case GearType.taming:
                case GearType.taming2:
                case GearType.taming3: 
                case GearType.tamingChair: return "騎寵";
                case GearType.saddle: return "馬鞍";
                case GearType.katana: return "太刀";
                case GearType.fan: return "扇子";
                case GearType.swordZB: return "琉";
                case GearType.swordZL: return "璃";
                case GearType.weapon: return "武器";
                case GearType.subWeapon: return "輔助武器";
                case GearType.heroMedal: return "獎牌";
                case GearType.rosario: return "羅札里歐";
                case GearType.chain: return "鐵鍊";
                case GearType.book1:
                case GearType.book2:
                case GearType.book3: return "魔導書";
                case GearType.bowMasterFeather: return "箭失";
                case GearType.crossBowThimble: return "弓箭指套";
                case GearType.shadowerSheath: return "短劍用劍套";
                case GearType.nightLordPoutch: return "符咒";
                case GearType.viperWristband: return "手環";
                case GearType.captainSight: return "照準器";
                case GearType.connonGunPowder: 
                case GearType.connonGunPowder2: return "火藥桶";
                case GearType.aranPendulum: return "壓力軸";
                case GearType.evanPaper: return "文件";
                case GearType.battlemageBall: return "魔法珠子";
                case GearType.wildHunterArrowHead: return "箭矢";
                case GearType.cygnusGem: return "寶石";
                case GearType.controller: return "控制";
                case GearType.foxPearl: return "狐狸寶珠";
                case GearType.chess: return "西洋棋";
                case GearType.powerSource: return "能源";

                case GearType.energySword: return "能量劍";
                case GearType.desperado: return "魔劍";
                case GearType.magicStick: return "幻獸棍棒";
                case GearType.whistle:
                case GearType.whistle2: return "哨子";
                case GearType.boxingClaw:
                case GearType.boxingClaw2: return "拳爪";
                case GearType.kodachi:
                case GearType.kodachi2: return "小太刀";
                case GearType.espLimiter: return "ESP限制器";

                case GearType.GauntletBuster: return "重拳槍";
                case GearType.ExplosivePill: return "裝填";

                case GearType.chain2: return "鎖鏈";
                case GearType.magicGauntlet: return "魔法護腕";
                case GearType.transmitter: return "武器傳送裝置";
                case GearType.magicWing: return "魔力翅膀";
                case GearType.pathOfAbyss: return "深淵通行";

                case GearType.ancientBow: return "古代之弓";
                case GearType.relic: return "遺物";

                case GearType.handFan: return "仙扇";
                case GearType.fanTassel: return "扇墜";
                    
                case GearType.tuner: return "調節器";
                case GearType.bracelet: return "手鐲";

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
                case 3: return "比較快";
                case 4:
                case 5: return "快";
                case 6: return "普通";
                case 7:
                case 8: return "慢";
                case 9: return "比較慢";
                default:
                    return attackSpeed.ToString();
            }
        }

        /// <summary>
        /// 获取套装裝備类型的字符串。
        /// </summary>
        /// <param Name="Type">表示套装裝備类型的GearType。</param>
        /// <returns></returns>
        public static string GetSetItemGearTypeString(GearType type)
        {
            return GetGearTypeString(type);
        }

        /// <summary>
        /// 获取裝備额外职业要求说明的字符串。
        /// </summary>
        /// <param Name="Type">表示裝備类型的GearType。</param>
        /// <returns></returns>
        public static string GetExtraJobReqString(GearType type)
        {
            switch (type)
            {
                //0xxx
                case GearType.heroMedal: return "英雄職業群可套用";
                case GearType.rosario: return "聖騎士職業群可套用";
                case GearType.chain: return "黑骑士職業群可套用";
                case GearType.book1: return "火毒系列魔法師可套用";
                case GearType.book2: return "冰雷系列魔法師可套用";
                case GearType.book3: return "主教系列魔法師可套用";
                case GearType.bowMasterFeather: return "箭神職業群可套用";
                case GearType.crossBowThimble: return "神射手職業群可套用";
                case GearType.relic: return "開拓者職業可穿載";
                case GearType.shadowerSheath: return "暗影神偷職業群可套用";
                case GearType.nightLordPoutch: return "夜使者職業群可套用";
                case GearType.katara: return "影武者可以裝備";
                case GearType.viperWristband: return "拳霸職業群可套用";
                case GearType.captainSight: return "槍神職業群可套用";
                case GearType.connonGunPowder: 
                case GearType.connonGunPowder2: return "重砲指揮官職業群可套用";
                case GearType.box:
                case GearType.boxingClaw:
                case GearType.boxingClaw2: return "蒼龍俠客可以裝備";

                //1xxx
                case GearType.cygnusGem: return "皇家騎士團可套用";

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
                case GearType.desperado: return "可以裝備在惡魔職業群上";
                case GearType.battlemageBall: return "煉獄巫師職業群可套用";
                case GearType.wildHunterArrowHead: return "狂豹獵人職業群可套用";
                case GearType.mailin: return "機甲戰神可套用";
                case GearType.controller:
                case GearType.powerSource:
                case GearType.energySword: return GetExtraJobReqString(36);
                case GearType.GauntletBuster:
                case GearType.ExplosivePill: return GetExtraJobReqString(37);

                //4xxx
                case GearType.katana:
                case GearType.kodachi:
                case GearType.kodachi2: return GetExtraJobReqString(41);
                case GearType.fan: return GetExtraJobReqString(42);

                //5xxx
                case GearType.soulShield: return "可套用米哈逸";

                //6xxx
                case GearType.novaMarrow: return GetExtraJobReqString(61);
                //case GearType.chain2:
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
                    
                case GearType.tuner:
                case GearType.bracelet: return GetExtraJobReqString(151);

                case GearType.magicGauntlet: 
                case GearType.magicWing: return GetExtraJobReqString(152);

                case GearType.pathOfAbyss: return GetExtraJobReqString(155);

                case GearType.fanTassel: return GetExtraJobReqString(164);

                default: return null;
            }
        }

        /// <summary>
        /// 获取裝備额外职业要求说明的字符串。
        /// </summary>
        /// <param Name="specJob">表示裝備属性的reqSpecJob的值。</param>
        /// <returns></returns>
        public static string GetExtraJobReqString(int specJob)
        {
            switch (specJob)
            {
                case 21: return "狂狼勇士職業群可套用";
                case 22: return "魔龍導士職業群可套用";
                case 23: return "精靈遊俠可以裝備";
                case 24: return "幻影俠盜可以裝備";
                case 25: return "隐月可套用";
                case 27: return "夜光可套用";
                case 31: return "可以裝備在惡魔職業群上";
                case 36: return "可以套用傑諾";
                case 37: return "爆拳槍神可以裝備";
                case 41: return "劍豪可套用";
                case 42: return "陰陽師可以裝備";
                case 51: return "可套用米哈逸";
                case 61: return "凱薩可套用";
                case 64: return "卡蒂娜可以裝備";
                case 65: return "天使破壞者可套用";
                case 101: return "神之子可以裝備";
                case 112: return "幻獸師可裝備";
                case 142: return "凱內西斯可以裝備";
                case 151: return "阿戴爾可以裝備";
                case 152: return "伊利恩可以裝備";
                case 155: return "亞克可以套用";
                case 164: return "虎影職業群可裝備";
                default: return null;
            }
        }

        public static string GetItemPropString(ItemPropType propType, int value)
        {
            switch (propType)
            {
                case ItemPropType.tradeBlock:
                    return GetGearPropString(GearPropType.tradeBlock, value);
                case ItemPropType.useTradeBlock:
                    return value == 0 ? null : "裝備後無法交換";
                case ItemPropType.tradeAvailable:
                    return GetGearPropString(GearPropType.tradeAvailable, value);
                case ItemPropType.only:
                    return GetGearPropString(GearPropType.only, value);
                case ItemPropType.accountSharable:
                    return GetGearPropString(GearPropType.accountSharable, value);
                case ItemPropType.sharableOnce:
                    return GetGearPropString(GearPropType.sharableOnce, value);
                case ItemPropType.quest:
                    return value == 0 ? null : "任務道具";
                case ItemPropType.pquest:
                    return value == 0 ? null : "組隊任務道具";
                case ItemPropType.permanent:
                    return value == 0 ? null : "魔法時間不會結束的奇幻寵物。";
                default:
                    return null;
            }
        }

        public static string GetSkillReqAmount(int skillID, int reqAmount)
        {
            switch (skillID / 10000)
            {
                case 11200: return "[需要巨熊技能點: " + reqAmount + "]";
                case 11210: return "[需要雪豹技能點: " + reqAmount + "]";
                case 11211: return "[需要猛鷹技能點: " + reqAmount + "]";
                case 11212: return "[需要猫咪技能點: " + reqAmount + "]";
                default: return "[需要？？技能點: " + reqAmount + "]";
            }
        }

        public static string GetJobName(int jobCode)
        {
            switch (jobCode)
            {
                case 0: return "初心者";
                case 100: return "劍士";
                case 110: return "狂戰士";
                case 111: return "十字軍";
                case 112: return "英雄";
                case 120: return "見習騎士";
                case 121: return "騎士";
                case 122: return "聖騎士";
                case 130: return "槍騎兵";
                case 131: return "嗜血狂騎";
                case 132: return "黑騎士";
                case 200: return "法師";
                case 210: return "巫師（火，毒）";
                case 211: return "魔導士（火，毒）";
                case 212: return "大魔導士（火，毒）";
                case 220: return "巫師（冰，雷）";
                case 221: return "魔導士（冰，雷）";
                case 222: return "大魔導士（冰，雷）";
                case 230: return "僧侶";
                case 231: return "祭司";
                case 232: return "主教";
                case 300: return "弓箭手";
                case 301: return "弓箭手";
                case 310: return "獵人";
                case 311: return "遊俠";
                case 312: return "箭神";
                case 320: return "弩弓手";
                case 321: return "狙擊手";
                case 322: return "神射手";
                case 330: return "古代弓箭手";
                case 331: return "追擊者";
                case 332: return "開拓者";
                case 400: return "盜賊";
                case 410: return "刺客";
                case 411: return "暗殺者";
                case 412: return "夜使者";
                case 420: return "侠客";
                case 421: return "神偷";
                case 422: return "暗影神偷";
                case 430: return "下忍";
                case 431: return "中忍";
                case 432: return "上忍";
                case 433: return "隱忍";
                case 434: return "影武者";
                case 500: return "海盜";
                case 501: return "海盗(炮手)";
                case 510: return "打手";
                case 511: return "格鬥家";
                case 512: return "拳霸";
                case 520: return "槍手";
                case 521: return "神槍手";
                case 522: return "槍神";
                case 530: return "重砲兵";
                case 531: return "重砲兵隊長";
                case 532: return "重砲指揮官";
                case 508: return "蒼龍俠客(1轉)";
                case 570: return "蒼龍俠客(2轉)";
                case 571: return "蒼龍俠客(3轉)";
                case 572: return "蒼龍俠客(4轉)";

                case 1000: return "初心者";
                case 1100: return "聖魂劍士(1轉)";
                case 1110: return "聖魂劍士(2轉)";
                case 1111: return "聖魂劍士(3轉)";
                case 1112: return "聖魂劍士(4轉)";
                case 1200: return "烈焰巫師(1轉)";
                case 1210: return "烈焰巫師(2轉)";
                case 1211: return "烈焰巫師(3轉)";
                case 1212: return "烈焰巫師(4轉)";
                case 1300: return "破風使者(1轉)";
                case 1310: return "破風使者(2轉)";
                case 1311: return "破風使者(3轉)";
                case 1312: return "破風使者(4轉)";
                case 1400: return "暗夜行者(1轉)";
                case 1410: return "暗夜行者(2轉)";
                case 1411: return "暗夜行者(3轉)";
                case 1412: return "暗夜行者";
                case 1500: return "閃雷悍將(1轉)";
                case 1510: return "閃雷悍將(2轉)";
                case 1511: return "閃雷悍將(3轉)";
                case 1512: return "閃雷悍將(4轉)";

                case 2000: return "傳說";
                case 2001: return "小不點";
                case 2002: return "精靈遊俠";
                case 2003: return "幻影俠盜";
                case 2004: return "夜光";
                case 2005: return "隱月";
                case 2100: return "狂狼勇士(1轉)";
                case 2110: return "狂狼勇士(2轉)";
                case 2111: return "狂狼勇士(3轉)";
                case 2112: return "狂狼勇士(4轉)";
                case 2200: return "龍魔導士(1轉)";
                case 2210:
                case 2211: return "龍魔導士(2轉)";
                case 2212:
                case 2213:
                case 2214: return "龍魔導士(3轉)";
                case 2215:
                case 2216:
                case 2217: return "龍魔導士(4轉)";
                case 2218:
                case 2300: return "精靈遊俠(1轉)";
                case 2310: return "精靈遊俠(2轉)";
                case 2311: return "精靈遊俠(3轉)";
                case 2312: return "精靈遊俠(4轉)";
                case 2400: return "幻影俠盜(1轉)";
                case 2410: return "幻影俠盜(2轉)";
                case 2411: return "幻影俠盜(3轉)";
                case 2412: return "幻影俠盜(4轉)";
                case 2500: return "隱月(1轉)";
                case 2510: return "隱月(2轉)";
                case 2511: return "隱月(3轉)";
                case 2512: return "隱月(4轉)";
                case 2700: return "夜光(1轉)";
                case 2710: return "夜光(2轉)";
                case 2711: return "夜光(3轉)";
                case 2712: return "夜光(4轉)";

                case 3000: return "市民";
                case 3001: return "惡魔殺手";
                case 3100: return "惡魔殺手(1轉)";
                case 3110: return "惡魔殺手(2轉)";
                case 3111: return "惡魔殺手(3轉)";
                case 3112: return "惡魔殺手(4轉)";
                case 3101: return "惡魔復仇者(1轉)";
                case 3120: return "惡魔復仇者(2轉)";
                case 3121: return "惡魔復仇者(3轉)";
                case 3122: return "惡魔復仇者(4轉)";
                case 3200: return "煉獄巫師(1轉)";
                case 3210: return "煉獄巫師(2轉)";
                case 3211: return "煉獄巫師(3轉)";
                case 3212: return "煉獄巫師(4轉)";
                case 3300: return "狂豹獵人(1轉)";
                case 3310: return "狂豹獵人(2轉)";
                case 3311: return "狂豹獵人(3轉)";
                case 3312: return "狂豹獵人(4轉)";
                case 3500: return "機甲戰神(1轉)";
                case 3510: return "機甲戰神(2轉)";
                case 3511: return "機甲戰神(3轉)";
                case 3512: return "機甲戰神(4轉)";
                case 3002: return "傑諾";
                case 3600: return "傑諾(1轉)";
                case 3610: return "傑諾(2轉)";
                case 3611: return "傑諾(3轉)";
                case 3612: return "傑諾(4轉)";
                case 3700: return "爆拳槍神(1轉)";
                case 3710: return "爆拳槍神(2轉)";
                case 3711: return "爆拳槍神(3轉)";
                case 3712: return "爆拳槍神(4轉)";

                case 4001: return "劍豪";
                case 4002: return "陰陽師";
                case 4100: return "劍豪(1轉)";
                case 4110: return "劍豪(2轉)";
                case 4111: return "劍豪(3轉)";
                case 4112: return "劍豪(4轉)";
                case 4200: return "陰陽師(1轉)";
                case 4210: return "陰陽師(2轉)";
                case 4211: return "陰陽師(3轉)";
                case 4212: return "陰陽師(4轉)";

                case 5000: return "無名少年";
                case 5100: return "米哈逸(1轉)";
                case 5110: return "米哈逸(2轉)";
                case 5111: return "米哈逸(3轉)";
                case 5112: return "米哈逸(4轉)";

                case 6000: return "凱薩";
                case 6001: return "天使破壞者";
                case 6002: return "卡蒂娜";
                case 6100: return "凱薩(1轉)";
                case 6110: return "凱薩(2轉)";
                case 6111: return "凱薩(3轉)";
                case 6112: return "凱薩(4轉)";
                case 6400: return "卡蒂娜(1轉)";
                case 6410: return "卡蒂娜(2轉)";
                case 6411: return "卡蒂娜(3轉)";
                case 6412: return "卡蒂娜(4轉)";
                case 6500: return "天使破壞者(1轉)";
                case 6510: return "天使破壞者(2轉)";
                case 6511: return "天使破壞者(3轉)";
                case 6512: return "天使破壞者(4轉)";

                case 10000: return "神之子";
                case 10100: return "神之子(1轉)";
                case 10110: return "神之子(2轉)";
                case 10111: return "神之子(3轉)";
                case 10112: return "神之子(4轉)";

                case 11000: return "幻獸師";
                case 11200: return "幻獸師(熊)";
                case 11210: return "幻獸師(豹)";
                case 11211: return "幻獸師(鷹)";
                case 11212: return "幻獸師(貓)";

                case 13000: return "皮卡啾";
                case 13100: return "皮卡啾";

                case 14000: return "凱內西斯";
                case 14200: return "凱內西斯(1轉)";
                case 14210: return "凱內西斯(2轉)";
                case 14211: return "凱內西斯(3轉)";
                case 14212: return "凱內西斯(4轉)";

                case 15000: return "伊利恩";
                case 15001: return "亞克";
                case 15200: return "伊利恩(1轉)";
                case 15210: return "伊利恩(2轉)";
                case 15211: return "伊利恩(3轉)";
                case 15212: return "伊利恩(4轉)";
                case 15500: return "亞克(1轉)";
                case 15510: return "亞克(2轉)";
                case 15511: return "亞克(3轉)";
                case 15512: return "亞克(4轉)";

                case 16000: return "虎影";
                case 16400: return "虎影(1轉)";
                case 16410: return "虎影(2轉)";
                case 16411: return "虎影(3轉)";
                case 16412: return "虎影(4轉)";
            }
            return null;
        }
    }
}
