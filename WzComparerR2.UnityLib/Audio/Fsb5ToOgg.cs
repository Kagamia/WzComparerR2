using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using WzComparerR2.UnityLib.Unity;

namespace WzComparerR2.UnityLib.Audio
{
    /// <summary>
    /// Repackages FSB5 (FMOD Sound Bank, Vorbis codec) into Ogg Vorbis.
    /// <para>
    /// The game's AudioClip sound data uses an FMOD-specific container: the Ogg layer is stripped from the Vorbis packets,
    /// each packet is prefixed with a u16(LE) length, the codebook (setup header) is not stored in the file,
    /// only a single crc32 is kept, and it maps to one of FMOD's built-in fixed setups.
    /// </para>
    /// <para>
    /// Reconstruction (same as the reference implementation in ClassicMS-MPST's <c>audio.rs</c>):
    /// read the crc32 from the VORBISDATA chunk in the sample header and look it up in the embedded setup table
    /// (<c>Audio/vorbis_setups.bin</c>, 13 entries, derived from Fmod5Sharp's
    /// <c>vorbis_headers_converted.json</c>). Then build the three header packets (info / comment / setup) manually and
    /// group the u16-prefixed Vorbis packets into Ogg pages, accumulating granule positions per the block flags.
    /// The output can be played or exported directly.
    /// </para>
    /// </summary>
    public static class Fsb5ToOgg
    {
        private const int HeaderSize = 60;
        private const uint CodecVorbis = 0x0F;
        private const uint ChunkVorbisData = 11;

        private static readonly uint[] Frequencies =
        {
            4000, 8000, 11000, 12000, 16000, 22050, 24000, 32000, 44100, 48000, 96000,
        };

        private static readonly byte[] ChannelCounts = { 1, 2, 6, 8 };

        #region setup table (crc32 → vorbis setup header)

        private sealed class SetupEntry
        {
            public SetupEntry(uint seekBit, byte[] header)
            {
                this.SeekBit = seekBit;
                this.Header = header;
            }

            public uint SeekBit { get; private set; }
            public byte[] Header { get; private set; }
        }

        private static Dictionary<uint, SetupEntry> setups;

        private static Dictionary<uint, SetupEntry> GetSetups()
        {
            if (setups != null)
            {
                return setups;
            }
            lock (typeof(Fsb5ToOgg))
            {
                if (setups != null)
                {
                    return setups;
                }

                byte[] bin = ReadEmbeddedSetups();
                Dictionary<uint, SetupEntry> table = new Dictionary<uint, SetupEntry>();
                int pos = 0;
                uint count = ReadU32(bin, ref pos);
                for (uint i = 0; i < count; i++)
                {
                    uint crc = ReadU32(bin, ref pos);
                    uint seekBit = ReadU32(bin, ref pos);
                    int length = checked((int)ReadU32(bin, ref pos));
                    if (length < 0 || pos + length > bin.Length)
                    {
                        throw new UnityException("The embedded vorbis setup table is corrupted.");
                    }
                    byte[] header = new byte[length];
                    Buffer.BlockCopy(bin, pos, header, 0, length);
                    pos += length;
                    table[crc] = new SetupEntry(seekBit, header);
                }
                setups = table;
                return table;
            }
        }

        private static byte[] ReadEmbeddedSetups()
        {
            Assembly assembly = typeof(Fsb5ToOgg).Assembly;
            using (Stream stream = assembly.GetManifestResourceStream(
                "WzComparerR2.UnityLib.Audio.vorbis_setups.bin"))
            {
                if (stream == null)
                {
                    throw new UnityException("The embedded vorbis setup table was not found.");
                }
                using (MemoryStream buffer = new MemoryStream())
                {
                    stream.CopyTo(buffer);
                    return buffer.ToArray();
                }
            }
        }

        private static uint ReadU32(byte[] data, ref int pos)
        {
            if (pos + 4 > data.Length)
            {
                throw new UnityException("The embedded vorbis setup table is truncated.");
            }
            uint value = (uint)(data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] << 24));
            pos += 4;
            return value;
        }

        #endregion

        #region bit reader (vorbis bit order: LSB first)

        private sealed class BitReader
        {
            private readonly byte[] data;
            private int position;

            public BitReader(byte[] data)
            {
                this.data = data;
            }

            public int Position
            {
                get { return this.position; }
                set { this.position = value; }
            }

            public int Read(int bits)
            {
                int value = 0;
                for (int i = 0; i < bits; i++)
                {
                    int index = this.position >> 3;
                    if (index >= this.data.Length)
                    {
                        throw new UnityException("The vorbis bit reader went out of range.");
                    }
                    value |= ((this.data[index] >> (this.position & 7)) & 1) << i;
                    this.position++;
                }
                return value;
            }
        }

        /// <summary>Parses each mode's block flag from the setup header (0 = short window 256, 1 = long window 2048).</summary>
        private static int[] GetBlockFlags(byte[] setup, uint seekBit)
        {
            BitReader reader = new BitReader(setup);
            if (reader.Read(8) != 5)
            {
                throw new UnityException("The setup header is not packet type 5.");
            }
            for (int i = 0; i < 6; i++)
            {
                reader.Read(8); // "vorbis"
            }
            reader.Position = checked((int)seekBit);
            int modeCount = reader.Read(6) + 1;
            if (modeCount > 64)
            {
                throw new UnityException("Invalid vorbis mode count: " + modeCount);
            }
            int[] flags = new int[modeCount];
            for (int i = 0; i < modeCount; i++)
            {
                flags[i] = reader.Read(1);
                reader.Read(16);
                reader.Read(16);
                reader.Read(8);
            }
            return flags;
        }

        /// <summary>Block size of a single vorbis packet (0 for non-audio packets).</summary>
        private static int GetPacketBlockSize(byte[] packet, int[] flags)
        {
            if (packet == null || packet.Length == 0)
            {
                return 0;
            }
            BitReader reader = new BitReader(packet);
            int first;
            try
            {
                first = reader.Read(1);
            }
            catch (UnityException)
            {
                return 0;
            }
            if (first == 1)
            {
                return 0;
            }
            int mode = flags.Length > 1 ? reader.Read(flags.Length - 1) : 0;
            return mode < flags.Length && flags[mode] == 1 ? 2048 : 256;
        }

        #endregion

        #region Ogg page output

        private static readonly uint[] OggCrcTable = CreateOggCrcTable();

        private static uint[] CreateOggCrcTable()
        {
            uint[] table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint r = i << 24;
                for (int j = 0; j < 8; j++)
                {
                    r = (r & 0x80000000u) != 0 ? (r << 1) ^ 0x04C11DB7u : r << 1;
                }
                table[i] = r;
            }
            return table;
        }

        private static uint OggCrc(byte[] data, int offset, int length)
        {
            uint crc = 0;
            for (int i = offset; i < offset + length; i++)
            {
                crc = (crc << 8) ^ OggCrcTable[((crc >> 24) ^ data[i]) & 0xFF];
            }
            return crc;
        }

        /// <summary>Builds one Ogg page (multiple packets, lacing generated automatically).</summary>
        private static void PushPage(
            MemoryStream output, uint serial, uint sequence, ulong granule,
            IList<byte[]> packets, bool bos, bool eos)
        {
            List<byte> segments = new List<byte>();
            MemoryStream body = new MemoryStream();
            foreach (byte[] packet in packets)
            {
                body.Write(packet, 0, packet.Length);
                int remaining = packet.Length;
                while (remaining >= 255)
                {
                    segments.Add(255);
                    remaining -= 255;
                }
                segments.Add((byte)remaining);
            }

            int headerType = (bos ? 0x02 : 0) | (eos ? 0x04 : 0);
            byte[] bodyBytes = body.ToArray();
            int pageLength = 27 + segments.Count + bodyBytes.Length;
            byte[] page = new byte[pageLength];
            int pos = 0;
            page[pos++] = (byte)'O';
            page[pos++] = (byte)'g';
            page[pos++] = (byte)'g';
            page[pos++] = (byte)'S';
            page[pos++] = 0; // version
            page[pos++] = (byte)headerType;
            WriteU64(page, ref pos, granule);
            WriteU32(page, ref pos, serial);
            WriteU32(page, ref pos, sequence);
            WriteU32(page, ref pos, 0); // crc placeholder
            page[pos++] = (byte)segments.Count;
            foreach (byte segment in segments)
            {
                page[pos++] = segment;
            }
            Buffer.BlockCopy(bodyBytes, 0, page, pos, bodyBytes.Length);

            uint crc = OggCrc(page, 0, page.Length);
            page[22] = (byte)crc;
            page[23] = (byte)(crc >> 8);
            page[24] = (byte)(crc >> 16);
            page[25] = (byte)(crc >> 24);

            output.Write(page, 0, page.Length);
        }

        private static void WriteU32(byte[] buffer, ref int pos, uint value)
        {
            buffer[pos++] = (byte)value;
            buffer[pos++] = (byte)(value >> 8);
            buffer[pos++] = (byte)(value >> 16);
            buffer[pos++] = (byte)(value >> 24);
        }

        private static void WriteU64(byte[] buffer, ref int pos, ulong value)
        {
            WriteU32(buffer, ref pos, (uint)value);
            WriteU32(buffer, ref pos, (uint)(value >> 32));
        }

        #endregion

        /// <summary>Whether the FSB5 data is in a format this converter supports (Vorbis, single sample).</summary>
        public static bool IsSupported(byte[] fsb)
        {
            if (fsb == null || fsb.Length < HeaderSize
                || fsb[0] != 'F' || fsb[1] != 'S' || fsb[2] != 'B' || fsb[3] != '5')
            {
                return false;
            }
            uint sampleCount = BitConverter.ToUInt32(fsb, 8);
            uint codec = BitConverter.ToUInt32(fsb, 24) & 0x1F;
            return sampleCount == 1 && codec == CodecVorbis;
        }

        /// <summary>Converts an entire FSB5 file (single sample, Vorbis codec) into playable Ogg Vorbis.</summary>
        public static byte[] ToOgg(byte[] fsb)
        {
            if (fsb == null || fsb.Length < HeaderSize)
            {
                throw new UnityException("The FSB5 data is too short.");
            }
            if (fsb[0] != 'F' || fsb[1] != 'S' || fsb[2] != 'B' || fsb[3] != '5')
            {
                throw new UnityException("The data is not in FSB5 format.");
            }

            uint sampleCount = BitConverter.ToUInt32(fsb, 8);
            int sampleHeaderSize = checked((int)BitConverter.ToUInt32(fsb, 12));
            int nameSize = checked((int)BitConverter.ToUInt32(fsb, 16));
            int dataSize = checked((int)BitConverter.ToUInt32(fsb, 20));
            uint codec = BitConverter.ToUInt32(fsb, 24) & 0x1F;
            if (sampleCount != 1)
            {
                throw new UnityException(string.Format(
                    "FSB5 supports a single sample, but the data contains {0}.", sampleCount));
            }
            if (codec != CodecVorbis)
            {
                throw new UnityException(string.Format(
                    "Unsupported FSB5 codec: {0:x} (only Vorbis {1:x} is supported).", codec, CodecVorbis));
            }

            int dataOffset = HeaderSize + sampleHeaderSize + nameSize;
            if (dataOffset + dataSize > fsb.Length)
            {
                throw new UnityException("The FSB5 data area is truncated.");
            }
            if (sampleHeaderSize < 8)
            {
                throw new UnityException("The FSB5 sample header is too short.");
            }

            // u64 bit fields: bit0 hasChunks | bits1-4 freqId | bits5-6 channels
            // | bits7-33 dataOffset(×32) | bits34-63 sampleCount
            ulong meta = BitConverter.ToUInt64(fsb, HeaderSize);
            bool hasChunks = (meta & 1) != 0;
            int frequencyId = (int)((meta >> 1) & 0xF);
            int channelIndex = (int)((meta >> 5) & 3);
            uint vorbisSampleCount = (uint)((meta >> 34) & 0x3FFFFFFF);
            if (frequencyId >= Frequencies.Length)
            {
                throw new UnityException("FSB5 FrequencyId is out of range: " + frequencyId);
            }
            int frequency = (int)Frequencies[frequencyId];
            int channels = ChannelCounts[channelIndex];

            // Find the VORBISDATA setup crc32 in the chunk chain
            uint? setupCrc = null;
            if (hasChunks)
            {
                int p = HeaderSize + 8;
                while (true)
                {
                    if (p + 4 > HeaderSize + sampleHeaderSize)
                    {
                        throw new UnityException("The FSB5 chunk header is truncated.");
                    }
                    uint info = BitConverter.ToUInt32(fsb, p);
                    bool more = (info & 1) != 0;
                    int chunkSize = (int)((info >> 1) & 0xFFFFFF);
                    uint chunkType = (info >> 25) & 0x7F;
                    if (chunkType == ChunkVorbisData && p + 8 <= fsb.Length)
                    {
                        setupCrc = BitConverter.ToUInt32(fsb, p + 4);
                    }
                    p += 4 + chunkSize;
                    if (!more)
                    {
                        break;
                    }
                }
            }
            if (!setupCrc.HasValue)
            {
                throw new UnityException("The FSB5 sample header has no VORBISDATA chunk.");
            }

            SetupEntry setup;
            if (!GetSetups().TryGetValue(setupCrc.Value, out setup))
            {
                throw new UnityException(string.Format(
                    "Unknown vorbis setup crc32: {0:x8}", setupCrc.Value));
            }
            int[] blockFlags = GetBlockFlags(setup.Header, setup.SeekBit);

            // Split packets by their u16 prefix (0 / 0xFFFF marks the end)
            List<byte[]> packets = new List<byte[]>();
            int cursor = dataOffset;
            int dataEnd = dataOffset + dataSize;
            while (cursor + 2 <= dataEnd)
            {
                int length = fsb[cursor] | (fsb[cursor + 1] << 8);
                cursor += 2;
                if (length == 0 || length == 0xFFFF)
                {
                    break;
                }
                if (cursor + length > dataEnd)
                {
                    break;
                }
                byte[] packet = new byte[length];
                Buffer.BlockCopy(fsb, cursor, packet, 0, length);
                packets.Add(packet);
                cursor += length;
            }
            if (packets.Count == 0)
            {
                throw new UnityException("The FSB5 data has no vorbis packet.");
            }

            byte[] infoPacket = new byte[30];
            int ip = 0;
            infoPacket[ip++] = 1;
            infoPacket[ip++] = (byte)'v';
            infoPacket[ip++] = (byte)'o';
            infoPacket[ip++] = (byte)'r';
            infoPacket[ip++] = (byte)'b';
            infoPacket[ip++] = (byte)'i';
            infoPacket[ip++] = (byte)'s';
            WriteU32(infoPacket, ref ip, 0); // version
            infoPacket[ip++] = (byte)channels;
            WriteU32(infoPacket, ref ip, (uint)frequency);
            ip += 12; // bitrate max/nominal/min
            infoPacket[ip++] = 0xB8; // blocksize: short 256 / long 2048
            infoPacket[ip++] = 1;    // framing

            byte[] vendor = new byte[] { (byte)'m', (byte)'s', (byte)'c', (byte)'l', (byte)'a', (byte)'s', (byte)'s', (byte)'i', (byte)'c' };
            byte[] commentPacket = new byte[7 + 4 + vendor.Length + 4 + 1];
            int cp = 0;
            commentPacket[cp++] = 3;
            commentPacket[cp++] = (byte)'v';
            commentPacket[cp++] = (byte)'o';
            commentPacket[cp++] = (byte)'r';
            commentPacket[cp++] = (byte)'b';
            commentPacket[cp++] = (byte)'i';
            commentPacket[cp++] = (byte)'s';
            WriteU32(commentPacket, ref cp, (uint)vendor.Length);
            Buffer.BlockCopy(vendor, 0, commentPacket, cp, vendor.Length);
            cp += vendor.Length;
            WriteU32(commentPacket, ref cp, 0); // 0 comments
            commentPacket[cp] = 1;              // framing

            const uint serial = 0x4D534350; // 'MSCP'
            using (MemoryStream output = new MemoryStream(dataSize + setup.Header.Length + 4096))
            {
                PushPage(output, serial, 0, 0, new[] { infoPacket }, true, false);
                PushPage(output, serial, 1, 0, new[] { commentPacket, setup.Header }, false, false);

                // Audio pages: 50 packets per page; accumulate granule = (cur + prev) / 4 and clamp it to sampleCount
                uint sequence = 2;
                ulong granule = 0;
                int previousBlockSize = 0;
                int index = 0;
                bool done = false;
                while (index < packets.Count && !done)
                {
                    List<byte[]> chunk = new List<byte[]>(50);
                    while (chunk.Count < 50 && index < packets.Count && !done)
                    {
                        byte[] packet = packets[index];
                        index++;
                        int blockSize = GetPacketBlockSize(packet, blockFlags);
                        if (previousBlockSize != 0)
                        {
                            granule += (ulong)(blockSize + previousBlockSize) / 4;
                        }
                        if (granule > vorbisSampleCount)
                        {
                            granule = vorbisSampleCount;
                        }
                        previousBlockSize = blockSize;
                        chunk.Add(packet);
                        if (granule == vorbisSampleCount)
                        {
                            done = true;
                        }
                    }
                    if (chunk.Count > 0)
                    {
                        bool eos = done || index >= packets.Count;
                        PushPage(output, serial, sequence, granule, chunk, false, eos);
                        sequence++;
                    }
                }
                return output.ToArray();
            }
        }
    }
}
