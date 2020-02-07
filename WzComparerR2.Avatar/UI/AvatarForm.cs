using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using DevComponents.Editors;
using DevComponents.DotNetBar.Controls;
using WzComparerR2.Common;
using WzComparerR2.WzLib;
using WzComparerR2.PluginBase;
using System.Collections;
using System.Data;

namespace WzComparerR2.Avatar.UI
{
    public partial class AvatarForm : DevComponents.DotNetBar.OfficeForm
    {
        EffectForm effectForm;
        List<EffectStruction> effectStruct = new List<EffectStruction>();
        MixHairInfo AvatarMixHair;

        public string es_ToArray(List<EffectStruction> es)
        {
            if(es != null)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var ess in es)
                {
                    sb.Append(ess.ToString() + "_");
                }
                return sb.ToString();
            }
            else
            {
                return "";
            }
        }
        public string AMH(MixHairInfo mhi)
        {
            if(mhi != null)
            {
                return mhi.ToString();
            }
            else
            {
                return "";
            }
        }

        public AvatarForm()
        {
            InitializeComponent();
            this.avatar = new AvatarCanvas();
            this.animator = new Animator();
            this.effectanimator = new EffectAnimator();
            btnReset_Click(btnReset, EventArgs.Empty);
            FillWeaponIdx();
            FillEarSelection();

            effectForm = new EffectForm(this);
            AvatarMixHair = new MixHairInfo(0, 0);

#if !DEBUG
            buttonItem1.Visible = false;
#endif
        }

        public SuperTabControlPanel GetTabPanel()
        {
            this.TopLevel = false;
            this.Dock = DockStyle.Fill;
            this.FormBorderStyle = FormBorderStyle.None;
            this.DoubleBuffered = true;
            var pnl = new SuperTabControlPanel();
            pnl.Controls.Add(this);
            pnl.Padding = new System.Windows.Forms.Padding(1);
            this.Visible = true;
            return pnl;
        }

        public Entry PluginEntry { get; set; }

        AvatarCanvas avatar;
        bool inited;
        string partsTag;
        bool suspendUpdate;
        bool needUpdate;
        Animator animator;
        EffectAnimator effectanimator;

        BackgroundWorker worker;
        ProgressDialog progressDialog;

        /// <summary>
        /// wz1节点选中事件。
        /// </summary>
        public void OnSelectedNode1Changed(object sender, WzNodeEventArgs e)
        {
            if (PluginEntry.Context.SelectedTab != PluginEntry.Tab || e.Node == null
                || this.btnLock.Checked)
            {
                return;
            }

            Wz_File file = e.Node.GetNodeWzFile();
            if (file == null)
            {
                return;
            }

            switch (file.Type)
            {
                case Wz_Type.Character: //读取装备
                    Wz_Image wzImg = e.Node.GetValue<Wz_Image>();
                    if (wzImg != null && wzImg.TryExtract())
                    {
                        this.SuspendUpdateDisplay();
                        LoadPart(wzImg.Node);
                        this.ResumeUpdateDisplay();
                    }
                    break;
            }
        }

        public void OnSelectedNode2Changed(object sender, WzNodeEventArgs e)
        {
            if (PluginEntry.Context.SelectedTab != PluginEntry.Tab || e.Node == null
                || this.btnLock.Checked)
            {
                return;
            }

            Wz_File file = e.Node.GetNodeWzFile();
            if (file == null)
            {
                return;
            }

            switch (file.Type)
            {
                case Wz_Type.Item:
                    if (Regex.IsMatch(e.Node.FullPathToFile, @"Item\\Cash\\0501\.img\\\d+"))
                    {
                        this.SuspendUpdateDisplay();
                        LoadPart(e.Node);
                        this.ResumeUpdateDisplay();
                    }
                    break;
            }
        }

        public void OnWzClosing(object sender, WzStructureEventArgs e)
        {
            bool hasChanged = false;
            for (int i = 0; i < avatar.Parts.Length; i++)
            {
                var part = avatar.Parts[i];
                if (part != null)
                {
                    var wzFile = part.Node.GetNodeWzFile();
                    if (wzFile != null && e.WzStructure.wz_files.Contains(wzFile))//将要关闭文件 移除
                    {
                        avatar.Parts[i] = null;
                        hasChanged = true;
                    }
                }
            }

            if (hasChanged)
            {
                this.FillAvatarParts();
                UpdateDisplay();
            }
        }

        /// <summary>
        /// 初始化纸娃娃资源。
        /// </summary>
        private bool AvatarInit()
        {
            this.inited = this.avatar.LoadZ()
                && this.avatar.LoadActions()
                && this.avatar.LoadEmotions();

            if (this.inited)
            {
                this.FillBodyAction();
                this.FillEmotion();
            }
            return this.inited;
        }

        /// <summary>
        /// 加载装备部件。
        /// </summary>
        /// <param name="imgNode"></param>
        private void LoadPart(Wz_Node imgNode)
        {
            if (!this.inited && !this.AvatarInit() && imgNode == null)
            {
                return;
            }

            AvatarPart part = this.avatar.AddPart(imgNode);
            if (part != null)
            {
                OnNewPartAdded(part);
                FillAvatarParts();
                UpdateDisplay();
            }
        }

        private void OnNewPartAdded(AvatarPart part)
        {
            if (part == null)
            {
                return;
            }

            if (part == avatar.Body) //同步head
            {
                int headID = 10000 + part.ID.Value % 10000;
                if (avatar.Head == null || avatar.Head.ID != headID)
                {
                    var headImgNode = PluginBase.PluginManager.FindWz(string.Format("Character\\{0:D8}.img", headID));
                    if (headImgNode != null)
                    {
                        this.avatar.AddPart(headImgNode);
                    }
                }
            }
            else if (part == avatar.Head) //同步body
            {
                int bodyID = part.ID.Value % 10000;
                if (avatar.Body == null || avatar.Body.ID != bodyID)
                {
                    var bodyImgNode = PluginBase.PluginManager.FindWz(string.Format("Character\\{0:D8}.img", bodyID));
                    if (bodyImgNode != null)
                    {
                        this.avatar.AddPart(bodyImgNode);
                    }
                }
            }
            else if (part == avatar.Face) //同步表情
            {
                this.avatar.LoadEmotions();
                FillEmotion();
            }
            else if (part == avatar.Taming) //同步座驾动作
            {
                this.avatar.LoadTamingActions();
                FillTamingAction();
                SetTamingDefaultBodyAction();
                SetTamingDefault();
            }
            else if (part == avatar.Weapon) //同步武器类型
            {
                FillWeaponTypes();
            }
            else if (part == avatar.Pants || part == avatar.Coat) //隐藏套装
            {
                if (avatar.Longcoat != null)
                {
                    avatar.Longcoat.Visible = false;
                }
            }
            else if (part == avatar.Longcoat) //还是。。隐藏套装
            {
                if (avatar.Pants != null && avatar.Pants.Visible
                    || avatar.Coat != null && avatar.Coat.Visible)
                {
                    avatar.Longcoat.Visible = false;
                }
            }
        }

        private void SuspendUpdateDisplay()
        {
            this.suspendUpdate = true;
            this.needUpdate = false;
        }

        private void ResumeUpdateDisplay()
        {
            if (this.suspendUpdate)
            {
                this.suspendUpdate = false;
                if (this.needUpdate)
                {
                    this.UpdateDisplay();
                }
            }
        }

        /// <summary>
        /// 更新画布。
        /// </summary>
        public void UpdateDisplay()
        {
            if (suspendUpdate)
            {
                this.needUpdate = true;
                return;
            }

            string newPartsTag = GetAllPartsTag();
            if (this.partsTag != newPartsTag)
            {
                this.partsTag = newPartsTag;
                this.avatarContainer1.ClearAllCache();
            }

            ComboItem selectedItem;
            //同步角色动作
            selectedItem = this.cmbActionBody.SelectedItem as ComboItem;
            this.avatar.ActionName = selectedItem != null ? selectedItem.Text : null;
            //同步表情
            selectedItem = this.cmbEmotion.SelectedItem as ComboItem;
            this.avatar.EmotionName = selectedItem != null ? selectedItem.Text : null;
            //同步骑宠动作
            selectedItem = this.cmbActionTaming.SelectedItem as ComboItem;
            this.avatar.TamingActionName = selectedItem != null ? selectedItem.Text : null;

            //获取动作帧
            selectedItem = this.cmbBodyFrame.SelectedItem as ComboItem;
            int bodyFrame = selectedItem != null ? Convert.ToInt32(selectedItem.Text) : -1;
            selectedItem = this.cmbEmotionFrame.SelectedItem as ComboItem;
            int emoFrame = selectedItem != null ? Convert.ToInt32(selectedItem.Text) : -1;
            selectedItem = this.cmbTamingFrame.SelectedItem as ComboItem;
            int tamingFrame = selectedItem != null ? Convert.ToInt32(selectedItem.Text) : -1;

            //获取武器状态
            selectedItem = this.cmbWeaponType.SelectedItem as ComboItem;
            this.avatar.WeaponType = selectedItem != null ? Convert.ToInt32(selectedItem.Text) : 0;

            selectedItem = this.cmbWeaponIdx.SelectedItem as ComboItem;
            this.avatar.WeaponIndex = selectedItem != null ? Convert.ToInt32(selectedItem.Text) : 0;

            //获取耳朵状态
            selectedItem = this.cmbEar.SelectedItem as ComboItem;
            this.avatar.EarType = selectedItem != null ? Convert.ToInt32(selectedItem.Text) : 0;

            string actionTag = string.Format("{0}:{1},{2}:{3},{4}:{5},{6},{7},{8},{9},{10},{11},{12}",
                this.avatar.ActionName,
                bodyFrame,
                this.avatar.EmotionName,
                emoFrame,
                this.avatar.TamingActionName,
                tamingFrame,
                this.avatar.HairCover ? 1 : 0,
                this.avatar.ShowHairShade ? 1 : 0,
                this.avatar.EarType,
                this.avatar.WeaponType,
                this.avatar.WeaponIndex,
                es_ToArray(effectStruct),
                AMH(AvatarMixHair)
                ) ;

            if (!avatarContainer1.HasCache(actionTag))
            {
                try
                {
                    var actionFrames = avatar.GetActionFrames(avatar.ActionName);
                    ActionFrame f = null;
                    if (bodyFrame > -1 && bodyFrame < actionFrames.Length)
                    {
                        f = actionFrames[bodyFrame];
                    }

                    var bone = avatar.CreateFrame(bodyFrame, emoFrame, tamingFrame, effectStruct, AvatarMixHair);
                    var layers = avatar.CreateFrameLayers(bone);
                    avatarContainer1.AddCache(actionTag, layers);
                }
                catch
                {
                }
            }

            avatarContainer1.SetKey(actionTag);
        }

        private string GetAllPartsTag(bool effectCode = false)
        {
            string[] partsID = new string[avatar.Parts.Length];
            for (int i = 0; i < avatar.Parts.Length; i++)
            {
                var part = avatar.Parts[i];
                if (part != null && part.Visible)
                {
                    if(effectCode && (part.ItemEff!=null))
                    {
                        partsID[i] = part.ID.ToString() + "_" + part.ID.ToString();
                    }
                    else
                    {
                        partsID[i] = part.ID.ToString();
                    }
                }
            }
            return string.Join(",", partsID);
        }

        private void buttonItem1_Click(object sender, EventArgs e)
        {
            AddPart("Character\\00002000.img");
            AddPart("Character\\00012000.img");
            AddPart("Character\\Face\\00020000.img");
            AddPart("Character\\Hair\\00030000.img");
            AddPart("Character\\Coat\\01040036.img");
            AddPart("Character\\Pants\\01060026.img");
            FillAvatarParts();
            UpdateDisplay();
        }

        void AddPart(string imgPath)
        {
            Wz_Node imgNode = PluginManager.FindWz(imgPath);
            if (imgNode != null)
            {
                this.avatar.AddPart(imgNode);
            }
        }

        private void SelectBodyAction(string actionName)
        {
            for (int i = 0; i < cmbActionBody.Items.Count; i++)
            {
                ComboItem item = cmbActionBody.Items[i] as ComboItem;
                if (item != null && item.Text == actionName)
                {
                    cmbActionBody.SelectedIndex = i;
                    return;
                }
            }
        }

        private void SelectEmotion(string emotionName)
        {
            for (int i = 0; i < cmbEmotion.Items.Count; i++)
            {
                ComboItem item = cmbEmotion.Items[i] as ComboItem;
                if (item != null && item.Text == emotionName)
                {
                    cmbEmotion.SelectedIndex = i;
                    return;
                }
            }
        }

        #region 同步界面
        private void FillBodyAction()
        {
            var oldSelection = cmbActionBody.SelectedItem as ComboItem;
            int? newSelection = null;
            cmbActionBody.BeginUpdate();
            cmbActionBody.Items.Clear();
            foreach (var action in this.avatar.Actions)
            {
                ComboItem cmbItem = new ComboItem(action.Name);
                switch (action.Level)
                {
                    case 0:
                        cmbItem.FontStyle = FontStyle.Bold;
                        cmbItem.ForeColor = Color.Indigo;
                        break;

                    case 1:
                        cmbItem.ForeColor = Color.Indigo;
                        break;
                }
                cmbItem.Tag = action;
                cmbActionBody.Items.Add(cmbItem);

                if (newSelection == null && oldSelection != null)
                {
                    if (cmbItem.Text == oldSelection.Text)
                    {
                        newSelection = cmbActionBody.Items.Count - 1;
                    }
                }
            }

            if (cmbActionBody.Items.Count > 0)
            {
                cmbActionBody.SelectedIndex = newSelection ?? 0;
            }

            cmbActionBody.EndUpdate();
        }

        private void FillEmotion()
        {
            FillComboItems(cmbEmotion, avatar.Emotions);
        }

        private void FillTamingAction()
        {
            FillComboItems(cmbActionTaming, avatar.TamingActions);
        }

        private void FillWeaponTypes()
        {
            List<int> weaponTypes = avatar.GetCashWeaponTypes();
            FillComboItems(cmbWeaponType, weaponTypes.ConvertAll(i => i.ToString()));
        }

        private void SetTamingDefaultBodyAction()
        {
            string actionName;
            var tamingAction = (this.cmbActionTaming.SelectedItem as ComboItem)?.Text;
            switch (tamingAction)
            {
                case "ladder":
                case "rope":
                    actionName = tamingAction;
                    break;
                default:
                    actionName = "sit";
                    break;
            }
            SelectBodyAction(actionName);
        }

        private void SetTamingDefault()
        {
            if (this.avatar.Taming != null)
            {
                var tamingAction =  (this.cmbActionTaming.SelectedItem as ComboItem)?.Text;
                if (tamingAction != null)
                {
                    string forceAction = this.avatar.Taming.Node.FindNodeByPath($@"characterAction\{tamingAction}").GetValueEx<string>(null);
                    if (forceAction != null)
                    {
                        this.SelectBodyAction(forceAction);
                    }

                    string forceEmotion = this.avatar.Taming.Node.FindNodeByPath($@"characterEmotion\{tamingAction}").GetValueEx<string>(null);
                    if (forceEmotion != null)
                    {
                        this.SelectEmotion(forceEmotion);
                    }
                }
            }
        }

        /// <summary>
        /// 更新当前显示部件列表。
        /// </summary>
        private void FillAvatarParts()
        {
            EffectStruction efs;
            itemPanel1.BeginUpdate();
            itemPanel1.Items.Clear();
            effectForm.ItemEffectListBox1.Items.Clear();//ListBox의 아이템 모두 지움.
            int itemid = 0;
            effectForm.effectDelayList.Clear();
            foreach (var part in avatar.Parts)
            {
                if (part != null)
                {
                    efs = new EffectStruction(part.ID.Value, 0);
                    var btn = new AvatarPartButtonItem();
                    var stringLinker = this.PluginEntry.Context.DefaultStringLinker;
                    StringResult sr;
                    string text;
                    string itemname = "";
                    if (part.ID != null && (stringLinker.StringEqp.TryGetValue(part.ID.Value, out sr) || stringLinker.StringItem.TryGetValue(part.ID.Value, out sr)))
                    {
                        itemname = sr.Name;
                        text = string.Format("{0}\r\n{1}", sr.Name, part.ID);
                    }
                    else
                    {
                        text = string.Format("{0}\r\n{1}", "(null)", part.ID == null ? "-" : part.ID.ToString());
                    }
                    btn.Text = text;
                    btn.SetIcon(part.Icon.Bitmap);//顯示/隱藏
                    btn.Tag = part;
                    btn.Checked = part.Visible;
                    btn.btnItemShow.Click += BtnItemShow_Click;
                    btn.btnItemDel.Click += BtnItemDel_Click;
                    btn.CheckedChanged += Btn_CheckedChanged;
                    itemPanel1.Items.Add(btn);
                    if(part.ItemEff != null)
                    {
                        //아이템 이펙트가 있을 경우
                        if(part.ID != null)
                        {
                            if(!effectForm.itemDescDic.ContainsKey(Convert.ToInt32(part.ID)))
                            {
                                EffectForm.EffectLayer eLayer;
                                eLayer.itemcode = part.ID.Value;
                                effectForm.itemDescDic.Add((Convert.ToInt32(part.ID)), itemname);
                                efs.itemcode = (Convert.ToInt32(part.ID));
                                effectStruct.Add(efs);
                                Wz_Node searchNode = part.ItemEff;
                                string Action = effectForm.EffectTextBox.Text;
                                if (string.IsNullOrEmpty(Action))
                                {
                                    Action = "default";
                                }
                                Wz_Node FrameNode = searchNode.FindNodeByPath(Action);
                                if (FrameNode == null)
                                {
                                    FrameNode = searchNode.FindNodeByPath("default");
                                }
                                if (FrameNode != null)
                                {
                                    eLayer.delays=(FindEffectDelay(FrameNode));
                                    eLayer.currentFrame = 0;
                                    eLayer.animated = false;
                                    effectForm.EffectLayerGroup.Add(part.ID.Value,eLayer);//List Test
                                }
                            }
                        }
                    }
                    //헤어
                    if((part.ID.Value >= 30000) && (part.ID.Value < 50000))
                    {
                        effectForm.BaseHairText.Text = itemname;
                        //ComboBox 생성
                        int BlackHairCode = ((part.ID.Value / 10) * 10);
                        BlackHairCode = BlackHairCode - (BlackHairCode % 10);
                        int Hair_Black = BlackHairCode;
                        int Hair_Red = BlackHairCode + 1;
                        int Hair_Orange = BlackHairCode + 2;
                        int Hair_Yellow = BlackHairCode + 3;
                        int Hair_Green = BlackHairCode + 4;
                        int Hair_Blue = BlackHairCode + 5;
                        int Hair_Purple = BlackHairCode + 6;
                        int Hair_Brown = BlackHairCode + 7;
                        StringResult HairNameResult;
                        DataTable HairBox = new DataTable();
                        DataRow HairStringRow = null;
                        HairBox.Columns.Add(new DataColumn("itemcode", typeof(int)));
                        HairBox.Columns.Add(new DataColumn("name", typeof(string)));
                        //black
                        stringLinker.StringEqp.TryGetValue(Hair_Black, out HairNameResult);
                        HairStringRow = HairBox.NewRow();
                        HairStringRow["itemcode"] = Hair_Black;
                        if(HairNameResult != null)
                        {
                            HairStringRow["name"] = HairNameResult.Name;
                        }
                        else
                        {
                            HairStringRow["name"] = "Hair_Black : " + Hair_Black.ToString();
                        }
                        HairBox.Rows.Add(HairStringRow);
                        //Red
                        stringLinker.StringEqp.TryGetValue(Hair_Red, out HairNameResult);
                        HairStringRow = HairBox.NewRow();
                        HairStringRow["itemcode"] = Hair_Red;
                        if (HairNameResult != null)
                        {
                            HairStringRow["name"] = HairNameResult.Name;
                        }
                        else
                        {
                            HairStringRow["name"] = "Hair_Red : " + Hair_Red.ToString();
                        }
                        HairBox.Rows.Add(HairStringRow);
                        //Orange
                        stringLinker.StringEqp.TryGetValue(Hair_Orange, out HairNameResult);
                        HairStringRow = HairBox.NewRow();
                        HairStringRow["itemcode"] = Hair_Orange;
                        if (HairNameResult != null)
                        {
                            HairStringRow["name"] = HairNameResult.Name;
                        }
                        else
                        {
                            HairStringRow["name"] = "Hair_Orange : " + Hair_Orange.ToString();
                        }
                        HairBox.Rows.Add(HairStringRow);
                        //Hair_Yellow
                        stringLinker.StringEqp.TryGetValue(Hair_Yellow, out HairNameResult);
                        HairStringRow = HairBox.NewRow();
                        HairStringRow["itemcode"] = Hair_Yellow;
                        if (HairNameResult != null)
                        {
                            HairStringRow["name"] = HairNameResult.Name;
                        }
                        else
                        {
                            HairStringRow["name"] = "Hair_Yellow : " + Hair_Yellow.ToString();
                        }
                        HairBox.Rows.Add(HairStringRow);
                        //Hair_Green
                        stringLinker.StringEqp.TryGetValue(Hair_Green, out HairNameResult);
                        HairStringRow = HairBox.NewRow();
                        HairStringRow["itemcode"] = Hair_Green;
                        if (HairNameResult != null)
                        {
                            HairStringRow["name"] = HairNameResult.Name;
                        }
                        else
                        {
                            HairStringRow["name"] = "Hair_Green : " + Hair_Green.ToString();
                        }
                        HairBox.Rows.Add(HairStringRow);
                        //Hair_Blue
                        stringLinker.StringEqp.TryGetValue(Hair_Blue, out HairNameResult);
                        HairStringRow = HairBox.NewRow();
                        HairStringRow["itemcode"] = Hair_Blue;
                        if (HairNameResult != null)
                        {
                            HairStringRow["name"] = HairNameResult.Name;
                        }
                        else
                        {
                            HairStringRow["name"] = "Hair_Blue : " + Hair_Blue.ToString();
                        }
                        HairBox.Rows.Add(HairStringRow);
                        //Hair_Purple
                        stringLinker.StringEqp.TryGetValue(Hair_Purple, out HairNameResult);
                        HairStringRow = HairBox.NewRow();
                        HairStringRow["itemcode"] = Hair_Purple;
                        if (HairNameResult != null)
                        {
                            HairStringRow["name"] = HairNameResult.Name;
                        }
                        else
                        {
                            HairStringRow["name"] = "Hair_Purple : " + Hair_Purple.ToString();
                        }
                        HairBox.Rows.Add(HairStringRow);
                        //Hair_Brown
                        stringLinker.StringEqp.TryGetValue(Hair_Brown, out HairNameResult);
                        HairStringRow = HairBox.NewRow();
                        HairStringRow["itemcode"] = Hair_Brown;
                        if (HairNameResult != null)
                        {
                            HairStringRow["name"] = HairNameResult.Name;
                        }
                        else
                        {
                            HairStringRow["name"] = "Hair_Brown : " + Hair_Brown.ToString();
                        }
                        HairBox.Rows.Add(HairStringRow);
                        effectForm.MixHairComboBox.DataSource = HairBox;
                        effectForm.MixHairComboBox.ValueMember = "itemcode";
                        effectForm.MixHairComboBox.DisplayMember = "name";
                    }
                }
            }
            FillEffectListbox();
            itemPanel1.EndUpdate();
        }

        private void BtnItemShow_Click(object sender, EventArgs e)
        {
            var btn = (sender as BaseItem).Parent as AvatarPartButtonItem;
            if (btn != null)
            {
                btn.Checked = !btn.Checked;
            }
        }

        private void BtnItemDel_Click(object sender, EventArgs e)
        {
            var btn = (sender as BaseItem).Parent as AvatarPartButtonItem;
            if (btn != null)
            {
                var part = btn.Tag as AvatarPart;
                if (part != null)
                {
                    int index = Array.IndexOf(this.avatar.Parts, part);
                    if (index > -1)
                    {
                        this.avatar.Parts[index] = null;
                        this.FillAvatarParts();
                        this.UpdateDisplay();
                    }
                }
            }
        }

        private void Btn_CheckedChanged(object sender, EventArgs e)
        {
            var btn = sender as AvatarPartButtonItem;
            if (btn != null)
            {
                var part = btn.Tag as AvatarPart;
                if (part != null)
                {
                    part.Visible = btn.Checked;
                    this.UpdateDisplay();
                }
            }
        }

        private void FillBodyActionFrame()
        {
            ComboItem actionItem = cmbActionBody.SelectedItem as ComboItem;
            if (actionItem != null)
            {
                var frames = avatar.GetActionFrames(actionItem.Text);
                FillComboItems(cmbBodyFrame, frames);
            }
            else
            {
                cmbBodyFrame.Items.Clear();
            }
        }

        private void FillEmotionFrame()
        {
            ComboItem emotionItem = cmbEmotion.SelectedItem as ComboItem;
            if (emotionItem != null)
            {
                var frames = avatar.GetFaceFrames(emotionItem.Text);
                FillComboItems(cmbEmotionFrame, frames);
            }
            else
            {
                cmbEmotionFrame.Items.Clear();
            }
        }

        private void FillTamingActionFrame()
        {
            ComboItem actionItem = cmbActionTaming.SelectedItem as ComboItem;
            if (actionItem != null)
            {
                var frames = avatar.GetTamingFrames(actionItem.Text);
                FillComboItems(cmbTamingFrame, frames);
            }
            else
            {
                cmbTamingFrame.Items.Clear();
            }
        }

        private void FillWeaponIdx()
        {
            FillComboItems(cmbWeaponIdx, 0, 4);
        }

        private void FillEarSelection()
        {
            FillComboItems(cmbEar, 0, 4);
        }

        private void FillComboItems(ComboBoxEx comboBox, int start, int count)
        {
            List<ComboItem> items = new List<ComboItem>(count);
            for (int i = 0; i < count; i++)
            {
                ComboItem item = new ComboItem();
                item.Text = (start + i).ToString();
                items.Add(item);
            }
            FillComboItems(comboBox, items);
        }

        private void FillComboItems(ComboBoxEx comboBox, IEnumerable<string> items)
        {
            List<ComboItem> _items = new List<ComboItem>();
            foreach (var itemText in items)
            {
                ComboItem item = new ComboItem();
                item.Text = itemText;
                _items.Add(item);
            }
            FillComboItems(comboBox, _items);
        }

        private void FillComboItems(ComboBoxEx comboBox, IEnumerable<ActionFrame> frames)
        {
            List<ComboItem> items = new List<ComboItem>();
            int i = 0;
            foreach (var f in frames)
            {
                ComboItem item = new ComboItem();
                item.Text = (i++).ToString();
                item.Tag = Math.Abs(f.Delay);
                items.Add(item);
            }
            FillComboItems(comboBox, items);
        }

        private void FillComboItems(ComboBoxEx comboBox, IEnumerable<ComboItem> items)
        {
            //保持原有选项
            var oldSelection = comboBox.SelectedItem as ComboItem;
            int? newSelection = null;
            comboBox.BeginUpdate();
            comboBox.Items.Clear();

            foreach (var item in items)
            {
                comboBox.Items.Add(item);

                if (newSelection == null && oldSelection != null)
                {
                    if (item.Text == oldSelection.Text)
                    {
                        newSelection = comboBox.Items.Count - 1;
                    }
                }
            }

            //恢复原有选项
            if (comboBox.Items.Count > 0)
            {
                comboBox.SelectedIndex = newSelection ?? 0;
            }

            comboBox.EndUpdate();
        }
        #endregion

        private void cmbActionBody_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.SuspendUpdateDisplay();
            FillBodyActionFrame();
            FillCmbEffect(effectForm.recentitem);
            this.ResumeUpdateDisplay();
            UpdateDisplay();
        }

        private void cmbEmotion_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.SuspendUpdateDisplay();
            FillEmotionFrame();
            this.ResumeUpdateDisplay();
            UpdateDisplay();
        }

        private void cmbActionTaming_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.SuspendUpdateDisplay();
            FillTamingActionFrame();
            SetTamingDefaultBodyAction();
            SetTamingDefault();
            this.ResumeUpdateDisplay();
            UpdateDisplay();
        }

        private void cmbBodyFrame_SelectedIndexChanged(object sender, EventArgs e)
        {
            effectForm.EffectTextBox.Text = (cmbActionBody.SelectedItem.ToString());
            UpdateDisplay();
        }

        private void cmbEmotionFrame_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void cmbTamingFrame_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void cmbWeaponType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void cmbWeaponIdx_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void cmbEar_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateDisplay();
        }

        private void chkBodyPlay_CheckedChanged(object sender, EventArgs e)
        {
            if (chkBodyPlay.Checked)
            {
                if (!this.timer1.Enabled)
                {
                    AnimateStart();
                }

                var item = cmbBodyFrame.SelectedItem as ComboItem;
                int? delay;
                if (item != null && ((delay = item.Tag as int?) != null) && delay.Value >= 0)
                {
                    this.animator.BodyDelay = delay.Value;
                }
            }
            else
            {
                this.animator.BodyDelay = -1;
                TimerEnabledCheck();
            }
        }

        public void resetEffectDelay(int itemcode)
        {
            switch ((int)(itemcode / 10000))
            {
                case 100:
                    effectanimator.HatDelay = -1;
                    break;

                case 101:
                    effectanimator.FaceDelay = -1;
                    break;

                case 105:
                    effectanimator.LongCoatDelay = -1;
                    break;

                case 107:
                    effectanimator.ShoesDelay = -1;
                    break;

                case 110:
                    effectanimator.CapeDelay = -1;
                    break;

                case 501:
                    effectanimator.EffectDelay = -1;
                    break;
            }
        }
        public void setEffectDelay(int itemcode)
        {
            switch((int)(itemcode / 10000))
            {
                case 100:
                    effectanimator.HatDelay = effectForm.EffectLayerGroup[itemcode].delays[effectForm.EffectLayerGroup[itemcode].currentFrame];
                    break;

                case 101:
                    effectanimator.FaceDelay = effectForm.EffectLayerGroup[itemcode].delays[effectForm.EffectLayerGroup[itemcode].currentFrame];
                    break;

                case 105:
                    effectanimator.LongCoatDelay = effectForm.EffectLayerGroup[itemcode].delays[effectForm.EffectLayerGroup[itemcode].currentFrame];
                    break;

                case 107:
                    effectanimator.ShoesDelay = effectForm.EffectLayerGroup[itemcode].delays[effectForm.EffectLayerGroup[itemcode].currentFrame];
                    break;

                case 110:
                    effectanimator.CapeDelay = effectForm.EffectLayerGroup[itemcode].delays[effectForm.EffectLayerGroup[itemcode].currentFrame];
                    break;

                case 501:
                    effectanimator.EffectDelay = effectForm.EffectLayerGroup[itemcode].delays[effectForm.EffectLayerGroup[itemcode].currentFrame];
                    break;
            }
        }
        private void chkEmotionPlay_CheckedChanged(object sender, EventArgs e)
        {
            if (chkEmotionPlay.Checked)
            {
                if (!this.timer1.Enabled)
                {
                    AnimateStart();
                }
                var item = cmbEmotionFrame.SelectedItem as ComboItem;
                int? delay;
                if (item != null && ((delay = item.Tag as int?) != null) && delay.Value >= 0)
                {
                    this.animator.EmotionDelay = delay.Value;
                }
            }
            else
            {
                this.animator.EmotionDelay = -1;
                TimerEnabledCheck();
            }
        }

        private void chkTamingPlay_CheckedChanged(object sender, EventArgs e)
        {
            if (chkTamingPlay.Checked)
            {
                if (!this.timer1.Enabled)
                {
                    AnimateStart();
                }
                var item = cmbTamingFrame.SelectedItem as ComboItem;
                int? delay;
                if (item != null && ((delay = item.Tag as int?) != null) && delay.Value >= 0)
                {
                    this.animator.TamingDelay = delay.Value;
                }
            }
            else
            {
                this.animator.TamingDelay = -1;
                TimerEnabledCheck();
            }
        }

        private void chkHairCover_CheckedChanged(object sender, EventArgs e)
        {
            avatar.HairCover = chkHairCover.Checked;
            UpdateDisplay();
        }

        private void chkHairShade_CheckedChanged(object sender, EventArgs e)
        {
            avatar.ShowHairShade = chkHairShade.Checked;
            UpdateDisplay();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            this.animator.Elapse(timer1.Interval);
            this.AnimateUpdate();
            int interval = this.animator.NextFrameDelay;

            if (interval <= 0)
            {
                this.timer1.Stop();
            }
            else
            {
                this.timer1.Interval = interval;
            }
        }

        private void effectTimer_Tick(object sender, EventArgs e)//Tick마다 실행이 됨.
        {
            this.effectanimator.Elapse(effectTimer.Interval);//delays 배열에 있는 delay값들을 모두 깐다.(시간이 흐른 만큼)
            this.EffectAnimateUpdate();//콤보박스를 다음걸로 넘김
            int interval = this.effectanimator.NextFrameDelay;

            if (interval <= 0)
            {
                this.effectTimer.Stop();
            }
            else
            {
                this.effectTimer.Interval = interval;
            }
        }
        private void EffectAnimateUpdate()
        {
            int Hatcode = 0, FaceCode = 0, LongCoatCode = 0, ShoesCode = 0, CapeCode = 0, EffectCode = 0;
            foreach (var lys in effectForm.EffectLayerGroup)
            {
                switch ((int)(lys.Key/10000))
                {
                    case 100:
                        Hatcode = lys.Key;
                        break;
                    case 101:
                        FaceCode = lys.Key;
                        break;
                    case 105:
                        LongCoatCode = lys.Key;
                        break;
                    case 107:
                        ShoesCode = lys.Key;
                        break;
                    case 110:
                        CapeCode = lys.Key;
                        break;
                    case 501:
                        EffectCode = lys.Key;
                        break;

                    default:
                        break;
                }
            }
            this.SuspendUpdateDisplay();
            if(this.effectanimator.HatDelay == 0 && effectForm.EffectLayerGroup.ContainsKey(Hatcode))
            {
                effectForm.EffectLayerGroup[Hatcode] = FindNextFrameE(effectForm.EffectLayerGroup[Hatcode]);
                this.effectanimator.HatDelay = effectForm.EffectLayerGroup[Hatcode].delays[effectForm.EffectLayerGroup[Hatcode].currentFrame];
                ChangeEffectStruct(Hatcode, effectForm.EffectLayerGroup[Hatcode].currentFrame);
            }
            if (this.effectanimator.FaceDelay == 0 && effectForm.EffectLayerGroup.ContainsKey(FaceCode))
            {
                effectForm.EffectLayerGroup[FaceCode] = FindNextFrameE(effectForm.EffectLayerGroup[FaceCode]);
                this.effectanimator.FaceDelay = effectForm.EffectLayerGroup[FaceCode].delays[effectForm.EffectLayerGroup[FaceCode].currentFrame];
                ChangeEffectStruct(FaceCode, effectForm.EffectLayerGroup[FaceCode].currentFrame);
            }
            if (this.effectanimator.LongCoatDelay == 0 && effectForm.EffectLayerGroup.ContainsKey(LongCoatCode))
            {
                effectForm.EffectLayerGroup[LongCoatCode] = FindNextFrameE(effectForm.EffectLayerGroup[LongCoatCode]);
                this.effectanimator.LongCoatDelay = effectForm.EffectLayerGroup[LongCoatCode].delays[effectForm.EffectLayerGroup[LongCoatCode].currentFrame];
                ChangeEffectStruct(LongCoatCode, effectForm.EffectLayerGroup[LongCoatCode].currentFrame);
            }
            if (this.effectanimator.ShoesDelay == 0 && effectForm.EffectLayerGroup.ContainsKey(ShoesCode))
            {
                effectForm.EffectLayerGroup[ShoesCode] = FindNextFrameE(effectForm.EffectLayerGroup[ShoesCode]);
                this.effectanimator.ShoesDelay = effectForm.EffectLayerGroup[ShoesCode].delays[effectForm.EffectLayerGroup[ShoesCode].currentFrame];
                ChangeEffectStruct(ShoesCode, effectForm.EffectLayerGroup[ShoesCode].currentFrame);
            }
            if (this.effectanimator.CapeDelay == 0 && effectForm.EffectLayerGroup.ContainsKey(CapeCode))
            {
                effectForm.EffectLayerGroup[CapeCode] = FindNextFrameE(effectForm.EffectLayerGroup[CapeCode]);
                this.effectanimator.CapeDelay = effectForm.EffectLayerGroup[CapeCode].delays[effectForm.EffectLayerGroup[CapeCode].currentFrame];
                ChangeEffectStruct(CapeCode, effectForm.EffectLayerGroup[CapeCode].currentFrame);
            }
            if (this.effectanimator.EffectDelay == 0 && effectForm.EffectLayerGroup.ContainsKey(EffectCode))
            {
                effectForm.EffectLayerGroup[EffectCode] = FindNextFrameE(effectForm.EffectLayerGroup[EffectCode]);
                this.effectanimator.EffectDelay = effectForm.EffectLayerGroup[EffectCode].delays[effectForm.EffectLayerGroup[EffectCode].currentFrame];
                ChangeEffectStruct(EffectCode, effectForm.EffectLayerGroup[EffectCode].currentFrame);
            }
            this.ResumeUpdateDisplay();

        }
        public void EffectAnimateStart()
        {
            EffectTimerEnabledCheck();//타이머를 돌린다.
            if (effectTimer.Enabled)
            {
                EffectAnimateUpdate();//콤보박스를 다음 프레임으로 넘긴다
            }
        }

        public void EffectTimerEnabledCheck()
        {
            int Hatcode = 0, FaceCode = 0, LongCoatCode = 0, ShoesCode = 0, CapeCode = 0, EffectCode = 0;
            foreach (var lys in effectForm.EffectLayerGroup)
            {
                switch ((int)(lys.Key / 10000))
                {
                    case 100:
                        Hatcode = lys.Key;
                        break;
                    case 101:
                        FaceCode = lys.Key;
                        break;
                    case 105:
                        LongCoatCode = lys.Key;
                        break;
                    case 107:
                        ShoesCode = lys.Key;
                        break;
                    case 110:
                        CapeCode = lys.Key;
                        break;
                    case 501:
                        EffectCode = lys.Key;
                        break;

                    default:
                        break;
                }
            }
            if (
                (effectForm.EffectLayerGroup.ContainsKey(Hatcode) && effectForm.EffectLayerGroup[Hatcode].animated) ||
                (effectForm.EffectLayerGroup.ContainsKey(FaceCode) && effectForm.EffectLayerGroup[FaceCode].animated) ||
                (effectForm.EffectLayerGroup.ContainsKey(LongCoatCode) && effectForm.EffectLayerGroup[LongCoatCode].animated) ||
                (effectForm.EffectLayerGroup.ContainsKey(ShoesCode) && effectForm.EffectLayerGroup[ShoesCode].animated) ||
                (effectForm.EffectLayerGroup.ContainsKey(CapeCode) && effectForm.EffectLayerGroup[CapeCode].animated) ||
                (effectForm.EffectLayerGroup.ContainsKey(EffectCode) && effectForm.EffectLayerGroup[EffectCode].animated)
                )
            {
                if (!this.effectTimer.Enabled)
                {
                    this.effectTimer.Interval = 1;//미싫행중이면 타이머 가동후 즉시 tick이벤트 실시.
                    this.effectTimer.Start();
                }
            }
            else
            {
                EffectAnimateStop();
            }
        }

        private void EffectAnimateStop()
        {
            effectForm.AnimateAllStop();

            this.effectTimer.Stop();
        }
        private void AnimateUpdate()//콤보박스를 넘겨서 다음걸로 넘어가게 하기
        {
            this.SuspendUpdateDisplay();

            if (this.animator.BodyDelay == 0 && FindNextFrame(cmbBodyFrame))
            {
                this.animator.BodyDelay = (int)(cmbBodyFrame.SelectedItem as ComboItem).Tag;
            }

            if (this.animator.EmotionDelay == 0 && FindNextFrame(cmbEmotionFrame))
            {
                this.animator.EmotionDelay = (int)(cmbEmotionFrame.SelectedItem as ComboItem).Tag;
            }

            if (this.animator.TamingDelay == 0 && FindNextFrame(cmbTamingFrame))
            {
                this.animator.TamingDelay = (int)(cmbTamingFrame.SelectedItem as ComboItem).Tag;
            }

            this.ResumeUpdateDisplay();
        }

        private void AnimateStart()
        {
            TimerEnabledCheck();
            if (timer1.Enabled)
            {
                AnimateUpdate();
            }
        }

        private void TimerEnabledCheck()
        {
            if (chkBodyPlay.Checked || chkEmotionPlay.Checked || chkTamingPlay.Checked)
            {
                if (!this.timer1.Enabled)
                {
                    this.timer1.Interval = 1;
                    this.timer1.Start();
                }
            }
            else
            {
                AnimateStop();
            }
        }

        private void AnimateStop()
        {
            chkBodyPlay.Checked = false;
            chkEmotionPlay.Checked = false;
            chkTamingPlay.Checked = false;
            this.timer1.Stop();
        }

        private EffectForm.EffectLayer FindNextFrameE(EffectForm.EffectLayer eLayer)
        {
            int delay = 0;
            if (eLayer.delays != null)
            {
                int i = eLayer.currentFrame;
                i = (++i) % eLayer.delays.Length;
                delay = eLayer.delays[i];
                if (delay > 0)
                {
                    eLayer.currentFrame = i;
                }
            }
            return eLayer;
        }

        private bool FindNextFrame(ComboBoxEx cmbFrames)
        {
            ComboItem item = cmbFrames.SelectedItem as ComboItem;
            if (item == null)
            {
                if (cmbFrames.Items.Count > 0)
                {
                    cmbFrames.SelectedIndex = 0;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            int selectedIndex = cmbFrames.SelectedIndex;
            int i = selectedIndex;
            do
            {
                i = (++i) % cmbFrames.Items.Count;
                item = cmbFrames.Items[i] as ComboItem;
                if (item != null && item.Tag is int)
                {
                    int delay = (int)item.Tag;
                    if (delay > 0)
                    {
                        cmbFrames.SelectedIndex = i;
                        return true;
                    }
                }
            }
            while (i != selectedIndex);

            return false;
        }

        private void btnCode_Click(object sender, EventArgs e)
        {
            var dlg = new AvatarCodeForm();
            string code = GetAllPartsTag();
            dlg.CodeText = code;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if (dlg.CodeText != code && !string.IsNullOrEmpty(dlg.CodeText))
                {
                    LoadCode(dlg.CodeText, dlg.LoadType);
                }
            }
        }

        private void btnMale_Click(object sender, EventArgs e)
        {
            if (MessageBoxEx.Show("初始化為男性角色？", "提示") == DialogResult.OK)
            {
                LoadCode("2000,12000,20000,30000,1040036,1060026", 0);
            }
        }

        private void btnFemale_Click(object sender, EventArgs e)
        {
            if (MessageBoxEx.Show("初始化為女性角色？", "提示") == DialogResult.OK)
            {
                LoadCode("2000,12000,21000,31000,1041046,1061039", 0);
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            this.avatarContainer1.Origin = new Point(this.avatarContainer1.Width / 2, this.avatarContainer1.Height / 2 + 40);
            this.avatarContainer1.Invalidate();
        }

        private void LoadCode(string code, int loadType)
        {
            //解析
            var matches = Regex.Matches(code, @"(\d+)([,\s]|$)");
            if (matches.Count <= 0)
            {
                MessageBoxEx.Show("無法解析的裝備代碼。", "提示");
                return;
            }

            var characWz = PluginManager.FindWz(Wz_Type.Character);
            if (characWz == null)
            {
                 MessageBoxEx.Show("没有打開Character.wz。", "提示");
                return;
            }

            var itemWz = PluginManager.FindWz(Wz_Type.Item);
            if (itemWz == null)
            {
                MessageBoxEx.Show("沒有打開Item.wz。", "提示");
                return;
            }

            //试图初始化
            if (!this.inited && !this.AvatarInit())
            {
                MessageBoxEx.Show("Avatar初始化失敗。", "錯誤");
                return;
            }

            if (loadType == 0) //先清空。。
            {
                Array.Clear(this.avatar.Parts, 0, this.avatar.Parts.Length);
            }

            List<int> failList = new List<int>();

            foreach (Match m in matches)
            {
                int gearID;
                if (Int32.TryParse(m.Result("$1"), out gearID))
                {
                    Wz_Node imgNode = FindNodeByGearID(characWz, itemWz, gearID);
                    if (imgNode != null)
                    {
                        var part = this.avatar.AddPart(imgNode);
                        OnNewPartAdded(part);
                    }
                    else
                    {
                        failList.Add(gearID);
                    }
                }
            }

            //刷新
            this.FillAvatarParts();
            this.UpdateDisplay();

            //其他提示
            if (failList.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("以下部件没有找到：");
                foreach (var gearID in failList)
                {
                    sb.Append("  ").AppendLine(gearID.ToString("D8"));
                }
                MessageBoxEx.Show(sb.ToString(), "提示");
            }

        }

        private Wz_Node FindNodeByGearID(Wz_Node characWz, Wz_Node itemWz, int id)
        {
            string imgName = id.ToString("D8") + ".img";
            string nodeName = id.ToString("D8");
            Wz_Node imgNode = null;

            foreach (var node1 in characWz.Nodes)
            {
                if (node1.Text == imgName)
                {
                    imgNode = node1;
                    break;
                }
                else if (node1.Nodes.Count > 0)
                {
                    foreach (var node2 in node1.Nodes)
                    {
                        if (node2.Text == imgName)
                        {
                            imgNode = node2;
                            break;
                        }
                    }
                    if (imgNode != null)
                    {
                        break;
                    }
                }
            }

            foreach (var node in itemWz.FindNodeByPath(@"Cash\0501.img", true).Nodes)
            {
                if (node.Text == nodeName)
                {
                    return node;
                }
            }

            if (imgNode != null)
            {
                Wz_Image img = imgNode.GetValue<Wz_Image>();
                if (img != null && img.TryExtract())
                {
                    return img.Node;
                }
            }

            return null;
        }
        public class EffectAnimator
        {
            private int[] EffectDelays;
            public int NextFrameDelay { get; private set; }
            public EffectAnimator()
            {
                this.EffectDelays = new int[6] { -1, -1, -1, -1, -1, -1 };
            }

            public int HatDelay//모자 100
            {
                get { return this.EffectDelays[0]; }
                set
                {
                    this.EffectDelays[0] = value;
                    Update();
                }
            }

            public int FaceDelay//얼굴장식 101
            {
                get { return this.EffectDelays[1]; }
                set
                {
                    this.EffectDelays[1] = value;
                    Update();
                }
            }

            public int LongCoatDelay //한벌옷 105
            {
                get { return this.EffectDelays[2]; }
                set
                {
                    this.EffectDelays[2] = value;
                    Update();
                }
            }
            public int ShoesDelay //신발 107
            {
                get { return this.EffectDelays[3]; }
                set
                {
                    this.EffectDelays[3] = value;
                    Update();
                }
            }

            public int CapeDelay //망토 110
            {
                get { return this.EffectDelays[4]; }
                set
                {
                    this.EffectDelays[4] = value;
                    Update();
                }
            }

            public int EffectDelay
            {
                get { return this.EffectDelays[5]; }
                set
                {
                    this.EffectDelays[5] = value;
                    Update();
                }
            }

            public void Elapse(int millisecond)
            {
                //밀리초마다 실행되며, millisecond를 받아서 시간을 체크하는 용도.
                for (int i = 0; i < EffectDelays.Length; i++)
                {
                    if (EffectDelays[i] >= 0)
                    {
                        EffectDelays[i] = EffectDelays[i] > millisecond ? (EffectDelays[i] - millisecond) : 0;
                    }
                }
            }

            private void Update()//타이머 다음 딜레이 시간을 정하는 함수
            {
                int nextFrame = 0;
                foreach (int delay in this.EffectDelays)
                {
                    if (delay > 0)
                    {
                        nextFrame = nextFrame <= 0 ? delay : Math.Min(nextFrame, delay);
                    }
                }
                this.NextFrameDelay = nextFrame;
            }
        }

        private class Animator
        {
            public Animator()
            {
                this.delays = new int[3] { -1, -1, -1};
            }

            private int[] delays;

            public int NextFrameDelay { get; private set; }

            public int BodyDelay
            {
                get { return this.delays[0]; }
                set
                {
                    this.delays[0] = value;
                    Update();
                }
            }

            public int EmotionDelay
            {
                get { return this.delays[1]; }
                set
                {
                    this.delays[1] = value;
                    Update();
                }
            }

            public int TamingDelay
            {
                get { return this.delays[2]; }
                set
                {
                    this.delays[2] = value;
                    Update();
                }
            }

            public void Elapse(int millisecond)
            {
                for (int i = 0; i < delays.Length; i++)
                {
                    if (delays[i] >= 0)
                    {
                        delays[i] = delays[i] > millisecond ? (delays[i] - millisecond) : 0;
                    }
                }
            }

            private void Update()
            {
                int nextFrame = 0;
                foreach (int delay in this.delays)
                {
                    if (delay > 0)
                    {
                        nextFrame = nextFrame <= 0 ? delay : Math.Min(nextFrame, delay);
                    }
                }
                this.NextFrameDelay = nextFrame;
            }
        }

        private void buttonItem1_Click_1(object sender, EventArgs e)
        {
            this.PluginEntry.btnSetting_Click(sender, e);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            ExportAvatar(sender, e, false);
        }

        private void btnEffect_Click(object sender, EventArgs e)
        {
            //Effect(아이템이펙트)
            effectForm.Show();
            effectForm.Activate();
        }

        private void ExportAvatar(object sender, EventArgs e, bool all)
        {
            ComboItem selectedItem;
            //同步角色动作
            selectedItem = this.cmbActionBody.SelectedItem as ComboItem;
            this.avatar.ActionName = selectedItem != null ? selectedItem.Text : null;
            //同步表情
            selectedItem = this.cmbEmotion.SelectedItem as ComboItem;
            this.avatar.EmotionName = selectedItem != null ? selectedItem.Text : null;
            //同步骑宠动作
            selectedItem = this.cmbActionTaming.SelectedItem as ComboItem;
            this.avatar.TamingActionName = selectedItem != null && !all ? selectedItem.Text : null;

            //获取动作帧
            selectedItem = this.cmbBodyFrame.SelectedItem as ComboItem;
            int bodyFrame = selectedItem != null ? Convert.ToInt32(selectedItem.Text) : -1;
            selectedItem = this.cmbEmotionFrame.SelectedItem as ComboItem;
            int emoFrame = selectedItem != null ? Convert.ToInt32(selectedItem.Text) : -1;
            selectedItem = this.cmbTamingFrame.SelectedItem as ComboItem;
            int tamingFrame = selectedItem != null ? Convert.ToInt32(selectedItem.Text) : -1;

            //获取武器状态
            selectedItem = this.cmbWeaponType.SelectedItem as ComboItem;
            this.avatar.WeaponType = selectedItem != null ? Convert.ToInt32(selectedItem.Text) : 0;

            selectedItem = this.cmbWeaponIdx.SelectedItem as ComboItem;
            this.avatar.WeaponIndex = selectedItem != null ? Convert.ToInt32(selectedItem.Text) : 0;

            if (this.avatar.ActionName == null)
            {
                MessageBoxEx.Show("無人物。");
                return;
            }

            var config = Config.ImageHandlerConfig.Default;

            if (!all && !(this.chkBodyPlay.Checked || this.chkTamingPlay.Checked))
            {
                Bone bone;
                if (effectStruct.Count > 0)
                {
                    bone = avatar.CreateFrame(bodyFrame, emoFrame, tamingFrame, this.effectStruct, AvatarMixHair);
                }
                else
                {
                    bone = avatar.CreateFrame(bodyFrame, emoFrame, tamingFrame, null, AvatarMixHair);
                }
                var bmp = avatar.DrawFrame(bone);

                string pngFileName = "avatar"
                    + (string.IsNullOrEmpty(avatar.ActionName) ? "" : ("_" + avatar.ActionName + "(" + bodyFrame + ")"))
                    + (string.IsNullOrEmpty(avatar.EmotionName) ? "" : ("_" + avatar.EmotionName + "(" + emoFrame + ")"))
                    + (string.IsNullOrEmpty(avatar.TamingActionName) ? "" : ("_" + avatar.TamingActionName + "(" + tamingFrame + ")"))
                    + ".png";

                var dlg = new SaveFileDialog();
                dlg.Filter = "PNG (*.png)|*.png|全部檔案 (*.*)|*.*";
                dlg.FileName = pngFileName;
                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                pngFileName = dlg.FileName;

                bmp.Bitmap.Save(pngFileName, System.Drawing.Imaging.ImageFormat.Png);

                return;
            }

            var encParams = AnimateEncoderFactory.GetEncoderParams(config.GifEncoder.Value);

            string aniFileName = "avatar"
                    + (string.IsNullOrEmpty(avatar.ActionName) ? "" : ("_" + avatar.ActionName))
                    + (string.IsNullOrEmpty(avatar.EmotionName) ? "" : ("_" + avatar.EmotionName))
                    + (string.IsNullOrEmpty(avatar.TamingActionName) ? "" : ("_" + avatar.TamingActionName))
                    + encParams.FileExtension;

            if (!all)
            {
                var dlg = new SaveFileDialog();

                dlg.Filter = string.Format("{0} (*{1})|*{1}|全部檔案(*.*)|*.*", encParams.FileDescription, encParams.FileExtension);
                dlg.FileName = aniFileName;

                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
                aniFileName = dlg.FileName;

                ExportGif(bodyFrame, emoFrame, tamingFrame, aniFileName, config);
                MessageBoxEx.Show("圖片儲存完成: " + aniFileName);
            }
            else
            {
            }
        }
        
        private void ExportGif(int bodyFrame, int emoFrame, int tamingFrame, string fileName, Config.ImageHandlerConfig config)
        {
            Gif gif = new Gif();
            var actionFrames = avatar.GetActionFrames(avatar.ActionName);
            var faceFrames = avatar.GetFaceFrames(avatar.EmotionName);
            var tamingFrames = avatar.GetTamingFrames(avatar.TamingActionName);
            int HatCode = 0, FaceCode = 0, LongCoatCode = 0, ShoesCode = 0, CapeCode = 0, EffectCode = 0;
            int HatMaxFrame = 0, FaceMaxFrame = 0, LongCoatMaxFrame = 0, ShoesMaxFrame = 0, CapeMaxFrame = 0, EffectMaxFrame = 0;
            foreach (var lys in effectForm.EffectLayerGroup)
            {
                switch ((int)(lys.Key / 10000))
                {
                    case 100:
                        HatCode = lys.Key;
                        HatMaxFrame = lys.Value.delays.Length;
                        break;
                    case 101:
                        FaceCode = lys.Key;
                        FaceMaxFrame = lys.Value.delays.Length;
                        break;
                    case 105:
                        LongCoatCode = lys.Key;
                        LongCoatMaxFrame = lys.Value.delays.Length;
                        break;
                    case 107:
                        ShoesCode = lys.Key;
                        ShoesMaxFrame = lys.Value.delays.Length;
                        break;
                    case 110:
                        CapeCode = lys.Key;
                        CapeMaxFrame = lys.Value.delays.Length;
                        break;
                    case 501:
                        EffectCode = lys.Key;
                        EffectMaxFrame = lys.Value.delays.Length;
                        break;

                    default:
                        break;
                }
            }
            int tp = 0;
            if (emoFrame <= -1 || emoFrame >= faceFrames.Length)
            {
                return;
            }

            foreach (var frame in string.IsNullOrEmpty(avatar.TamingActionName) ? actionFrames : tamingFrames)
            {
                if (frame.Delay != 0)
                {
                    if (HatMaxFrame > 0)
                    {
                        ChangeEffectStruct(HatCode, tp%HatMaxFrame);
                    }
                    if (FaceMaxFrame > 0)
                    {
                        ChangeEffectStruct(FaceCode, tp%FaceMaxFrame);
                    }
                    if (LongCoatMaxFrame > 0)
                    {
                        ChangeEffectStruct(LongCoatCode, tp%LongCoatMaxFrame);
                    }
                    if (ShoesMaxFrame > 0)
                    {
                        ChangeEffectStruct(ShoesCode, tp%ShoesMaxFrame);
                    }
                    if (CapeMaxFrame > 0)
                    {
                        ChangeEffectStruct(CapeCode, tp%CapeMaxFrame);
                    }
                    if (EffectMaxFrame > 0)
                    {
                        ChangeEffectStruct(EffectCode, tp % EffectMaxFrame);
                    }
                    Bone bone;
                    if (effectStruct.Count > 0)
                    {
                        bone = string.IsNullOrEmpty(avatar.TamingActionName) ? avatar.CreateFrame(frame, faceFrames[emoFrame], null, effectStruct, AvatarMixHair) : avatar.CreateFrame(actionFrames[bodyFrame], faceFrames[emoFrame], frame, effectStruct, AvatarMixHair);
                    }
                    else
                    {
                        bone = string.IsNullOrEmpty(avatar.TamingActionName) ? avatar.CreateFrame(frame, faceFrames[emoFrame], null, null, AvatarMixHair) : avatar.CreateFrame(actionFrames[bodyFrame], faceFrames[emoFrame], frame, null, AvatarMixHair);
                    }
                    var bmp = avatar.DrawFrame(bone);

                    Point pos = bmp.OpOrigin;
                    pos.Offset(frame.Flip ? new Point(-frame.Move.X, frame.Move.Y) : frame.Move);
                    GifFrame f = new GifFrame(bmp.Bitmap, new Point(-pos.X, -pos.Y), Math.Abs(frame.Delay));
                    gif.Frames.Add(f);
                    tp+=1;
                }
            }

            GifEncoder enc = AnimateEncoderFactory.CreateEncoder(fileName, gif.GetRect().Width, gif.GetRect().Height, config);
            gif.SaveGif(enc, fileName, Color.Transparent);
        }
        public void FillCmbEffect(int itemcode)//자세/선택한 아이템이 바뀌면 이펙트 번호 다시 들고와야함.
        {
            //아이템 이름 바꾸기
            var stringLinker = this.PluginEntry.Context.DefaultStringLinker;
            StringResult sr;
            string itemname = "";
            Wz_Node searchNode = null;
            Wz_Node FrameNode = null;
            string Action = "default";
            for(int i = 0; i < avatar.Parts.Length; i++)
            {
                if(avatar.Parts[i] != null)
                {
                    if((avatar.Parts[i] != null) && (avatar.Parts[i].ID.Value == itemcode))
                    {
                        searchNode = avatar.Parts[i].ItemEff;
                        if (stringLinker.StringEqp.TryGetValue(avatar.Parts[i].ID.Value, out sr) || stringLinker.StringItem.TryGetValue(avatar.Parts[i].ID.Value, out sr))
                        {
                            itemname = sr.Name;
                        }
                        break;
                    }
                }
            }
            /*foreach (var pts in avatar.Parts)
            {
                if(pts != null)
                {
                    if ((pts.ItemEff != null) && (pts.ID.Value == itemcode))
                    {
                        searchNode = pts.ItemEff;
                        if (stringLinker.StringEqp.TryGetValue(pts.ID.Value, out sr))
                        {
                            itemname = sr.Name;
                        }
                        break;
                    }
                }
            }*/
            effectForm.ItemDescBox.Text = itemcode.ToString() + " : " + itemname;
            //이펙트 번호 들고 오기
            var oldSelection = Convert.ToInt32(effectForm.EffectComboBox.SelectedItem);
            effectForm.EffectComboBox.Items.Clear();
            if (searchNode != null)
            {
                Action = effectForm.EffectTextBox.Text;
                if(string.IsNullOrEmpty(Action))
                {
                    Action = "default";
                }
                //자식 노드 찾기
                FrameNode = searchNode.FindNodeByPath(Action);
                if(FrameNode == null)
                {
                    FrameNode = searchNode.FindNodeByPath("default");
                }
                foreach (var childnode in FrameNode.Nodes)
                {
                    if(int.TryParse(childnode.Text,out int frameNum))
                    {
                        effectForm.EffectComboBox.Items.Add(frameNum.ToString());
                    }
                }
                EffectForm.EffectLayer eLayer;
                eLayer = effectForm.EffectLayerGroup[itemcode];
                eLayer.delays = FindEffectDelay(FrameNode);
                eLayer.currentFrame = 0;
                effectForm.EffectLayerGroup[itemcode] = eLayer;
                //effectForm.effectDelayList.Add(FindEffectDelay(FrameNode));
            }
            effectForm.EffectComboBox.SelectedItem = 0;
        }

        public int[] FindEffectDelay(Wz_Node targetNode) //이펙트에서 default, walk1 이런 노드를 받아서 돌려주는거.
        {
            int framecount = targetNode.Nodes.Count;
            List<int> frameList = new List<int>(new int[framecount]);
            Wz_Node delayNode;
            foreach (var childnode in targetNode.Nodes)
            {
                if (int.TryParse(childnode.Text, out int frameNum))
                {
                    delayNode = childnode.FindNodeByPath("delay");
                    if(delayNode == null)
                    {
                        frameList[frameNum] = 100;
                    }
                    else
                    {
                        frameList[frameNum] = Convert.ToInt32(delayNode.Value);
                    }
                }
            }
            frameList.RemoveAll(frms => frms == 0);
            return frameList.ToArray();
        }
        public void FillEffectListbox()//리스트박스 채우기
        {
            effectForm.ItemEffectListBox1.Items.Clear();
            foreach(var pts in avatar.Parts)
            {
                if((pts!=null) && pts.Visible && (pts.ItemEff!=null))
                {
                    effectForm.ItemEffectListBox1.Items.Add(pts.ID.Value);
                }
            }
        }

        public void ChangeEffectStruct(int itemcode, int Frame)//그 아이템의 프레임 바꾸는 함수
        {
            foreach(EffectStruction es in effectStruct)
            {
                if(es.itemcode == itemcode)
                {
                    es.frame = Frame;
                    break;
                }
            }
        }

        public void ChangeMixHair(int colorcode, int opacity)
        {
            AvatarMixHair.MixHairColor = colorcode;
            AvatarMixHair.MixHairOpacity = opacity;
            UpdateDisplay();
        }
    }
}