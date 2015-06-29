using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.CharaSim
{
    public class SetItemIDList
    {
        public SetItemIDList()
        {
            this.Parts = new List<KeyValuePair<int, SetItemIDPart>>();
        }

        public List<KeyValuePair<int, SetItemIDPart>> Parts {get;private set;}

        public void Add(int partID, SetItemIDPart part)
        {
            this.Parts.Add(new KeyValuePair<int, SetItemIDPart>(partID, part));
        }

        public void Remove(int partID)
        {
            this.Parts.RemoveAll(kv => kv.Key == partID);
        }

        /// <summary>
        /// 获取或设置装备是否有效。
        /// </summary>
        /// <param Name="ItemID">装备ID。</param>
        /// <returns></returns>
        public bool this[int itemID]
        {
            get
            {
                foreach (var kv in Parts)
                {
                    bool enabled;
                    kv.Value.ItemIDs.TryGetValue(itemID, out enabled);
                    if (enabled)
                        return true;
                }
                return false;
            }
            set
            {
                foreach (var kv in Parts)
                {
                    bool enabled;
                    if (kv.Value.ItemIDs.TryGetValue(itemID, out enabled) && (enabled ^ value))
                    {
                        kv.Value.ItemIDs[itemID] = value;
                    }
                }
            }
        }
    }
}
