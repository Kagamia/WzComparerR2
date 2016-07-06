using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows.Forms;
using System.Drawing;
using System.Text;
using CharaSimResource;
using WzComparerR2.CharaSim;
using WzComparerR2.Common;
using WzComparerR2.Controls;

namespace WzComparerR2.CharaSimControl
{
    public class AfrmEquip : AlphaForm
    {
        public AfrmEquip()
        {
            sec = new int[5];
            for (int i = 0; i < sec.Length; i++)
                sec[i] = 1 << i;

            initCtrl();
            this.AllowDrop = true;
            this.TotemVisible = true;
        }

        private BitVector32 partVisible;
        private int[] sec;

        private Point baseOffset;
        private Point newLocation;
        private bool waitForRefresh;
        private Character character;

        private ACtrlButton btnPet;
        private ACtrlButton btnDragon;
        private ACtrlButton btnMechanic;
        private ACtrlButton btnAndroid;
        private ACtrlButton btnClose;

        public Character Character
        {
            get { return character; }
            set { character = value; }
        }

        public bool PetVisible
        {
            get { return partVisible[sec[0]]; }
            private set { partVisible[sec[0]] = value; }
        }

        public bool DragonVisible
        {
            get { return partVisible[sec[1]]; }
            private set
            {
                partVisible[sec[1]] = value;
                if (value)
                {
                    partVisible[sec[2]] = false;
                    partVisible[sec[3]] = false;
                }
            }
        }

        public bool MechanicVisible
        {
            get { return partVisible[sec[2]]; }
            private set
            {
                partVisible[sec[2]] = value;
                if (value)
                {
                    partVisible[sec[1]] = false;
                    partVisible[sec[3]] = false;
                }
            }
        }

        public bool AndroidVisible
        {
            get { return partVisible[sec[3]]; }
            private set
            {
                partVisible[sec[3]] = value;
                if (value)
                {
                    partVisible[sec[1]] = false;
                    partVisible[sec[2]] = false;
                }
            }
        }

        public bool TotemVisible
        {
            get { return partVisible[sec[4]]; }
            private set { partVisible[sec[4]] = value; }
        }

        private Rectangle DragonRect
        {
            get
            {
                return new Rectangle(
                    new Point(baseOffset.X - Resource.Equip_dragon_backgrnd.Width, baseOffset.Y),
                    Resource.Equip_dragon_backgrnd.Size);
            }
        }

        private Rectangle MechanicRect
        {
            get
            {
                return new Rectangle(
                    new Point(baseOffset.X - Resource.Equip_mechanic_backgrnd.Width, baseOffset.Y),
                    Resource.Equip_mechanic_backgrnd.Size);
            }
        }

        private Rectangle AndroidRect
        {
            get
            {
                return new Rectangle(
                    new Point(baseOffset.X - Resource.Equip_Android_backgrnd.Width, baseOffset.Y),
                    Resource.Equip_Android_backgrnd.Size);
            }
        }

        private Rectangle PetRect
        {
            get
            {
                return new Rectangle(
                    new Point(baseOffset.X + Resource.Equip_character_backgrnd.Width,
                        baseOffset.Y + Resource.Equip_character_backgrnd.Height - Resource.Equip_pet_backgrnd.Height),
                    Resource.Equip_pet_backgrnd.Size);
            }
        }

        private Rectangle TotemRect
        {
            get
            {
                return new Rectangle(
                    new Point(baseOffset.X - Resource.Equip_totem_backgrnd.Width,
                        baseOffset.Y + Resource.Equip_character_backgrnd.Height - Resource.Equip_totem_backgrnd.Height),
                    Resource.Equip_pet_backgrnd.Size);
            }
        }

        private void initCtrl()
        {
            this.btnPet = new ACtrlButton();
            this.btnPet.Normal = new BitmapOrigin(Resource.Equip_character_BtPet_normal_0);
            this.btnPet.Pressed = new BitmapOrigin(Resource.Equip_character_BtPet_pressed_0);
            this.btnPet.MouseOver = new BitmapOrigin(Resource.Equip_character_BtPet_mouseOver_0);
            this.btnPet.Disabled = new BitmapOrigin(Resource.Equip_character_BtPet_disabled_0);
            this.btnPet.Location = new Point(139, 264);
            this.btnPet.Size = new Size(36, 17);
            this.btnPet.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);
            this.btnPet.MouseClick += new System.Windows.Forms.MouseEventHandler(btnPet_MouseClick);

            this.btnDragon = new ACtrlButton();
            this.btnDragon.Normal = new BitmapOrigin(Resource.Equip_character_BtDragon_normal_0);
            this.btnDragon.Pressed = new BitmapOrigin(Resource.Equip_character_BtDragon_pressed_0);
            this.btnDragon.MouseOver = new BitmapOrigin(Resource.Equip_character_BtDragon_mouseOver_0);
            this.btnDragon.Disabled = new BitmapOrigin(Resource.Equip_character_BtDragon_disabled_0);
            this.btnDragon.Location = new Point(10, 264);
            this.btnDragon.Size = new Size(43, 17);
            this.btnDragon.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);
            this.btnDragon.MouseClick += new MouseEventHandler(btnDragon_MouseClick);

            this.btnMechanic = new ACtrlButton();
            this.btnMechanic.Normal = new BitmapOrigin(Resource.Equip_character_BtMechanic_normal_0);
            this.btnMechanic.Pressed = new BitmapOrigin(Resource.Equip_character_BtMechanic_pressed_0);
            this.btnMechanic.MouseOver = new BitmapOrigin(Resource.Equip_character_BtMechanic_mouseOver_0);
            this.btnMechanic.Disabled = new BitmapOrigin(Resource.Equip_character_BtMechanic_disabled_0);
            this.btnMechanic.Location = new Point(10, 264);
            this.btnMechanic.Size = new Size(43, 17);
            this.btnMechanic.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);
            this.btnMechanic.MouseClick += new MouseEventHandler(btnMechanic_MouseClick);

            this.btnAndroid = new ACtrlButton();
            this.btnAndroid.Normal = new BitmapOrigin(Resource.Equip_character_BtAndroid_normal_0);
            this.btnAndroid.Pressed = new BitmapOrigin(Resource.Equip_character_BtAndroid_pressed_0);
            this.btnAndroid.MouseOver = new BitmapOrigin(Resource.Equip_character_BtAndroid_mouseOver_0);
            this.btnAndroid.Disabled = new BitmapOrigin(Resource.Equip_character_BtAndroid_disabled_0);
            this.btnAndroid.Location = new Point(65, 266);
            this.btnAndroid.Size = new Size(25, 12);
            this.btnAndroid.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);
            this.btnAndroid.MouseClick += new MouseEventHandler(btnAndroid_MouseClick);

            this.btnClose = new ACtrlButton();
            this.btnClose.Normal = new BitmapOrigin(Resource.BtClose3_normal_0);
            this.btnClose.Pressed = new BitmapOrigin(Resource.BtClose3_pressed_0);
            this.btnClose.MouseOver = new BitmapOrigin(Resource.BtClose3_mouseOver_0);
            this.btnClose.Disabled = new BitmapOrigin(Resource.BtClose3_disabled_0);
            this.btnClose.Location = new Point(162, 6);
            this.btnClose.Size = new Size(13, 13);
            this.btnClose.ButtonStateChanged += new EventHandler(aCtrl_RefreshCall);
            this.btnClose.MouseClick += new MouseEventHandler(btnClose_MouseClick);
        }

        public override void Refresh()
        {
            this.preRender();
            this.SetBitmap(this.Bitmap);
            this.CaptionRectangle = new Rectangle(this.baseOffset, new Size(Resource.Equip_character_backgrnd.Width, 24));
            this.Location = newLocation;
            base.Refresh();
        }

        protected override bool captionHitTest(Point point)
        {
            Rectangle rect = this.btnClose.Rectangle;
            rect.Offset(this.baseOffset);
            if (rect.Contains(point))
                return false;
            return base.captionHitTest(point);
        }

        private void preRender()
        {
            if (Bitmap != null)
                Bitmap.Dispose();

            //处理按钮可见
            setControlState();

            //计算图像大小
            Point baseOffsetnew = calcRenderBaseOffset();
            Size size = Resource.Equip_character_backgrnd.Size;
            size.Width += baseOffsetnew.X;
            if (this.PetVisible)
                size.Width += Resource.Equip_pet_backgrnd.Width;

            //处理偏移
            this.newLocation = new Point(this.Location.X + this.baseOffset.X - baseOffsetnew.X,
                this.Location.Y + this.baseOffset.Y - baseOffsetnew.Y);
            this.baseOffset = baseOffsetnew;

            //绘制图像
            Bitmap bitmap = new Bitmap(size.Width, size.Height);
            Graphics g = Graphics.FromImage(bitmap);

            renderBase(g);
            if (this.DragonVisible) renderDragon(g);
            else if (this.MechanicVisible) renderMechanic(g);
            else if (this.AndroidVisible) renderAndroid(g);
            if (this.PetVisible) renderPet(g);
            if (this.TotemVisible) renderTotem(g);

            g.Dispose();
            this.Bitmap = bitmap;
        }

        private Point calcRenderBaseOffset()
        {
            if (this.DragonVisible)
                return new Point(Resource.Equip_dragon_backgrnd.Width, 0);
            else if (this.MechanicVisible)
                return new Point(Resource.Equip_mechanic_backgrnd.Width, 0);
            else if (this.AndroidVisible)
                return new Point(Resource.Equip_Android_backgrnd.Width, 0);
            else
                return new Point(Resource.Equip_totem_backgrnd.Width, 0);
        }

        private void setControlState()
        {
            if (this.character == null)
            {
                this.btnDragon.Visible = false;
                this.btnMechanic.Visible = false;
                this.DragonVisible = false;
                this.MechanicVisible = false;
            }
            else
            {
                if (this.character.Status.Job / 100 == 22) //龙神
                {
                    this.btnDragon.Visible = true;
                }
                else
                {
                    this.btnDragon.Visible = false;
                    this.DragonVisible = false;
                }

                if (this.character.Status.Job / 100 == 35) //机械
                {
                    this.btnMechanic.Visible = true;
                }
                else
                {
                    this.btnMechanic.Visible = false;
                    this.MechanicVisible = false;
                }
            }
        }

        private void renderBase(Graphics g)
        {
            g.TranslateTransform(baseOffset.X, baseOffset.Y);
            g.DrawImage(Resource.Equip_character_backgrnd, 0, 0);
            g.DrawImage(Resource.Equip_character_backgrnd2, 6, 22);
            g.DrawImage(Resource.Equip_character_backgrnd3, 10, 27);
            g.DrawImage(Resource.Equip_character_cashPendant, 76, 93);
            g.DrawImage(Resource.Equip_character_charmPocket, 10, 93);
            if (this.character != null
                && (this.character.Status.Job / 100 == 23 || this.character.Status.Job == 2002))
            {
                g.DrawImage(Resource.Equip_character_magicArrow, 142, 126);
            }

            foreach (AControl aCtrl in this.aControls)
            {
                aCtrl.Draw(g);
            }

            if (this.character != null)
            {
                for (int i = 0; i < 30; i++)
                {
                    Gear gear = this.character.Equip.GearSlots[i];
                    if (gear != null)
                    {
                        int dx = 10 + i % 5 * 33, dy = 27 + i / 5 * 33;
                        drawGearIcon(gear, g, dx, dy);
                    }
                }
            }
            g.ResetTransform();
        }

        private void renderDragon(Graphics g)
        {
            Rectangle rect = this.DragonRect;
            g.TranslateTransform(rect.X, rect.Y);
            g.DrawImage(Resource.Equip_dragon_backgrnd, 0, 0);
            g.DrawImage(Resource.Equip_dragon_backgrnd2, 6, 22);
            g.DrawImage(Resource.Equip_dragon_backgrnd3, 10, 29);

            if (this.character != null)
            {
                for (int i = 35; i < 39; i++)
                {
                    Gear gear = this.character.Equip.GearSlots[i];
                    if (gear != null)
                    {
                        int dx = 10 + (i - 35) * 33, dy = 22 + (((i - 1) % 2) + 1) * 33;
                        drawGearIcon(gear, g, dx, dy);
                    }
                }
            }
            g.ResetTransform();
        }

        private void renderMechanic(Graphics g)
        {
            Rectangle rect = this.MechanicRect;
            g.TranslateTransform(rect.X, rect.Y);
            g.DrawImage(Resource.Equip_mechanic_backgrnd, 0, 0);
            g.DrawImage(Resource.Equip_mechanic_backgrnd2, 6, 22);
            g.DrawImage(Resource.Equip_mechanic_backgrnd3, 12, 35);

            if (this.character != null)
            {
                int dx, dy;
                for (int i = 39; i < 44; i++)
                {
                    Gear gear = this.character.Equip.GearSlots[i];
                    if (gear != null)
                    {
                        switch(i)
                        {
                            case 39: dx = 1; dy = 1; break;
                            case 40: dx = 1; dy = 2; break;
                            case 41: dx = 2; dy = 2; break;
                            case 42: dx = 0; dy = 3; break;
                            case 43: dx = 1; dy = 3; break;
                            default: continue;
                        }
                        dx = 10 + dx * 33;
                        dy = 22 + dy * 33;
                        drawGearIcon(gear, g, dx, dy);
                    }
                }
            }
            g.ResetTransform();
        }

        private void renderPet(Graphics g)
        {
            Rectangle rect = this.PetRect;
            g.TranslateTransform(rect.X, rect.Y);
            g.DrawImage(Resource.Equip_pet_backgrnd, 0, 0);
            g.DrawImage(Resource.Equip_pet_backgrnd2, 6, 21);
            g.DrawImage(Resource.Equip_pet_backgrnd3, 11, 27);
            g.ResetTransform();
        }

        private void renderAndroid(Graphics g)
        {
            Rectangle rect = this.AndroidRect;
            g.TranslateTransform(rect.X, rect.Y);
            g.DrawImage(Resource.Equip_Android_backgrnd, 0, 0);
            g.DrawImage(Resource.Equip_Android_backgrnd2, 6, 24);
            g.DrawImage(Resource.Equip_Android_backgrnd3, 12, 28);
            g.ResetTransform();
        }

        private void renderTotem(Graphics g)
        {
            Rectangle rect = this.TotemRect;
            g.TranslateTransform(rect.X, rect.Y);
            g.DrawImage(Resource.Equip_totem_backgrnd, 0, 0);
            g.ResetTransform();
        }

        private void drawGearIcon(Gear gear, Graphics g, int x, int y)
        {
            if (gear == null || g == null)
                return;
            if (gear.State == GearState.disable)
                g.DrawImage(Resource.Equip_character_disabled, x, y);
            Pen pen = GearGraphics.GetGearItemBorderPen(gear.Grade);
            if (pen != null)
            {
                Point[] path = GearGraphics.GetIconBorderPath(x, y);
                g.DrawLines(pen, path);
            }
            g.DrawImage(gear.Icon.Bitmap,
                x - gear.Icon.Origin.X,
                y + 32 - gear.Icon.Origin.Y);
        }

        private IEnumerable<AControl> aControls
        {
            get
            {
                yield return btnDragon;
                yield return btnMechanic;
                yield return btnPet;
                yield return btnClose;
                yield return btnAndroid;
            }
        }

        private void aCtrl_RefreshCall(object sender, EventArgs e)
        {
            this.waitForRefresh = true;
        }

        private void btnPet_MouseClick(object sender, MouseEventArgs e)
        {
            this.PetVisible = !this.PetVisible;
            this.waitForRefresh = true;
        }

        private void btnDragon_MouseClick(object sender, MouseEventArgs e)
        {
            this.DragonVisible = !this.DragonVisible;
            this.waitForRefresh = true;
        }

        private void btnMechanic_MouseClick(object sender, MouseEventArgs e)
        {
            this.MechanicVisible = !this.MechanicVisible;
            this.waitForRefresh = true;
        }

        private void btnAndroid_MouseClick(object sender, MouseEventArgs e)
        {
            this.AndroidVisible = !this.AndroidVisible;
            this.waitForRefresh = true;
        }

        private void btnClose_MouseClick(object sender, MouseEventArgs e)
        {
            this.Visible = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            MouseEventArgs childArgs = new MouseEventArgs(e.Button, e.Clicks, e.X - baseOffset.X, e.Y - baseOffset.Y, e.Delta);

            foreach (AControl ctrl in this.aControls)
            {
                ctrl.OnMouseMove(childArgs);
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
            MouseEventArgs childArgs = new MouseEventArgs(e.Button, e.Clicks, e.X - baseOffset.X, e.Y - baseOffset.Y, e.Delta);

            foreach (AControl ctrl in this.aControls)
            {
                ctrl.OnMouseDown(childArgs);
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
            MouseEventArgs childArgs = new MouseEventArgs(e.Button, e.Clicks, e.X - baseOffset.X, e.Y - baseOffset.Y, e.Delta);

            foreach (AControl ctrl in this.aControls)
            {
                ctrl.OnMouseUp(childArgs);
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
            MouseEventArgs childArgs = new MouseEventArgs(e.Button, e.Clicks, e.X - baseOffset.X, e.Y - baseOffset.Y, e.Delta);

            foreach (AControl ctrl in this.aControls)
            {
                ctrl.OnMouseClick(childArgs);
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
