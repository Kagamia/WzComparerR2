using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace WzComparerR2.MapRender
{
    public class SceneNode
    {
        public SceneNode()
        {
            this.Nodes = new SceneNodeCollection(this);
        }

        public SceneNode Parent { get; private set; }
        public SceneNodeCollection Nodes { get; private set; }


        public IEnumerable<SceneNode> Descendants()
        {
            foreach(var node in this.Nodes)
            {
                yield return node;
                foreach(var subNode in node.Descendants())
                {
                    yield return subNode;
                }
            }
        }

        public class SceneNodeCollection : Collection<SceneNode>
        {
            public SceneNodeCollection(SceneNode owner)
            {
                this._owner = owner;
            }

            private SceneNode _owner;

            public void AddRange(IEnumerable<SceneNode> nodes)
            {
                foreach(var node in nodes)
                {
                    this.Add(node);
                }
            }

            protected override void InsertItem(int index, SceneNode item)
            {
                if (item.Parent != null)
                    throw new ArgumentException("Item already has parent.");
                item.Parent = _owner;
                base.InsertItem(index, item);
            }

            protected override void SetItem(int index, SceneNode item)
            {
                if (item.Parent != null)
                    throw new ArgumentException("Item already has parent.");
                this[index].Parent = null;
                item.Parent = _owner;
                base.SetItem(index, item);
            }

            protected override void RemoveItem(int index)
            {
                this[index].Parent = null;
                base.RemoveItem(index);
            }

            protected override void ClearItems()
            {
                foreach (var item in this)
                    item.Parent = null;
                base.ClearItems();
            }
        }
    }
}
