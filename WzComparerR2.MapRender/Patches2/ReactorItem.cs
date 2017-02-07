using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.WzLib;
using WzComparerR2.Animation;
using System.Text.RegularExpressions;

namespace WzComparerR2.MapRender.Patches2
{
    public class ReactorItem : SceneItem
    {
        public int ID { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public bool Flip { get; set; }
        public string ReactorName { get; set; }
        public int ReactorTime { get; set; }

        public ItemView View { get; set; }

        public static ReactorItem LoadFromNode(Wz_Node node)
        {
            var item = new ReactorItem()
            {
                ID = node.Nodes["id"].GetValueEx(0),
                X = node.Nodes["x"].GetValueEx(0),
                Y = node.Nodes["y"].GetValueEx(0),
                Flip = node.Nodes["f"].GetValueEx(false),
                ReactorName = node.Nodes["name"].GetValueEx<string>(null),
                ReactorTime = node.Nodes["reactorTime"].GetValueEx<int>(0),
            };
            return item;
        }

        public class ItemView
        {
            public int Stage { get; set; }
            public int? NextStage { get; set; }
            public object Animator { get; set; }
            public Controller Controller { get; set; }
        }

        public class Controller : IDisposable
        {
            public Controller(ItemView view)
            {
                this.View = view;
                AttachEvent();
            }

            public ItemView View { get; private set; }

            private StateMachineAnimator animator;

            public void Update(TimeSpan elapsed)
            {
                var ani = this.animator.GetCurrent();
                if (ani == null) //隐藏状态
                {
                    if (OnStateUpdate(null, out ani) && ani != null)
                    {
                        this.animator.SetAnimation(ani);
                    }
                }
                else
                {
                    animator.Update(elapsed);
                }
            }

            private void AttachEvent()
            {
                this.animator = this.View.Animator as StateMachineAnimator;
                if (animator != null)
                {
                    animator.AnimationEnd += Animator_AnimationEnd;
                }
            }

            private void Animator_AnimationEnd(object sender, StateMachineAnimator.AnimationEndEventArgs e)
            {
                string nextState;
                if (OnStateUpdate(e.CurrentState, out nextState))
                {
                    e.NextState = nextState;
                }
            }

            private bool OnStateUpdate(string curState, out string nextState)
            {
                switch (curState)
                {
                    case null: //初始状态
                        if (this.View.Stage > -1)
                        {
                            nextState = this.View.Stage.ToString();
                        }
                        else
                        {
                            nextState = null;
                        }
                        return true;

                    default:
                        Match m;
                        if ((m = Regex.Match(curState, @"^(\d+)$")).Success)
                        {
                            int curStage = int.Parse(m.Result("$1"));
                            if (this.View.NextStage != null)
                            {
                                string hitAniName = $@"{curStage}/hit";
                                if (this.animator.Data.States.Contains(hitAniName)) //跳转到hit动作
                                {
                                    nextState = hitAniName;
                                    return true;
                                }
                                else
                                {
                                    goto _lbl1;
                                }
                            }
                        }
                        else if ((m = Regex.Match(curState, @"^(\d+)/hit$")).Success)
                        {
                            int curStage = int.Parse(m.Result("$1"));
                            if (this.View.NextStage != null)
                            {
                                goto _lbl1;
                            }
                        }
                        break;
                }

                nextState = null;
                return false;

                _lbl1:
                {
                    string aniName = this.View.NextStage.Value.ToString();
                    if (this.animator.Data.States.Contains(aniName)) //动作存在 直接跳转
                    {
                        nextState = aniName;
                        AfterStageChanged(this.View.NextStage.Value);
                    }
                    else //动作不存在 忽略
                    {
                        nextState = curState;
                        this.View.NextStage = null;
                    }
                    return true;
                }
            }

            private void AfterStageChanged(int newStage)
            {
                this.View.Stage = newStage;
                this.View.NextStage = null;
            }

            public void Dispose()
            {
                if (animator != null)
                {
                    animator.AnimationEnd -= Animator_AnimationEnd;
                }
            }
        }
    }
}
