using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using DevComponents.Editors;
using DevComponents.DotNetBar.Controls;
using WzComparerR2.Common;
using WzComparerR2.WzLib;
using WzComparerR2.PluginBase;

namespace WzComparerR2.Avatar.UI
{
    internal partial class AvatarForm : DevComponents.DotNetBar.OfficeForm
    {
        public AvatarForm()
        {
            InitializeComponent();
            this.avatar = new AvatarCanvas();
            this.animator = new Animator();
            this.avatarContainer1.Origin = new Point(this.avatarContainer1.Width / 2, this.avatarContainer1.Height / 2);
            FillWeaponIdx();
            chkHairOverHead.Checked = true;
            
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

        /// <summary>
        /// wz1节点选中事件。
        /// </summary>
        public void OnSelectedNode1Changed(object sender, WzNodeEventArgs e)
        {
            if (PluginEntry.Context.SelectedTab != PluginEntry.Tab || e.Node == null)
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
                }
                else if (part == avatar.Weapon) //同步武器类型
                {
                    FillWeaponTypes();
                }

                this.FillAvatarParts();
                UpdateDisplay();
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
        private void UpdateDisplay()
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


            string actionTag = string.Format("{0}:{1},{2}:{3},{4}:{5},{6},{7},{8},{9}",
                this.avatar.ActionName,
                bodyFrame,
                this.avatar.EmotionName,
                emoFrame,
                this.avatar.TamingActionName,
                tamingFrame,
                this.avatar.ShowhairOverHead ? 1 : 0,
                this.avatar.ShowEar ? 1 : 0,
                this.avatar.WeaponType,
                this.avatar.WeaponIndex);

            var actionFrames = avatar.GetActionFrames(avatar.ActionName);
            ActionFrame f = null;
            if (bodyFrame > -1 && bodyFrame < actionFrames.Length)
            {
                f = actionFrames[bodyFrame];
            }

            if (!avatarContainer1.HasCache(actionTag))
            {
                try
                {
                    var bone = avatar.CreateFrame(bodyFrame, emoFrame, tamingFrame);
                    var bmp = avatar.DrawFrame(bone, f);
                    avatarContainer1.AddCache(actionTag, bmp);
                }
                catch
                {
                }
            }

            avatarContainer1.SetKey(actionTag);
        }

        private string GetAllPartsTag()
        {
            string[] partsID = new string[avatar.Parts.Length];
            for (int i = 0; i < avatar.Parts.Length; i++)
            {
                var part = avatar.Parts[i];
                if (part != null && part.Visible)
                {
                    partsID[i] = part.ID.ToString();
                }
            }
            return string.Join(",", partsID);
        }

        private void buttonItem2_Click(object sender, EventArgs e)
        {
            this.avatar.LoadZ();
            this.avatar.LoadActions();
            this.avatar.LoadEmotions();

            FillBodyAction();
            FillEmotion();
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
        }

        void AddPart(string imgPath)
        {
            Wz_Node imgNode = PluginManager.FindWz(imgPath);
            if (imgNode != null)
            {
                this.avatar.AddPart(imgNode);
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

        /// <summary>
        /// 更新当前显示部件列表。
        /// </summary>
        private void FillAvatarParts()
        {
            itemPanel1.BeginUpdate();
            itemPanel1.Items.Clear();
            foreach (var part in avatar.Parts)
            {
                if (part != null)
                {
                    AvatarPartButtonItem btn = new AvatarPartButtonItem();
                    var stringLinker = this.PluginEntry.Context.DefaultStringLinker;
                    StringResult sr;
                    string text;
                    if (part.ID != null && stringLinker.StringEqp.TryGetValue(part.ID.Value, out sr))
                    {
                        text = string.Format("{0}\r\n{1}", sr.Name, part.ID);
                    }
                    else
                    {
                        text = string.Format("{0}\r\n{1}", "(null)", part.ID == null ? "-" : part.ID.ToString());
                    }
                    btn.Text = text;
                    btn.Image = part.Icon.Bitmap;
                    itemPanel1.Items.Add(btn);
                }
            }
            itemPanel1.EndUpdate();
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
            int i = 0;
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
            foreach(var f in frames)
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
            this.ResumeUpdateDisplay();
            UpdateDisplay();
        }

        private void cmbBodyFrame_SelectedIndexChanged(object sender, EventArgs e)
        {
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

        private void chkHairOverHead_CheckedChanged(object sender, EventArgs e)
        {
            avatar.ShowhairOverHead = chkHairOverHead.Checked;
            UpdateDisplay();
        }

        private void chkEar_CheckedChanged(object sender, EventArgs e)
        {
            avatar.ShowEar = chkEar.Checked;
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

        private void AnimateUpdate()
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

        private class Animator
        {
            public Animator()
            {
                this.delays = new int[3] { -1, -1, -1 };
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
    }
}