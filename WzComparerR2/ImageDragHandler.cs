using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace WzComparerR2
{
    public class ImageDragHandler
    {
        public ImageDragHandler(PictureBox owner)
        {
            this.OwnerControl = owner;
            this.dragBox = Rectangle.Empty;
        }

        public PictureBox OwnerControl { get; private set; }

        private Rectangle dragBox;

        public void AttachEvents()
        {
            this.OwnerControl.MouseDown += OwnerControl_MouseDown;
            this.OwnerControl.MouseMove += OwnerControl_MouseMove;
            this.OwnerControl.MouseUp += OwnerControl_MouseUp;
        }

        void OwnerControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && OwnerControl.Image != null)
            {
                this.dragBox = new Rectangle(e.Location, SystemInformation.DragSize);
            }
        }

        void OwnerControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && OwnerControl.Image != null
                && this.dragBox != Rectangle.Empty && !this.dragBox.Contains(e.Location))
            {
                string fileName = Convert.ToString(OwnerControl.Tag);
                ImageDataObject dataObj = new ImageDataObject(OwnerControl.Image, fileName);
                var dragEff = this.OwnerControl.DoDragDrop(dataObj, DragDropEffects.Copy);
            }
        }

        void OwnerControl_MouseUp(object sender, MouseEventArgs e)
        {
            this.dragBox = Rectangle.Empty;
        }

        private class ImageDataObject : DataObject
        {
            public ImageDataObject(Image image, string fileName)
            {
                this.Image = image;
                this.FileName = fileName;

                this.SetData(DataFormats.Bitmap, fileName);
                this.SetData(DataFormats.FileDrop, fileName);
                this.SetData(QQ_RichEdit_Format, new MemoryStream(new byte[0]));
                this.SetData(QQ_Unicode_RichEdit_Format, new MemoryStream(new byte[0]));
            }

            public Image Image { get; private set; }
            public string FileName { get; private set; }

            private static readonly string QQ_RichEdit_Format = "QQ_RichEdit_Format";
            private static readonly string QQ_Unicode_RichEdit_Format = "QQ_Unicode_RichEdit_Format";

            public override object GetData(string format, bool autoConvert)
            {
                if (format == DataFormats.Bitmap
                    || format == typeof(Bitmap).FullName)
                {
                    PrepareImageFile();
                    base.SetData(DataFormats.Bitmap, this.FileName);
                }
                else if (format == DataFormats.FileDrop
                    || format == "FileName"
                    || format == "FileNameW")
                {
                    PrepareImageFile();
                    base.SetData(DataFormats.FileDrop, new string[] { this.FileName });
                }
                else if (format == QQ_RichEdit_Format)
                {
                    PrepareImageFile();
                    byte[] buffer = Encoding.Default.GetBytes(GetQQRichFormatString());
                    this.SetData(QQ_RichEdit_Format, new MemoryStream(buffer));
                }
                else if (format == QQ_Unicode_RichEdit_Format)
                {
                    PrepareImageFile();
                    byte[] buffer = Encoding.Unicode.GetBytes(GetQQRichFormatString());
                    this.SetData(QQ_Unicode_RichEdit_Format, new MemoryStream(buffer));
                }

                return base.GetData(format, autoConvert);
            }

            private void PrepareImageFile()
            {
                string fileName = this.FileName;
                string tempDir = new DirectoryInfo(Environment.GetEnvironmentVariable("TEMP")).FullName;
                bool willSaveImage = false;
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = Path.Combine(tempDir, Path.GetRandomFileName());
                    willSaveImage = true;
                }
                else
                {
                    if (string.IsNullOrEmpty(Path.GetDirectoryName(fileName)))//没有文件夹 保存文件
                    {
                        fileName = Path.Combine(tempDir, fileName);
                        if (File.Exists(fileName))
                        {
                            string fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);
                            string ext = Path.GetExtension(fileName);
                            for (int i = 1; ; i++)
                            {
                                fileName = Path.Combine(tempDir, string.Format("{0}({1}){2}", fileNameNoExt, i, ext));
                                if (!File.Exists(fileName))
                                {
                                    break;
                                }
                            }
                        }
                        willSaveImage = true;
                    }
                }

                if (willSaveImage)
                {
                    Image.Save(fileName, Image.RawFormat);
                    this.FileName = fileName;
                }
            }

            private string GetQQRichFormatString()
            {
                return string.Format(@"<QQRichEditFormat><Info version=""1001""></Info><EditElement type=""1"" filepath=""{0}"" shortcut=""""></EditElement></QQRichEditFormat>", FileName);
            }
        }
    }
}
