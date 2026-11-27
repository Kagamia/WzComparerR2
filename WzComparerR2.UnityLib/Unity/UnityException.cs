using System;

namespace WzComparerR2.UnityLib.Unity
{
    /// <summary>
    /// Exception thrown when parsing a Unity container (UnityFS / SerializedFile / object layout) fails.
    /// </summary>
    public class UnityException : Exception
    {
        public UnityException(string message)
            : base(message)
        {
        }

        public UnityException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
