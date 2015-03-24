using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace WzComparerR2
{
    public static class QueryPerformance
    {
        static QueryPerformance()
        {
            counter = 0;
            IsSupposed = QueryPerformanceFrequency(ref counter);
            if (!IsSupposed)
                throw new Exception("QueryPerformance无法初始化。");
        }

        [DllImport("kernel32.dll")]
        private extern static bool QueryPerformanceCounter(ref long lPerformanceCounter);
        [DllImport("kernel32.dll")]
        private extern static bool QueryPerformanceFrequency(ref long lFrequency);

        public static bool IsSupposed;
        private static long counter;
        private static long length;
        private static long tempStart;

        /// <summary>
        /// 启动计时器，开始计时。
        /// </summary>
        public static void Start()
        {
            if (IsSupposed)
            {
                length = 0;
                QueryPerformanceCounter(ref tempStart);
            }
        }

        /// <summary>
        /// 关闭计时器，结束计时。
        /// </summary>
        public static void End()
        {
            if (IsSupposed && length == 0)
            {
                QueryPerformanceCounter(ref length);
                length -= tempStart;
            }
        }
        /// <summary>
        /// 返回上次开始结束计时的时间间隔，单位为秒。
        /// </summary>
        /// <returns></returns>
        public static double GetLastInterval()
        {
            return (double)length / counter;
        }

        public static long GetLastCount()
        {
            return length;
        }
    }

}
