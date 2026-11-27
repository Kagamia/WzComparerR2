namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// Location of a single object in the catalog (bundle + node + absolute offset of the object data).
    /// </summary>
    public sealed class UnityEntryLocation
    {
        public UnityEntryLocation(int bundleIndex, int nodeIndex, long absoluteOffset, uint byteSize, int classId)
        {
            this.BundleIndex = bundleIndex;
            this.NodeIndex = nodeIndex;
            this.AbsoluteOffset = absoluteOffset;
            this.ByteSize = byteSize;
            this.ClassId = classId;
        }

        public int BundleIndex { get; private set; }

        public int NodeIndex { get; private set; }

        /// <summary>Absolute offset of the object data within the node (already includes the SerializedFile dataOffset).</summary>
        public long AbsoluteOffset { get; private set; }

        public uint ByteSize { get; private set; }

        /// <summary>Asset type: 114 = MonoBehaviour (WZJS), 49 = TextAsset, 83 = AudioClip, 28 = Texture2D.</summary>
        public int ClassId { get; private set; }
    }
}
