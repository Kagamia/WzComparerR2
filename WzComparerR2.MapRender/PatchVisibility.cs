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
            this.questVisible = new Dictionary<int, int>();
            foreach (RenderObjectType type in Enum.GetValues(typeof(RenderObjectType)))
            {
                this.dictVisible[type] = true;
            }
            this.PortalInEditMode = false;
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

        public bool EffectVisible
        {
            get { return IsVisible(RenderObjectType.Effect); }
            set { this.SetVisible(RenderObjectType.Effect, value); }
        }

        private Dictionary<RenderObjectType, bool> dictVisible;
        private Dictionary<int, int> questVisible;

        public bool IsVisible(RenderObjectType type)
        {
            bool visible;
            dictVisible.TryGetValue(type, out visible);
            return visible;
        }

        private void SetVisible(RenderObjectType type, bool visible)
        {
            this.dictVisible[type] = visible;
        }

        public bool IsVisible(int questID, int questState)
        {
            int visible;
            if (!questVisible.TryGetValue(questID, out visible))
            {
                return true;
            }
            return visible == -1 || visible == questState;
        }

        public bool IsVisibleExact(int questID, int questState)
        {
            int visible;
            if (!questVisible.TryGetValue(questID, out visible))
            {
                return false;
            }
            return visible == questState;
        }

        public void SetVisible(int questID, int questState)
        {
            this.questVisible[questID] = questState;
        }
    }
}
