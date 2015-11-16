using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Drawing;
using DevComponents.DotNetBar;
using DevComponents.AdvTree;
using WzComparerR2.WzLib;
using WzComparerR2.Common;
using WzComparerR2.PluginBase;
using Spine;

namespace WzComparerR2.MonsterCard.UI
{
    abstract class Handler
    {
        public Handler(MonsterCardForm form)
        {
            this.Form = form;
        }

        public MonsterCardForm Form { get; private set; }

        public Entry PluginEntry
        {
            get{ return this.Form.PluginEntry; }
        }

        public virtual IEnumerable<string> GetAnimateNames()
        {
            yield break;
        }

        public virtual void OnLoad(Wz_Node imgNode)
        {
        }

        public virtual void OnLoadAnimates(Wz_Node imgNode)
        {

        }

        public virtual void ShowTooltipWindow(Wz_Node imgNode)
        {
        }

        public virtual void OnShowAnimate(string aniName)
        {
        }

        public virtual void DisplayInfo(AdvTree advTreeMobInfo)
        {
        }

        public void DisplayGif(Gif gif)
        {
            HideSpineControl();
            this.Form.gifControl1.Visible = true;
            this.Form.lblMode.Text = "mode-gif";
            this.Form.gifControl1.AnimateGif = gif;
        }

        public void TryDisplaySpine(Wz_Node imgNode, string aniName, bool? useJson)
        {
            if (CanShowSpine())
            {
                this.Form.gifControl1.Visible = false;
                LoadSpineAndShow(imgNode, aniName, useJson);
            }
        }

        private bool CanShowSpine()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Any(asm =>
                string.Equals("Microsoft.Xna.Framework.dll",
                    asm.ManifestModule?.Name,
                    StringComparison.CurrentCultureIgnoreCase));
        }

        private void LoadSpineAndShow(Wz_Node imgNode, string aniName, bool? useJson)
        {
            SpineControl spineControl = this.Form.panelEx1.Controls.OfType<SpineControl>().FirstOrDefault();
            if (spineControl == null)
            {
                spineControl = new SpineControl();
                spineControl.Dock = System.Windows.Forms.DockStyle.Fill;
                this.Form.panelEx1.Controls.Add(spineControl);
                this.Form.aniArgs.RegisterEvents(spineControl);
                spineControl.AniDrawArgs = this.Form.aniArgs;
            }

            spineControl.Visible = true;
            this.Form.lblMode.Text = "mode-xna";
            spineControl.LoadSkeleton(imgNode, aniName, useJson);
        }

        private void FillSpineActions()
        {

        }

        public void HideSpineControl()
        {
            SpineControl spineControl = this.Form.panelEx1.Controls.OfType<SpineControl>().FirstOrDefault();
            if (spineControl != null)
            {
                spineControl.Visible = false;
            }
        }

        public IEnumerable<LifeAnimate> LoadAllAnimate(Wz_Node imgNode, string aniPrefix)
        {
            for (int i = 0; ; i++)
            {
                string aniName = aniPrefix + (i > 0 ? i.ToString() : "");
                var ani = LoadAnimateByName(imgNode, aniName);
                if (ani != null)
                {
                    yield return ani;
                }
                else if (i >= 1)
                {
                    yield break;
                }
            }
        }

        public LifeAnimate LoadAnimateByName(Wz_Node imgNode, string aniName)
        {
            var aniNode = imgNode.FindNodeByPath(aniName);
            if (aniNode == null)
            {
                return null;
            }
            var ani = new LifeAnimate(aniName);
            ani.AnimateGif = Gif.CreateFromNode(aniNode, PluginManager.FindWz);
            return ani;
        }

        public Node CreateNode(params string[] cells)
        {
            Node node = new Node(cells.Length > 0 ? cells[0] : null);
            for (int i = 1; i < cells.Length; i++)
            {
                node.Cells.Add(new Cell(cells[i]));
            }
            return node;
        }

        public Node CreateNodeWithValue(string propName, bool value)
        {
            return CreateNodeWithValue(propName, value, false);
        }

        public Node CreateNodeWithValue(string propName, bool value, bool defaultValue)
        {
            var node = CreateNode(propName, value ? "是(1)" : "否(0)");
            if (value == defaultValue)
            {
                node.Style = new ElementStyle(Color.Gray);
            }
            return node;
        }

        public Node CreateNodeWithValue(string propName, int value)
        {
            return CreateNode(propName, value.ToString());
        }
    }
}
