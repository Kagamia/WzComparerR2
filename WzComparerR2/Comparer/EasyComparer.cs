using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using WzComparerR2.WzLib;

namespace WzComparerR2
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
            Dictionary<Wz_Type, List<CompareDifference>> differences = new Dictionary<Wz_Type, List<CompareDifference>>();

            WzFileComparer comparer = new WzFileComparer();
            IEnumerable<CompareDifference> comp = comparer.Compare(fileNew.Node, fileOld.Node);
            int count = 0;

            StateInfo = "正在对比wz概况...";

            foreach (CompareDifference diff in comp)
            {
                Wz_File wzf = null;
                if (diff.NodeNew != null)
                {
                    wzf = diff.NodeNew.GetNodeWzFile();
                }
                else if (diff.NodeOld != null)
                {
                    wzf = diff.NodeOld.GetNodeWzFile();
                }

                Wz_Type wztype = (wzf == null) ? wzf.Type : Wz_Type.Unknown;

                List<CompareDifference> diffLst;
                if (!differences.TryGetValue(wzf.Type, out diffLst))
                {
                    diffLst = new List<CompareDifference>();
                    differences[wzf.Type] = diffLst;
                }
                diffLst.Add(diff);

                count++;
            }

            CreateStyleSheet(outputDir);

            foreach (var kv in differences)
            {
                Wz_File f1 = null, f2 = null;
                foreach (Wz_File f in fileNew.WzStructure.wz_files)
                {
                    if (f.Type == kv.Key)
                    {
                        f1 = f;
                        break;
                    }
                }
                foreach (Wz_File f in fileOld.WzStructure.wz_files)
                {
                    if (f.Type == kv.Key)
                    {
                        f2 = f;
                        break;
                    }
                }
                if (f1 != null && f2 != null)
                {
                    OutputFile(f1, f2, kv.Key, kv.Value, outputDir);
                }
                else
                {
                    throw new Exception("怎么可能呢...");
                }
                GC.Collect();
            }
        }

        private void OutputFile(Wz_File fileNew, Wz_File fileOld, Wz_Type type, List<CompareDifference> diffLst, string outputDir)
        {
            string htmlFilePath = Path.Combine(outputDir, type.ToString() + ".html");
            string srcDirPath = Path.Combine(outputDir, type.ToString() + "_files");
            if (OutputPng && !Directory.Exists(srcDirPath))
                Directory.CreateDirectory(srcDirPath);

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
                sw.WriteLine("<title>{0} {1}←{2}</title>", type, fileNew.Header.WzVersion, fileOld.Header.WzVersion);
                sw.WriteLine("<link type=\"text/css\" rel=\"stylesheet\" href=\"style.css\" />");
                sw.WriteLine("</head>");
                sw.WriteLine("<body>");
                //输出概况
                sw.WriteLine("<p class=\"wzf\">");
                sw.WriteLine("新文件：{0}<br />", fileNew.Header.FileName);
                sw.WriteLine("旧文件：{0}<br />", fileOld.Header.FileName);
                sw.WriteLine("文件大小：{0:n0} bytes ← {1:n0} bytes<br />", fileNew.Header.FileSize, fileOld.Header.FileSize);
                sw.WriteLine("文件版本：{0} ← {1}<br />", fileNew.Header.WzVersion, fileOld.Header.WzVersion);
                sw.WriteLine("对比时间：{0:yyyy-MM-dd HH:mm:ss.fff}<br />", DateTime.Now);
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
                    sw.WriteLine("<br />");
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
            sw.WriteLine("<table class=\"img\">");
            sw.WriteLine("<tr><th colspan=\"3\"><a name=\"{1}\">{0}</a> 修改:{2} 新增:{3} 移除:{4}</th></tr>", imgName, anchorName, count[0], count[1], count[2]);
            sw.WriteLine(sb.ToString());
            sw.WriteLine("<tr><td colspan=\"3\"><a href=\"#{1}\">{0}</a></td></tr>", "回到目录", menuAnchorName);
            sw.WriteLine("</table>");
            sw.WriteLine("<br />");
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
            sw.WriteLine("<br />");
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
                    string filePath = fullPath.Replace('\\', '.') + "_" + col + ".png";

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
            sw.WriteLine("th { text-align:left; }");
            sw.WriteLine("table.lst0 { }");
            sw.WriteLine("table.lst1 { }");
            sw.WriteLine("table.lst2 { }");
            sw.WriteLine("table.img { }");
            sw.WriteLine("table.img tr.r0 { background-color:#fff4c4; }");
            sw.WriteLine("table.img tr.r1 { background-color:#ebf2f8; }");
            sw.WriteLine("table.img tr.r2 { background-color:#ffffff; }");
            sw.Flush();
            sw.Close();
        }
    }
}
