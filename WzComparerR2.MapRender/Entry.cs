using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using WzComparerR2.PluginBase;
using DevComponents.DotNetBar;
using System.Threading;
using System.Windows.Forms;
using Game = Microsoft.Xna.Framework.Game;

namespace WzComparerR2.MapRender
{
    public class Entry : PluginEntry
    {
        public Entry(PluginContext context)
            : base(context)
        {

        }

        private RibbonBar bar;
        private ButtonItem btnItemMapRender;
        private FrmMapRender mapRenderGame1;

        private RibbonBar bar2;
        private ButtonItem btnItemMapRenderV2;
        private FrmMapRender2 mapRenderGame2;


        protected override void OnLoad()
        {
            this.bar = Context.AddRibbonBar("Modules", "MapRender");
            btnItemMapRender = new ButtonItem("", "MapRender");
            btnItemMapRender.Click += btnItem_Click;
            bar.Items.Add(btnItemMapRender);

            this.bar2 = Context.AddRibbonBar("Modules", "MapRender2");
            btnItemMapRenderV2 = new ButtonItem("", "MapRenderV2");
            btnItemMapRenderV2.Click += btnItem_Click;
            bar2.Items.Add(btnItemMapRenderV2);
        }

        void btnItem_Click(object sender, EventArgs e)
        {
            btnItemMapRender.Enabled = false;
            Wz_Node node = Context.SelectedNode1;
            if (node != null)
            {
                Wz_Image img = node.Value as Wz_Image;
                Wz_File wzFile = node.GetNodeWzFile();

                if (img != null && img.TryExtract())
                {
                    if (wzFile == null || wzFile.Type != Wz_Type.Map)
                    {
                        if (MessageBoxEx.Show("所选Img不属于Map.wz，是否继续？", "提示", MessageBoxButtons.OKCancel) != DialogResult.OK)
                        {
                            goto exit;
                        }
                    }

                    StringLinker sl = this.Context.DefaultStringLinker;
                    if (!sl.HasValues) //生成默认stringLinker
                    {
                        sl = new StringLinker();
                        sl.Load(PluginManager.FindWz(Wz_Type.String).GetValueEx<Wz_File>(null));
                    }

                    //开始绘制
                    Thread thread = new Thread(() =>
                    {
#if !DEBUG
                        try
                        {
#endif
                        if (sender == btnItemMapRender)
                        {
                            if (this.mapRenderGame1 != null)
                            {
                                return;
                            }
                            this.mapRenderGame1 = new FrmMapRender(img) { StringLinker = sl };
                            try
                            {
                                using (this.mapRenderGame1)
                                {
                                    this.mapRenderGame1.Run();
                                }
                            }
                            finally
                            {
                                this.mapRenderGame1 = null;
                            }
                        }
                        else
                        {
                            if (this.mapRenderGame2 != null)
                            {
                                return;
                            }
                            this.mapRenderGame2 = new FrmMapRender2(img) { StringLinker = sl };
                            try
                            {
                                using (this.mapRenderGame2)
                                {
                                    this.mapRenderGame2.Run();
                                }
                            }
                            finally
                            {
                                this.mapRenderGame2 = null;
                            }
                        }
#if !DEBUG
                        }
                        catch (Exception ex)
                        {
                            MessageBoxEx.Show(ex.ToString(), "MapRender");
                        }
#endif
                    });
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.IsBackground = true;
                    thread.Start();
                    goto exit;
                }
            }

            MessageBoxEx.Show("没有选择一个map的img", "MapRender");

            exit:
            btnItemMapRender.Enabled = true;
        }

    }
}
