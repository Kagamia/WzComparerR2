using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using IE = System.Collections.IEnumerator;

namespace WzComparerR2.MapRender
{
    class CoroutineManager : GameComponent
    {
        public CoroutineManager(Game game) : base(game)
        {
            this.Enabled = true;
            this.runList = new LinkedList<Coroutine>();
            this.preAdd = new List<Coroutine>();
            this.preRemove = new List<Coroutine>();
        }

        public GameTime GameTime { get; private set; }

        private LinkedList<Coroutine> runList;
        private List<Coroutine> preAdd;
        private List<Coroutine> preRemove;
        private bool isUpdating;
   

        public override void Update(GameTime gameTime)
        {
            this.GameTime = gameTime;
            this.isUpdating = true;

            LinkedListNode<Coroutine> node = null;
            while ((node = (node == null ? runList.First : node.Next)) != null)
            {
                preAdd.Clear();
                preRemove.Clear();
                bool removeMe = true;
                var coroutine = node.Value;

                while (coroutine != null)
                {
                    if (coroutine.Enumerator != null)
                    {
                        bool hasNext = coroutine.Enumerator.MoveNext();
                        if (hasNext)
                        {
                            removeMe = false;
                            var value = coroutine.Enumerator.Current;
                            if (value == null)
                            {
                                //跳到下次update
                            }
                            else if (value is YieldCoroutine)
                            {
                                node.Value = ((YieldCoroutine)value);
                            }
                            else if (value is Coroutine)
                            {
                                //栈执行
                                var nextCoroutine = (Coroutine)value;
                                nextCoroutine.Prev = coroutine;
                                node.Value = nextCoroutine;

                                preAdd.Remove(nextCoroutine);
                            }
                            else
                            {
                                //其他奇妙类型 忽略
                            }
                            break;
                        }
                    }

                    coroutine = coroutine.Prev;
                }

                foreach (var c in preRemove)
                {
                    runList.Remove(c);
                }
                foreach (var c in preAdd)
                {
                    runList.AddLast(c);
                }
                if (removeMe)
                {
                    var nextNode = node.Previous;
                    runList.Remove(node);
                    node = nextNode;
                }
            }

            this.isUpdating = false;
        }

        public Coroutine StartCoroutine(IE ie)
        {
            var coroutine = new Coroutine() { Enumerator = ie };

            if (this.isUpdating)
            {
                preAdd.Add(coroutine);
            }
            else
            {
                this.runList.AddLast(coroutine);
            }

            return coroutine;
        }

        public Coroutine Yield(IE ie)
        {
            return new YieldCoroutine() { Enumerator = ie };
        }

        public Coroutine Yield(Coroutine coroutine)
        {
            return new YieldCoroutine() { Enumerator = coroutine.Enumerator };
        }

        public void StopCoroutine(Coroutine coroutine)
        {
            if (this.isUpdating)
            {
                preRemove.Add(coroutine);
            }
            else
            {
                this.runList.Remove(coroutine);
            }
        }
    }

    class Coroutine
    {
        public IE Enumerator { get; set; } 
        public Coroutine Prev { get; set; }
    }

    sealed class YieldCoroutine : Coroutine
    {
    }

    sealed class WaitTaskCompletedCoroutine : Coroutine
    {
        public WaitTaskCompletedCoroutine(Task task)
        {
            this.Task = task;
            this.Enumerator = this.GetEnumerator();
        }
        public Task Task { get; set; }

        private IE GetEnumerator()
        {
            while (!Task.IsCompleted)
            {
                yield return null;
            }
            if (Task.IsFaulted)
            {
                PluginBase.PluginManager.LogError("MapRender", Task.Exception, "Coroutine Error: ");
#if DEBUG
                if (Debugger.IsAttached)
                {
                    var ex = Task.Exception;
                    Debugger.Break();
                }
#endif
            }
        }
    }
}
