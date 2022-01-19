﻿using System;
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

            //添加视图状态
            this.viewData = new MapViewData()
            {
                MapID = mapData?.ID ?? -1,
                Portal = "sp"
            };

            if (this.mapData != null)
            {
                yield return cm.Yield(OnSceneEnter());
            }
            else
            {
                //添加提示语
                this.ui.ChatBox.AppendTextSystem("MapRender加载失败，没有地图数据。");
                this.opacity = 1;
                yield return cm.Yield(OnSceneRunning());
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
            var mapData = new MapData(this.Services.GetService<IRandom>());
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

        private Music LoadBgm(MapData mapData, string multiBgmText = null)
        {
            if (!string.IsNullOrEmpty(mapData.Bgm))
            {
                var path = new List<string>() { "Sound" };
                path.AddRange(mapData.Bgm.Split('/'));
                path[1] += ".img";
                var bgmNode = PluginManager.FindWz(string.Join("\\", path));
                if (bgmNode != null)
                {
                    if (bgmNode.Value == null)
                    {
                        bgmNode = multiBgmText == null ? bgmNode.Nodes.FirstOrDefault(n => n.Value is Wz_Sound || n.Value is Wz_Uol) : bgmNode.Nodes[multiBgmText];
                        if (bgmNode == null)
                        {
                            return null;
                        }
                    }

                    while (bgmNode.Value is Wz_Uol uol)
                    {
                        bgmNode = uol.HandleUol(bgmNode);
                    }
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
            foreach (var portal in mapData.Scene.Portals)
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

                    case 8:
                        if (portal.ShownAtMinimap)
                        {
                            this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                            {
                                IconType = UIMinimap2.IconType.HiddenPortal,
                                Tooltip = portal.Tooltip,
                                WorldPosition = new EmptyKeys.UserInterface.PointF(portal.X, portal.Y)
                            });
                        }
                        break;

                    case 10:
                        this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                        {
                            IconType = portal.ToMap == mapData.ID ? UIMinimap2.IconType.ArrowUp : UIMinimap2.IconType.HiddenPortal,
                            Tooltip = portal.Tooltip,
                            WorldPosition = new EmptyKeys.UserInterface.PointF(portal.X, portal.Y)
                        });
                        break;

                    case 11:
                        if (portal.ShownAtMinimap)
                        {
                            this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                            {
                                IconType = UIMinimap2.IconType.HiddenPortal,
                                Tooltip = portal.Tooltip,
                                WorldPosition = new EmptyKeys.UserInterface.PointF(portal.X, portal.Y)
                            });
                        }
                        break;
                }
            }

            foreach (var npc in mapData.Scene.Npcs)
            {
                object tooltip = null;
                var npcNode = PluginManager.FindWz(string.Format("Npc/{0:D7}.img/info", npc.ID));
                if ((npcNode?.Nodes["hide"].GetValueEx(0) ?? 0) != 0 || (npcNode?.Nodes["hideName"].GetValueEx(0) ?? 0) != 0)
                {
                    continue;
                }
                if (StringLinker.StringNpc.TryGetValue(npc.ID, out sr))
                {
                    if (sr.Desc != null)
                    {
                        tooltip = new KeyValuePair<string, string>(sr.Name, sr.Desc);
                    }
                    else
                    {
                        tooltip = sr.Name;
                    }
                }
                if (npc.ID == 9010022)
                {
                    this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                    {
                        IconType = UIMinimap2.IconType.Transport,
                        Tooltip = tooltip,
                        WorldPosition = new EmptyKeys.UserInterface.PointF(npc.X, npc.Y)
                    });
                }
                else if ((npcNode?.Nodes["shop"].GetValueEx(0) ?? 0) != 0)
                {
                    this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                    {
                        IconType = UIMinimap2.IconType.Shop,
                        Tooltip = tooltip,
                        WorldPosition = new EmptyKeys.UserInterface.PointF(npc.X, npc.Y)
                    });
                }
                else if (npc.ID / 10000 == 900 || npc.ID / 10000 == 901)
                {
                    this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                    {
                        IconType = UIMinimap2.IconType.EventNpc,
                        Tooltip = tooltip,
                        WorldPosition = new EmptyKeys.UserInterface.PointF(npc.X, npc.Y)
                    });
                }
                else if ((npcNode?.Nodes["trunkPut"].GetValueEx(0) ?? 0) != 0)
                {
                    this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                    {
                        IconType = UIMinimap2.IconType.Trunk,
                        Tooltip = tooltip,
                        WorldPosition = new EmptyKeys.UserInterface.PointF(npc.X, npc.Y)
                    });
                }
                else
                {
                    this.ui.Minimap.Icons.Add(new UIMinimap2.MapIcon()
                    {
                        IconType = UIMinimap2.IconType.Npc,
                        Tooltip = tooltip,
                        WorldPosition = new EmptyKeys.UserInterface.PointF(npc.X, npc.Y)
                    });
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
                if (this.viewData.IsMoveBack 
                    && this.viewData.ToMapID == this.viewHistory.Last?.Value?.MapID)
                {
                    var last = this.viewHistory.Last.Value;
                    this.viewHistory.RemoveLast();
                    var toViewData = new MapViewData()
                    {
                        MapID = last.MapID,
                        Portal = last.Portal ?? "sp"
                    };
                    this.viewData = toViewData;
                }
                else
                {
                    viewHistory.AddLast(this.viewData);
                    var toViewData = new MapViewData()
                    {
                        MapID = this.viewData.ToMapID.Value,
                        Portal = this.viewData.ToPortal ?? "sp"
                    };
                    this.viewData = toViewData;
                }
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
                if (this.viewData?.ToMapID != null)
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
            if (mapData != null)
            {
                UpdateAllItems(mapData.Scene, gameTime.ElapsedGameTime);
            }
            //更新tooltip
            UpdateTooltip();
        }

        private void MoveToPortal(int? toMap, string pName, string fromPName = null, bool isBack = false)
        {
            if (toMap != null && toMap != this.mapData?.ID) //跳转地图
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
                        viewData.IsMoveBack = isBack;
                    }
                }
                else
                {
                    this.ui.ChatBox.AppendTextSystem($"没有找到ID:{toMap.Value}的地图。");
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

        private void MoveToLastMap()
        {
            if (viewHistory.Count > 0)
            {
                var last = viewHistory.Last.Value;
                if (last.MapID > -1)
                {
                    MoveToPortal(last.MapID, last.Portal, null, true);
                }
            }
        }

        class MapViewData
        {
            public int MapID { get; set; }
            public string Portal { get; set; }
            public int? ToMapID { get; set; }
            public string ToPortal { get; set; }
            public bool IsMoveBack { get; set; }
        }
    }
}
