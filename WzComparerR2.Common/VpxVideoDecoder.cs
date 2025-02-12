using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using vpx_codec_ctx_t = WzComparerR2.VpxVideoDecoder.Interop.vpx_codec_ctx;
using vpx_codec_dec_cfg_t = WzComparerR2.VpxVideoDecoder.Interop.vpx_codec_dec_cfg;
using vpx_color_range_t = WzComparerR2.VpxVideoDecoder.Interop.vpx_color_range;
using vpx_codec_err_t = WzComparerR2.VpxVideoDecoder.Interop.vpx_codec_err;
using vpx_codec_flags_t = WzComparerR2.VpxVideoDecoder.Interop.vpx_codec_flags;
using vpx_image_t = WzComparerR2.VpxVideoDecoder.Interop.vpx_image;
using vpx_img_fmt_t = WzComparerR2.VpxVideoDecoder.Interop.vpx_img_fmt;
using vpx_codec_iface_ptr = nint;
using vpx_codec_iter_t = nint;
using System.Runtime.CompilerServices;
using System.Windows.Forms.VisualStyles;

namespace WzComparerR2
{
    public class VpxVideoDecoder : IDisposable
    {
        public static ReadOnlySpan<byte> FourCC_VP8 => "VP80"u8;
        public static ReadOnlySpan<byte> FourCC_VP9 => "VP90"u8;

        public VpxVideoDecoder(ReadOnlySpan<byte> fourCC)
        {
            this.VpxCodecInit(fourCC, new vpx_codec_dec_cfg_t());
        }

        public VpxVideoDecoder(ReadOnlySpan<byte> fourCC, int width, int height, int threads)
        {
            this.VpxCodecInit(fourCC, new vpx_codec_dec_cfg_t
            {
                w = (uint)width,
                h = (uint)height,
                threads = (uint)threads,
            });
        }

        private IntPtr pVpxCodecCtx;
        private IntPtr pVpxCodecDecConfig;
        private vpx_codec_iter_t iter;

        private unsafe void VpxCodecInit(ReadOnlySpan<byte> fourCC, vpx_codec_dec_cfg_t config)
        {
            vpx_codec_iface_ptr iface;
            if (fourCC.Length != 4)
            {
                throw new ArgumentException("Invalid length.", nameof(fourCC));
            }

            if (fourCC.SequenceEqual(FourCC_VP9))
            {
                iface = Interop.vpx_codec_vp9_dx();
            }
            else if (fourCC.SequenceEqual(FourCC_VP8))
            {
                iface = Interop.vpx_codec_vp8_dx();
            }
            else
            {
                throw new ArgumentException($"Unknown fourCC value: {MemoryMarshal.Read<int>(fourCC)}", nameof(fourCC));
            }

            IntPtr pVpxCodecCtx = Marshal.AllocHGlobal(Marshal.SizeOf<vpx_codec_ctx_t>());
            IntPtr pVpxCodecDecConfig = Marshal.AllocHGlobal(Marshal.SizeOf<vpx_codec_dec_cfg_t>());
           
            try
            {
                Unsafe.Copy(pVpxCodecDecConfig.ToPointer(), ref config);
                vpx_codec_err_t err = Interop.vpx_codec_dec_init_ver((vpx_codec_ctx_t*)pVpxCodecCtx, iface, (vpx_codec_dec_cfg_t*)pVpxCodecDecConfig, 0, Interop.VPX_DECODER_ABI_VERSION);
                ThrowOnNonSuccessfulError(err, nameof(Interop.vpx_codec_dec_init_ver));
            }
            catch
            {
                Marshal.FreeHGlobal(pVpxCodecCtx);
                Marshal.FreeHGlobal(pVpxCodecDecConfig);
            }

            this.pVpxCodecCtx = pVpxCodecCtx;
            this.pVpxCodecDecConfig = pVpxCodecDecConfig;
        }

        public unsafe void DecodeData(ReadOnlySpan<byte> data)
        {
            this.ThrowOnObjectDisposed();
            if (data.IsEmpty)
            {
                return;
            }
            fixed (byte* pData = data)
            {
                this.iter = 0;
                var err = Interop.vpx_codec_decode((vpx_codec_ctx_t*)this.pVpxCodecCtx, pData, (uint)data.Length, 0, 0);
                ThrowOnNonSuccessfulError(err, nameof(Interop.vpx_codec_decode));
            }
        }

        public unsafe bool GetNextFrame(out VpxFrame frame)
        {
            this.ThrowOnObjectDisposed();

            vpx_codec_iter_t iter = this.iter;
            vpx_image_t* img = Interop.vpx_codec_get_frame((vpx_codec_ctx_t*)this.pVpxCodecCtx, &iter);
            this.iter = iter;

            if (img != null)
            {
                frame = new VpxFrame(img);
                return true;
            }
            else
            {
                frame = default;
                return false;
            }
        }

        #region IDisposable
        private bool isDisposed;

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~VpxVideoDecoder()
        {
            this.Dispose(false);
        }

        protected virtual unsafe void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }

            if (disposing)
            {
                if (this.pVpxCodecCtx != IntPtr.Zero)
                {
                    Interop.vpx_codec_destroy((vpx_codec_ctx_t*)this.pVpxCodecCtx);
                    Marshal.FreeHGlobal(this.pVpxCodecCtx);
                    this.pVpxCodecCtx = IntPtr.Zero;
                }
                if (this.pVpxCodecDecConfig != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(this.pVpxCodecDecConfig);
                    this.pVpxCodecDecConfig = IntPtr.Zero;
                }
            }
            isDisposed = true;
        }

        private void ThrowOnObjectDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(nameof(VpxVideoDecoder));
            }
        }

        private void ThrowOnNonSuccessfulError(vpx_codec_err_t err, string methodName = null)
        {
            if (err != vpx_codec_err_t.VPX_CODEC_OK)
            {
                throw new Exception($"{methodName ?? "libVpx"} returns error: ${err}");
            }
        }
        #endregion

        #region interop
        public static class Interop
        {
            private const string libVpx = @"libvpx";

            [DllImport(libVpx)]
            public static unsafe extern vpx_codec_err_t vpx_codec_dec_init_ver([Out] vpx_codec_ctx_t* ctx, vpx_codec_iface_ptr iface, [In] vpx_codec_dec_cfg* cfg, vpx_codec_flags_t flags, int ver);
            [DllImport(libVpx)]
            public static extern vpx_codec_iface_ptr vpx_codec_vp8_dx();
            [DllImport(libVpx)]
            public static extern vpx_codec_iface_ptr vpx_codec_vp9_dx();
            [DllImport(libVpx)]
            public static unsafe extern vpx_codec_err_t vpx_codec_decode([In] vpx_codec_ctx_t* ctx, [In] byte* data, uint data_sz, nint user_priv, long deadline);
            [DllImport(libVpx)]
            public static unsafe extern vpx_image_t* vpx_codec_get_frame([In] vpx_codec_ctx_t* ctx, [In][Out] vpx_codec_iter_t* iter);
            [DllImport(libVpx)]
            public static unsafe extern vpx_codec_err_t vpx_codec_destroy([In] vpx_codec_ctx_t* ctx);

            public const int VPX_IMAGE_ABI_VERSION = 5;
            public const int VPX_CODEC_ABI_VERSION = (4 + VPX_IMAGE_ABI_VERSION);
            public const int VPX_DECODER_ABI_VERSION = (3 + VPX_CODEC_ABI_VERSION);

            public enum vpx_codec_err
            {
                VPX_CODEC_OK = 0,
                VPX_CODEC_ERROR,
                VPX_CODEC_MEM_ERROR,
                VPX_CODEC_ABI_MISMATCH,
                VPX_CODEC_INCAPABLE,
                VPX_CODEC_UNSUP_BITSTREAM,
                VPX_CODEC_UNSUP_FEATURE,
                VPX_CODEC_CORRUPT_FRAME,
                VPX_CODEC_INVALID_PARAM,
                VPX_CODEC_LIST_END
            }

            public struct vpx_codec_dec_cfg
            {
                public uint threads;
                public uint w;
                public uint h;
            }

            public unsafe struct vpx_codec_ctx
            {
                public nint name;
                public vpx_codec_iface_ptr iface;
                public vpx_codec_err err;
                public nint err_detail;
                public int init_flags;
                public vpx_codec_dec_cfg* config;
                public nint priv;
            }

            public enum vpx_img_fmt
            {
                VPX_IMG_FMT_NONE = 0,
                VPX_IMG_FMT_PLANAR = 0x100,
                VPX_IMG_FMT_UV_FLIP = 0x200,
                VPX_IMG_FMT_HAS_ALPHA = 0x400,
                VPX_IMG_FMT_HIGHBITDEPTH = 0x800,
                VPX_IMG_FMT_YV12 = VPX_IMG_FMT_PLANAR | VPX_IMG_FMT_UV_FLIP | 1,
                VPX_IMG_FMT_I420 = VPX_IMG_FMT_PLANAR | 2,
                VPX_IMG_FMT_I422 = VPX_IMG_FMT_PLANAR | 5,
                VPX_IMG_FMT_I444 = VPX_IMG_FMT_PLANAR | 6,
                VPX_IMG_FMT_I440 = VPX_IMG_FMT_PLANAR | 7,
                VPX_IMG_FMT_NV12 = VPX_IMG_FMT_PLANAR | 9,
                VPX_IMG_FMT_I42016 = VPX_IMG_FMT_I420 | VPX_IMG_FMT_HIGHBITDEPTH,
                VPX_IMG_FMT_I42216 = VPX_IMG_FMT_I422 | VPX_IMG_FMT_HIGHBITDEPTH,
                VPX_IMG_FMT_I44416 = VPX_IMG_FMT_I444 | VPX_IMG_FMT_HIGHBITDEPTH,
                VPX_IMG_FMT_I44016 = VPX_IMG_FMT_I440 | VPX_IMG_FMT_HIGHBITDEPTH
            }

            public enum vpx_codec_flags
            {
                VPX_CODEC_USE_POSTPROC = 0x10000,
                VPX_CODEC_USE_ERROR_CONCEALMENT = 0x20000,
                VPX_CODEC_USE_INPUT_FRAGMENTS = 0x40000,
                VPX_CODEC_USE_FRAME_THREADING = 0x80000,
            }

            public enum vpx_color_space
            {
                VPX_CS_UNKNOWN = 0,
                VPX_CS_BT_601 = 1,
                VPX_CS_BT_709 = 2,
                VPX_CS_SMPTE_170 = 3,
                VPX_CS_SMPTE_240 = 4,
                VPX_CS_BT_2020 = 5,
                VPX_CS_RESERVED = 6,
                VPX_CS_SRGB = 7,
            }

            public enum vpx_color_range
            {
                VPX_CR_STUDIO_RANGE = 0,
                VPX_CR_FULL_RANGE = 1,
            }

            public const int VPX_PLANE_PACKED = 0;
            public const int VPX_PLANE_Y = 0;
            public const int VPX_PLANE_U = 1;
            public const int VPX_PLANE_V = 2;
            public const int VPX_PLANE_ALPHA = 3;

            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct vpx_image
            {
                public vpx_img_fmt fmt;
                public vpx_color_space cs;
                public vpx_color_range_t range;

                public uint w;
                public uint h;
                public uint bit_depth;

                public uint d_w;
                public uint d_h;

                public uint r_w;
                public uint r_h;

                public uint x_chroma_shift;
                public uint y_chroma_shift;

                // fixed byte* planes[4]; //error: CS1663
                private byte* planes_0;
                private byte* planes_1;
                private byte* planes_2;
                private byte* planes_3;
                public byte** planes
                {
                    get
                    {
                        fixed (vpx_image* pImage = &this)
                            return &pImage->planes_0;
                    }
                }
                public fixed int stride[4];

                public int bps;
                public void* user_priv;
                public byte* img_data;
                public int img_data_owner;
                public int self_allocd;
                public void* fb_priv;
            }
        }
        #endregion
    }

    public unsafe struct VpxFrame
    {
        internal VpxFrame(vpx_image_t* image)
        {
            this.image = image;
        }

        private readonly vpx_image_t* image;

        public vpx_img_fmt_t Format => image->fmt;
        public int DisplayWidth => (int)image->d_w;
        public int DisplayHeight => (int)image->d_h;
        public IntPtr PlanesY => new IntPtr(image->planes[VpxVideoDecoder.Interop.VPX_PLANE_Y]);
        public IntPtr PlanesU => new IntPtr(image->planes[VpxVideoDecoder.Interop.VPX_PLANE_U]);
        public IntPtr PlanesV => new IntPtr(image->planes[VpxVideoDecoder.Interop.VPX_PLANE_V]);
        public IntPtr PlanesAlpha => new IntPtr(image->planes[VpxVideoDecoder.Interop.VPX_PLANE_ALPHA]);
        public int StrideY => image->stride[VpxVideoDecoder.Interop.VPX_PLANE_Y];
        public int StrideU => image->stride[VpxVideoDecoder.Interop.VPX_PLANE_U];
        public int StrideV => image->stride[VpxVideoDecoder.Interop.VPX_PLANE_V];
        public int StrideAlpha => image->stride[VpxVideoDecoder.Interop.VPX_PLANE_ALPHA];
    }
}
