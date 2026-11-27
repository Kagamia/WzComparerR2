using System;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// Exception thrown when an asset path does not exist, the type does not match, or the catalog cache location is invalid.
    /// </summary>
    public class ClassicAssetException : Exception
    {
        public ClassicAssetException(string message)
            : base(message)
        {
        }

        public ClassicAssetException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
