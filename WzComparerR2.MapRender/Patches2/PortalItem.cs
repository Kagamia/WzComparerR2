using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WzComparerR2.WzLib;
using WzComparerR2.Animation;

namespace WzComparerR2.MapRender.Patches2
{
    public class PortalItem : SceneItem
    {
        public int Type { get; set; }
        public string PName { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int? ToMap { get; set; }
        public string ToName { get; set; }
        public string Script { get; set; }
        public int Image { get; set; }
        public bool EnchantPortal { get; set; }
        public bool ShownAtMinimap { get; set; }

        public ItemView View { get; set; }
        public ItemTooltip Tooltip { get; set; }

        public static PortalItem LoadFromNode(Wz_Node node)
        {
            var item = new PortalItem()
            {
                PName = node.Nodes["pn"].GetValueEx<string>(null),
                Type = node.Nodes["pt"].GetValueEx(0),
                X = node.Nodes["x"].GetValueEx(0),
                Y = node.Nodes["y"].GetValueEx(0),
                ToMap = node.Nodes["tm"].GetValueEx<int>(),
                ToName = node.Nodes["tn"].GetValueEx<string>(null),
                Script = node.Nodes["script"].GetValueEx<string>(null),
                Image = node.Nodes["image"].GetValueEx<int>(0),
                EnchantPortal = node.Nodes["enchantPortal"].GetValueEx<int>(0) != 0,
                ShownAtMinimap = node.Nodes["shownAtMinimap"].GetValueEx<int>(0) != 0
            };
            return item;
        }

        public static readonly string[] PortalTypes = new[] { "sp", "pi", "pv", "pc", "pg", "tp", "ps", "pgi", "psi", "pcs", "ph", "psh", "pcj", "pci", "pcig", "pshg", "pcc" };

        public class ItemView
        {
            public bool IsEditorMode { get; set; }
            public bool IsFocusing { get; set; }

            public object Animator { get; set; }
            public object EditorAnimator { get; set; }

            public Controller Controller { get; set; }
        }

        public class ItemTooltip
        {
            public string Title { get; set; }
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
                        if (this.View.IsFocusing)
                        {
                            nextState = "portalStart";
                        }
                        else
                        {
                            nextState = null;
                        }
                        return true;

                    case "portalStart": //开始动画
                        if (this.View.IsFocusing)
                        {
                            nextState = "portalContinue";
                        }
                        else
                        {
                            nextState = "portalExit";
                        }
                        return true;

                    case "portalContinue": //循环动画
                        if (this.View.IsFocusing)
                        {
                            nextState = "portalContinue";
                        }
                        else
                        {
                            nextState = "portalExit";
                        }
                        return true;

                    case "portalExit": //结束动画
                        if (this.View.IsFocusing)
                        {
                            nextState = "portalStart";
                        }
                        else
                        {
                            nextState = null;
                        }
                        return true;
                }

                nextState = null;
                return false;
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
