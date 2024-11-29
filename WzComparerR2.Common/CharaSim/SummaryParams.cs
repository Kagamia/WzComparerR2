using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.CharaSim
{
    public struct SummaryParams
    {
        private string r;
        private string n;
        private string cStart;
        private string cEnd;
        private string eStart;
        private string eEnd;
        private string gStart;
        private string gEnd;

        /// <summary>
        /// 获取或设置回车符(\r)的替换字符串。
        /// </summary>
        public string R
        {
            get { return r; }
            set { r = value; }
        }

        /// <summary>
        /// 获取或设置换行符(\n)的替换字符串。
        /// </summary>
        public string N
        {
            get { return n; }
            set { n = value; }
        }

        /// <summary>
        /// 获取或设置高亮起始符(#c)的替换字符串。
        /// </summary>
        public string CStart
        {
            get { return cStart; }
            set { cStart = value; }
        }

        /// <summary>
        /// 获取或设置高亮结束符(#)的替换字符串
        /// </summary>
        public string CEnd
        {
            get { return cEnd; }
            set { cEnd = value; }
        }

        /// <summary>
        /// 获取或设置自定义高亮起始符(#e)的替换字符串。
        /// </summary>
        public string EStart
        {
            get { return eStart; }
            set { eStart = value; }
        }

        /// <summary>
        /// 获取或设置自定义高亮结束符(#)的替换字符串
        /// </summary>
        public string EEnd
        {
            get { return eEnd; }
            set { eEnd = value; }
        }

        /// <summary>
        /// 获取或设置自定义高亮起始符(#g)的替换字符串。
        /// </summary>
        public string GStart
        {
            get { return gStart; }
            set { gStart = value; }
        }

        /// <summary>
        /// 获取或设置自定义高亮结束符(#)的替换字符串
        /// </summary>
        public string GEnd
        {
            get { return gEnd; }
            set { gEnd = value; }
        }

        /// <summary>
        /// 获取默认的替换字符串组合。
        /// </summary>
        public static SummaryParams Default
        {
            get
            {
                return new SummaryParams()
                {
                    R = @"\r",
                    N = @"\n",
                    cStart = @"#c",
                    cEnd = @"#",
                    eStart = @"#e",
                    eEnd = @"#",
                    gStart = @"#g",
                    gEnd = @"#"
                };
            }
        }

        public static SummaryParams Text
        {
            get
            {
                return new SummaryParams()
                {
                    R = "\r",
                    N = "\n",
                    cStart = @"#c",
                    cEnd = @"#",
                    eStart = @"#e",
                    eEnd = @"#",
                    gStart = @"#g",
                    gEnd = @"#"
                };
            }
        }

        public static SummaryParams Html
        {
            get
            {
                return new SummaryParams()
                {
                    R = null,
                    N = "<br />",
                    cStart = @"<span style=""font-weight:bold; color:orange;"">",
                    cEnd = @"</span>",
                    eStart = @"<span style=""font-weight:bold; color:#aaffff;"">",
                    eEnd = @"</span>",
                    gStart = @"<span style=""font-weight:bold; color:#3f0;"">",
                    gEnd = @"</span>"
                };
            }
        }
    }
}
