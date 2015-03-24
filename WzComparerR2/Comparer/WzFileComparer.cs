using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2
{
    public class WzFileComparer
    {
        public WzFileComparer()
        {
            this.PngComparison = WzPngComparison.SizeAndDataLength;
        }

        public WzPngComparison PngComparison { get; set; }

        public IEnumerable<CompareDifference> Compare(Wz_Node nodeNew, Wz_Node nodeOld)
        {
            if (nodeNew == null && nodeOld == null) //do nothing
            {
                yield break;
            }

            //初始化 排序
            Wz_Node[] arrayNew = new Wz_Node[nodeNew == null ? 0 : nodeNew.Nodes.Count];
            Wz_Node[] arrayOld = new Wz_Node[nodeOld == null ? 0 : nodeOld.Nodes.Count];

            if (nodeNew != null)
            {
                nodeNew.Nodes.CopyTo(arrayNew, 0);
                if (arrayNew.Length > 1)
                    Array.Sort<Wz_Node>(arrayNew);
            }

            if (nodeOld != null)
            {
                nodeOld.Nodes.CopyTo(arrayOld, 0);
                if (arrayOld.Length > 1)
                    Array.Sort<Wz_Node>(arrayOld);
            }

            //逐层对比
            int l = 0, r = 0;
            while (l < arrayNew.Length || r < arrayOld.Length)
            {
                int comp = -2;
                if (r == arrayOld.Length) //输出左边
                {
                    comp = -1;
                }
                else if (l == arrayNew.Length) //输出右边
                {
                    comp = 1;
                }
                else
                {
                    comp = string.Compare(arrayNew[l].Text, arrayOld[r].Text);
                }

                switch (comp)
                {
                    case -1:
                        yield return new CompareDifference(arrayNew[l], null, DifferenceType.Append);
                        foreach (CompareDifference diff in Compare(arrayNew[l], null))
                        {
                            yield return diff;
                        }
                        l++;
                        break;
                    case 0:
                        if (!CompareData(arrayNew[l].Value, arrayOld[r].Value))
                        {
                            yield return new CompareDifference(arrayNew[l], arrayOld[r], DifferenceType.Changed);
                        }
                        foreach (CompareDifference diff in Compare(arrayNew[l], arrayOld[r]))
                        {
                            yield return diff;
                        }
                        l++; r++;
                        break;
                    case 1:
                        yield return new CompareDifference(null, arrayOld[r], DifferenceType.Remove);
                        foreach (CompareDifference diff in Compare(null, arrayOld[r]))
                        {
                            yield return diff;
                        }
                        r++;
                        break;
                    default:
                        throw new Exception("未知的比较结果。");
                }
            }

            //移除引用
            arrayNew = null;
            arrayOld = null;
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
    }
}
