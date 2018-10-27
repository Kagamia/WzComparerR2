using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.WzLib;

namespace WzComparerR2.CharaSim
{
    public class Mob
    {
        public Mob()
        {
            this.ID = -1;
            this.ElemAttr = new MobElemAttr(null);
            this.Revive = new List<int>();
            //this.Animates = new LifeAnimateCollection();

            this.FirstAttack = false;
            this.BodyAttack = false;
            this.DamagedByMob = false;
        }

        public int ID { get; set; }
        public int Level { get; set; }
        public string DefaultHP { get; set; }
        public string DefaultMP { get; set; }
        public string FinalMaxHP { get; set; }
        public string FinalMaxMP { get; set; }
        public long MaxHP { get; set; }
        public long MaxMP { get; set; }
        public int HPRecovery { get; set; }
        public int MPRecovery { get; set; }
        public int? Speed { get; set; }
        public int? FlySpeed { get; set; }
        public int PADamage { get; set; }
        public int MADamage { get; set; }
        public int PDRate { get; set; }
        public int MDRate { get; set; }
        public int Acc { get; set; }
        public int Eva { get; set; }
        public int Pushed { get; set; }
        public int Exp { get; set; }
        public bool Boss { get; set; }
        public bool Undead { get; set; }
        public int Category { get; set; }
        public bool FirstAttack { get; set; }
        public bool BodyAttack { get; set; }
        public int RemoveAfter { get; set; }
        public bool DamagedByMob { get; set; }
        public bool Invincible { get; set; }
        public bool NotAttack { get; set; }
        public int FixedDamage { get; set; }
        public MobElemAttr ElemAttr { get; set; }

        public int? Link { get; set; }
        public bool Skeleton { get; set; }
        public bool JsonLoad { get; set; }

        public List<int> Revive { get; private set; }

        public BitmapOrigin Default { get; set; }
        //public LifeAnimateCollection Animates { get; private set; }


        public static Mob CreateFromNode(Wz_Node node, GlobalFindNodeFunction findNode)
        {
            int mobID;
            Match m = Regex.Match(node.Text, @"^(\d{7})\.img$");
            if (!(m.Success && Int32.TryParse(m.Result("$1"), out mobID)))
            {
                return null;
            }

            Mob mobInfo = new Mob();
            mobInfo.ID = mobID;
            Wz_Node infoNode = node.FindNodeByPath("info");
            //加载基础属性
            if (infoNode != null)
            {
                foreach (var propNode in infoNode.Nodes)
                {
                    switch (propNode.Text)
                    {
                        case "level": mobInfo.Level = propNode.GetValueEx<int>(0); break;
                        case "defaultHP": mobInfo.DefaultHP = propNode.GetValueEx<string>(null); break;
                        case "defaultMP": mobInfo.DefaultMP = propNode.GetValueEx<string>(null); break;
                        case "finalmaxHP": mobInfo.FinalMaxHP = propNode.GetValueEx<string>(null); break;
                        case "finalmaxMP": mobInfo.FinalMaxMP = propNode.GetValueEx<string>(null); break;
                        case "maxHP": mobInfo.MaxHP = propNode.GetValueEx<long>(0); break;
                        case "maxMP": mobInfo.MaxMP = propNode.GetValueEx<long>(0); break;
                        case "hpRecovery": mobInfo.HPRecovery = propNode.GetValueEx<int>(0); break;
                        case "mpRecovery": mobInfo.MPRecovery = propNode.GetValueEx<int>(0); break;
                        case "speed": mobInfo.Speed = propNode.GetValueEx<int>(0); break;
                        case "flySpeed": mobInfo.FlySpeed = propNode.GetValueEx<int>(0); break;

                        case "PADamage": mobInfo.PADamage = propNode.GetValueEx<int>(0); break;
                        case "MADamage": mobInfo.MADamage = propNode.GetValueEx<int>(0); break;
                        case "PDRate": mobInfo.PDRate = propNode.GetValueEx<int>(0); break;
                        case "MDRate": mobInfo.MDRate = propNode.GetValueEx<int>(0); break;
                        case "acc": mobInfo.Acc = propNode.GetValueEx<int>(0); break;
                        case "eva": mobInfo.Eva = propNode.GetValueEx<int>(0); break;
                        case "pushed": mobInfo.Pushed = propNode.GetValueEx<int>(0); break;
                        case "exp": mobInfo.Exp = propNode.GetValueEx<int>(0); break;

                        case "boss": mobInfo.Boss = propNode.GetValueEx<int>(0) != 0; break;
                        case "undead": mobInfo.Undead = propNode.GetValueEx<int>(0) != 0; break;
                        case "firstAttack": mobInfo.FirstAttack = propNode.GetValueEx<int>(0) != 0; break;
                        case "bodyAttack": mobInfo.BodyAttack = propNode.GetValueEx<int>(0) != 0; break;
                        case "category": mobInfo.Category = propNode.GetValueEx<int>(0); break;
                        case "removeAfter": mobInfo.RemoveAfter = propNode.GetValueEx<int>(0); break;
                        case "damagedByMob": mobInfo.DamagedByMob = propNode.GetValueEx<int>(0) != 0; break;
                        case "invincible": mobInfo.Invincible = propNode.GetValueEx<int>(0) != 0; break;
                        case "notAttack": mobInfo.NotAttack = propNode.GetValueEx<int>(0) != 0; break;
                        case "fixedDamage": mobInfo.FixedDamage = propNode.GetValueEx<int>(0); break;
                        case "elemAttr": mobInfo.ElemAttr = new MobElemAttr(propNode.GetValueEx<string>(null)); break;

                        case "link": mobInfo.Link = propNode.GetValueEx<int>(0); break;
                        case "skeleton": mobInfo.Skeleton = propNode.GetValueEx<int>(0) != 0; break;
                        case "jsonLoad": mobInfo.JsonLoad = propNode.GetValueEx<int>(0) != 0; break;

                        //case "skill": LoadSkill(mobInfo, propNode); break;
                        //case "attack": LoadAttack(mobInfo, propNode); break;
                        //case "buff": LoadBuff(mobInfo, propNode); break;
                        case "revive":
                            for (int i = 0; ; i++)
                            {
                                var reviveNode = propNode.FindNodeByPath(i.ToString());
                                if (reviveNode == null)
                                {
                                    break;
                                }
                                mobInfo.Revive.Add(reviveNode.GetValue<int>());
                            }
                            break;
                    }
                }
            }

            //读取怪物默认动作
            {
                Wz_Node linkNode = null;
                if (mobInfo.Link != null && findNode != null)
                {
                    linkNode = findNode(string.Format("Mob\\{0:d7}.img", mobInfo.Link));
                }
                if (linkNode == null)
                {
                    linkNode = node;
                }

                var imageFrame = new BitmapOrigin();

                foreach (var action in new[] { "stand", "move", "fly" })
                {
                    var actNode = linkNode.FindNodeByPath(action + @"\0");
                    if (actNode != null)
                    {
                        imageFrame = BitmapOrigin.CreateFromNode(actNode, findNode);
                        if (imageFrame.Bitmap != null)
                        {
                            break;
                        }
                    }
                }

                mobInfo.Default = imageFrame;
            }

            return mobInfo;
        }
    }
}
