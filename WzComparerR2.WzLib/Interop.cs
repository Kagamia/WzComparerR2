using System;
using System.Runtime.InteropServices;

namespace WzComparerR2.WzLib
{
    public static partial class Interop
    {
        /// <summary>
        /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/strmif/ns-strmif-am_media_type" />
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public sealed class AM_MEDIA_TYPE
        {
            public Guid MajorType;
            public Guid SubType;
            [MarshalAs(UnmanagedType.Bool)]
            public bool FixedSizeSamples;
            [MarshalAs(UnmanagedType.Bool)]
            public bool TemporalCompression;
            public uint SampleSize;
            public Guid FormatType;
            [MarshalAs(UnmanagedType.IUnknown)]
            public object Unknown;
            public uint CbFormat;
            // this should be IntPtr, but we don't really marshal it.
            public object PbFormat;
        }

        /// <summary>
        /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/mmreg/ns-mmreg-waveformatex" />
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct WAVEFORMATEX
        {
            public ushort FormatTag;
            public ushort Channels;
            public uint SamplesPerSec;
            public uint AvgBytesPerSec;
            public ushort BlockAlign;
            public ushort BitsPerSample;
            public ushort CbSize;
        }

        /// <summary>
        /// <see href="https://learn.microsoft.com/en-us/windows/win32/api/mmreg/ns-mmreg-mpeglayer3waveformat" />
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MPEGLAYER3WAVEFORMAT
        {
            public WAVEFORMATEX Wfx;
            public ushort ID;
            public uint Flags;
            public ushort BlockSize;
            public ushort FramesPerBlock;
            public ushort CodecDelay;
        }

        public static readonly Guid MEDIATYPE_Stream = Guid.Parse("{e436eb83-524f-11ce-9f53-0020af0ba770}");
        public static readonly Guid MEDIASUBTYPE_MPEG1Audio = Guid.Parse("{e436eb87-524f-11ce-9f53-0020af0ba770}");
        public static readonly Guid MEDIASUBTYPE_WAVE = Guid.Parse("{e436eb8b-524f-11ce-9f53-0020af0ba770}");
        public static readonly Guid FORMAT_WaveFormatEx = Guid.Parse("{05589f81-c356-11ce-bf01-00aa0055595a}");
        public const ushort WAVE_FORMAT_PCM = 0x0001;
        public const ushort WAVE_FORMAT_MPEGLAYER3 = 0x0055;
        public const uint MPEGLAYER3_WFX_EXTRA_BYTES = 12;
    }
}
