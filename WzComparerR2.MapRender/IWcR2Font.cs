using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using WzComparerR2.Rendering;

namespace WzComparerR2.MapRender
{
    public interface IWcR2Font
    {
        float Size { get; }
        float LineHeight { get; set; }
        object BaseFont { get; }
        Vector2 MeasureString(string text);
        Vector2 MeasureString(StringBuilder text);
    }

    public class D2DFontAdapter : IWcR2Font
    {
        public D2DFontAdapter(D2DFont baseFont)
        {
            if (baseFont == null)
            {
                throw new ArgumentNullException("baseFont");
            }
            this._baseFont = baseFont;
        }

        public float Size { get { return this._baseFont.Size; } }
        public float LineHeight
        {
            get { return this._baseFont.Height; }
            set { this._baseFont.Height = value; }
        }
        public object BaseFont { get { return this._baseFont; } }

        private readonly D2DFont _baseFont;

        public Vector2 MeasureString(string text)
        {
            return this._baseFont.MeasureString(text);
        }

        public Vector2 MeasureString(StringBuilder text)
        {
            return this._baseFont.MeasureString(text.ToString());
        }
    }

    public class XnaFontAdapter : IWcR2Font, IDisposable
    {
        public XnaFontAdapter(XnaFont baseFont)
        {
            if (baseFont == null)
            {
                throw new ArgumentNullException("baseFont");
            }
            this._baseFont = baseFont;
        }

        public float Size { get { return this._baseFont.Height; } }
        public float LineHeight
        {
            get { return this._baseFont.Height; }
            set { this._baseFont.Height = (int)value; }
        }
        public object BaseFont { get { return this._baseFont; } }

        private readonly XnaFont _baseFont;

        public Vector2 MeasureString(string text)
        {
            return this._baseFont.MeasureString(text);
        }

        public Vector2 MeasureString(StringBuilder text)
        {
            return this._baseFont.MeasureString(text);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this._baseFont.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
