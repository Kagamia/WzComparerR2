using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using WzComparerR2.Common;
using WzComparerR2.MapRender.UI;
using WzComparerR2.MapRender.Patches2;
using WzComparerR2.Animation;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WzComparerR2.Controls;
using WzComparerR2.MapRender.Effects;

namespace WzComparerR2.MapRender
{
    public partial class FrmMapRender2
    {
        private void UpdateAllItems(SceneNode node, TimeSpan elapsed)
        {
            var container = node as ContainerNode;
            if (container != null)  //暂时不考虑缩进z层递归合并  container下没有子节点
            {
                foreach (var item in container.Slots)
                {
                    if (item is BackItem)
                    {
                        var back = (BackItem)item;
                        (back.View.Animator as WzComparerR2.Controls.AnimationItem)?.Update(elapsed);
                        back.View.Time += (int)elapsed.TotalMilliseconds;
                    }
                    else if (item is ObjItem)
                    {
                        var _item = (ObjItem)item;
                        (_item.View.Animator as WzComparerR2.Controls.AnimationItem)?.Update(elapsed);
                        _item.View.Time += (int)elapsed.TotalMilliseconds;
                    }
                    else if (item is TileItem)
                    {
                        var tile = (TileItem)item;
                        (tile.View.Animator as WzComparerR2.Controls.AnimationItem)?.Update(elapsed);
                        tile.View.Time += (int)elapsed.TotalMilliseconds;
                    }
                    else if (item is LifeItem)
                    {
                        var life = (LifeItem)item;
                        var smAni = (life.View.Animator as StateMachineAnimator);
                        if (smAni != null)
                        {
                            if (smAni.GetCurrent() == null) //当前无动作
                            {
                                smAni.SetAnimation(smAni.Data.States[0]); //动作0
                            }
                            smAni.Update(elapsed);
                        }

                        life.View.Time += (int)elapsed.TotalMilliseconds;
                    }
                    else if (item is PortalItem)
                    {
                        var portal = (PortalItem)item;

                        //更新状态
                        var cursorPos = renderEnv.Camera.CameraToWorld(renderEnv.Input.MousePosition);
                        var sensorRect = new Rectangle(portal.X - 250, portal.Y - 150, 500, 300);
                        portal.View.IsFocusing = sensorRect.Contains(cursorPos);

                        //更新动画
                        var ani = portal.View.IsEditorMode ? portal.View.EditorAnimator : portal.View.Animator;
                        if (ani is StateMachineAnimator)
                        {
                            if (portal.View.Controller != null)
                            {
                                portal.View.Controller.Update(elapsed);
                            }
                            else
                            {
                                ((StateMachineAnimator)ani).Update(elapsed);
                            }
                        }
                        else if (ani is FrameAnimator)
                        {
                            var frameAni = (FrameAnimator)ani;
                            frameAni.Update(elapsed);
                        }
                    }
                    else if (item is IlluminantClusterItem)
                    {
                        var illuminantCluster = (IlluminantClusterItem)item;
                        (illuminantCluster.StartView.Animator as WzComparerR2.Controls.AnimationItem)?.Update(elapsed);
                        illuminantCluster.StartView.Time += (int)elapsed.TotalMilliseconds;
                        (illuminantCluster.EndView.Animator as WzComparerR2.Controls.AnimationItem)?.Update(elapsed);
                        illuminantCluster.EndView.Time += (int)elapsed.TotalMilliseconds;
                    }
                    else if (item is ReactorItem)
                    {
                        var reactor = (ReactorItem)item;
                        var ani = reactor.View.Animator;
                        if (ani is StateMachineAnimator)
                        {
                            if (reactor.View.Controller != null)
                            {
                                reactor.View.Controller.Update(elapsed);
                            }
                            else
                            {
                                ((StateMachineAnimator)ani).Update(elapsed);
                            }
                        }
                    }
                    else if (item is ParticleItem)
                    {
                        var particle = (ParticleItem)item;
                        var pSystem = particle.View?.ParticleSystem;
                        if (pSystem != null)
                        {
                            pSystem.Update(elapsed);
                        }
                    }
                }
            }
            else
            {
                for (int i = 0, i1 = node.Nodes.Count; i < i1; i++)
                {
                    UpdateAllItems(node.Nodes[i], elapsed);
                }
            }
        }

        private void UpdateTooltip()
        {
            var mouse = renderEnv.Input.MousePosition;

            var mouseElem = EmptyKeys.UserInterface.Input.InputManager.Current.MouseDevice.MouseOverElement;
            object target = null;
            if (mouseElem == this.ui.ContentControl)
            {
                var mouseTarget = this.allItems.Reverse<ItemRect>().FirstOrDefault(item =>
                {
                    return item.rect.Contains(mouse) && (item.item is LifeItem || item.item is PortalItem || item.item is IlluminantClusterItem || item.item is ReactorItem);
                });
                target = mouseTarget.item;
            }
            else if (mouseElem is ITooltipTarget)
            {
                var pos = EmptyKeys.UserInterface.Input.InputManager.Current.MouseDevice.GetPosition(mouseElem);
                target = ((ITooltipTarget)mouseElem).GetTooltipTarget(pos);
            }
            tooltip.TooltipTarget = target;
        }

        private void UpdateTopBar()
        {
            StringBuilder sb = new StringBuilder();
            var topbar = this.ui.TopBar;

            //显示地图名字
            int? mapID = this.mapData?.ID;
            sb.Append("[").Append(mapID != null ? mapID.ToString() : mapImg?.Node.FullPathToFile);
            if (!topbar.IsShortMode)
            {
                sb.Append(" ");
                StringResult sr;
                if (this.StringLinker != null && mapID != null
                    && this.StringLinker.StringMap.TryGetValue(mapID.Value, out sr))
                {
                    sb.Append(sr.Name);
                }
                else
                {
                    sb.Append("(null)");
                }
            }
            sb.Append("]");

            //显示bgm名字
            sb.Append(" [").Append(this.mapData?.Bgm ?? "(noBgm)").Append("]");

            //显示fps
            sb.AppendFormat(" [fps u:{0:f2} d:{1:f2}]", fpsCounter.UpdatePerSec, fpsCounter.DrawPerSec);

            //可见性
            sb.Append(" ctrl+");
            int[] array = new[] { 1, 2, 3, 4, 5, 6, 7, 9, 10 };
            for (int i = 0; i < array.Length; i++)
            {
                var objType = (Patches.RenderObjectType)array[i];
                sb.Append(this.patchVisibility.IsVisible(objType) ? "-" : (i + 1).ToString());
            }

            sb.Append(" Mouse:");
            var mouse = this.renderEnv.Input.MousePosition;
            var mousePos = this.renderEnv.Camera.CameraToWorld(mouse);
            sb.AppendFormat("{0},{1}", mousePos.X, mousePos.Y);
            this.ui.TopBar.Text = sb.ToString();
        }

        private void OnSceneItemClick(SceneItem item)
        {
            if (item is PortalItem)
            {
                var portal = (PortalItem)item;
                if (portal.ToMap != 999999999)
                {
                    MoveToPortal(portal.ToMap, portal.ToName, portal.PName);
                }
                else if (portal.GraphTargetMap.Count == 1)
                {
                    MoveToPortal(portal.GraphTargetMap[0], "sp", portal.PName);
                }
                else if (portal.GraphTargetMap.Count > 1)
                {
                    this.ui.Teleport.Sl = this.StringLinker;
                    this.ui.Teleport.CmbMaps.ItemsSource = portal.GraphTargetMap.ToList();
                    this.ui.Teleport.CmbMaps.SelectedIndex = 0;
                    this.ui.Teleport.Toggle();
                }
            }
            else if (item is IlluminantClusterItem)
            {
                var illuminantCluster = (IlluminantClusterItem)item;
                this.cm.StartCoroutine(OnCameraMoving(new Point(illuminantCluster.End.X, illuminantCluster.End.Y), 500));
            }
        }

        private void DrawScene(GameTime gameTime)
        {
            if (this.mapData == null)
            {
                return;
            }
            RenderTarget2D lightMapTexture = null;

            // draw lightmap if available
            if (this.mapData?.Light != null)
            {
                var viewport = this.GraphicsDevice.Viewport;
                lightMapTexture = new RenderTarget2D(this.GraphicsDevice, viewport.Width, viewport.Height, false, SurfaceFormat.Color, DepthFormat.None);
                var oldRenderTarget = this.GraphicsDevice.GetRenderTargets();
                this.GraphicsDevice.SetRenderTarget(lightMapTexture);
                this.PrepareLightMap();
                this.GraphicsDevice.SetRenderTargets(oldRenderTarget);
            }

            allItems.Clear();
            var camera = this.renderEnv.Camera;
            var origin = camera.Origin;
            this.batcher.Begin(origin, (float)(gameTime.TotalGameTime.TotalSeconds % 1000));
            Rectangle[] rects = null;
            //绘制场景
            foreach (var kv in GetDrawableItems(this.mapData.Scene))
            {
                this.batcher.Draw(kv.Value);

                //绘制标签
                DrawName(kv.Key);

                //缓存绘图区域
                {
                    int rectCount;
                    this.batcher.Measure(kv.Value, ref rects, out rectCount);
                    if (kv.Value.RenderObject is Frame)
                    {
                        var frame = (Frame)kv.Value.RenderObject;
                    }
                    if (rects != null && rectCount > 0)
                    {
                        for (int i = 0; i < rectCount; i++)
                        {
                            rects[i].X -= (int)origin.X;
                            rects[i].Y -= (int)origin.Y;
                            allItems.Add(new ItemRect() { item = kv.Key, rect = rects[i] });
                        }
                    }
                }

                this.batcher.MeshPush(kv.Value);
            }

            //在场景之上绘制额外标记
            DrawFootholds(gameTime);
            this.batcher.End();

            // apply light map
            if (lightMapTexture != null)
            {
                this.ApplyLightMap(lightMapTexture);
                lightMapTexture.Dispose();
            }
        }

        private void DrawTooltipItems(GameTime gameTime)
        {
            var pos = renderEnv.Camera.CameraToWorld(renderEnv.Input.MousePosition);
            var origin = renderEnv.Camera.Origin.ToPoint();
            foreach (var item in mapData.Tooltips)
            {
                if (item.CharRect.Contains(pos) || item.Rect.Contains(pos))
                {
                    var center = new Vector2(item.Rect.Center.X - origin.X, item.Rect.Center.Y - origin.Y);
                    tooltip.Draw(gameTime, renderEnv, item, center);
                }
            }
        }

        private void DrawFootholds(GameTime gameTime)
        {
            var color = MathHelper2.HSVtoColor((float)gameTime.TotalGameTime.TotalSeconds * 100 % 360, 1f, 1f);
            if (patchVisibility.FootHoldVisible)
            {
                var lines = new List<Point>();
                foreach (LayerNode layer in this.mapData.Scene.Layers.Nodes)
                {
                    var fhList = layer.Foothold.Nodes.OfType<ContainerNode<FootholdItem>>()
                        .Select(container => container.Item);
                    foreach (var fh in fhList)
                    {
                        lines.Add(new Point(fh.X1, fh.Y1));
                        lines.Add(new Point(fh.X2, fh.Y2));
                    }
                }

                if (lines.Count > 0)
                {
                    var meshItem = this.batcher.MeshPop();
                    meshItem.RenderObject = new LineListMesh(lines.ToArray(), color, 2);
                    this.batcher.Draw(meshItem);
                    this.batcher.MeshPush(meshItem);
                }
            }

            if (patchVisibility.LadderRopeVisible)
            {
                var lines = new List<Point>();
                var ladderList = this.mapData.Scene.Fly.LadderRope.Slots.OfType<LadderRopeItem>();
                foreach (var item in ladderList)
                {
                    lines.Add(new Point(item.X, item.Y1));
                    lines.Add(new Point(item.X, item.Y2));
                }

                if (lines.Count > 0)
                {
                    var meshItem = this.batcher.MeshPop();
                    meshItem.RenderObject = new LineListMesh(lines.ToArray(), color, 3);
                    this.batcher.Draw(meshItem);
                    this.batcher.MeshPush(meshItem);
                }
            }

            if (patchVisibility.SkyWhaleVisible)
            {
                var lines = new List<Point>();
                var skyWhaleList = this.mapData.Scene.Fly.SkyWhale.Slots.OfType<SkyWhaleItem>();
                foreach (var item in skyWhaleList)
                {
                    foreach (var dx in new[] { 0, -item.Width / 2, item.Width / 2 })
                    {
                        Point start = new Point(item.Start.X + dx, item.Start.Y);
                        Point end = new Point(item.End.X + dx, item.End.Y);
                        //画箭头
                        lines.Add(start);
                        lines.Add(end);
                        lines.Add(end);
                        lines.Add(new Point(end.X - 5, end.Y + 8));
                        lines.Add(end);
                        lines.Add(new Point(end.X + 5, end.Y + 8));
                    }
                }

                if (lines.Count > 0)
                {
                    var meshItem = this.batcher.MeshPop();
                    meshItem.RenderObject = new LineListMesh(lines.ToArray(), color, 1);
                    this.batcher.Draw(meshItem);
                    this.batcher.MeshPush(meshItem);
                }
            }

            if (patchVisibility.IlluminantClusterPathVisible)
            {
                var lines = new List<Point>();
                var illuminentClusterList = this.mapData.Scene.Fly.IlluminantCluster.Slots.OfType<IlluminantClusterItem>();
                foreach (var item in illuminentClusterList)
                {
                    foreach (var dx in new[] { 0, -item.Size / 2, item.Size / 2 })
                    {
                        Point start = new Point(item.Start.X + dx, item.Start.Y);
                        Point end = new Point(item.End.X + dx, item.End.Y);
                        //画箭头
                        lines.Add(start);
                        lines.Add(end);
                        lines.Add(end);
                        lines.Add(new Point(end.X - 5, end.Y + 8));
                        lines.Add(end);
                        lines.Add(new Point(end.X + 5, end.Y + 8));
                    }
                }

                if (lines.Count > 0)
                {
                    var meshItem = this.batcher.MeshPop();
                    meshItem.RenderObject = new LineListMesh(lines.ToArray(), color, 1);
                    this.batcher.Draw(meshItem);
                    this.batcher.MeshPush(meshItem);
                }
            }
        }

        private void DrawName(SceneItem item)
        {
            StringResult sr = null;
            MeshItem mesh = null;

            if (item is LifeItem)
            {
                var life = (LifeItem)item;
                switch (life.Type)
                {
                    case LifeItem.LifeType.Mob:
                        if (this.patchVisibility.MobNameVisible)
                        {
                            string lv = "Lv." + (life.LifeInfo?.level ?? 0);
                            string name;
                            if (this.StringLinker?.StringMob.TryGetValue(life.ID, out sr) ?? false)
                                name = sr.Name;
                            else
                                name = life.ID.ToString();

                            //绘制怪物名称
                            mesh = batcher.MeshPop();
                            mesh.Position = new Vector2(life.X, life.Cy + 4);
                            mesh.RenderObject = new TextMesh()
                            {
                                Align = Alignment.Center,
                                ForeColor = Color.White,
                                BackColor = new Color(Color.Black, 0.7f),
                                Font = renderEnv.Fonts.MobNameFont,
                                Padding = new Margins(2, 2, 2, 2),
                                Text = name
                            };
                            batcher.Draw(mesh);

                            //绘制怪物等级
                            var nameRect = batcher.Measure(mesh)[0];
                            mesh.Position = new Vector2(nameRect.X - 2, nameRect.Y + 3);
                            mesh.RenderObject = new TextMesh()
                            {
                                Align = Alignment.Far,
                                ForeColor = Color.White,
                                BackColor = new Color(Color.Black, 0.7f),
                                Font = renderEnv.Fonts.MobLevelFont,
                                Padding = new Margins(2, 1, 1, 1),
                                Text = lv
                            };
                            batcher.Draw(mesh);
                            batcher.MeshPush(mesh);
                        }
                        break;

                    case LifeItem.LifeType.Npc:
                        if (this.patchVisibility.NpcNameVisible)
                        {
                            if (life.HideName) break;

                            string name, desc;
                            if (this.StringLinker?.StringNpc.TryGetValue(life.ID, out sr) ?? false)
                            {
                                name = sr.Name;
                                desc = sr.Desc;
                            }
                            else
                            {
                                name = life.ID.ToString();
                                desc = null;
                            }

                            if (name != null)
                            {
                                mesh = batcher.MeshPop();
                                mesh.Position = new Vector2(life.X, life.Cy + 4);
                                mesh.RenderObject = new TextMesh()
                                {
                                    Align = Alignment.Center,
                                    ForeColor = Color.Yellow,
                                    BackColor = new Color(Color.Black, 0.7f),
                                    Font = renderEnv.Fonts.NpcNameFont,
                                    Padding = new Margins(2, 2, 2, 2),
                                    Text = name
                                };
                                batcher.Draw(mesh);
                                batcher.MeshPush(mesh);
                            }
                            if (desc != null)
                            {
                                mesh = batcher.MeshPop();
                                mesh.Position = new Vector2(life.X, life.Cy + 20);
                                // temporarily ignore font name and size here.
                                mesh.RenderObject = new TextMesh()
                                {
                                    Align = Alignment.Center,
                                    ForeColor = life.CustomFont?.FontColor ?? Color.Yellow,
                                    BackColor = new Color(Color.Black, 0.7f),
                                    Font = renderEnv.Fonts.NpcNameFont,
                                    Padding = new Margins(2, 2, 2, 2),
                                    Text = desc
                                };
                                batcher.Draw(mesh);
                                batcher.MeshPush(mesh);
                            }
                        }
                        break;
                }
            }
        }

        private void PrepareLightMap()
        {
            var mapLight = this.mapData.Light;
            var origin = this.renderEnv.Camera.Origin.ToPoint();
            this.GraphicsDevice.Clear(mapLight.BackColor);

            this.lightRenderer.Begin(Matrix.CreateTranslation(new Vector3(-origin.X, -origin.Y, 0)));
            // render spot light
            foreach (var light2D in mapLight.Lights)
            {
                this.lightRenderer.DrawSpotLight(light2D);
            }
            // render texture light
            foreach(var container in GetSceneContainers(this.mapData.Scene))
            {
                foreach (var item in container.Slots)
                {
                    if (item is ObjItem obj && obj.Light && obj.View.Animator is FrameAnimator frameAni)
                    {
                        var frame = frameAni.CurrentFrame;
                        this.lightRenderer.DrawTextureLight(frame.Texture, new Vector2(obj.X, obj.Y), frame.AtlasRect, frame.Origin.ToVector2(), obj.View.Flip, new Color(Color.White, frame.A0));
                    }
                }
            }
            this.lightRenderer.End();
        }

        private void ApplyLightMap(Texture2D lightMapTexture)
        {
            this.renderEnv.Sprite.Begin(blendState: this.renderEnv.BlendStateMultiplyRGB);
            this.renderEnv.Sprite.Draw(lightMapTexture, Vector2.Zero, Color.White);
            this.renderEnv.Sprite.End();
        }

        private IEnumerable<ContainerNode> GetSceneContainers(SceneNode node)
        {
            /*
            var container = node as ContainerNode;
            if (container != null)  //暂时不考虑缩进z层递归合并  container下没有子节点
            {
                yield return container;
            }
            else 
            {
                foreach (var mesh in node.Nodes.SelectMany(child => GetSceneContainers(child)))
                {
                    yield return mesh;
                }
            }*/
            Stack<SceneNode> sceneStack = new Stack<SceneNode>();
            Stack<int> indices = new Stack<int>();

            SceneNode currNode = node;
            int i = 0;

            while (currNode != null)
            {
                var container = currNode as ContainerNode;
                if (container != null)
                {
                    yield return container;
                    goto _pop;
                }
                else
                {
                    if (i < currNode.Nodes.Count)
                    {
                        var child = currNode.Nodes[i];
                        //push
                        sceneStack.Push(currNode);
                        indices.Push(i + 1);
                        currNode = child;
                        i = 0;
                        continue;
                    }
                    else
                    {
                        goto _pop;
                    }
                }

                _pop:
                if (sceneStack.Count > 0)
                {
                    currNode = sceneStack.Pop();
                    i = indices.Pop();
                }
                else
                {
                    break;
                }
                continue;
            }
        }

        private IEnumerable<KeyValuePair<SceneItem, MeshItem>> GetDrawableItems(MapScene scene)
        {
            var containers = GetSceneContainers(scene);
            var kvList = this.drawableItemsCache;

            foreach (var container in containers)
            {
                kvList.Clear();
                foreach (var item in container.Slots)
                {
                    var mesh = GetMesh(item);
                    if (mesh != null)
                    {
                        kvList.Add(new KeyValuePair<SceneItem, MeshItem>(item, mesh));
                    }
                    if (item is IlluminantClusterItem)
                    {
                        if (patchVisibility.IlluminantClusterVisible)
                        {
                            foreach (var meshIlluminantCluster in GetMeshesIlluminantCluster((IlluminantClusterItem)item))
                            {
                                kvList.Add(new KeyValuePair<SceneItem, MeshItem>(item, meshIlluminantCluster));
                            }
                        }
                    }
                }
                kvList.Sort((kv1, kv2) => kv1.Value.CompareTo(kv2.Value));
                foreach (var kv in kvList)
                {
                    yield return kv;
                }
            }

            kvList.Clear();
        }

        private MeshItem GetMesh(SceneItem item)
        {
            if (item.Tags != null && item.Tags.Any(tag => !patchVisibility.IsTagVisible(tag)))
            {
                return null;
            }

            switch (item)
            {
                case BackItem back:
                    if (back.Quest.Exists(quest => !patchVisibility.IsQuestVisible(quest.ID, quest.State)))
                    {
                        return null;
                    }
                    if (back.IsFront ? patchVisibility.FrontVisible : patchVisibility.BackVisible)
                    {
                        return GetMeshBack(back);
                    }
                    break;

                case ObjItem obj:
                    if (patchVisibility.ObjVisible && !obj.Light)
                    {
                        if (obj.Quest.Exists(quest => !patchVisibility.IsQuestVisible(quest.ID, quest.State)))
                        {
                            return null;
                        }
                        if (obj.Questex.Exists(questex => !patchVisibility.IsQuestVisible(questex.ID, questex.Key, questex.State)))
                        {
                            return null;
                        }
                        return GetMeshObj(obj);
                    }
                    break;

                case TileItem tile:
                    if (patchVisibility.TileVisible)
                    {
                        return GetMeshTile(tile);
                    }
                    break;

                case LifeItem life:
                    if ((life.Type == LifeItem.LifeType.Mob && patchVisibility.MobVisible)
                        || (life.Type == LifeItem.LifeType.Npc && patchVisibility.NpcVisible))
                    {
                        return GetMeshLife(life);
                    }
                    break;

                case PortalItem portal:
                    if (patchVisibility.PortalVisible)
                    {
                        return GetMeshPortal(portal);
                    }
                    break;

                case ReactorItem reactor:
                    if (patchVisibility.ReactorVisible)
                    {
                        return GetMeshReactor(reactor);
                    }
                    break;

                case ParticleItem particle:
                    if (particle.Quest.Exists(quest => !patchVisibility.IsQuestVisible(quest.ID, quest.State)))
                    {
                        return null;
                    }
                    if (patchVisibility.EffectVisible)
                    {
                        return GetMeshParticle((ParticleItem)item);
                    }
                    break;
            }
            return null;
        }

        private MeshItem GetMeshBack(BackItem back)
        {
            //计算计算culling
            if (back.ScreenMode != 0 && back.ScreenMode != renderEnv.Camera.DisplayMode + 1)
            {
                return null;
            }

            //计算坐标
            int cx = back.Cx;
            int cy = back.Cy;
            if ((back.TileMode & TileMode.BothTile) != 0 && (cx == 0 || cy == 0))
            {
                Point renderSize = Point.Zero;
                switch (back.View.Animator)
                {
                    case FrameAnimator frameAni:
                        renderSize = frameAni.Data.GetBound().Size;
                        break;
                    case AnimationItem aniItem:
                        // For spine animation, we don't know how to calculate the correct cx and cy
                        renderSize = aniItem.Measure().Size;
                        break;
                    case MsCustomSprite msCustomSprite:
                        renderSize = msCustomSprite.Size.ToPoint();
                        break;
                }

                if (cx == 0) cx = renderSize.X;
                if (cy == 0) cy = renderSize.Y;
            }

            Vector2 tileOff = new Vector2(cx, cy);
            Vector2 position = new Vector2(back.X, back.Y);

            //计算水平卷动
            if ((back.TileMode & TileMode.ScrollHorizontal) != 0)
            {
                position.X += ((float)back.Rx * 5 * back.View.Time / 1000) % cx;// +this.Camera.Center.X * (100 - Math.Abs(this.rx)) / 100;
            }
            else //镜头移动比率偏移
            {
                position.X += renderEnv.Camera.Center.X * (100 + back.Rx) / 100;
            }

            //计算垂直卷动
            if ((back.TileMode & TileMode.ScrollVertical) != 0)
            {
                position.Y += ((float)back.Ry * 5 * back.View.Time / 1000) % cy;// +this.Camera.Center.Y * (100 - Math.Abs(this.ry)) / 100;
            }
            else //镜头移动比率偏移
            {
                position.Y += (renderEnv.Camera.Center.Y) * (100 + back.Ry) / 100;
            }

            //y轴镜头调整
            //if (back.TileMode == TileMode.None && renderEnv.Camera.WorldRect.Height > 600)
            //    position.Y += (renderEnv.Camera.Height - 600) / 2;

            //取整
            position.X = (float)Math.Floor(position.X);
            position.Y = (float)Math.Floor(position.Y);

            //计算tile
            Rectangle? tileRect = null;
            if (back.TileMode != TileMode.None)
            {
                var cameraRect = renderEnv.Camera.ClipRect;

                int l, t, r, b;
                if ((back.TileMode & TileMode.Horizontal) != 0 && cx > 0)
                {
                    l = (int)Math.Floor((cameraRect.Left - position.X) / cx) - 1;
                    r = (int)Math.Ceiling((cameraRect.Right - position.X) / cx) + 1;
                }
                else
                {
                    l = 0;
                    r = 1;
                }

                if ((back.TileMode & TileMode.Vertical) != 0 && cy > 0)
                {
                    t = (int)Math.Floor((cameraRect.Top - position.Y) / cy) - 1;
                    b = (int)Math.Ceiling((cameraRect.Bottom - position.Y) / cy) + 1;
                }
                else
                {
                    t = 0;
                    b = 1;
                }

                tileRect = new Rectangle(l, t, r - l, b - t);
            }

            //生成mesh
            var renderObj = GetRenderObject(back.View.Animator, back.Flip, back.Alpha);
            if (renderObj == null)
            {
                return null;
            }
            var mesh = batcher.MeshPop();
            mesh.RenderObject = renderObj;
            mesh.Position = position;
            mesh.Z0 = 0;
            mesh.Z1 = back.Index;
            mesh.FlipX = back.Flip;
            mesh.TileRegion = tileRect;
            mesh.TileOffset = tileOff;
            return mesh;
        }

        private MeshItem GetMeshObj(ObjItem obj)
        {
            var renderObj = GetRenderObject(obj.View.Animator, flip: obj.View.Flip);
            if (renderObj == null)
            {
                return null;
            }
            var mesh = batcher.MeshPop();
            mesh.RenderObject = renderObj;
            mesh.Position = new Vector2(obj.X, obj.Y);
            mesh.Z0 = obj.Z;
            mesh.Z1 = obj.Index;

            if (obj.MoveW != 0 || obj.MoveH != 0)
            {
                mesh.Position += GetMovingObjPos(obj);
            }
            mesh.FlipX = obj.View.Flip;

            return mesh;
        }

        private MeshItem GetMeshTile(TileItem tile)
        {
            var renderObj = GetRenderObject(tile.View.Animator);
            if (renderObj == null)
            {
                return null;
            }
            var mesh = batcher.MeshPop();
            mesh.RenderObject = renderObj;
            mesh.Position = new Vector2(tile.X, tile.Y);
            mesh.Z0 = ((renderObj as Frame)?.Z ?? 0);
            mesh.Z1 = tile.Index;
            return mesh;
        }

        private MeshItem GetMeshLife(LifeItem life)
        {
            var renderObj = GetRenderObject(life.View.Animator);
            if (renderObj == null)
            {
                return null;
            }
            var mesh = batcher.MeshPop();
            mesh.RenderObject = renderObj;
            mesh.Position = new Vector2(life.X, life.Cy);
            mesh.FlipX = life.Flip;
            mesh.Z0 = ((renderObj as Frame)?.Z ?? 0);
            mesh.Z1 = life.Index;
            return mesh;
        }

        private MeshItem GetMeshPortal(PortalItem portal)
        {
            var renderObj = GetRenderObject(portal.View.IsEditorMode ? portal.View.EditorAnimator : portal.View.Animator);
            if (renderObj == null)
            {
                return null;
            }
            var mesh = batcher.MeshPop();
            mesh.RenderObject = renderObj;
            mesh.Position = new Vector2(portal.X, portal.Y);
            mesh.Z0 = ((renderObj as Frame)?.Z ?? 0);
            mesh.Z1 = portal.Index;
            return mesh;
        }

        private MeshItem[] GetMeshesIlluminantCluster(IlluminantClusterItem illuminantCluster)
        {
            var renderObj = GetRenderObject(illuminantCluster.StartView.Animator);
            if (renderObj == null)
            {
                return null;
            }
            var startMesh = batcher.MeshPop();
            startMesh.RenderObject = renderObj;
            startMesh.Position = new Vector2(illuminantCluster.Start.X + illuminantCluster.Size / 2, illuminantCluster.Start.Y - illuminantCluster.Size / 2);
            startMesh.Z0 = ((renderObj as Frame)?.Z ?? 0);
            startMesh.Z1 = illuminantCluster.Index;
            renderObj = GetRenderObject(illuminantCluster.EndView.Animator);
            if (renderObj == null)
            {
                return null;
            }
            var endMesh = batcher.MeshPop();
            endMesh.RenderObject = renderObj;
            endMesh.Position = new Vector2(illuminantCluster.End.X + illuminantCluster.Size / 2, illuminantCluster.End.Y - illuminantCluster.Size / 2);
            endMesh.Z0 = ((renderObj as Frame)?.Z ?? 0);
            endMesh.Z1 = illuminantCluster.Index;
            return new MeshItem[] { startMesh, endMesh };
        }

        private MeshItem GetMeshReactor(ReactorItem reactor)
        {
            var renderObj = GetRenderObject(reactor.View.Animator);
            if (renderObj == null)
            {
                return null;
            }
            var mesh = batcher.MeshPop();
            mesh.RenderObject = renderObj;
            mesh.Position = new Vector2(reactor.X, reactor.Y);
            mesh.FlipX = reactor.Flip;
            mesh.Z0 = ((renderObj as Frame)?.Z ?? 0);
            mesh.Z1 = reactor.Index;
            return mesh;
        }

        private MeshItem GetMeshParticle(ParticleItem particle)
        {
            var pSystem = particle.View?.ParticleSystem;
            if (pSystem == null)
            {
                return null;
            }

            Vector2 position;
            position.X = renderEnv.Camera.Center.X * (100 + particle.Rx) / 100;
            position.Y = renderEnv.Camera.Center.Y * (100 + particle.Ry) / 100;

            var mesh = batcher.MeshPop();
            mesh.RenderObject = pSystem;
            mesh.Position = position;
            mesh.Z0 = particle.Z;
            return mesh;
        }

        private object GetRenderObject(object animator, bool flip = false, int alpha = 255)
        {
            if (animator is FrameAnimator frameAni)
            {
                var frame = frameAni.CurrentFrame;
                if (frame != null)
                {
                    if (alpha < 255) //理论上应该返回一个新的实例
                    {
                        frame.A0 = frame.A0 * alpha / 255;
                    }
                    return frame;
                }
            }
            else if (animator is SpineAnimatorV2 spineAniV2)
            {
                var skeleton = spineAniV2.Skeleton;
                if (skeleton != null)
                {
                    if (alpha < 255)
                    {
                        skeleton.A = alpha / 255.0f;
                    }
                    return skeleton;
                }
            }
            else if (animator is SpineAnimatorV4 spineAniV4)
            {
                var skeleton = spineAniV4.Skeleton;
                if (skeleton != null)
                {
                    if (alpha < 255)
                    {
                        skeleton.A = alpha / 255.0f;
                    }
                    return skeleton;
                }
            }
            else if (animator is StateMachineAnimator smAni)
            {
                return smAni.Data.GetMesh();
            }
            else if (animator is MsCustomSprite msCustomSprite)
            {
                this.UpdateShaderConstant(msCustomSprite.Material);
                return msCustomSprite;
            }

            return null;
        }

        private Vector2 GetMovingObjPos(ObjItem obj)
        {
            double movingX = 0;
            double movingY = 0;
            double time = obj.View.Time;
            switch (obj.MoveType)
            {
                case 1:
                case 2: // line
                    time *= Math.PI * 2 / obj.MoveP;
                    movingX = obj.MoveW * Math.Cos(time);
                    movingY = obj.MoveH * Math.Cos(time);
                    break;
                case 3: // circle
                    time *= Math.PI * 2 / obj.MoveP;
                    movingX = obj.MoveW * Math.Cos(time);
                    movingY = obj.MoveH * Math.Sin(time);
                    break;

                case 6:
                case 7:
                case 8:
                    int sign = -1;
                    double freq = (double)(obj.MoveP + obj.MoveDelay) * 2;
                    time = time % freq;
                    if (time >= freq / 2)
                    {
                        time -= freq / 2;
                        if (obj.MoveType == 8)
                            obj.View.Flip = !obj.Flip;

                        if (obj.MoveType != 6)
                            sign = +1;
                    }
                    else
                    {
                        if (obj.MoveType == 8)
                            obj.View.Flip = obj.Flip;
                    }

                    movingX = (Math.Min(1, Math.Max(-1, (time - obj.MoveDelay) * -2 / obj.MoveP + 1)) * sign + 1) / 2 * obj.MoveW;
                    movingY = (Math.Min(1, Math.Max(-1, (time - obj.MoveDelay) * -2 / obj.MoveP + 1)) * sign + 1) / 2 * obj.MoveH;

                    break;

                default:
                    break;
            }
            return new Vector2((float)movingX, (float)movingY);
        }

        private void UpdateShaderConstant(ShaderMaterial shaderMaterial)
        {
            // we don't know the exact value that being used in the original client
            switch (shaderMaterial)
            {
                case LightPixelShaderMaterial light:
                    light.PlayerPos = renderEnv.Camera.CameraToWorld(renderEnv.Input.MousePosition).ToVector2();
                    light.LightInnerRadius = 50f;
                    light.LightOuterRadius = 200f;
                    light.PlayerLightColor = Color.White.ToVector4();
                    light.TopColor = Color.White.ToVector4();
                    light.BottomColor = new Color(0.2f, 0.2f, 0.2f, 1f).ToVector4();
                    light.MinY = 4500;
                    light.MaxY = 19500;
                    break;

                case WaterFrontPixelShaderMaterial waterFront:
                    waterFront.PlayerPos = renderEnv.Camera.CameraToWorld(renderEnv.Input.MousePosition).ToVector2();
                    waterFront.Factor1 = 1f;
                    waterFront.MinY = 4500;
                    waterFront.MaxY = 19500;
                    waterFront.DistNoiseCenterPos = waterFront.PlayerPos;
                    waterFront.Factor2 = 1f;
                    break;
            }
        }
    }
}
