using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.MapRender.Patches2
{
    public class ItemEvent
    {
        public ItemEvent(string index, string collision, string animation, string slotName, string target, List<string> actionKeys)
        {
            Index = index;
            Collision = collision;
            Animation = animation;
            SlotName = slotName;
            Target = target;
            ActionKeys = actionKeys;
        }

        public string Index { get; set; }
        public string Collision { get; set; }
        public string Animation { get; set; }
        public string SlotName { get; set; }
        public string Target { get; set; }
        public List<string> ActionKeys { get; set; }
    }
}
