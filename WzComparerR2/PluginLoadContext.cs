#if NET6_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Runtime.Loader;

namespace WzComparerR2
{
    internal class PluginLoadContext : AssemblyLoadContext
    {
        public PluginLoadContext(string unmanagedDllFolder, string pluginPath) 
        {
            this.assemblyResolver = new AssemblyDependencyResolver(pluginPath);
            this.unmanagedDllResolver = new AssemblyDependencyResolver(unmanagedDllFolder);
        }

        private AssemblyDependencyResolver assemblyResolver;
        private AssemblyDependencyResolver unmanagedDllResolver;

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = this.assemblyResolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return this.LoadFromAssemblyPath(assemblyPath);
            }

            return base.Load(assemblyName);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath = this.unmanagedDllResolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return this.LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}
#endif
