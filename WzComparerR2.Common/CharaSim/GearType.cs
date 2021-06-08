using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.CharaSim
{
    public enum GearType
    {
        body = 0,
        head = 1,
        face = 2,
        hair = 3,
        hair2 = 4,
        face2 = 5,
        hair3 = 6,

        /// <summary>
        /// 脸饰 101
        /// </summary>
        faceAccessory = 101,
        /// <summary>
        /// 眼饰 102
        /// </summary>
        eyeAccessory = 102,
        /// <summary>
        /// 耳环 103
        /// </summary>
        earrings = 103,
        /// <summary>
        /// 坠子 112
        /// </summary>
        pendant = 112,
        /// <summary>
        /// 腰带 113
        /// </summary>
        belt = 113,
        /// <summary>
        /// 勋章 114
        /// </summary>
        medal = 114,
        /// <summary>
        /// 肩饰 115
        /// </summary>
        shoulderPad = 115,
        /// <summary>
        /// 头盔 100
        /// </summary>
        cap = 100,
        /// <summary>
        /// 披风 110
        /// </summary>
        cape = 110,
        /// <summary>
        /// 上衣 104
        /// </summary>
        coat = 104,
        /// <summary>
        /// 龙神帽子 194
        /// </summary>
        dragonMask = 194,
        /// <summary>
        /// 龙神吊坠 195
        /// </summary>
        dragonPendant = 195,
        /// <summary>
        /// 龙神翅膀 196
        /// </summary>
        dragonWings = 196,
        /// <summary>
        /// 龙神尾巴 197
        /// </summary>
        dragonTail = 197,
        /// <summary>
        /// 手套 108
        /// </summary>
        glove = 108,
        /// <summary>
        /// 套服 105
        /// </summary>
        longcoat = 105,
        /// <summary>
        /// 机甲引擎 161
        /// </summary>
        machineEngine = 161,
        /// <summary>
        /// 机甲机械臂 162
        /// </summary>
        machineArms = 162,
        /// <summary>
        /// 机甲机械腿 163
        /// </summary>
        machineLegs = 163,
        /// <summary>
        /// 机甲机身材质 164
        /// </summary>
        machineBody = 164,
        /// <summary>
        /// 机甲晶体管 165
        /// </summary>
        machineTransistors = 165,
        /// <summary>
        /// 安卓 166
        /// </summary>
        android = 166,
        /// <summary>
        /// 心脏 167
        /// </summary>
        machineHeart = 167,
        /// <summary>
        /// 口袋物品 116
        /// </summary>
        pocket = 116,
        /// <summary>
        /// 徽章 118
        /// </summary>
        badge = 118,
        /// <summary>
        /// 纹章 119
        /// </summary>
        emblem = 119,
        powerSource = 119020,
        /// <summary>
        /// 裤/裙 106
        /// </summary>
        pants = 106,
        /// <summary>
        /// 戒指 111
        /// </summary>
        ring = 111,
        /// <summary>
        /// 盾牌 109
        /// </summary>
        shield = 109,
        /// <summary>
        /// 灵魂盾 1098xxx
        /// </summary>
        soulShield = 1098,
        /// <summary>
        /// 精气盾 1099xxx
        /// </summary>
        demonShield = 1099,
        /// <summary>
        /// 鞋子 107
        /// </summary>
        shoes = 107,
        /// <summary>
        /// 双头杖 1212
        /// </summary>
        shiningRod = 1212,
        /// <summary>
        /// 调谐器 1213
        /// </summary>
        tuner = 1213,
        /// <summary>
        /// 龙息臂箭 1214
        /// </summary>
        breathShooter = 1214,
        /// <summary>
        /// 灵魂手铳 122
        /// </summary>
        soulShooter = 122,
        /// <summary>
        /// 亡命剑 123
        /// </summary>
        desperado = 123,
        /// <summary>
        /// 能量剑 124
        /// </summary>
        energySword = 124,
        /// <summary>
        /// 驯兽魔法棒 125
        /// </summary>
        magicStick = 125,
        /// <summary>
        /// ESP限制器
        /// </summary>
        espLimiter = 126,
        /// <summary>
        /// 锁链 127
        /// </summary>
        chain2 = 127,
        /// <summary>
        /// 魔力手套 128
        /// </summary>
        magicGauntlet = 128,
        /// <summary>
        /// 扇子 129
        /// </summary>
        handFan = 129,
        /// <summary>
        /// 单手剑 130
        /// </summary>
        ohSword = 130,
        /// <summary>
        /// 单手斧 131
        /// </summary>
        ohAxe = 131,
        /// <summary>
        /// 单手钝器 132
        /// </summary>
        ohBlunt = 132,
        /// <summary>
        /// 短刀 133
        /// </summary>
        dagger = 133,
        /// <summary>
        /// 刀 134
        /// </summary>
        katara = 134,
        /// <summary>
        /// 魔法箭矢 135_00
        /// </summary>
        magicArrow = 135200,
        /// <summary>
        /// 卡片 135_10
        /// </summary>
        card = 135210,
        /// <summary>
        /// 吊坠 135_20
        /// </summary>
        heroMedal = 135220,
        /// <summary>
        /// 念珠 135_21
        /// </summary>
        rosario = 135221,
        /// <summary>
        /// 铁链 135_22
        /// </summary>
        chain = 135222,
        /// <summary>
        /// 魔导书(火毒) 135_23
        /// </summary>
        book1 = 135223,
        /// <summary>
        /// 魔导书(冰雷) 135_24
        /// </summary>
        book2 = 135224,
        /// <summary>
        /// 魔导书(牧师) 135_25
        /// </summary>
        book3 = 135225,
        /// <summary>
        /// 箭羽 135_26
        /// </summary>
        bowMasterFeather = 135226,
        /// <summary>
        /// 扳指 135_27
        /// </summary>
        crossBowThimble = 135227,
        /// <summary>
        /// 短剑剑鞘 135_28
        /// </summary>
        shadowerSheath = 135228,
        /// <summary>
        /// 护身符 135_29
        /// </summary>
        nightLordPoutch = 135229,
        /// <summary>
        /// 宝盒 135_30
        /// </summary>
        box = 135230,
        /// <summary>
        /// 宝珠 135_40
        /// </summary>
        orb = 135240,
        /// <summary>
        /// 龙之精髓 135_50
        /// </summary>
        novaMarrow = 135250,
        /// <summary>
        /// 灵魂戒指 135_60
        /// </summary>
        soulBangle = 135260,
        /// <summary>
        /// 麦林 135_70
        /// </summary>
        mailin = 135270,
        /// <summary>
        /// 小太刀 135_80
        /// </summary>
        katana2 = 135280,
        /// <summary>
        /// 哨子 135_81
        /// </summary>
        whistle = 135281,
        /// <summary>
        /// 拳爪 135_82
        /// </summary>
        boxingClaw = 135282,
        /// <summary>
        /// 拳天 135_86
        /// </summary>
        boxingSky = 135286,
        /// <summary>
        /// 手腕护带 135_90
        /// </summary>
        viperWristband = 135290,
        /// <summary>
        /// 望远镜 135_91
        /// </summary>
        captainSight = 135291,
        /// <summary>
        /// 火药桶 135_92
        /// </summary>
        connonGunPowder = 135292,
        /// <summary>
        /// 砝码 135_93
        /// </summary>
        aranPendulum = 135293,
        /// <summary>
        /// 文件 135_94
        /// </summary>
        evanPaper = 135294,
        /// <summary>
        /// 魔法球 135_95
        /// </summary>
        battlemageBall = 135295,
        /// <summary>
        /// 箭轴 135_96
        /// </summary>
        wildHunterArrowHead = 135296,
        /// <summary>
        /// 珠宝 135_97
        /// </summary>
        cygnusGem = 135297,
        /// <summary>
        /// 火药桶 135_98
        /// </summary>
        connonGunPowder2 = 135298,
        /// <summary>
        /// 控制器 135300
        /// </summary>
        controller = 135300,
        /// <summary>
        /// 狐狸珠 135310
        /// </summary>
        foxPearl = 135310,
        /// <summary>
        /// 棋子 135320
        /// </summary>
        chess = 135320,
        /// <summary>
        /// 武器传送装置 135330
        /// </summary>
        transmitter = 135330,
        /// <summary>
        /// 装弹 135340
        /// </summary>
        ExplosivePill = 135340,
        /// <summary>
        /// 魔力翅膀 135350
        /// </summary>
        magicWing = 135350,
        /// <summary>
        /// 精气珠 135360
        /// </summary>
        pathOfAbyss = 135360,
        /// <summary>
        /// 遗物 135370x
        /// </summary>
        relic = 135370,
        /// <summary>
        /// 扇坠 135380x
        /// </summary>
        fanTassel = 135380,
        /// <summary>
        /// 手链 135400x
        /// </summary>
        bracelet = 135400,
        /// <summary>
        /// 武器腰带 135401x
        /// </summary>
        weaponBelt = 135401,
        /// <summary>
        /// 手杖
        /// </summary>
        cane = 136,
        /// <summary>
        /// 短杖 137
        /// </summary>
        wand = 137,
        /// <summary>
        /// 长杖 138
        /// </summary>
        staff = 138,
        /// <summary>
        /// 空手 139
        /// </summary>
        barehand = 139,
        /// <summary>
        /// 双手剑 140
        /// </summary>
        thSword = 140,
        /// <summary>
        /// 拳封 140_3xxx
        /// </summary>
        boxingCannon = 1403,
        /// <summary>
        /// 双手斧 141
        /// </summary>
        thAxe = 141,
        /// <summary>
        /// 双手钝器 142
        /// </summary>
        thBlunt = 142,
        /// <summary>
        /// 枪 143
        /// </summary>
        spear = 143,
        /// <summary>
        /// 矛 144
        /// </summary>
        polearm = 144,
        /// <summary>
        /// 弓 145
        /// </summary>
        bow = 145,
        /// <summary>
        /// 弩 146
        /// </summary>
        crossbow = 146,
        /// <summary>
        /// 拳套 147
        /// </summary>
        throwingGlove = 147,
        /// <summary>
        /// 指节 148
        /// </summary>
        knuckle = 148,
        /// <summary>
        /// 短枪 149
        /// </summary>
        gun = 149,
        /// <summary>
        /// 采药工具 150
        /// </summary>
        shovel = 150,
        /// <summary>
        /// 采矿工具 151
        /// </summary>
        pickaxe = 151,
        /// <summary>
        /// 双弓 152
        /// </summary>
        dualBow = 152,
        /// <summary>
        /// 手持火炮 153
        /// </summary>
        handCannon = 153,
        /// <summary>
        /// 太刀 154
        /// </summary>
        katana = 154,
        /// <summary>
        /// 扇 155
        /// </summary>
        fan = 155,

        /// <summary>
        /// 大剑 156
        /// </summary>
        swordZB = 156,
        /// <summary>
        /// 太刀 157
        /// </summary>
        swordZL = 157,
        /// <summary>
        /// 机甲手枪 158
        /// </summary>
        GauntletBuster = 158,
        /// <summary>
        /// 远古弓 159
        /// </summary>
        ancientBow = 159,
        /// <summary>
        /// 拼图 168
        /// </summary>
        bit = 168,
        /// <summary>
        /// 点装武器 170
        /// </summary>
        cashWeapon = 170,
        /// <summary>
        /// 武器 -1
        /// </summary>
        weapon = -1,
        /// <summary>
        /// 武器 -1
        /// </summary>
        subWeapon = -2,
        /// <summary>
        /// 图腾 120
        /// </summary>
        totem = 120,
        /// <summary>
        /// 宠物装备 180
        /// </summary>
        petEquip = 180,
        /// <summary>
        /// 骑兽 190
        /// </summary>
        taming = 190,
        /// <summary>
        /// 鞍子 191
        /// </summary>
        saddle = 191,
        /// <summary>
        /// 骑兽 193
        /// </summary>
        taming2 = 193,
         /// <summary>
        /// 椅子用骑兽 198
        /// </summary>
        tamingChair = 198,
        /// <summary>
        /// 骑兽 199
        /// </summary>
        taming3 = 199
    }
}
