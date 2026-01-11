using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace WzComparerR2.WzLib
{
    public class Wz_Header
    {
        public const string PKG1 = "PKG1";
        public const string PKG2 = "PKG2";

        public Wz_Header(string signature, string copyright, string file_name, int head_size, long data_size, long file_size, long dataStartPosition)
        {
            this.Signature = signature;
            this.Copyright = copyright;
            this.FileName = file_name;
            this.HeaderSize = head_size;
            this.DataSize = data_size;
            this.FileSize = file_size;
            this.DataStartPosition = dataStartPosition;
            this.VersionChecked = false;
        }

        public string Signature { get; private set; }
        public string Copyright { get; private set; }
        public string FileName { get; private set; }

        public int HeaderSize { get; private set; }
        public long DataSize { get; private set; }
        public long FileSize { get; private set; }
        public long DataStartPosition { get; private set; }
        public long DirEndPosition { get; set; }

        public bool VersionChecked { get; set; }
        public Wz_Capabilities Capabilities { get; internal set; }

        public int WzVersion => this.versionDetector?.WzVersion ?? 0;
        public uint HashVersion => this.versionDetector?.HashVersion ?? 0;
        public bool TryGetNextVersion() => this.versionDetector?.TryGetNextVersion() ?? false;

        public uint Pkg2Hash1 => this.versionDetector is Pkg2WzVersionDetector pkg2 ? pkg2.Hash1 : throw new NotSupportedException();

        private IWzVersionDetector versionDetector;

        public void SetWzVersion(int wzVersion)
        {
            this.versionDetector = new FixedVersion(wzVersion);
        }

        public void SetOrdinalVersionDetector(int encryptedVersion)
        {
            this.versionDetector = new OrdinalVersionDetector(encryptedVersion);
        }

        public void SetWzVersionPkg2(uint hash1, uint hash2)
        {
            this.versionDetector = new Pkg2WzVersionDetector(hash1, hash2);
        }

        public bool HasCapabilities(Wz_Capabilities cap)
        {
            return cap == (this.Capabilities & cap);
        }

        public static uint CalcHashVersion(int wzVersion)
        {
            uint sum = 0;
            string versionStr = wzVersion.ToString(System.Globalization.CultureInfo.InvariantCulture);
            for (int j = 0; j < versionStr.Length; j++)
            {
                sum <<= 5;
                sum += (uint)versionStr[j] + 1;
            }
            
            return sum;
        }

        // For pkg2 wz files, the version is a string that stored in MapleStory.exe, we can't find it without disassembling.
        public static uint CalcHashVersionPkg2(string wzVersion)
        {
            ReadOnlySpan<byte> strBytes = MemoryMarshal.Cast<char, byte>(wzVersion.AsSpan());
            uint hash = 0x811C9DC5;
            foreach (var c in strBytes)
            {
                hash = (hash ^ c) * 0x1000193;
            }
            hash = 0x85EBCA6B * (hash ^ (hash >> 13));
            return hash ^ (hash >> 16);
        }

        public interface IWzVersionDetector
        {
            bool TryGetNextVersion();
            int WzVersion { get; }
            uint HashVersion { get; }
        }

        public class FixedVersion : IWzVersionDetector
        {
            public FixedVersion(int wzVersion)
            {
                this.WzVersion = wzVersion;
                this.HashVersion = CalcHashVersion(wzVersion);
            }

            private bool hasReturned;


            public int WzVersion { get; private set; }

            public uint HashVersion { get; private set; }

            public bool TryGetNextVersion()
            {
                if (!hasReturned)
                {
                    hasReturned = true;
                    return true;
                }

                return false;
            }
        }

        public class OrdinalVersionDetector : IWzVersionDetector
        {
            public OrdinalVersionDetector(int encryptVersion)
            {
                this.EncryptedVersion = encryptVersion;
                this.versionTest = new List<int>();
                this.hasVersionTest = new List<uint>();
                this.startVersion = -1;
            }

            public int EncryptedVersion { get; private set; }

            private int startVersion;
            private List<int> versionTest;
            private List<uint> hasVersionTest;

            public int WzVersion
            {
                get
                {
                    int idx = this.versionTest.Count - 1;
                    return idx > -1 ? this.versionTest[idx] : 0;
                }
            }

            public uint HashVersion
            {
                get
                {
                    int idx = this.hasVersionTest.Count - 1;
                    return idx > -1 ? this.hasVersionTest[idx] : 0;
                }
            }

            public bool TryGetNextVersion()
            {
                for (int i = startVersion + 1; i < Int16.MaxValue; i++)
                {
                    uint sum = CalcHashVersion(i);
                    uint enc = 0xff
                        ^ ((sum >> 24) & 0xFF)
                        ^ ((sum >> 16) & 0xFF)
                        ^ ((sum >> 8) & 0xFF)
                        ^ ((sum) & 0xFF);

                    // if encver does not passed, try every version one by one
                    if (enc == this.EncryptedVersion)
                    {
                        this.versionTest.Add(i);
                        this.hasVersionTest.Add(sum);
                        startVersion = i;
                        return true;
                    }
                }

                return false;
            }
        }

        public class Pkg2WzVersionDetector : IWzVersionDetector
        {
            const uint magic = 0x1A2B3C4D;

            public Pkg2WzVersionDetector(uint hash1, uint hash2)
            {
                this.Hash1 = hash1;
                this.Hash2 = hash2;
                this.WzVersion = 0;
                this.HashVersion = 0;
            }

            public uint Hash1 { get; private set; }
            public uint Hash2 { get; private set; }
            public int WzVersion { get; private set; }
            public uint HashVersion { get; private set; }
            
            private IEnumerator<uint> resultEnumerator;

            public bool TryGetNextVersion()
            {
                if (resultEnumerator == null)
                {
                    resultEnumerator = this.GetAllVersions().GetEnumerator();
                }

                bool hasNext = resultEnumerator.MoveNext();
                if (hasNext)
                {
                    this.HashVersion = resultEnumerator.Current;
                }
                return hasNext;
            }

            private IEnumerable<uint> GetAllVersions()
            {
                yield return this.CalcHashVersionV1();
                foreach (var v in this.CalcHashVersionV2())
                {
                    yield return v;
                }
            }

            // KMST1196
            private bool VerifyHashVersionV1(uint hashVersion)
            {
                uint lt = ROL(this.Hash1, 7) ^ hashVersion;
                return (lt ^ hashVersion) == this.Hash2;
            }

            // KMST1197
            private bool VerifyHashVersionV2(uint hashVersion)
            {
                uint lt = ROL(this.Hash1 ^ (hashVersion + magic), (int)(hashVersion & 0x1F));
                return (lt ^ hashVersion) == this.Hash2;
            }

            private uint CalcHashVersionV1()
            {
                return ROL(this.Hash1, 7) ^ this.Hash2;
            }

            private List<uint> CalcHashVersionV2()
            {
                List<uint> results = new();
                Span<uint> carries = stackalloc uint[33];
                Span<uint> lhsBits = stackalloc uint[32];
                for (int sCandidate = 0; sCandidate < 32; sCandidate++)
                {
                    carries.Clear();
                    lhsBits.Clear();
                    Backtrack(0, 0, sCandidate, carries, lhsBits, results);
                }
                return results;
            }

            // disclaimer: this function is generated by google AI.
            private void Backtrack(int bitIdx, uint vHash, int s, Span<uint> carries, Span<uint> lhsBits, List<uint> results)
            {
                if (bitIdx == 32)
                {
                    // full verify
                    if ((vHash & 0x1f) == s && this.VerifyHashVersionV2(vHash))
                    {
                        results.Add(vHash);
                    }
                    return;
                }

                // initial constraints for the lower 5 bits
                uint start, end;
                if (bitIdx < 5)
                {
                    start = end = (uint)(s >> bitIdx) & 1;
                }
                else
                {
                    start = 0;
                    end = 1;
                }

                for (uint vBit = start; vBit <= end; vBit++)
                {
                    // backward Check
                    int prevLhsIdx = (bitIdx - s + 32) & 0x1f;
                    if (prevLhsIdx < bitIdx)
                    {
                        uint v_xor_h2 = vBit ^ ((this.Hash2 >> bitIdx) & 1);
                        if (v_xor_h2 != lhsBits[prevLhsIdx]) continue;
                    }

                    uint sum = vBit + ((magic >> bitIdx) & 1) + carries[bitIdx];
                    uint currentLhsBit = (sum ^ (this.Hash1 >> bitIdx)) & 1;

                    // forward Check
                    int futureVIdx = (bitIdx + s) & 0x1f;
                    if (futureVIdx <= bitIdx)
                    {
                        uint knownVBit = (uint)((vHash >> futureVIdx) & 1);
                        uint targetV_xor_H2 = knownVBit ^ ((this.Hash2 >> futureVIdx) & 1);
                        if (currentLhsBit != targetV_xor_H2) continue;
                    }
                    else if (futureVIdx < 5)
                    {
                        uint knownVBit = (uint)((s >> futureVIdx) & 1);
                        uint targetV_xor_H2 = knownVBit ^ ((this.Hash2 >> futureVIdx) & 1);
                        if (currentLhsBit != targetV_xor_H2) continue;
                    }

                    lhsBits[bitIdx] = currentLhsBit;
                    carries[bitIdx + 1] = sum >> 1;
                    Backtrack(bitIdx + 1, vHash | (vBit << bitIdx), s, carries, lhsBits, results);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static uint ROL(uint v, int n) => (v << n) | (v >> (32 - n));
        }
    }
}
