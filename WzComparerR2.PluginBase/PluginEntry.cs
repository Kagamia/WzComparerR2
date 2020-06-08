using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace WzComparerR2.PluginBase
{
    public abstract class PluginEntry
    {
        public PluginEntry(PluginContext context)
        {
            this.Context = context;
        }

        public PluginContext Context
        {
            get;
            private set;
        }

        public virtual string Name
        {
            get
            {
                var attr = GetAsmAttr<AssemblyTitleAttribute>();
                return attr != null ? attr.Title : null;
            }
        }

        public virtual string Author
        {
            get
            {
                var attr = GetAsmAttr<AssemblyCompanyAttribute>();
                return attr != null ? attr.Company : null;
            }
        }

        public virtual string Version
        {
            get
            {
                return this.GetType().Assembly.GetName().Version.ToString();
            }
        }

        public virtual string FileVersion
        {
            get
            {
                var attrInfoVersion = GetAsmAttr<AssemblyInformationalVersionAttribute>();
                if (!string.IsNullOrEmpty(attrInfoVersion?.InformationalVersion))
                {
                    return attrInfoVersion.InformationalVersion;
                }

                var attrFileVersion = GetAsmAttr<AssemblyFileVersionAttribute>();
                return attrFileVersion?.Version;
            }
        }

        private T GetAsmAttr<T>()
        {
            object[] attr = this.GetType().Assembly.GetCustomAttributes(typeof(T), true);
            if (attr != null && attr.Length > 0)
            {
                return (T)attr[0];
            }
            return default(T);
        }

        protected internal virtual void OnLoad()
        {

        }

        protected internal virtual void OnUnload()
        {

        }
    }
}
