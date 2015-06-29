using System;
using System.Collections.Generic;
using System.Text;

namespace WzComparerR2.CharaSim
{
    public abstract class ItemBase : ICloneable
    {
        public ItemBase()
        {
        }

        private BitmapOrigin icon;
        private BitmapOrigin iconRaw;
        private int itemID;

        public int ItemID
        {
            get { return itemID; }
            set { itemID = value; }
        }

        public BitmapOrigin Icon
        {
            get { return icon; }
            set { icon = value; }
        }

        public BitmapOrigin IconRaw
        {
            get { return iconRaw; }
            set { iconRaw = value; }
        }

        public virtual ItemBaseType Type
        {
            get { return (ItemBaseType)(this.itemID / 1000000); }
        }

        public virtual object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
