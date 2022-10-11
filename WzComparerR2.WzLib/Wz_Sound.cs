using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.WzLib
{
    public class Wz_Sound
    {
        public Wz_Sound(uint offset, int length, byte[] header, int ms, Wz_Image wz_i)
        {
            this.offset = offset;
            this.dataLength = length;
            this.header = header;
            this.ms = ms;
            this.wz_i = wz_i;
            TryDecryptHeader();
        }

        private uint offset;
        private byte[] header;
        private int dataLength;
        private int ms;

        private Wz_Image wz_i;

        /// <summary>
        /// 获取或设置数据块对于文件的偏移。
        /// </summary>
        public uint Offset
        {
            get { return offset; }
            set { offset = value; }
        }

        /// <summary>
        /// 获取或设置头部字节段。
        /// </summary>
        public byte[] Header
        {
            get { return header; }
            set { header = value; }
        }

        public int Frequency
        {
            get
            {
                if (header == null || header.Length < 0x3c)
                {
                    return 0;
                }
                return BitConverter.ToInt32(header, 0x38);
            }
        }

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

        /// <summary>
        /// 获取或设置图片所属的WzFile。
        /// </summary>
        public Wz_File WzFile
        {
            get { return wz_i.WzFile; }
            set { wz_i.WzFile = value; }
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
                Wz_SoundType soundType;
                if (this.header == null)
                {
                    soundType = Wz_SoundType.Mp3;
                }
                else
                {
                    switch (this.header.Length)
                    {
                        default:
                        case 0x52:
                            soundType = Wz_SoundType.Mp3;
                            break;

                        case 0x46:
                            {
                                if (this.Frequency == this.dataLength && this.Ms == 1000)
                                {
                                    soundType = Wz_SoundType.Binary;
                                }
                                else
                                {
                                    soundType = Wz_SoundType.WavRaw;
                                }
                            }
                            break;
                    }
                }

                return soundType;
            }
        }

        public byte[] ExtractSound()
        {
            switch (this.SoundType)
            {
                case Wz_SoundType.Mp3:
                    {
                        byte[] data = new byte[this.dataLength];
                        this.WzFile.FileStream.Seek(this.offset, System.IO.SeekOrigin.Begin);
                        this.WzFile.FileStream.Read(data, 0, this.dataLength);
                        return data;
                    }
                case Wz_SoundType.WavRaw:
                    {
                        byte[] data = new byte[this.dataLength + 44];
                        this.WzFile.FileStream.Seek(this.offset, System.IO.SeekOrigin.Begin);
                        this.WzFile.FileStream.Read(data, 44, this.dataLength);
                        byte[] wavHeader = new byte[44]{
                          0x52,0x49,0x46,0x46, //"RIFF"
                          0,0,0,0, //ChunkSize
                          0x57,0x41,0x56,0x45, //"WAVE"

                          0x66,0x6d,0x74,0x20, //"fmt "
                          0x10,0,0,0, //chunk1Size
                          0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, // copy16字节

                          0x64,0x61,0x74,0x61, //"data"
                          0,0,0,0 //chunk2Size
                        };
                        Array.Copy(BitConverter.GetBytes(this.dataLength + 36), 0, wavHeader, 4, 4);
                        Array.Copy(this.header, 0x34, wavHeader, 20, 16);
                        Array.Copy(BitConverter.GetBytes(this.dataLength), 0, wavHeader, 40, 4);
                        Array.Copy(wavHeader, data, wavHeader.Length);
                        return data;
                    }
            }
            return null;
        }

        private void TryDecryptHeader()
        {
            if (this.header == null)
            {
                return;
            }
            if (this.header.Length > 51)
            {
                byte waveFormatLen = this.header[51];
                if (this.header.Length != 52 + waveFormatLen) //长度错误
                {
                    return;
                }
                int cbSize = BitConverter.ToUInt16(this.header, 52 + 16);
                if (cbSize + 18 != waveFormatLen)
                {
                    byte[] tempHeader = new byte[waveFormatLen];
                    Buffer.BlockCopy(this.header, 52, tempHeader, 0, tempHeader.Length);
                    var encKeys = this.WzImage.EncKeys;
                    encKeys.Decrypt(tempHeader, 0, tempHeader.Length); //解密
                    cbSize = BitConverter.ToUInt16(tempHeader, 16); //重新验证
                    if (cbSize + 18 == waveFormatLen)
                    {
                        Buffer.BlockCopy(tempHeader, 0, this.header, 52, tempHeader.Length);
                    }
                }
            }
        }
    }
}
