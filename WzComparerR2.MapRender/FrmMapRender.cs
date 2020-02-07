#if MapRenderV1

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

//using JLChnToZ.IMEHelper;
using WzComparerR2.MapRender.Patches;
using WzComparerR2.Common;
using WzComparerR2.Rendering;

namespace WzComparerR2.MapRender
{
    public class FrmMapRender : Game
    {
        public FrmMapRender()
        {
            graphics = new GraphicsDeviceManager(this);
            this.loadState = LoadState.NotLoad;
            this.IsMouseVisible = true;
            this.renderingList = new List<RenderPatch>();
            this.willReloadBgm = true;
            this.profile = new StringBuilder();
            this.showProfile = true;
            this.fpsCounter = new FpsCounter(this);
            this.fpsCounter.UseStopwatch = true;
            this.cameraInMap = true;
            this.patchVisibility = new PatchVisibility();
            this.patchVisibility.FootHoldVisible = false;
            this.patchVisibility.LadderRopeVisible = false;
            this.volumeGlobal = 100;

            Form windowForm = (Form)Form.FromHandle(this.Window.Handle);
            windowForm.GotFocus += windowForm_GotFocus;
            windowForm.LostFocus += windowForm_LostFocus;

            GameExt.FixKeyboard(this);
            /*
            this.chat = new Chat();
            this.chat.Connected += chat_Connected;
            this.chat.Error += chat_Error;
            this.chat.MessageReceived += chat_MessageReceived;*/
        }

        void chat_Connected(object sender, EventArgs e)
        {
            if (this.txtChat.Length > 0)
            {
                this.txtChat.AppendLine();
            }
            this.txtChat.Append("已经连接服务器");
        }

        protected override void OnActivated(object sender, EventArgs args)
        {
            base.OnActivated(sender, args);
            GameExt.FixKeyboard(this);
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            base.OnDeactivated(sender, args);
        }
        /*
        void chat_Error(object sender, ChatErrorEventArgs e)
        {
            if (this.txtChat.Length > 0)
            {
                this.txtChat.AppendLine();
            }
            this.txtChat.Append("错误：" + e.Error);
        }

        void chat_MessageReceived(object sender, ChatMessageEventArgs e)
        {
            if (this.txtChat.Length > 0)
            {
                this.txtChat.AppendLine();
            }
            this.txtChat.AppendFormat("[{0}] {1}", e.FromName ?? "系统公告", e.MessageText);
        }*/

        public FrmMapRender(Wz_Image mapImg)
            : this()
        {
            this.mapImg = mapImg;
        }

        RenderEnv renderEnv;
        Tooltip tooltip;
        GraphicsDeviceManager graphics;
        LoadState loadState;
        TimeSpan stateChangedTime;
        const int EnteringTime = 1500;
        const int ExitingTime = 1500;

        //双缓冲用...
        RenderTarget2D target2D;

        //资源...
        Wz_Image mapImg;
        int mapID;
        string mapMark;
        MiniMap miniMap;
        List<RenderPatch> renderingList;
        StringLinker stringLinker;
        TextureLoader texLoader;
        PatchVisibility patchVisibility;

        //bgm用
        IntPtr bgmStream;
        byte[] bgmFile;
        string bgmName;
        float bgmVolume;
        int volumeGlobal; //0-100
        bool isSilent;
        bool silentOnDeactive;

        //和地图切换相关的参数...
        bool willReloadBgm;
        string enterPortal;

        //截图用
        bool prepareCapture;
        bool snapshotSaving;

        //调试用
        StringBuilder profile;
        bool showProfile;
        FpsCounter fpsCounter;
        bool cameraInMap;

        //ui
        UIMiniMap uiMinimap;

        //输入
        //IMEHandler ime;
        StringBuilder txtInput = new StringBuilder();
        bool isOnCommand;

        //Chat chat;
        StringBuilder txtChat = new StringBuilder();

        public StringLinker StringLinker
        {
            get { return stringLinker; }
            set { stringLinker = value; }
        }

        protected override void Initialize()
        {
            base.Initialize();
            //this.ime = new IMEHandler(this, true);
            //this.ime.onResultReceived += ime_onResultReceived;
            ChangeDisplayMode(0);
            //ThreadPool.QueueUserWorkItem((o) => this.chat.Connect());
        }


        /*
        void ime_onResultReceived(object sender, IMEResultEventArgs e)
        {
            if (isOnCommand)
            {
                switch (e.result)
                {
                    case '\b':
                        if (txtInput.Length > 0)
                        {
                            txtInput.Remove(txtInput.Length - 1, 1);
                        }
                        break;
                    case '\x03': //复制
                        break;
                    case '\x16': //粘贴
                        {
                            string text = System.Windows.Forms.Clipboard.GetText();
                            if (text != null)
                            {
                                int idx = text.IndexOfAny("\r\n".ToCharArray());
                                if (idx > -1)
                                {
                                    text = text.Substring(0, idx);
                                }
                                txtInput.Append(text);
                            }
                        }
                        break;
                    case '\r': //回车
                        
                        if (this.chat.IsConnected)
                        {
                            string content = this.txtInput.ToString();
                            if (content.StartsWith("/setName "))
                            {
                                ThreadPool.QueueUserWorkItem(o => this.chat.SetName(content.Substring(9)));
                            }
                            else
                            {
                                ThreadPool.QueueUserWorkItem(o => this.chat.Talk(content));
                            }
                            this.txtInput.Remove(0, this.txtInput.Length);
                        }
                        this.ime.Enabled = false;
                        break;
                    default:
                        txtInput.Append(e.result);
                        break;
                }
            }
        }*/

        protected override void LoadContent()
        {
            base.LoadContent();
            this.renderEnv = new RenderEnv(this, this.graphics);
            this.tooltip = new Tooltip(this.GraphicsDevice);
            this.texLoader = new TextureLoader(this.GraphicsDevice);
            
            //初始化UI
            this.uiMinimap = new UIMiniMap(this.GraphicsDevice);
            this.uiMinimap.MapNameFont = this.renderEnv.Fonts.MapNameFont;
        }

        void windowForm_LostFocus(object sender, EventArgs e)
        {
            if (this.silentOnDeactive)
            {
                isSilent = true;
                this.UpdateBgmVolume();
            }
        }

        void windowForm_GotFocus(object sender, EventArgs e)
        {
            if (this.silentOnDeactive)
            {
                isSilent = false;
                this.UpdateBgmVolume();
            }
        }

        private void ChangeDisplayMode(int i)
        {
            this.renderEnv.Camera.DisplayMode = i;
            Form windowForm = (Form)Form.FromHandle(this.Window.Handle);

            switch (i)
            {
                default:
                case 0:
                case 1:
                case 2:
                    windowForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
                    break;
                case 3:
                    windowForm.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                    break;
            }

            windowForm.SetDesktopLocation(0, 0);
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            foreach (RenderPatch patch in this.renderingList)
            {
                patch.Dispose();
            }
            this.renderingList.Clear();
            Bass.BASS_StreamFree(this.bgmStream.ToInt32());
            this.renderEnv.Dispose();
           // this.ime.Dispose();
            //this.chat.Disconnect();
        }

        protected override void Update(GameTime gameTime)
        {
            this.fpsCounter.Update(gameTime);
            this.renderEnv.Input.Update(gameTime);

            LoadState oldState = this.loadState;
            switch (this.loadState)
            {
                case LoadState.NotLoad:
                    if (this.mapImg != null)
                    {
                        Thread loadMapThread = new Thread(LoadMap);
                        loadMapThread.Start();
                        this.loadState = LoadState.Loading;
                    }
                    break;
                case LoadState.Loading:
                    // waiting...
                    break;
                case LoadState.LoadSuccessed:
                    if (willReloadBgm)
                    {
                        this.bgmVolume = 0f;
                        UpdateBgmVolume();
                    }
                    Bass.BASS_ChannelPlay(this.bgmStream.ToInt32(), false);
                    //prepare camera
                    {
                        foreach (RenderPatch patch in this.renderingList)
                        {
                            PortalPatch portal;
                            if (patch.ObjectType == RenderObjectType.Portal
                                && (portal = patch as PortalPatch) != null
                                && ((this.enterPortal == null) ?
                                portal.PortalType == 0 : //sp
                                portal.PortalName == this.enterPortal))
                            {
                                this.renderEnv.Camera.Center = portal.Position;
                                break;
                            }
                        }
                    }
                    
                    this.loadState = LoadState.Entering;

                    //prepare minimap
                    this.uiMinimap.MiniMap = this.miniMap;
                    
                    StringResult sr;
                    if (this.stringLinker != null && this.stringLinker.StringMap.TryGetValue(this.mapID, out sr))
                    {
                        this.uiMinimap.MapName = sr["mapName"];
                        this.uiMinimap.StreetName = sr["streetName"];
                    }
                    else
                    {
                        this.uiMinimap.MapName = null;
                        this.uiMinimap.StreetName = null;
                    }
                    this.uiMinimap.UpdateSize();

                    //首次更新portal状态
                    UpdatePortalVisibility();
                    break;
                case LoadState.LoadFailed:
                    // flag state
                    break;
                case LoadState.Entering:
                    UpdateCamera(gameTime);
                    UpdatePatch(gameTime);
                    {
                        double time = (gameTime.TotalGameTime - stateChangedTime).TotalMilliseconds;
                        if (willReloadBgm)
                        {
                            this.bgmVolume = (float)(time / EnteringTime);
                            UpdateBgmVolume();
                        }
                        if (time > EnteringTime)
                        {
                            this.loadState = LoadState.Rendering;
                        }
                    }
                    break;
                case LoadState.Rendering:
                    UpdateInput(gameTime);
                    UpdateCamera(gameTime);
                    UpdatePatch(gameTime);
                    break;
                case LoadState.Exiting:
                    UpdateInput(gameTime);
                    UpdatePatch(gameTime);
                    {
                        double time = (gameTime.TotalGameTime - stateChangedTime).TotalMilliseconds;
                        if (willReloadBgm)
                        {
                            this.bgmVolume = (float)(1 - time / ExitingTime);
                            UpdateBgmVolume();
                        }
                        if (time > ExitingTime)
                        {
                            if (willReloadBgm)
                            {
                                Bass.BASS_ChannelStop(this.bgmStream.ToInt32());
                                Bass.BASS_StreamFree(this.bgmStream.ToInt32());
                            }
                            DisposePatch();
                            this.loadState = LoadState.NotLoad;
                        }
                    }
                    break;
            }

            if (this.loadState != oldState)
            {
                stateChangedTime = gameTime.TotalGameTime;
            }

            base.Update(gameTime);
        }

        private void UpdateInput(GameTime gameTime)
        {
            if (!this.IsActive)
                return;
            InputState input = renderEnv.Input;

            if (!this.isOnCommand)
            {
                OnGlobalHotKey(gameTime);
            }
            else
            {
                //OnInputMethod(gameTime);
            }
        }

        /*
        private void OnInputMethod(GameTime gameTime)
        {
            InputState input = renderEnv.Input;
            if (!this.ime.Enabled)
            {
                this.isOnCommand = false;
            }
        }*/

        private void OnGlobalHotKey(GameTime gameTime)
        {
            Camera camera = renderEnv.Camera;
            InputState input = renderEnv.Input;

            if (input.IsAltPressing && input.IsCtrlPressing && input.IsShiftPressing && input.IsKeyDown(Keys.Insert))
            {
                this.isOnCommand = !this.isOnCommand;
                //this.ime.Enabled = this.isOnCommand; //开关输入法支持
            }

            if (input.IsAltPressing && input.IsKeyDown(Keys.Enter))
            {
                camera.DisplayMode = (camera.DisplayMode + 1) % 4;
                this.ChangeDisplayMode(camera.DisplayMode);
            }


            if (input.IsKeyDown(Keys.Scroll))
            {
                if (!snapshotSaving)
                {
                    this.renderEnv.Camera.UseWorldRect = true;
                    prepareCapture = true;
                }
            }

            if (input.IsKeyDown(Keys.F1))
            {
                this.SaveAllTexture();
            }

            if (input.IsKeyDown(Keys.F5))
            {
                this.showProfile = !this.showProfile;
            }

            //调整音量
            if (input.IsKeyDown(Keys.OemPlus) || input.IsKeyDown(Keys.Add))
            {
                if (input.IsCtrlPressing) //ctrl+加号
                {
                    this.silentOnDeactive = true;
                }
                else
                {
                    this.volumeGlobal = Math.Max(0, Math.Min(this.volumeGlobal + 5, 100));
                }
                this.UpdateBgmVolume();
            }
            if (input.IsKeyDown(Keys.OemMinus) || input.IsKeyDown(Keys.Subtract))
            {
                if (input.IsCtrlPressing) //ctrl+减号
                {
                    this.silentOnDeactive = false;
                }
                else
                {
                    this.volumeGlobal = Math.Max(0, Math.Min(this.volumeGlobal - 5, 100));
                }
                this.UpdateBgmVolume();
            }
            if (input.IsKeyDown(Keys.M))
            {
                this.uiMinimap.Visible = !this.uiMinimap.Visible;
            }

            if (input.IsCtrlPressing)
            {
                if (input.IsKeyDown(Keys.U)) //设置镜头限制
                {
                    this.cameraInMap = !this.cameraInMap;
                }

                if (input.IsKeyDown(Keys.D1))
                {
                    this.patchVisibility.BackVisible = !this.patchVisibility.BackVisible;
                }
                if (input.IsKeyDown(Keys.D2))
                {
                    this.patchVisibility.ReactorVisible = !this.patchVisibility.ReactorVisible;
                }
                if (input.IsKeyDown(Keys.D3))
                {
                    this.patchVisibility.ObjVisible = !this.patchVisibility.ObjVisible;
                }
                if (input.IsKeyDown(Keys.D4))
                {
                    this.patchVisibility.TileVisible = !this.patchVisibility.TileVisible;
                }
                if (input.IsKeyDown(Keys.D5))
                {
                    this.patchVisibility.NpcVisible = !this.patchVisibility.NpcVisible;
                }
                if (input.IsKeyDown(Keys.D6))
                {
                    this.patchVisibility.MobVisible = !this.patchVisibility.MobVisible;
                }
                if (input.IsKeyDown(Keys.D7))
                {
                    if (this.patchVisibility.FootHoldVisible)
                    {
                        this.patchVisibility.FootHoldVisible = false;
                        this.patchVisibility.LadderRopeVisible = false;
                    }
                    else
                    {
                        this.patchVisibility.FootHoldVisible = true;
                        this.patchVisibility.LadderRopeVisible = true;
                    }
                }
                if (input.IsKeyDown(Keys.D8))
                {
                    if (!this.patchVisibility.PortalVisible)
                    {
                        this.patchVisibility.PortalVisible = true;
                        this.patchVisibility.PortalInEditMode = false;
                    }
                    else if (!this.patchVisibility.PortalInEditMode)
                    {
                        this.patchVisibility.PortalInEditMode = true;
                    }
                    else
                    {
                        this.patchVisibility.PortalVisible = false;
                    }

                    UpdatePortalVisibility();
                }
                if (input.IsKeyDown(Keys.D9))
                {
                    this.patchVisibility.FrontVisible = !this.patchVisibility.FrontVisible;
                }
            }
        }

        private void UpdatePortalVisibility()
        {
            PortalPatch portal;
            foreach (RenderPatch patch in this.renderingList)
            {
                portal = patch as PortalPatch;
                if (portal != null)
                {
                    portal.EditMode = this.patchVisibility.PortalInEditMode;
                }
            }
        }

        private void UpdateCamera(GameTime gameTime)
        {
            if (!this.IsActive)
            {
                return;
            }
            InputState input = renderEnv.Input;

            int boost = 1;
            Vector2 offs = new Vector2();
            if (input.IsKeyPressing(Keys.Left))
                offs.X -= 5;
            if (input.IsKeyPressing(Keys.Right))
                offs.X += 5;
            if (input.IsKeyPressing(Keys.Up))
                offs.Y -= 5;
            if (input.IsKeyPressing(Keys.Down))
                offs.Y += 5;

            if (input.IsMouseButtonPressing(MouseButton.RightButton))
            {
                offs.X = 5f * input.MousePosition.X / this.renderEnv.Camera.Width - 2.5f;
                if (offs.X > 1.5f || offs.X < -1.5f)
                {
                    offs.X = offs.X - 1.5f * Math.Sign(offs.X);
                }
                else
                {
                    offs.X = 0;
                }

                offs.Y = 5f * input.MousePosition.Y / this.renderEnv.Camera.Height - 2.5f;
                if (offs.Y > 1.5f || offs.Y < -1.5f)
                {
                    offs.Y = offs.Y - 1.5f * Math.Sign(offs.Y);
                }
                else
                {
                    offs.Y = 0;
                }

                offs *= 10;
            }

            if (input.IsCtrlPressing)
                boost = 2;
            this.renderEnv.Camera.Center += offs * boost;
            if (this.cameraInMap)
            {
                this.renderEnv.Camera.AdjustToWorldRect();
            }
        }

        private void UpdateBgmVolume()
        {
            float percent = this.bgmVolume;
            if (this.isSilent)
            {
                percent = 0;
            }
            else
            {
                percent *= 0.01f * volumeGlobal;
            }
            percent = MathHelper.Clamp(percent, 0, 1);
            Bass.BASS_ChannelSetAttribute(this.bgmStream.ToInt32(), BASSAttribute.BASS_ATTRIB_VOL, percent);
        }

        private void UpdatePatch(GameTime gameTime)
        {
            bool movingOnce = false;
            tooltip.TooltipTarget = null;
            foreach (RenderPatch patch in this.renderingList)
            {
                patch.Update(gameTime, renderEnv);

                bool mouseover = patch.RenderArgs.DisplayRectangle.Contains(renderEnv.Camera.CameraToWorld(renderEnv.Input.MousePosition));
                if (mouseover)
                {
                    tooltip.TooltipTarget = patch;
                }

                if (this.IsActive && !movingOnce && patch.ObjectType == RenderObjectType.Portal)
                {
                    PortalPatch portal = patch as PortalPatch;

                    if (mouseover && this.renderEnv.Input.IsMouseButtonDown(MouseButton.LeftButton))
                    {
                        if (portal.ToMap == this.mapID)
                        {
                            PortalPatch portal2;
                            foreach (RenderPatch patch2 in this.renderingList)
                            {
                                if (patch.ObjectType == RenderObjectType.Portal
                                    && (portal2 = patch2 as PortalPatch) != null
                                   && portal2.PortalName == portal.ToName)
                                {
                                    this.renderEnv.Camera.Center = portal2.Position;
                                    movingOnce = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            if (PreLoadNextMap(portal.ToMap))
                            {
                                this.enterPortal = portal.ToName;
                                this.loadState = LoadState.Exiting;
                            }
                        }
                    }
                }
            }
        }

        private bool PreLoadNextMap(int mapID)
        {
            Wz_Image newMapImg = FindMapByID(mapID);
            if (newMapImg == null)
                return false;
            this.mapImg = newMapImg;
            string newBgm = newMapImg.Node.FindNodeByPath("info\\bgm").GetValueEx<string>(null);
            this.willReloadBgm = (newBgm != this.bgmName);
            return true;
        }

        private void DisposePatch()
        {
            //foreach (RenderPatch patch in this.renderingList)
            //{
            //    patch.Dispose();
            //}
            this.renderingList.Clear();
        }

        protected override void Draw(GameTime gameTime)
        {
            this.fpsCounter.Draw(gameTime);
            Color bgColor = Color.Black;

            if (prepareCapture)
            {
                //检查显卡支持纹理大小
                var maxTextureWidth = 4096;
                var maxTextureHeight = 4096;

                Rectangle oldRect = this.renderEnv.Camera.WorldRect;
                int width = Math.Min(oldRect.Width, maxTextureWidth);
                int height = Math.Min(oldRect.Height, maxTextureHeight);

                var target2d = new RenderTarget2D(this.GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.Depth24);

                //计算一组截图区
                int horizonBlock = (int)Math.Ceiling(1.0 * oldRect.Width / width);
                int verticalBlock = (int)Math.Ceiling(1.0 * oldRect.Height / height);
                Color[,][] picBlocks = new Color[horizonBlock, verticalBlock][];
                for (int j = 0; j < verticalBlock; j++)
                {
                    for (int i = 0; i < horizonBlock; i++)
                    {
                        //计算镜头区域
                        this.renderEnv.Camera.WorldRect = new Rectangle(
                            oldRect.X + i * width,
                            oldRect.Y + j * height,
                            width,
                            height);
                        //绘制
                        GraphicsDevice.SetRenderTarget(target2d);
                        GraphicsDevice.Clear(bgColor);
                        this.renderEnv.Sprite.Begin(SpriteSortMode.Deferred, StateEx.NonPremultipled_Hidef());
                        //this.SetRenderState();
                        foreach (RenderPatch patch in this.renderingList)
                        {
                            if (this.patchVisibility.IsVisible(patch.ObjectType)
                                && patch.RenderArgs.Visible)
                            {
                                patch.Draw(gameTime, renderEnv);
                            }
                        }
                        this.renderEnv.Sprite.End();
                        GraphicsDevice.SetRenderTarget(null);
                        //保存
                        Texture2D t2d = target2d;
                        Color[] data = new Color[target2d.Width * target2d.Height];
                        t2d.GetData<Color>(data);
                        picBlocks[i, j] = data;
                    }
                }

                target2d.Dispose();
                SaveTexture(picBlocks, oldRect.Width, oldRect.Height, target2d.Width, target2d.Height);

                //这帧就过去吧 阿门...
                GraphicsDevice.Clear(bgColor);
                this.renderEnv.Camera.WorldRect = oldRect;
                this.renderEnv.Camera.UseWorldRect = false;
                prepareCapture = false;
            }
            else
            {
                PresentationParameters pp = GraphicsDevice.PresentationParameters;
                if (this.target2D == null
                    || this.target2D.Width != pp.BackBufferWidth
                    || this.target2D.Height != pp.BackBufferHeight)
                {
                    if (this.target2D != null)
                    {
                        this.target2D.Dispose();
                    }
                    this.target2D = new RenderTarget2D(this.GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight, false , SurfaceFormat.Color, DepthFormat.Depth24);
                }
                GraphicsDevice.SetRenderTarget(this.target2D);
                GraphicsDevice.Clear(bgColor);

                switch (this.loadState)
                {
                    case LoadState.Entering:
                    case LoadState.Rendering:
                    case LoadState.Exiting:
                        this.renderEnv.Sprite.Begin(SpriteSortMode.Deferred, StateEx.NonPremultipled_Hidef());
                        //this.SetRenderState();
                        foreach (RenderPatch patch in this.renderingList)
                        {
                            if (this.patchVisibility.IsVisible(patch.ObjectType)
                                && patch.RenderArgs.Visible && !patch.RenderArgs.Culled)
                            {
                                patch.Draw(gameTime, renderEnv);
                                tooltip.DrawNameTooltip(gameTime, renderEnv, patch, stringLinker);
                            }
                        }
                        //绘制tooltip
                        tooltip.DrawTooltip(gameTime, renderEnv, stringLinker);
                        this.renderEnv.Sprite.End();

                        //渲染UI
                        if (this.uiMinimap.Visible)
                        {
                            this.uiMinimap.Position = new Vector2(0, this.showProfile ? 16 : 0);
                            this.uiMinimap.Draw(this.renderEnv, gameTime);
                        }
                        break;
                }

                GraphicsDevice.SetRenderTarget(null);
                GraphicsDevice.Clear(bgColor);
                float alpha;
                switch (this.loadState)
                {
                    case LoadState.Entering:
                        {
                            double time = (gameTime.TotalGameTime - stateChangedTime).TotalMilliseconds;
                            alpha = MathHelper.Clamp((float)(time / EnteringTime), 0, 1);
                        }
                        break;
                    case LoadState.Exiting:
                        {
                            double time = (gameTime.TotalGameTime - stateChangedTime).TotalMilliseconds;
                            alpha = MathHelper.Clamp((float)(1 - time / ExitingTime), 0, 1);
                        }
                        break;
                    case LoadState.Rendering: alpha = 1f; break;
                    default: alpha = 0f; break;
                }

                this.renderEnv.Sprite.Begin(SpriteSortMode.Deferred, StateEx.NonPremultipled_Hidef());
                //this.SetRenderState();
                this.renderEnv.Sprite.Draw(this.target2D,
                    new Rectangle(0, 0, this.target2D.Width, this.target2D.Height),
                    new Color(Color.White, alpha));
                this.renderEnv.Sprite.End();

                this.renderEnv.Sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
                if (showProfile)
                {
                    profile.Remove(0, profile.Length);
                    profile.Append("[F5-Hide] ");
                    //显示地图名字
                    if (!this.uiMinimap.Visible)
                    {
                        profile.Append("[").Append(this.mapID).Append(" ");
                        StringResult sr;
                        if (this.stringLinker != null && this.stringLinker.StringMap.TryGetValue(this.mapID, out sr))
                            profile.Append(sr.Name);
                        else
                            profile.Append("(null)");
                        profile.Append("] ");
                    }
                    //显示bgm名字
                    profile.Append("[");
                    profile.Append(bgmName);
                    profile.AppendFormat(" {0}%{1}", volumeGlobal, this.silentOnDeactive ? "A" : null);
                    profile.Append("] ");
                    //显示当前渲染状态机
                    profile.AppendFormat("[{0:p2} {1}] ", alpha, this.loadState);
                    profile.AppendFormat("[fps u:{0:f2} d:{1:f2}] ", fpsCounter.UpdatePerSec, fpsCounter.DrawPerSec);

                    //可见性：
                    profile.Append(" ctrl+");

                    int[] array = new[] { 1, 2, 3, 4, 5, 6, 7, 9, 10 };
                    for (int i = 0; i < array.Length; i++)
                    {
                        RenderObjectType type = (RenderObjectType)array[i];
                        profile.Append(this.patchVisibility.IsVisible(type) ? "-" : (i + 1).ToString());
                    }
                    this.renderEnv.Sprite.FillRectangle(new Rectangle(0, 0, renderEnv.Camera.Width, 16), new Color(Color.Black, 0.4f));
                    this.renderEnv.Sprite.DrawStringEx(this.renderEnv.Fonts.DefaultFont, profile, Vector2.Zero, Color.Cyan);
                }
                this.renderEnv.Sprite.End();

                //绘制文本框
                this.renderEnv.Sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
                if (this.isOnCommand)
                {
                    Rectangle rect = new Rectangle(0, this.renderEnv.Camera.Height - 16, renderEnv.Camera.Width, 16);
                    this.renderEnv.Sprite.FillRectangle(rect, new Color(Color.Black, 0.8f));
                    this.renderEnv.Sprite.DrawRectangle(rect, Color.Gray);
                    this.renderEnv.Sprite.DrawStringEx(this.renderEnv.Fonts.DefaultFont, txtInput, new Vector2(rect.Left + 2, rect.Top + 2), Color.White);
                }
                this.renderEnv.Sprite.End();

                //绘制聊天栏

                if (this.txtChat.Length > 0)
                {
                    Rectangle rect = new Rectangle(0, this.renderEnv.Camera.Height - 16 - 150, renderEnv.Camera.Width / 2, 150);
                    XnaFont font = this.renderEnv.Fonts.DefaultFont;
                    Vector2 size = font.MeasureString(this.txtChat);
                    Vector2 origin = Vector2.Zero;
                    if (size.Y > rect.Height)
                    {
                        origin = new Vector2(0, rect.Height - size.Y);
                    }
                    this.GraphicsDevice.ScissorRectangle = rect;
                    this.GraphicsDevice.RasterizerState = StateEx.Scissor();
                    this.renderEnv.Sprite.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
                    this.renderEnv.Sprite.FillRectangle(rect, new Color(Color.Black, this.isOnCommand ? 0.8f : 0.5f));
                    this.renderEnv.Sprite.DrawRectangle(rect, Color.Gray);
                    this.renderEnv.Sprite.DrawStringEx(font, txtChat, new Vector2(rect.X + origin.X, rect.Y + origin.Y), Color.White);
                    this.renderEnv.Sprite.End();

                    this.GraphicsDevice.ScissorRectangle = Rectangle.Empty;
                    this.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                }

            }
            base.Draw(gameTime);
        }

        private void SaveTexture(Color[,][] picBlocks, int mapWidth, int mapHeight, int blockWidth, int blockHeight)
        {
            this.snapshotSaving = true;

            new Thread(() =>
            {

                //透明处理
                foreach (Color[] data in picBlocks)
                {
                    for (int i = 0, j = data.Length; i < j; i++)
                    {
                        if (data[i].A < 255)
                        {
                            //data[i].R = (byte)((data[i].R * data[i].A + bgColor.R * (255 - data[i].A)) / 255);
                            //data[i].G = (byte)((data[i].G * data[i].A + bgColor.G * (255 - data[i].A)) / 255);
                            //data[i].B = (byte)((data[i].B * data[i].A + bgColor.B * (255 - data[i].A)) / 255);
                            data[i].A = 255;
                        }
                    }
                }

                //组装
                byte[] mapData = new byte[mapWidth * mapHeight * 4];
                for (int j = 0; j < picBlocks.GetLength(1); j++)
                {
                    for (int i = 0; i < picBlocks.GetLength(0); i++)
                    {
                        Color[] data = picBlocks[i, j];
                        IntPtr pData = Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);

                        Rectangle blockRect = new Rectangle();
                        blockRect.X = i * blockWidth;
                        blockRect.Y = j * blockHeight;
                        blockRect.Width = Math.Min(mapWidth - blockRect.X, blockWidth);
                        blockRect.Height = Math.Min(mapHeight - blockRect.Y, blockHeight);

                        int length = blockRect.Width * 4;
                        if (blockRect.X == 0 && blockRect.Width == mapWidth) //整块复制
                        {
                            int startIndex = mapWidth * 4 * blockRect.Y;
                            Marshal.Copy(pData, mapData, startIndex, blockRect.Width * blockRect.Height * 4);
                        }
                        else //逐行扫描
                        {
                            for (int y = blockRect.Top, y0 = blockRect.Bottom; y < y0; y++)
                            {
                                int startIndex = (y * mapWidth + blockRect.X) * 4;
                                Marshal.Copy(pData, mapData, startIndex, length);
                                pData = new IntPtr(pData.ToInt32() + blockWidth * 4);
                            }
                        }
                    }
                }

                //反色
                MonogameUtils.BgraToColor(mapData);

                try
                {
                    System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(
                        mapWidth,
                        mapHeight,
                        mapWidth * 4,
                        System.Drawing.Imaging.PixelFormat.Format32bppArgb,
                        System.Runtime.InteropServices.Marshal.UnsafeAddrOfPinnedArrayElement(mapData, 0));

                    bitmap.Save(DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + mapID.ToString("D9") + ".png",
                        System.Drawing.Imaging.ImageFormat.Png);

                    bitmap.Dispose();
                }
                catch
                {
                }
                finally
                {
                    this.snapshotSaving = false;
                }
            }).Start();
        }

        private void SaveAllTexture()
        {
            DirectoryInfo dir = Directory.CreateDirectory(DateTime.Now.ToString("yyyyMMddHHmmssfff"));
            foreach (RenderPatch patch in this.renderingList)
            {
                string path = Path.Combine(dir.FullName, patch.Name + ".png");
                if (patch.Frames != null)
                {
                    var texture = patch.Frames[0].Texture;
                    using (var file = File.OpenWrite(path))
                    {
                        texture.SaveAsPng(file);
                    }
                }
            }
        }

        private void LoadMap()
        {
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            this.texLoader.BeginCounting();
            try
            {
                this.LoadInfo();
                this.LoadBack();
                this.LoadObjTile();
                this.LoadFoothold();
                this.LoadLife();
                this.LoadPortal();
                this.LoadReactor();
                this.LoadTooltip();
                this.LoadLadderRope();
                this.LoadMinimap();
                this.CalcMapSize();
                this.renderingList.Sort(new Comparison<RenderPatch>(RenderPatchComarison));
                this.loadState = LoadState.LoadSuccessed;
            }
            catch (Exception ex)
            {
                this.loadState = LoadState.LoadFailed;
                System.Windows.Forms.MessageBox.Show(ex.ToString(), "MapRender");
            }
            this.texLoader.EndCounting();
            this.texLoader.ClearUnusedTexture();
            sw.Stop();
            double ms = sw.Elapsed.TotalMilliseconds;

            StringResult sr;
            if (this.stringLinker != null && this.stringLinker.StringMap.TryGetValue(this.mapID, out sr))
            {
                this.Window.Title = this.mapID + " " + sr.Name;
            }
            else
            {
                this.Window.Title = this.mapID + " (null)";
            }
        }

        private int RenderPatchComarison(RenderPatch a, RenderPatch b)
        {
            for (int i = 0; i < a.ZIndex.Length; i++)
            {
                int dz = a.ZIndex[i].CompareTo(b.ZIndex[i]);
                if (dz != 0)
                    return dz;
            }
            return ((int)a.ObjectType).CompareTo((int)b.ObjectType);
        }

        private void LoadInfo()
        {
            this.renderEnv.Camera.WorldRect = new Rectangle();
            this.mapID = -1;

            if (mapImg == null || !mapImg.TryExtract())
                return;

            int mapID;
            if (mapImg.Name.Length >= 9
                && Int32.TryParse(mapImg.Name.Substring(0, 9), out mapID))
            {
                this.mapID = mapID;
            }

            Wz_Node info = mapImg.Node.FindNodeByPath("info");
            if (info != null)
            {
                Wz_Node left = info.FindNodeByPath("VRLeft"),
                    top = info.FindNodeByPath("VRTop"),
                    right = info.FindNodeByPath("VRRight"),
                    bottom = info.FindNodeByPath("VRBottom"),
                    bgm = info.FindNodeByPath("bgm"),
                    link = info.FindNodeByPath("link"),
                    mapMark = info.FindNodeByPath("mapMark");

                // load mapSize
                if (!(left == null || top == null || right == null || bottom == null))
                {
                    int l = left.GetValue<int>();
                    int t = top.GetValue<int>();
                    int r = right.GetValue<int>();
                    int b = bottom.GetValue<int>();
                    this.renderEnv.Camera.WorldRect = new Rectangle(l, t, r - l, b - t);
                }

                // load sound
                if (bgm != null)
                {
                    string bgmPath = bgm.GetValueEx<string>(null);
                    if (bgmPath != this.bgmName)
                    {
                        this.bgmName = bgmPath;
                        LoadBgm();
                    }
                }

                // load minimap
                this.mapMark = mapMark.GetValueEx<string>(null);


                // load link
                int _link = link.GetValueEx<int>(-1);
                if (_link > -1)
                {
                    Wz_Image linkMapImg = FindMapByID(_link);
                    if (linkMapImg != null)
                    {
                        this.mapImg = linkMapImg;
                    }
                }
            }

            /* 暂时不用小地图方式来判定了
            Wz_Node minimap = mapImg.Node.FindChildByPath("miniMap");
            if (minimap != null && this.renderEnv.Camera.WorldRect.IsEmpty)
            {
                Wz_Node width = minimap.FindChildByPath("width"),
                  height = minimap.FindChildByPath("height"),
                  centerX = minimap.FindChildByPath("centerX"),
                  centerY = minimap.FindChildByPath("centerY");
                if (width != null && height != null && centerX != null && centerY != null)
                {
                    int w = width.GetValue<int>();
                    int h = height.GetValue<int>();
                    int ox = centerX.GetValue<int>();
                    int oy = centerY.GetValue<int>();
                    this.renderEnv.Camera.WorldRect = new Rectangle(-ox + 50, -oy + 50, w - 100, h - 100);
                    return;
                }
            }
            */
        }

        private Wz_Image FindMapByID(int mapID)
        {
            string fullPath = string.Format(@"Map\Map\Map{0}\{1:D9}.img", (mapID / 100000000), mapID);
            Wz_Node mapImgNode = PluginManager.FindWz(fullPath);
            Wz_Image mapImg;
            if (mapImgNode != null
                && (mapImg = mapImgNode.GetValueEx<Wz_Image>(null)) != null
                && mapImg.TryExtract())
            {
                return mapImg;
            }
            return null;
        }

        private void LoadBgm()
        {
            Wz_Node soundWz = PluginManager.FindWz(Wz_Type.Sound);

            this.bgmFile = null;
            this.bgmStream = IntPtr.Zero;

            if (soundWz == null || this.bgmName == null)
                return;

            string[] path = this.bgmName.Split('/');
            if (path.Length <= 1)
                return;
            path[0] += ".img";

            Wz_Node soundNode = soundWz.FindNodeByPath(true, true, path);
            Wz_Sound _sound = soundNode.GetValueEx<Wz_Sound>(null);
            if (_sound != null)
            {
                byte[] newBgmFile = _sound.ExtractSound();
                if (newBgmFile != null)
                {
                    int newBgmStream = Bass.BASS_StreamCreateFile(
                        Marshal.UnsafeAddrOfPinnedArrayElement(newBgmFile, 0),
                        0,
                        newBgmFile.LongLength,
                        BASSFlag.BASS_DEFAULT
                        );

                    if (newBgmStream != 0)
                    {
                        this.bgmFile = newBgmFile;
                        this.bgmStream = new IntPtr(newBgmStream);
                        Bass.BASS_ChannelFlags(newBgmStream, BASSFlag.BASS_SAMPLE_LOOP, BASSFlag.BASS_SAMPLE_LOOP);
                    }
                }
            }
        }

        private void LoadMinimap()
        {
            if (this.uiMinimap != null && !this.uiMinimap.ResourceLoaded)
            {
                this.uiMinimap.LoadResource(this.GraphicsDevice);
            }

            MiniMap miniMap = new MiniMap();

            Wz_Node miniMapNode = mapImg.Node.FindNodeByPath("miniMap");

            //读取小地图标记
            if (mapMark != null)
            {
                Wz_Node markNode = PluginManager.FindWz("Map\\MapHelper.img\\mark\\" + mapMark);
                Wz_Png markImage = markNode.GetValueEx<Wz_Png>(null);
                if (markImage != null)
                {
                    miniMap.MapMark = TextureLoader.PngToTexture(this.GraphicsDevice, markImage);
                }
            }

            //读取小地图
            if (miniMapNode != null)
            {
                Wz_Node canvas = miniMapNode.FindNodeByPath("canvas"),
                    width = miniMapNode.FindNodeByPath("width"),
                    height = miniMapNode.FindNodeByPath("height"),
                    centerX = miniMapNode.FindNodeByPath("centerX"),
                    centerY = miniMapNode.FindNodeByPath("centerY"),
                    mag = miniMapNode.FindNodeByPath("mag");

                Wz_Png _canvas = canvas.GetValueEx<Wz_Png>(null);
                if (_canvas != null)
                {
                    miniMap.Canvas = TextureLoader.PngToTexture(this.GraphicsDevice, _canvas);
                }
                miniMap.Width = width.GetValueEx<int>(0);
                miniMap.Height = height.GetValueEx<int>(0);
                miniMap.CenterX = centerX.GetValueEx<int>(0);
                miniMap.CenterY = centerY.GetValueEx<int>(0);
                miniMap.Mag = mag.GetValueEx<int>(0);
            }
            else
            {
                this.miniMap = null;
                return;
            }

            //读取传送门
            this.uiMinimap.Portals.Clear();
            this.uiMinimap.Transports.Clear();
            foreach (var patch in this.renderingList)
            {
                if (patch.ObjectType == RenderObjectType.Portal)
                {
                    PortalPatch portal = patch as PortalPatch;
                    switch (portal.PortalType)
                    {
                        case 2: //一般传送门
                        case 7: //指令传送门
                            this.uiMinimap.Portals.Add(portal.Position);
                            break;

                        case 10: //地图内传送
                            this.uiMinimap.Transports.Add(portal.Position);
                            break;
                    }
                    
                }
            }
            this.miniMap = miniMap;
        }

        private void LoadObjTile()
        {
            Dictionary<string, RenderFrame> loadedObjRes = new Dictionary<string, RenderFrame>();
            Dictionary<string, RenderFrame> loadedTileRes = new Dictionary<string, RenderFrame>();
            Dictionary<string, RenderFrame[]> loadedFrames = new Dictionary<string, RenderFrame[]>();

            for (int layer = 0; ; layer++)
            {
                Wz_Node objTileNode = mapImg.Node.FindNodeByPath(layer.ToString());
                if (objTileNode == null)
                {
                    break;
                }

                Wz_Node objLstNode = objTileNode.FindNodeByPath("obj");
                if (objLstNode != null && objLstNode.Nodes.Count > 0)
                {
                    string[] path = new string[5];
                    int loadIndex = 0;
                    foreach (Wz_Node node in objLstNode.Nodes)
                    {
                        Wz_Node oS = node.FindNodeByPath("oS"),
                            l0 = node.FindNodeByPath("l0"),
                            l1 = node.FindNodeByPath("l1"),
                            l2 = node.FindNodeByPath("l2"),
                            x = node.FindNodeByPath("x"),
                            y = node.FindNodeByPath("y"),
                            z = node.FindNodeByPath("z"),
                            f = node.FindNodeByPath("f"),
                            zM = node.FindNodeByPath("zM"),
                            tags = node.FindNodeByPath("tags");
                        
                        if (oS != null && l0 != null && l1 != null && l2 != null)
                        {
                            path[0] = "Obj";
                            path[1] = oS.GetValue<string>() + ".img";
                            path[2] = l0.GetValue<string>();
                            path[3] = l1.GetValue<string>();
                            path[4] = l2.GetValue<string>();
                            string key = string.Join("\\", path);

                            RenderFrame[] frames;
                            Wz_Node objResNode = PluginManager.FindWz("Map\\" + key);
                            if (objResNode == null)
                                continue;

                            if (!loadedFrames.TryGetValue(key, out frames))
                            {
                                frames = LoadFrames(objResNode, loadedObjRes);
                                loadedFrames[key] = frames;
                            }

                            RenderPatch patch = new ObjTilePatch();
                            patch.ObjectType = RenderObjectType.Obj;
                            patch.Position = new Vector2(x.GetValueEx<int>(0), y.GetValueEx<int>(0));
                            patch.Flip = f.GetValueEx<int>(0) != 0;
                            patch.ZIndex[0] = (int)RenderObjectType.Obj;
                            patch.ZIndex[1] = layer;
                            patch.ZIndex[2] = (int)patch.ObjectType;
                            patch.ZIndex[3] = z.GetValueEx<int>(0);
                            patch.ZIndex[4] = loadIndex;
                            patch.ZIndex[5] = zM.GetValueEx<int>(0); //not use for sort
                            patch.Frames = new RenderAnimate(frames);
                            patch.Frames.Repeat = objResNode.FindNodeByPath("repeat").GetValueEx<int>(0);

                            patch.Name = string.Format("obj_{0}_{1}", layer, node.Text);
                            //patch.RenderArgs.Visible = string.IsNullOrEmpty(tags.GetValueEx<string>(null));
                            this.renderingList.Add(patch);
                        }
                        loadIndex++;
                    }
                }

                Wz_Node tS = objTileNode.FindNodeByPath("info\\tS");
                string _tS = tS.GetValueEx<string>(null);

                Wz_Node tileLstNode = objTileNode.FindNodeByPath("tile");
                if (tileLstNode != null)
                {
                    string[] path = new string[4];
                    int loadIndex = 0;
                    foreach (Wz_Node node in tileLstNode.Nodes)
                    {
                        Wz_Node x = node.FindNodeByPath("x"),
                            y = node.FindNodeByPath("y"),
                            u = node.FindNodeByPath("u"),
                            no = node.FindNodeByPath("no"),
                            zM = node.FindNodeByPath("zM");
                        if (u != null && no != null)
                        {
                            path[0] = "Tile";
                            path[1] = _tS + ".img";
                            path[2] = u.GetValue<string>();
                            path[3] = no.GetValue<string>();

                            string key = string.Join("\\", path);

                            RenderFrame[] frames;
                            if (!loadedFrames.TryGetValue(key, out frames))
                            {
                                Wz_Node objResNode = PluginManager.FindWz("Map\\" + key);
                                if (objResNode == null)
                                    continue;
                                frames = LoadFrames(objResNode, loadedObjRes);
                                loadedFrames[key] = frames;
                            }

                            RenderPatch patch = new ObjTilePatch();
                            patch.ObjectType = RenderObjectType.Tile;
                            patch.Position = new Vector2(x.GetValueEx<int>(0), y.GetValueEx<int>(0));
                            patch.ZIndex[0] = (int)RenderObjectType.Obj;
                            patch.ZIndex[1] = layer;
                            patch.ZIndex[2] = (int)patch.ObjectType;
                            patch.ZIndex[3] = frames[0].Z;
                            patch.ZIndex[4] = loadIndex;
                            patch.ZIndex[5] = zM.GetValueEx<int>(0); //not use for order
                            patch.Frames = new RenderAnimate(frames);

                            patch.Name = string.Format("tile_{0}_{1}", layer, node.Text);
                            this.renderingList.Add(patch);
                        }
                        loadIndex++;
                    }
                }
            }
        }

        private void LoadBack()
        {
            Dictionary<string, RenderFrame> loadedBackRes = new Dictionary<string, RenderFrame>();
            Dictionary<string, RenderFrame[]> loadedFrames = new Dictionary<string, RenderFrame[]>();

            Wz_Node backLstNode = mapImg.Node.FindNodeByPath("back");
            if (backLstNode != null)
            {
                string[] path = new string[4];
                int loadIndex = 0;
                foreach (Wz_Node node in backLstNode.Nodes)
                {
                    Wz_Node x = node.FindNodeByPath("x"),
                        y = node.FindNodeByPath("y"),
                        bs = node.FindNodeByPath("bS"),
                        ani = node.FindNodeByPath("ani"),
                        no = node.FindNodeByPath("no"),
                        f = node.FindNodeByPath("f"),
                        front = node.FindNodeByPath("front"),
                        type = node.FindNodeByPath("type"),
                        cx = node.FindNodeByPath("cx"),
                        cy = node.FindNodeByPath("cy"),
                        rx = node.FindNodeByPath("rx"),
                        ry = node.FindNodeByPath("ry"),
                        a = node.FindNodeByPath("a"),
                        screenMode = node.FindNodeByPath("screenMode");

                    if (bs != null && no != null)
                    {
                        int _ani = ani.GetValueEx<int>(0);
                        
                        int _type = type.GetValueEx<int>(0);

                        path[0] = "Back";
                        path[1] = bs.GetValue<string>() + ".img";
                        switch (_ani)
                        {
                            case 0: path[2] = "back"; break;
                            case 1: path[2] = "ani"; break;
                            case 2: path[2] = "spine"; break;
                        }
                        path[3] = no.GetValue<string>();

                        string key = string.Join("\\", path);

                        RenderFrame[] frames;
                        if (!loadedFrames.TryGetValue(key, out frames))
                        {
                            Wz_Node objResNode = PluginManager.FindWz("Map\\" + key);
                            if (objResNode == null)
                                continue;
                            frames = LoadFrames(objResNode, loadedBackRes);
                            loadedFrames[key] = frames;
                        }

                        BackPatch patch = new BackPatch();
                        patch.ObjectType = front.GetValueEx<int>(0) != 0 ? RenderObjectType.Front : RenderObjectType.Back;
                        patch.Position = new Vector2(x.GetValueEx<int>(0), y.GetValueEx<int>(0));
                        patch.Cx = cx.GetValueEx<int>(0);
                        patch.Cy = cy.GetValueEx<int>(0);
                        patch.Rx = rx.GetValueEx<int>(0);
                        patch.Ry = ry.GetValueEx<int>(0);
                        patch.Frames = new RenderAnimate(frames);
                        patch.Flip = f.GetValueEx<int>(0) != 0;
                        patch.TileMode = GetBackTileMode(_type);
                        patch.Alpha = a.GetValueEx<int>(255);
                        patch.ScreenMode = screenMode.GetValueEx<int>(0);

                        patch.ZIndex[0] = (int)patch.ObjectType;
                        Int32.TryParse(node.Text, out patch.ZIndex[1]);

                        patch.Name = string.Format("back_{0}", node.Text);
                        this.renderingList.Add(patch);
                    }
                    loadIndex++;
                }
            }
        }

        private void LoadFoothold()
        {
            Wz_Node fhListNode = mapImg.Node.FindNodeByPath("foothold");
            if (fhListNode != null)
            {
                int _layer, _z, _fh;
                foreach (Wz_Node layerNode in fhListNode.Nodes)
                {
                    Int32.TryParse(layerNode.Text, out _layer);
                    foreach (Wz_Node zNode in layerNode.Nodes)
                    {
                        Int32.TryParse(zNode.Text, out _z);
                        foreach (Wz_Node fhNode in zNode.Nodes)
                        {
                            Int32.TryParse(fhNode.Text, out _fh);

                            Wz_Node x1 = fhNode.FindNodeByPath("x1"),
                                x2 = fhNode.FindNodeByPath("x2"),
                                y1 = fhNode.FindNodeByPath("y1"),
                                y2 = fhNode.FindNodeByPath("y2"),
                                prev = fhNode.FindNodeByPath("prev"),
                                next = fhNode.FindNodeByPath("next"),
                                piece = fhNode.FindNodeByPath("piece");

                            FootholdPatch patch = new FootholdPatch();
                            patch.ObjectType = RenderObjectType.Foothold;

                            patch.X1 = x1.GetValueEx<int>(0);
                            patch.X2 = x2.GetValueEx<int>(0);
                            patch.Y1 = y1.GetValueEx<int>(0);
                            patch.Y2 = y2.GetValueEx<int>(0);
                            patch.Prev = prev.GetValueEx<int>(0);
                            patch.Next = next.GetValueEx<int>(0);
                            patch.Piece = piece.GetValueEx<int>(0);

                            patch.ZIndex[0] = (int)patch.ObjectType;
                            patch.ZIndex[1] = _layer;
                            patch.ZIndex[2] = _z;
                            patch.ZIndex[3] = _fh;

                            patch.Name = string.Format("foothold_{0}", fhNode.Text);
                            this.renderingList.Add(patch);
                        }
                    }
                }
            }
        }

        private void LoadLadderRope()
        {
            Wz_Node ropeListNode = mapImg.Node.FindNodeByPath("ladderRope");
            if (ropeListNode != null)
            {
                int index;
                foreach (Wz_Node ropeNode in ropeListNode.Nodes)
                {
                    Int32.TryParse(ropeNode.Text, out index);

                    Wz_Node x = ropeNode.FindNodeByPath("x"),
                        y1 = ropeNode.FindNodeByPath("y1"),
                        y2 = ropeNode.FindNodeByPath("y2"),
                        l = ropeNode.FindNodeByPath("l"),
                        uf = ropeNode.FindNodeByPath("uf"),
                        page = ropeNode.FindNodeByPath("page");

                    LadderRopePatch patch = new LadderRopePatch();
                    patch.ObjectType = RenderObjectType.LadderRope;
                    patch.X = x.GetValueEx<int>(0);
                    patch.Y1 = y1.GetValueEx<int>(0);
                    patch.Y2 = y2.GetValueEx<int>(0);
                    patch.L = l.GetValueEx<int>(0);
                    patch.Uf = uf.GetValueEx<int>(0);
                    patch.Page = page.GetValueEx<int>(0);

                    patch.ZIndex[0] = (int)patch.ObjectType;
                    patch.ZIndex[1] = 0;
                    patch.ZIndex[2] = 0;
                    patch.ZIndex[3] = index;

                    patch.Name = string.Format("ladderRope_{0}", ropeNode.Text);
                    this.renderingList.Add(patch);
                }
            }
        }

        private void LoadLife()
        {
            Dictionary<int, Dictionary<string, RenderFrame[]>> loadedMob = new Dictionary<int, Dictionary<string, RenderFrame[]>>();
            Dictionary<int, Dictionary<string, RenderFrame[]>> loadedNpc = new Dictionary<int, Dictionary<string, RenderFrame[]>>();

            Wz_Node lifeLstNode = mapImg.Node.FindNodeByPath("life");
            if (lifeLstNode != null)
            {
                string[] path = new string[1];
                int loadIndex = 0;
                foreach (Wz_Node node in lifeLstNode.Nodes)
                {
                    Wz_Node type = node.FindNodeByPath("type"),
                        id = node.FindNodeByPath("id"),
                        x = node.FindNodeByPath("x"),
                        cy = node.FindNodeByPath("cy"),
                        f = node.FindNodeByPath("f"),
                        fh = node.FindNodeByPath("fh"),
                        hide = node.FindNodeByPath("hide");

                    int _id = id.GetValueEx<int>(-1);
                    if (type != null && _id > -1)
                    {
                        LifePatch patch = new LifePatch();
                        patch.Position = new Vector2(x.GetValueEx<int>(0), cy.GetValueEx<int>(0));
                        patch.Flip = f.GetValueEx<int>(0) != 0;
                        patch.RenderArgs.Visible = hide.GetValueEx<int>(0) == 0;
                        patch.LifeID = _id;
                        patch.Foothold = fh.GetValueEx<int>(0);

                        path[0] = string.Format("{0:D7}.img", _id);
                        Dictionary<int, Dictionary<string, RenderFrame[]>> loadedLife;
                        string lifeWz;

                        switch (type.GetValueEx<string>(null))
                        {
                            case "m":
                                loadedLife = loadedMob;
                                lifeWz = "Mob\\";
                                patch.ObjectType = RenderObjectType.Mob;
                                break;

                            case "n":
                                loadedLife = loadedNpc;
                                lifeWz = "Npc\\";
                                patch.ObjectType = RenderObjectType.Npc;
                                break;

                            default:
                                continue;
                        }

                        if (lifeWz == null)
                            continue;

                        // load info
                        Wz_Node lifeImgNode = PluginManager.FindWz(lifeWz+ path[0]);
                        if (lifeImgNode == null)
                            continue;

                        LoadLifeInfo(lifeImgNode.FindNodeByPath("info"), patch.LifeInfo);

                        // load actions
                        Dictionary<string, RenderFrame[]> actions;
                        if (!loadedLife.TryGetValue(_id, out actions))
                        {
                            Wz_Node link = lifeImgNode.FindNodeByPath("info\\link");
                            int _link = link.GetValueEx<int>(-1);

                            if (_link >= 0)
                            {
                                if (!loadedLife.TryGetValue(_link, out actions))
                                {
                                    Wz_Node linkImgNode = PluginManager.FindWz(lifeWz + string.Format("{0:D7}.img", _link));
                                    if (linkImgNode == null)
                                        continue;
                                    actions = LoadLifeActions(linkImgNode);
                                    loadedLife[_link] = actions;
                                }
                            }
                            else
                            {
                                actions = LoadLifeActions(lifeImgNode);
                                loadedLife[_id] = actions;
                            }
                        }

                        foreach (var kv in actions)
                        {
                            patch.Actions.Add(kv.Key, new RenderAnimate(kv.Value));
                        }

                        patch.SwitchToDefaultAction();
                        patch.Name = string.Format("life_{0}", node.Text);

                        //查找已读取的foothold计算zindex
                        bool find = false;
                        foreach (RenderPatch fhPatch in renderingList)
                        {
                            if (fhPatch.ObjectType == RenderObjectType.Foothold
                                && fhPatch.ZIndex[3] == patch.Foothold)
                            {
                                patch.ZIndex[0] = (int)RenderObjectType.Obj;
                                patch.ZIndex[1] = fhPatch.ZIndex[1];
                                patch.ZIndex[2] = (int)patch.ObjectType;
                                find = true;
                                break;
                            }
                        }
                        if (!find)
                        {
                            patch.ZIndex[0] = (int)patch.ObjectType;
                        }
                        patch.ZIndex[4] = loadIndex;
                        this.renderingList.Add(patch);
                    }

                    loadIndex++;
                }
            }
        }

        private void LoadLifeInfo(Wz_Node infoNode, LifeInfo lifeInfo)
        {
            if (infoNode == null || lifeInfo == null)
                return;
            foreach (Wz_Node node in infoNode.Nodes)
            {
                switch (node.Text)
                {
                    case "level": lifeInfo.level = node.GetValueEx<int>(0); break;
                    case "maxHP": lifeInfo.maxHP = node.GetValueEx<int>(0); break;
                    case "maxMP": lifeInfo.maxMP = node.GetValueEx<int>(0); break;
                    case "speed": lifeInfo.speed = node.GetValueEx<int>(0); break;
                    case "PADamage": lifeInfo.PADamage = node.GetValueEx<int>(0); break;
                    case "PDDamage": lifeInfo.PDDamage = node.GetValueEx<int>(0); break;
                    case "PDRate": lifeInfo.PDRate = node.GetValueEx<int>(0); break;
                    case "MADamage": lifeInfo.MADamage = node.GetValueEx<int>(0); break;
                    case "MDDamage": lifeInfo.MDDamage = node.GetValueEx<int>(0); break;
                    case "MDRate": lifeInfo.MDRate = node.GetValueEx<int>(0); break;
                    case "acc": lifeInfo.acc = node.GetValueEx<int>(0); break;
                    case "eva": lifeInfo.eva = node.GetValueEx<int>(0); break;
                    case "pushed": lifeInfo.pushed = node.GetValueEx<int>(0); break;
                    case "exp": lifeInfo.exp = node.GetValueEx<int>(0); break;
                    case "undead": lifeInfo.undead = node.GetValueEx<int>(0) != 0; break;
                    case "boss": lifeInfo.boss = node.GetValueEx<int>(0) != 0; break;
                    case "elemAttr":
                        string elem = node.GetValueEx<string>(string.Empty);
                        for (int i = 0; i < elem.Length; i += 2)
                        {
                            LifeInfo.ElemResistance resist = (LifeInfo.ElemResistance)(elem[i + 1] - 48);
                            switch (elem[i])
                            {
                                case 'I': lifeInfo.elemAttr.I = resist; break;
                                case 'L': lifeInfo.elemAttr.L = resist; break;
                                case 'F': lifeInfo.elemAttr.F = resist; break;
                                case 'S': lifeInfo.elemAttr.S = resist; break;
                                case 'H': lifeInfo.elemAttr.H = resist; break;
                                case 'D': lifeInfo.elemAttr.D = resist; break;
                                case 'P': lifeInfo.elemAttr.P = resist; break;
                            }
                        }
                        break;
                }
            }
        }

        private void LoadPortal()
        {
            Dictionary<string, RenderFrame> loadedRes = new Dictionary<string, RenderFrame>();
            Dictionary<string, RenderFrame[]> loadedFrames = new Dictionary<string, RenderFrame[]>();

            Wz_Node portalImageNode = PluginManager.FindWz("Map\\MapHelper.img\\portal");

            if (portalImageNode == null)
                return;

            Wz_Node editorNode = portalImageNode.FindNodeByPath("editor");

            if (editorNode == null)
                return;

            string[] ptList = new [] { "sp", "pi", "pv", "pc", "pg", "tp", "ps", "pgi", "psi", "pcs", "ph", "psh", "pcj", "pci", "pcig", "pshg" };

            Wz_Node portalNode = mapImg.Node.FindNodeByPath("portal");
            if (portalNode != null)
            {
                string[] path = new string[4];

                foreach (Wz_Node node in portalNode.Nodes)
                {
                    Wz_Node pn = node.FindNodeByPath("pn"),
                       pt = node.FindNodeByPath("pt"),
                       x = node.FindNodeByPath("x"),
                       y = node.FindNodeByPath("y"),
                       tm = node.FindNodeByPath("tm"),
                       tn = node.FindNodeByPath("tn"),
                       script = node.FindNodeByPath("script"),
                       image = node.FindNodeByPath("image");

                    if (pt != null)
                    {
                        PortalPatch patch = new PortalPatch();
                        patch.ObjectType = RenderObjectType.Portal;
                        patch.PortalName = pn.GetValueEx<string>(null);
                        patch.ToMap = tm.GetValueEx<int>(0);
                        patch.ToName = tn.GetValueEx<string>(null);
                        patch.Script = script.GetValueEx<string>(null);
                        patch.Position = new Vector2(x.GetValueEx<int>(0), y.GetValueEx<int>(0));
                        int _pt = pt.GetValueEx<int>(-1);
                        patch.PortalType = _pt;
                        if (_pt < 0 || _pt >= ptList.Length)
                            continue;

                        //读取aniEditor
                        path[0] = "editor";
                        path[1] = ptList[_pt];
                        string key = string.Join("\\", path, 0, 2);

                        RenderFrame[] frames;
                        Wz_Node resNode;
                        if (!loadedFrames.TryGetValue(key, out frames))
                        {
                            resNode = portalImageNode.FindNodeByPath(false, path[0], path[1]);
                            if (resNode == null)
                                continue;
                            frames = LoadFrames(resNode, loadedRes);
                            loadedFrames[key] = frames;
                        }
                        patch.AniEditor = new RenderAnimate(frames);

                        //读取aniPortal
                        switch (_pt)
                        {
                            case 7: _pt = 2; break;
                        }

                        path[0] = "game";
                        path[1] = ptList[_pt];

                        resNode = portalImageNode.FindNodeByPath(false, path[0], path[1]);
                        if (resNode != null)
                        {
                            if (resNode.FindNodeByPath("default") != null) //寻找image对应的节点  否则本身作为根节点
                            {
                                int _image = image.GetValueEx<int>(0);
                                if (_image == 0)
                                    path[2] = "default";
                                else
                                    path[2] = _image.ToString();

                                if ((resNode = resNode.FindNodeByPath(path[2])) == null)
                                    continue;

                            }
                            string[] animeNames = new string[] { "portalStart", "portalContinue", "portalExit" };
                            bool existsAnimes = false;
                            foreach (string animeName in animeNames)
                            {
                                if (resNode.FindNodeByPath(animeName) != null)
                                {
                                    existsAnimes = true;
                                    break;
                                }
                            }

                            if (existsAnimes) //三段式读取动画
                            {
                                RenderFrame[][] animeFrames = new RenderFrame[animeNames.Length][];
                                for (int i = 0; i < animeNames.Length; i++)
                                {
                                    path[3] = animeNames[i];
                                    key = string.Join("\\", path);

                                    if (!loadedFrames.TryGetValue(key, out frames))
                                    {
                                        Wz_Node aniNode = resNode.FindNodeByPath(animeNames[i]);
                                        if (aniNode == null)
                                            continue;
                                        frames = LoadFrames(aniNode, loadedRes);
                                        loadedFrames[key] = frames;
                                    }

                                    animeFrames[i] = frames;
                                }
                                patch.AniStart = new RenderAnimate(animeFrames[0]);
                                patch.AniContinue = new RenderAnimate(animeFrames[1]);
                                patch.AniExit = new RenderAnimate(animeFrames[2]);
                            }
                            else //只读取动画作为continue
                            {
                                key = string.Join("\\", path, 0, 2);
                                if (!loadedFrames.TryGetValue(key, out frames))
                                {
                                    frames = LoadFrames(resNode, loadedRes);
                                    loadedFrames[key] = frames;
                                }
                                patch.AniContinue = new RenderAnimate(frames);
                            }
                        }


                        patch.ZIndex[0] = (int)patch.ObjectType;
                        Int32.TryParse(node.Text, out patch.ZIndex[3]);

                        patch.Name = string.Format("portal_{0}", node.Text);
                        this.renderingList.Add(patch);
                    }
                }
            }
        }

        private void LoadReactor()
        {
            Wz_Node reactorWz = PluginManager.FindWz(Wz_Type.Reactor);
            Dictionary<int, List<RenderFrame[]>> loadedFrames = new Dictionary<int, List<RenderFrame[]>>();

            if (reactorWz == null)
                return;

            Wz_Node reactorNode = mapImg.Node.FindNodeByPath("reactor");
            if (reactorNode != null)
            {
                string[] path = new string[1];

                foreach (Wz_Node node in reactorNode.Nodes)
                {
                    Wz_Node id = node.FindNodeByPath("id"),
                       x = node.FindNodeByPath("x"),
                       y = node.FindNodeByPath("y"),
                       f = node.FindNodeByPath("f"),
                       reactorTime = node.FindNodeByPath("reactorTime"),
                       name = node.FindNodeByPath("name");

                    int _id = id.GetValueEx<int>(-1);
                    if (_id > -1)
                    {
                        ReactorPatch patch = new ReactorPatch();
                        patch.ReactorID = _id;
                        patch.Position = new Vector2(x.GetValueEx<int>(0), y.GetValueEx<int>(0));
                        patch.ReactorName = name.GetValueEx<string>(null);
                        patch.Flip = (f.GetValueEx<int>(0) != 0);
                        patch.ObjectType = RenderObjectType.Reactor;

                        path[0] = string.Format("{0:D7}.img", _id);

                        Wz_Node reactorImgNode = reactorWz.FindNodeByPath(path[0], true);
                        if (reactorImgNode == null)
                            continue;

                        List<RenderFrame[]> stages;
                        if (!loadedFrames.TryGetValue(_id, out stages))
                        {
                            Wz_Node link = reactorImgNode.FindNodeByPath("info\\link");
                            int _link = link.GetValueEx<int>(-1);

                            if (_link >= 0)
                            {
                                if (!loadedFrames.TryGetValue(_link, out stages))
                                {
                                    Wz_Node linkImgNode = reactorWz.FindNodeByPath(string.Format("{0:D7}.img", _link), true);
                                    if (linkImgNode == null)
                                        continue;
                                    stages = LoadReactorStages(linkImgNode);
                                    loadedFrames[_link] = stages;
                                }
                            }
                            else
                            {
                                stages = LoadReactorStages(reactorImgNode);
                                loadedFrames[_link] = stages;
                            }
                        }

                        foreach (var stage in stages)
                        {
                            patch.Stages.Add(new RenderAnimate(stage));
                        }

                        int _time = reactorTime.GetValueEx<int>(0);
                        if (_time < 0)
                            _time = 0;
                        if (_time < patch.Stages.Count)
                            patch.Frames = patch.Stages[_time];

                        patch.ZIndex[0] = (int)patch.ObjectType;
                        Int32.TryParse(node.Text, out patch.ZIndex[3]);
                        patch.Name = string.Format("reactor_{0}", node.Text);

                        this.renderingList.Add(patch);
                    }
                }
            }
        }

        private void LoadTooltip()
        {
            Wz_Node tooltipNode = mapImg.Node.FindNodeByPath("ToolTip");
        }

        private void CalcMapSize()
        {
            if (this.renderEnv.Camera.WorldRect.IsEmpty)
            {
                Rectangle worldRect = Rectangle.Empty;
                foreach (RenderPatch patch in this.renderingList)
                {
                    Rectangle patchRect = Rectangle.Empty;
                    switch (patch.ObjectType)
                    {
                        case RenderObjectType.Foothold:
                            FootholdPatch fh = patch as FootholdPatch;
                            patchRect = new Rectangle(fh.X1, fh.Y1, fh.X2 - fh.X1, fh.Y2 - fh.Y1);
                            break;
                        case RenderObjectType.LadderRope:
                            LadderRopePatch rope = patch as LadderRopePatch;
                            patchRect = new Rectangle(rope.X, rope.Y1, 1, rope.Y2 - rope.Y1);
                            break;

                        case RenderObjectType.Obj:
                        case RenderObjectType.Tile:
                            ObjTilePatch objTile = patch as ObjTilePatch;
                            foreach (RenderFrame f in objTile.Frames)
                            {
                                if (f.Texture != null && !f.Texture.IsDisposed)
                                {
                                    Rectangle rect = this.renderEnv.Camera.MeasureDrawingRect(f.Texture.Width, f.Texture.Height, objTile.Position, f.Origin, objTile.Flip);

                                    if (patchRect.IsEmpty)
                                    {
                                        patchRect = rect;
                                    }
                                    else
                                    {
                                        Rectangle.Union(ref patchRect, ref rect, out patchRect);
                                    }
                                }
                            }
                            break;
                    }

                    if (!patchRect.IsEmpty)
                    {
                        if (worldRect.IsEmpty)
                        {
                            worldRect = patchRect;
                        }
                        else
                        {
                            Rectangle.Union(ref worldRect, ref patchRect, out worldRect);
                        }
                    }
                }
                worldRect.Y -= 400;
                worldRect.Height += 400;
                this.renderEnv.Camera.WorldRect = worldRect;
            }
        }

        private Dictionary<string, RenderFrame[]> LoadLifeActions(Wz_Node lifeNode)
        {
            Dictionary<string, RenderFrame[]> actions = new Dictionary<string, RenderFrame[]>();
            Dictionary<string, RenderFrame> loadedRes = new Dictionary<string, RenderFrame>();

            foreach (Wz_Node actionNode in lifeNode.Nodes)
            {
                if (actionNode.Text != "info")
                {
                    RenderFrame[] frames = LoadFrames(actionNode, loadedRes);
                    if (frames.Length > 0)
                    {
                        actions[actionNode.Text] = frames;
                    }
                }
            }
            return actions;
        }

        private List<RenderFrame[]> LoadReactorStages(Wz_Node reactorNode)
        {
            List<RenderFrame[]> stages = new List<RenderFrame[]>();
            Dictionary<string, RenderFrame> loadedRes = new Dictionary<string, RenderFrame>();

            for (int i = 0; ; i++)
            {
                Wz_Node stageNode = reactorNode.FindNodeByPath(i.ToString());
                if (stageNode == null)
                    break;
                stages.Add(LoadFrames(stageNode, loadedRes));
            }
            return stages;
        }

        private RenderFrame[] LoadFrames(Wz_Node resNode, Dictionary<string, RenderFrame> loadedRes)
        {
            if (resNode.Value is Wz_Png)
            {
                RenderFrame frame;
                if (!loadedRes.TryGetValue(resNode.FullPath, out frame))
                {
                    frame = this.texLoader.CreateFrame(resNode);
                    loadedRes[resNode.FullPath] = frame;
                }
                return new RenderFrame[1] { frame };
            }

            List<RenderFrame> frames = new List<RenderFrame>();
            Wz_Uol uol;

            for (int i = 0; ; i++)
            {
                Wz_Node frameNode = resNode.FindNodeByPath(i.ToString());
                if (frameNode == null)
                    break;
                while ((uol = frameNode.Value as Wz_Uol) != null)
                {
                    frameNode = uol.HandleUol(frameNode);
                }
                frameNode = frameNode.GetLinkedSourceNode(PluginManager.FindWz);
                RenderFrame frame;
                if (!loadedRes.TryGetValue(frameNode.FullPath, out frame))
                {
                    frame = this.texLoader.CreateFrame(frameNode);
                    loadedRes[frameNode.FullPath] = frame;
                }
                frames.Add(frame);
            }

            return frames.ToArray();
        }

        private TileMode GetBackTileMode(int type)
        {
            switch (type)
            {
                case 0: return TileMode.None;
                case 1: return TileMode.Horizontal;
                case 2: return TileMode.Vertical;
                case 3: return TileMode.BothTile;
                case 4: return TileMode.Horizontal | TileMode.ScrollHorizontial;
                case 5: return TileMode.Vertical | TileMode.ScrollVertical;
                case 6: return TileMode.BothTile | TileMode.ScrollHorizontial;
                case 7: return TileMode.BothTile | TileMode.ScrollVertical;
                default: return TileMode.None;
            }
        }

        private enum LoadState
        {
            NotLoad,
            Loading,
            LoadSuccessed,
            Entering,
            Rendering,
            Exiting,
            LoadFailed
        }
    }
}
#endif