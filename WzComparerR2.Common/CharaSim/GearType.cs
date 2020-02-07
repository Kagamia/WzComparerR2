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

        /// <summary>
        /// 臉飾 101
        /// </summary>
        faceAccessory = 101,
        /// <summary>
        /// 眼飾 102
        /// </summary>
        eyeAccessory = 102,
        /// <summary>
        /// 耳環 103
        /// </summary>
        earrings = 103,
        /// <summary>
        /// 項鍊 112
        /// </summary>
        pendant = 112,
        /// <summary>
        /// 腰帶 113
        /// </summary>
        belt = 113,
        /// <summary>
        /// 勳章 114
        /// </summary>
        medal = 114,
        /// <summary>
        /// 肩飾 115
        /// </summary>
        shoulderPad = 115,
        /// <summary>
        /// 頭盔 100
        /// </summary>
        cap = 100,
        /// <summary>
        /// 披風 110
        /// </summary>
        cape = 110,
        /// <summary>
        /// 上衣 104
        /// </summary>
        coat = 104,
        /// <summary>
        /// 龍魔導士帽子 194
        /// </summary>
        dragonMask = 194,
        /// <summary>
        /// 龍魔導士項鍊 195
        /// </summary>
        dragonPendant = 195,
        /// <summary>
        /// 龍魔導士翅膀 196
        /// </summary>
        dragonWings = 196,
        /// <summary>
        /// 龍魔導士尾巴 197
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
        /// 機甲引擎 161
        /// </summary>
        machineEngine = 161,
        /// <summary>
        /// 機甲手臂 162
        /// </summary>
        machineArms = 162,
        /// <summary>
        /// 機甲腿部 163
        /// </summary>
        machineLegs = 163,
        /// <summary>
        /// 機甲機身軀 164
        /// </summary>
        machineBody = 164,
        /// <summary>
        /// 機甲電晶管 165
        /// </summary>
        machineTransistors = 165,
        /// <summary>
        /// 機器人 166
        /// </summary>
        android = 166,
        /// <summary>
        /// 心臟 167
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
        /// 徽章 119
        /// </summary>
        emblem = 119,
        powerSource = 119020,
        /// <summary>
        /// 褲/裙 106
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
        /// 靈魂盾牌 1098xxx
        /// </summary>
        soulShield = 1098,
        /// <summary>
        /// 力量之盾 1099xxx
        /// </summary>
        demonShield = 1099,
        /// <summary>
        /// 鞋子 107
        /// </summary>
        shoes = 107,
        /// <summary>
        /// 閃亮克魯 1212
        /// </summary>
        shiningRod = 1212,
        /// <summary>
        /// 튜너 1213
        /// </summary>
        tuner = 1213,
        /// <summary>
        /// 靈魂射手 122
        /// </summary>
        soulShooter = 122,
        /// <summary>
        /// 魔劍 123
        /// </summary>
        desperado = 123,
        /// <summary>
        /// 能量劍 124
        /// </summary>
        energySword = 124,
        /// <summary>
        /// 幻獸棍棒 125
        /// </summary>
        magicStick = 125,
        /// <summary>
        /// ESP限制器 126
        /// </summary>
        espLimiter = 126,
        /// <summary>
        /// 鎖鏈 127
        /// </summary>
        chain2 = 127,
        /// <summary>
        /// 魔法護手 128
        /// </summary>
        magicGauntlet = 128,
        /// <summary>
        /// 仙扇 129
        /// </summary>
        handFan = 129,
        /// <summary>
        /// 單手劍 130
        /// </summary>
        ohSword = 130,
        /// <summary>
        /// 單手斧 131
        /// </summary>
        ohAxe = 131,
        /// <summary>
        /// 單手棍 132
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
        /// 魔法箭 135_00
        /// </summary>
        magicArrow = 135200,
        /// <summary>
        /// 卡牌 135_10
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
        /// 鎖鏈 135_22
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
        /// 寶盒 135_30
        /// </summary>
        box = 135230,
        /// <summary>
        /// 宝珠 135_40
        /// </summary>
        orb = 135240,
        /// <summary>
        /// 超新星精華 135_50
        /// </summary>
        novaMarrow = 135250,
        /// <summary>
        /// 靈魂之環 135_60
        /// </summary>
        soulBangle = 135260,
        /// <summary>
        /// 麦林 135_70
        /// </summary>
        mailin = 135270,
        /// <summary>
        /// 小太刀 135_80
        /// </summary>
        kodachi = 135280,
        /// <summary>
        /// 祕寶刀 135_83
        /// </summary>
        kodachi2 = 135283,
        /// <summary>
        /// 哨子 135_81
        /// </summary>
        whistle = 135281,
        /// <summary>
        /// 哨子 135_84
        /// </summary>
        whistle2 = 135284,
        /// <summary>
        /// 拳爪 135_82
        /// </summary>
        boxingClaw = 135282,
        /// <summary>
        /// 拳爪 135_85
        /// </summary>
        boxingClaw2 = 135285,
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
        /// 西洋棋 135320
        /// </summary>
        chess = 135320,
        /// <summary>
        /// 發信器 135330
        /// </summary>
        transmitter = 135330,
        /// <summary>
        /// 爆發能力 135340
        /// </summary>
        ExplosivePill = 135340,
        /// <summary>
        /// 魔法翅膀 135350
        /// </summary>
        magicWing = 135350,
        /// <summary>
        /// 精气珠 135360
        /// </summary>
        pathOfAbyss = 135360,
        /// <summary>
        /// 遺物 135370
        /// </summary>
        relic = 135370,
        /// <summary>
        /// 扇墜 135380
        /// </summary>
        fanTassel = 135380,
        /// <summary>
        /// 브레이슬릿 135400
        /// </summary>
        bracelet = 135400,
        /// <summary>
        /// 手杖
        /// </summary>
        cane = 136,
        /// <summary>
        /// 短杖 137
        /// </summary>
        wand = 137,
        /// <summary>
        /// 長杖 138
        /// </summary>
        staff = 138,
        /// <summary>
        /// 空手 139
        /// </summary>
        barehand = 139,
        /// <summary>
        /// 雙手劍 140
        /// </summary>
        thSword = 140,
        /// <summary>
        /// 雙手斧 141
        /// </summary>
        thAxe = 141,
        /// <summary>
        /// 雙手棍 142
        /// </summary>
        thBlunt = 142,
        /// <summary>
        /// 槍 143
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
        /// 指虎 148
        /// </summary>
        knuckle = 148,
        /// <summary>
        /// 短枪 149
        /// </summary>
        gun = 149,
        /// <summary>
        /// 採藥 150
        /// </summary>
        shovel = 150,
        /// <summary>
        /// 採礦 151
        /// </summary>
        pickaxe = 151,
        /// <summary>
        /// 雙弩槍 152
        /// </summary>
        dualBow = 152,
        /// <summary>
        /// 加農砲 153
        /// </summary>
        handCannon = 153,
        /// <summary>
        /// 太刀 154
        /// </summary>
        katana = 154,
        /// <summary>
        /// 扇子 155
        /// </summary>
        fan = 155,
        /// <summary>
        /// 琉 156
        /// </summary>
        swordZB = 156,
        /// <summary>
        /// 璃 157
        /// </summary>
        swordZL = 157,
        /// <summary>
        /// 重拳槍 158
        /// </summary>
        GauntletBuster = 158,
        /// <summary>
        /// 遠古弓 159
        /// </summary>
        ancientBow = 159,
        /// <summary>
        /// 拼圖 168
        /// </summary>
        bit = 168,
        /// <summary>
        /// 點裝武器 170
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
        /// 圖騰 120
        /// </summary>
        totem = 120,
        /// <summary>
        /// 寵物裝備 180
        /// </summary>
        petEquip = 180,
        /// <summary>
        /// 騎寵 190
        /// </summary>
        taming = 190,
        /// <summary>
        /// 馬鞍 191
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
