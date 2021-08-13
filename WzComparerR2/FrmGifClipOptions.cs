using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DevComponents.Editors;
using WzComparerR2.Controls;

namespace WzComparerR2
{
    public partial class FrmGifClipOptions : DevComponents.DotNetBar.Office2007Form
    {
        public FrmGifClipOptions()
        {
            InitializeComponent();
        }

        public AnimationClipOptions ClipOptions
        {
            get { return this.CollectControlValues(false); }
            set { this.ApplyControlValues(value, false); }
        }

        public AnimationClipOptions ClipOptionsNew
        {
            get { return this.CollectControlValues(true); }
            set { this.ApplyControlValues(value, true); }
        }

        private bool isUpdating;

        private IntegerInput[] GetInputGroup(bool isNew)
        {
            return isNew
               ? new[] { this.txtStartTimeNew, this.txtStopTimeNew, this.txtClipLeftNew, this.txtClipTopNew, this.txtClipRightNew, this.txtClipBottomNew, this.txtWidthNew, this.txtHeightNew, this.txtScaleNew }
               : new[] { this.txtStartTime, this.txtStopTime, this.txtClipLeft, this.txtClipTop, this.txtClipRight, this.txtClipBottom, this.txtWidth, this.txtHeight, this.txtScale };
        }

        private void ApplyControlValues(AnimationClipOptions value, bool isNew)
        {
            var controls = GetInputGroup(isNew);

            if (value != null)
            {
                controls[0].ValueObject = value.StartTime;
                controls[1].ValueObject = value.StopTime;
                controls[2].ValueObject = value.Left;
                controls[3].ValueObject = value.Top;
                controls[4].ValueObject = value.Right;
                controls[5].ValueObject = value.Bottom;
                controls[6].ValueObject = value.OutputWidth;
                controls[7].ValueObject = value.OutputHeight;
            }
            else
            {
                foreach (var txt in controls)
                {
                    txt.ValueObject = null;
                }
            }
        }

        private AnimationClipOptions CollectControlValues(bool isNew)
        {
            var controls = GetInputGroup(isNew);

            return new AnimationClipOptions()
            {
                StartTime = controls[0].ValueObject as int?,
                StopTime = controls[1].ValueObject as int?,
                Left = controls[2].ValueObject as int?,
                Top = controls[3].ValueObject as int?,
                Right = controls[4].ValueObject as int?,
                Bottom = controls[5].ValueObject as int?,
                OutputWidth = controls[6].ValueObject as int?,
                OutputHeight = controls[7].ValueObject as int?,
            };
        }

        private void LockEvent(Action action)
        {
            if (this.isUpdating)
            {
                return;
            }
            this.isUpdating = true;
            try
            {
                action?.Invoke();
            }
            finally
            {
                this.isUpdating = false;
            }
        }

        private void txtTime_ValueObjectChanged(object sender, EventArgs e)
        {
            LockEvent(()=>this.onUpdateDuration(false));
        }

        private void txtTimeNew_ValueObjectChanged(object sender, EventArgs e)
        {
            LockEvent(() => this.onUpdateDuration(true));
        }

        private void txtBound_ValueObjectChanged(object sender, EventArgs e)
        {
            LockEvent(() => this.onUpdateSizeAndScale(false, isBoundChanging: true));
        }

        private void txtBoundNew_ValueObjectChanged(object sender, EventArgs e)
        {
            LockEvent(() => this.onUpdateSizeAndScale(true, isBoundChanging: true));
        }

        private void txtSize_ValueObjectChanged(object sender, EventArgs e)
        {
            LockEvent(() => this.onUpdateSizeAndScale(false, isOutSizeChanging: true));
        }

        private void txtSizeNew_ValueObjectChanged(object sender, EventArgs e)
        {
            LockEvent(() => this.onUpdateSizeAndScale(true, isOutSizeChanging: true));
        }

        private void txtScaleNew_ValueObjectChanged(object sender, EventArgs e)
        {
            LockEvent(() => this.onUpdateSizeAndScale(true, isScaleChanging: true));
        }

        private void onUpdateDuration(bool isNew)
        {
            var controls = GetInputGroup(isNew);
            var lbl = isNew ? this.lblDurationNew : this.lblDuration;
            int? duration = (controls[1].ValueObject as int?) - (controls[0].ValueObject as int?);
            lbl.Text = $"{duration?.ToString() ?? "-"} ms";
        }

        private void onUpdateSizeAndScale(bool isNew, bool isBoundChanging = false, bool isOutSizeChanging = false, bool isScaleChanging = false)
        {
            var controls = GetInputGroup(isNew);

            int? width = (controls[4].ValueObject as int?) - (controls[2].ValueObject as int?);
            int? height = (controls[5].ValueObject as int?) - (controls[3].ValueObject as int?);
            int? outWidth = (controls[6].ValueObject as int?);
            int? outHeight = (controls[7].ValueObject as int?);
            var lblSize = isNew ? this.lblClipSizeNew : this.lblClipSize;
            var txtScale = isNew ? this.txtScaleNew : this.txtScale;

            if (isBoundChanging)
            {
                lblSize.Text = $"{width?.ToString() ?? "?"}x{height?.ToString() ?? "?"}";
                isOutSizeChanging = true;
            }

            if (isOutSizeChanging)
            {
                if (new[] { width, height, outWidth, outHeight }.All(v => v != null))
                {
                    var scaleX = 1.0 * outWidth / width;
                    var scaleY = 1.0 * outHeight / height;
                    txtScale.ValueObject = (int)Math.Round(Math.Min(scaleX.Value, scaleY.Value) * 100);
                }
                else
                {
                    txtScale.ValueObject = null;
                }
            }

            if (isScaleChanging)
            {
                if (new[] { width, height, txtScale.ValueObject }.All(v => v != null))
                {
                    controls[6].ValueObject = (int)Math.Round(0.01 * width.Value * (int)txtScale.ValueObject);
                    controls[7].ValueObject = (int)Math.Round(0.01 * height.Value * (int)txtScale.ValueObject);
                }
            }
        }
    }
}
