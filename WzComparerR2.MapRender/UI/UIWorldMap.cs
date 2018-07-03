using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using EmptyKeys.UserInterface;
using EmptyKeys.UserInterface.Controls;
using EmptyKeys.UserInterface.Media;
using EmptyKeys.UserInterface.Data;
using EmptyKeys.UserInterface.Renderers;
using EmptyKeys.UserInterface.Media.Imaging;
using EmptyKeys.UserInterface.Input;

using WzComparerR2.WzLib;
using WzComparerR2.Common;

using Res = CharaSimResource.Resource;
using MRes = WzComparerR2.MapRender.Properties.Resources;
using MathHelper = Microsoft.Xna.Framework.MathHelper;

namespace WzComparerR2.MapRender.UI
{
    class UIWorldMap : WindowEx
    {
        public static readonly DependencyProperty CurrentWorldMapProperty = DependencyProperty.Register("CurrentWorldMap", typeof(WorldMapInfo), typeof(UIWorldMap), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty CurrentMapIDProperty = DependencyProperty.Register("CurrentMapID", typeof(int?), typeof(UIWorldMap), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty UseImageNameAsInfoNameProperty = DependencyProperty.Register("UseImageNameAsInfoName", typeof(bool), typeof(UIWorldMap), new FrameworkPropertyMetadata(false));
        public static readonly DependencyProperty SelectedQuestLimitIndexProperty = DependencyProperty.Register("SelectedQuestLimitIndex", typeof(int), typeof(UIWorldMap), new FrameworkPropertyMetadata(-1));

        public UIWorldMap() : base()
        {
            this.worldMaps = new ObservableCollection<WorldMapInfo>();

            this.CmbMaps.ItemsSource = this.worldMaps;

            //添加默认item样式
            var template = this.CmbMaps.FindResource(typeof(string)) as DataTemplate;
            if (template != null)
            {
                this.CmbMaps.Resources.Add(typeof(WorldMapInfo), template);
                this.CmbQuestList.Resources.Add(typeof(QuestLimitInfo), template);
            }

            //添加默认点击事件
            UIHelper.RegisterClickEvent(this.MapArea, (_, point) => this.MapArea.HitTest(point), this.OnMapAreaClick);
        }

        public bool IsDataLoaded { get; private set; }
        public ComboBox CmbMaps { get; private set; }
        public ComboBox CmbQuestList { get; private set; }
        public WorldMapArea MapArea { get; private set; }

        public event EventHandler<MapSpotEventArgs> MapSpotClick;

        private ObservableCollection<WorldMapInfo> worldMaps;

        public WorldMapInfo CurrentWorldMap
        {
            get { return (WorldMapInfo)this.GetValue(CurrentWorldMapProperty); }
            set { this.SetValue(CurrentWorldMapProperty, value); }
        }

        public int? CurrentMapID
        {
            get { return (int?)this.GetValue(CurrentMapIDProperty); }
            set { this.SetValue(CurrentMapIDProperty, value); }
        }

        public bool UseImageNameAsInfoName
        {
            get { return (bool)this.GetValue(UseImageNameAsInfoNameProperty); }
            set { this.SetValue(UseImageNameAsInfoNameProperty, value); }
        }

        public int SelectedQuestLimitIndex
        {
            get { return (int)this.GetValue(SelectedQuestLimitIndexProperty); }
            set { this.SetValue(SelectedQuestLimitIndexProperty, value); }
        }

        protected override void InitializeComponents()
        {
            var canvas = new Canvas();
            var canvasBackTexture = Engine.Instance.AssetManager.LoadTexture(null, nameof(MRes.UIWindow2_img_WorldMap_Border_0));
            canvas.Background = new ImageBrush() { ImageSource = new BitmapImage() { Texture = canvasBackTexture }, Stretch = Stretch.None };
            canvas.SetBinding(Canvas.WidthProperty, new Binding(UIWorldMap.WidthProperty) { Source = this, Mode = BindingMode.TwoWay });
            canvas.SetBinding(Canvas.HeightProperty, new Binding(UIWorldMap.HeightProperty) { Source = this, Mode = BindingMode.TwoWay });
            canvas.Parent = this;
            this.Content = canvas;

            Border title = new Border();
            title.Height = 17;
            title.SetBinding(Canvas.WidthProperty, new Binding(Canvas.WidthProperty) { Source = canvas });
            canvas.Children.Add(title);
            this.SetDragTarget(title);

            ComboBox cmbMaps = new ComboBox();
            cmbMaps.Width = 150;
            cmbMaps.Height = 20;
            Canvas.SetLeft(cmbMaps, 10);
            Canvas.SetTop(cmbMaps, 23);
            cmbMaps.SetBinding(ComboBox.SelectedItemProperty, new Binding(UIWorldMap.CurrentWorldMapProperty) { Source = this, Mode = BindingMode.TwoWay });
            canvas.Children.Add(cmbMaps);
            this.CmbMaps = cmbMaps;

            ComboBox cmbQuestList = new ComboBox();
            cmbQuestList.Width = 100;
            cmbQuestList.Height = 20;
            Canvas.SetLeft(cmbQuestList, 250);
            Canvas.SetTop(cmbQuestList, 23);
            cmbQuestList.SetBinding(ComboBox.SelectedIndexProperty, new Binding(UIWorldMap.SelectedQuestLimitIndexProperty) { Source = this, Mode = BindingMode.TwoWay });
            canvas.Children.Add(cmbQuestList);
            this.CmbQuestList = cmbQuestList;

            WorldMapArea mapArea = new WorldMapArea();
            mapArea.Width = 640;
            mapArea.Height = 480;
            mapArea.InputBindings.Add(new InputBinding(new RelayCommand(MapArea_RightClick), new MouseGesture(MouseAction.RightClick)));
            Canvas.SetLeft(mapArea, 7);
            Canvas.SetTop(mapArea, 44);
            canvas.Children.Add(mapArea);
            this.SetBinding(CurrentWorldMapProperty, new Binding(Control.DataContextProperty) { Source = mapArea, Mode = BindingMode.OneWayToSource });
            this.SetBinding(CurrentMapIDProperty, new Binding("CurrentMapID") { Source = mapArea, Mode = BindingMode.OneWayToSource });
            this.SetBinding(SelectedQuestLimitIndexProperty, new Binding("SelectedQuestIndex") { Source = mapArea, Mode = BindingMode.OneWayToSource });
            this.MapArea = mapArea;

            Button btnBack = new Button();
            btnBack.Width = 50;
            btnBack.Height = 20;
            btnBack.Content = "返回";
            btnBack.Click += BtnBack_Click;
            Canvas.SetLeft(btnBack, 180);
            Canvas.SetTop(btnBack, 23);
            canvas.Children.Add(btnBack);

            ImageButton btnClose = new ImageButton();
            btnClose.Name = "Close";
            btnClose.Click += BtnClose_Click;
            btnClose.SetResourceReference(UIElement.StyleProperty, MapRenderResourceKey.MapRenderButtonStyle);
            Canvas.SetRight(btnClose, 7);
            Canvas.SetTop(btnClose, 5);
            canvas.Children.Add(btnClose);

            this.Width = canvasBackTexture.Width;
            this.Height = canvasBackTexture.Height;
            base.InitializeComponents();
        }

        protected override void OnPropertyChanged(DependencyProperty property)
        {
            base.OnPropertyChanged(property);

            if (property == CurrentWorldMapProperty)
            {
                if (this.CmbQuestList != null)
                {
                    try
                    {
                        var quests = this.CurrentWorldMap?.QuestLimit;
                        if (quests == null || quests.Count <= 0)
                        {
                            this.CmbQuestList.ItemsSource = null;
                            this.CmbQuestList.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            var dataSource = new List<object>();
                            dataSource.AddRange(quests);
                            dataSource.Add("(none)");
                            this.CmbQuestList.ItemsSource = dataSource;
                            this.CmbQuestList.SelectedIndex = 0;
                            this.CmbQuestList.Visibility = Visibility.Visible;
                        }
                    }
                    catch //ignore exceptions on closing.
                    {
                    }
                }
            }
        }

        public void LoadWzResource()
        {
            if (this.IsDataLoaded)
            {
                return;
            }

            //读取所有世界地图
            var worldmapNode = PluginBase.PluginManager.FindWz("Map/WorldMap");
            if (worldmapNode == null) //加载失败
            {
                return;
            }

            this.worldMaps.Clear();
            this.CurrentWorldMap = null;

            foreach (var imgNode in worldmapNode.Nodes)
            {
                var img = imgNode.GetNodeWzImage();
                Wz_Node node;
                if (img != null && img.TryExtract())
                {
                    var worldMapInfo = new WorldMapInfo();

                    //加载地图索引
                    node = img.Node.Nodes["info"];
                    if (node != null)
                    {
                        if (!this.UseImageNameAsInfoName)
                        {
                            worldMapInfo.Name = node.Nodes["WorldMap"].GetValueEx<string>(null);
                        }
                        worldMapInfo.ParentMap = node.Nodes["parentMap"].GetValueEx<string>(null);
                    }
                    if (string.IsNullOrEmpty(worldMapInfo.Name))
                    {
                        var m = Regex.Match(img.Name, @"^(.*)\.img$");
                        worldMapInfo.Name = m.Success ? m.Result("$1") : img.Name;
                    }

                    //加载地图名称
                    {
                        var m = Regex.Match(worldMapInfo.Name, @"^WorldMap(.+)$");
                        if (m.Success)
                        {
                            var stringNode = PluginBase.PluginManager.FindWz("String/WorldMap.img/" + m.Result("$1"));
                            worldMapInfo.DisplayName = stringNode?.Nodes["name"].GetValueEx<string>(null);
                        }
                    }

                    //加载baseImg
                    node = img.Node.Nodes["BaseImg"]?.Nodes["0"];
                    if (node != null)
                    {
                        worldMapInfo.BaseImg = LoadTextureItem(node);
                    }

                    //加载地图列表
                    node = img.Node.Nodes["MapList"];
                    if (node != null)
                    {
                        for (int i = 0; ; i++)
                        {
                            var spotNode = node.Nodes[i.ToString()];
                            if (spotNode == null)
                            {
                                break;
                            }

                            var spot = new MapSpot();
                            var location = spotNode.Nodes["spot"]?.GetValueEx<Wz_Vector>(null);
                            if (location != null)
                            {
                                spot.Spot = location.ToPointF();
                            }
                            else //兼容pre-bb的格式
                            {
                                spot.IsPreBB = true;
                                spot.Spot = new PointF(spotNode.Nodes["x"].GetValueEx<int>(0),
                                    spotNode.Nodes["y"].GetValueEx<int>(0));
                            }
                            spot.Type = spotNode.Nodes["type"].GetValueEx<int>(0);
                            spot.Title = spotNode.Nodes["title"].GetValueEx<string>(null);
                            spot.Desc = spotNode.Nodes["desc"].GetValueEx<string>(null);
                            spot.NoTooltip = spotNode.Nodes["noToolTip"].GetValueEx<int>(0) != 0;
                            var pathNode = spotNode.Nodes["path"];
                            if (pathNode != null)
                            {
                                spot.Path = LoadTextureItem(pathNode);
                            }
                            var mapNoNode = spotNode.Nodes["mapNo"];
                            if (mapNoNode != null)
                            {
                                foreach (var subNode in mapNoNode.Nodes)
                                {
                                    spot.MapNo.Add(subNode.GetValue<int>());
                                }
                            }
                            worldMapInfo.MapList.Add(spot);
                        }
                    }

                    //加载地图链接
                    node = img.Node.Nodes["MapLink"];
                    if (node != null)
                    {
                        foreach (var mapLinkNode in node.Nodes)
                        {
                            var link = new MapLink();
                            link.Index = int.Parse(mapLinkNode.Text);
                            link.Tooltip = mapLinkNode.Nodes["toolTip"].GetValueEx<string>(null);
                            var linkNode = mapLinkNode.Nodes["link"];
                            if (linkNode != null)
                            {
                                link.LinkMap = linkNode.Nodes["linkMap"].GetValueEx<string>(null);
                                var linkImgNode = linkNode.Nodes["linkImg"];
                                if (linkImgNode != null)
                                {
                                    link.LinkImg = LoadTextureItem(linkImgNode, true);
                                }
                            }
                            worldMapInfo.MapLinks.Add(link);
                        }
                    }

                    //KMST1070 加载阶段大地图
                    node = img.Node.Nodes["QuestLimit"];
                    if (node != null)
                    {
                        var qlNode = node.Nodes["default"];
                        if (qlNode != null)
                        {
                            worldMapInfo.QuestLimit.Add(this.LoadQuestLimit(qlNode));
                        }
                        for (int i = 0; ; i++)
                        {
                            qlNode = node.Nodes[i.ToString()];
                            if (qlNode == null)
                            {
                                break;
                            }

                            worldMapInfo.QuestLimit.Add(this.LoadQuestLimit(qlNode));
                        }
                    }

                    this.worldMaps.Add(worldMapInfo);
                }
            }

            //读取公共资源
            var worldMapResNode = PluginBase.PluginManager.FindWz("Map/MapHelper.img/worldMap");
            var mapImageNode = worldMapResNode?.Nodes["mapImage"];
            if (mapImageNode != null)
            {
                foreach (var imgNode in mapImageNode.Nodes)
                {
                    var texture = this.LoadTextureItem(imgNode);
                    var key = "mapImage/" + imgNode.Text;
                    this.Resources[key] = texture;
                }
            }

            var curPosNode = worldMapResNode?.FindNodeByPath(@"curPos\0");
            if (curPosNode != null)
            {
                var texture = this.LoadTextureItem(curPosNode);
                this.Resources["curPos"] = texture;
            }

            //处理当前地图信息
            foreach (var map in this.worldMaps)
            {
                if (map.ParentMap != null)
                {
                    map.ParentMapInfo = this.worldMaps.FirstOrDefault(_map => _map.Name == map.ParentMap);
                }
            }

            this.IsDataLoaded = true;
            this.JumpToCurrentMap();
        }

        private QuestLimitInfo LoadQuestLimit(Wz_Node node)
        {
            var questLimit = new QuestLimitInfo();
            questLimit.Name = node.Text;

            var baseImgNode = node.Nodes["0"];
            if (baseImgNode != null)
            {
                questLimit.BaseImg = this.LoadTextureItem(baseImgNode);
            }

            var openMapNode = node.Nodes["openMap"];
            if (openMapNode != null)
            {
                for (int i = 0; ; i++)
                {
                    var subNode = openMapNode.Nodes[i.ToString()];
                    if (subNode == null)
                    {
                        break;
                    }

                    var spotIndex = subNode.GetValue<int>();
                    questLimit.OpenMaps.Add(spotIndex);
                }
            }

            var closeMapNode = node.Nodes["closeMap"];
            if (closeMapNode != null)
            {
                for (int i = 0; ; i++)
                {
                    var subNode = closeMapNode.Nodes[i.ToString()];
                    if (subNode == null)
                    {
                        break;
                    }

                    var spotIndex = subNode.GetValue<int>();
                    questLimit.CloseMaps.Add(spotIndex);
                }
            }

            questLimit.IsDefault = node.Nodes["default"].GetValueEx(0) != 0;
            questLimit.Quest = node.Nodes["quest"].GetValueEx(0);
            questLimit.State = node.Nodes["state"].GetValueEx(0);
            questLimit.CloseMapImageType = node.Nodes["closeMapImageType"].GetValueEx<int?>(null);

            return questLimit;
        }

        public void JumpToCurrentMap()
        {
            if (this.CurrentMapID != null)
            {
                var mapID = this.CurrentMapID.Value;
                var query = this.worldMaps.Where(_map => _map.MapList.Any(_spot => _spot.MapNo.Contains(mapID)))
                    .Select(_map =>
                    {
                        int level = 0;
                        for (var cur = _map; cur != null; cur = cur.ParentMapInfo)
                        {
                            level++;
                        }
                        return new { map = _map, level = level };
                    })
                    .OrderByDescending(item => item.level);
                var suitMap = query.FirstOrDefault()?.map;
                if (suitMap != null)
                {
                    this.CurrentWorldMap = suitMap;
                    return;
                }
            }
            this.CurrentWorldMap = this.worldMaps.FirstOrDefault();
        }

        private TextureItem LoadTextureItem(Wz_Node node, bool loadHitMap = false)
        {
            var item = new TextureItem();
            var linkNode = node.GetLinkedSourceNode(PluginBase.PluginManager.FindWz);
            item.Texture = UIHelper.LoadTexture(linkNode);
            item.Z = node.Nodes["z"].GetValueEx<int>(0);
            var origin = node.Nodes["origin"]?.GetValueEx<Wz_Vector>(null);
            item.Origin = origin.ToPointF();
            if (item.Texture != null && loadHitMap)
            {
                item.HitMap = UIHelper.CreateHitMap(item.Texture.GetNativeTexture() as Microsoft.Xna.Framework.Graphics.Texture2D);
            }
            return item;
        }

        private void OnMapAreaClick(object obj)
        {
            if (obj is MapLink)
            {
                var link = (MapLink)obj;
                var linkmapInfo = this.worldMaps.FirstOrDefault(map => map.Name == link.LinkMap);
                if (linkmapInfo != null)
                {
                    this.CurrentWorldMap = linkmapInfo;
                }
            }
            else if (obj is MapSpot)
            {
                var spot = (MapSpot)obj;
                if (spot.MapNo.Count > 0)
                {
                    this.MapSpotClick?.Invoke(this, new MapSpotEventArgs(spot.MapNo[0]));
                }
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            this.GoBack();
        }

        private void MapArea_RightClick(object obj)
        {
            this.GoBack();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void GoBack()
        {
            var curMap = this.CurrentWorldMap;
            if (curMap != null && curMap.ParentMap != null)
            {
                if (curMap.ParentMapInfo != null)
                {
                    this.CurrentWorldMap = curMap.ParentMapInfo;
                }
            }
        }

        public class WorldMapArea : Control, ITooltipTarget
        {
            public WorldMapArea()
            {
                this.hitAreaCache = new List<DrawItem>();
            }

            public WorldMapInfo WorldMap { get; set; }
            public int? CurrentMapID { get; set; }
            public int SelectedQuestIndex { get; set; }

            private List<DrawItem> hitAreaCache;

            public object GetTooltipTarget(PointF mouseLocation)
            {
                var hitItem = HitTest(mouseLocation);
                if (hitItem is MapSpot)
                {
                    var spot = (MapSpot)hitItem;
                    var tooltip = new UIWorldMap.Tooltip()
                    {
                        Spot = spot
                    };
                    return tooltip;
                }
                return null;
            }

            public object HitTest(PointF position)
            {
                for (int i = this.hitAreaCache.Count - 1; i >= 0; i--)
                {
                    var item = this.hitAreaCache[i];
                    var pos = new PointF(position.X - item.Position.X, position.Y - item.Position.Y);
                    if (item.Target != null)
                    {
                        if (item.TextureItem.HitMap != null ? item.TextureItem.HitMap[(int)pos.X, (int)pos.Y]
                            : new Rect(0, 0, item.TextureItem.Texture.Width, item.TextureItem.Texture.Height).Contains(pos))
                        {
                            return item.Target;
                        }
                    }
                }
                return null;
            }

            protected override void OnDraw(Renderer spriterenderer, double elapsedGameTime, float opacity)
            {
                base.OnDraw(spriterenderer, elapsedGameTime, opacity);
                this.hitAreaCache.Clear();

                var curMap = this.DataContext as WorldMapInfo;
                if (curMap == null)
                {
                    return;
                }

                TextureItem baseImg = curMap.BaseImg;
                SpotState[] spotState = new SpotState[curMap.MapList.Count];

                if (curMap.QuestLimit != null && this.SelectedQuestIndex > -1 && this.SelectedQuestIndex < curMap.QuestLimit.Count)
                {
                    for(int i = 0; i <= this.SelectedQuestIndex; i++)
                    {
                        var quest = curMap.QuestLimit[i];
                        //重写底图
                        if (quest.BaseImg != null)
                        {
                            baseImg = quest.BaseImg;
                        }
                        else if (quest.IsDefault)
                        {
                            baseImg = curMap.BaseImg;
                        }
                        //重写spot属性

                        if (quest.IsDefault)
                        {
                            for (int j = 0; j < spotState.Length; j++)
                            {
                                spotState[j].IsOverride = true;
                                spotState[j].IsVisible = false;
                            }
                        }
                        foreach (var spotIndex in quest.CloseMaps)
                        {
                            spotState[spotIndex].IsOverride = true;
                            spotState[spotIndex].IsVisible = true;
                            spotState[spotIndex].IsOpen = false;
                            spotState[spotIndex].ImgType = quest.CloseMapImageType ?? -1;
                        }
                        foreach (var spotIndex in quest.OpenMaps)
                        {
                            spotState[spotIndex].IsOverride = true;
                            spotState[spotIndex].IsVisible = true;
                            spotState[spotIndex].IsOpen = true;
                            spotState[spotIndex].ImgType = -1;
                        }
                    }
                }

                var baseOrigin = new PointF((int)this.Width / 2, (int)this.Height / 2);

                var drawOrder = new List<DrawItem>();
                var addItem = new Action<TextureItem, object>((texture, obj) =>
                {
                    drawOrder.Add(new DrawItem() { Target = obj, TextureItem = texture });
                });

                //获取鼠标位置
                var mousePos = InputManager.Current.MouseDevice.GetPosition(this);
                MapSpot curSpot = null;

                //绘制底图
                if (baseImg != null)
                {
                    addItem(baseImg, null);
                }

                //绘制link
                foreach (var link in curMap.MapLinks)
                {
                    if (link.LinkImg != null)
                    {
                        var pos = new PointF(mousePos.X - (baseOrigin.X - link.LinkImg.Origin.X),
                           mousePos.Y - (baseOrigin.Y - link.LinkImg.Origin.Y));
                        if (link.LinkImg.HitMap?[(int)pos.X, (int)pos.Y] ?? false)
                        {
                            addItem(link.LinkImg, link);
                        }
                    }
                }

                //绘制地图点
                for (int i = 0, i0 = curMap.MapList.Count; i < i0; i++)
                {
                    var spot = curMap.MapList[i];
                    int spotType = spot.Type;
                    if (spotState[i].IsOverride) //重写判定
                    {
                        if (!spotState[i].IsVisible)
                        {
                            continue;
                        }
                        if (!spotState[i].IsOpen) //close
                        {
                            if (spotState[i].ImgType > -1)
                            {
                                spotType = spotState[i].ImgType;
                            }
                        }
                    }

                    var texture = this.FindResource("mapImage/" + spotType) as TextureItem;
                    if (texture != null)
                    {
                        var item = new TextureItem()
                        {
                            Texture = texture.Texture,
                            Origin = new PointF(-spot.Spot.X + texture.Origin.X, -spot.Spot.Y + texture.Origin.Y),
                            Z = texture.Z
                        };
                        if (spot.IsPreBB && curMap.BaseImg != null) //pre-bb地图点调整
                        {
                            item.Origin.X += curMap.BaseImg.Origin.X;
                            item.Origin.Y += curMap.BaseImg.Origin.Y;
                        }
                        addItem(item, spot);

                        //判断鼠标位置绘制path
                        if (spot.Path != null)
                        {
                            var rect = new Rect(baseOrigin.X - item.Origin.X, baseOrigin.Y - item.Origin.Y, texture.Texture.Width, texture.Texture.Height);
                            if (rect.Contains(mousePos))
                            {
                                addItem(spot.Path, null);
                            }
                        }
                    }

                    if (this.CurrentMapID != null && spot.MapNo.Contains(this.CurrentMapID.Value))
                    {
                        curSpot = spot;
                    }
                }

                //绘制当前地图标记
                if (curSpot != null)
                {
                    var posTexture = this.FindResource("curPos") as TextureItem;
                    if (posTexture != null)
                    {
                        var item = new TextureItem()
                        {
                            Texture = posTexture.Texture,
                            Origin = new PointF(-curSpot.Spot.X + posTexture.Origin.X, -curSpot.Spot.Y + posTexture.Origin.Y),
                            Z = 255,
                        };
                        addItem(item, null);
                    }
                }

                //开始绘制
                foreach (var item in drawOrder.OrderBy(_item => _item.TextureItem.Z))
                {
                    var tex = item.TextureItem;
                    if (tex != null)
                    {
                        var pos = new PointF(baseOrigin.X - tex.Origin.X, baseOrigin.Y - tex.Origin.Y);
                        var size = new Size(tex.Texture.Width, tex.Texture.Height);
                        spriterenderer.Draw(tex.Texture,
                            new PointF(this.RenderPosition.X + pos.X, this.RenderPosition.Y + pos.Y),
                            size,
                            new ColorW(1f, 1f, 1f, opacity),
                            false);

                        item.Position = pos;
                        this.hitAreaCache.Add(item);
                    }
                }
            }

            private class DrawItem
            {
                public object Target;
                public PointF Position;
                public TextureItem TextureItem;
            }

            private struct SpotState
            {
                public bool IsOverride;
                public bool IsVisible;
                public bool IsOpen;
                public int ImgType;
            }
        }

        public class WorldMapInfo
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public string ParentMap { get; set; }
            public TextureItem BaseImg { get; set; }
            public List<MapSpot> MapList { get; private set; } = new List<MapSpot>();
            public List<MapLink> MapLinks { get; private set; } = new List<MapLink>();
            public List<QuestLimitInfo> QuestLimit { get; private set; } = new List<QuestLimitInfo>();

            //缓存数据
            public WorldMapInfo ParentMapInfo { get; set; }

            public override string ToString()
            {
                return this.DisplayName ?? this.Name ?? base.ToString();
            }
        }

        public class MapSpot
        {
            public PointF Spot { get; set; }
            public bool IsPreBB { get; set; }
            public int Type { get; set; }
            public TextureItem Path { get; set; }
            public string Title { get; set; }
            public string Desc { get; set; }
            public List<int> MapNo { get; private set; } = new List<int>();
            public bool NoTooltip { get; set; }

            public override string ToString()
            {
                return string.Format("{0}, {1} maps, {2}", this.Title ?? "(null)", this.MapNo.Count, this.GetType().Name);
            }
        }

        public class MapLink
        {
            public int Index { get; set; }
            public string Tooltip { get; set; }
            public string LinkMap { get; set; }
            public TextureItem LinkImg { get; set; }
        }

        public class QuestLimitInfo
        {
            public string Name { get; set; }
            public TextureItem BaseImg { get; set; }
            public List<int> OpenMaps { get; private set; } = new List<int>();
            public List<int> CloseMaps { get; private set; } = new List<int>();
            public int Quest { get; set; }
            public int State { get; set; }
            public int? CloseMapImageType { get; set; }
            public bool IsDefault { get; set; }

            public override string ToString()
            {
                return this.Name;
            }
        }

        public class TextureItem
        {
            public TextureBase Texture;
            public PointF Origin;
            public int Z;
            public HitMap HitMap;
        }

        public class Tooltip
        {
            public MapSpot Spot { get; set; }
        }

        public class MapSpotEventArgs : EventArgs
        {
            public MapSpotEventArgs(int mapID)
            {
                this.MapID = mapID;
            }

            public int MapID { get; private set; }
        }
    }
}
