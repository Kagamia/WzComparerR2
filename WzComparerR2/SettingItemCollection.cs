using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;

namespace WzComparerR2
{
    public class SettingItemCollection : KeyedCollection<string, SettingItem>
    {
        public SettingItemCollection()
            : base()
        {
            
        }

        protected override string GetKeyForItem(SettingItem item)
        {
            if (item != null)
                return item.Key;
            return null;
        }

        public virtual bool TryGetValue(string key, out SettingItem value)
        {
            if (this.Dictionary != null)
            {
                return Dictionary.TryGetValue(key, out value);
            }
            else if (this.Items != null)
            {
                foreach (SettingItem item in Items)
                {
                    if (GetKeyForItem(item) == key)
                    {
                        value = item;
                        return true;
                    }
                }
            }
            value = null;
            return false;
        }
    }
}
