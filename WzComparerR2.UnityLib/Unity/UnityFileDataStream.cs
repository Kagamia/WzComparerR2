using System;
using System.IO;

namespace WzComparerR2.UnityLib.Unity
{
    /// <summary>
    /// Random-access read source implemented on top of a shared <see cref="FileStream"/> plus seek.
    /// </summary>
    public sealed class UnityFileDataStream : IUnityDataStream
    {
        private readonly object syncRoot = new object();
        private readonly FileStream stream;
        private readonly long length;

        public UnityFileDataStream(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            this.stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            this.length = this.stream.Length;
        }

        public long Length
        {
            get { return this.length; }
        }

        public void ReadAt(long offset, byte[] buffer, int bufferOffset, int count)
        {
            if (offset < 0 || count < 0 || offset > this.length - count)
            {
                throw new UnityException(string.Format(
                    "File read out of range: offset={0}, count={1}, length={2}", offset, count, this.length));
            }
            lock (this.syncRoot)
            {
                this.stream.Position = offset;
                int read = 0;
                while (read < count)
                {
                    int got = this.stream.Read(buffer, bufferOffset + read, count - read);
                    if (got <= 0)
                    {
                        throw new UnityException(string.Format(
                            "Insufficient file read: offset={0}, {1} bytes required, {2} bytes read", offset, count, read));
                    }
                    read += got;
                }
            }
        }

        public void Dispose()
        {
            this.stream.Dispose();
        }
    }
}
