using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2.Comparer
{
    public class WzFileComparer
    {
        public WzFileComparer()
        {
            this.PngComparison = WzPngComparison.SizeAndDataLength;
        }

        public WzPngComparison PngComparison { get; set; }
        public bool IgnoreWzFile { get; set; }

        public IEnumerable<CompareDifference> Compare(Wz_Node nodeNew, Wz_Node nodeOld)
        {
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
            var cmp = Compare(
               nodeNew == null ? null : new WzVirtualNodeAgent(nodeNew).Children,
               nodeOld == null ? null : new WzVirtualNodeAgent(nodeOld).Children);

            foreach (var diff in cmp)
            {
                yield return diff;
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

            //逐层对比
            int l = 0, r = 0;
            while (l < arrayNew.Count || r < arrayOld.Count)
            {
                int comp = -2;
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
                    comp = string.Compare(arrayNew[l].Name, arrayOld[r].Name);
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
                        if (!CompareData(arrayNew[l].Value, arrayOld[r].Value))
                        {
                            yield return new CompareDifference(arrayNew[l].LinkNode, arrayOld[r].LinkNode, DifferenceType.Changed);
                        }
                        if (CompareChild(arrayNew[l], arrayOld[r]))
                        {
                            foreach (CompareDifference diff in Compare(arrayNew[l], arrayOld[r]))
                            {
                                yield return diff;
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

            //移除引用
            arrayNew = null;
            arrayOld = null;
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
            public abstract IEnumerable<ComparableNode> Children { get; }
            public virtual Wz_Node LinkNode
            {
                get { return null; }
            }

            int IComparable<ComparableNode>.CompareTo(ComparableNode other)
            {
                return string.CompareOrdinal(this.Name, other.Name);
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
                    if (this.Target.LinkNodes.Count <= 0)
                    {
                        return null;
                    }
                    else if (this.Target.LinkNodes.Count == 1)
                    {
                        return this.Target.LinkNodes[0].Value;
                    }
                    else
                    {
                        foreach (var node in this.Target.LinkNodes)
                        {
                            if (node.Value != null)
                            {
                                return node.Value;
                            }
                        }
                        return null;
                    }
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
    }
}
