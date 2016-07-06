using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace WzComparerR2.Config
{
    public class ConfigItemCollectionBase<T> : ConfigurationElementCollection
        where T : ConfigurationElement, new()
    {
        
        public T this[int index]
        {
            get { return (T)base.BaseGet(index); }
            set
            {
                base.BaseRemoveAt(index);
                base.BaseAdd(index, value);
            }
        }

        public void Add(T item)
        {
            base.BaseAdd(item, false);
        }

        public void Insert(int index, T item)
        {
            base.BaseAdd(index, item);
        }

        public void RemoveAt(int index)
        {
            base.BaseRemoveAt(index);
        }

        public void Remove(T item)
        {
            int index = base.BaseIndexOf(item);
            if (index > -1)
            {
                base.BaseRemoveAt(index);
            }
        }

        public void Clear()
        {
            base.BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new T();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return element;
        }
    }
}
