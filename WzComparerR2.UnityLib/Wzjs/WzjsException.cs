using System;

namespace WzComparerR2.UnityLib.Wzjs
{
    /// <summary>
    /// Exception thrown when parsing a WZJS payload fails.
    /// </summary>
    public class WzjsException : Exception
    {
        public WzjsException(string message)
            : base(message)
        {
        }

        public WzjsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
