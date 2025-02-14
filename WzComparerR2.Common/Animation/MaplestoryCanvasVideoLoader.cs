using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
#if NET6_0_OR_GREATER
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
#endif
using Microsoft.Xna.Framework.Graphics;
using WzComparerR2.WzLib;
using WzComparerR2.WzLib.Utilities;

namespace WzComparerR2.Animation
{
    public class MaplestoryCanvasVideoLoader
    {
        public FrameAnimationData Load(Wz_Video wzVideo, GraphicsDevice graphicsDevice)
        {
            var wzFile = wzVideo.WzFile;
            var stream = wzVideo.WzImage.OpenRead();
            var mcvHeader = wzVideo.ReadVideoFileHeader();

            Span<byte> fourCC = stackalloc byte[4];
            MemoryMarshal.Cast<byte, uint>(fourCC)[0] = mcvHeader.FourCC;

            bool separateAlphaChannel = (mcvHeader.DataFlag & McvDataFlags.AlphaMap) != 0;
            using VpxVideoDecoder dataDecoder = new VpxVideoDecoder(fourCC, mcvHeader.Width, mcvHeader.Height, 0);
            using VpxVideoDecoder alphaMapDecoder = separateAlphaChannel ? new VpxVideoDecoder(fourCC, mcvHeader.Width, mcvHeader.Height, 0) : null;

            var frames = new List<Frame>(mcvHeader.FrameCount);
            byte[] textureBuffer = new byte[mcvHeader.Width * mcvHeader.Height * 4];
            byte[] alphaMapBuffer = separateAlphaChannel ? new byte[mcvHeader.Width * mcvHeader.Height * 4] : null;

            // shared function to decode frame
            void readAndDecodeFrame(VpxVideoDecoder decoder, long videoDataOffset, int count, byte[] outputBuffer)
            {
                byte[] dataBuffer = ArrayPool<byte>.Shared.Rent(count);
                try
                {
                    lock (wzFile.ReadLock)
                    {
                        stream.Position = wzVideo.Offset + videoDataOffset;
                        stream.ReadExactly(dataBuffer, 0, count);
                    }
                    // read and decode frame data
                    decoder.DecodeData(dataBuffer.AsSpan(0, count));
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(dataBuffer);
                }

                // get raw frames and convert format to bgra.
                // expected that we should only get single frame.
                if (decoder.GetNextFrame(out VpxFrame vpxFrame))
                {
                    if (vpxFrame.DisplayWidth != mcvHeader.Width || vpxFrame.DisplayHeight != mcvHeader.Height)
                    {
                        throw new Exception(string.Format("Decoded frame size ({0}*{1}) does not match the video size({2}*{3}).",
                            vpxFrame.DisplayWidth, vpxFrame.DisplayHeight, mcvHeader.Width, mcvHeader.Height));
                    }

                    // TODO: in future we can support rendering yuv420 in pixel shader instead of soft decoding.
                    switch (vpxFrame.Format)
                    {
                        case VpxVideoDecoder.Interop.vpx_img_fmt.VPX_IMG_FMT_I420:
                            I420ToARGB(vpxFrame.PlanesY, vpxFrame.StrideY, vpxFrame.PlanesU, vpxFrame.StrideU, vpxFrame.PlanesV, vpxFrame.StrideV,
                                outputBuffer, mcvHeader.Width * 4, mcvHeader.Width, mcvHeader.Height);
                            break;
                        default:
                            throw new Exception($"Unsupported frame format: {vpxFrame.Format}");
                    }

                    if (decoder.GetNextFrame(out vpxFrame))
                    {
                        throw new Exception($"Unexpectedly read more than one frame.");
                    }
                }
                else
                {
                    throw new Exception($"Failed to get vpx frame.");
                }
            }

            foreach (var fi in mcvHeader.Frames)
            {
                readAndDecodeFrame(dataDecoder, fi.DataOffset, fi.DataCount, textureBuffer);
                if (separateAlphaChannel && fi.AlphaDataOffset > -1 && fi.AlphaDataCount > 0)
                {
                    readAndDecodeFrame(alphaMapDecoder, fi.AlphaDataOffset, fi.AlphaDataCount, alphaMapBuffer);
                    MergeAlphaMap(textureBuffer, alphaMapBuffer);
                }
                var texture = new Texture2D(graphicsDevice, mcvHeader.Width, mcvHeader.Height, false, SurfaceFormat.Bgra32);
                texture.SetData(textureBuffer);
                var frame = new Frame();
                frame.Texture = texture;
                frame.Delay = (int)fi.Delay.TotalMilliseconds;
                frames.Add(frame);
            }

            return new FrameAnimationData(frames);
        }

        private static unsafe void I420ToARGB(IntPtr src_y, int src_stride_y, IntPtr src_u, int src_stride_u, IntPtr src_v, int src_stride_v,
                                                Span<byte> dst_bgra, int dst_stride_bgra, int width, int height)
        {
            fixed (byte* pDest = dst_bgra)
            {
                I420ToARGB((byte*)src_y, src_stride_y, (byte*)src_u, src_stride_u, (byte*)src_v, src_stride_v, pDest, dst_stride_bgra, width, height);
            }
        }

        private static unsafe void MergeAlphaMap(Span<byte> textureData, ReadOnlySpan<byte> alphaMapData)
        {
            if ((textureData.Length & 3) != 0)
            {
                throw new ArgumentException("The length of TextureData must be a multiple of 4.", nameof(textureData));
            }
            if ((alphaMapData.Length & 3) != 0)
            {
                throw new ArgumentException("The length of AlphaMapData must be a multiple of 4.", nameof(alphaMapData));
            }
            if (textureData.Length != alphaMapData.Length)
            {
                throw new ArgumentException("The length of TextureData should be equal to the length of AlphaMapData.");
            }

#if NET6_0_OR_GREATER
            if (textureData.Length >= 32 && Avx2.IsSupported)
            {
                Vector256<byte> blendMask = Vector256.Create(0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255,
                                                            0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255);
                Vector256<byte> ymm0, ymm1;
                while (textureData.Length >= 32)
                {
                    fixed (byte* pData = textureData)
                        ymm0 = Avx.LoadVector256(pData);
                    fixed (byte* pData = alphaMapData)
                        ymm1 = Avx.LoadVector256(pData);

                    //    b g r a  b g r a  b g r a  b g r a
                    // => 0 b g r  a b g r  a b g r  a b g r
                    ymm1 = Avx2.ShiftLeftLogical128BitLane(ymm1, 1);
                    //    b g r _  b g r _  b g r _  b g r _
                    //    _ _ _ r  _ _ _ r  _ _ _ r  _ _ _ r 
                    ymm0 = Avx2.BlendVariable(ymm0, ymm1, blendMask);
                    fixed (byte* pData = textureData)
                        Avx.Store(pData, ymm0);

                    textureData = textureData.Slice(32);
                    alphaMapData = alphaMapData.Slice(32);
                }
            }

            if (textureData.Length >= 16 && Sse41.IsSupported)
            {
                Vector128<byte> blendMask = Vector128.Create(0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255, 0, 0, 0, 255);
                Vector128<byte> xmm0, xmm1;
                while (textureData.Length >= 16)
                {
                    fixed (byte* pData = textureData)
                        xmm0 = Sse2.LoadVector128(pData);
                    fixed (byte* pData = alphaMapData)
                        xmm1 = Sse2.LoadVector128(pData);
                    xmm1 = Sse2.ShiftLeftLogical128BitLane(xmm0, 1);
                    xmm0 = Sse41.BlendVariable(xmm0, xmm1, blendMask);
                    fixed (byte* pData = textureData)
                        Sse2.Store(pData, xmm0);

                    textureData = textureData.Slice(16);
                    alphaMapData = alphaMapData.Slice(16);
                }
            }
#endif

            for (int i = 0; i < textureData.Length; i += 4)
            {
                textureData[i + 3] = alphaMapData[i + 2];
            }
        }

        #region Interop
        private const string libYuv = @"libyuv";

        [DllImport(libYuv)]
        private static unsafe extern int I420ToARGB([In] byte* src_y, int src_stride_y,
                                                    [In] byte* src_u, int src_stride_u,
                                                    [In] byte* src_v, int src_stride_v,
                                                    [Out] byte* dst_bgra, int dst_stride_bgra,
                                                    int width, int height);
        #endregion
    }
}
