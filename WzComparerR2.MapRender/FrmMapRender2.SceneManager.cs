using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WzComparerR2.WzLib;
using WzComparerR2.PluginBase;
using WzComparerR2.Common;
using WzComparerR2.MapRender.Patches2;
using WzComparerR2.MapRender.UI;
using Microsoft.Xna.Framework;
using IE = System.Collections.IEnumerator;

namespace WzComparerR2.MapRender
{
    public partial class FrmMapRender2
    {
        MapViewData viewData;
        LinkedList<MapViewData> viewHistory;

        private IE OnStart()
        {
            //初始化
            viewHistory = new LinkedList<MapViewData>();

            //开始加载地图
            yield return new WaitTaskCompletedCoroutine(LoadMap());
            if (this.mapData != null)
            {
                //添加视图状态
                viewData = new MapViewData()
                {
                    MapID = mapData?.ID ?? -1,
                    Portal = "sp"
                };
                yield return cm.Yield(OnSceneEnter());
            }
        }

        private async Task LoadMap()
        {
            if (this.mapImg == null)
            {
                return;
            }

            //开始加载
            this.resLoader.ClearAnimationCache();
            this.resLoader.BeginCounting();

            //加载地图数据
            var mapData = new MapData();
            mapData.Load(mapImg.Node, resLoader);

            //处理bgm
            Music newBgm = LoadBgm(mapData);
            Task bgmTask = null;
            bool willSwitchBgm = this.mapData?.Bgm != mapData.Bgm;
            if (willSwitchBgm && this.bgm != null) //准备切换
            {
                bgmTask = FadeOut(this.bgm, 1000);
            }

            //加载资源
            mapData.PreloadResource(resLoader);

            //准备UI和初始化
            this.AfterLoadMap(mapData);

            if (bgmTask != null)
            {
                await bgmTask;
            }

            //回收资源
            this.resLoader.EndCounting();
            this.resLoader.Recycle();

            //准备场景和bgm
            this.mapData = mapData;
            this.bgm = newBgm;
            if (willSwitchBgm && this.bgm != null)
            {
                bgmTask = FadeIn(this.bgm, 1000);
            }
        }

        private async Task FadeOut(Music music, int ms)
        {
            float vol = music.Volume;
            for (int i = 0; i < ms; i += 30)
            {
                music.Volume = vol * (ms - i) / ms;
                await Task.Delay(30);
            }
            music.Volume = 0f;
            music.Stop();
        }

        private async Task FadeIn(Music music, int ms)
        {
            music.Play();
            float vol = music.Volume;
            for (int i = 0; i < ms; i += 30)
            {
                music.Volume = vol + (1 - vol) * i / ms;
                await Task.Delay(30);
            }
            music.Volume = 1f;
        }

        private Music LoadBgm(MapData mapData)
        {
            if (!string.IsNullOrEmpty(mapData.Bgm))
            {
                var path = new List<string>() { "Sound" };
                path.AddRange(mapData.Bgm.Split('/'));
                path[1] += ".img";
                var bgmNode = PluginManager.FindWz(string.Join("\\", path));
                if (bgmNode != null)
                {
                    var bgm = resLoader.Load<Music>(bgmNode);
                    bgm.IsLoop = true;
                    return bgm;
                }
            }
            return null;
        }

        private void AfterLoadMap(MapData mapData)
        {
            //同步可视化状态
            foreach(var portal in mapData.Scene.Portals)
            {
                portal.View.IsEditorMode = this.patchVisibility.PortalInEditMode;
            }
           
            //同步UI
            this.renderEnv.Camera.WorldRect = mapData.VRect;

            StringResult sr;
            if (mapData.ID != null && this.StringLinker != null
                && StringLinker.StringMap.TryGetValue(mapData.ID.Value, out sr))
            {
                this.ui.Minimap.StreetName = sr["streetName"];
                this.ui.Minimap.MapName = sr["mapName"];
            }
            else
            {
                this.ui.Minimap.StreetName = null;
                this.ui.Minimap.MapName = null;
            }

            if (mapData.MiniMap.MapMark != null)
            {
                this.ui.Minimap.MapMark = engine.Renderer.CreateTexture(mapData.MiniMap.MapMark);
            }
            else
            {
                this.ui.Minimap.MapMark = null;
            }

            if (mapData.MiniMap.Canvas != null)
            {
                this.ui.Minimap.MinimapCanvas = engine.Renderer.CreateTexture(mapData.MiniMap.Canvas);
            }
            else
            {
                this.ui.Minimap.MinimapCanvas = null;
            }

            this.ui.Minimap.Icons.Clear();
            foreach (var portal in mapData.Scene.Portals)
            {
                switch (portal.Type)
                {
                    case 2:
                    case 7:
                        object tooltip = portal.Tooltip;
                        if (tooltip == null && portal.ToMap != null && portal.ToMap != 999999999
                            && StringLinker.StringMap.TryGetValue(portal.ToMap.Value, out sr))
                        {
                            tooltip = sr["mapName"];
                        }
                        this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                        {
                            IconType = UIMinimap2.IconType.Portal,
                            Tooltip = tooltip,
                            WorldPosition = new EmptyKeys.UserInterface.PointF(portal.X, portal.Y)
                        });
                        break;

                    case 10:
                        this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                        {
                            IconType = UIMinimap2.IconType.Transport,
                            Tooltip = portal.Tooltip,
                            WorldPosition = new EmptyKeys.UserInterface.PointF(portal.X, portal.Y)
                        });
                        break;
                }
            }

            if (mapData.MiniMap.Width > 0 && mapData.MiniMap.Height > 0)
            {
                this.ui.Minimap.MapRegion = new Rectangle(-mapData.MiniMap.CenterX, -mapData.MiniMap.CenterY, mapData.MiniMap.Width, mapData.MiniMap.Height).ToRect();
            }
            else
            {
                this.ui.Minimap.MapRegion = mapData.VRect.ToRect();
            }

            this.ui.WorldMap.CurrentMapID = mapData?.ID;
        }

        private IE OnSceneEnter()
        {
            //初始化指向传送门
            if (!string.IsNullOrEmpty(viewData.Portal))
            {
                var portal = this.mapData.Scene.FindPortal(viewData.Portal);
                if (portal != null)
                {
                    this.renderEnv.Camera.Center = new Vector2(portal.X, portal.Y);
                }
                else
                {
                    this.renderEnv.Camera.Center = Vector2.Zero;
                }
                this.renderEnv.Camera.AdjustToWorldRect();
            }
            viewData.Portal = null;

            //场景渐入
            this.opacity = 0;
            double time = 500;
            for (double i = 0; i < time; i += cm.GameTime.ElapsedGameTime.TotalMilliseconds)
            {
                this.opacity = (float)(i / time);
                SceneUpdate();
                yield return null;
            }
            this.opacity = 1;
            yield return cm.Yield(OnSceneRunning());
        }

        private IE OnSceneExit()
        {
            //场景渐出
            this.opacity = 1;
            double time = 500;
            for (double i = 0; i < time; i += cm.GameTime.ElapsedGameTime.TotalMilliseconds)
            {
                this.opacity = 1f - (float)(i / time);
                yield return null;
            }
            this.opacity = 0;
            yield return null;
            yield return cm.Yield(OnSwitchMap());
        }

        private IE OnSwitchMap()
        {
            //记录历史
            if (this.viewData.MapID != this.viewData.ToMapID && this.viewData.ToMapID != null)
            {
                viewHistory.AddLast(this.viewData);
                var toViewData = new MapViewData()
                {
                    MapID = this.viewData.ToMapID.Value,
                    Portal = this.viewData.ToPortal ?? "sp"
                };
                this.viewData = toViewData;
            }
            else
            {
                this.viewData.ToMapID = null;
                this.viewData.Portal = this.viewData.ToPortal;
                this.viewData.ToPortal = null;
            }

            yield return new WaitTaskCompletedCoroutine(LoadMap());
            if (this.mapData != null)
            {
                yield return cm.Yield(OnSceneEnter());
            }
        }

        private IE OnSceneRunning()
        {
            while (true)
            {
                SceneUpdate();
                if (viewData.ToMapID != null)
                {
                    break;
                }
                yield return null;
            }
            yield return cm.Yield(OnSceneExit());
        }

        private IE OnCameraMoving(Point toPos, int ms)
        {
            Vector2 cameraFrom = this.renderEnv.Camera.Center;
            Vector2 cameraTo = toPos.ToVector2();
            for (double i = 0; i < ms; i += cm.GameTime.ElapsedGameTime.TotalMilliseconds)
            {
                var percent = (i / ms);
                this.renderEnv.Camera.Center = Vector2.Lerp(cameraFrom, cameraTo, (float)Math.Sqrt(percent));
                this.renderEnv.Camera.AdjustToWorldRect();
                yield return null;
            }
            this.renderEnv.Camera.Center = cameraTo;
            this.renderEnv.Camera.AdjustToWorldRect();
        }

        private void SceneUpdate()
        {
            var gameTime = cm.GameTime;
            var mapData = this.mapData;

            if (this.IsActive)
            {
                this.renderEnv.Input.Update(gameTime);
                this.ui.UpdateInput(gameTime.ElapsedGameTime.TotalMilliseconds);
            }

            //需要手动更新数据部分
            this.renderEnv.Camera.AdjustToWorldRect();
            {
                var rect = this.renderEnv.Camera.ClipRect;
                this.ui.Minimap.CameraViewPort = new EmptyKeys.UserInterface.Rect(rect.X, rect.Y, rect.Width, rect.Height);
            }
            //更新topbar
            UpdateTopBar();
            //更新ui
            this.ui.UpdateLayout(gameTime.ElapsedGameTime.TotalMilliseconds);
            //更新场景
            UpdateAllItems(mapData.Scene, gameTime.ElapsedGameTime);
            //更新tooltip
            UpdateTooltip();
        }

        private void MoveToPortal(int? toMap, string pName, string fromPName = null)
        {
            if (toMap != null && toMap != this.mapData.ID) //跳转地图
            {
                //寻找地图数据
                Wz_Node node;
                if (MapData.FindMapByID(toMap.Value, out node))
                {
                    Wz_Image img = node.GetNodeWzImage();
                    if (img != null)
                    {
                        this.mapImg = img;
                        viewData.ToMapID = toMap;
                        viewData.ToPortal = pName;
                        viewData.Portal = fromPName;
                    }
                }
            }
            else //当前地图
            {
                viewData.ToMapID = null;
                viewData.ToPortal = null;

                var portal = this.mapData.Scene.FindPortal(pName);
                if (portal != null)
                {
                    this.cm.StartCoroutine(OnCameraMoving(new Point(portal.X, portal.Y), 500));
                }
            }
        }

        class MapViewData
        {
            public int MapID { get; set; }
            public string Portal { get; set; }
            public int? ToMapID { get; set; }
            public string ToPortal { get; set; }
        }
    }
}
