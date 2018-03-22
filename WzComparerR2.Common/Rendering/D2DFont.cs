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
                    var font = this.GetMatchingFont();
                    if (font != null)
                    {
                        using (font)
                        {
                            var metrics = font.Metrics;
                            float ratio = this.textFormat.FontSize / metrics.DesignUnitsPerEm;
                            float size = (metrics.Ascent + metrics.Descent + metrics.LineGap) * ratio;
                            this.lineHeight = size;
                        }
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
                /*
                LineSpacingMethod method;
                float lineSpacing;
                float baseLine;
                this.textFormat.GetLineSpacing(out method, out lineSpacing, out baseLine);
                if (baseLine <= 0)
                {
                    var font = this.GetMatchingFont();
                    if (font != null)
                    {
                        using (font)
                        {
                            var metrics = font.Metrics;
                            float ratio = this.textFormat.FontSize / metrics.DesignUnitsPerEm;
                            float ascent = metrics.Ascent * ratio;
                            baseLine = ascent;
                        }
                    }
                }
                this.textFormat.SetLineSpacing(LineSpacingMethod.Uniform, value, value * 0.8f);
                */
                this.lineHeight = value;
            }
        }

        private readonly TextFormat textFormat;
        private float lineHeight;

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

        internal void DrawText(D2DContext context, string text, Vector2 position, Color color)
        {
            var rt = context.D2DRenderTarget;
            /*
            rt.DrawText(text, this.textFormat,
                new SharpDX.RectangleF(position.X, position.Y, Int16.MaxValue, 0),
                context.GetBrush(color),
                DrawTextOptions.None,
                MeasuringMode.GdiNatural);
            */
            using (var layout = this.LayoutString(text, 0, 0))
            {
                rt.DrawTextLayout(new SharpDX.Vector2(position.X, position.Y),
                    layout,
                    context.GetBrush(color),
                    DrawTextOptions.None);
            }
        }

        public Vector2 MeasureString(string text)
        {
            using (var layout = this.LayoutString(text, 0, 0))
            {
                var metrics = layout.Metrics;
                return new Vector2(metrics.Width, metrics.Height);
            }
        }

        private TextLayout LayoutString(string text, int maxWidth, int maxHeight)
        {
            if (maxWidth <= 0)
            {
                maxWidth = Int16.MaxValue;
            }
            var layout = new TextLayout(D2DFactory.Instance.factoryDWrite, text, this.textFormat, maxWidth, 0, 1, false);
            layout.WordWrapping = WordWrapping.NoWrap;
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
