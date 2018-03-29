using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Drawing;
using Microsoft.Xna.Framework.Content;
using WzComparerR2.Rendering;
using WzComparerR2.MapRender.Config;

namespace WzComparerR2.MapRender
{
    public class MapRenderFonts : IDisposable
    {
        public static readonly IReadOnlyList<string> DefaultFonts = new ReadOnlyCollection<string>(new[]
        {
            "SimSun", "Dotum"
        });

        public static string GetFontResourceKey(string familyName, float size, FontStyle style)
        {
            string assetName = string.Join(",", familyName, size, style);
            return assetName;
        }

        public MapRenderFonts()
        {
            this.fonts = new Dictionary<string, IWcR2Font>();
        }

        private Dictionary<string, IWcR2Font> fonts;

        public void LoadContent(ContentManager content)
        {
            var config = MapRenderConfig.Default;

            var fontIndex = config.DefaultFontIndex;
            if (fontIndex < 0 || fontIndex >= DefaultFonts.Count)
            {
                fontIndex = 0;
            }

            string familyName = DefaultFonts[fontIndex];

            fonts["default"] = content.Load<IWcR2Font>(GetFontResourceKey(familyName, 12f, FontStyle.Regular));
            fonts["npcName"] = fonts["default"];
            fonts["mobName"] = fonts["default"];
            fonts["mobLevel"] = content.Load<IWcR2Font>(GetFontResourceKey("Tahoma", 9f, FontStyle.Regular));
            fonts["tooltipTitle"] = content.Load<IWcR2Font>(GetFontResourceKey(familyName, 14f, FontStyle.Bold));
            fonts["tooltipContent"] = fonts["default"];
        }

        protected IWcR2Font this[string key]
        {
            get
            {
                IWcR2Font font;
                this.fonts.TryGetValue(key, out font);
                return font;
            }
        }

        public IWcR2Font DefaultFont
        {
            get { return this["default"]; }
        }

        public IWcR2Font NpcNameFont
        {
            get { return this["npcName"]; }
        }

        public IWcR2Font MobNameFont
        {
            get { return this["mobName"]; }
        }

        public IWcR2Font MobLevelFont
        {
            get { return this["mobLevel"]; }
        }

        public IWcR2Font MapNameFont
        {
            get { return this["npcName"]; }
        }

        public IWcR2Font TooltipTitleFont
        {
            get { return this["tooltipTitle"]; }
        }

        public IWcR2Font TooltipContentFont
        {
            get { return this["tooltipContent"]; }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.fonts.Clear();
        }
    }
}
