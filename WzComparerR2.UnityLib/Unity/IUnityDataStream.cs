namespace WzComparerR2.UnityLib.Unity
{
    /// <summary>
    /// Random-access read abstraction over a bundle file. The desktop build implements it with <see cref="UnityFileDataStream"/>,
    /// so other sources (for example in-memory or network caches) can reuse the whole parsing pipeline by implementing this interface.
    /// </summary>
    public interface IUnityDataStream : System.IDisposable
    {
        /// <summary>Total length of the file.</summary>
        long Length { get; }

        /// <summary>Reads the full buffer starting at an absolute offset.</summary>
        void ReadAt(long offset, byte[] buffer, int bufferOffset, int count);
    }
}
