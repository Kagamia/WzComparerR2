namespace WzComparerR2.Avatar.UI
{
    partial class AvatarPartButtonItem
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.btnItemShow = new DevComponents.DotNetBar.ButtonItem();
            this.btnItemDel = new DevComponents.DotNetBar.ButtonItem();
            // 
            // btnItemShow
            // 
            this.btnItemShow.Name = "btnItemShow";
            this.btnItemShow.Text = "顯示/隱藏";
            // 
            // btnItemDel
            // 
            this.btnItemDel.Name = "btnItemDel";
            this.btnItemDel.Text = "移除";
            // 
            // AvatarPartButtonItem
            // 
            this.AutoCheckOnClick = true;
            this.ButtonStyle = DevComponents.DotNetBar.eButtonStyle.ImageAndText;
            this.GlobalItem = false;
            this.ImageFixedSize = new System.Drawing.Size(32, 32);
            this.SubItems.AddRange(new DevComponents.DotNetBar.BaseItem[] {
            this.btnItemShow,
            this.btnItemDel});
            this.SubItemsExpandWidth = 16;

        }

        #endregion

        public DevComponents.DotNetBar.ButtonItem btnItemShow;
        public DevComponents.DotNetBar.ButtonItem btnItemDel;
    }
}
