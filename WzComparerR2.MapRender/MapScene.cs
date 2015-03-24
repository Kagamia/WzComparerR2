using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.MapRender
{
    public class MapScene
    {
        public MapScene()
        {
        }
    }

    public class MapSceneNode
    {
        public MapSceneNode()
        {
            this.Nodes = new MapSceneNodeCollection(this);
        }

        public MapSceneNodeCollection Nodes { get; private set; }
        public MapSceneNode Parent { get; internal set; }
    }

    public class MapSceneNodeCollection : IList<MapSceneNode>
    {
        internal MapSceneNodeCollection(MapSceneNode owner)
        {
            this.owner = owner;
            this.innerList = new List<MapSceneNode>();
        }
        private MapSceneNode owner;
        private List<MapSceneNode> innerList;

        public int IndexOf(MapSceneNode item)
        {
            return this.innerList.IndexOf(item);
        }

        public void Insert(int index, MapSceneNode item)
        {
            if (item.Parent != null)
            {
                throw new ArgumentException("item already has parent.");
            }
            item.Parent = this.owner;
            this.innerList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            this[index].Parent = null;
            this.innerList.RemoveAt(index);
        }

        public MapSceneNode this[int index]
        {
            get
            {
                return this.innerList[index];
            }
            set
            {
                this.innerList[index] = value;
            }
        }

        public void Add(MapSceneNode item)
        {
            if (item.Parent != null)
            {
                throw new ArgumentException("item already has parent.");
            }
            item.Parent = this.owner;
            this.innerList.Add(item);
        }

        public void Clear()
        {
            this.innerList.ForEach(item => item.Parent = null);
            this.innerList.Clear();
        }

        public bool Contains(MapSceneNode item)
        {
            return this.innerList.Contains(item);
        }

        public int Count
        {
            get { return this.innerList.Count; }
        }

        public bool Remove(MapSceneNode item)
        {
            bool success = this.innerList.Remove(item);
            if (success)
            {
                item.Parent = null;
            }
            return success;
        }

        public IEnumerator<MapSceneNode> GetEnumerator()
        {
            return this.innerList.GetEnumerator();
        }

        bool ICollection<MapSceneNode>.IsReadOnly
        {
            get { return false; }
        }

        void ICollection<MapSceneNode>.CopyTo(MapSceneNode[] array, int arrayIndex)
        {
            this.innerList.CopyTo(array, arrayIndex);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.innerList.GetEnumerator();
        }
    }
}
