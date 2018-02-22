using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Linq;
using WzComparerR2.WzLib;

namespace WzComparerR2.Comparer
{
    public class EasyComparer
    {
        public EasyComparer()
        {
            this.Comparer = new WzFileComparer();
        }

        public WzFileComparer Comparer { get; protected set; }
        private string stateInfo;
        private string stateDetail;
        public bool OutputPng { get; set; }
        public bool OutputAddedImg { get; set; }
        public bool OutputRemovedImg { get; set; }
        
        public string StateInfo
        {
            get { return stateInfo; }
            set
            {
                stateInfo = value;
                this.OnStateInfoChanged(EventArgs.Empty);
            }
        }

        public string StateDetail
        {
            get { return stateDetail; }
            set
            {
                stateDetail = value;
                this.OnStateDetailChanged(EventArgs.Empty);
            }
        }

        public event EventHandler StateInfoChanged;
        public event EventHandler StateDetailChanged;

        protected virtual void OnStateInfoChanged(EventArgs e)
        {
            if (this.StateInfoChanged != null)
                this.StateInfoChanged(this, e);
        }

        protected virtual void OnStateDetailChanged(EventArgs e)
        {
            if (this.StateDetailChanged != null)
                this.StateDetailChanged(this, e);
        }

        public void EasyCompareWzFiles(Wz_File fileNew, Wz_File fileOld, string outputDir)
        {
            StateInfo = "正在对比wz概况...";
           
            if (fileNew.Type == Wz_Type.Base || fileOld.Type == Wz_Type.Base) //至少有一个base 拆分对比
            {
                var virtualNodeNew = RebuildWzFile(fileNew);
                var virtualNodeOld = RebuildWzFile(fileOld);
                WzFileComparer comparer = new WzFileComparer();
                comparer.IgnoreWzFile = true;

                var dictNew = SplitVirtualNode(virtualNodeNew);
                var dictOld = SplitVirtualNode(virtualNodeOld);

                //寻找共同wzType
                var wzTypeList = dictNew.Select(kv => kv.Key)
                    .Where(wzType => dictOld.ContainsKey(wzType));

                CreateStyleSheet(outputDir);

                foreach (var wzType in wzTypeList)
                {
                    var vNodeNew = dictNew[wzType];
                    var vNodeOld = dictOld[wzType];
                    var cmp = comparer.Compare(vNodeNew, vNodeOld);
                    OutputFile(vNodeNew.LinkNodes.Select(node => node.Value).OfType<Wz_File>().ToList(),
                        vNodeOld.LinkNodes.Select(node => node.Value).OfType<Wz_File>().ToList(),
                        wzType,
                        cmp.ToList(),
                        outputDir);
                }
            }
            else //执行传统对比
            {
                WzFileComparer comparer = new WzFileComparer();
                comparer.IgnoreWzFile = false;
                var cmp = comparer.Compare(fileNew.Node, fileOld.Node);
                CreateStyleSheet(outputDir);
                OutputFile(fileNew, fileOld, fileNew.Type, cmp.ToList(), outputDir);
            }

            GC.Collect();
        }

        private WzVirtualNode RebuildWzFile(Wz_File wzFile)
        {
            //分组
            List<Wz_File> subFiles = new List<Wz_File>();
            WzVirtualNode topNode = new WzVirtualNode(wzFile.Node);

            foreach (var childNode in wzFile.Node.Nodes)
            {
                var subFile = childNode.GetValue<Wz_File>();
                if (subFile != null) //wz子文件
                {
                    subFiles.Add(subFile);
                }
                else //其他
                {
                    topNode.AddChild(childNode, true);
                }
            }

            if (wzFile.Type == Wz_Type.Base)
            {
                foreach (var grp in subFiles.GroupBy(f => f.Type))
                {
                    WzVirtualNode fileNode = new WzVirtualNode();
                    fileNode.Name = grp.Key.ToString();
                    foreach (var file in grp)
                    {
                        fileNode.Combine(file.Node);
                    }
                    topNode.AddChild(fileNode);
                }
            }
            return topNode;
        }

        private Dictionary<Wz_Type, WzVirtualNode> SplitVirtualNode(WzVirtualNode node)
        {
            var dict = new Dictionary<Wz_Type, WzVirtualNode>();
            Wz_File wzFile = node.LinkNodes[0].Value as Wz_File;
            dict[wzFile.Type] = node;

            if (wzFile.Type == Wz_Type.Base) //额外处理
            {
                var wzFileList = node.ChildNodes
                    .Select(child => new { Node = child, WzFile = child.LinkNodes[0].Value as Wz_File })
                    .Where(item => item.WzFile != null);

                foreach (var item in wzFileList)
                {
                    dict[item.WzFile.Type] = item.Node;
                }
            }

            return dict;
        }

        private void OutputFile(Wz_File fileNew, Wz_File fileOld, Wz_Type type, List<CompareDifference> diffLst, string outputDir)
        {
            OutputFile(new List<Wz_File>() { fileNew },
                new List<Wz_File>() { fileOld },
                type,
                diffLst,
                outputDir);
        }
        private void OutputFile(List<Wz_File> fileNew, List<Wz_File> fileOld, Wz_Type type, List<CompareDifference> diffLst, string outputDir)
        {
            string htmlFilePath = Path.Combine(outputDir, type.ToString() + ".html");
            for (int i = 1; File.Exists(htmlFilePath); i++)
            {
                htmlFilePath = Path.Combine(outputDir, string.Format("{0}_{1}.html", type, i));
            }
            string srcDirPath = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(htmlFilePath) + "_files");
            if (OutputPng && !Directory.Exists(srcDirPath))
            {
                Directory.CreateDirectory(srcDirPath);
            }

            FileStream htmlFile = null;
            StreamWriter sw = null;
            StateInfo = "正在努力对比文件..." + type;
            StateDetail = "正在构造输出文件";
            try
            {
                htmlFile = new FileStream(htmlFilePath, FileMode.Create, FileAccess.Write);
                sw = new StreamWriter(htmlFile, Encoding.UTF8);
                sw.WriteLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">");
                sw.WriteLine("<html>");
                sw.WriteLine("<head>");
                sw.WriteLine("<meta http-equiv=\"content-type\" content=\"text/html;charset=utf-8\">");
                sw.WriteLine("<title>{0} {1}←{2}</title>", type, fileNew[0].Header.WzVersion, fileOld[0].Header.WzVersion);
                sw.WriteLine("<link type=\"text/css\" rel=\"stylesheet\" href=\"style.css\" />");
                sw.WriteLine("</head>");
                sw.WriteLine("<body>");
                //输出概况
                sw.WriteLine("<p class=\"wzf\">");
                sw.WriteLine("<table>");
                sw.WriteLine("<tr><th>&nbsp;</th><th>文件名</th><th>文件大小</th><th>文件版本</th></tr>");
                sw.WriteLine("<tr><td>新文件</td><td>{0}</td><td>{1}</td><td>{2}</td></tr>",
                    string.Join("<br/>", fileNew.Select(wzf => wzf.Header.FileName).ToArray()),
                    string.Join("<br/>", fileNew.Select(wzf => wzf.Header.FileSize.ToString("N0")).ToArray()),
                    string.Join("<br/>", fileNew.Select(wzf => wzf.Header.WzVersion.ToString()).ToArray())
                    );
                sw.WriteLine("<tr><td>旧文件</td><td>{0}</td><td>{1}</td><td>{2}</td></tr>",
                    string.Join("<br/>", fileOld.Select(wzf => wzf.Header.FileName).ToArray()),
                    string.Join("<br/>", fileOld.Select(wzf => wzf.Header.FileSize.ToString("N0")).ToArray()),
                    string.Join("<br/>", fileOld.Select(wzf => wzf.Header.WzVersion.ToString()).ToArray())
                    );
                sw.WriteLine("<tr><td>对比时间</td><td colspan='3'>{0:yyyy-MM-dd HH:mm:ss.fff}</td></tr>", DateTime.Now);
                sw.WriteLine("<tr><td>参数</td><td colspan='3'>{0}</td></tr>", string.Join("<br/>", new[] {
                    this.OutputPng ? "-OutputPng" : null,
                    this.OutputAddedImg ? "-OutputAddedImg" : null,
                    this.OutputRemovedImg ? "-OutputRemovedImg" : null,
                    "-PngComparison " + this.Comparer.PngComparison,
                    this.Comparer.ResolvePngLink ? "-ResolvePngLink" : null,
                }.Where(p => p != null)));
                sw.WriteLine("</table>");
                sw.WriteLine("</p>");

                //输出目录
                StringBuilder[] sb = { new StringBuilder(), new StringBuilder(), new StringBuilder() };
                int[] count = new int[6];
                string[] diffStr = { "修改", "新增", "移除" };
                foreach (CompareDifference diff in diffLst)
                {
                    int idx = -1;
                    string detail = null;
                    switch (diff.DifferenceType)
                    {
                        case DifferenceType.Changed:
                            idx = 0;
                            detail = string.Format("<a name=\"m_{1}_{2}\" href=\"#a_{1}_{2}\">{0}</a>", diff.NodeNew.FullPathToFile, idx, count[idx]);
                            break;
                        case DifferenceType.Append:
                            idx = 1;
                            if (this.OutputAddedImg)
                            {
                                detail = string.Format("<a name=\"m_{1}_{2}\" href=\"#a_{1}_{2}\">{0}</a>", diff.NodeNew.FullPathToFile, idx, count[idx]);
                            }
                            else
                            {
                                detail = diff.NodeNew.FullPathToFile;
                            }
                            break;
                        case DifferenceType.Remove:
                            idx = 2;
                            if (this.OutputRemovedImg)
                            {
                                detail = string.Format("<a name=\"m_{1}_{2}\" href=\"#a_{1}_{2}\">{0}</a>", diff.NodeOld.FullPathToFile, idx, count[idx]);
                            }
                            else
                            {
                                detail = diff.NodeOld.FullPathToFile;
                            }
                            break;
                        default:
                            continue;
                    }
                    sb[idx].Append("<tr><td>");
                    sb[idx].Append(detail);
                    sb[idx].AppendLine("</td></tr>");
                    count[idx]++;
                }
                StateDetail = "正在输出目录";
                Array.Copy(count, 0, count, 3, 3);
                for (int i = 0; i < sb.Length; i++)
                {
                    sw.WriteLine("<table class=\"lst{0}\">", i);
                    sw.WriteLine("<tr><th>{0}共{1}项</th></tr>", diffStr[i], count[i]);
                    sw.Write(sb[i].ToString());
                    sw.WriteLine("</table>");
                    sb[i] = null;
                    count[i] = 0;
                }

                foreach (CompareDifference diff in diffLst)
                {
                    switch (diff.DifferenceType)
                    {
                        case DifferenceType.Changed:
                            {
                                StateInfo = string.Format("{0}/{1}正在对比{2}", count[0], count[3], diff.NodeNew.FullPath);
                                Wz_Image imgNew, imgOld;
                                if ((imgNew = diff.ValueNew as Wz_Image) != null
                                    && ((imgOld = diff.ValueOld as Wz_Image) != null))
                                {
                                    string anchorName = "a_0_" + count[0];
                                    string menuAnchorName = "m_0_" + count[0];
                                    CompareImg(imgNew, imgOld, diff.NodeNew.FullPathToFile, anchorName, menuAnchorName, srcDirPath, sw);
                                }
                                count[0]++;
                            }
                            break;

                        case DifferenceType.Append:
                            if (this.OutputAddedImg)
                            {
                                StateInfo = string.Format("{0}/{1}正在输出新增{2}", count[1], count[4], diff.NodeNew.FullPath);
                                Wz_Image imgNew = diff.ValueNew as Wz_Image;
                                if (imgNew != null)
                                {
                                    string anchorName = "a_1_" + count[1];
                                    string menuAnchorName = "m_1_" + count[1];
                                    OutputImg(imgNew, diff.DifferenceType, diff.NodeNew.FullPathToFile, anchorName, menuAnchorName, srcDirPath, sw);
                                }
                                count[1]++;
                            }
                            break;

                        case DifferenceType.Remove:
                            if (this.OutputRemovedImg)
                            {
                                StateInfo = string.Format("{0}/{1}正在输出删除{2}", count[2], count[5], diff.NodeOld.FullPath);
                                Wz_Image imgOld = diff.ValueOld as Wz_Image;
                                if (imgOld != null)
                                {
                                    string anchorName = "a_2_" + count[2];
                                    string menuAnchorName = "m_2_" + count[2];
                                    OutputImg(imgOld, diff.DifferenceType, diff.NodeOld.FullPathToFile, anchorName, menuAnchorName, srcDirPath, sw);
                                }
                                count[2]++;
                            }
                            break;

                        case DifferenceType.NotChanged:
                            break;
                    }

                }
                //html结束
                sw.WriteLine("</body>");
                sw.WriteLine("</html>");
            }
            finally
            {
                try
                {
                    if (sw != null)
                    {
                        sw.Flush();
                        sw.Close();
                    }
                }
                catch
                {
                }
            }
        }

        private void CompareImg(Wz_Image imgNew, Wz_Image imgOld, string imgName, string anchorName, string menuAnchorName, string outputDir, StreamWriter sw)
        {
            StateDetail = "正在解压img";
            if (!imgNew.TryExtract() || !imgOld.TryExtract())
                return;
            StateDetail = "正在对比img";
            List<CompareDifference> diffList = new List<CompareDifference>(Comparer.Compare(imgNew.Node, imgOld.Node));
            StringBuilder sb = new StringBuilder();
            int[] count = new int[3];
            StateDetail = "正在统计概况并输出资源文件...变动项共" + diffList.Count;
            foreach (var diff in diffList)
            {
                int idx = -1;
                string col0 = null;
                switch (diff.DifferenceType)
                {
                    case DifferenceType.Changed:
                        idx = 0;
                        col0 = diff.NodeNew.FullPath;
                        break;
                    case DifferenceType.Append:
                        idx = 1;
                        col0 = diff.NodeNew.FullPath;
                        break;
                    case DifferenceType.Remove:
                        idx = 2;
                        col0 = diff.NodeOld.FullPath;
                        break;
                }
                sb.AppendFormat("<tr class=\"r{0}\">", idx);
                sb.AppendFormat("<td>{0}</td>", col0 ?? " ");
                sb.AppendFormat("<td>{0}</td>", OutputNodeValue(col0, diff.ValueNew, 0, outputDir) ?? " ");
                sb.AppendFormat("<td>{0}</td>", OutputNodeValue(col0, diff.ValueOld, 1, outputDir) ?? " ");
                sb.AppendLine("</tr>");
                count[idx]++;
            }
            StateDetail = "正在输出对比报告";
            bool noChange = diffList.Count <= 0;
            sw.WriteLine("<table class=\"img{0}\">", noChange ? " noChange" : "");
            sw.WriteLine("<tr><th colspan=\"3\"><a name=\"{1}\">{0}</a> 修改:{2} 新增:{3} 移除:{4}</th></tr>",
                imgName, anchorName, count[0], count[1], count[2]);
            sw.WriteLine(sb.ToString());
            sw.WriteLine("<tr><td colspan=\"3\"><a href=\"#{1}\">{0}</a></td></tr>", "回到目录", menuAnchorName);
            sw.WriteLine("</table>");
            imgNew.Unextract();
            imgOld.Unextract();
            sb = null;
        }

        private void OutputImg(Wz_Image img, DifferenceType diffType, string imgName, string anchorName, string menuAnchorName, string outputDir, StreamWriter sw)
        {
            StateDetail = "正在解压img";
            if (!img.TryExtract())
                return;

            int idx = 0; ;
            switch (diffType)
            {
                case DifferenceType.Changed:
                    idx = 0;
                    break;
                case DifferenceType.Append:
                    idx = 1;
                    break;
                case DifferenceType.Remove:
                    idx = 2;
                    break;
            }
            Action<Wz_Node> fnOutput = null;
            fnOutput = node =>
            {
                if (node != null)
                {
                    string fullPath = node.FullPath;
                    sw.Write("<tr class=\"r{0}\">", idx);
                    sw.Write("<td>{0}</td>", fullPath ?? " ");
                    sw.Write("<td>{0}</td>", OutputNodeValue(fullPath, node.Value, 0, outputDir) ?? " ");
                    sw.WriteLine("</tr>");

                    if (node.Nodes.Count > 0)
                    {
                        foreach (Wz_Node child in node.Nodes)
                        {
                            fnOutput(child);
                        }
                    }
                }
            };

            StateDetail = "正在输出完整img结构";
            sw.WriteLine("<table class=\"img\">");
            sw.WriteLine("<tr><th colspan=\"2\"><a name=\"{1}\">wz_image: {0}</a></th></tr>", imgName, anchorName);
            fnOutput(img.Node);
            sw.WriteLine("<tr><td colspan=\"2\"><a href=\"#{1}\">{0}</a></td></tr>", "回到目录", menuAnchorName);
            sw.WriteLine("</table>");
            img.Unextract();
        }

        protected virtual string OutputNodeValue(string fullPath, object value, int col, string outputDir)
        {

            if (value == null)
                return null;

            Wz_Png png;
            Wz_Uol uol;
            Wz_Sound sound;
            Wz_Vector vector;
            
            if ((png = value as Wz_Png) != null)
            {
                if (OutputPng)
                {
                    char[] invalidChars = Path.GetInvalidFileNameChars();
                    string colName = col == 0 ? "new" : (col == 1 ? "old" : col.ToString());
                    string filePath = fullPath.Replace('\\', '.') + "_" + colName + ".png";

                    for (int i = 0; i < invalidChars.Length; i++)
                    {
                        filePath = filePath.Replace(invalidChars[i].ToString(), null);
                    }

                    Bitmap bmp = png.ExtractPng();
                    if (bmp != null)
                    {
                        bmp.Save(Path.Combine(outputDir, filePath), System.Drawing.Imaging.ImageFormat.Png);
                        bmp.Dispose();
                    }
                    return string.Format("<img src=\"{0}\" />", Path.Combine(new DirectoryInfo(outputDir).Name, filePath));
                }
                else
                {
                    return string.Format("png {0}*{1} ({2} bytes)", png.Width, png.Height, png.DataLength);
                }

            }
            else if ((uol = value as Wz_Uol) != null)
            {
                return uol.Uol;
            }
            else if ((vector = value as Wz_Vector) != null)
            {
                return string.Format("({0}, {1})", vector.X, vector.Y);
            }
            else if ((sound = value as Wz_Sound) != null)
            {
                return string.Format("sound {0}ms", sound.Ms);
            }
            else if (value is Wz_Image)
            {
                return "{ img }";
            }
            return Convert.ToString(value);

        }

        public virtual void CreateStyleSheet(string outputDir)
        {
            string path = Path.Combine(outputDir, "style.css");
            if (File.Exists(path))
                return;
            FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
            sw.WriteLine("body { font-size:12px; }");
            sw.WriteLine("p.wzf { }");
            sw.WriteLine("table, tr, th, td { border:1px solid #ff8000; border-collapse:collapse; }");
            sw.WriteLine("table { margin-bottom:16px; }");
            sw.WriteLine("th { text-align:left; }");
            sw.WriteLine("table.lst0 { }");
            sw.WriteLine("table.lst1 { }");
            sw.WriteLine("table.lst2 { }");
            sw.WriteLine("table.img { }");
            sw.WriteLine("table.img tr.r0 { background-color:#fff4c4; }");
            sw.WriteLine("table.img tr.r1 { background-color:#ebf2f8; }");
            sw.WriteLine("table.img tr.r2 { background-color:#ffffff; }");
            sw.WriteLine("table.img.noChange { display:none; }");
            sw.Flush();
            sw.Close();
        }
    }
}
