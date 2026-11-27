using System;

namespace WzComparerR2.WzLib
{
    /// <summary>
    /// A node value that provides an image from PNG-encoded data.
    /// <para>
    /// Conventional WZ images use <see cref="Wz_Png"/> (pixels live inside the Wz file's data stream);
    /// other sources (for example, a Canvas node in a Unity asset) only need to implement this interface
    /// to reuse the existing image decoding and tooltip flow as-is.
    /// </para>
    /// </summary>
    public interface IWzPngSource
    {
        /// <summary>Gets the PNG-encoded bytes. Returns null when there is no image.</summary>
        byte[] GetPngData();
    }
}
