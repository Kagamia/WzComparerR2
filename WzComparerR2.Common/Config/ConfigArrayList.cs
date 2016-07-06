using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace WzComparerR2.Config
{
    public class ConfigArrayList<T> : ConfigurationElementCollection, IEnumerable<T>
    {
        public T this[int index]
        {
            get { return ((ItemElement)base.BaseGet(index)).Value; }
            set
            {
                base.BaseRemoveAt(index);
                base.BaseAdd(index, new ItemElement() { Value = value });
            }
        }

        public void Add(T item)
        {
            base.BaseAdd(new ItemElement() { Value = item }, false);
        }

        public void Insert(int index, T item)
        {
            base.BaseAdd(index, new ItemElement() { Value = item });
        }

        public void RemoveAt(int index)
        {
            base.BaseRemoveAt(index);
        }

        public bool Remove(T item)
        {
            int index = this.IndexOf(item);
            if (index > -1)
            {
                base.BaseRemoveAt(index);
                return true;
            }
            return false;
        }

        public int IndexOf(T item)
        {
            var elem = this.OfType<ItemElement>().FirstOrDefault(e => object.Equals(e.Value, item));
            int index = elem == null ? -1 : base.BaseIndexOf(elem);
            return index;
        }

        public void Clear()
        {
            base.BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ItemElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return element;
        }

        public new IEnumerator<T> GetEnumerator()
        {
            return this.OfType<ItemElement>().Select(elem => elem.Value).GetEnumerator();
        }

        protected override string ElementName
        {
            get { return "item"; }
        }

        public class ItemElement : ConfigurationElement
        {
            public ItemElement()
            {
                this.Hash = Guid.NewGuid();
            }

            [ConfigurationProperty("hash", IsRequired = true)]
            public Guid Hash
            {
                get { return (Guid)this["hash"]; }
                set { this["hash"] = value; }
            }

            [ConfigurationProperty("value", IsRequired = true)]
            public T Value
            {
                get { return (T)this["value"]; }
                set { this["value"] = value; }
            }
        }
    }
}
