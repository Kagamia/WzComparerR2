using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WzComparerR2.MapRender
{
    public class MapEvent
    {
        public MapEvent(string index, string type, string defaultAnimation, string changedAnimation, string tags)
        {
            if (!Enum.TryParse<MapEventType>(type, out var eventType))
            {
                eventType = MapEventType.Unknown;
            }
            Index = index;
            Type = eventType;
            DefaultAnimation = defaultAnimation;
            ChangedAnimation = changedAnimation;
            Tags = tags;
        }

        public string Index { get; set; }
        public MapEventType Type { get; set; }
        public string DefaultAnimation { get; set; }
        public string ChangedAnimation { get; set; }
        public string Tags { get; set; }
    }

    public enum MapEventType
    {
        Unknown = 0,
        SetAnimationOnceAndReturn = 1,
    }
}
