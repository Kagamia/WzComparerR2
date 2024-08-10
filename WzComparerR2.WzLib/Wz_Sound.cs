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
        /// 获取或设置数据块对于文件的偏移。
        /// </summary>
        public uint Offset
        {
            get { return offset; }
            set { offset = value; }
        }

        public int Channels => this.mediaType?.PbFormat switch
        {
            Interop.WAVEFORMATEX waveFmtEx => (int)waveFmtEx.Channels,
            Interop.MPEGLAYER3WAVEFORMAT mp3WaveFmt => (int)mp3WaveFmt.Wfx.Channels,
            _ => 0,
        };

        public int Frequency => this.mediaType?.PbFormat switch
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
            get { return dataLength; }
            set { dataLength = value; }
        }

        /// <summary>
        /// 获取或设置Mp3的声音毫秒数。
        /// </summary>
        public int Ms
        {
            get { return ms; }
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
