using System;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using CharaSimResource;
using WzComparerR2.CharaSim;
using WzComparerR2.Common;
using WzComparerR2.Controls;

namespace WzComparerR2.CharaSimControl
{
    public class AfrmStat : AlphaForm
    {
        public AfrmStat()
        {
            initCtrl();
        }

        private bool fullMode;
        private Character character;

        private ACtrlButton btnHPUp;
        private ACtrlButton btnMPUp;
        private ACtrlButton btnStrUp;
        private ACtrlButton btnDexUp;
        private ACtrlButton btnIntUp;
        private ACtrlButton btnLukUp;
        private ACtrlButton btnAuto;
        private ACtrlButton btnClose;
        private ACtrlButton btnDetailOpen;
        private ACtrlButton btnDetailClose;
        private bool waitForRefresh;

        public Character Character
        {
            get { return character; }
            set { character = value; }
        }

        private void initCtrl()
        {
            this.btnHPUp = new ACtrlButton();
            this.btnHPUp.Location = new Point(167, 157);

            this.btnMPUp = new ACtrlButton();
            this.btnMPUp.Location = new Point(167, 175);

            this.btnStrUp = new ACtrlButton();
            this.btnStrUp.Location = new Point(167, 262);

            this.btnDexUp = new ACtrlButton();
            this.btnDexUp.Location = new Point(167, 280);

            this.btnIntUp = new ACtrlButton();
            this.btnIntUp.Location = new Point(167, 298);

            this.btnLukUp = new ACtrlButton();
            this.btnLukUp.Location = new Point(167, 316);

            ACtrlButton[] addBtnList = new ACtrlButton[] { btnHPUp, btnMPUp, btnStrUp, btnDexUp, btnIntUp, btnLukUp };
            for (int i = 0; i < addBtnList.Length; i++)
            {
                addBtnList[i].Normal = new BitmapOrigin(Resource.Stat_main_BtUp_normal_0);
                addBtnList[i].MouseOver = new BitmapOrigin(Resource.Stat_main_BtUp_mouseOver_0);
                addBtnList[i].Pressed = new BitmapOrigin(Resource.Stat_main_BtUp_pressed_0);
                addBtnList[i].Disabled = new BitmapOrigin(Resource.Stat_main_BtUp_disabled_0);
                addBtnList[i].Size = new Size(12, 12);
                addBtnList[i].ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);
            }

            this.btnClose = new ACtrlButton();
            this.btnClose.Normal = new BitmapOrigin(Resource.BtClose3_normal_0);
            this.btnClose.Pressed = new BitmapOrigin(Resource.BtClose3_pressed_0);
            this.btnClose.MouseOver = new BitmapOrigin(Resource.BtClose3_mouseOver_0);
            this.btnClose.Disabled = new BitmapOrigin(Resource.BtClose3_disabled_0);
            this.btnClose.Location = new Point(170, 6);
            this.btnClose.Size = new Size(13, 13);
            this.btnClose.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);
            this.btnClose.MouseClick += new MouseEventHandler(btnClose_MouseClick);

            this.btnDetailOpen = new ACtrlButton();
            this.btnDetailOpen.Normal = new BitmapOrigin(Resource.Stat_main_BtDetailOpen_normal_0);
            this.btnDetailOpen.Pressed = new BitmapOrigin(Resource.Stat_main_BtDetailOpen_pressed_0);
            this.btnDetailOpen.MouseOver = new BitmapOrigin(Resource.Stat_main_BtDetailOpen_mouseOver_0);
            this.btnDetailOpen.Disabled = new BitmapOrigin(Resource.Stat_main_BtDetailOpen_disabled_0);
            this.btnDetailOpen.Location = new Point(112,344);
            this.btnDetailOpen.Size = new Size(68, 16);
            this.btnDetailOpen.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);
            this.btnDetailOpen.MouseClick += new MouseEventHandler(btnDetailOpen_MouseClick);

            this.btnDetailClose = new ACtrlButton();
            this.btnDetailClose.Normal = new BitmapOrigin(Resource.Stat_main_BtDetailClose_normal_0);
            this.btnDetailClose.Pressed = new BitmapOrigin(Resource.Stat_main_BtDetailClose_pressed_0);
            this.btnDetailClose.MouseOver = new BitmapOrigin(Resource.Stat_main_BtDetailClose_mouseOver_0);
            this.btnDetailClose.Disabled = new BitmapOrigin(Resource.Stat_main_BtDetailClose_disabled_0);
            this.btnDetailClose.Location = new Point(112, 344);
            this.btnDetailClose.Size = new Size(68, 16);
            this.btnDetailClose.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);
            this.btnDetailClose.MouseClick += new MouseEventHandler(btnDetailClose_MouseClick);

            this.btnAuto = new ACtrlButton();
            this.btnAuto.Normal = new BitmapOrigin(Resource.Stat_main_BtAuto_normal_3);
            this.btnAuto.Pressed = new BitmapOrigin(Resource.Stat_main_BtAuto_pressed_0);
            this.btnAuto.MouseOver = new BitmapOrigin(Resource.Stat_main_BtAuto_mouseOver_0);
            this.btnAuto.Disabled = new BitmapOrigin(Resource.Stat_main_BtAuto_disabled_0);
            this.btnAuto.Location = new Point(108, 217);
            this.btnAuto.Size = new Size(67, 34);
            this.btnAuto.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);
        }

        private IEnumerable<AControl> aControls
        {
            get
            {
                yield return btnHPUp;
                yield return btnMPUp;
                yield return btnStrUp;
                yield return btnDexUp;
                yield return btnIntUp;
                yield return btnLukUp;
                yield return btnAuto;
                yield return btnClose;
                yield return btnDetailOpen;
                yield return btnDetailClose;
            }
        }

        public override void Refresh()
        {
            this.preRender();
            this.SetBitmap(this.Bitmap);
            this.CaptionRectangle = new Rectangle(0, 0, this.Bitmap.Width, 24);
            base.Refresh();
        }

        protected override bool captionHitTest(Point point)
        {
            if (this.btnClose.Rectangle.Contains(point))
                return false;
            return base.captionHitTest(point);
        }

        private void preRender()
        {
            if (Bitmap != null)
                Bitmap.Dispose();

            setControlState();

            Size size = Resource.Stat_main_backgrnd.Size;
            if (fullMode)
                size = new Size(size.Width + Resource.Stat_detail_backgrnd.Width, size.Height);

            //绘制背景
            Bitmap stat = new Bitmap(size.Width, size.Height);
            Graphics g = Graphics.FromImage(stat);
            renderBase(g);
            if (fullMode)
                renderDetail(g);

            //绘制按钮
            foreach (AControl ctrl in this.aControls)
            {
                ctrl.Draw(g);
            }

            g.Dispose();
            this.Bitmap = stat;
        }

        private void setControlState()
        {
            if (this.fullMode)
            {
                this.btnDetailOpen.Visible = true;
                this.btnDetailClose.Visible = false;
            }
            else
            {
                this.btnDetailOpen.Visible = false;
                this.btnDetailClose.Visible = true; 
            }

            if (this.character != null)
            {
                CharacterStatus charStat = this.character.Status;
                setButtonEnabled(this.btnHPUp, charStat.Ap > 0 && charStat.MaxHP.BaseVal < charStat.MaxHP.TotalMax);
                setButtonEnabled(this.btnMPUp, charStat.Ap > 0 && charStat.MaxMP.BaseVal < charStat.MaxMP.TotalMax);
                setButtonEnabled(this.btnStrUp, charStat.Ap > 0 && charStat.Strength.BaseVal <= 999);
                setButtonEnabled(this.btnDexUp, charStat.Ap > 0 && charStat.Dexterity.BaseVal <= 999);
                setButtonEnabled(this.btnIntUp, charStat.Ap > 0 && charStat.Intelligence.BaseVal <= 999);
                setButtonEnabled(this.btnLukUp, charStat.Ap > 0 && charStat.Luck.BaseVal <= 999);
                setButtonEnabled(this.btnAuto, charStat.Ap > 0);
            }
            else
            {
                foreach (AControl ctrl in this.aControls)
                {
                    setButtonEnabled(ctrl as ACtrlButton, true);
                }
            }
        }

        private void setButtonEnabled(ACtrlButton button, bool enabled)
        {
            if (button == null)
                return;
            if (enabled)
            {
                if (button.State == ButtonState.Disabled)
                {
                    button.State = ButtonState.Normal;
                }
            }
            else
            {
                if (button.State != ButtonState.Disabled)
                {
                    button.State = ButtonState.Disabled;
                }
            }
        }

        private void renderBase(Graphics g)
        {
            g.DrawImage(Resource.Stat_main_backgrnd, 0, 0);
            g.DrawImage(Resource.Stat_main_backgrnd2, 6, 22);
            g.DrawImage(Resource.Stat_main_backgrnd3, 7, 211);

            if (this.character != null)
            {
                CharacterStatus charStat = this.character.Status;
                //绘制自动分配
               // g.DrawImage(charStat.Ap > 0 ? Resource.Stat_main_BtAuto_normal_3 : Resource.Stat_main_BtAuto_disabled_0, 94, 180);
                switch (charStat.Job / 100 % 10)//绘制角色属性灰色背景
                {
                    case 0:
                    case 1:
                    case 3:
                    case 5:
                        g.DrawImage(Resource.Stat_main_Disabled_INT, 11, 296);
                        g.DrawImage(Resource.Stat_main_Disabled_LUK, 11, 314);
                        break;
                    case 2:
                        g.DrawImage(Resource.Stat_main_Disabled_STR, 11, 260);
                        g.DrawImage(Resource.Stat_main_Disabled_DEX, 11, 278);
                        break;
                    case 4:
                        g.DrawImage(Resource.Stat_main_Disabled_STR, 11, 260);
                        g.DrawImage(Resource.Stat_main_Disabled_INT, 11, 296);
                        break;
                }
                g.DrawString(this.character.Name, GearGraphics.ItemDetailFont, GearGraphics.StatDetailGrayBrush, 72f, 32f);
                g.DrawString(ItemStringHelper.GetJobName(charStat.Job), GearGraphics.ItemDetailFont, GearGraphics.StatDetailGrayBrush, 72f, 50f);
                g.DrawString(charStat.Level.ToString(), GearGraphics.ItemDetailFont, GearGraphics.StatDetailGrayBrush, 72f, 68f);
                g.DrawString(charStat.Exp + " (" + (charStat.Exp == -1 ? 0 : ((long)charStat.Exp * 100 / charStat.Exptnl)) + "%)",
                    GearGraphics.ItemDetailFont, GearGraphics.StatDetailGrayBrush, 72f, 86f);
                g.DrawString("1", GearGraphics.ItemDetailFont, GearGraphics.StatDetailGrayBrush, 72f, 104f);
                g.DrawString("0 (0%)", GearGraphics.ItemDetailFont, GearGraphics.StatDetailGrayBrush, 72f, 122f);
                g.DrawString(string.IsNullOrEmpty(this.character.Guild) ? "-" : this.character.Guild, GearGraphics.ItemDetailFont, GearGraphics.StatDetailGrayBrush, 72f, 140f);
                g.DrawString(charStat.HP + " / " + charStat.MaxHP.GetSum(), GearGraphics.ItemDetailFont, GearGraphics.StatDetailGrayBrush, 72f, 158f);
                g.DrawString(charStat.MP + " / " + charStat.MaxMP.GetSum(), GearGraphics.ItemDetailFont, GearGraphics.StatDetailGrayBrush, 72f, 176f);
                g.DrawString(charStat.Pop.ToString(), GearGraphics.ItemDetailFont, GearGraphics.StatDetailGrayBrush, 72f, 194f);

                g.DrawString(charStat.Ap.ToString().PadLeft(4), GearGraphics.ItemDetailFont, GearGraphics.StatDetailGrayBrush, 64f, 236f);
                g.DrawString(charStat.Strength.ToString(), GearGraphics.ItemDetailFont, GearGraphics.StatDetailGrayBrush, 72f, 263f);
                g.DrawString(charStat.Dexterity.ToString(), GearGraphics.ItemDetailFont, GearGraphics.StatDetailGrayBrush, 72f, 281f);
                g.DrawString(charStat.Intelligence.ToString(), GearGraphics.ItemDetailFont, GearGraphics.StatDetailGrayBrush, 72f, 299f);
                g.DrawString(charStat.Luck.ToString(), GearGraphics.ItemDetailFont, GearGraphics.StatDetailGrayBrush, 72f, 317f);
            }
        }

        private void renderDetail(Graphics g)
        {
            g.TranslateTransform(Resource.Stat_main_backgrnd.Width,
                Resource.Stat_main_backgrnd.Height - Resource.Stat_detail_backgrnd.Height);
            g.DrawImage(Resource.Stat_detail_backgrnd, 0, 0);
            g.DrawImage(Resource.Stat_detail_backgrnd2, 6, 7);

            if (this.character != null)
            {
                CharacterStatus charStat = this.character.Status;
                //g.DrawString("0 ( 0% )", GearGraphics.GearDetailFont, getDetailBrush(0), 72f, 16f);
                //g.DrawString("0", GearGraphics.GearDetailFont, getDetailBrush(0), 72f, 34f);
                int brushSign;

                double max, min;
                this.character.CalcAttack(out max, out min, out brushSign);
                float y = 16f;
                g.DrawString(max == 0 ? "0" : Math.Round(min) + " ~ " + Math.Round(max), GearGraphics.ItemDetailFont, getDetailBrush(brushSign), 72f, y);
                g.DrawString(charStat.CriticalRate.GetSum() + "%", GearGraphics.ItemDetailFont, charStat.CriticalRate.BuffAdd > 0 ? Brushes.Red : GearGraphics.StatDetailGrayBrush, 72f, (y += 18f));
                g.DrawString(charStat.PDDamage.ToStringDetail(out brushSign), GearGraphics.ItemDetailFont, getDetailBrush(brushSign), 72f, (y += 18f));
                g.DrawString(charStat.MDDamage.ToStringDetail(out brushSign), GearGraphics.ItemDetailFont, getDetailBrush(brushSign), 72f, (y += 18f));
                g.DrawString(charStat.PAccurate.ToStringDetail(out brushSign), GearGraphics.ItemDetailFont, getDetailBrush(brushSign), 72f, (y += 18f));
                g.DrawString(charStat.MAccurate.ToStringDetail(out brushSign), GearGraphics.ItemDetailFont, getDetailBrush(brushSign), 72f, (y += 18f));
                g.DrawString(charStat.PEvasion.ToStringDetail(out brushSign), GearGraphics.ItemDetailFont, getDetailBrush(brushSign), 72f, (y += 18f));
                g.DrawString(charStat.MEvasion.ToStringDetail(out brushSign), GearGraphics.ItemDetailFont, getDetailBrush(brushSign), 72f, (y += 18f));
                g.DrawString(charStat.MoveSpeed.GetSum() + "%", GearGraphics.ItemDetailFont, getDetailBrush(0), 72f, (y += 18f));
                g.DrawString(charStat.Jump.GetSum() + "%", GearGraphics.ItemDetailFont, getDetailBrush(0), 72f, (y += 18f));
            }

            g.ResetTransform();
        }

        private Brush getDetailBrush(int sign)
        {
            switch (sign)
            {
                case 1: return Brushes.Red;
                case -1: return Brushes.Blue;
                case 0:
                default: return GearGraphics.StatDetailGrayBrush;
            }

        }

        private void btnClose_MouseClick(object sender, MouseEventArgs e)
        {
            this.Visible = false;
        }

        private void aCtrl_RefreshCall(object sender, EventArgs e)
        {
            this.waitForRefresh = true;
        }

        private void btnDetailOpen_MouseClick(object sender, MouseEventArgs e)
        {
            this.fullMode = false;
            this.waitForRefresh = true;
        }

        private void btnDetailClose_MouseClick(object sender, MouseEventArgs e)
        {
            this.fullMode = true;
            this.waitForRefresh = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            foreach (AControl ctrl in this.aControls)
            {
                ctrl.OnMouseMove(e);
            }

            if (this.waitForRefresh)
            {
                this.Refresh();
                waitForRefresh = false;
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            foreach (AControl ctrl in this.aControls)
            {
                ctrl.OnMouseDown(e);
            }

            if (this.waitForRefresh)
            {
                this.Refresh();
                waitForRefresh = false;
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            foreach (AControl ctrl in this.aControls)
            {
                ctrl.OnMouseUp(e);
            }

            if (this.waitForRefresh)
            {
                this.Refresh();
                waitForRefresh = false;
            }

            base.OnMouseUp(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            foreach (AControl ctrl in this.aControls)
            {
                ctrl.OnMouseClick(e);
            }

            if (this.waitForRefresh)
            {
                this.Refresh();
                waitForRefresh = false;
            }

            base.OnMouseClick(e);
        }
    }
}
