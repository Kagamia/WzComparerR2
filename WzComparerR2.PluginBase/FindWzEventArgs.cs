using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;

namespace WzComparerR2
{
    public class FindWzEventArgs:EventArgs
    {
        public FindWzEventArgs()
        {
        }

        public FindWzEventArgs(Wz_Type type)
        {
            this.wzType = type;
        }

        private string fullPath;
        private Wz_Type wzType;
        private Wz_File wzFile;
        private Wz_Node wzNode;

        /// <summary>
        /// 获取或设置要查找wz节点的完全名称，用于输入参数。
        /// </summary>
        public string FullPath
        {
            get { return fullPath; }
            set { fullPath = value; }
        }

        /// <summary>
        /// 获取或设置要查找wz节点的Wz_Type，用于输入参数。
        /// </summary>
        public Wz_Type WzType
        {
            get { return wzType; }
            set { wzType = value; }
        }

        /// <summary>
        /// 获取或设置要查找wz节点的所属Wz_File，用于输入和输出参数。
        /// </summary>
        public Wz_File WzFile
        {
            get { return wzFile; }
            set { wzFile = value; }
        }

        /// <summary>
        /// 获取或设置要查找wz节点的Wz_Node，用于输出参数。
        /// </summary>
        public Wz_Node WzNode
        {
            get { return wzNode; }
            set { wzNode = value; }
        }
    }
}
