using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;

namespace WzComparerR2.Rendering
{
    public class D2DFactory : IDisposable
    {
        private static D2DFactory _instance;

        public static D2DFactory Instance
        {
            get
            {
                if (_instance == null || _instance.IsDisposed)
                {
                    _instance = new D2DFactory();
                }
                return _instance;
            }
        }

        private D2DFactory()
        {
            this.factory2D = new SharpDX.Direct2D1.Factory();
            this.factoryDWrite = new SharpDX.DirectWrite.Factory();
            this.dictContext = new ConditionalWeakTable<SharpDX.DisposeBase, D2DContext>();
            this.deviceSwapChainField = typeof(GraphicsDevice)
                    .GetField("_swapChain", BindingFlags.Instance | BindingFlags.NonPublic);
            this.textureResourceField = typeof(Texture)
                    .GetField("_texture", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public bool IsDisposed { get; private set; }

        internal readonly SharpDX.Direct2D1.Factory factory2D;
        internal readonly SharpDX.DirectWrite.Factory factoryDWrite;
        private ConditionalWeakTable<SharpDX.DisposeBase, D2DContext> dictContext;
        private readonly FieldInfo deviceSwapChainField;
        private readonly FieldInfo textureResourceField;

        public D2DContext GetContext(GraphicsDevice graphicsDevice)
        {
            SharpDX.ComObject obj = GetRenderTargetResource(graphicsDevice);
            D2DContext context = GetOrCreateContext(obj);

            if (context == null)
            {
                return null;
            }

            AlphaMode alphaMode = AlphaMode.Ignore;
            if (context.DxgiSurface == null || context.DxgiSurface.IsDisposed)
            {
                if (obj is SharpDX.DXGI.SwapChain)
                {
                    var swapChain = (SharpDX.DXGI.SwapChain)obj;
                    context.DxgiSurface = SharpDX.DXGI.Surface.FromSwapChain(swapChain, 0);
                    alphaMode = AlphaMode.Ignore;
                }
                else if (obj is SharpDX.Direct3D11.Resource)
                {
                    context.DxgiSurface = obj.QueryInterface<SharpDX.DXGI.Surface>();
                    alphaMode = AlphaMode.Premultiplied;
                }
                else
                {
                    return null;
                }
            }

            if (context.D2DRenderTarget == null || context.D2DRenderTarget.IsDisposed)
            {
                var rtProp = new RenderTargetProperties(new PixelFormat(SharpDX.DXGI.Format.Unknown, alphaMode));
                var d2drt = new RenderTarget(this.factory2D, context.DxgiSurface, rtProp);
                d2drt.TextRenderingParams = new RenderingParams(factoryDWrite, 1f, 0f, 0f, PixelGeometry.Flat, RenderingMode.CleartypeGdiClassic);
                d2drt.TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode.Grayscale;
                context.D2DRenderTarget = d2drt;
                context.DxgiSurface.Disposing += (o, e) => d2drt.Dispose();
            }
            
            return context;
        }

        public void ReleaseContext(GraphicsDevice graphicsDevice)
        {
            SharpDX.ComObject obj = GetRenderTargetResource(graphicsDevice);
            D2DContext context;
            if (this.dictContext.TryGetValue(obj, out context))
            {
                context.Dispose();
            }
        }

        private SharpDX.ComObject GetRenderTargetResource(GraphicsDevice graphicsDevice)
        {
            var rt = graphicsDevice.GetRenderTargets();

            SharpDX.ComObject obj;
            if (rt.Length <= 0)
            {
                obj = (SharpDX.DXGI.SwapChain)this.deviceSwapChainField.GetValue(graphicsDevice);
            }
            else
            {
                obj = (SharpDX.Direct3D11.Resource)this.textureResourceField.GetValue(rt[0].RenderTarget);
            }

            return obj;
        }

        private D2DContext GetOrCreateContext(SharpDX.ComObject comObject)
        {
            if (comObject == null)
            {
                return null;
            }

            if (comObject.IsDisposed)
            {
                dictContext.Remove(comObject);
                return null;
            }

            D2DContext context;
            if (!this.dictContext.TryGetValue(comObject, out context))
            {
                context = new D2DContext();
                comObject.Disposing += ComObject_Disposing;
                this.dictContext.Add(comObject, context);
            }
            return context;
        }

        private void ComObject_Disposing(object sender, EventArgs e)
        {
            var comObject = sender as SharpDX.ComObject;
            D2DContext context;
            if (comObject != null && this.dictContext.TryGetValue(comObject, out context))
            {
                context?.Dispose();
                this.dictContext.Remove(comObject);
            }
        }

        ~D2DFactory()
        {
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !this.IsDisposed)
            {
                if (this.factory2D != null)
                {
                    this.factory2D.Dispose();
                }
                if (this.factoryDWrite != null)
                {
                    this.factoryDWrite.Dispose();
                }

                this.dictContext = null;
                this.IsDisposed = true;
            }
        }
    }
}
