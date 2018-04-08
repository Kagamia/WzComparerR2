using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using SharpDX.Direct2D1;
using SharpDX.DXGI;
using SharpDX.DirectWrite;
using Microsoft.Xna.Framework;

namespace WzComparerR2.Rendering
{
    public class D2DFont : IDisposable
    {
        public D2DFont(string familyName, float size)
            : this(familyName, size, false, false)
        {
            
        }

        public D2DFont(string familyName, float size, bool bold, bool italic)
        {
            this.FamilyName = familyName;
            this.Size = size;

            FontWeight weight = bold ? FontWeight.Bold : FontWeight.Normal;
            FontStyle style = italic ? FontStyle.Italic : FontStyle.Normal;
            var factory = D2DFactory.Instance.factoryDWrite;
            this.textFormat = new TextFormat(factory, this.FamilyName, weight, style, this.Size);

            this.CacheFontMetrics();
            this.Height = this.Height;
        }

        public D2DFont(System.Drawing.Font font) :
            this(font.Name, font.SizeInPoints * 96 / 72, font.Bold, font.Italic)
        {

        }

        public string FamilyName { get; private set; }
        public float Size { get; private set; }

        public float Height
        {
            get
            {
                if (this.lineHeight <= 0)
                {
                    if (this.metrics.DesignUnitsPerEm > 0)
                    {
                        float ratio = this.textFormat.FontSize / this.metrics.DesignUnitsPerEm;
                        float size = (this.metrics.Ascent + this.metrics.Descent + this.metrics.LineGap) * ratio;
                        this.lineHeight = (float)Math.Ceiling(size);
                    }
                    else
                    {
                        this.lineHeight = this.Size;
                    }
                }

                return this.lineHeight;
            }
            set
            {
                LineSpacingMethod method;
                float lineSpacing;
                float baseLine;
                this.textFormat.GetLineSpacing(out method, out lineSpacing, out baseLine);
                if (method == LineSpacingMethod.Default || baseLine <= 0)
                {
                    if (this.metrics.DesignUnitsPerEm > 0)
                    {
                        float ratio = this.textFormat.FontSize / metrics.DesignUnitsPerEm;
                        baseLine = metrics.Ascent * ratio;
                    }
                    else
                    {
                        baseLine = this.Size;
                    }
                }
                this.textFormat.SetLineSpacing(LineSpacingMethod.Uniform, value, baseLine);
                this.lineHeight = value;
            }
        }

        private readonly TextFormat textFormat;
        private float lineHeight;
        private FontMetrics metrics;

        private Font GetMatchingFont()
        {
            var fontCollection = this.textFormat.FontCollection;
            int index;
            if (fontCollection.FindFamilyName(this.textFormat.FontFamilyName, out index))
            {
                using (var family = fontCollection.GetFontFamily(index))
                {
                    var font = family.GetFirstMatchingFont(this.textFormat.FontWeight,
                        this.textFormat.FontStretch,
                        this.textFormat.FontStyle);
                    return font;
                }
            }
            return null;
        }

        private bool CacheFontMetrics()
        {
            var font = this.GetMatchingFont();
            if (font != null)
            {
                using (font)
                {
                    this.metrics = font.Metrics;
                    return true;
                }
            }
            return false;
        }

        internal void DrawText(D2DContext context, string text, Vector2 position, Color color)
        {
             this.DrawText(context, text, position, Vector2.Zero, color);
        }

        internal void DrawText(D2DContext context, string text, Vector2 position, Vector2 size, Color color)
        {
            var rt = context.D2DRenderTarget;

            using (var layout = this.LayoutString(text, size.X, size.Y))
            {
                rt.DrawTextLayout(new SharpDX.Vector2(position.X, position.Y),
                    layout,
                    context.GetBrush(color),
                    DrawTextOptions.None);
            }
        }

        public Vector2 MeasureString(string text)
        {
            return this.MeasureString(text, Vector2.Zero);
        }

        public Vector2 MeasureString(string text, Vector2 size)
        {
            using (var layout = this.LayoutString(text, size.X, size.Y))
            {
                var metrics = layout.Metrics;
                if (metrics.LineCount > 0 && this.metrics.DesignUnitsPerEm > 0)
                {
                    float ratio = this.textFormat.FontSize / this.metrics.DesignUnitsPerEm;
                    var gap = this.lineHeight - (this.metrics.Ascent + this.metrics.Descent) * ratio;
                    if (gap > 0)
                    {
                        metrics.Height -= gap;
                    }
                }

                return new Vector2(metrics.WidthIncludingTrailingWhitespace, metrics.Height);
            }
        }

        private TextLayout LayoutString(string text, float maxWidth, float maxHeight)
        {
            if (maxWidth <= 0)
            {
                maxWidth = Int16.MaxValue;
            }
            var layout = new TextLayout(D2DFactory.Instance.factoryDWrite, text, this.textFormat, maxWidth, 0, 1, false);
            layout.WordWrapping = WordWrapping.Wrap;
            layout.TextAlignment = TextAlignment.Leading;
            layout.ParagraphAlignment = ParagraphAlignment.Near;
            return layout;
        }

        ~D2DFont()
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
            if (disposing)
            {
                this.textFormat.Dispose();
            }
        }
    }
}
