using System;
using System.IO;
using System.Text;
using WzComparerR2.WzLib.Utilities;

namespace WzComparerR2.WzLib
{
    public class Wz_Video : IMapleStoryBlob
    {
        public Wz_Video(uint offset, int length, Wz_Image wz_Image)
        {
            this.Offset = offset;
            this.Length = length;
            this.WzImage = wz_Image;
        }

        public uint Offset { get; set; }
        public int Length { get; set; }
        public Wz_Image WzImage { get; set; }
        public IMapleStoryFile WzFile => this.WzImage?.WzFile;

        public McvHeader ReadVideoFileHeader()
        {
            lock (this.WzFile.ReadLock)
            {
                var s = this.WzImage.OpenRead();
                s.Position = this.Offset;
                var br = new BinaryReader(s, Encoding.ASCII);
                string signature = new string(br.ReadChars(4));
                if (signature != "MCV0")
                {
                    throw new Exception("File signature does not match.");
                }
                s.Position += 2;
                int headerLen = br.ReadUInt16();
                uint fourcc = br.ReadUInt32() ^ 0xa5a5a5a5;
                int width = br.ReadUInt16();
                int height = br.ReadUInt16();
                int frameCount = br.ReadInt32();
                McvDataFlags dataFlag = (McvDataFlags)br.ReadByte();
                s.Position += 3;
                long frameDelayUnit = br.ReadInt64();
                int defaultDelay = br.ReadInt32();
                s.Position = this.Offset + headerLen;

                McvFrameInfo[] frames = new McvFrameInfo[frameCount];
                // read base data
                for (int i = 0; i < frames.Length; i++)
                {
                    var fi = new McvFrameInfo();
                    fi.DataOffset = br.ReadInt32();
                    fi.DataCount = br.ReadInt32();
                    frames[i] = fi;
                }
                // read alpha map data
                if ((dataFlag & McvDataFlags.AlphaMap) != 0)
                {
                    foreach (var fi in frames)
                    {
                        fi.AlphaDataOffset = br.ReadInt32();
                        fi.AlphaDataCount = br.ReadInt32();
                    }
                }
                // read frame delay
                if ((dataFlag & McvDataFlags.PerFrameDelay) != 0)
                {
                    foreach (var fi in frames)
                    {
                        fi.DelayInNanoseconds = br.ReadInt32() * frameDelayUnit;
                    }
                }
                else
                {
                    foreach (var fi in frames)
                    {
                        fi.DelayInNanoseconds = defaultDelay * frameDelayUnit;
                    }
                }
                // read frame timeline
                if ((dataFlag & McvDataFlags.PerFrameTimeline) != 0)
                {
                    foreach (var fi in frames)
                    {
                        fi.StartTimeInNanoseconds = br.ReadInt64() * frameDelayUnit;
                    }
                }
                else
                {
                    long time = 0;
                    foreach (var fi in frames)
                    {
                        fi.StartTimeInNanoseconds = time;
                        time += fi.DelayInNanoseconds;
                    }
                }

                // reassign data offset
                long dataStartPosition = s.Position - this.Offset;
                foreach (var fi in frames)
                {
                    fi.DataOffset += dataStartPosition;
                    if (fi.AlphaDataCount > 0 && fi.AlphaDataOffset > -1)
                    {
                        fi.AlphaDataOffset += dataStartPosition;
                    }
                }

                return new McvHeader()
                {
                    Signature = signature,
                    HeaderLength = headerLen,
                    FourCC = fourcc,
                    Width = width,
                    Height = height,
                    FrameCount = frameCount,
                    DataFlag = dataFlag,
                    Frames = frames,
                };
            }
        }

        public void CopyTo(byte[] buffer, int offset)
        {
            if (buffer.Length - offset < this.Length)
            {
                throw new ArgumentException("Insufficient buffer size");
            }
            lock (this.WzFile.ReadLock)
            {
                var s = this.WzImage.OpenRead();
                s.Position = this.Offset;
                s.ReadExactly(buffer, offset, this.Length);
            }
        }

        public void CopyTo(Span<byte> span)
        {
            if (span.Length < this.Length)
            {
                throw new ArgumentException("Insufficient buffer size");
            }
            lock (this.WzFile.ReadLock)
            {
                var s = this.WzImage.OpenRead();
                s.Position = this.Offset;
                s.ReadExactly(span.Slice(0, this.Length));
            }
        }
    }
}
