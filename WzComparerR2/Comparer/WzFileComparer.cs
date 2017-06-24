using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using WzComparerR2.WzLib;
using WzComparerR2.Common;

namespace WzComparerR2.Comparer
{
    public class WzFileComparer
    {
        public WzFileComparer()
        {
            this.PngComparison = WzPngComparison.SizeAndDataLength;
            ResolvePngLink = true;
        }

        public WzPngComparison PngComparison { get; set; }
        public bool IgnoreWzFile { get; set; }
        public bool ResolvePngLink { get; set; }

        private DisposeQueue _disposeQueue;
        private List<Wz_Image> _currentWzImg = new List<Wz_Image>();

        public IEnumerable<CompareDifference> Compare(Wz_Node nodeNew, Wz_Node nodeOld)
        {
            _currentWzImg.Clear();
            AppendContext(nodeNew);
            AppendContext(nodeOld);

            var cmp = Compare(
                nodeNew == null ? null : new WzNodeAgent(nodeNew).Children,
                nodeOld == null ? null : new WzNodeAgent(nodeOld).Children);

            foreach (var diff in cmp)
            {
                yield return diff;
            }


        }

        public IEnumerable<CompareDifference> Compare(WzVirtualNode nodeNew, WzVirtualNode nodeOld)
        {
            _currentWzImg.Clear();
            foreach (var node in nodeNew.LinkNodes)
                AppendContext(node);
            foreach (var node in nodeOld.LinkNodes)
                AppendContext(node);

            var cmp = Compare(
               nodeNew == null ? null : new WzVirtualNodeAgent(nodeNew).Children,
               nodeOld == null ? null : new WzVirtualNodeAgent(nodeOld).Children);

            foreach (var diff in cmp)
            {
                yield return diff;
            }
        }

        private void AppendContext(Wz_Node node)
        {
            Wz_Image wzImg = node.GetNodeWzImage();
            if (wzImg != null)
            {
                _currentWzImg.Add(wzImg);
            }
        }

        private IEnumerable<CompareDifference> Compare(ComparableNode nodeNew, ComparableNode nodeOld)
        {
            var cmp = Compare(
                nodeNew == null ? null : nodeNew.Children,
                nodeOld == null ? null : nodeOld.Children);

            foreach (var diff in cmp)
            {
                yield return diff;
            }
        }

        private IEnumerable<CompareDifference> Compare(IEnumerable<ComparableNode> nodeNew, IEnumerable<ComparableNode> nodeOld)
        {
            if (nodeNew == null && nodeOld == null) //do nothing
            {
                yield break;
            }

            //初始化 排序
            var arrayNew = new List<ComparableNode>();
            var arrayOld = new List<ComparableNode>();

            if (nodeNew != null)
            {
                arrayNew.AddRange(nodeNew);
                arrayNew.Sort();
            }

            if (nodeOld != null)
            {
                arrayOld.AddRange(nodeOld);
                arrayOld.Sort();
            }

            foreach (var diff in CompareSortedNodes(arrayNew, arrayOld))
            {
                yield return diff;
            }

            //移除引用
            arrayNew = null;
            arrayOld = null;
        }

        private IEnumerable<CompareDifference> CompareSortedNodes(IList<ComparableNode> arrayNew, IList<ComparableNode> arrayOld, Comparison<ComparableNode> compFunc = null)
        {
            //逐层对比
            int l = 0, r = 0;
            while (l < arrayNew.Count || r < arrayOld.Count)
            {
                int? comp = null;
                if (r == arrayOld.Count) //输出左边
                {
                    comp = -1;
                }
                else if (l == arrayNew.Count) //输出右边
                {
                    comp = 1;
                }
                else
                {
                    comp = compFunc != null ? compFunc(arrayNew[l], arrayOld[r]) : arrayNew[l].CompareTo(arrayOld[r]);
                }

                switch (comp)
                {
                    case -1:
                        yield return new CompareDifference(arrayNew[l].LinkNode, null, DifferenceType.Append);
                        if (CompareChild(arrayNew[l], null))
                        {
                            foreach (CompareDifference diff in Compare(arrayNew[l], null))
                            {
                                yield return diff;
                            }
                        }
                        l++;
                        break;
                    case 0:
                        //TODO: 试着比较多linkNode的场合。。
                        if ((arrayNew[l].HasMultiValues || arrayOld[r].HasMultiValues)
                            && !(arrayNew[l].Value?.GetType() == typeof(Wz_File) || arrayOld[r].Value?.GetType() == typeof(Wz_File)) //file跳过
                            )
                        {
                            //对比node的绝对路径
                            var left = (arrayNew[l] as WzVirtualNodeAgent).Target.LinkNodes;
                            var right = (arrayOld[r] as WzVirtualNodeAgent).Target.LinkNodes;
                            var compFunc2 = new Comparison<Wz_Node>((a, b) => string.Compare(a.FullPathToFile, b.FullPathToFile));
                            left.Sort(compFunc2);
                            right.Sort(compFunc2);

                            foreach (var diff in CompareSortedNodes(
                                left.Select(n => (ComparableNode)new WzNodeAgent(n)).ToList(),
                                right.Select(n => (ComparableNode)new WzNodeAgent(n)).ToList(),
                                (a, b) => Math.Sign(string.CompareOrdinal(a.LinkNode.FullPath, b.LinkNode.FullPath)))
                                )
                            {
                                yield return diff;
                            }
                        }
                        else
                        {
                            bool compared = false;
                            bool linkFilter = false;

                            //同是png 检测link
                            if (ResolvePngLink)
                            {
                                ComparableNode nodeNew = arrayNew[l],
                                    nodeOld = arrayOld[r];
                                PngLinkInfo linkInfoNew, linkInfoOld;
                                bool linkNew = TryGetLink(nodeNew, out linkInfoNew),
                                    linkOld = TryGetLink(nodeOld, out linkInfoOld);

                                if (linkNew && !linkOld && nodeOld.Value is Wz_Png) //图片转化为link
                                {
                                    var newPng = GetLinkedPng(nodeNew.LinkNode);
                                    if (newPng != null)
                                    {
                                        if (!CompareData(newPng, nodeOld.Value))
                                        {
                                            yield return new CompareDifference(nodeNew.LinkNode, nodeOld.LinkNode, DifferenceType.Changed);
                                        }
                                        else //链接后图片一致 过滤link标记
                                        {
                                            linkFilter = true;
                                        }
                                        compared = true;
                                    }
                                }
                                else if (!linkNew && linkOld && nodeNew.Value is Wz_Png) //link恢复为图片
                                {
                                    var oldPng = GetLinkedPng(nodeOld.LinkNode);
                                    if (oldPng != null)
                                    {
                                        if (!CompareData(nodeNew.Value, oldPng))
                                        {
                                            yield return new CompareDifference(nodeNew.LinkNode, nodeOld.LinkNode, DifferenceType.Changed);
                                        }
                                        else //链接后图片一致 过滤link标记
                                        {
                                            linkFilter = true;
                                        }
                                        compared = true;
                                    }
                                }
                                else if (linkNew && linkOld) //两边都是link
                                {
                                    if (linkInfoNew.LinkType == linkInfoOld.LinkType 
                                        && linkInfoNew.LinkUrl == linkInfoOld.LinkUrl) //link没有变动
                                    {
                                        compared = true;
                                    }
                                    else
                                    {
                                        var newPng = GetLinkedPng(nodeNew.LinkNode);
                                        var oldPng = GetLinkedPng(nodeOld.LinkNode);
                                        if (newPng != null && oldPng != null)
                                        {
                                            if (newPng != oldPng && !CompareData(newPng, oldPng)) //对比有差异 不输出dummy
                                            {
                                                //yield return new CompareDifference(nodeNew.LinkNode, nodeOld.LinkNode, DifferenceType.Changed);
                                            }
                                            else
                                            {
                                                linkFilter = true;
                                            }
                                            compared = true;
                                        }
                                    }
                                }
                            }

                            //正常对比
                            if (!compared && !CompareData(arrayNew[l].Value, arrayOld[r].Value))
                            {
                                yield return new CompareDifference(arrayNew[l].LinkNode, arrayOld[r].LinkNode, DifferenceType.Changed);
                            }

                            //对比子集
                            if (CompareChild(arrayNew[l], arrayOld[r]))
                            {
                                foreach (CompareDifference diff in Compare(arrayNew[l], arrayOld[r]))
                                {
                                    if (linkFilter) // && diff.DifferenceType != DifferenceType.Changed) [s]过滤新增或删除[/s] 全部过滤
                                    {
                                        if ((diff.NodeNew?.ParentNode == arrayNew[l].LinkNode
                                            || diff.NodeOld?.ParentNode == arrayOld[r].LinkNode)) //差异节点为当前的子级
                                        {
                                            var nodeText = diff.NodeNew?.Text ?? diff.NodeOld?.Text;
                                            if (nodeText == "_inlink" || nodeText == "_outlink")
                                            {
                                                continue;
                                            }
                                        }
                                    }
                                    yield return diff;
                                }
                            }
                        }

                        l++; r++;
                        break;
                    case 1:
                        yield return new CompareDifference(null, arrayOld[r].LinkNode, DifferenceType.Remove);
                        if (CompareChild(null, arrayOld[r]))
                        {
                            foreach (CompareDifference diff in Compare(null, arrayOld[r]))
                            {
                                yield return diff;
                            }
                        }
                        r++;
                        break;
                    default:
                        throw new Exception("什么鬼");
                }
            }
        }

        private bool CompareChild(ComparableNode node1, ComparableNode node2)
        {
            if ((node1 != null && node1.Value is Wz_File)
                || (node2 != null && node2.Value is Wz_File))
            {
                return !IgnoreWzFile;
            }
            return true;
        }

        private bool TryGetLink(ComparableNode node, out PngLinkInfo linkInfo)
        {
            linkInfo = new PngLinkInfo();
            var png = node.Value as Wz_Png;
            if (png != null && png.Width == 1 && png.Height == 1)
            {
                var node1 = node.LinkNode;
                WzLib.Wz_Node linkNode;
                if ((linkNode = node1.Nodes["_inlink"]) != null)
                {
                    linkInfo.LinkType = PngLinkType.Inlink;
                    linkInfo.LinkUrl = linkNode.GetValue<string>();
                    return true;
                }
                else if ((linkNode = node1.Nodes["_outlink"]) != null)
                {
                    linkInfo.LinkType = PngLinkType.Inlink;
                    linkInfo.LinkUrl = linkNode.GetValue<string>();
                    return true;
                }
            }
            return false;
        }

        private Wz_Png GetLinkedPng(Wz_Node node)
        {
            var wzFile = node.GetNodeWzFile();
            if (wzFile != null)
            {
                var linkNode = node.GetLinkedSourceNode(path =>
                    PluginBase.PluginManager.FindWz(path, wzFile));

                //添加回收池机制...
                if (linkNode != null)
                {
                    var linkImg = linkNode.GetNodeWzImage();
                    if (linkImg != null && !_currentWzImg.Contains(linkImg))
                    {
                        if (_disposeQueue == null)
                        {
                            _disposeQueue = new DisposeQueue(32);
                        }
                        _disposeQueue.Add(linkImg, _currentWzImg);
                    }
                }

                return linkNode.GetValueEx<Wz_Png>(null);
            }
            return null;
        }

        /// <summary>
        /// 比较两个节点绑定的值是否相同。
        /// </summary>
        /// <param Name="dataNew">新的值。</param>
        /// <param Name="dataOld">旧的值。</param>
        /// <returns></returns>
        public virtual bool CompareData(object dataNew, object dataOld)
        {
            if (dataNew == null && dataOld == null)
                return true;
            if (dataNew == null ^ dataOld == null)
                return false;

            Type type = dataNew.GetType();
            if (type != dataOld.GetType())
                return false;

            string str;
            Wz_Image img;
            Wz_File file;
            Wz_Png png;
            Wz_Vector vector;
            Wz_Uol uol;
            Wz_Sound sound;

            if (type.IsClass)
            {
                if ((str = dataNew as string) != null)
                {
                    return str == (string)dataOld;
                }
                else if ((img = dataNew as Wz_Image) != null)
                {
                    Wz_Image imgOld = (Wz_Image)dataOld;
                    return img.Size == imgOld.Size
                        && img.Checksum == imgOld.Checksum;
                }
                else if ((file = dataNew as Wz_File) != null)
                {
                    Wz_File fileOld = (Wz_File)dataOld;
                    return file.Type == fileOld.Type;
                }
                else if ((png = dataNew as Wz_Png) != null)
                {
                    Wz_Png pngOld = (Wz_Png)dataOld;
                    switch (this.PngComparison)
                    {
                        case WzPngComparison.SizeOnly:
                            return png.Width == pngOld.Width && png.Height == pngOld.Height;

                        case WzPngComparison.SizeAndDataLength:
                            return png.Width == pngOld.Width
                                && png.Height == pngOld.Height
                                && png.DataLength == pngOld.DataLength;

                        case WzPngComparison.Pixel:
                            if (!(png.Width == pngOld.Width && png.Height == pngOld.Height && png.Form == pngOld.Form))
                            {
                                return false;
                            }
                            byte[] pixelNew = png.GetRawData();
                            byte[] pixelOld = pngOld.GetRawData();
                            if (pixelNew == null || pixelOld == null || pixelNew.Length != pixelOld.Length)
                            {
                                return false;
                            }
                            for (int i = 0, i1 = pixelNew.Length; i < i1; i++)
                            {
                                if (pixelNew[i] != pixelOld[i])
                                {
                                    return false;
                                }
                            }
                            return true;

                        default:
                            goto case WzPngComparison.SizeAndDataLength;

                    }
                }
                else if ((vector = dataNew as Wz_Vector) != null)
                {
                    Wz_Vector vectorOld = (Wz_Vector)dataOld;
                    return vector.X == vectorOld.X
                        && vector.Y == vectorOld.Y;
                }
                else if ((uol = dataNew as Wz_Uol) != null)
                {
                    return uol.Uol == ((Wz_Uol)dataOld).Uol;
                }
                else if ((sound = dataNew as Wz_Sound) != null)
                {
                    Wz_Sound soundOld = (Wz_Sound)dataOld;
                    return sound.Ms == soundOld.Ms
                        && sound.DataLength == soundOld.DataLength;
                }
            }

            return object.Equals(dataNew, dataOld);
        }

        private abstract class ComparableNode : IComparable<ComparableNode>
        {
            public abstract string Name { get; }
            public abstract object Value { get; }
            public abstract bool HasMultiValues { get; }
            public virtual IEnumerable<object> Values
            {
                get
                {
                    if (this.Value == null) return Enumerable.Empty<object>();
                    return new[] { this.Value };
                }
            }
            public abstract IEnumerable<ComparableNode> Children { get; }
            public virtual Wz_Node LinkNode
            {
                get { return null; }
            }

            public int CompareTo(ComparableNode other)
            {
                return Math.Sign(string.CompareOrdinal(this.Name, other.Name));
            }
        }

        private class WzNodeAgent : ComparableNode
        {
            public WzNodeAgent(Wz_Node target)
            {
                this.Target = target;
            }

            public Wz_Node Target { get; private set; }

            public override string Name
            {
                get { return this.Target.Text; }
            }

            public override object Value
            {
                get { return this.Target.Value; }
            }

            public override bool HasMultiValues
            {
                get { return false; }
            }

            public override IEnumerable<ComparableNode> Children
            {
                get
                {
                    foreach (var node in this.Target.Nodes)
                    {
                        yield return new WzNodeAgent(node);
                    }
                }
            }

            public override Wz_Node LinkNode
            {
                get
                {
                    return this.Target;
                }
            }
        }

        private class WzVirtualNodeAgent : ComparableNode
        {
            public WzVirtualNodeAgent(WzVirtualNode target)
            {
                this.Target = target;
            }

            public WzVirtualNode Target { get; private set; }

            public override string Name
            {
                get { return this.Target.Name; }
            }

            public override object Value
            {
                get
                {
                    return this.Target.LinkNodes.Select(n => n.Value)
                        .Where(v => v != null)
                        .FirstOrDefault();
                }
            }

            public override bool HasMultiValues
            {
                get
                {
                    return this.Values.Count() > 1;
                }
            }

            public override IEnumerable<object> Values
            {
                get
                {
                    return this.Target.LinkNodes.Select(n => n.Value)
                      .Where(v => v != null);
                }
            }

            public override IEnumerable<ComparableNode> Children
            {
                get
                {
                    foreach (var node in this.Target.ChildNodes)
                    {
                        yield return new WzVirtualNodeAgent(node);
                    }
                }
            }

            public override Wz_Node LinkNode
            {
                get
                {
                    if (this.Target.LinkNodes.Count <= 0)
                    {
                        return null;
                    }
                    else if (this.Target.LinkNodes.Count == 1)
                    {
                        return this.Target.LinkNodes[0];
                    }
                    else
                    {
                        foreach (var node in this.Target.LinkNodes)
                        {
                            if (node.Value != null)
                            {
                                return node;
                            }
                        }
                        return this.Target.LinkNodes[0];
                    }
                }
            }
        }

        private class DisposeQueue
        {
            public DisposeQueue(int maxCount)
            {
                this.MaxCount = maxCount;
                _list = new LinkedList<Wz_Image>();
                _dict = new Dictionary<Wz_Image, LinkedListNode<Wz_Image>>();
            }

            public int MaxCount { get; set; }

            private LinkedList<Wz_Image> _list;
            private Dictionary<Wz_Image, LinkedListNode<Wz_Image>> _dict;

            public void Add(Wz_Image wzImage)
            {
                Add(wzImage, null);
            }

            public void Add(Wz_Image wzImage, List<Wz_Image> currentImages)
            {
                LinkedListNode<Wz_Image> node;
                if (_dict.TryGetValue(wzImage, out node))
                {
                    //提升位置
                    if (node.Previous != null)
                    {
                        _list.Remove(node);
                        _list.AddFirst(node);
                    }
                }
                else
                {
                    //添加item
                    while (_list.Count >= MaxCount && _list.Count > 0)
                    {
                        DisposeLast(currentImages);
                    }

                    node = _list.AddFirst(wzImage);
                    _dict.Add(wzImage, node);
                }
            }

            public void DisposeAll()
            {
                while(_list.Count > 0)
                {
                    DisposeLast();
                }
            }

            private void DisposeLast()
            {
                this.DisposeLast(null);
            }

            private void DisposeLast(List<Wz_Image> currentImages)
            {
                var last = _list.Last;
                if (currentImages == null || !currentImages.Contains(last.Value))
                {
                    last.Value.Unextract();
                }
                _dict.Remove(last.Value);
                _list.Remove(last);
            }
        }

        private struct PngLinkInfo
        {
            public PngLinkType LinkType { get; set; }
            public string LinkUrl { get; set; }
        }

        private enum PngLinkType
        {
            None = 0,
            Inlink = 1,
            Outlink = 2
        }
    }
}
