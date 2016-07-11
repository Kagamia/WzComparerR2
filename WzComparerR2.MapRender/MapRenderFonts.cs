using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Microsoft.Xna.Framework.Graphics;
using WzComparerR2.Rendering;

namespace WzComparerR2.MapRender
{
    public class MapRenderFonts : IDisposable
    {
        public MapRenderFonts(GraphicsDevice graphicsDevice)
        {
            this.fonts = new Dictionary<string, XnaFont>();
            this.graphicsDevice = graphicsDevice;
            fonts["default"] = new XnaFont(graphicsDevice, "宋体", 12f);
            fonts["npcName"] = new XnaFont(graphicsDevice, new Font("宋体", 12f, FontStyle.Bold, GraphicsUnit.Pixel));
            fonts["mobName"] = new XnaFont(graphicsDevice, new Font("宋体", 12f, GraphicsUnit.Pixel));
            fonts["tooltipTitle"] = new XnaFont(graphicsDevice, new Font("宋体", 14f, FontStyle.Bold, GraphicsUnit.Pixel));
            fonts["tooltipContent"] = fonts["mobName"];
        }

        Dictionary<string, XnaFont> fonts;
        GraphicsDevice graphicsDevice;

        public GraphicsDevice GraphicsDevice
        {
            get { return graphicsDevice; }
        }

        protected XnaFont this[string key]
        {
            get
            {
                XnaFont font;
                this.fonts.TryGetValue(key, out font);
                return font;
            }
        }

        public XnaFont DefaultFont
        {
            get { return this["default"]; }
        }

        public XnaFont NpcNameFont
        {
            get { return this["npcName"]; }
        }

        public XnaFont MobNameFont
        {
            get { return this["mobName"]; }
        }

        public XnaFont MapNameFont
        {
            get { return this["npcName"]; }
        }

        public XnaFont TooltipTitleFont
        {
            get { return this["tooltipTitle"]; }
        }

        public XnaFont TooltipContentFont
        {
            get { return this["tooltipContent"]; }
        }

        public void Dispose()
        {
            this.Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var kv in fonts)
                {
                    kv.Value.Dispose();
                }
            }
        }
    }
}
