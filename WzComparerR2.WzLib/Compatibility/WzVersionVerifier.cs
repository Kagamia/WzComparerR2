using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

#if NET6_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif

using static WzComparerR2.WzLib.Utilities.MathHelper;

namespace WzComparerR2.WzLib.Compatibility
{
    #region Version iterators
    public interface IWzVersionIterator
    {
        bool TryGetNextVersion();
        void Reset();
        int WzVersion { get; }
        uint HashVersion { get; }
        bool IsMatch(int wzVersion, uint hashVersion);
    }

    /// <summary>
    /// PKG1 version iterator. Supports ordinal iteration (matching encrypted version byte)
    /// and fixed mode (for encverMissing, wzVersion = 777).
    /// </summary>
    public class Pkg1VersionIterator : IWzVersionIterator
    {
        private readonly int encryptedVersion;
        private readonly int fixedWzVersion;
        private readonly uint fixedHashVersion;

        private int startVersion;
        private bool hasReturned;

        /// <summary>
        /// Ordinal mode: iterates wzVersions whose encrypted version byte matches.
        /// </summary>
        public Pkg1VersionIterator(int encryptedVersion)
        {
            this.encryptedVersion = encryptedVersion;
            this.fixedWzVersion = -1;
            this.startVersion = -1;
        }

        private Pkg1VersionIterator(int fixedWzVersion, uint fixedHashVersion)
        {
            this.encryptedVersion = -1;
            this.fixedWzVersion = fixedWzVersion;
            this.fixedHashVersion = fixedHashVersion;
            this.startVersion = -1;
        }

        /// <summary>
        /// Fixed mode: yields a single known wzVersion (e.g. 777 for encverMissing).
        /// </summary>
        public static Pkg1VersionIterator CreateFixed(int wzVersion)
        {
            return new Pkg1VersionIterator(wzVersion, Wz_Header.CalcHashVersion(wzVersion));
        }

        private bool IsFixed => this.fixedWzVersion >= 0;

        public int WzVersion { get; private set; }
        public uint HashVersion { get; private set; }

        private static uint CalcEncryptedVersion(uint hashVersion)
        {
            return 0xff
                ^ ((hashVersion >> 24) & 0xFF)
                ^ ((hashVersion >> 16) & 0xFF)
                ^ ((hashVersion >> 8) & 0xFF)
                ^ ((hashVersion) & 0xFF);
        }

        public bool TryGetNextVersion()
        {
            if (IsFixed)
            {
                if (!hasReturned)
                {
                    hasReturned = true;
                    WzVersion = fixedWzVersion;
                    HashVersion = fixedHashVersion;
                    return true;
                }
                return false;
            }

            for (int i = startVersion + 1; i < short.MaxValue; i++)
            {
                uint sum = Wz_Header.CalcHashVersion(i);
                if (CalcEncryptedVersion(sum) == (uint)this.encryptedVersion)
                {
                    WzVersion = i;
                    HashVersion = sum;
                    startVersion = i;
                    return true;
                }
            }

            return false;
        }

        public bool IsMatch(int wzVersion, uint hashVersion)
        {
            if (IsFixed)
                return wzVersion == fixedWzVersion && hashVersion == fixedHashVersion;

            if (Wz_Header.CalcHashVersion(wzVersion) != hashVersion)
                return false;
            return CalcEncryptedVersion(hashVersion) == (uint)this.encryptedVersion;
        }

        public void Reset()
        {
            startVersion = -1;
            WzVersion = 0;
            HashVersion = 0;
            hasReturned = false;
        }
    }

    /// <summary>
    /// PKG2 version iterator. Candidates are lazily computed on first iteration.
    /// IsMatch uses a lightweight O(1) verify function, avoiding expensive candidate enumeration.
    /// </summary>
    public class Pkg2VersionIterator : IWzVersionIterator
    {
        public Pkg2VersionIterator(int wzVersion, Func<IReadOnlyList<uint>> candidatesFactory, Func<uint, bool> verifier)
        {
            this.wzVersion = wzVersion;
            this.candidatesFactory = candidatesFactory;
            this.verifier = verifier;
            this.index = -1;
        }

        private readonly int wzVersion;
        private readonly Func<IReadOnlyList<uint>> candidatesFactory;
        private readonly Func<uint, bool> verifier;
        private IReadOnlyList<uint> candidates;
        private int index;

        public int WzVersion => wzVersion;
        public uint HashVersion { get; private set; }

        public bool TryGetNextVersion()
        {
            this.candidates ??= this.candidatesFactory();
            if (++this.index < this.candidates.Count)
            {
                this.HashVersion = this.candidates[this.index];
                return true;
            }
            return false;
        }

        public bool IsMatch(int wzVersion, uint hashVersion)
        {
            if (wzVersion != this.wzVersion)
                return false;
            return this.verifier(hashVersion);
        }

        public void Reset()
        {
            this.index = -1;
            this.HashVersion = 0;
        }
    }

    /// <summary>
    /// Computes hash version candidates and verifies cached versions for PKG2 files.
    /// </summary>
    public interface IPkg2HashVersionCalc
    {
        IReadOnlyList<uint> CalcCandidates(uint hash1, uint hash2);
        bool Verify(uint hash1, uint hash2, uint hashVersion);
    }

    /// <summary>
    /// PKG2 hash version calculation for KMST 1196 (V1).
    /// </summary>
    public sealed class Pkg2HashVersionCalcV1 : IPkg2HashVersionCalc
    {
        public IReadOnlyList<uint> CalcCandidates(uint hash1, uint hash2)
        {
            return new[] { ROL(hash1, 7) ^ hash2 };
        }

        public bool Verify(uint hash1, uint hash2, uint hashVersion)
        {
            return hashVersion == (ROL(hash1, 7) ^ hash2);
        }
    }

    /// <summary>
    /// PKG2 hash version calculation for KMST 1197 (V2). Uses backtrack solver.
    /// </summary>
    public sealed class Pkg2HashVersionCalcV2 : IPkg2HashVersionCalc
    {
        public IReadOnlyList<uint> CalcCandidates(uint hash1, uint hash2)
        {
            List<uint> results = new List<uint>();
            Pkg2BacktrackSolver.BacktrackParameters p = new Pkg2BacktrackSolver.BacktrackParameters
            {
                Hash1 = hash1,
                LowBitLen = 5,
                Target = hash2,
                Carries = stackalloc uint[33],
                LhsBits = stackalloc uint[32],
                Validator = (v) => Verify(hash1, hash2, v),
                Results = results,
            };

            for (uint sCandidate = 0; sCandidate < 32; sCandidate++)
            {
                p.Carries.Clear();
                p.LhsBits.Clear();
                p.S = (int)sCandidate;
                p.LowBits = sCandidate;
                Pkg2BacktrackSolver.Solve(p, 0, 0);
            }
            return results;
        }

        public bool Verify(uint hash1, uint hash2, uint hashVersion)
        {
            uint lt = ROL(hash1 ^ (hashVersion + Pkg2BacktrackSolver.Magic), (int)(hashVersion & 0x1F));
            return (lt ^ hashVersion) == hash2;
        }
    }

    /// <summary>
    /// PKG2 hash version calculation for KMST 1198 (V3). Uses backtrack solver.
    /// </summary>
    public sealed class Pkg2HashVersionCalcV3 : IPkg2HashVersionCalc
    {
        public IReadOnlyList<uint> CalcCandidates(uint hash1, uint hash2)
        {
            List<uint> results = new List<uint>();
            Pkg2BacktrackSolver.BacktrackParameters p = new Pkg2BacktrackSolver.BacktrackParameters
            {
                Hash1 = hash1,
                LowBitLen = 4,
                Target = (~hash2) ^ hash1,
                Carries = stackalloc uint[33],
                LhsBits = stackalloc uint[32],
                Validator = (v) => Verify(hash1, hash2, v),
                Results = results,
            };

            for (uint sCandidate = 0; sCandidate < 16; sCandidate++)
            {
                p.Carries.Clear();
                p.LhsBits.Clear();
                p.S = (int)(sCandidate + (hash1 & 0xF));
                p.LowBits = sCandidate;
                Pkg2BacktrackSolver.Solve(p, 0, 0);
            }
            return results;
        }

        public bool Verify(uint hash1, uint hash2, uint hashVersion)
        {
            uint lt = ROL(hash1 ^ (hashVersion + Pkg2BacktrackSolver.Magic), (int)((hashVersion & 0xF) + (hash1 & 0xF)));
            return ~(lt ^ hashVersion ^ hash1) == hash2;
        }
    }

    /// <summary>
    /// PKG2 hash version calculation for KMST 1199 (V4). Uses parallel brute-force with SIMD.
    /// </summary>
    public sealed class Pkg2HashVersionCalcV4 : IPkg2HashVersionCalc
    {
        private const uint magic = Pkg2BacktrackSolver.Magic;

        public IReadOnlyList<uint> CalcCandidates(uint hash1, uint hash2)
        {
            uint hash1Low4 = hash1 & 0xF;
            uint target = ~hash2;

            List<uint> results = new List<uint>();
            object lockObj = new object();

            Parallel.For(0, 256, (int chunk) =>
            {
                uint start = (uint)chunk << 24;
                uint count = 1u << 24;

#if NET6_0_OR_GREATER
                if (Avx2.IsSupported)
                {
                    Vector256<uint> hash1Vec = Vector256.Create(hash1);
                    Vector256<uint> hash1Low4Vec = Vector256.Create(hash1Low4);
                    Vector256<uint> targetVec = Vector256.Create(target);

                    var hashVersion = Avx2.Add(Vector256.Create((uint)chunk << 24), Vector256.Create(0u, 1u, 2u, 3u, 4u, 5u, 6u, 7u));

                    for (uint i = 0; i < count; i += 8)
                    {
                        // preHash = hash1 ^ hashVersion
                        Vector256<uint> preHash = Avx2.Xor(hash1Vec, hashVersion);
                        // mixedHash = Mix(preHash ^ 0x6D4C3B2A) ^ 0x91E10DA5
                        Vector256<uint> mixedHash = Avx2.Xor(preHash, Vector256.Create(0x6D4C3B2Au));
                        mixedHash = Avx2.Xor(mixedHash, Avx2.ShiftRightLogical(mixedHash, 16));
                        mixedHash = Avx2.MultiplyLow(mixedHash, Vector256.Create(0x7FEB352Du));
                        mixedHash = Avx2.Xor(mixedHash, Avx2.ShiftRightLogical(mixedHash, 15));
                        mixedHash = Avx2.MultiplyLow(mixedHash, Vector256.Create(0x846CA68Bu));
                        mixedHash = Avx2.Xor(mixedHash, Avx2.ShiftRightLogical(mixedHash, 16));
                        mixedHash = Avx2.Xor(mixedHash, Vector256.Create(0x91E10DA5u));

                        // lt = hash1 ^ ((ushort)mixedHash + hashVersion + magic)
                        Vector256<uint> lt = Avx2.And(Vector256.Create(0xFFFFu), mixedHash);
                        lt = Avx2.Add(Avx2.Add(lt, hashVersion), Vector256.Create(magic));
                        lt = Avx2.Xor(hash1Vec, lt);
                        // rt = ((mixedHash ^ hashVersion) & 0xF) + hash1Low4
                        Vector256<uint> rt = Avx2.Xor(mixedHash, hashVersion);
                        rt = Avx2.And(rt, Vector256.Create(0xFu));
                        rt = Avx2.Add(rt, hash1Low4Vec);
                        // lt = ROL(lt, rt)
                        lt = Avx2.Or(Avx2.ShiftLeftLogicalVariable(lt, rt),
                                Avx2.ShiftRightLogicalVariable(lt, Avx2.Subtract(Vector256.Create(32u), rt)));

                        // (lt ^ (preHash + mixedHash)) == target
                        Vector256<uint> actual = Avx2.Xor(lt, Avx2.Add(preHash, mixedHash));
                        Vector256<uint> cmp = Avx2.CompareEqual(actual, targetVec);
                        int mask = Avx2.MoveMask(cmp.AsByte());

                        if (mask != 0)
                        {
                            for (int lane = 0; lane < 8; lane++)
                            {
                                if ((mask & (0xF << (lane * 4))) != 0)
                                {
                                    lock (lockObj)
                                    {
                                        results.Add(hashVersion.GetElement(lane));
                                    }
                                }
                            }
                        }

                        hashVersion = Avx2.Add(hashVersion, Vector256.Create(8u));
                    }
                }
                else if (Sse41.IsSupported)
                {
                    Vector128<uint> hash1Vec = Vector128.Create(hash1);
                    Vector128<uint> hash1Low4Vec = Vector128.Create(hash1Low4);
                    Vector128<uint> targetVec = Vector128.Create(target);

                    var hashVersion = Sse2.Add(Vector128.Create(start), Vector128.Create(0u, 1u, 2u, 3u));

                    for (uint i = 0; i < count; i += 4)
                    {
                        // preHash = hash1 ^ hashVersion
                        Vector128<uint> preHash = Sse2.Xor(hash1Vec, hashVersion);
                        // mixedHash = Mix(preHash ^ 0x6D4C3B2A) ^ 0x91E10DA5
                        Vector128<uint> mixedHash = Sse2.Xor(preHash, Vector128.Create(0x6D4C3B2Au));
                        mixedHash = Sse2.Xor(mixedHash, Sse2.ShiftRightLogical(mixedHash, 16));
                        mixedHash = Sse41.MultiplyLow(mixedHash, Vector128.Create(0x7FEB352Du));
                        mixedHash = Sse2.Xor(mixedHash, Sse2.ShiftRightLogical(mixedHash, 15));
                        mixedHash = Sse41.MultiplyLow(mixedHash, Vector128.Create(0x846CA68Bu));
                        mixedHash = Sse2.Xor(mixedHash, Sse2.ShiftRightLogical(mixedHash, 16));
                        mixedHash = Sse2.Xor(mixedHash, Vector128.Create(0x91E10DA5u));

                        // lt = hash1 ^ ((ushort)mixedHash + hashVersion + magic)
                        Vector128<uint> lt = Sse2.And(Vector128.Create(0xFFFFu), mixedHash);
                        lt = Sse2.Add(Sse2.Add(lt, hashVersion), Vector128.Create(magic));
                        lt = Sse2.Xor(hash1Vec, lt);
                        // rt = ((mixedHash ^ hashVersion) & 0xF) + hash1Low4
                        Vector128<uint> rt = Sse2.Xor(mixedHash, hashVersion);
                        rt = Sse2.And(rt, Vector128.Create(0xFu));
                        rt = Sse2.Add(rt, hash1Low4Vec);

                        // lt = ROL(lt, rt) — no vpsllvd/vpsrlvd in SSE4.1, variable ROL per-lane
                        lt = Vector128.Create(
                            ROL(lt.GetElement(0), (int)rt.GetElement(0)),
                            ROL(lt.GetElement(1), (int)rt.GetElement(1)),
                            ROL(lt.GetElement(2), (int)rt.GetElement(2)),
                            ROL(lt.GetElement(3), (int)rt.GetElement(3)));

                        // (lt ^ (preHash + mixedHash)) == target
                        Vector128<uint> actual = Sse2.Xor(lt, Sse2.Add(preHash, mixedHash));
                        Vector128<uint> cmp = Sse2.CompareEqual(actual, targetVec);
                        int mask = Sse2.MoveMask(cmp.AsByte());

                        if (mask != 0)
                        {
                            for (int lane = 0; lane < 4; lane++)
                            {
                                if ((mask & (0xF << (lane * 4))) != 0)
                                {
                                    lock (lockObj)
                                    {
                                        results.Add(hashVersion.GetElement(lane));
                                    }
                                }
                            }
                        }

                        hashVersion = Sse2.Add(hashVersion, Vector128.Create(4u));
                    }
                }
                else
                {
#endif
                    for (uint i = 0; i < count; i++)
                    {
                        uint hashVersion = start + i;
                        uint preHash = hash1 ^ hashVersion;
                        uint mixedHash = Mix(preHash ^ 0x6D4C3B2A) ^ 0x91E10DA5;
                        uint lt = ROL(hash1 ^ ((ushort)mixedHash + hashVersion + magic), (int)(((mixedHash ^ hashVersion) & 0xF) + hash1Low4));
                        if ((lt ^ (preHash + mixedHash)) == target)
                        {
                            lock (lockObj)
                            {
                                results.Add(hashVersion);
                            }
                        }
                    }
#if NET6_0_OR_GREATER
                }
#endif
            });

            return results;
        }

        public bool Verify(uint hash1, uint hash2, uint hashVersion)
        {
            uint preHash = hash1 ^ hashVersion;
            uint mixedHash = Mix(preHash ^ 0x6D4C3B2A) ^ 0x91E10DA5;
            uint lt = ROL(hash1 ^ ((ushort)mixedHash + hashVersion + magic), (int)(((mixedHash ^ hashVersion) & 0xF) + (hash1 & 0xF)));
            return (lt ^ (preHash + mixedHash)) == ~hash2;
        }
    }

    /// <summary>
    /// Shared backtrack solver used by KMST 1197-1198 hash version calculations.
    /// </summary>
    // This function is originally generated by google AI.
    // Refactor by Kagamia.
    internal static class Pkg2BacktrackSolver
    {
        public const uint Magic = 0x1A2B3C4D;

        public static void Solve(in BacktrackParameters p, int bitIdx, uint vHash)
        {
            if (bitIdx == 32)
            {
                // full verify
                if (p.Validator(vHash))
                {
                    p.Results.Add(vHash);
                }
                return;
            }

            // initial constraints for the lower bits
            uint start, end;
            if (bitIdx < p.LowBitLen)
            {
                start = end = (p.LowBits >> bitIdx) & 1;
            }
            else
            {
                start = 0;
                end = 1;
            }

            for (uint vBit = start; vBit <= end; vBit++)
            {
                // backward check
                int prevLhsIdx = (bitIdx - p.S + 32) & 0x1f;
                if (prevLhsIdx < bitIdx)
                {
                    uint v_xor_h2 = vBit ^ ((p.Target >> bitIdx) & 1);
                    if (v_xor_h2 != p.LhsBits[prevLhsIdx]) continue;
                }

                uint sum = vBit + ((Magic >> bitIdx) & 1) + p.Carries[bitIdx];
                uint currentLhsBit = (sum ^ (p.Hash1 >> bitIdx)) & 1;

                // forward check
                int futureVIdx = (bitIdx + p.S) & 0x1f;
                if (futureVIdx <= bitIdx)
                {
                    uint knownVBit = (uint)((vHash >> futureVIdx) & 1);
                    uint targetV_xor_H2 = knownVBit ^ ((p.Target >> futureVIdx) & 1);
                    if (currentLhsBit != targetV_xor_H2) continue;
                }
                else if (futureVIdx < p.LowBitLen)
                {
                    uint knownVBit = (uint)((p.LowBits >> futureVIdx) & 1);
                    uint targetV_xor_H2 = knownVBit ^ ((p.Target >> futureVIdx) & 1);
                    if (currentLhsBit != targetV_xor_H2) continue;
                }

                p.LhsBits[bitIdx] = currentLhsBit;
                p.Carries[bitIdx + 1] = sum >> 1;
                Solve(p, bitIdx + 1, vHash | (vBit << bitIdx));
            }
        }

        public ref struct BacktrackParameters
        {
            public uint Hash1;
            public int S;
            public int LowBitLen;
            public uint LowBits;
            public uint Target;
            public Span<uint> Carries;
            public Span<uint> LhsBits;
            public Func<uint, bool> Validator;
            public List<uint> Results;
        }
    }

    #endregion

    #region Shared detection helpers

    public static class WzVersionDetectHelper
    {
        public static bool FastDetectWithPreReadNodes(Wz_File wzFile, WzPreReadResult preReadResult,
            IWzVersionIterator iterator, OffsetCalcFactory createOffsetCalc)
        {
            var nodes = preReadResult.Nodes;
            if (nodes.Count == 0)
                return false;

            var imageSizes = new SizeRange[nodes.Count];
            long fileLen = wzFile.FileStream.Length;

            while (iterator.TryGetNextVersion())
            {
                var calc = createOffsetCalc(wzFile, iterator.HashVersion);

                if (!ValidateEntryCountsIfPkg2(preReadResult, calc))
                    continue;

                if (ValidateOffsets(nodes, imageSizes, fileLen, preReadResult.DirStartPosition, preReadResult.DirEndPosition, calc))
                {
                    wzFile.Header.WzVersion = iterator.WzVersion;
                    wzFile.Header.HashVersion = iterator.HashVersion;
                    wzFile.Header.VersionChecked = true;
                    wzFile.OffsetCalc = calc;
                    return true;
                }
            }

            return false;
        }

        public static bool FastDetectSingleVersion(Wz_File wzFile, WzPreReadResult preReadResult,
            int wzVersion, uint hashVersion, OffsetCalcFactory createOffsetCalc)
        {
            var nodes = preReadResult.Nodes;
            if (nodes.Count == 0)
                return false;

            var imageSizes = new SizeRange[nodes.Count];
            long fileLen = wzFile.FileStream.Length;

            var calc = createOffsetCalc(wzFile, hashVersion);

            if (ValidateEntryCountsIfPkg2(preReadResult, calc)
                && ValidateOffsets(nodes, imageSizes, fileLen, preReadResult.DirStartPosition, preReadResult.DirEndPosition, calc))
            {
                wzFile.Header.WzVersion = wzVersion;
                wzFile.Header.HashVersion = hashVersion;
                wzFile.Header.VersionChecked = true;
                wzFile.OffsetCalc = calc;
                return true;
            }

            return false;
        }

        private static bool ValidateEntryCountsIfPkg2(WzPreReadResult preReadResult, IWzImageOffsetCalc calc)
        {
            if (preReadResult.Pkg2DirEntryCounts == null || calc is not IPkg2ImageOffsetCalc pkg2Calc)
                return true;

            foreach (var ec in preReadResult.Pkg2DirEntryCounts)
            {
                if (pkg2Calc.DecryptEntryCount(ec.EncryptedEntryCount) != ec.ActualEntryCount)
                    return false;
            }
            return true;
        }

        private static bool ValidateOffsets(List<WzPreReadNodeInfo> nodes, SizeRange[] imageSizes,
            long fileLen, long dirStartPosition, long dirEndPosition, IWzImageOffsetCalc calc)
        {
            int imgCount = 0;

            foreach (var node in nodes)
            {
                uint offs = calc.CalcOffset(node.HashedOffsetPosition, node.HashedOffset);

                if (node.NodeType == 0x04 || node.NodeType == 0x02)
                {
                    if (offs < dirEndPosition || offs + node.DataLength > fileLen)
                        return false;
                    imageSizes[imgCount++] = new SizeRange { Start = offs, End = offs + node.DataLength };
                }
                else if (node.NodeType == 0x03)
                {
                    if (offs < dirStartPosition || offs + 1 > dirEndPosition)
                        return false;
                    imageSizes[imgCount++] = new SizeRange { Start = offs, End = offs + 1 };
                }
            }

            if (imgCount > 1)
            {
                Array.Sort(imageSizes, 0, imgCount);
                for (int i = 1; i < imgCount; i++)
                {
                    if (imageSizes[i - 1].End > imageSizes[i].Start)
                        return false;
                }
            }

            return true;
        }

        private struct SizeRange : IComparable<SizeRange>
        {
            public long Start;
            public long End;

            public int CompareTo(SizeRange sr)
            {
                int result = this.Start.CompareTo(sr.Start);
                if (result == 0)
                    result = this.End.CompareTo(sr.End);
                return result;
            }
        }
    }

    #endregion
}
