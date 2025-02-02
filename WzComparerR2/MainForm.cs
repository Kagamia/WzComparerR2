using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using static Microsoft.Xna.Framework.MathHelper;
using Timer = System.Timers.Timer;
using DevComponents.AdvTree;
using DevComponents.AdvTree.Display;
using DevComponents.DotNetBar;
using DevComponents.DotNetBar.Controls;

using WzComparerR2.Animation;
using WzComparerR2.CharaSim;
using WzComparerR2.CharaSimControl;
using WzComparerR2.Common;
using WzComparerR2.Comparer;
using WzComparerR2.Config;
using WzComparerR2.Controls;
using WzComparerR2.Encoders;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;

namespace WzComparerR2
{
    public partial class MainForm : Office2007RibbonForm, PluginContextProvider
    {
        public MainForm()
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font(new FontFamily("Microsoft Sans Serif"), 8f);
#endif
            Form.CheckForIllegalCrossThreadCalls = false;
            this.MinimumSize = new Size(600, 450);
            advTree1.AfterNodeSelect += new AdvTreeNodeEventHandler(advTree1_AfterNodeSelect_2);
            advTree2.AfterNodeSelect += new AdvTreeNodeEventHandler(advTree2_AfterNodeSelect_2);
            //new ImageDragHandler(this.pictureBox1).AttachEvents();
            RegisterPluginEvents();
            createStyleItems();
            initFields();
        }

        List<Wz_Structure> openedWz;
        StringLinker stringLinker;
        HistoryList<Node> historyNodeList;
        bool historySelecting;

        //soundPlayer
        BassSoundPlayer soundPlayer;
        Timer soundTimer;
        bool timerChangeValue;

        //charaSim
        AfrmTooltip tooltipQuickView;
        CharaSimControlGroup charaSimCtrl;
        AdvTree lastSelectedTree;
        DefaultLevel skillDefaultLevel = DefaultLevel.Level0;
        int skillInterval = 32;

        //compare
        Thread compareThread;

        private void initFields()
        {
            openedWz = new List<Wz_Structure>();
            stringLinker = new StringLinker();
            historyNodeList = new HistoryList<Node>();

            tooltipQuickView = new AfrmTooltip();
            tooltipQuickView.Visible = false;
            tooltipQuickView.StringLinker = this.stringLinker;
            tooltipQuickView.KeyDown += new KeyEventHandler(afrm_KeyDown);
            tooltipQuickView.ShowID = true;
            tooltipQuickView.ShowMenu = true;

            charaSimCtrl = new CharaSimControlGroup();
            charaSimCtrl.StringLinker = this.stringLinker;
            charaSimCtrl.Character = new Character();
            charaSimCtrl.Character.Name = "Test";

            string[] images = new string[] { "dir", "mp3", "num", "png", "str", "uol", "vector", "img", "rawdata", "convex", "video" };
            foreach (string img in images)
            {
                imageList1.Images.Add(img, (Image)Properties.Resources.ResourceManager.GetObject(img));
            }

            soundPlayer = new BassSoundPlayer();
            if (!soundPlayer.Init())
            {
                ManagedBass.Errors error = soundPlayer.GetLastError();
                MessageBoxEx.Show("Bass初始化失败！\r\n\r\nerrorCode : " + (int)error + "(" + error + ")", "虫子");
            }
            soundTimer = new Timer(120d);
            soundTimer.Elapsed += new System.Timers.ElapsedEventHandler(soundTimer_Elapsed);
            soundTimer.Enabled = true;

            PluginBase.PluginManager.WzFileFinding += new FindWzEventHandler(CharaSimLoader_WzFileFinding);

            foreach (WzPngComparison comp in Enum.GetValues(typeof(WzPngComparison)))
            {
                cmbComparePng.Items.Add(comp);
            }
            cmbComparePng.SelectedItem = WzPngComparison.SizeAndDataLength;
        }

        /// <summary>
        /// 插件加载时执行的方法，用于初始化配置文件。
        /// </summary>
        internal void PluginOnLoad()
        {
            ConfigManager.RegisterAllSection(this.GetType().Assembly);
            var conf = ImageHandlerConfig.Default;
            //刷新最近打开文件列表
            refreshRecentDocItems();
            //读取CharaSim配置
            UpdateCharaSimSettings();
            //wz加载配置
            UpdateWzLoadingSettings();

            //杂项配置
            labelItemAutoSaveFolder.Text = ImageHandlerConfig.Default.AutoSavePictureFolder;
            buttonItemAutoSave.Checked = ImageHandlerConfig.Default.AutoSaveEnabled;
            comboBoxItemLanguage.SelectedIndex = Clamp(CharaSimConfig.Default.SelectedFontIndex, 0, comboBoxItemLanguage.Items.Count);


            //更新界面颜色
            styleManager1.ManagerStyle = WcR2Config.Default.MainStyle;
            UpdateButtonItemStyles();
            styleManager1.ManagerColorTint = WcR2Config.Default.MainStyleColor;
        }

        void UpdateCharaSimSettings()
        {
            var Setting = CharaSimConfig.Default;
            this.buttonItemAutoQuickView.Checked = Setting.AutoQuickView;
            tooltipQuickView.SkillRender.ShowProperties = Setting.Skill.ShowProperties;
            tooltipQuickView.SkillRender.ShowObjectID = Setting.Skill.ShowID;
            tooltipQuickView.SkillRender.ShowDelay = Setting.Skill.ShowDelay;
            tooltipQuickView.SkillRender.DisplayCooltimeMSAsSec = Setting.Skill.DisplayCooltimeMSAsSec;
            tooltipQuickView.SkillRender.DisplayPermyriadAsPercent = Setting.Skill.DisplayPermyriadAsPercent;
            tooltipQuickView.SkillRender.IgnoreEvalError = Setting.Skill.IgnoreEvalError;
            this.skillDefaultLevel = Setting.Skill.DefaultLevel;
            this.skillInterval = Setting.Skill.IntervalLevel;
            tooltipQuickView.GearRender.ShowObjectID = Setting.Gear.ShowID;
            tooltipQuickView.GearRender.ShowSpeed = Setting.Gear.ShowWeaponSpeed;
            tooltipQuickView.GearRender.ShowLevelOrSealed = Setting.Gear.ShowLevelOrSealed;
            tooltipQuickView.GearRender.ShowMedalTag = Setting.Gear.ShowMedalTag;
            tooltipQuickView.ItemRender.ShowObjectID = Setting.Item.ShowID;
            tooltipQuickView.ItemRender.LinkRecipeInfo = Setting.Item.LinkRecipeInfo;
            tooltipQuickView.ItemRender.LinkRecipeItem = Setting.Item.LinkRecipeItem;
            tooltipQuickView.ItemRender.ShowNickTag = Setting.Item.ShowNickTag;
            tooltipQuickView.RecipeRender.ShowObjectID = Setting.Recipe.ShowID;
        }

        void UpdateWzLoadingSettings()
        {
            var config = WcR2Config.Default;
            Encoding enc;
            try
            {
                enc = Encoding.GetEncoding(config.WzEncoding);
            }
            catch
            {
                enc = null;
            }
            Wz_Structure.DefaultEncoding = enc;
            Wz_Structure.DefaultAutoDetectExtFiles = config.AutoDetectExtFiles;
            Wz_Structure.DefaultImgCheckDisabled = config.ImgCheckDisabled;
            Wz_Structure.DefaultWzVersionVerifyMode = config.WzVersionVerifyMode;
        }

        void CharaSimLoader_WzFileFinding(object sender, FindWzEventArgs e)
        {
            string[] fullPath = null;
            if (!string.IsNullOrEmpty(e.FullPath)) //用fullpath作为输入参数
            {
                fullPath = e.FullPath.Split('/', '\\');
                e.WzType = Enum.TryParse<Wz_Type>(fullPath[0], true, out var wzType) ? wzType : Wz_Type.Unknown;
            }

            List<Wz_Node> preSearch = new List<Wz_Node>();
            if (e.WzType != Wz_Type.Unknown) //用wztype作为输入参数
            {
                IEnumerable<Wz_Structure> preSearchWz = e.WzFile?.WzStructure != null ?
                    Enumerable.Repeat(e.WzFile.WzStructure, 1) :
                    this.openedWz;
                foreach (var wzs in preSearchWz)
                {
                    Wz_File baseWz = null;
                    bool find = false;
                    foreach (Wz_File wz_f in wzs.wz_files)
                    {
                        if (wz_f.Type == e.WzType)
                        {
                            preSearch.Add(wz_f.Node);
                            find = true;
                            //e.WzFile = wz_f;
                        }
                        if (wz_f.Type == Wz_Type.Base)
                        {
                            baseWz = wz_f;
                        }
                    }

                    // detect data.wz
                    if (baseWz != null && !find)
                    {
                        string key = e.WzType.ToString();
                        foreach (Wz_Node node in baseWz.Node.Nodes)
                        {
                            if (node.Text == key && node.Nodes.Count > 0)
                            {
                                preSearch.Add(node);
                            }
                        }
                    }
                }
            }

            if (fullPath == null || fullPath.Length <= 1)
            {
                if (e.WzType != Wz_Type.Unknown && preSearch.Count > 0) //返回wzFile
                {
                    e.WzNode = preSearch[0];
                    e.WzFile = preSearch[0].Value as Wz_File;
                }
                return;
            }

            if (preSearch.Count <= 0)
            {
                return;
            }

            foreach (var wzFileNode in preSearch)
            {
                var searchNode = wzFileNode;
                for (int i = 1; i < fullPath.Length && searchNode != null; i++)
                {
                    searchNode = searchNode.Nodes[fullPath[i]];
                    var img = searchNode.GetValueEx<Wz_Image>(null);
                    if (img != null)
                    {
                        searchNode = img.TryExtract() ? img.Node : null;
                    }
                }

                if (searchNode != null)
                {
                    e.WzNode = searchNode;
                    e.WzFile = wzFileNode.Value as Wz_File;
                    return;
                }
            }
            //寻找失败
            e.WzNode = null;
        }

        #region 界面主题配置
        private void createStyleItems()
        {
            //添加菜单
            foreach (eStyle style in Enum.GetValues(typeof(eStyle)).OfType<eStyle>().Distinct())
            {
                var buttonItemStyle = new ButtonItem() { Tag = style, Text = style.ToString(), Checked = (styleManager1.ManagerStyle == style) };
                buttonItemStyle.Click += new EventHandler(buttonItemStyle_Click);
                this.buttonItemStyle.SubItems.Add(buttonItemStyle);
            }

            var styleColorPicker = new ColorPickerDropDown() { Text = "StyleColorTint", BeginGroup = true, SelectedColor = styleManager1.ManagerColorTint };
            styleColorPicker.SelectedColorChanged += new EventHandler(styleColorPicker_SelectedColorChanged);
            buttonItemStyle.SubItems.Add(styleColorPicker);
        }

        private void buttonItemStyle_Click(object sender, EventArgs e)
        {
            var style = (eStyle)((sender as ButtonItem).Tag);
            styleManager1.ManagerStyle = style;
            UpdateButtonItemStyles();
            ConfigManager.Reload();
            WcR2Config.Default.MainStyle = style;
            ConfigManager.Save();
        }

        private void UpdateButtonItemStyles()
        {
            foreach (BaseItem item in buttonItemStyle.SubItems)
            {
                ButtonItem buttonItem = item as ButtonItem;
                if (buttonItem != null)
                {
                    buttonItem.Checked = (buttonItem.Tag as eStyle?) == styleManager1.ManagerStyle;
                }
            }
        }

        private void styleColorPicker_SelectedColorChanged(object sender, EventArgs e)
        {
            var color = (sender as ColorPickerDropDown).SelectedColor;
            styleManager1.ManagerColorTint = color;
            ConfigManager.Reload();
            WcR2Config.Default.MainStyleColor = color;
            ConfigManager.Save();
        }
        #endregion

        #region 读取wz相关方法
        private Node createNode(Wz_Node wzNode)
        {
            if (wzNode == null)
                return null;

            Node parentNode = new Node(wzNode.Text) { Tag = new WeakReference(wzNode) };
            foreach (Wz_Node subNode in wzNode.Nodes)
            {
                Node subTreeNode = createNode(subNode);
                if (subTreeNode != null)
                    parentNode.Nodes.Add(subTreeNode);
            }
            return parentNode;
        }

        private void sortWzNode(Wz_Node wzNode)
        {
            this.sortWzNode(wzNode, WcR2Config.Default.SortWzByImgID);
        }

        private void sortWzNode(Wz_Node wzNode, bool sortByImgID)
        {
            if (wzNode.Nodes.Count > 1)
            {
                if (sortByImgID)
                {
                    wzNode.Nodes.SortByImgID();
                }
                else
                {
                    wzNode.Nodes.Sort();
                }
            }
            foreach (Wz_Node subNode in wzNode.Nodes)
            {
                sortWzNode(subNode, sortByImgID);
            }
        }
        #endregion

        #region wz提取右侧
        private void cmbItemAniNames_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.cmbItemAniNames.SelectedIndex > -1 && this.pictureBoxEx1.Items.Count > 0)
            {
                if (this.pictureBoxEx1.Items[0] is ISpineAnimator aniItem)
                {
                    string aniName = this.cmbItemAniNames.SelectedItem as string;
                    aniItem.SelectedAnimationName = aniName;
                    this.cmbItemAniNames.Tooltip = aniName;
                }
            }
        }

        private void cmbItemSkins_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.cmbItemSkins.SelectedIndex > -1 && this.pictureBoxEx1.Items.Count > 0)
            {
                if (this.pictureBoxEx1.Items[0] is ISpineAnimator aniItem)
                {
                    string skinName = this.cmbItemSkins.SelectedItem as string;
                    aniItem.SelectedSkin = skinName;
                    this.cmbItemSkins.Tooltip = skinName;
                }
            }
        }

        private void buttonItemSaveImage_Click(object sender, EventArgs e)
        {
            this.OnSaveImage(false);
        }

        private void buttonItemSaveWithOptions_Click(object sender, EventArgs e)
        {
            this.OnSaveImage(true);
        }

        private Node handleUol(Node currentNode, string uolString)
        {
            if (currentNode == null || currentNode.Parent == null || string.IsNullOrEmpty(uolString))
                return null;
            string[] dirs = uolString.Split('/');
            currentNode = currentNode.Parent;

            for (int i = 0; i < dirs.Length; i++)
            {
                string dir = dirs[i];
                if (dir == "..")
                {
                    currentNode = currentNode.Parent;
                }
                else
                {
                    bool find = false;
                    foreach (Node child in currentNode.Nodes)
                    {
                        if (child.Text == dir)
                        {
                            currentNode = child;
                            find = true;
                            break;
                        }
                    }
                    if (!find)
                        currentNode = null;
                }
                if (currentNode == null)
                    return null;
            }
            return currentNode;
        }

        private void labelItemAutoSaveFolder_Click(object sender, EventArgs e)
        {
            string dir = ImageHandlerConfig.Default.AutoSavePictureFolder;
            if (!string.IsNullOrEmpty(dir))
            {
                System.Diagnostics.Process.Start("explorer.exe", dir);
            }
        }

        private void buttonItemGif_Click(object sender, EventArgs e)
        {
            if (advTree3.SelectedNode == null)
                return;

            Wz_Node node = advTree3.SelectedNode.AsWzNode();
            string aniName = GetSelectedNodeImageName();

            //添加到动画控件
            var spineDetectResult = SpineLoader.Detect(node);
            if (spineDetectResult.Success)
            {
                var spineData = this.pictureBoxEx1.LoadSpineAnimation(spineDetectResult);

                if (spineData != null)
                {
                    this.pictureBoxEx1.ShowAnimation(spineData);
                    var aniItem = this.pictureBoxEx1.Items[0] as ISpineAnimator;

                    this.cmbItemAniNames.Items.Clear();
                    this.cmbItemAniNames.Items.Add("");
                    this.cmbItemAniNames.Items.AddRange(aniItem.Animations.ToArray());
                    this.cmbItemAniNames.SelectedIndex = 0;

                    this.cmbItemSkins.Visible = true;
                    this.cmbItemSkins.Items.Clear();
                    this.cmbItemSkins.Items.AddRange(aniItem.Skins.ToArray());
                    this.cmbItemSkins.SelectedIndex = aniItem.Skins.IndexOf(aniItem.SelectedSkin);
                }
            }
            else
            {
                var options = (sender == this.buttonItemExtractGifEx) ? FrameAnimationCreatingOptions.ScanAllChildrenFrames: default;
                var frameData = this.pictureBoxEx1.LoadFrameAnimation(node, options);

                if (frameData != null)
                {
                    this.pictureBoxEx1.ShowAnimation(frameData);
                    this.cmbItemAniNames.Items.Clear();
                    this.cmbItemSkins.Visible = false;
                }
            }
            this.pictureBoxEx1.PictureName = aniName;
        }

        private string GetSelectedNodeImageName()
        {
            Wz_Node node = advTree3.SelectedNode.AsWzNode();

            string aniName;
            switch (ImageHandlerConfig.Default.ImageNameMethod.Value)
            {
                default:
                case ImageNameMethod.Default:
                    advTree3.PathSeparator = ".";
                    aniName = advTree3.SelectedNode.FullPath;
                    break;

                case ImageNameMethod.PathToImage:
                    aniName = node.FullPath.Replace('\\', '.');
                    break;

                case ImageNameMethod.PathToWz:
                    aniName = node.FullPathToFile.Replace('\\', '.');
                    break;
            }

            return aniName;
        }

        private void buttonItemGifSetting_Click(object sender, EventArgs e)
        {
            FrmGifSetting frm = new FrmGifSetting();
            frm.Load(ImageHandlerConfig.Default);
            frm.FFmpegBinPathHint = FFmpegEncoder.DefaultExecutionFileName;
            frm.FFmpegArgumentHint = FFmpegEncoder.DefaultArgumentFormat;
            frm.FFmpegDefaultExtensionHint = FFmpegEncoder.DefaultOutputFileExtension;
            if (frm.ShowDialog() == DialogResult.OK)
            {
                ConfigManager.Reload();
                frm.Save(ImageHandlerConfig.Default);
                ConfigManager.Save();
            }
        }

        private void buttonItemAutoSave_Click(object sender, EventArgs e)
        {
            ConfigManager.Reload();
            ImageHandlerConfig.Default.AutoSaveEnabled = buttonItemAutoSave.Checked;
            ConfigManager.Save();
        }

        private void buttonItemAutoSaveFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "请选择自动保存图片的文件夹...";
                dlg.SelectedPath = ImageHandlerConfig.Default.AutoSavePictureFolder;
                if (DialogResult.OK == dlg.ShowDialog())
                {
                    labelItemAutoSaveFolder.Text = dlg.SelectedPath;
                    ConfigManager.Reload();
                    ImageHandlerConfig.Default.AutoSavePictureFolder = dlg.SelectedPath;
                    ConfigManager.Save();
                }
            }
        }

        private void OnSaveImage(bool options)
        {
            if (this.pictureBoxEx1.Items.Count <= 0)
            {
                return;
            }

            var aniItem = this.pictureBoxEx1.Items[0];
            var frameData = (aniItem as FrameAnimator)?.Data;
            if (frameData != null && frameData.Frames.Count == 1 
                && frameData.Frames[0].A0 == 255 && frameData.Frames[0].A1 == 255 && frameData.Frames[0].Delay == 0)
            {
                // save still picture as png
                this.OnSavePngFile(frameData.Frames[0]);
            }
            else
            {
                // save as gif/apng
                this.OnSaveGifFile(aniItem, options);
            }
        }

        private void OnSavePngFile(Frame frame)
        {
            if (frame.Png != null)
            {
                var config = ImageHandlerConfig.Default;
                string pngFileName = pictureBoxEx1.PictureName + ".png";

                if (config.AutoSaveEnabled)
                {
                    pngFileName = Path.Combine(config.AutoSavePictureFolder, string.Join("_", pngFileName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.None)));
                }
                else
                {
                    var dlg = new SaveFileDialog();
                    dlg.Filter = "Png图片(*.png)|*.png|全部文件(*.*)|*.*";
                    dlg.FileName = pngFileName;
                    if (dlg.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }

                    pngFileName = dlg.FileName;
                }

                using (var bmp = frame.Png.ExtractPng())
                {
                    bmp.Save(pngFileName, System.Drawing.Imaging.ImageFormat.Png);
                }
                labelItemStatus.Text = "图片保存于" + pngFileName;
            }
            else
            {
                labelItemStatus.Text = "没有文件被保存。";
            }
        }

        private void OnSaveGifFile(AnimationItem aniItem, bool options)
        {
            var config = ImageHandlerConfig.Default;
            using var encoder = AnimateEncoderFactory.CreateEncoder(config);
            var cap = encoder.Compatibility;

            string aniName = this.cmbItemAniNames.SelectedItem as string;
            string aniFileName = pictureBoxEx1.PictureName
                    + (string.IsNullOrEmpty(aniName) ? "" : ("." + aniName))
                    + cap.DefaultExtension;

            if (config.AutoSaveEnabled)
            {
                var fullFileName = Path.Combine(config.AutoSavePictureFolder, string.Join("_", aniFileName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.None)));
                int i = 1;
                while (File.Exists(fullFileName))
                {
                    fullFileName = Path.Combine(config.AutoSavePictureFolder, string.Format("{0}({1}){2}",
                        Path.GetFileNameWithoutExtension(aniFileName), i, Path.GetExtension(aniFileName)));
                    i++;
                }
                aniFileName = fullFileName;
            }
            else
            {
                var dlg = new SaveFileDialog();
                string extensionFilter = string.Join(";", cap.SupportedExtensions.Select(ext => $"*{ext}"));
                dlg.Filter = string.Format("{0} Supported Files ({1})|{1}|All files (*.*)|*.*", encoder.Name, extensionFilter);
                dlg.FileName = aniFileName;

                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                aniFileName = dlg.FileName;
            }

            var clonedAniItem = (AnimationItem)aniItem.Clone();
            if (this.pictureBoxEx1.SaveAsGif(clonedAniItem, aniFileName, config, encoder, options))
            {
                labelItemStatus.Text = "图片保存于" + aniFileName;
            }
        }
        #endregion

        #region File菜单的事件
        private void btnItemOpenWz_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "请选择冒险岛wz文件...";
                dlg.Filter = "MapleStory Data File(Base.wz, *.wz, *.ms)|*.wz;*.ms";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    openWz(dlg.FileName);
                }
            }
        }

        private void openWz(string wzFilePath)
        {
            foreach (Wz_Structure wzs in openedWz)
            {
                foreach (Wz_File wz_f in wzs.wz_files)
                {
                    if (string.Compare(wz_f.Header.FileName, wzFilePath, true) == 0)
                    {
                        MessageBoxEx.Show("已经打开的wz。", "喵~");
                        return;
                    }
                }
            }

            Wz_Structure wz = new Wz_Structure();
            QueryPerformance.Start();
            advTree1.BeginUpdate();
            try
            {
                if (string.Equals(Path.GetExtension(wzFilePath), ".ms", StringComparison.OrdinalIgnoreCase))
                {
                    wz.LoadMsFile(wzFilePath);
                }
                else if (wz.IsKMST1125WzFormat(wzFilePath))
                {
                    wz.LoadKMST1125DataWz(wzFilePath);
                    if (string.Equals(Path.GetFileName(wzFilePath), "Base.wz", StringComparison.OrdinalIgnoreCase))
                    {
                        string packsDir = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(wzFilePath)), "Packs");
                        if (Directory.Exists(packsDir))
                        {
                            foreach (var msFile in Directory.GetFiles(packsDir, "*.ms"))
                            {
                                wz.LoadMsFile(msFile);
                            }
                        }
                    }
                }
                else
                {
                    wz.Load(wzFilePath, true);
                }
                
                if (WcR2Config.Default.SortWzOnOpened)
                {
                    sortWzNode(wz.WzNode);
                }
                Node node = createNode(wz.WzNode);
                node.Expand();
                advTree1.Nodes.Add(node);
                this.openedWz.Add(wz);
                OnWzOpened(new WzStructureEventArgs(wz)); //触发事件
                QueryPerformance.End();
                labelItemStatus.Text = "读取成功,用时" + (Math.Round(QueryPerformance.GetLastInterval(), 4) * 1000) + "ms,共读取" + wz.img_number + "img.";

                ConfigManager.Reload();
                WcR2Config.Default.RecentDocuments.Remove(wzFilePath);
                WcR2Config.Default.RecentDocuments.Insert(0, wzFilePath);
                ConfigManager.Save();
                refreshRecentDocItems();
            }
            catch (FileNotFoundException)
            {
                MessageBoxEx.Show("文件没有找到", "嗯?");
            }
            catch (Exception ex)
            {
                MessageBoxEx.Show(ex.ToString(), "嗯?");
                wz.Clear();
            }
            finally
            {
                advTree1.EndUpdate();
            }
        }

        private void btnItemOpenImg_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = "请选择冒险岛img文件...";
                dlg.Filter = "*.img|*.img|*.wz|*.wz";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    openImg(dlg.FileName);
                }
            }
        }

        private void openImg(string imgFileName)
        {
            foreach (Wz_Structure wzs in openedWz)
            {
                foreach (Wz_File wz_f in wzs.wz_files)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(wz_f.Header.FileName, imgFileName))
                    {
                        MessageBoxEx.Show("已经打开的wz。", "喵~");
                        return;
                    }
                }
            }

            Wz_Structure wz = new Wz_Structure();
            var sw = Stopwatch.StartNew();
            advTree1.BeginUpdate();
            try
            {
                wz.LoadImg(imgFileName);

                Node node = createNode(wz.WzNode);
                node.Expand();
                advTree1.Nodes.Add(node);
                this.openedWz.Add(wz);
                OnWzOpened(new WzStructureEventArgs(wz)); //触发事件
                sw.Stop();
                labelItemStatus.Text = $"读取成功,用时{sw.ElapsedMilliseconds}ms.";
                refreshRecentDocItems();
            }
            catch (FileNotFoundException)
            {
                MessageBoxEx.Show("文件没有找到", "嗯?");
            }
            catch (Exception ex)
            {
                MessageBoxEx.Show(ex.ToString(), "嗯?");
                wz.Clear();
            }
            finally
            {
                advTree1.EndUpdate();
            }
        }

        private void buttonItemClose_Click(object sender, EventArgs e)
        {
            if (advTree1.SelectedNode == null)
            {
                MessageBoxEx.Show("没有选中要关闭的wz.", "喵~");
                return;
            }
            Node baseWzNode = advTree1.SelectedNode;
            while (baseWzNode.Parent != null)
                baseWzNode = baseWzNode.Parent;
            if (baseWzNode.Text.ToLower() == "list.wz")
            {
                advTree1.Nodes.Remove(baseWzNode);
                labelItemStatus.Text = "已经关闭list.wz...";
                return;
            }

            Wz_File wz_f = advTree1.SelectedNode.AsWzNode()?.GetNodeWzFile();
            if (wz_f == null)
            {
                MessageBoxEx.Show("没有正确选择要关闭的wz。", "喵~");
                return;
            }
            Wz_Structure wz = wz_f.WzStructure;

            advTree1.Nodes.Remove(baseWzNode);

            listViewExWzDetail.Items.Clear();

            Wz_Image image = null;
            if (advTree2.Nodes.Count > 0
                && (image = advTree2.Nodes[0].AsWzNode()?.GetValue<Wz_Image>()) != null
                && image.WzFile.WzStructure == wz)
            {
                advTree2.Nodes.Clear();
            }

            if (advTree3.Nodes.Count > 0
                && (image = advTree3.Nodes[0].AsWzNode()?.GetNodeWzImage()) != null
                && image.WzFile.WzStructure == wz)
            {
                advTree3.Nodes.Clear();
            }

            OnWzClosing(new WzStructureEventArgs(wz));
            wz.Clear();
            if (this.openedWz.Remove(wz))
                labelItemStatus.Text = "已经关闭所选wz...";
            else
                labelItemStatus.Text = "wz已经关闭,但是发生了诡异的错误...";
        }

        private void buttonItemCloseAll_Click(object sender, EventArgs e)
        {
            advTree1.ClearAndDisposeAllNodes();
            advTree1.ClearLayoutCellInfo();
            advTree2.ClearAndDisposeAllNodes();
            advTree2.ClearLayoutCellInfo();
            advTree3.ClearAndDisposeAllNodes();
            advTree3.ClearLayoutCellInfo();
            foreach (Wz_Structure wz in openedWz)
            {
                OnWzClosing(new WzStructureEventArgs(wz));
                wz.Clear();
            }
            openedWz.Clear();
            CharaSimLoader.ClearAll();
            stringLinker.Clear();
            labelItemStatus.Text = "已经清理全部已读取资源...";
            GC.Collect();
        }

        private void refreshRecentDocItems()
        {
            List<BaseItem> items = new List<BaseItem>();
            foreach (BaseItem item in galleryContainerRecent.SubItems)
            {
                if (item is ButtonItem)
                {
                    items.Add(item);
                }
            }
            galleryContainerRecent.SubItems.RemoveRange(items.ToArray());
            items.Clear();

            foreach (var doc in WcR2Config.Default.RecentDocuments)
            {
                ButtonItem item = new ButtonItem() { Text = "&" + (items.Count + 1) + ". " + Path.GetFileName(doc), Tooltip = doc, Tag = doc };
                item.Click += new EventHandler(buttonItemRecentDocument_Click);
                items.Add(item);
            }
            galleryContainerRecent.SubItems.AddRange(items.ToArray());
        }

        void buttonItemRecentDocument_Click(object sender, EventArgs e)
        {
            ButtonItem btnItem = sender as ButtonItem;
            string path;
            if (btnItem == null || (path = btnItem.Tag as string) == null)
                return;
            openWz(path);
        }
        #endregion

        #region wzView和提取的事件和方法
        private void advTree1_DragEnter(object sender, DragEventArgs e)
        {
            string[] types = e.Data.GetFormats();
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    if (Path.GetExtension(file) != ".wz")
                    {
                        e.Effect = DragDropEffects.None;
                        return;
                    }
                }
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void advTree1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string file in files)
                {
                    openWz(file);
                }
            }
        }

        private void advTree1_AfterNodeSelect(object sender, AdvTreeNodeEventArgs e)
        {
            Wz_Node selectedNode = e.Node.AsWzNode();

            if (selectedNode == null)
            {
                return;
            }

            listViewExWzDetail.BeginUpdate();
            listViewExWzDetail.Items.Clear();

            if (selectedNode.Value == null)
            {
                listViewExWzDetail.Items.Add(new ListViewItem(new string[] { "Dir Name", Path.GetFileName(e.Node.Text) }));
                autoResizeColumns(listViewExWzDetail);
            }
            else if (selectedNode.Value is Wz_File wzFile)
            {
                listViewExWzDetail.Items.Add(new ListViewItem(new string[] { "File Name", wzFile.Header.FileName }));
                listViewExWzDetail.Items.Add(new ListViewItem(new string[] { "File Size", wzFile.Header.FileSize + " bytes" }));
                listViewExWzDetail.Items.Add(new ListViewItem(new string[] { "Copyright", wzFile.Header.Copyright }));
                listViewExWzDetail.Items.Add(new ListViewItem(new string[] { "Version", wzFile.GetMergedVersion().ToString() }));
                listViewExWzDetail.Items.Add(new ListViewItem(new string[] { "Wz Type", wzFile.IsSubDir ? "SubDir" : wzFile.Type.ToString() }));

                foreach (Wz_File subFile in wzFile.MergedWzFiles)
                {
                    listViewExWzDetail.Items.Add(" ");
                    listViewExWzDetail.Items.Add(new ListViewItem(new string[] { "File Name", subFile.Header.FileName }));
                    listViewExWzDetail.Items.Add(new ListViewItem(new string[] { "File Size", subFile.Header.FileSize + " bytes" }));
                    listViewExWzDetail.Items.Add(new ListViewItem(new string[] { "Copyright", subFile.Header.Copyright }));
                    listViewExWzDetail.Items.Add(new ListViewItem(new string[] { "Version", subFile.Header.WzVersion.ToString() }));
                }

                autoResizeColumns(listViewExWzDetail);
            }
            else if (selectedNode.Value is Wz_Image wzImage)
            {
                listViewExWzDetail.Items.Add(new ListViewItem(new string[] { "Image Name", wzImage.Name }));
                listViewExWzDetail.Items.Add(new ListViewItem(new string[] { "Image Size", wzImage.Size + " bytes" }));
                listViewExWzDetail.Items.Add(new ListViewItem(new string[] { "Image Offset", wzImage.Offset + " bytes" }));
                listViewExWzDetail.Items.Add(new ListViewItem(new string[] { "Path", wzImage.Node.FullPathToFile }));
                listViewExWzDetail.Items.Add(new ListViewItem(new string[] { "Check Sum", wzImage.Checksum.ToString() }));
                autoResizeColumns(listViewExWzDetail);

                advTree2.ClearAndDisposeAllNodes();
                //advTree2.Nodes.Clear();

                QueryPerformance.Start();
                try
                {
                    Exception ex;
                    if (wzImage.TryExtract(out ex))
                    {
                        advTree2.Nodes.Add(createNode(wzImage.Node));
                        advTree2.Nodes[0].Expand();
                        QueryPerformance.End();
                        double ms = (Math.Round(QueryPerformance.GetLastInterval(), 4) * 1000);

                        labelItemStatus.Text = "读取image成功~用时" + ms + "ms...";
                    }
                    else
                    {

                        labelItemStatus.Text = "读取image失败..." + ex.Message;
                    }
                }
                catch (Exception ex)
                {
                    labelItemStatus.Text = "读取image失败:" + ex.Message;
                }
            }
            listViewExWzDetail.EndUpdate();
        }

        private void autoResizeColumns(ListViewEx listView)
        {
            listView.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            foreach (System.Windows.Forms.ColumnHeader column in listView.Columns)
            {
                column.Width += (int)(listView.Font.Size * 2);
            }
        }

        private void advTree2_NodeDoubleClick(object sender, TreeNodeMouseEventArgs e)
        {
            if (e.Node == null || e.Button != MouseButtons.Left)
                return;
            historyNodeList.Clear();
            advTree3.Nodes.Clear();

            var selectedNode = e.Node.AsWzNode();
            if (selectedNode != null)
            {
                advTree3.BeginUpdate();
                try
                {
                    var node = createNodeDetail(e.Node);
                    node.ExpandAll();
                    advTree3.Nodes.Add(node);
                    advTree3.SelectedNode = node;
                }
                finally
                {
                    advTree3.EndUpdate();
                }
            }
        }

        private Node createNodeDetail(Node parentNode)
        {
            Node newNode = new Node(parentNode.Text);
            newNode.Tag = parentNode.Tag;
            Wz_Node wzNode = newNode.AsWzNode();
            if (wzNode != null)
            {
                newNode.Cells.Add(new Cell(wzNode.Value == null ? "<" + parentNode.Nodes.Count + ">" : getValueString(wzNode.Value)));
                newNode.Cells.Add(new Cell(wzNode.Value == null ? null : wzNode.Value.GetType().Name));
                newNode.ImageKey = wzNode.Value == null ? "dir" : (getValueImageKey(wzNode.Value) ?? "num");
            }
            foreach (Node subNode in parentNode.Nodes)
            {
                newNode.Nodes.Add(createNodeDetail(subNode));
            }
            return newNode;
        }

        private string getValueString(object value)
        {
            switch (value)
            {
                case Wz_Png png:
                    return $"png {png.Width}*{png.Height} ({png.Form})";

                case Wz_Vector vector:
                    return $"({vector.X}, {vector.Y})";

                case Wz_Uol uol:
                    return uol.Uol;

                case Wz_Sound sound:
                    return $"sound {sound.Ms}ms";

                case Wz_Image img:
                    return $"<{img.Node.Nodes.Count}>";

                case Wz_RawData rawData:
                    return $"rawdata {rawData.Length}";

                case Wz_Convex convex:
                    return $"convex [{convex.Points.Length}]";

                case Wz_Video video:
                    return $"video {video.Length}";

                default:
                    string cellVal = Convert.ToString(value);
                    if (cellVal != null && cellVal.Length > 50)
                    {
                        cellVal = cellVal.Substring(0, 50);
                    }
                    return cellVal;
            }
        }

        private string getValueImageKey(object value)
        {
            return value switch
            {
                string => "str",
                short or int or long or float or double=> "num",
                Wz_Png => "png",
                Wz_Vector => "vector",
                Wz_Uol => "uol",
                Wz_Sound sound => sound.SoundType == Wz_SoundType.Binary ? "rawdata" : "mp3",
                Wz_Image => "img",
                Wz_RawData => "rawdata",
                Wz_Convex => "convex",
                Wz_Video => "video",
                _ => null
            };
        }

        private void advTree3_AfterNodeSelect(object sender, AdvTreeNodeEventArgs e)
        {
            if (e.Node == null)
                return;

            if (!historySelecting && (historyNodeList.Count == 0 || e.Node != historyNodeList.Current))
            {
                historyNodeList.Add(e.Node);
            }
            else
            {
                historySelecting = false;
            }

            Wz_Node selectedNode = e.Node.AsWzNode();
            if (selectedNode == null)
                return;

            switch (selectedNode.Value)
            {
                case Wz_Png png:
                    pictureBoxEx1.PictureName = GetSelectedNodeImageName();
                    pictureBoxEx1.ShowImage(png);
                    this.cmbItemAniNames.Items.Clear();
                    advTree3.PathSeparator = ".";
                    textBoxX1.Text = "dataLength: " + png.DataLength + " bytes\r\n" +
                        "offset: " + png.Offset + "\r\n" +
                        "size: " + png.Width + "*" + png.Height + "\r\n" +
                        "png format: " + png.Form;
                    break;

                case Wz_Vector vector:
                    textBoxX1.Text = "x: " + vector.X + " px\r\n" +
                        "y: " + vector.Y + " px";
                    break;

                case Wz_Convex convex:
                    var sb = new StringBuilder();
                    for (int i = 0; i < convex.Points.Length; i++)
                    {
                        if (i > 0) sb.AppendLine();
                        sb.AppendFormat("({0}, {1})", convex.Points[i].X, convex.Points[i].Y);
                    }
                    textBoxX1.Text = sb.ToString();
                    break;

                case Wz_Uol uol:
                    textBoxX1.Text = "uolPath: " + uol.Uol;
                    break;

                case Wz_Sound sound:
                    preLoadSound(sound, selectedNode.Text);
                    textBoxX1.Text = "dataLength: " + sound.DataLength + " bytes\r\n" +
                        "offset: " + sound.Offset + "\r\n" +
                        "duration: " + sound.Ms + " ms\r\n" +
                        "channels: " + sound.Channels + "\r\n" +
                        "freq: " + sound.Frequency + " Hz\r\n" +
                        "type: " + sound.SoundType.ToString();
                    break;

                case Wz_Image:
                    //do nothing;
                    break;

                case Wz_RawData rawData:
                    textBoxX1.Text = "dataLength: " + rawData.Length + " bytes\r\n" +
                        "offset: " + rawData.Offset;
                    break;

                case Wz_Video video:
                    textBoxX1.Text = "dataLength: " + video.Length + " bytes\r\n" +
                        "offset: " + video.Offset;
                    break;

                default:
                    string valueStr = Convert.ToString(selectedNode.Value);
                    if (valueStr != null && valueStr.Contains("\n") && !valueStr.Contains("\r\n"))
                    {
                        valueStr = valueStr.Replace("\n", "\r\n");
                    }
                    textBoxX1.Text = Convert.ToString(valueStr);

                    switch (selectedNode.Text)
                    {
                        case "source":
                        case "_inlink":
                        case "_outlink":
                            {
                                var parentNode = selectedNode.ParentNode;
                                if (parentNode != null && parentNode.Value is Wz_Png)
                                {
                                    var linkNode = parentNode.GetLinkedSourceNode(PluginManager.FindWz);
                                    var png = linkNode.GetValueEx<Wz_Png>(null);

                                    if (png != null)
                                    {
                                        pictureBoxEx1.PictureName = GetSelectedNodeImageName();
                                        pictureBoxEx1.ShowImage(png);
                                        this.cmbItemAniNames.Items.Clear();
                                        advTree3.PathSeparator = ".";
                                        textBoxX1.AppendText("\r\n\r\ndataLength: " + png.DataLength + " bytes\r\n" +
                                        "offset: " + png.Offset + "\r\n" +
                                        "size: " + png.Width + "*" + png.Height + "\r\n" +
                                            "png format: " + png.Form);
                                    }
                                }
                            }
                            break;
                    }
                    break;
            }
        }

        private void pictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            /*
            if (pictureBox1.Image != null && e.Button == MouseButtons.Left)
            {
                string tempFile = Path.Combine(Path.GetTempPath(), Convert.ToString(pictureBox1.Tag));
                switch (Path.GetExtension(tempFile))
                {
                    case ".png":
                        pictureBox1.Image.Save(tempFile, System.Drawing.Imaging.ImageFormat.Png);
                        System.Diagnostics.Process.Start(tempFile);
                        break;
                    case ".gif":
                        pictureBox1.Image.Save(tempFile, System.Drawing.Imaging.ImageFormat.Gif);
                        System.Diagnostics.Process.Start(tempFile);
                        break;
                    default:
                        MessageBoxEx.Show("不识别的文件名：" + tempFile, "喵~");
                        break;
                }
            }*/
        }

        private void listViewExString_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.listViewExStringFind();
            }
        }

        private void listViewExString_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.listViewExStringFind();
            }
            else if (e.KeyCode == Keys.C && e.Control)
            {
                this.listViewExStringCopy();
            }
        }

        private void listViewExStringFind()
        {
            if (listViewExString.SelectedItems.Count == 0 || advTree1.Nodes.Count == 0)
            {
                return;
            }
            string id = listViewExString.SelectedItems[0].Text;
            string nodePath = listViewExString.SelectedItems[0].SubItems[3].Text;
            List<string[]> objPathList = detectObjPathByStringPath(id, nodePath);

            //分离wz路径和img路径
            foreach (string[] fullPath in objPathList)
            {
                //寻找所有可能的wzfile
                List<Wz_Node> allWzFile = new List<Wz_Node>();
                Wz_Type wzType = ParseType(fullPath[0]);
                foreach (var wzs in this.openedWz)
                {
                    foreach (var wzf in wzs.wz_files)
                    {
                        if (wzf.Type == wzType && wzf.OwnerWzFile == null)
                        {
                            allWzFile.Add(wzf.Node);
                        }
                    }
                }

                //开始搜索
                foreach (var wzFileNode in allWzFile)
                {
                    Wz_Node node = SearchNode(wzFileNode, fullPath, 1);
                    if (node != null)
                    {
                        OnSelectedWzNode(node); //遇到第一个 选中 返回
                        return;
                    }
                }
            }

            //失败
            string path;
            if (objPathList.Count == 1)
            {
                path = string.Join("\\", objPathList[0]);
            }
            else
            {
                path = "(" + objPathList.Count + ")个节点";
            }
            labelItemStatus.Text = "无法找到imageNode: " + path;
        }

        private Wz_Node SearchNode(Wz_Node parent, string[] path, int startIndex)
        {
            if (startIndex >= path.Length)
            {
                return null;
            }
            if (parent.Value is Wz_Image)
            {
                Wz_Image img = parent.GetValue<Wz_Image>();
                if (!img.TryExtract())
                {
                    return null;
                }
                parent = img.Node;
            }
            string nodeName = path[startIndex];
            if (!string.IsNullOrEmpty(nodeName))
            {
                Wz_Node child = parent.FindNodeByPath(false, true, nodeName);
                if (child != null)
                {
                    return (startIndex == path.Length - 1) ? child : SearchNode(child, path, startIndex + 1);
                }
            }
            else //遍历全部
            {
                foreach (Wz_Node child in parent.Nodes)
                {
                    if (child.Nodes.Count == 0) //只过滤文件夹 未来有需求再改
                    {
                        continue;
                    }
                    Wz_Node find = SearchNode(child, path, startIndex + 1);
                    if (find != null)
                    {
                        return (startIndex == path.Length - 1) ? null : find;
                    }

                }
            }

            return null;
        }

        private bool OnSelectedWzNode(Wz_Node wzNode)
        {
            Wz_File wzFile = wzNode.GetNodeWzFile();
            string[] path = wzNode.FullPathToFile.Split('\\');
            if (wzFile == null)
            {
                return false;
            }

            Node treeNode = findWzFileTreeNode(wzFile);
            if (treeNode == null)
            {
                return false;
            }

            for (int i = 1; i < path.Length; i++)
            {
                Node find = null;
                foreach (Node child in treeNode.Nodes)
                {
                    if (child.Text == path[i])
                    {
                        find = child;
                        break;
                    }
                }
                if (find == null)
                {
                    return false;
                }

                if (find.AsWzNode()?.Value is Wz_Image)
                {
                    advTree1.SelectedNode = find;
                    if (advTree2.Nodes.Count > 0)
                    {
                        treeNode = advTree2.Nodes[0];
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    treeNode = find;
                }
            }

            advTree2.SelectedNode = treeNode;
            return true;
        }

        private void listViewExStringCopy()
        {
            if (listViewExString.SelectedItems.Count == 0 || advTree1.Nodes.Count == 0)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();
            foreach (ListViewItem.ListViewSubItem item in listViewExString.SelectedItems[0].SubItems)
            {
                sb.Append(item.Text).Append(" ");
            }
            sb.Remove(sb.Length - 1, 1);
            Clipboard.SetText(sb.ToString(), TextDataFormat.UnicodeText);
            labelItemStatus.Text = "已复制string条目到剪切板。";
        }

        private List<string[]> detectObjPathByStringPath(string id, string stringNodePath)
        {
            List<string[]> pathList = new List<string[]>();

            List<string> wzPath = new List<string>();
            List<string> imagePath = new List<string>();

            Action addPath = () =>
            {
                List<string> fullPath = new List<string>(wzPath.Count + imagePath.Count);
                fullPath.AddRange(wzPath);
                fullPath.AddRange(imagePath);
                pathList.Add(fullPath.ToArray());
            };

            string[] pathArray = stringNodePath.Split('\\');
            switch (pathArray[0])
            {
                case "Cash.img":
                case "Consume.img":
                case "Etc.img":
                case "Pet.img":
                    wzPath.Add("Item");
                    wzPath.Add(pathArray[0].Substring(0, pathArray[0].IndexOf(".img")));
                    if (pathArray[0] == "Pet.img")
                    {
                        wzPath.Add(id.TrimStart('0') + ".img");
                    }
                    else
                    {
                        id = id.PadLeft(8, '0');
                        wzPath.Add(id.Substring(0, 4) + ".img");
                        imagePath.Add(id);
                    }
                    addPath();
                    break;

                case "Ins.img": //KMST1066
                    wzPath.Add("Item");
                    wzPath.Add("Install");
                    wzPath.Add("");
                    id = id.PadLeft(8, '0');
                    imagePath.Add(id);
                    for (int len = 4; len <= 6; len++)
                    {
                        wzPath[2] = id.Substring(0, len) + ".img";
                        addPath();
                    }
                    break;

                case "Eqp.img":
                    wzPath.Add("Character");
                    if (pathArray[2] == "Taming")
                    {
                        wzPath.Add("TamingMob");
                    }
                    else
                    {
                        wzPath.Add(pathArray[2]);
                    }
                    wzPath.Add(id.PadLeft(8, '0') + ".img");
                    addPath();
                    //往往这个不靠谱。。 加一个任意门备用
                    wzPath[1] = "";
                    addPath();
                    break;

                case "Map.img":
                    id = id.PadLeft(9, '0');
                    wzPath.AddRange(new string[] { "Map", "Map", "Map" + id[0], id + ".img" });
                    addPath();
                    break;

                case "Mob.img":
                    wzPath.Add("Mob");
                    wzPath.Add(id.PadLeft(7, '0') + ".img");
                    addPath();
                    break;

                case "Npc.img":
                    wzPath.Add("Npc");
                    wzPath.Add(id.PadLeft(7, '0') + ".img");
                    addPath();
                    break;

                case "Skill.img":
                    id = id.PadLeft(7, '0');
                    wzPath.Add("Skill");
                    //old skill
                    wzPath.Add(id.Substring(0, id.Length - 4) + ".img");
                    imagePath.Add("skill");
                    imagePath.Add(id);
                    addPath();
                    if (Regex.IsMatch(id, @"80\d{6}")) //kmst new skill
                    {
                        wzPath[1] = id.Substring(0, 6) + ".img";
                        addPath();
                    }
                    break;
                default:
                    break;
            }

            return pathList;
        }

        /// <summary>
        /// 通过给定的wz名称，在advTree1中寻找第一个对应的wz_file节点。
        /// </summary>
        /// <param Name="wzName">要寻找的wz名称，不包含".wz"后缀。</param>
        /// <returns></returns>
        private Node findWzFileTreeNode(string wzName)
        {
            Wz_Type type = ParseType(wzName);
            if (type == Wz_Type.Unknown)
            {
                return null;
            }

            foreach (var wzs in this.openedWz)
            {
                foreach (var wzf in wzs.wz_files)
                {
                    if (wzf.Type == type)
                    {
                        Node node = findWzFileTreeNode(wzf);
                        if (node != null)
                        {
                            return node;
                        }
                    }
                }
            }

            return null;
        }

        private Wz_Type ParseType(string wzName)
        {
            Wz_Type type;
            try
            {
                type = (Wz_Type)Enum.Parse(typeof(Wz_Type), wzName, true);
            }
            catch
            {
                type = Wz_Type.Unknown;
            }

            return type;
        }

        private Node findWzFileTreeNode(Wz_File wzFile)
        {
            foreach (Node baseNode in advTree1.Nodes)
            {
                Wz_File wz_f = baseNode.AsWzNode()?.Value as Wz_File;
                if (wz_f != null)
                {
                    if (wz_f == wzFile)
                    {
                        return baseNode;
                    }
                    else if (wz_f.Type == Wz_Type.Base)
                    {
                        foreach (Node wzNode in baseNode.Nodes)
                        {
                            if ((wz_f = wzNode.AsWzNode()?.Value as Wz_File) != null && wz_f == wzFile)
                            {
                                return wzNode;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private Node findChildTreeNode(Node parent, string[] path)
        {
            if (parent == null || path == null)
                return null;
            for (int i = 0; i < path.Length; i++)
            {
                bool find = false;
                foreach (Node subNode in parent.Nodes)
                {
                    if (subNode.Text == path[i])
                    {
                        parent = subNode;
                        find = true;
                        break;
                    }
                }
                if (!find)
                {
                    return null;
                }
            }
            return parent;
        }
        #endregion

        #region contextMenuStrip1
        private void tsmi1Sort_Click(object sender, EventArgs e)
        {
            if (openedWz.Count > 0)
            {
                var sw = Stopwatch.StartNew();
                advTree1.BeginUpdate();
                try
                {
                    advTree1.ClearAndDisposeAllNodes();
                    foreach (Wz_Structure wz in openedWz)
                    {
                        sortWzNode(wz.WzNode);
                        Node node = createNode(wz.WzNode);
                        node.Expand();
                        advTree1.Nodes.Add(node);
                    }
                }
                finally
                {
                    advTree1.EndUpdate();
                    sw.Stop();
                }
                GC.Collect();
                labelItemStatus.Text = $"排序成功,用时{sw.ElapsedMilliseconds}ms.";
            }
            else
            {
                labelItemStatus.Text = "没有打开的wz";
            }
        }

        private void tsmi1Export_Click(object sender, EventArgs e)
        {
            Wz_Image img = advTree1.SelectedNode?.AsWzNode()?.GetValue<Wz_Image>();
            if (img == null)
            {
                MessageBoxEx.Show("没有选中一个用于导出的wz_img。");
                return;
            }
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = ".img";
            dlg.FileName = img.Name;
            dlg.Filter = "*.img|*.img";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write);
                    var s = img.OpenRead();
                    s.Position = 0;
                    s.CopyTo(fs);
                    fs.Close();
                    labelItemStatus.Text = img.Name + "导出完毕。";
                }
                catch (Exception ex)
                {
                    fs?.Close();
                    MessageBoxEx.Show(ex.ToString(), "错了");
                }
            }
        }

        private void tsmi1DumpAsXml_Click(object sender, EventArgs e)
        {
            Wz_Image img = advTree1.SelectedNode?.AsWzNode()?.GetValue<Wz_Image>();
            if (img == null)
            {
                MessageBoxEx.Show("没有选中一个用于导出的wz_img。");
                return;
            }
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = ".xml";
            dlg.Filter = "*.xml|*.xml";
            dlg.FileName = img.Node.FullPathToFile.Replace('\\', '.') + ".xml";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write);
                    var xsetting = new XmlWriterSettings()
                    {
                        CloseOutput = false,
                        Indent = true,
                        Encoding = Encoding.UTF8,
                        CheckCharacters = true,
                        NewLineChars = Environment.NewLine,
                        NewLineOnAttributes = false,
                    };
                    var writer = XmlWriter.Create(fs, xsetting);
                    writer.WriteStartDocument(true);
                    img.Node.DumpAsXml(writer);
                    writer.WriteEndDocument();
                    writer.Close();

                    labelItemStatus.Text = img.Name + "导出完毕。";
                }
                catch (Exception ex)
                {
                    MessageBoxEx.Show(ex.ToString(), "错了");
                }
                finally
                {
                    if (fs != null)
                    {
                        fs.Close();
                    }
                }
            }
        }
        #endregion

        #region Tools菜单事件和方法
        private void buttonItemSearchWz_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxItemSearchWz.Text))
                return;
            if (comboBoxItem1.SelectedIndex == -1)
            {
                comboBoxItem1.SelectedIndex = 0;
            }

            switch (comboBoxItem1.SelectedIndex)
            {
                case 0:
                    searchAdvTree(advTree1, 0, textBoxItemSearchWz.Text, checkBoxItemExact1.Checked, checkBoxItemRegex1.Checked);
                    break;
                case 1:
                    searchAdvTree(advTree2, 0, textBoxItemSearchWz.Text, checkBoxItemExact1.Checked, checkBoxItemRegex1.Checked);
                    break;
                case 2:
                    searchAdvTree(advTree3, 1, textBoxItemSearchWz.Text, checkBoxItemExact1.Checked, checkBoxItemRegex1.Checked);
                    break;
            }
        }

        private void searchAdvTree(AdvTree advTree, int cellIndex, string searchText, bool exact, bool regex)
        {
            if (string.IsNullOrEmpty(searchText))
                return;

            try
            {
                Node searchNode = searchAdvTree(advTree, cellIndex, searchText, exact, regex, true);
                advTree.SelectedNode = searchNode;
                if (searchNode == null)
                    MessageBoxEx.Show("已经搜索到末尾。", "喵呜~");
            }
            catch (Exception ex)
            {
                MessageBoxEx.Show(this, ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Node searchAdvTree(AdvTree advTree, int cellIndex, string searchText, bool exact, bool isRegex, bool ignoreCase)
        {
            if (advTree.Nodes.Count == 0)
                return null;

            if (isRegex)
            {
                Regex r = new Regex(searchText, ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
                foreach (var node in findNextNode(advTree))
                {
                    if (node != null && node.Cells.Count > cellIndex && r.IsMatch(node.Cells[cellIndex].Text))
                    {
                        return node;
                    }
                }
            }
            else
            {
                string[] pattern = searchText.Split('\\');
                foreach (var node in findNextNode(advTree))
                {
                    if (checkSearchNodeText(node, cellIndex, pattern, exact, ignoreCase))
                    {
                        return node;
                    }
                }
            }
            
            return null;
        }

        private IEnumerable<Node> findNextNode(AdvTree advTree)
        {
            var node = advTree.SelectedNode;
            if (node == null)
            {
                node = advTree.Nodes[0];
                yield return node;
            }

            var levelStack = new Stack<int>();
            int index = node.Index + 1;

            while (true)
            {
                if (node.Nodes.Count > 0)
                {
                    levelStack.Push(index);
                    index = 0;
                    yield return node = node.Nodes[index++];
                    continue;
                }

                NodeCollection owner;

                while (index >= (owner = (node.Parent?.Nodes ?? advTree.Nodes)).Count)
                {
                    node = node.Parent;
                    if (node == null)
                    {
                        yield break;
                    }
                    if (levelStack.Count > 0)
                    {
                        index = levelStack.Pop();
                    }
                    else
                    {
                        index = node.Index + 1;
                    }
                }

                yield return node = owner[index++];
            }
        }

        private bool checkSearchNodeText(Node node, int cellIndex, string[] searchTextArray, bool exact, bool ignoreCase)
        {
            if (node == null || searchTextArray == null || searchTextArray.Length == 0)
                return false;
            for (int i = searchTextArray.Length - 1; i >= 0; i--)
            {
                if (node == null || node.Cells.Count <= cellIndex)
                    return false;
                if (exact)
                {
                    if (string.Compare(node.Cells[cellIndex].Text, searchTextArray[i], ignoreCase) != 0)
                        return false;
                }
                else
                {
                    if (ignoreCase ? node.Cells[cellIndex].Text.IndexOf(searchTextArray[i], StringComparison.CurrentCultureIgnoreCase) < 0 :
                        !node.Text.Contains(searchTextArray[i]))
                        return false;
                }

                node = node.Parent;
            }
            return true;
        }

        private void textBoxItemSearchWz_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                buttonItemSearchWz_Click(buttonItemSearchWz, EventArgs.Empty);
            }
        }

        private void buttonItemSearchString_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxItemSearchString.Text))
                return;
            QueryPerformance.Start();
            if (!this.stringLinker.HasValues)
            {
                if (!this.TryLoadStringWz())
                {
                    MessageBoxEx.Show("没有初始化string链接，请手动指定一个String.wz。", "喵~~");
                    return;
                }
                QueryPerformance.End();
                double ms = (Math.Round(QueryPerformance.GetLastInterval(), 4) * 1000);
                labelItemStatus.Text = "初始化StringLinker成功, 用时" + ms + "ms.";
            }
            if (comboBoxItem2.SelectedIndex < 0)
                comboBoxItem2.SelectedIndex = 0;

            List<Dictionary<int, StringResult>> dicts = new List<Dictionary<int, StringResult>>();
            switch (comboBoxItem2.SelectedIndex)
            {
                case 0:
                    dicts.Add(stringLinker.StringEqp);
                    dicts.Add(stringLinker.StringItem);
                    dicts.Add(stringLinker.StringMap);
                    dicts.Add(stringLinker.StringMob);
                    dicts.Add(stringLinker.StringNpc);
                    dicts.Add(stringLinker.StringSkill);
                    break;
                case 1:
                    dicts.Add(stringLinker.StringEqp);
                    break;
                case 2:
                    dicts.Add(stringLinker.StringItem);
                    break;
                case 3:
                    dicts.Add(stringLinker.StringMap);
                    break;
                case 4:
                    dicts.Add(stringLinker.StringMob);
                    break;
                case 5:
                    dicts.Add(stringLinker.StringNpc);
                    break;
                case 6:
                    dicts.Add(stringLinker.StringSkill);
                    break;
            }

            listViewExString.BeginUpdate();
            try
            {
                listViewExString.Items.Clear();
                IEnumerable<KeyValuePair<int, StringResult>> results = searchStringLinker(dicts, textBoxItemSearchString.Text, checkBoxItemExact2.Checked, checkBoxItemRegex2.Checked);
                foreach (KeyValuePair<int, StringResult> kv in results)
                {
                    string[] item = new string[] { kv.Key.ToString(), kv.Value.Name, kv.Value.Desc, kv.Value.FullPath };
                    listViewExString.Items.Add(new ListViewItem(item));
                }
            }
            catch (Exception ex)
            {
                MessageBoxEx.Show(ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                listViewExString.EndUpdate();
            }            
        }

        private bool TryLoadStringWz()
        {
            foreach (Wz_Structure wz in openedWz)
            {
                foreach (Wz_File file in wz.wz_files)
                {
                    if (file.Type == Wz_Type.String && this.stringLinker.Load(file))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private IEnumerable<KeyValuePair<int, StringResult>> searchStringLinker(IEnumerable<Dictionary<int, StringResult>> dicts, string key, bool exact, bool isRegex)
        {
            string[] match = key.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Regex re = null;
            if (isRegex)
            {
                re = new Regex(key, RegexOptions.IgnoreCase);
            }

            foreach (Dictionary<int, StringResult> dict in dicts)
            {
                foreach (KeyValuePair<int, StringResult> kv in dict)
                {
                    if (exact)
                    {
                        if (kv.Key.ToString() == key || kv.Value.Name == key)
                            yield return kv;
                    }
                    else if (isRegex)
                    {
                        if (re.IsMatch(kv.Key.ToString()) || (!string.IsNullOrEmpty(kv.Value.Name) && re.IsMatch(kv.Value.Name)))
                        {
                            yield return kv;
                        }
                    }
                    else
                    {
                        string id = kv.Key.ToString();
                        bool r = true;
                        foreach (string str in match)
                        {
                            if (!(id.Contains(str) || (!string.IsNullOrEmpty(kv.Value.Name) && kv.Value.Name.Contains(str))))
                            {
                                r = false;
                                break;
                            }
                        }
                        if (r)
                        {
                            yield return kv;
                        }
                    }
                }
            }
        }

        private void textBoxItemSearchString_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                buttonItemSearchString_Click(buttonItemSearchString, EventArgs.Empty);
            }
        }

        private void buttonItemSelectStringWz_Click(object sender, EventArgs e)
        {
            Wz_File stringWzFile = advTree1.SelectedNode?.AsWzNode()?.GetNodeWzFile();
            if (stringWzFile == null)
            {
                MessageBoxEx.Show("没有选择一个用于初始化StringLinker的WzFile。", "喵...");
                return;
            }
            QueryPerformance.Start();
            bool r = stringLinker.Load(stringWzFile);
            QueryPerformance.End();
            if (r)
            {
                double ms = (Math.Round(QueryPerformance.GetLastInterval(), 4) * 1000);
                labelItemStatus.Text = "初始化StringLinker成功, 用时" + ms + "ms.";
            }
            else
            {
                MessageBoxEx.Show("初始化StringLinker失败。", "喵..");
            }
        }

        private void buttonItemClearStringWz_Click(object sender, EventArgs e)
        {
            stringLinker.Clear();
            labelItemStatus.Text = "StringLinker已经清空...";
        }

        private void buttonItemPatcher_Click(object sender, EventArgs e)
        {
            foreach (Form form in Application.OpenForms)
            {
                if (form is FrmPatcher && !form.IsDisposed)
                {
                    form.Show();
                    form.BringToFront();
                    return;
                }
            }
            FrmPatcher patcher = new FrmPatcher();
            var config = WcR2Config.Default;
            var defaultEnc = config?.WzEncoding?.Value ?? 0;
            if (defaultEnc != 0)
            {
                patcher.PatcherNoticeEncoding = Encoding.GetEncoding(defaultEnc);
            }
            patcher.Owner = this;
            patcher.Show();
        }
        #endregion

        #region soundPlayer相关事件
        private void preLoadSound(Wz_Sound sound, string soundName)
        {
            byte[] data = sound.ExtractSound();
            if (data == null || data.Length <= 0)
            {
                return;
            }
            soundPlayer.PreLoad(data);
            labelItemSoundTitle.Text = soundName;

            switch (sound.SoundType)
            {
                case Wz_SoundType.Mp3: soundName += ".mp3"; break;
                case Wz_SoundType.Pcm: soundName += ".wav"; break;
            }
            soundPlayer.PlayingSoundName = soundName;
            labelItemSoundTitle.Tooltip = soundName;
        }

        private void sliderItemSoundTime_ValueChanged(object sender, EventArgs e)
        {
            if (!timerChangeValue)
                soundPlayer.SoundPosition = sliderItemSoundTime.Value;
        }

        private void sliderItemSoundVol_ValueChanged(object sender, EventArgs e)
        {
            soundPlayer.Volume = sliderItemSoundVol.Value;
        }

        private void buttonItemLoadSound_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                List<string> supportExt = new List<string>();
                supportExt.Add("Sound File(*.mp3;*.ogg;*.wav)|*.mp3;*.ogg;*.wav");
                foreach (string ext in this.soundPlayer.GetPluginSupportedExt())
                {
                    supportExt.Add(ext);
                }
                supportExt.Add("Any File|*.*");

                dlg.Title = "请选择一个声音文件...";
                dlg.Filter = string.Join("|", supportExt.ToArray());
                dlg.Multiselect = false;

                if (DialogResult.OK == dlg.ShowDialog())
                {
                    loadCostumSoundFile(dlg.FileName);
                }
            }
        }

        private void buttonItemSoundPlay_Click(object sender, EventArgs e)
        {
            if (soundPlayer.State == PlayState.Playing)
            {
                soundPlayer.Pause();
                buttonItemSoundPlay.Image = WzComparerR2.Properties.Resources.Play;
                //buttonItemSoundPlay.Text = " Play";
            }
            else if (soundPlayer.State == PlayState.Paused)
            {
                soundPlayer.Resume();
                //buttonItemSoundPlay.Text = "Pause";
                buttonItemSoundPlay.Image = WzComparerR2.Properties.Resources.Pause;
            }
            else
            {
                soundPlayer.Play();
                //buttonItemSoundPlay.Text = "Pause";
                buttonItemSoundPlay.Image = WzComparerR2.Properties.Resources.Pause;
            }
        }

        private void buttonItemSoundStop_Click(object sender, EventArgs e)
        {
            soundPlayer.Stop();
            //buttonItemSoundPlay.Text = " Play";
            buttonItemSoundPlay.Image = WzComparerR2.Properties.Resources.Play;
        }

        private void buttonItemSoundSave_Click(object sender, EventArgs e)
        {
            byte[] data = soundPlayer.Data;
            if (data == null)
                return;

            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.AddExtension = true;
                dlg.Title = "请选择保存路径...";
                dlg.Filter = "*.*|*.*";
                dlg.AddExtension = false;
                dlg.FileName = soundPlayer.PlayingSoundName;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    FileStream fs = null;
                    try
                    {
                        fs = new FileStream(dlg.FileName, FileMode.Create);
                        fs.Write(data, 0, data.Length);

                        MessageBoxEx.Show("保存成功！");
                    }
                    catch (Exception ex)
                    {
                        MessageBoxEx.Show("保存失败\r\n\r\n" + ex.ToString(), "错误");
                    }
                    finally
                    {
                        if (fs != null)
                        {
                            fs.Close();
                        }
                    }
                }
            }
        }

        private void checkBoxItemSoundLoop_CheckedChanged(object sender, CheckBoxChangeEventArgs e)
        {
            soundPlayer.Loop = checkBoxItemSoundLoop.Checked;
        }

        private void soundTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            TimeSpan currentTime = TimeSpan.FromSeconds(soundPlayer.SoundPosition);
            TimeSpan totalTime = TimeSpan.FromSeconds(soundPlayer.SoundLength);
            labelItemSoundTime.Text = string.Format("{0:d2}:{1:d2}:{2:d2}.{3:d3} / {4:d2}:{5:d2}:{6:d2}.{7:d3}",
                currentTime.Hours, currentTime.Minutes, currentTime.Seconds, currentTime.Milliseconds,
                totalTime.Hours, totalTime.Minutes, totalTime.Seconds, totalTime.Milliseconds);
            timerChangeValue = true;
            sliderItemSoundTime.Maximum = (int)totalTime.TotalSeconds;
            sliderItemSoundTime.Value = (int)currentTime.TotalSeconds;
            timerChangeValue = false;
        }

        private void ribbonBar3_DragEnter(object sender, DragEventArgs e)
        {
            string[] types = e.Data.GetFormats();
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Move;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void ribbonBar3_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                loadCostumSoundFile(files[0]);
            }
        }

        private void loadCostumSoundFile(string fileName)
        {
            CustomSoundFile soundFile = new CustomSoundFile(fileName, 0, (int)(new FileInfo(fileName).Length));
            soundPlayer.PreLoad(soundFile);
            soundPlayer.PlayingSoundName = Path.GetFileName(fileName);
            labelItemSoundTitle.Text = "(载入文件)" + soundPlayer.PlayingSoundName;
            labelItemSoundTitle.Tooltip = fileName;
        }
        #endregion

        #region contextMenuStrip2
        private void tsmi2SaveAs_Click(object sender, EventArgs e)
        {
            object item = advTree3.SelectedNode?.AsWzNode()?.Value;

            if (item == null)
                return;

            if (item is string str)
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.FileName = advTree3.SelectedNode.Text;
                if (!dlg.FileName.Contains("."))
                {
                    dlg.FileName += ".txt";
                }
                dlg.Filter = "*.*|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(dlg.FileName, str);
                        this.labelItemStatus.Text = "保存成功。";
                    }
                    catch (Exception ex)
                    {
                        MessageBoxEx.Show("文件保存失败。\r\n" + ex.ToString(), "提示");
                    }
                }
            }
            else if (item is IMapleStoryBlob blob)
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.FileName = advTree3.SelectedNode.Text;
                if (!dlg.FileName.Contains(".") && blob is Wz_Sound wzSound)
                {
                    switch (wzSound.SoundType)
                    {
                        case Wz_SoundType.Mp3: dlg.FileName += ".mp3"; break;
                        case Wz_SoundType.Pcm: dlg.FileName += ".pcm"; break;
                    }
                }
                dlg.Filter = "*.*|*.*";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        byte[] data = new byte[blob.Length];
                        blob.CopyTo(data, 0);
                        using (var f = File.Create(dlg.FileName))
                        {
                            f.Write(data, 0, data.Length);
                            f.Flush();
                        }
                        this.labelItemStatus.Text = "保存成功。";
                    }
                    catch (Exception ex)
                    {
                        MessageBoxEx.Show("文件保存失败。\r\n" + ex.ToString(), "提示");
                    }
                }
            }
        }

        private void tsmi2HandleUol_Click(object sender, EventArgs e)
        {
            Wz_Uol uol = advTree3.SelectedNode?.AsWzNode()?.Value as Wz_Uol;
            if (uol == null)
            {
                labelItemStatus.Text = "没有选中适当的uol节点...";
                return;
            }

            Node uolNode = handleUol(advTree3.SelectedNode, uol.Uol);
            if (uolNode == null)
            {
                labelItemStatus.Text = "没有找到uol对应的节点...请试着载入更底层的父节点重新执行..";
                return;
            }
            else
            {
                advTree3.SelectedNode = uolNode;
            }
        }

        private void tsmi2ExpandAll_Click(object sender, EventArgs e)
        {
            if (advTree3.SelectedNode == null)
                return;
            advTree3.BeginUpdate();
            advTree3.SelectedNode.ExpandAll();
            advTree3.SelectedNode.Expand();
            advTree3.EndUpdate();
        }

        private void tsmi2CollapseAll_Click(object sender, EventArgs e)
        {
            if (advTree3.SelectedNode == null)
                return;
            advTree3.BeginUpdate();
            advTree3.SelectedNode.Collapse();
            advTree3.SelectedNode.CollapseAll();
            advTree3.EndUpdate();
        }

        private void tsmi2ExpandLevel_Click(object sender, EventArgs e)
        {
            if (advTree3.SelectedNode == null)
                return;

            advTree3.BeginUpdate();
            foreach (Node node in getEqualLevelNode(advTree3.SelectedNode))
            {
                node.Expand();
            }
            advTree3.EndUpdate();
        }

        private void tsmi2CollapseLevel_Click(object sender, EventArgs e)
        {
            if (advTree3.SelectedNode == null)
                return;

            advTree3.BeginUpdate();
            foreach (Node node in getEqualLevelNode(advTree3.SelectedNode))
            {
                node.Collapse();
            }
            advTree3.EndUpdate();
        }

        private IEnumerable<Node> getEqualLevelNode(Node currentNode)
        {
            if (currentNode == null)
                yield break;
            int level = currentNode.Level;
            Node parent = currentNode;
            while (parent != null && parent.Parent != null)
            {
                parent = parent.Parent;
            }
            Queue<Node> nodeList = new Queue<Node>();
            nodeList.Enqueue(parent);
            for (int i = 0; i < level; i++)
            {
                int count = nodeList.Count;
                for (int j = 0; j < count; j++)
                {
                    Node node = nodeList.Dequeue();
                    foreach (Node child in node.Nodes)
                        nodeList.Enqueue(child);
                }
            }

            while (nodeList.Count > 0)
            {
                yield return nodeList.Dequeue();
            }
        }

        private void tsmi2Prev_Click(object sender, EventArgs e)
        {
            if (historyNodeList.PrevCount > 0)
            {
                historySelecting = true;
                advTree3.SelectedNode = historyNodeList.MovePrev();
            }
        }

        private void tsmi2Next_Click(object sender, EventArgs e)
        {
            if (historyNodeList.NextCount > 0)
            {
                historySelecting = true;
                advTree3.SelectedNode = historyNodeList.MoveNext();
            }
        }

        private void contextMenuStrip2_Opening(object sender, CancelEventArgs e)
        {
            var node = advTree3.SelectedNode.AsWzNode();
            tsmi2SaveAs.Visible = false;
            tsmi2HandleUol.Visible = false;
            if (node != null)
            {
                if (node.Value is Wz_Sound || node.Value is Wz_Png || node.Value is string || node.Value is Wz_RawData || node.Value is Wz_Video)
                {
                    tsmi2SaveAs.Visible = true;
                    tsmi2SaveAs.Enabled = true;
                }
                else if (node.Value is Wz_Uol)
                {
                    tsmi2HandleUol.Visible = true;
                }
                else
                {
                    tsmi2SaveAs.Visible = true;
                    tsmi2SaveAs.Enabled = false;
                }
            }
        }
        #endregion

        #region charaSim相关
        private void buttonItemQuickView_Click(object sender, EventArgs e)
        {
            quickView();
        }

        private void advTree1_AfterNodeSelect_2(object sender, AdvTreeNodeEventArgs e)
        {
            lastSelectedTree = advTree1;
            if (buttonItemAutoQuickView.Checked)
            {
                quickView(advTree1.SelectedNode);
            }
        }

        private void advTree2_AfterNodeSelect_2(object sender, AdvTreeNodeEventArgs e)
        {
            lastSelectedTree = advTree2;
            if (buttonItemAutoQuickView.Checked)
            {
                quickView(advTree2.SelectedNode);
            }
        }

        private void quickView()
        {
            if (lastSelectedTree != null)
            {
                quickView(lastSelectedTree.SelectedNode);
            }
        }

        private void quickView(Node node)
        {
            Wz_Node selectedNode = node.AsWzNode();
            if (selectedNode == null)
            {
                return;
            }

            Wz_Image image;

            Wz_File wzf = selectedNode.GetNodeWzFile();
            if (wzf == null)
            {
                labelItemStatus.Text = "没有查询到节点所属的wzfile。";
                return;
            }

            if (!this.stringLinker.HasValues)
            {
                this.TryLoadStringWz();
            }

            object obj = null;
            string fileName = null;
            switch (wzf.Type)
            {
                case Wz_Type.Character:
                    if ((image = selectedNode.GetValue<Wz_Image>()) == null || !image.TryExtract())
                        return;
                    CharaSimLoader.LoadSetItemsIfEmpty();
                    CharaSimLoader.LoadExclusiveEquipsIfEmpty();
                    var gear = Gear.CreateFromNode(image.Node, PluginManager.FindWz);
                    obj = gear;
                    if (gear != null)
                    {
                        fileName = gear.ItemID + ".png";
                    }
                    break;
                case Wz_Type.Item:
                    Wz_Node itemNode = selectedNode;
                    if (Regex.IsMatch(itemNode.FullPathToFile, @"^Item\\(Cash|Consume|Etc|Install|Cash)\\\d{4,6}.img\\\d+$"))
                    {
                        var item = Item.CreateFromNode(itemNode, PluginManager.FindWz);
                        obj = item;
                        if (item != null)
                        {
                            fileName = item.ItemID + ".png";
                        }
                    }
                    else if (Regex.IsMatch(itemNode.FullPathToFile, @"^Item\\Pet\\\d{7}.img"))
                    {
                        if (CharaSimLoader.LoadedSetItems.Count == 0) //宠物 预读套装
                        {
                            CharaSimLoader.LoadSetItemsIfEmpty();
                        }
                        if ((image = selectedNode.GetValue<Wz_Image>()) == null || !image.TryExtract())
                            return;
                        var item = Item.CreateFromNode(image.Node, PluginManager.FindWz);
                        obj = item;
                        if (item != null)
                        {
                            fileName = item.ItemID + ".png";
                        }
                    }

                    break;
                case Wz_Type.Skill:
                    Wz_Node skillNode = selectedNode;
                    //模式路径分析
                    if (Regex.IsMatch(skillNode.FullPathToFile, @"^Skill\d*\\Recipe_\d+.img\\\d+$"))
                    {
                        Recipe recipe = Recipe.CreateFromNode(skillNode);
                        obj = recipe;
                        if (recipe != null)
                        {
                            fileName = "recipe_" + recipe.RecipeID + ".png";
                        }
                    }
                    else if (Regex.IsMatch(skillNode.FullPathToFile, @"^Skill\d*\\\d+.img\\skill\\\d+$"))
                    {
                        Skill skill = Skill.CreateFromNode(skillNode, PluginManager.FindWz);
                        if (skill != null)
                        {
                            switch (this.skillDefaultLevel)
                            {
                                case DefaultLevel.Level0: skill.Level = 0; break;
                                case DefaultLevel.Level1: skill.Level = 1; break;
                                case DefaultLevel.LevelMax: skill.Level = skill.MaxLevel; break;
                                case DefaultLevel.LevelMaxWithCO: skill.Level = skill.MaxLevel + 2; break;
                            }
                            obj = skill;
                            fileName = "skill_" + skill.SkillID + ".png";
                        }
                    }
                    break;

                case Wz_Type.Mob:
                    if ((image = selectedNode.GetValue<Wz_Image>()) == null || !image.TryExtract())
                        return;
                    var mob = Mob.CreateFromNode(image.Node, PluginManager.FindWz);
                    obj = mob;
                    if (mob != null)
                    {
                        fileName = mob.ID + ".png";
                    }
                    break;

                case Wz_Type.Npc:
                    if ((image = selectedNode.GetValue<Wz_Image>()) == null || !image.TryExtract())
                        return;
                    var npc = Npc.CreateFromNode(image.Node, PluginManager.FindWz);
                    obj = npc;
                    if (npc != null)
                    {
                        fileName = npc.ID + ".png";
                    }
                    break;
            }
            if (obj != null)
            {
                tooltipQuickView.TargetItem = obj;
                tooltipQuickView.ImageFileName = fileName;
                tooltipQuickView.Refresh();
                tooltipQuickView.HideOnHover = false;
                tooltipQuickView.Show();
            }
        }

        private void comboBoxItemLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            DevComponents.Editors.ComboItem item = comboBoxItemLanguage.SelectedItem as DevComponents.Editors.ComboItem;

            if (item != null)
            {
                GearGraphics.SetFontFamily(item.Text);
                ConfigManager.Reload();
                CharaSimConfig.Default.SelectedFontIndex = comboBoxItemLanguage.SelectedIndex;
                ConfigManager.Save();
            }
        }

        private void buttonItemClearSetItems_Click(object sender, EventArgs e)
        {
            int count = CharaSimLoader.LoadedSetItems.Count;
            CharaSimLoader.LoadedSetItems.Clear();
            labelItemStatus.Text = "已经清空预读套装共" + count + "项。";
        }

        private void buttonItemCharItem_CheckedChanged(object sender, EventArgs e)
        {
            if (buttonItemCharItem.Checked)
                this.charaSimCtrl.UIItem.Refresh();
            this.charaSimCtrl.UIItem.Visible = buttonItemCharItem.Checked;
        }

        private void buttonItemAddItem_Click(object sender, EventArgs e)
        {
            bool success;

            success = this.charaSimCtrl.UIItem.AddItem(this.tooltipQuickView.TargetItem as ItemBase);
            if (!success)
            {
                labelItemStatus.Text = "背包加入物品失败...";
            }
        }

        private void afrm_KeyDown(object sender, KeyEventArgs e)
        {
            AfrmTooltip frm = sender as AfrmTooltip;
            if (frm == null)
                return;

            switch (e.KeyCode)
            {
                case Keys.Escape:
                    frm.Hide();
                    break;
                case Keys.Up:
                    frm.Top -= 1;
                    break;
                case Keys.Down:
                    frm.Top += 1;
                    break;
                case Keys.Left:
                    frm.Left -= 1;
                    break;
                case Keys.Right:
                    frm.Left += 1;
                    break;
            }

            Skill skill = frm.TargetItem as Skill;
            if (skill != null)
            {
                switch (e.KeyCode)
                {
                    case Keys.Oemplus:
                    case Keys.Add:
                        skill.Level += 1;
                        break;

                    case Keys.OemMinus:
                    case Keys.Subtract:
                        skill.Level -= 1;
                        break;

                    case Keys.OemOpenBrackets:
                        skill.Level -= this.skillInterval;
                        break;
                    case Keys.OemCloseBrackets:
                        skill.Level += this.skillInterval;
                        break;
                }
                frm.Refresh();
            }
        }

        private void buttonItemCharaStat_CheckedChanged(object sender, EventArgs e)
        {
            if (buttonItemCharaStat.Checked)
            {
                this.charaSimCtrl.UIStat.Refresh();
            }
            this.charaSimCtrl.UIStat.Visible = buttonItemCharaStat.Checked;
        }

        private void buttonItemCharaEquip_CheckedChanged(object sender, EventArgs e)
        {
            if (buttonItemCharaEquip.Checked)
            {
                this.charaSimCtrl.UIEquip.Refresh();
            }
            this.charaSimCtrl.UIEquip.Visible = buttonItemCharaEquip.Checked;
        }

        private void buttonItemQuickViewSetting_Click(object sender, EventArgs e)
        {
            using (FrmQuickViewSetting frm = new FrmQuickViewSetting())
            {
                frm.Load(CharaSimConfig.Default);

                if (frm.ShowDialog() == DialogResult.OK)
                {
                    ConfigManager.Reload();
                    frm.Save(CharaSimConfig.Default);
                    ConfigManager.Save();
                    UpdateCharaSimSettings();
                }
            }
        }
        #endregion

        #region 实现插件接口
        Office2007RibbonForm PluginContextProvider.MainForm
        {
            get { return this; }
        }

        DotNetBarManager PluginContextProvider.DotNetBarManager
        {
            get { return this.dotNetBarManager1; }
        }

        IList<Wz_Structure> PluginContextProvider.LoadedWz
        {
            get { return new System.Collections.ObjectModel.ReadOnlyCollection<Wz_Structure>(this.openedWz); }
        }

        Wz_Node PluginContextProvider.SelectedNode1
        {
            get { return advTree1.SelectedNode.AsWzNode(); }
        }

        Wz_Node PluginContextProvider.SelectedNode2
        {
            get { return advTree2.SelectedNode.AsWzNode(); }
        }

        Wz_Node PluginContextProvider.SelectedNode3
        {
            get { return advTree3.SelectedNode.AsWzNode(); }
        }

        private EventHandler<WzNodeEventArgs> selectedNode1Changed;
        private EventHandler<WzNodeEventArgs> selectedNode2Changed;
        private EventHandler<WzNodeEventArgs> selectedNode3Changed;
        private EventHandler<WzStructureEventArgs> wzOpened;
        private EventHandler<WzStructureEventArgs> wzClosing;

        event EventHandler<WzNodeEventArgs> PluginContextProvider.SelectedNode1Changed
        {
            add { selectedNode1Changed += value; }
            remove { selectedNode1Changed -= value; }
        }

        event EventHandler<WzNodeEventArgs> PluginContextProvider.SelectedNode2Changed
        {
            add { selectedNode2Changed += value; }
            remove { selectedNode2Changed -= value; }
        }

        event EventHandler<WzNodeEventArgs> PluginContextProvider.SelectedNode3Changed
        {
            add { selectedNode3Changed += value; }
            remove { selectedNode3Changed -= value; }
        }

        event EventHandler<WzStructureEventArgs> PluginContextProvider.WzOpened
        {
            add { wzOpened += value; }
            remove { wzOpened -= value; }
        }

        event EventHandler<WzStructureEventArgs> PluginContextProvider.WzClosing
        {
            add { wzClosing += value; }
            remove { wzClosing -= value; }
        }

        StringLinker PluginContextProvider.DefaultStringLinker
        {
            get { return this.stringLinker; }
        }

        AlphaForm PluginContextProvider.DefaultTooltipWindow
        {
            get { return this.tooltipQuickView; }
        }

        private void RegisterPluginEvents()
        {
            advTree1.AfterNodeSelect += advTree1_AfterNodeSelect_Plugin;
            advTree2.AfterNodeSelect += advTree2_AfterNodeSelect_Plugin;
            advTree3.AfterNodeSelect += advTree3_AfterNodeSelect_Plugin;
        }

        private void advTree1_AfterNodeSelect_Plugin(object sender, AdvTreeNodeEventArgs e)
        {
            if (selectedNode1Changed != null)
            {
                var wzNode = ((PluginContextProvider)(this)).SelectedNode1;
                var args = new WzNodeEventArgs(wzNode);
                selectedNode1Changed(this, args);
            }
        }

        private void advTree2_AfterNodeSelect_Plugin(object sender, AdvTreeNodeEventArgs e)
        {
            if (selectedNode2Changed != null)
            {
                var wzNode = ((PluginContextProvider)(this)).SelectedNode2;
                var args = new WzNodeEventArgs(wzNode);
                selectedNode2Changed(this, args);
            }
        }

        private void advTree3_AfterNodeSelect_Plugin(object sender, AdvTreeNodeEventArgs e)
        {
            if (selectedNode3Changed != null)
            {
                var wzNode = ((PluginContextProvider)(this)).SelectedNode3;
                var args = new WzNodeEventArgs(wzNode);
                selectedNode3Changed(this, args);
            }
        }

        protected virtual void OnWzOpened(WzStructureEventArgs e)
        {
            if (wzOpened != null)
            {
                wzOpened(this, e);
            }
        }

        protected virtual void OnWzClosing(WzStructureEventArgs e)
        {
            if (wzClosing != null)
            {
                wzClosing(this, e);
            }
        }
        #endregion

        private void btnEasyCompare_Click(object sender, EventArgs e)
        {
            if (compareThread != null)
            {
                compareThread.Suspend();
                if (DialogResult.Yes == MessageBoxEx.Show("正在执行一个对比操作，要中止吗？", "Compare", MessageBoxButtons.YesNoCancel))
                {
                    compareThread.Resume();
                    compareThread.Abort();
                }
                else
                {
                    compareThread.Resume();
                }
                return;
            }

            if (openedWz.Count < 2)
            {
                MessageBoxEx.Show("没有成功打开两个WZ。", "Compare");
                return;
            }

            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = "请选择输出报告的文件夹";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                compareThread = new Thread(() =>
                {
                    System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
                    EasyComparer comparer = new EasyComparer();
                    comparer.Comparer.PngComparison = (WzPngComparison)cmbComparePng.SelectedItem;
                    comparer.Comparer.ResolvePngLink = chkResolvePngLink.Checked;
                    comparer.OutputPng = chkOutputPng.Checked;
                    comparer.OutputAddedImg = chkOutputAddedImg.Checked;
                    comparer.OutputRemovedImg = chkOutputRemovedImg.Checked;
                    comparer.EnableDarkMode = chkEnableDarkMode.Checked;
                    comparer.HashPngFileName = chkHashPngFileName.Checked;
                    comparer.StateInfoChanged += new EventHandler(comparer_StateInfoChanged);
                    comparer.StateDetailChanged += new EventHandler(comparer_StateDetailChanged);
                    try
                    {
                        Wz_File fileNew = openedWz[0].wz_files[0];
                        Wz_File fileOld = openedWz[1].wz_files[0];

                        while (true)
                        {
                            string txt = string.Format("待比较wz文件：\r\n\r\n  new : {0} (ver:{1})\r\n  old : {2} (ver:{3})\r\n\r\n如果继续对比请选择Yes，如果想交换文件顺序请选择No。",
                                fileNew.Header.FileName,
                                fileNew.GetMergedVersion(),
                                fileOld.Header.FileName,
                                fileOld.GetMergedVersion()
                                );
                            switch (MessageBoxEx.Show(txt, "Easy Compare", MessageBoxButtons.YesNoCancel))
                            {
                                case DialogResult.Yes:
                                    comparer.EasyCompareWzFiles(fileNew, fileOld, dlg.SelectedPath);
                                    return;

                                case DialogResult.No:
                                    Wz_File tmp = fileNew;
                                    fileNew = fileOld;
                                    fileOld = tmp;
                                    break;

                                case DialogResult.Cancel:
                                default:
                                    return;
                            }
                        }

                    }
                    catch (ThreadAbortException)
                    {
                        MessageBoxEx.Show(this, "比较已经中止。", "Compare");
                    }
                    catch (Exception ex)
                    {
                        MessageBoxEx.Show(this, "出现异常\r\n" + ex.ToString(), "Compare");
                    }
                    finally
                    {
                        sw.Stop();
                        compareThread = null;
                        labelXComp1.Text = "wz对比结束...共用时" + sw.Elapsed.ToString();
                        labelXComp2.Text = "";
                    }
                });
                compareThread.Priority = ThreadPriority.Highest;
                compareThread.Start();
            }
        }

        void comparer_StateDetailChanged(object sender, EventArgs e)
        {
            EasyComparer comp = sender as EasyComparer;
            if (comp != null)
            {
                labelXComp1.Text = comp.StateInfo;
            }
        }

        void comparer_StateInfoChanged(object sender, EventArgs e)
        {
            EasyComparer comp = sender as EasyComparer;
            if (comp != null)
            {
                labelXComp2.Text = comp.StateDetail;
            }
        }

        private void buttonItemAbout_Click(object sender, EventArgs e)
        {
            new FrmAbout().ShowDialog();
        }

        private void btnExportSkill_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = "请选择技能列表输出文件夹";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if (!this.stringLinker.HasValues)
                    this.TryLoadStringWz();

                DBConnection conn = new DBConnection(this.stringLinker);
                DataSet ds = conn.GenerateSkillTable();
                foreach (DataTable dt in ds.Tables)
                {
                    FileStream fs = new FileStream(Path.Combine(dlg.SelectedPath, dt.TableName + ".csv"), FileMode.Create);
                    StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);
                    conn.OutputCsv(sw, dt);
                    sw.Close();
                    fs.Dispose();
                }
                MessageBoxEx.Show("数据导出完毕。");
            }
        }

        private void btnExportSkillOption_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = "请选择魂宝珠系统输出文件夹";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if (!this.stringLinker.HasValues)
                    this.TryLoadStringWz();

                DBConnection conn = new DBConnection(this.stringLinker);
                conn.ExportSkillOption(dlg.SelectedPath);
                MessageBoxEx.Show("数据导出完毕。");
            }
        }

        private void buttonItemAutoQuickView_Click(object sender, EventArgs e)
        {
            ConfigManager.Reload();
            CharaSimConfig.Default.AutoQuickView = buttonItemAutoQuickView.Checked;
            ConfigManager.Save();
        }

        private void panelExLeft_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState != FormWindowState.Minimized)
            {
                if (panelExLeft.Tag is int)
                {
                    int oldHeight = (int)panelExLeft.Tag;
                    advTree1.Height = (int)(1.0 * advTree1.Height / oldHeight * panelExLeft.Height);
                }
                panelExLeft.Tag = panelExLeft.Height;
            }
        }

        private void buttonItem1_Click(object sender, EventArgs e)
        {
        }

        private void labelItemStatus_TextChanged(object sender, EventArgs e)
        {
            ribbonBar2.RecalcLayout();
        }

        private void btnNodeBack_Click(object sender, EventArgs e)
        {

        }

        private void btnNodeForward_Click(object sender, EventArgs e)
        {

        }

        private void buttonItemUpdate_Click(object sender, EventArgs e)
        {
#if NET6_0_OR_GREATER
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = "https://github.com/Kagamia/WzComparerR2/releases",
            });
#else
            Process.Start("https://github.com/Kagamia/WzComparerR2/releases");
#endif
        }

        private void btnItemOptions_Click(object sender, System.EventArgs e)
        {
            var frm = new FrmOptions();
            frm.Load(WcR2Config.Default);
            if (frm.ShowDialog() == DialogResult.OK)
            {
                ConfigManager.Reload();
                frm.Save(WcR2Config.Default);
                ConfigManager.Save();
                UpdateWzLoadingSettings();
            }
        }
    }

    #region 内部用扩展方法
    internal static partial class Ext
    {
        public static Wz_Node AsWzNode(this Node node)
        {
            return (node?.Tag as WeakReference)?.Target as Wz_Node;
        }

        public static void ClearLayoutCellInfo(this AdvTree advTree)
        {
            var bindingPrivateField = BindingFlags.NonPublic | BindingFlags.Instance;
            {
                var field1 = advTree.GetType().GetField("Ֆ", bindingPrivateField);
                var obj1 = field1.GetValue(advTree);
                if (obj1 != null)
                {
                    var field2 = obj1.GetType().BaseType.GetField("ӹ", bindingPrivateField);
                    var obj2 = field2.GetValue(obj1);
                    if (obj2 != null)
                    {
                        var field3 = obj2.GetType().GetField("ܦ", bindingPrivateField);
                        var obj3 = field3.GetValue(obj2);
                        if (obj3 != null)
                        {
                            field3.SetValue(obj2, null);
                        }
                    }
                }
            }

            {
                var display = advTree.NodeDisplay as NodeTreeDisplay;
                if (display != null)
                {
                    var field4 = display.GetType().GetField("☼", bindingPrivateField);
                    var obj4 = field4.GetValue(display) as NodeCellRendererEventArgs;
                    if (obj4 != null)
                    {
                        obj4.Node = null;
                        obj4.Cell = null;
                    }
                }
            }
        }
    }
    #endregion
}
