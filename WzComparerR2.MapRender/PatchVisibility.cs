using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.MapRender.Patches;

namespace WzComparerR2.MapRender
{
    public class PatchVisibility
    {
        public PatchVisibility()
        {
            this.dictVisible = new Dictionary<RenderObjectType, bool>();
            this.tagsVisible = new SortedDictionary<string, bool>();
            foreach (RenderObjectType type in Enum.GetValues(typeof(RenderObjectType)))
            {
                this.dictVisible[type] = true;
            }
            this.PortalInEditMode = false;
            this.DefaultTagVisible = true;
        }

        public bool BackVisible
        {
            get { return IsVisible(RenderObjectType.Back); }
            set { this.SetVisible(RenderObjectType.Back, value); }
        }

        public bool ReactorVisible
        {
            get { return IsVisible(RenderObjectType.Reactor); }
            set { this.SetVisible(RenderObjectType.Reactor, value); }
        }

        public bool ObjVisible
        {
            get { return IsVisible(RenderObjectType.Obj); }
            set { this.SetVisible(RenderObjectType.Obj, value); }
        }

        public bool TileVisible
        {
            get { return IsVisible(RenderObjectType.Tile); }
            set { this.SetVisible(RenderObjectType.Tile, value); }
        }

        public bool NpcVisible
        {
            get { return IsVisible(RenderObjectType.Npc); }
            set { this.SetVisible(RenderObjectType.Npc, value); }
        }

        public bool MobVisible
        {
            get { return IsVisible(RenderObjectType.Mob); }
            set { this.SetVisible(RenderObjectType.Mob, value); }
        }

        public bool FootHoldVisible
        {
            get { return IsVisible(RenderObjectType.Foothold); }
            set { this.SetVisible(RenderObjectType.Foothold, value); }
        }

        public bool LadderRopeVisible
        {
            get { return IsVisible(RenderObjectType.LadderRope); }
            set { this.SetVisible(RenderObjectType.LadderRope, value); }
        }

        public bool SkyWhaleVisible { get; set; }

        public bool PortalVisible
        {
            get { return IsVisible(RenderObjectType.Portal); }
            set { this.SetVisible(RenderObjectType.Portal, value); }
        }

        public bool PortalInEditMode { get; set; }

        public bool FrontVisible
        {
            get { return IsVisible(RenderObjectType.Front); }
            set { this.SetVisible(RenderObjectType.Front, value); }
        }

        public bool NpcNameVisible
        {
            get { return IsVisible(RenderObjectType.NpcName); }
            set { this.SetVisible(RenderObjectType.NpcName, value); }
        }

        public bool MobNameVisible
        {
            get { return IsVisible(RenderObjectType.MobName); }
            set { this.SetVisible(RenderObjectType.MobName, value); }
        }

        public IReadOnlyDictionary<string, bool> TagsVisible
        {
            get { return this.tagsVisible; }
        }

        public bool DefaultTagVisible { get; set; }

        private Dictionary<RenderObjectType, bool> dictVisible;
        private SortedDictionary<string, bool> tagsVisible;

        public bool IsVisible(RenderObjectType type)
        {
            bool visible;
            dictVisible.TryGetValue(type, out visible);
            return visible;
        }

        public bool IsTagVisible(string tag)
        {
            return this.tagsVisible.TryGetValue(tag, out var isVisible) ? isVisible : this.DefaultTagVisible;
        }

        public void SetTagVisible(string tag, bool isVisible)
        {
            this.tagsVisible[tag] = isVisible;
        }

        public void ResetTagVisible()
        {
            this.tagsVisible.Clear();
        }

        public void ResetTagVisible(string[] tags)
        {
            foreach(var tag in tags)
            {
                this.tagsVisible.Remove(tag);
            }
        }

        private void SetVisible(RenderObjectType type, bool visible)
        {
            this.dictVisible[type] = visible;
        }
    }
}
