using System;
using System.Collections.Generic;
using System.Text;
using WzComparerR2.WzLib;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Collections.ObjectModel;
using DevComponents.DotNetBar;
using System.Windows.Forms;
using System.Linq;

namespace WzComparerR2.PluginBase
{
    public class PluginManager
    {
        /// <summary>
        /// 当执行FindWz函数时发生，用来寻找对应的Wz_File。
        /// </summary>
        internal static event FindWzEventHandler WzFileFinding;

        /// <summary>
        /// 为CharaSim组件提供全局的搜索Wz_File的方法。
        /// </summary>
        /// <param Name="Type">要搜索wz文件的Wz_Type。</param>
        /// <returns></returns>
        public static Wz_Node FindWz(Wz_Type type)
        {
            return FindWz(type, null);
        }

        public static Wz_Node FindWz(Wz_Type type, Wz_File sourceWzFile)
        {
            FindWzEventArgs e = new FindWzEventArgs(type) { WzFile = sourceWzFile };
            if (WzFileFinding != null)
            {
                WzFileFinding(null, e);
                if (e.WzNode != null)
                {
                    return e.WzNode;
                }
                if (e.WzFile != null)
                {
                    return e.WzFile.Node;
                }
            }
            return null;
        }

        /// <summary>
        /// 通过wz完整路径查找对应的Wz_Node，若没有找到则返回null。
        /// </summary>
        /// <param name="fullPath">要查找节点的完整路径，可用'/'或者'\'作分隔符，如"Mob/8144006.img/die1/6"。</param>
        /// <returns></returns>
        public static Wz_Node FindWz(string fullPath)
        {
            return FindWz(fullPath, null);
        }

        public static Wz_Node FindWz(string fullPath, Wz_File sourceWzFile)
        {
            FindWzEventArgs e = new FindWzEventArgs() { FullPath = fullPath, WzFile = sourceWzFile };
            if (WzFileFinding != null)
            {
                WzFileFinding(null, e);
                if (e.WzNode != null)
                {
                    return e.WzNode;
                }
                if (e.WzFile != null)
                {
                    return e.WzFile.Node;
                }
            }
            return null;
        }

        public static void LogError(string logger, string format, params object[] args)
        {
            LogError(logger, null, format, args);
        }

        public static void LogError(string logger, Exception ex, string format, params object[] args)
        {
            string logText = string.Format("[{0:yyyy-MM-dd HH:mm:ss}][Error][{1}] {2}{3}",
                DateTime.Now,
                logger,
                args == null ? format : string.Format(format, args),
                ex?.ToString());

            string logFile = Path.Combine(Path.GetDirectoryName(MainExecutorPath), "error.log");

            try
            {
                File.AppendAllLines(logFile, new[] { logText });
            }
            catch
            {
            }
        }

        internal static string MainExecutorPath
        {
            get
            {
                var asmArray = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var asm in asmArray)
                {
                    string asmName = asm.GetName().Name;
                    if (string.Equals(asmName, "WzComparerR2", StringComparison.OrdinalIgnoreCase))
                    {
                        return asm.Location;
                    }
                }
                return "";
            }
        }

        internal static string[] GetPluginFiles()
        {
            List<string> fileList = new List<string>();
            string baseDir = Path.GetDirectoryName(MainExecutorPath);
            string pluginDir = Path.Combine(baseDir, "Plugin");
            if (Directory.Exists(pluginDir))
            {
                foreach (string file in Directory.GetFiles(pluginDir, "WzComparerR2.*.dll", SearchOption.AllDirectories))
                {
                    fileList.Add(file);
                }
            }
            else
            {
                Directory.CreateDirectory(pluginDir);
            }
            return fileList.ToArray();
        }

        internal static IReadOnlyCollection<PluginInfo> LoadPlugin(Assembly assembly, PluginContext context)
        {
            var baseType = typeof(PluginEntry);
            return assembly.GetExportedTypes().Where(type => type.IsSubclassOf(baseType) && !type.IsAbstract)
                .Select(type =>
                {
                    try
                    {
                        var entry = Activator.CreateInstance(type, context) as PluginEntry;
                        var plugin = new PluginInfo()
                        {
                            Assembly = assembly,
                            FileName = assembly.Location,
                            Instance = entry,
                        };
                        loadedPlugins.Add(plugin);
                        return plugin;
                    }
                    catch
                    {
                        return null;
                    }
                }).OfType<PluginInfo>()
                .ToList();
        }

        internal static void PluginOnLoad()
        {
            foreach (var plugin in loadedPlugins)
            {
                try
                {
                    plugin.Instance.OnLoad();
                }
                catch (Exception ex)
                {
                    MessageBoxEx.Show("插件初始化失败。\r\n" + ex.Message, plugin.Instance.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        internal static void PluginOnUnLoad()
        {
            foreach (var plugin in loadedPlugins)
            {
                try
                {
                    plugin.Instance.OnUnload();
                }
                catch
                {
                }
            }
        }

        static List<PluginInfo> loadedPlugins = new List<PluginInfo>();
        static ReadOnlyCollection<PluginInfo> readonlyLoadedPlugins = new ReadOnlyCollection<PluginInfo>(loadedPlugins);
        internal static ReadOnlyCollection<PluginInfo> LoadedPlugins
        {
            get
            {
                return readonlyLoadedPlugins;
            }
        }
    }
}
