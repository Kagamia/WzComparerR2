using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using WzComparerR2.WzLib.Utilities;

namespace WzComparerR2.WzLib
{
    public class Wz_Sound : IMapleStoryBlob
    {
        public Wz_Sound(uint offset, int length, int ms, Interop.AM_MEDIA_TYPE mediaType, Wz_Image wz_i)
        {
            this.offset = offset;
            this.dataLength = length;
            this.ms = ms;
            this.mediaType = mediaType;
            this.wz_i = wz_i;
        }

        private uint offset;
        private int dataLength;
        private int ms;
        private Interop.AM_MEDIA_TYPE mediaType;
        private Wz_Image wz_i;

        /// <summary>
        /// Sound data provider for non-WZ file sources (e.g. a Unity dataset AudioClip).
        /// When set, sound data is taken from here and <see cref="DataLength"/> reports the actual length.
        /// </summary>
        public Func<byte[]> DataProvider { get; set; }

        /// <summary>Channel count of a non-WZ source (null for WZ sources).</summary>
        public int? ExternalChannels { get; set; }

        /// <summary>Sampling rate of a non-WZ source (null for WZ sources).</summary>
        public int? ExternalFrequency { get; set; }

        /// <summary>Length of a non-WZ source in milliseconds (null for WZ sources).</summary>
        public int? ExternalMs { get; set; }

        /// <summary>Sound format of a non-WZ source. When set, the format is not guessed from the data.</summary>
        public Wz_SoundType? ExternalSoundType { get; set; }

        private byte[] providerCache;

        /// <summary>Gets sound data from an external source (cached).</summary>
        private byte[] GetProviderData()
        {
            Func<byte[]> provider = this.DataProvider;
            if (provider == null)
            {
                return null;
            }
            byte[] cached = this.providerCache;
            if (cached == null)
            {
                cached = provider();
                this.providerCache = cached;
            }
            return cached;
        }

        private Wz_SoundType DetectProviderSoundType(byte[] data)
        {
            if (data == null || data.Length < 4)
            {
                return Wz_SoundType.Unknown;
            }
            if (data[0] == 'O' && data[1] == 'g' && data[2] == 'g' && data[3] == 'S')
            {
                return Wz_SoundType.Ogg;
            }
            if (data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F')
            {
                return Wz_SoundType.Pcm;
            }
            if ((data[0] == 'I' && data[1] == 'D' && data[2] == '3')
                || (data[0] == 0xFF && (data[1] & 0xE0) == 0xE0))
            {
                return Wz_SoundType.Mp3;
            }
            return Wz_SoundType.Unknown;
        }

        /// <summary>
        /// 获取或设置数据块对于文件的偏移。
        /// </summary>
        public uint Offset
        {
            get { return offset; }
            set { offset = value; }
        }

        public int Channels => this.ExternalChannels ?? this.mediaType?.PbFormat switch
        {
            Interop.WAVEFORMATEX waveFmtEx => (int)waveFmtEx.Channels,
            Interop.MPEGLAYER3WAVEFORMAT mp3WaveFmt => (int)mp3WaveFmt.Wfx.Channels,
            _ => 0,
        };

        public int Frequency => this.ExternalFrequency ?? this.mediaType?.PbFormat switch
        {
            Interop.WAVEFORMATEX waveFmtEx => (int)waveFmtEx.SamplesPerSec,
            Interop.MPEGLAYER3WAVEFORMAT mp3WaveFmt => (int)mp3WaveFmt.Wfx.SamplesPerSec,
            _ => 0,
        };

        /// <summary>
        /// 获取或设置数据块的长度。
        /// </summary>
        public int DataLength
        {
            get
            {
                if (this.DataProvider != null)
                {
                    byte[] data = this.GetProviderData();
                    return data == null ? 0 : data.Length;
                }
                return dataLength;
            }
            set { dataLength = value; }
        }

        /// <summary>
        /// 获取或设置Mp3的声音毫秒数。
        /// </summary>
        public int Ms
        {
            get { return this.ExternalMs ?? ms; }
            set { ms = value; }
        }

        public Interop.AM_MEDIA_TYPE MediaType
        {
            get { return mediaType; }
            set { mediaType = value; }
        }

        /// <summary>
        /// 获取或设置图片所属的WzFile。
        /// </summary>
        public IMapleStoryFile WzFile
        {
            get { return wz_i?.WzFile; }
        }

        /// <summary>
        /// 获取或设置图片所属的WzImage。
        /// </summary>
        public Wz_Image WzImage
        {
            get { return wz_i; }
            set { wz_i = value; }
        }

        public Wz_SoundType SoundType
        {
            get
            {
                if (this.ExternalSoundType.HasValue)
                {
                    return this.ExternalSoundType.Value;
                }
                if (this.DataProvider != null)
                {
                    return this.DetectProviderSoundType(this.GetProviderData());
                }

                if (this.mediaType?.MajorType == Interop.MEDIATYPE_Stream)
                {
                    if (this.mediaType.SubType == Interop.MEDIASUBTYPE_MPEG1Audio)
                    {
                        return Wz_SoundType.Mp3;
                    }
                    else if (this.mediaType.SubType == Interop.MEDIASUBTYPE_WAVE)
                    {
                        if (this.mediaType.PbFormat is Interop.MPEGLAYER3WAVEFORMAT)
                        {
                            return Wz_SoundType.Mp3;
                        }
                        else if (this.mediaType.PbFormat is Interop.WAVEFORMATEX waveFmtEx && waveFmtEx.FormatTag == Interop.WAVE_FORMAT_PCM)
                        {
                            if (this.Ms == 1000 && this.Frequency == this.dataLength)
                            {
                                return Wz_SoundType.Binary;
                            }
                            else
                            {
                                return Wz_SoundType.Pcm;
                            }
                        }
                    }
                }

                return Wz_SoundType.Unknown;
            }
        }

        int IMapleStoryBlob.Length => this.DataLength;

        public byte[] ExtractSound()
        {
            if (this.DataProvider != null)
            {
                return this.GetProviderData();
            }

            switch (this.SoundType)
            {
                case Wz_SoundType.Mp3:
                    {
                        byte[] data = new byte[this.dataLength];
                        this.CopyTo(data, 0);
                        return data;
                    }
                case Wz_SoundType.Pcm:
                    {
                        var waveFmtEx = (Interop.WAVEFORMATEX)this.mediaType.PbFormat;
                        byte[] data = new byte[this.dataLength + 44];
                        using var ms = new MemoryStream(data, true);
                        using var br = new BinaryWriter(ms);
                        br.Write(new byte[] { 0x52, 0x49, 0x46, 0x46 }); //"RIFF"
                        br.Write(this.dataLength + 36); //chunkSize
                        br.Write(new byte[] { 0x57, 0x41, 0x56, 0x45 }); //"WAVE"
                        br.Write(new byte[] { 0x66, 0x6d, 0x74, 0x20 }); //"fmt "
                        br.Write(16); //chunk1Size
                        br.Write(waveFmtEx.FormatTag);
                        br.Write(waveFmtEx.Channels);
                        br.Write(waveFmtEx.SamplesPerSec);
                        br.Write(waveFmtEx.AvgBytesPerSec);
                        br.Write(waveFmtEx.BlockAlign);
                        br.Write(waveFmtEx.BitsPerSample);
                        br.Write(new byte[] { 0x64, 0x61, 0x74, 0x61 }); //"data"
                        br.Write(this.dataLength); //chunk2Size
                        this.CopyTo(data, 44);
                        return data;
                    }
            }
            return null;
        }

        public void CopyTo(byte[] buffer, int offset)
        {
            if (this.DataProvider != null)
            {
                byte[] data = this.GetProviderData();
                if (data == null)
                {
                    throw new InvalidOperationException("The external audio data does not exist.");
                }
                if (buffer.Length - offset < data.Length)
                {
                    throw new ArgumentException("Insufficient buffer size");
                }
                Buffer.BlockCopy(data, 0, buffer, offset, data.Length);
                return;
            }

            if (buffer.Length - offset < this.DataLength)
            {
                throw new ArgumentException("Insufficient buffer size");
            }
            lock (this.WzFile.ReadLock)
            {
                var s = this.WzImage.OpenRead();
                s.Position = this.Offset;
                s.ReadExactly(buffer, offset, this.DataLength);
            }
        }

        public void CopyTo(Span<byte> span)
        {
            if (this.DataProvider != null)
            {
                byte[] data = this.GetProviderData();
                if (data == null)
                {
                    throw new InvalidOperationException("The external audio data does not exist.");
                }
                if (span.Length < data.Length)
                {
                    throw new ArgumentException("Insufficient buffer size");
                }
                data.AsSpan().CopyTo(span);
                return;
            }

            if (span.Length < this.DataLength)
            {
                throw new ArgumentException("Insufficient buffer size");
            }
            lock (this.WzFile.ReadLock)
            {
                var s = this.WzImage.OpenRead();
                s.Position = this.Offset;
                s.ReadExactly(span.Slice(0, this.DataLength));
            }
        }
    }
}
