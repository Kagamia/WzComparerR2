using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2
{
    public class HistoryList<T> : IEnumerable<T>
    {
        public HistoryList()
        {
            this.stackPrev = new Stack<T>();
            this.stackNext = new Stack<T>();
        }
        private Stack<T> stackPrev;
        private Stack<T> stackNext;

        /// <summary>
        /// 在历史列表中添加一个新项，这会舍弃全部next列表中的项。
        /// </summary>
        /// <param Name="Item">要添加的新项。</param>
        public void Add(T item)
        {
            this.stackPrev.Push(item);
            this.stackNext.Clear();
        }

        public void AddRange(IEnumerable<T> collection)
        {
            foreach (T item in collection)
            {
                this.stackPrev.Push(item);
            }
            this.stackNext.Clear();
        }

        /// <summary>
        /// 返回历史列表的上一项。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public T MovePrev()
        {
            stackNext.Push(stackPrev.Pop());
            return this.Current;
        }

        /// <summary>
        /// 返回历史列表的下一项。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public T MoveNext()
        {
            stackPrev.Push(stackNext.Pop());
            return this.Current;
        }

        /// <summary>
        /// 获取历史列表的当前项。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public T Current
        {
            get
            {
                if (stackPrev.Count > 0)
                    return stackPrev.Peek();
                else
                    throw new InvalidOperationException("当前列表中没有添加项。");
            }
        }

        public int PrevCount
        {
            get { return stackPrev.Count == 0 ? 0 : stackPrev.Count - 1; }
        }

        public int NextCount
        {
            get { return stackNext.Count; }
        }

        public int Count
        {
            get { return stackPrev.Count + stackNext.Count; }
        }

        public void Clear()
        {
            this.stackPrev.Clear();
            this.stackNext.Clear();
        }

        public IEnumerator<T> GetEnumerator()
        {
            List<T> lst = new List<T>(this.stackPrev.Count + this.stackNext.Count);
            lst.AddRange(stackNext);
            lst.Reverse(0, lst.Count);
            lst.AddRange(stackPrev);
            return lst.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
