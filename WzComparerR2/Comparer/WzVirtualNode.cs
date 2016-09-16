using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using WzComparerR2.WzLib;

namespace WzComparerR2.Comparer
{
    public class WzVirtualNode
    {
        public WzVirtualNode()
        {
            this.LinkNodes = new List<Wz_Node>(4);
            this.ChildNodes = new List<WzVirtualNode>();
        }

        public WzVirtualNode(Wz_Node wzNode) : this()
        {
            this.Name = wzNode.Text;
            this.LinkNodes.Add(wzNode);
        }

        public string Name { get; set; }
        public List<Wz_Node> LinkNodes { get; private set; }
        public List<WzVirtualNode> ChildNodes { get; private set; }

        public void AddChild(WzVirtualNode childNode)
        {
            this.ChildNodes.Add(childNode);
        }

        public void AddChild(Wz_Node wzNode)
        {
            this.AddChild(wzNode, false);
        }

        public void AddChild(Wz_Node wzNode, bool addAllChildren)
        {
            var childNode = new WzVirtualNode(wzNode);
            this.AddChild(childNode);

            if (addAllChildren && wzNode.Nodes.Count > 0)
            {
                foreach (var node in wzNode.Nodes)
                {
                    childNode.AddChild(node, addAllChildren);
                }
            }
        }

        public void Combine(Wz_Node wzNode)
        {
            this.LinkNodes.Add(wzNode);
            bool needCheck = this.ChildNodes.Count > 0;

            foreach (var fromChild in wzNode.Nodes)
            {
                //如果当前本身为空 省去检查合并 因为wz本身并不重复...
                WzVirtualNode toChild = null;
                if (needCheck)
                {
                    toChild = FindChild(fromChild.Text);
                }

                if (toChild == null) //没有找到 新增
                {
                    this.AddChild(fromChild, true);
                }
                else if (fromChild.Value == null && toChild.HasNoValue()) //同为目录
                {
                    toChild.Combine(fromChild);
                }
                else if (fromChild.Nodes.Count <= 0 && !toChild.HasDirectory()) //没有子集 合并测试
                {
                    toChild.Combine(fromChild);
                }
                else
                {
                    throw new Exception(string.Format("WZ合并失败，{0}已存在并且存在子级。", fromChild.FullPathToFile));
                }
            }
        }

        private WzVirtualNode FindChild(string name)
        {
            foreach (var child in this.ChildNodes)
            {
                if (child.Name == name)
                {
                    return child;
                }
            }
            return null;
        }

        private bool HasNoValue()
        {
            foreach (var linkNode in this.LinkNodes)
            {
                if (linkNode.Value != null)
                {
                    return false;
                }
            }
            return true;
        }

        private bool HasDirectory()
        {
            foreach(var linkNode in this.LinkNodes)
            {
                if (linkNode.Nodes.Count > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return string.Format("{0} link:{1} child:{2}", this.Name, this.LinkNodes.Count, this.ChildNodes.Count);
        }

    }
}
