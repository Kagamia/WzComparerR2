using System;
using WzComparerR2.WzLib;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// A WZ-like Canvas node value: represents "the image at some Unity asset path";
    /// the PNG is decoded on first use (equivalent to the classic WZ <c>Wz_Png</c>).
    /// <para>
    /// By inheriting from <see cref="Wz_Png"/>, existing code that only handles WZ types (MapRender, preview tools, etc.)
    /// can use it as-is: the image is provided by <see cref="Wz_Png.PngDataProvider"/>, and
    /// <see cref="Wz_Png.ExtractPng(int)"/> decodes this PNG instead of reading a WZ data stream.
    /// </para>
    /// </summary>
    public sealed class UnityCanvas : Wz_Png, IWzPngSource
    {
        private readonly ClassicWzSource source;
        private readonly object syncRoot = new object();
        private byte[] png;
        private long pngGeneration = -1;

        public UnityCanvas(ClassicWzSource source, string path)
            : base(0, 0, 0, Wz_TextureFormat.ARGB8888, 0, 0, 0, 0, null)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            this.source = source;
            this.Path = path;
            this.PngDataProvider = this.GetPng;
        }

        /// <summary>Logical path, e.g. <c>Character/00002000/stand1/0/body</c>.</summary>
        public string Path { get; private set; }

        /// <summary>Gets (and caches) the PNG bytes; returns null when no matching image is found.</summary>
        public byte[] GetPng()
        {
            long generation = this.source.LocaleGeneration;
            byte[] cached = this.png;
            if (cached != null && this.pngGeneration == generation)
            {
                return cached;
            }
            lock (this.syncRoot)
            {
                if (this.png == null || this.pngGeneration != generation)
                {
                    // After a locale switch, the same Canvas may map to an image from another region, so it must be decoded again.
                    this.png = this.source.GetImage(this.Path);
                    this.pngGeneration = generation;
                }
                return this.png;
            }
        }

        byte[] IWzPngSource.GetPngData()
        {
            return this.GetPng();
        }

        public override string ToString()
        {
            return "canvas " + this.Path;
        }
    }
}
