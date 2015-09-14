using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using WzComparerR2.WzLib;
using WzComparerR2.PluginBase;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using System.IO;
using System.IO.Compression;
using System.Threading;
using Form = System.Windows.Forms.Form;
using Un4seen.Bass;
using WzComparerR2.MapRender.UI;

using JLChnToZ.IMEHelper;


namespace WzComparerR2.MapRender
{
    public class FrmMapRender2 : Game
    {
        public FrmMapRender2(Wz_Image img)
        {
            graphics = new GraphicsDeviceManager(this);
            this.mapImg = img;
        }

        GraphicsDeviceManager graphics;
        Wz_Image mapImg;
        XnaFont font;
        RenderEnv renderEnv;
        MapScene scene;

        protected override void Initialize()
        {
            base.Initialize();
            this.renderEnv = new RenderEnv(this.graphics);
        }

        protected override void LoadContent()
        {
            base.LoadContent();
            this.font = new XnaFont(this.GraphicsDevice, "微软雅黑", 24);
            this.scene = new MapScene(this.GraphicsDevice);
            this.scene.LoadMap(this.mapImg);
        }

        protected override void Update(GameTime gameTime)
        {
            this.scene.Update(gameTime, renderEnv);
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            this.GraphicsDevice.Clear(Color.Black);
            this.scene.Draw(gameTime, renderEnv);
            base.Draw(gameTime);
        }
    }
}
