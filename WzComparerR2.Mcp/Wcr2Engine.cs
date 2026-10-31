using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using DevComponents.AdvTree;
using DevComponents.DotNetBar;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WzComparerR2.CharaSim;
using WzComparerR2.CharaSimControl;
using WzComparerR2.Common;
using WzComparerR2.Comparer;
using WzComparerR2.Config;
using WzComparerR2.Encoders;
using WzComparerR2.LuaConsole;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;

namespace WzComparerR2.Mcp
{
    public class Wcr2Engine
    {
        private readonly PluginContext context;
        private readonly object syncLock = new object();
        private readonly List<Wz_Structure> headlessOpenedWz = new List<Wz_Structure>();
        private readonly StringLinker headlessStringLinker = new StringLinker();
        private AfrmTooltip headlessTooltip;
        private bool findWzHooked;

        public Wcr2Engine(PluginContext context)
        {
            this.context = context;
            if (this.context == null)
            {
                HookHeadlessFindWz();
            }
        }

        public bool IsGuiMode
        {
            get { return this.context != null && this.context.MainForm != null; }
        }

        private void HookHeadlessFindWz()
        {
            if (this.findWzHooked)
                return;
            try
            {
                EventInfo ev = typeof(PluginManager).GetEvent("WzFileFinding", BindingFlags.Static | BindingFlags.NonPublic);
                if (ev != null)
                {
                    MethodInfo addMethod = ev.GetAddMethod(true);
                    if (addMethod != null)
                    {
                        addMethod.Invoke(null, new object[] { new FindWzEventHandler(HeadlessWzFileFinding) });
                        this.findWzHooked = true;
                    }
                }
            }
            catch
            {
            }
        }

        private IList<Wz_Structure> GetOpenedWz()
        {
            if (this.IsGuiMode)
            {
                FieldInfo f = this.context.MainForm.GetType().GetField("openedWz", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (f != null)
                {
                    IList<Wz_Structure> list = f.GetValue(this.context.MainForm) as IList<Wz_Structure>;
                    if (list != null)
                        return list;
                }
            }
            return this.headlessOpenedWz;
        }

        private StringLinker GetStringLinker()
        {
            if (this.IsGuiMode && this.context.DefaultStringLinker != null)
            {
                return this.context.DefaultStringLinker;
            }
            return this.headlessStringLinker;
        }

        private AfrmTooltip GetTooltipWindow()
        {
            if (this.IsGuiMode && this.context.DefaultTooltipWindow is AfrmTooltip)
            {
                return (AfrmTooltip)this.context.DefaultTooltipWindow;
            }
            if (this.headlessTooltip == null)
            {
                this.headlessTooltip = new AfrmTooltip();
                this.headlessTooltip.Visible = false;
                this.headlessTooltip.StringLinker = this.headlessStringLinker;
                this.headlessTooltip.ShowID = true;
                this.headlessTooltip.ShowMenu = false;
                SetPropIfExists(this.headlessTooltip, "Enable22AniStyle", true);
                if (this.headlessTooltip.SkillRender != null)
                {
                    this.headlessTooltip.SkillRender.ShowProperties = true;
                    this.headlessTooltip.SkillRender.ShowObjectID = true;
                    this.headlessTooltip.SkillRender.ShowDelay = true;
                    this.headlessTooltip.SkillRender.DisplayCooltimeMSAsSec = true;
                    this.headlessTooltip.SkillRender.DisplayPermyriadAsPercent = true;
                    this.headlessTooltip.SkillRender.IgnoreEvalError = true;
                    SetPropIfExists(this.headlessTooltip.SkillRender, "Enable22AniStyle", true);
                }
            }
            return this.headlessTooltip;
        }

        private void HeadlessWzFileFinding(object sender, FindWzEventArgs e)
        {
            string[] fullPath = null;
            if (!string.IsNullOrEmpty(e.FullPath))
            {
                fullPath = e.FullPath.Split('/', '\\');
                Wz_Type parsedType;
                e.WzType = Enum.TryParse<Wz_Type>(fullPath[0], true, out parsedType) ? parsedType : Wz_Type.Unknown;
            }

            List<Wz_Node> preSearch = new List<Wz_Node>();
            if (e.WzType != Wz_Type.Unknown)
            {
                IEnumerable<Wz_Structure> preSearchWz = (e.WzFile != null && e.WzFile.WzStructure != null)
                    ? Enumerable.Repeat(e.WzFile.WzStructure, 1)
                    : this.GetOpenedWz();

                foreach (Wz_Structure wzs in preSearchWz)
                {
                    Wz_File baseWz = null;
                    bool find = false;
                    foreach (Wz_File wz_f in wzs.wz_files)
                    {
                        if (wz_f.Type == e.WzType)
                        {
                            PropertyInfo hasChildProp = e.GetType().GetProperty("HasChildNodes");
                            bool requireChildren = false;
                            if (hasChildProp != null)
                            {
                                object val = hasChildProp.GetValue(e, null);
                                if (val is bool && (bool)val)
                                {
                                    requireChildren = true;
                                }
                            }
                            if (!requireChildren || wz_f.Node.Nodes.Count > 0)
                            {
                                preSearch.Add(wz_f.Node);
                                find = true;
                            }
                        }
                        if (wz_f.Type == Wz_Type.Base)
                        {
                            baseWz = wz_f;
                        }
                    }

                    if (baseWz != null && !find)
                    {
                        string key = e.WzType.ToString();
                        foreach (Wz_Node node in baseWz.Node.Nodes)
                        {
                            if (node.Text == key && node.Nodes.Count > 0)
                            {
                                preSearch.Add(node);
                            }
                        }
                    }
                }
            }

            if (fullPath == null || fullPath.Length <= 1)
            {
                if (e.WzType != Wz_Type.Unknown && preSearch.Count > 0)
                {
                    e.WzNode = preSearch[0];
                    e.WzFile = preSearch[0].Value as Wz_File;
                }
                return;
            }

            if (preSearch.Count <= 0)
            {
                return;
            }

            foreach (Wz_Node wzFileNode in preSearch)
            {
                Wz_Node searchNode = wzFileNode;
                for (int i = 1; i < fullPath.Length && searchNode != null; i++)
                {
                    searchNode = searchNode.Nodes[fullPath[i]];
                    Wz_Image img = searchNode.GetValueEx<Wz_Image>(null);
                    if (img != null)
                    {
                        searchNode = img.TryExtract() ? img.Node : null;
                    }
                }

                if (searchNode != null)
                {
                    e.WzNode = searchNode;
                    e.WzFile = wzFileNode.Value as Wz_File;
                    return;
                }
            }

            e.WzNode = null;
        }

        public JObject Execute(string method, JObject args)
        {
            if (args == null)
                args = new JObject();

            if (this.IsGuiMode && this.context.MainForm.IsHandleCreated && this.context.MainForm.InvokeRequired)
            {
                return (JObject)this.context.MainForm.Invoke(new Func<JObject>(delegate
                {
                    return ExecuteCore(method, args);
                }));
            }

            lock (this.syncLock)
            {
                return ExecuteCore(method, args);
            }
        }

        private JObject ExecuteCore(string method, JObject args)
        {
            string normalized = (method ?? "").Trim().ToLowerInvariant();
            if (normalized.StartsWith("wcr2_"))
            {
                normalized = normalized.Substring(5);
            }

            switch (normalized)
            {
                case "status":
                    return HandleStatus(args);
                case "open_wz":
                    return HandleOpenWz(args);
                case "close_wz":
                    return HandleCloseWz(args);
                case "list_wz_types":
                    return HandleListWzTypes(args);
                case "get_node":
                    return HandleGetNode(args);
                case "search_nodes":
                    return HandleSearchNodes(args);
                case "search_strings":
                    return HandleSearchStrings(args);
                case "get_object_info":
                    return HandleGetObjectInfo(args);
                case "render_tooltip":
                    return HandleRenderTooltip(args);
                case "extract_png":
                    return HandleExtractPng(args);
                case "extract_animation":
                    return HandleExtractAnimation(args);
                case "extract_sound":
                    return HandleExtractSound(args);
                case "export_xml":
                    return HandleExportXml(args);
                case "compare_wz":
                    return HandleCompareWz(args);
                case "run_lua":
                    return HandleRunLua(args);
                case "avatar_render":
                    return HandleAvatarRender(args);
                case "export_db_csv":
                    return HandleExportDbCsv(args);
                case "gui_control":
                    return HandleGuiControl(args);
                default:
                    throw new ArgumentException("Unknown WzComparerR2 MCP method: " + method);
            }
        }

        private static string FindDefaultBaseWzPath()
        {
            string envBase = Environment.GetEnvironmentVariable("WCR2_BASE_WZ");
            if (!string.IsNullOrEmpty(envBase) && File.Exists(envBase))
                return envBase;

            string envPath = Environment.GetEnvironmentVariable("WCR2_DEFAULT_WZ_PATH");
            if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
                return envPath;

            try
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(home))
                {
                    string cfgFile = Path.Combine(home, ".wcr2-mcp.json");
                    if (File.Exists(cfgFile))
                    {
                        JObject cfg = JObject.Parse(File.ReadAllText(cfgFile, Encoding.UTF8));
                        string userWz = cfg["baseWzPath"] != null ? cfg["baseWzPath"].ToString() : (cfg["wzPath"] != null ? cfg["wzPath"].ToString() : null);
                        if (!string.IsNullOrEmpty(userWz) && File.Exists(userWz))
                            return userWz;
                    }
                }
            }
            catch
            {
            }

            string[] candidates = new string[]
            {
                @"C:\Nexon\Maple\Data\Base\Base.wz",
                @"D:\Nexon\Maple\Data\Base\Base.wz",
                @"C:\Nexon\Maple\Base.wz",
                @"D:\Nexon\Maple\Base.wz",
                @"C:\Nexon\Maple\Data.wz",
                @"D:\Nexon\Maple\Data.wz",
                @"C:\MapleStory\Data\Base\Base.wz",
                @"D:\MapleStory\Data\Base\Base.wz",
                @"C:\MapleStory\Base.wz",
                @"D:\MapleStory\Base.wz"
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(candidates[i]))
                    return candidates[i];
            }
            return null;
        }

        private void EnsureWzLoaded(JObject args)
        {
            bool autoOpen = GetBool(args, "auto_open", true);
            if (!autoOpen)
                return;

            IList<Wz_Structure> opened = GetOpenedWz();
            if (opened.Count > 0)
                return;

            string defaultWz = GetString(args, "wz_path", null) ?? FindDefaultBaseWzPath();
            if (!string.IsNullOrEmpty(defaultWz) && File.Exists(defaultWz))
            {
                JObject openArgs = new JObject();
                openArgs["path"] = defaultWz;
                openArgs["load_strings"] = false;
                HandleOpenWz(openArgs);
            }
        }

        private bool EnsureStringLinkerLoaded()
        {
            StringLinker sl = GetStringLinker();
            if (sl.HasValues)
                return true;

            IList<Wz_Structure> opened = GetOpenedWz();
            if (opened.Count == 0)
            {
                string defaultWz = FindDefaultBaseWzPath();
                if (!string.IsNullOrEmpty(defaultWz) && File.Exists(defaultWz))
                {
                    JObject openArgs = new JObject();
                    openArgs["path"] = defaultWz;
                    openArgs["load_strings"] = false;
                    HandleOpenWz(openArgs);
                    opened = GetOpenedWz();
                }
            }

            if (this.IsGuiMode)
            {
                MethodInfo m = this.context.MainForm.GetType().GetMethod("TryLoadStringWz", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (m != null)
                {
                    object r = m.Invoke(this.context.MainForm, null);
                    if (r is bool && (bool)r)
                        return true;
                }
            }

            foreach (Wz_Structure wz in opened)
            {
                Wz_File strWz = null;
                Wz_File itemWz = null;
                Wz_File etcWz = null;
                Wz_File questWz = null;
                Wz_File reactorWz = null;

                foreach (Wz_File f in wz.wz_files)
                {
                    if (f.OwnerWzFile != null)
                        continue;
                    if (f.Type == Wz_Type.String && strWz == null) strWz = f;
                    else if (f.Type == Wz_Type.Item && itemWz == null) itemWz = f;
                    else if (f.Type == Wz_Type.Etc && etcWz == null) etcWz = f;
                    else if (f.Type == Wz_Type.Quest && questWz == null) questWz = f;
                    else if (f.Type == Wz_Type.Reactor && reactorWz == null) reactorWz = f;
                }

                if (strWz != null)
                {
                    MethodInfo[] loadMethods = sl.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
                    for (int i = 0; i < loadMethods.Length; i++)
                    {
                        if (loadMethods[i].Name == "Load")
                        {
                            ParameterInfo[] ps = loadMethods[i].GetParameters();
                            object res = null;
                            if (ps.Length == 1)
                            {
                                res = loadMethods[i].Invoke(sl, new object[] { strWz });
                            }
                            else if (ps.Length == 5)
                            {
                                res = loadMethods[i].Invoke(sl, new object[] { strWz, itemWz, etcWz, questWz, reactorWz });
                            }
                            if (res is bool && (bool)res)
                                return true;
                        }
                    }
                }
            }

            return sl.HasValues;
        }

        private JObject HandleStatus(JObject args)
        {
            JObject res = new JObject();
            res["mode"] = this.IsGuiMode ? "gui" : "headless";
            res["pid"] = Process.GetCurrentProcess().Id;
            res["executablePath"] = Application.ExecutablePath;
            res["defaultBaseWzPath"] = FindDefaultBaseWzPath();

            IList<Wz_Structure> opened = GetOpenedWz();
            JArray wzArr = new JArray();
            foreach (Wz_Structure wz in opened)
            {
                JObject wzObj = new JObject();
                wzObj["rootText"] = wz.WzNode != null ? wz.WzNode.Text : null;
                wzObj["wzFileCount"] = wz.wz_files != null ? wz.wz_files.Count : 0;
                wzObj["imgCount"] = wz.img_number;
                if (wz.wz_files != null && wz.wz_files.Count > 0 && wz.wz_files[0] != null && wz.wz_files[0].Header != null)
                {
                    wzObj["fileName"] = wz.wz_files[0].Header.FileName;
                    wzObj["version"] = wz.wz_files[0].GetMergedVersion();
                    wzObj["wzType"] = wz.wz_files[0].Type.ToString();
                }
                wzArr.Add(wzObj);
            }
            res["openWzCount"] = wzArr.Count;
            res["loadedWz"] = wzArr;

            StringLinker sl = GetStringLinker();
            res["stringLinkerLoaded"] = sl != null && sl.HasValues;
            if (sl != null && sl.HasValues)
            {
                JObject counts = new JObject();
                counts["eqp"] = sl.StringEqp != null ? sl.StringEqp.Count : 0;
                counts["item"] = sl.StringItem != null ? sl.StringItem.Count : 0;
                counts["map"] = sl.StringMap != null ? sl.StringMap.Count : 0;
                counts["mob"] = sl.StringMob != null ? sl.StringMob.Count : 0;
                counts["npc"] = sl.StringNpc != null ? sl.StringNpc.Count : 0;
                counts["skill"] = sl.StringSkill != null ? sl.StringSkill.Count : 0;
                AddDictCountIfExists(sl, counts, "StringQuest", "quest");
                AddDictCountIfExists(sl, counts, "StringSetItem", "setItem");
                AddDictCountIfExists(sl, counts, "StringAchievement", "achievement");
                AddDictCountIfExists(sl, counts, "StringReactor", "reactor");
                res["stringCounts"] = counts;
            }

            if (this.IsGuiMode)
            {
                JObject gui = new JObject();
                gui["title"] = this.context.MainForm.Text;
                gui["selectedNode1"] = this.context.SelectedNode1 != null ? this.context.SelectedNode1.FullPathToFile : null;
                gui["selectedNode2"] = this.context.SelectedNode2 != null ? this.context.SelectedNode2.FullPathToFile : null;
                gui["selectedNode3"] = this.context.SelectedNode3 != null ? this.context.SelectedNode3.FullPathToFile : null;
                gui["selectedTab"] = this.context.SelectedTab != null ? this.context.SelectedTab.Text : null;
                LabelItem lblStatus = GetPrivateField<LabelItem>(this.context.MainForm, "labelItemStatus");
                if (lblStatus != null)
                {
                    gui["statusText"] = lblStatus.Text;
                }
                res["gui"] = gui;
            }

            return res;
        }

        private JObject HandleOpenWz(JObject args)
        {
            string path = GetString(args, "path", null);
            if (string.IsNullOrEmpty(path) || string.Equals(path, "auto", StringComparison.OrdinalIgnoreCase))
            {
                path = FindDefaultBaseWzPath();
            }

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                throw new FileNotFoundException("WZ file not found: " + (path ?? "(null)"));
            }

            path = Path.GetFullPath(path);
            bool loadStrings = GetBool(args, "load_strings", false);

            IList<Wz_Structure> opened = GetOpenedWz();
            foreach (Wz_Structure wzs in opened)
            {
                foreach (Wz_File wz_f in wzs.wz_files)
                {
                    if (wz_f.Header != null && string.Equals(wz_f.Header.FileName, path, StringComparison.OrdinalIgnoreCase))
                    {
                        if (loadStrings)
                            EnsureStringLinkerLoaded();

                        JObject already = new JObject();
                        already["alreadyLoaded"] = true;
                        already["path"] = path;
                        already["wzFileCount"] = wzs.wz_files.Count;
                        already["imgCount"] = wzs.img_number;
                        already["stringLinkerLoaded"] = GetStringLinker().HasValues;
                        return already;
                    }
                }
            }

            Stopwatch sw = Stopwatch.StartNew();

            if (this.IsGuiMode)
            {
                string ext = Path.GetExtension(path);
                if (string.Equals(ext, ".img", StringComparison.OrdinalIgnoreCase))
                {
                    MethodInfo mImg = this.context.MainForm.GetType().GetMethod("openImg", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (mImg != null)
                        mImg.Invoke(this.context.MainForm, new object[] { path });
                }
                else
                {
                    MethodInfo mWz = this.context.MainForm.GetType().GetMethod("openWz", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (mWz != null)
                        mWz.Invoke(this.context.MainForm, new object[] { path });
                }
            }
            else
            {
                Wz_Structure wz = new Wz_Structure();
                string ext = Path.GetExtension(path);
                string[] msExts = new string[] { ".ms", ".mn" };
                if (string.Equals(ext, ".img", StringComparison.OrdinalIgnoreCase))
                {
                    wz.LoadImg(path);
                }
                else if (msExts.Any(e => string.Equals(ext, e, StringComparison.OrdinalIgnoreCase)))
                {
                    wz.LoadMsFile(path);
                }
                else if (wz.IsKMST1125WzFormat(path))
                {
                    wz.LoadKMST1125DataWz(path);
                    if (string.Equals(Path.GetFileName(path), "Base.wz", StringComparison.OrdinalIgnoreCase))
                    {
                        string parentDir = Path.GetDirectoryName(Path.GetDirectoryName(path));
                        if (!string.IsNullOrEmpty(parentDir))
                        {
                            string packsDir = Path.Combine(parentDir, "Packs");
                            if (Directory.Exists(packsDir))
                            {
                                for (int i = 0; i < msExts.Length; i++)
                                {
                                    string[] msFiles = Directory.GetFiles(packsDir, "*" + msExts[i]);
                                    for (int j = 0; j < msFiles.Length; j++)
                                    {
                                        try
                                        {
                                            wz.LoadMsFile(msFiles[j]);
                                        }
                                        catch
                                        {
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    wz.Load(path, true);
                }

                this.headlessOpenedWz.Add(wz);
            }

            if (loadStrings)
            {
                EnsureStringLinkerLoaded();
            }

            sw.Stop();
            IList<Wz_Structure> afterOpened = GetOpenedWz();
            Wz_Structure lastWz = afterOpened.Count > 0 ? afterOpened[afterOpened.Count - 1] : null;

            JObject result = new JObject();
            result["alreadyLoaded"] = false;
            result["path"] = path;
            result["elapsedMs"] = sw.ElapsedMilliseconds;
            result["wzFileCount"] = lastWz != null ? lastWz.wz_files.Count : 0;
            result["imgCount"] = lastWz != null ? lastWz.img_number : 0;
            result["stringLinkerLoaded"] = GetStringLinker().HasValues;
            return result;
        }

        private JObject HandleCloseWz(JObject args)
        {
            string path = GetString(args, "path", "all");
            IList<Wz_Structure> opened = GetOpenedWz();
            int closedCount = 0;

            if (this.IsGuiMode && (string.IsNullOrEmpty(path) || string.Equals(path, "all", StringComparison.OrdinalIgnoreCase)))
            {
                MethodInfo mCloseAll = this.context.MainForm.GetType().GetMethod("buttonItemCloseAll_Click", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (mCloseAll != null)
                {
                    closedCount = opened.Count;
                    mCloseAll.Invoke(this.context.MainForm, new object[] { null, EventArgs.Empty });
                }
            }
            else
            {
                for (int i = opened.Count - 1; i >= 0; i--)
                {
                    Wz_Structure wz = opened[i];
                    bool match = string.IsNullOrEmpty(path) || string.Equals(path, "all", StringComparison.OrdinalIgnoreCase);
                    if (!match)
                    {
                        foreach (Wz_File f in wz.wz_files)
                        {
                            if (f.Header != null && string.Equals(f.Header.FileName, path, StringComparison.OrdinalIgnoreCase))
                            {
                                match = true;
                                break;
                            }
                        }
                    }
                    if (match)
                    {
                        wz.Clear();
                        opened.RemoveAt(i);
                        closedCount++;
                    }
                }
                if (opened.Count == 0)
                {
                    GetStringLinker().Clear();
                    CharaSimLoader.ClearAll();
                    GC.Collect();
                }
            }

            JObject res = new JObject();
            res["closedCount"] = closedCount;
            res["remainingWzCount"] = GetOpenedWz().Count;
            return res;
        }

        private JObject HandleListWzTypes(JObject args)
        {
            EnsureWzLoaded(args);
            IList<Wz_Structure> opened = GetOpenedWz();
            JArray typesArr = new JArray();

            foreach (Wz_Structure wz in opened)
            {
                if (wz.WzNode != null)
                {
                    foreach (Wz_Node topChild in wz.WzNode.Nodes)
                    {
                        JObject item = new JObject();
                        item["name"] = topChild.Text;
                        item["childCount"] = topChild.Nodes.Count;
                        Wz_File wzf = topChild.Value as Wz_File;
                        if (wzf != null && wzf.Header != null)
                        {
                            item["type"] = wzf.Type.ToString();
                            item["fileName"] = wzf.Header.FileName;
                            item["fileSize"] = wzf.Header.FileSize;
                            item["version"] = wzf.GetMergedVersion();
                            item["mergedFileCount"] = wzf.MergedWzFiles != null ? wzf.MergedWzFiles.Count() : 1;
                        }
                        else if (topChild.Value is Wz_Image)
                        {
                            item["type"] = "Wz_Image";
                        }
                        else
                        {
                            item["type"] = "Directory";
                        }
                        typesArr.Add(item);
                    }
                }
            }

            JObject res = new JObject();
            res["count"] = typesArr.Count;
            res["categories"] = typesArr;
            return res;
        }

        private Wz_Node ResolveWzNodeByPath(string path, bool resolveUol)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            string cleanPath = path.Trim('/', '\\');
            Wz_Node node = PluginManager.FindWz(cleanPath);

            if (node == null)
            {
                string[] parts = cleanPath.Split('/', '\\');
                foreach (Wz_Structure wz in GetOpenedWz())
                {
                    if (wz.WzNode == null)
                        continue;

                    Wz_Node cur = wz.WzNode;
                    int startIdx = 0;
                    if (string.Equals(cur.Text, parts[0], StringComparison.OrdinalIgnoreCase))
                    {
                        startIdx = 1;
                    }

                    for (int i = startIdx; i < parts.Length && cur != null; i++)
                    {
                        if (cur.Value is Wz_Image)
                        {
                            Wz_Image curImg = (Wz_Image)cur.Value;
                            if (curImg.TryExtract())
                                cur = curImg.Node;
                            else
                                cur = null;
                        }
                        if (cur == null)
                            break;

                        Wz_Node next = cur.Nodes[parts[i]];
                        if (next == null)
                        {
                            foreach (Wz_Node ch in cur.Nodes)
                            {
                                if (string.Equals(ch.Text, parts[i], StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(ch.Text, parts[i] + ".img", StringComparison.OrdinalIgnoreCase))
                                {
                                    next = ch;
                                    break;
                                }
                            }
                        }
                        cur = next;
                    }

                    if (cur != null)
                    {
                        node = cur;
                        break;
                    }
                }
            }

            if (node != null && node.Value is Wz_Image)
            {
                Wz_Image img = (Wz_Image)node.Value;
                if (img.TryExtract())
                {
                    node = img.Node;
                }
            }

            if (node != null && resolveUol && node.Value is Wz_Uol)
            {
                Wz_Node uolTarget = node.ResolveUol();
                if (uolTarget != null)
                    node = uolTarget;
            }

            return node;
        }

        private JObject SerializeWzNode(Wz_Node node, int depth, int maxChildren)
        {
            if (node == null)
                return null;

            if (node.Value is Wz_Image)
            {
                Wz_Image img = (Wz_Image)node.Value;
                if (img.TryExtract())
                {
                    node = img.Node;
                }
            }

            JObject obj = new JObject();
            obj["text"] = node.Text;
            obj["fullPath"] = node.FullPath;
            obj["fullPathToFile"] = node.FullPathToFile;
            obj["childCount"] = node.Nodes != null ? node.Nodes.Count : 0;

            object val = node.Value;
            obj["valueType"] = val != null ? val.GetType().Name : "Null";

            if (val is Wz_Png)
            {
                Wz_Png png = (Wz_Png)val;
                JObject pngInfo = new JObject();
                pngInfo["width"] = png.Width;
                pngInfo["height"] = png.Height;
                pngInfo["format"] = png.Format.ToString();
                pngInfo["formatCode"] = (int)png.Format;
                pngInfo["scale"] = png.Scale;
                pngInfo["pages"] = png.ActualPages;
                pngInfo["dataLength"] = png.DataLength;

                Wz_Node originNode = node.Nodes["origin"];
                if (originNode != null && originNode.Value is Wz_Vector)
                {
                    Wz_Vector orig = (Wz_Vector)originNode.Value;
                    JObject oObj = new JObject();
                    oObj["x"] = orig.X;
                    oObj["y"] = orig.Y;
                    pngInfo["origin"] = oObj;
                }
                Wz_Node zNode = node.Nodes["z"];
                if (zNode != null && zNode.Value != null)
                {
                    pngInfo["z"] = zNode.Value.ToString();
                }
                Wz_Node inlinkNode = node.Nodes["_inlink"];
                if (inlinkNode != null && inlinkNode.Value != null)
                {
                    pngInfo["_inlink"] = inlinkNode.Value.ToString();
                }
                Wz_Node outlinkNode = node.Nodes["_outlink"];
                if (outlinkNode != null && outlinkNode.Value != null)
                {
                    pngInfo["_outlink"] = outlinkNode.Value.ToString();
                }
                Wz_Node sourceNode = node.Nodes["source"];
                if (sourceNode != null && sourceNode.Value != null)
                {
                    pngInfo["source"] = sourceNode.Value.ToString();
                }
                if (png.Width <= 1 && png.Height <= 1 && (inlinkNode != null || outlinkNode != null || sourceNode != null))
                {
                    Wz_Png linkedPng = ExtractResolvedPng(node, true);
                    if (linkedPng != null)
                    {
                        pngInfo["resolvedWidth"] = linkedPng.Width;
                        pngInfo["resolvedHeight"] = linkedPng.Height;
                    }
                }
                obj["value"] = pngInfo;
            }
            else if (val is Wz_Vector)
            {
                Wz_Vector vec = (Wz_Vector)val;
                JObject vecObj = new JObject();
                vecObj["x"] = vec.X;
                vecObj["y"] = vec.Y;
                obj["value"] = vecObj;
            }
            else if (val is Wz_Uol)
            {
                Wz_Uol uol = (Wz_Uol)val;
                JObject uolObj = new JObject();
                uolObj["uol"] = uol.Uol;
                Wz_Node resolved = node.ResolveUol();
                if (resolved != null)
                {
                    uolObj["resolvedPath"] = resolved.FullPathToFile;
                }
                obj["value"] = uolObj;
            }
            else if (val is Wz_Sound)
            {
                Wz_Sound snd = (Wz_Sound)val;
                JObject sndObj = new JObject();
                sndObj["ms"] = snd.Ms;
                sndObj["frequency"] = snd.Frequency;
                sndObj["channels"] = snd.Channels;
                sndObj["soundType"] = snd.SoundType.ToString();
                sndObj["dataLength"] = snd.DataLength;
                obj["value"] = sndObj;
            }
            else if (val is Wz_Convex)
            {
                Wz_Convex cvx = (Wz_Convex)val;
                JArray pts = new JArray();
                for (int i = 0; i < cvx.Points.Length; i++)
                {
                    JObject pt = new JObject();
                    pt["x"] = cvx.Points[i].X;
                    pt["y"] = cvx.Points[i].Y;
                    pts.Add(pt);
                }
                obj["value"] = pts;
            }
            else if (val is Wz_RawData)
            {
                Wz_RawData raw = (Wz_RawData)val;
                JObject rawObj = new JObject();
                rawObj["length"] = raw.Length;
                rawObj["offset"] = raw.Offset;
                obj["value"] = rawObj;
            }
            else if (val is Wz_Video)
            {
                Wz_Video vid = (Wz_Video)val;
                JObject vidObj = new JObject();
                vidObj["length"] = vid.Length;
                vidObj["offset"] = vid.Offset;
                obj["value"] = vidObj;
            }
            else if (val is Wz_File)
            {
                Wz_File wf = (Wz_File)val;
                JObject wfObj = new JObject();
                wfObj["type"] = wf.Type.ToString();
                wfObj["fileName"] = wf.Header != null ? wf.Header.FileName : null;
                obj["value"] = wfObj;
            }
            else if (val != null)
            {
                obj["value"] = JToken.FromObject(val);
            }
            else
            {
                obj["value"] = null;
            }

            if (depth > 0 && node.Nodes != null && node.Nodes.Count > 0)
            {
                JArray children = new JArray();
                int count = 0;
                foreach (Wz_Node child in node.Nodes)
                {
                    if (count >= maxChildren)
                    {
                        obj["childrenTruncated"] = true;
                        break;
                    }
                    if (depth == 1)
                    {
                        JObject summary = new JObject();
                        summary["text"] = child.Text;
                        summary["valueType"] = child.Value != null ? child.Value.GetType().Name : "Null";
                        summary["value"] = FormatSimpleValue(child.Value);
                        summary["childCount"] = child.Nodes != null ? child.Nodes.Count : 0;
                        if (child.Value is Wz_Png && child.Nodes != null)
                        {
                            Wz_Node outNode = child.Nodes["_outlink"];
                            if (outNode != null && outNode.Value != null)
                                summary["_outlink"] = outNode.Value.ToString();
                            Wz_Node inNode = child.Nodes["_inlink"];
                            if (inNode != null && inNode.Value != null)
                                summary["_inlink"] = inNode.Value.ToString();
                        }
                        children.Add(summary);
                    }
                    else
                    {
                        children.Add(SerializeWzNode(child, depth - 1, maxChildren));
                    }
                    count++;
                }
                obj["children"] = children;
            }

            return obj;
        }

        private static JToken FormatSimpleValue(object val)
        {
            if (val == null) return null;
            if (val is Wz_Png)
            {
                Wz_Png p = (Wz_Png)val;
                return string.Format("png {0}x{1}", p.Width, p.Height);
            }
            if (val is Wz_Vector)
            {
                Wz_Vector v = (Wz_Vector)val;
                return string.Format("({0}, {1})", v.X, v.Y);
            }
            if (val is Wz_Uol)
            {
                return ((Wz_Uol)val).Uol;
            }
            if (val is Wz_Sound)
            {
                Wz_Sound s = (Wz_Sound)val;
                return string.Format("sound {0}ms ({1})", s.Ms, s.SoundType);
            }
            if (val is Wz_Image)
            {
                return "Wz_Image";
            }
            if (val is Wz_File)
            {
                return "Wz_File (" + ((Wz_File)val).Type + ")";
            }
            if (val is IEnumerable<Potential>)
            {
                JArray potArr = new JArray();
                foreach (Potential pot in (IEnumerable<Potential>)val)
                {
                    if (pot != null)
                        potArr.Add(pot.ConvertSummary());
                }
                return potArr;
            }
            if (val is short || val is int || val is long || val is float || val is double || val is bool || val is string)
            {
                return JToken.FromObject(val);
            }
            return val.ToString();
        }

        private JObject HandleGetNode(JObject args)
        {
            EnsureWzLoaded(args);
            string path = GetString(args, "path", null);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Parameter 'path' is required.");

            int depth = GetInt(args, "depth", 1);
            if (depth < 0) depth = 0;
            if (depth > 6) depth = 6;
            int maxChildren = GetInt(args, "max_children", 200);
            bool resolveUol = GetBool(args, "resolve_uol", false);

            Wz_Node node = ResolveWzNodeByPath(path, resolveUol);
            if (node == null)
            {
                JObject notFound = new JObject();
                notFound["found"] = false;
                notFound["path"] = path;
                return notFound;
            }

            JObject res = SerializeWzNode(node, depth, maxChildren);
            res["found"] = true;
            return res;
        }

        private JObject HandleSearchNodes(JObject args)
        {
            EnsureWzLoaded(args);
            string rootPath = GetString(args, "root_path", null);
            string query = GetString(args, "query", "");
            string rawScope = GetString(args, "match_scope", null);
            if (string.IsNullOrEmpty(rawScope))
            {
                string rawTarget = GetString(args, "target", null);
                if (string.Equals(rawTarget, "name", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(rawTarget, "value", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(rawTarget, "both", StringComparison.OrdinalIgnoreCase))
                {
                    rawScope = rawTarget.ToLowerInvariant();
                }
                else
                {
                    bool matchVal = GetBool(args, "match_value", true);
                    rawScope = matchVal ? "both" : "name";
                }
            }
            string matchScope = rawScope.ToLowerInvariant();
            string valueType = GetString(args, "value_type", null);
            bool exact = GetBool(args, "exact", false);
            bool isRegex = GetBool(args, "regex", false);
            bool defaultEnterImg = !string.IsNullOrEmpty(rootPath) && rootPath.IndexOf(".img", StringComparison.OrdinalIgnoreCase) >= 0;
            bool includeImages = GetBool(args, "include_images", GetBool(args, "enter_img", defaultEnterImg));
            int maxDepth = GetInt(args, "max_depth", 6);
            if (maxDepth <= 0) maxDepth = 6;
            if (maxDepth > 16) maxDepth = 16;
            int limit = GetInt(args, "limit", 100);
            if (limit <= 0) limit = 100;
            if (limit > 1000) limit = 1000;

            Regex regexObj = isRegex && !string.IsNullOrEmpty(query) ? new Regex(query, RegexOptions.IgnoreCase) : null;

            List<Wz_Node> startNodes = new List<Wz_Node>();
            if (!string.IsNullOrEmpty(rootPath))
            {
                Wz_Node rNode = ResolveWzNodeByPath(rootPath, false);
                if (rNode != null)
                    startNodes.Add(rNode);
            }
            else
            {
                foreach (Wz_Structure wz in GetOpenedWz())
                {
                    if (wz.WzNode != null)
                        startNodes.Add(wz.WzNode);
                }
            }

            JArray matches = new JArray();
            for (int i = 0; i < startNodes.Count && matches.Count < limit; i++)
            {
                SearchNodesRecursive(startNodes[i], query, matchScope, valueType, exact, regexObj, includeImages, 0, maxDepth, limit, matches);
            }

            JObject res = new JObject();
            res["count"] = matches.Count;
            res["matches"] = matches;
            return res;
        }

        private void SearchNodesRecursive(
            Wz_Node current,
            string query,
            string target,
            string valueType,
            bool exact,
            Regex regexObj,
            bool includeImages,
            int currentDepth,
            int maxDepth,
            int limit,
            JArray matches)
        {
            if (current == null || matches.Count >= limit || currentDepth > maxDepth)
                return;

            string nodeValType = current.Value != null ? current.Value.GetType().Name : "Null";
            bool typeOk = string.IsNullOrEmpty(valueType) || string.Equals(nodeValType, valueType, StringComparison.OrdinalIgnoreCase);

            if (typeOk)
            {
                bool isMatch = false;
                if (string.IsNullOrEmpty(query))
                {
                    isMatch = !string.IsNullOrEmpty(valueType);
                }
                else
                {
                    if (target == "name" || target == "both")
                    {
                        isMatch = MatchString(current.Text, query, exact, regexObj);
                    }
                    if (!isMatch && (target == "value" || target == "both") && current.Value != null && !(current.Value is Wz_Image) && !(current.Value is Wz_File))
                    {
                        JToken simple = FormatSimpleValue(current.Value);
                        string valStr = simple != null ? simple.ToString() : "";
                        isMatch = MatchString(valStr, query, exact, regexObj);
                    }
                }

                if (isMatch)
                {
                    JObject item = new JObject();
                    item["text"] = current.Text;
                    item["fullPath"] = current.FullPathToFile;
                    item["valueType"] = nodeValType;
                    item["value"] = FormatSimpleValue(current.Value);
                    matches.Add(item);
                    if (matches.Count >= limit)
                        return;
                }
            }

            if (currentDepth >= maxDepth)
                return;

            Wz_Node traverseNode = current;
            Wz_Image extractedImg = null;
            if (includeImages && current.Value is Wz_Image)
            {
                Wz_Image img = (Wz_Image)current.Value;
                if (img.TryExtract())
                {
                    traverseNode = img.Node;
                    extractedImg = img;
                }
            }

            if (traverseNode != null && traverseNode.Nodes != null)
            {
                foreach (Wz_Node child in traverseNode.Nodes)
                {
                    SearchNodesRecursive(child, query, target, valueType, exact, regexObj, includeImages, currentDepth + 1, maxDepth, limit, matches);
                    if (matches.Count >= limit)
                        break;
                }
            }

            if (extractedImg != null)
            {
                // Keep memory clean during bulk image scans
                extractedImg.Unextract();
            }
        }

        private static bool MatchString(string input, string query, bool exact, Regex regexObj)
        {
            if (input == null)
                return false;
            if (regexObj != null)
                return regexObj.IsMatch(input);
            if (exact)
                return string.Equals(input, query, StringComparison.OrdinalIgnoreCase);
            return input.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private JObject HandleSearchStrings(JObject args)
        {
            EnsureStringLinkerLoaded();
            StringLinker sl = GetStringLinker();
            if (!sl.HasValues)
            {
                throw new InvalidOperationException("StringLinker is not loaded. Make sure Base.wz or String.wz is opened.");
            }

            string query = GetString(args, "query", "");
            string category = GetString(args, "category", "all").ToLowerInvariant();
            bool searchInDesc = GetBool(args, "search_in_desc", GetBool(args, "search_desc", false));
            bool exact = GetBool(args, "exact", false);
            bool isRegex = GetBool(args, "regex", false);
            int limit = GetInt(args, "limit", 50);
            if (limit <= 0) limit = 50;
            if (limit > 500) limit = 500;

            List<KeyValuePair<string, Dictionary<int, StringResult>>> dicts = new List<KeyValuePair<string, Dictionary<int, StringResult>>>();
            if (category == "all" || category == "eqp" || category == "gear")
                dicts.Add(new KeyValuePair<string, Dictionary<int, StringResult>>("eqp", sl.StringEqp));
            if (category == "all" || category == "item")
                dicts.Add(new KeyValuePair<string, Dictionary<int, StringResult>>("item", sl.StringItem));
            if (category == "all" || category == "map")
                dicts.Add(new KeyValuePair<string, Dictionary<int, StringResult>>("map", sl.StringMap));
            if (category == "all" || category == "mob")
                dicts.Add(new KeyValuePair<string, Dictionary<int, StringResult>>("mob", sl.StringMob));
            if (category == "all" || category == "npc")
                dicts.Add(new KeyValuePair<string, Dictionary<int, StringResult>>("npc", sl.StringNpc));
            if (category == "all" || category == "skill")
                dicts.Add(new KeyValuePair<string, Dictionary<int, StringResult>>("skill", sl.StringSkill));

            AddExtraStringDictIfRequested(sl, dicts, category, "quest", "StringQuest");
            AddExtraStringDictIfRequested(sl, dicts, category, "set_item", "StringSetItem");
            AddExtraStringDictIfRequested(sl, dicts, category, "achievement", "StringAchievement");
            AddExtraStringDictIfRequested(sl, dicts, category, "reactor", "StringReactor");
            AddExtraStringDictIfRequested(sl, dicts, category, "familiar_skill", "StringFamiliarSkill");

            string[] tokens = query.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Regex re = isRegex && !string.IsNullOrEmpty(query) ? new Regex(query, RegexOptions.IgnoreCase) : null;

            JArray results = new JArray();
            for (int d = 0; d < dicts.Count && results.Count < limit; d++)
            {
                string catName = dicts[d].Key;
                Dictionary<int, StringResult> dict = dicts[d].Value;
                if (dict == null)
                    continue;

                foreach (KeyValuePair<int, StringResult> kv in dict)
                {
                    string idStr = kv.Key.ToString();
                    string name = kv.Value != null ? kv.Value.Name : null;
                    string desc = kv.Value != null ? kv.Value.Desc : null;

                    bool matched = false;
                    if (exact)
                    {
                        matched = string.Equals(idStr, query, StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(name, query, StringComparison.OrdinalIgnoreCase);
                    }
                    else if (re != null)
                    {
                        matched = re.IsMatch(idStr) ||
                                  (!string.IsNullOrEmpty(name) && re.IsMatch(name)) ||
                                  (searchInDesc && !string.IsNullOrEmpty(desc) && re.IsMatch(desc));
                    }
                    else
                    {
                        matched = true;
                        for (int t = 0; t < tokens.Length; t++)
                        {
                            string tok = tokens[t];
                            bool tokMatch = idStr.IndexOf(tok, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                            (!string.IsNullOrEmpty(name) && name.IndexOf(tok, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                            (searchInDesc && !string.IsNullOrEmpty(desc) && desc.IndexOf(tok, StringComparison.OrdinalIgnoreCase) >= 0);
                            if (!tokMatch)
                            {
                                matched = false;
                                break;
                            }
                        }
                    }

                    if (matched)
                    {
                        JObject item = new JObject();
                        item["id"] = kv.Key;
                        item["category"] = catName;
                        item["name"] = name;
                        item["desc"] = desc;
                        string strFullPath = kv.Value != null ? kv.Value.FullPath : null;
                        item["stringPath"] = strFullPath;

                        List<string> candidatePaths = DetectCandidateWzPaths(kv.Key, catName, strFullPath);
                        if (candidatePaths.Count > 0)
                        {
                            item["wzPath"] = candidatePaths[0];
                            item["candidateWzPaths"] = new JArray(candidatePaths.ToArray());
                        }

                        results.Add(item);
                        if (results.Count >= limit)
                            break;
                    }
                }
            }

            JObject res = new JObject();
            res["count"] = results.Count;
            res["results"] = results;
            return res;
        }

        private List<string> DetectCandidateWzPaths(int id, string category, string stringNodePath)
        {
            List<string> results = new List<string>();
            string idStr = id.ToString();

            if (!string.IsNullOrEmpty(stringNodePath))
            {
                string normPath = stringNodePath.Replace('/', '\\');
                string[] pathArray = normPath.Split('\\');
                string first = pathArray[0];

                if (first.StartsWith("Cash.img", StringComparison.OrdinalIgnoreCase) ||
                    first.StartsWith("Consume.img", StringComparison.OrdinalIgnoreCase) ||
                    first.StartsWith("Etc.img", StringComparison.OrdinalIgnoreCase) ||
                    first.StartsWith("Pet.img", StringComparison.OrdinalIgnoreCase))
                {
                    string subFolder = first.Substring(0, first.IndexOf(".img", StringComparison.OrdinalIgnoreCase));
                    if (string.Equals(subFolder, "Pet", StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(string.Format("Item/Pet/{0}.img", idStr.TrimStart('0')));
                    }
                    else
                    {
                        string padded8 = idStr.PadLeft(8, '0');
                        results.Add(string.Format("Item/{0}/{1}.img/{2}", subFolder, padded8.Substring(0, 4), padded8));
                    }
                }
                else if (first.StartsWith("Ins.img", StringComparison.OrdinalIgnoreCase))
                {
                    string padded8 = idStr.PadLeft(8, '0');
                    for (int len = 4; len <= 6; len++)
                    {
                        results.Add(string.Format("Item/Install/{0}.img/{1}", padded8.Substring(0, len), padded8));
                    }
                }
                else if (first.StartsWith("Eqp.img", StringComparison.OrdinalIgnoreCase) && pathArray.Length >= 3)
                {
                    string subCat = pathArray[2] == "Taming" ? "TamingMob" : pathArray[2];
                    string padded8 = idStr.PadLeft(8, '0');
                    results.Add(string.Format("Character/{0}/{1}.img", subCat, padded8));
                }
            }

            switch (category)
            {
                case "eqp":
                case "gear":
                    {
                        string padded8 = idStr.PadLeft(8, '0');
                        string[] gearDirs = new string[]
                        {
                            "Weapon", "Cap", "Coat", "Longcoat", "Pants", "Shoes", "Glove", "Cape", "Shield",
                            "Accessory", "Ring", "Android", "Dragon", "Mechanic", "PetEquip", "TamingMob",
                            "Bits", "MonsterBattle", "Totem", "SubWeapon", "ArcaneForce", "AuthenticForce", "Familiar"
                        };
                        for (int i = 0; i < gearDirs.Length; i++)
                        {
                            string candidate = string.Format("Character/{0}/{1}.img", gearDirs[i], padded8);
                            if (PluginManager.FindWz(candidate) != null)
                            {
                                if (!results.Contains(candidate))
                                    results.Insert(0, candidate);
                                break;
                            }
                        }
                        string bodyCandidate = string.Format("Character/{0}.img", padded8);
                        if (PluginManager.FindWz(bodyCandidate) != null && !results.Contains(bodyCandidate))
                        {
                            results.Insert(0, bodyCandidate);
                        }
                        break;
                    }
                case "item":
                    {
                        if (results.Count == 0)
                        {
                            string padded8 = idStr.PadLeft(8, '0');
                            int prefix = id / 1000000;
                            string folder = prefix == 1 ? "Cash" : (prefix == 2 ? "Consume" : (prefix == 3 ? "Install" : (prefix == 4 ? "Etc" : "Cash")));
                            if (id >= 5000000 && id < 5010000)
                            {
                                results.Add(string.Format("Item/Pet/{0}.img", id));
                            }
                            else
                            {
                                results.Add(string.Format("Item/{0}/{1}.img/{2}", folder, padded8.Substring(0, 4), padded8));
                                if (folder == "Install")
                                {
                                    results.Add(string.Format("Item/Install/{0}.img/{1}", padded8.Substring(0, 5), padded8));
                                    results.Add(string.Format("Item/Install/{0}.img/{1}", padded8.Substring(0, 6), padded8));
                                }
                            }
                        }
                        break;
                    }
                case "mob":
                    {
                        results.Add(string.Format("Mob/{0}.img", idStr.PadLeft(7, '0')));
                        break;
                    }
                case "npc":
                    {
                        results.Add(string.Format("Npc/{0}.img", idStr.PadLeft(7, '0')));
                        break;
                    }
                case "map":
                    {
                        string padded9 = idStr.PadLeft(9, '0');
                        results.Add(string.Format("Map/Map/Map{0}/{1}.img", padded9[0], padded9));
                        break;
                    }
                case "skill":
                    {
                        string padded7 = idStr.PadLeft(7, '0');
                        string jobPrefix = padded7.Substring(0, padded7.Length - 4);
                        string c1 = string.Format("Skill/{0}.img/skill/{1}", jobPrefix, padded7);
                        results.Add(c1);
                        if (padded7.Length >= 8)
                        {
                            string c2 = string.Format("Skill/{0}.img/skill/{1}", padded7.Substring(0, 6), padded7);
                            if (PluginManager.FindWz(c2) != null)
                                results.Insert(0, c2);
                        }
                        break;
                    }
                case "quest":
                    {
                        results.Add(string.Format("Quest/QuestData/{0}.img", id));
                        results.Add(string.Format("Quest/Check.img/{0}", id));
                        break;
                    }
                case "set_item":
                    {
                        results.Add(string.Format("Etc/SetItemInfo.img/{0}", id));
                        break;
                    }
                case "achievement":
                    {
                        results.Add(string.Format("Etc/Achievement/AchievementData/{0}.img", id));
                        break;
                    }
            }

            return results;
        }

        private Wz_Node ResolveObjectAndType(string requestedType, string idOrPath, out string resolvedType, out int resolvedId)
        {
            resolvedType = (requestedType ?? "auto").ToLowerInvariant();
            resolvedId = 0;

            int parsedId;
            bool isNumeric = int.TryParse(idOrPath.Trim(), out parsedId);
            if (isNumeric)
            {
                resolvedId = parsedId;
            }

            EnsureStringLinkerLoaded();
            StringLinker sl = GetStringLinker();

            if (!isNumeric)
            {
                Wz_Node directNode = ResolveWzNodeByPath(idOrPath, true);
                if (directNode != null)
                {
                    if (resolvedType == "auto")
                    {
                        string fp = directNode.FullPathToFile ?? "";
                        if (fp.StartsWith("Character", StringComparison.OrdinalIgnoreCase))
                            resolvedType = fp.IndexOf("Familiar", StringComparison.OrdinalIgnoreCase) >= 0 ? "familiar" : "gear";
                        else if (fp.StartsWith("Item", StringComparison.OrdinalIgnoreCase))
                            resolvedType = "item";
                        else if (fp.StartsWith("Skill", StringComparison.OrdinalIgnoreCase))
                            resolvedType = fp.IndexOf("Recipe", StringComparison.OrdinalIgnoreCase) >= 0 ? "recipe" : "skill";
                        else if (fp.StartsWith("Mob", StringComparison.OrdinalIgnoreCase))
                            resolvedType = "mob";
                        else if (fp.StartsWith("Npc", StringComparison.OrdinalIgnoreCase))
                            resolvedType = "npc";
                        else if (fp.StartsWith("Map", StringComparison.OrdinalIgnoreCase))
                            resolvedType = "map";
                        else if (fp.StartsWith("Quest", StringComparison.OrdinalIgnoreCase))
                            resolvedType = "quest";
                        else if (fp.IndexOf("SetItemInfo", StringComparison.OrdinalIgnoreCase) >= 0)
                            resolvedType = "set_item";
                        else if (fp.IndexOf("Achievement", StringComparison.OrdinalIgnoreCase) >= 0)
                            resolvedType = "achievement";
                        else if (fp.IndexOf("DamageSkin", StringComparison.OrdinalIgnoreCase) >= 0)
                            resolvedType = "damage_skin";
                    }
                    return directNode;
                }
            }

            if (isNumeric)
            {
                if (resolvedType == "auto")
                {
                    if (sl.StringEqp != null && sl.StringEqp.ContainsKey(parsedId)) resolvedType = "gear";
                    else if (sl.StringItem != null && sl.StringItem.ContainsKey(parsedId)) resolvedType = "item";
                    else if (sl.StringSkill != null && sl.StringSkill.ContainsKey(parsedId)) resolvedType = "skill";
                    else if (sl.StringMob != null && sl.StringMob.ContainsKey(parsedId)) resolvedType = "mob";
                    else if (sl.StringMap != null && sl.StringMap.ContainsKey(parsedId)) resolvedType = "map";
                    else if (sl.StringNpc != null && sl.StringNpc.ContainsKey(parsedId)) resolvedType = "npc";
                    else if (parsedId >= 1000000 && parsedId < 2000000) resolvedType = "gear";
                    else if (parsedId >= 2000000 && parsedId < 6000000) resolvedType = "item";
                    else if (parsedId >= 100000000) resolvedType = "map";
                    else resolvedType = "skill";
                }

                string strPath = null;
                if (resolvedType == "gear" && sl.StringEqp != null && sl.StringEqp.ContainsKey(parsedId))
                    strPath = sl.StringEqp[parsedId].FullPath;
                else if (resolvedType == "item" && sl.StringItem != null && sl.StringItem.ContainsKey(parsedId))
                    strPath = sl.StringItem[parsedId].FullPath;

                List<string> candidates = DetectCandidateWzPaths(parsedId, resolvedType, strPath);
                if (resolvedType == "damage_skin")
                {
                    candidates.Add(string.Format("Etc/DamageSkin.img/{0}", parsedId));
                }
                else if (resolvedType == "familiar")
                {
                    candidates.Add(string.Format("Character/Familiar/{0}.img", parsedId.ToString().PadLeft(7, '0')));
                }

                for (int i = 0; i < candidates.Count; i++)
                {
                    Wz_Node found = ResolveWzNodeByPath(candidates[i], true);
                    if (found != null)
                        return found;
                }
            }

            return null;
        }

        private object CreateCharaSimObject(string objType, Wz_Node node, int skillLevel)
        {
            if (node == null)
                return null;

            Wz_File wzf = node.GetNodeWzFile();
            switch (objType)
            {
                case "gear":
                case "eqp":
                    CharaSimLoader.LoadSetItemsIfEmpty();
                    CharaSimLoader.LoadExclusiveEquipsIfEmpty();
                    return InvokeCreateFromNode(typeof(Gear), node, wzf);
                case "item":
                    CharaSimLoader.LoadSetItemsIfEmpty();
                    return InvokeCreateFromNode(typeof(Item), node, wzf);
                case "skill":
                    {
                        Skill skill = (Skill)InvokeCreateFromNode(typeof(Skill), node, wzf);
                        if (skill != null)
                        {
                            skill.Level = skillLevel > 0 ? skillLevel : skill.MaxLevel;
                        }
                        return skill;
                    }
                case "mob":
                    return InvokeCreateFromNode(typeof(Mob), node, wzf);
                case "npc":
                    return InvokeCreateFromNode(typeof(Npc), node, wzf);
                case "map":
                    {
                        Type mapType = typeof(Gear).Assembly.GetType("WzComparerR2.CharaSim.Map");
                        if (mapType != null)
                            return InvokeCreateFromNode(mapType, node, wzf);
                        break;
                    }
                case "quest":
                    {
                        Type questType = typeof(Gear).Assembly.GetType("WzComparerR2.CharaSim.Quest");
                        if (questType != null)
                            return InvokeCreateFromNode(questType, node, wzf);
                        break;
                    }
                case "set_item":
                    {
                        CharaSimLoader.LoadSetItemsIfEmpty();
                        int setId;
                        if (int.TryParse(node.Text, out setId) && CharaSimLoader.LoadedSetItems.ContainsKey(setId))
                        {
                            return CharaSimLoader.LoadedSetItems[setId];
                        }
                        Wz_Node itemWz = PluginManager.FindWz(Wz_Type.Item);
                        Wz_Node optionNode = itemWz != null ? itemWz.FindNodeByPath("ItemOption.img", true) : null;
                        return SetItem.CreateFromNode(node, optionNode);
                    }
                case "recipe":
                    return Recipe.CreateFromNode(node);
                case "familiar":
                    return InvokeCreateFromNode(typeof(Familiar), node, wzf);
                case "achievement":
                    {
                        Type achvType = typeof(Gear).Assembly.GetType("WzComparerR2.CharaSim.Achievement");
                        if (achvType != null)
                            return InvokeCreateFromNode(achvType, node, wzf);
                        break;
                    }
                case "damage_skin":
                    {
                        Type dsType = typeof(Gear).Assembly.GetType("WzComparerR2.CharaSim.DamageSkin");
                        if (dsType != null)
                            return InvokeCreateFromNode(dsType, node, wzf);
                        break;
                    }
            }
            return null;
        }

        private static object InvokeCreateFromNode(Type targetType, Wz_Node node, Wz_File wzf)
        {
            MethodInfo[] methods = targetType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == "CreateFromNode")
                {
                    ParameterInfo[] ps = methods[i].GetParameters();
                    if (ps.Length == 1)
                    {
                        return methods[i].Invoke(null, new object[] { node });
                    }
                    if (ps.Length == 2)
                    {
                        Delegate findDel = CreateFindWzDelegate(ps[1].ParameterType);
                        return methods[i].Invoke(null, new object[] { node, findDel });
                    }
                    if (ps.Length == 3)
                    {
                        Delegate findDel = CreateFindWzDelegate(ps[1].ParameterType);
                        return methods[i].Invoke(null, new object[] { node, findDel, wzf });
                    }
                    if (ps.Length == 4)
                    {
                        Delegate findDel = CreateFindWzDelegate(ps[1].ParameterType);
                        return methods[i].Invoke(null, new object[] { node, findDel, wzf, null });
                    }
                }
            }
            return null;
        }

        private static Delegate CreateFindWzDelegate(Type delegateType)
        {
            MethodInfo invokeMethod = delegateType.GetMethod("Invoke");
            ParameterInfo[] ps = invokeMethod.GetParameters();
            Type[] paramTypes = new Type[ps.Length];
            for (int i = 0; i < ps.Length; i++)
            {
                paramTypes[i] = ps[i].ParameterType;
            }
            MethodInfo findWzMethod = typeof(PluginManager).GetMethod("FindWz", BindingFlags.Public | BindingFlags.Static, null, paramTypes, null);
            if (findWzMethod != null)
            {
                return Delegate.CreateDelegate(delegateType, findWzMethod);
            }
            return null;
        }

        private JObject HandleGetObjectInfo(JObject args)
        {
            EnsureWzLoaded(args);
            string reqType = GetString(args, "type", "auto");
            string idOrPath = GetString(args, "id_or_path", null) ?? GetString(args, "id", null) ?? GetString(args, "path", null);
            if (string.IsNullOrEmpty(idOrPath))
                throw new ArgumentException("Parameter 'id_or_path' is required.");

            int skillLevel = GetInt(args, "skill_level", GetInt(args, "level", 0));
            string resolvedType;
            int resolvedId;
            Wz_Node node = ResolveObjectAndType(reqType, idOrPath, out resolvedType, out resolvedId);
            if (node == null)
            {
                JObject nf = new JObject();
                nf["found"] = false;
                nf["idOrPath"] = idOrPath;
                nf["requestedType"] = reqType;
                return nf;
            }

            object obj = CreateCharaSimObject(resolvedType, node, skillLevel);
            StringLinker sl = GetStringLinker();

            JObject res = new JObject();
            res["found"] = true;
            res["objectType"] = resolvedType;
            res["wzPath"] = node.FullPathToFile;

            if (obj is Gear)
            {
                Gear g = (Gear)obj;
                res["id"] = g.ItemID;
                StringResult sr;
                if (sl.StringEqp != null && sl.StringEqp.TryGetValue(g.ItemID, out sr))
                {
                    res["name"] = sr.Name;
                    res["desc"] = sr.Desc;
                }
                res["gearType"] = g.type.ToString();
                res["grade"] = g.Grade.ToString();
                res["star"] = g.Star;
                res["hasTuc"] = g.HasTuc;
                res["cash"] = g.Cash;
                JObject propsObj = new JObject();
                foreach (KeyValuePair<GearPropType, int> kv in g.Props)
                {
                    if (kv.Value != 0)
                        propsObj[kv.Key.ToString()] = kv.Value;
                }
                res["props"] = propsObj;
            }
            else if (obj is Item)
            {
                Item it = (Item)obj;
                res["id"] = it.ItemID;
                StringResult sr;
                if (sl.StringItem != null && sl.StringItem.TryGetValue(it.ItemID, out sr))
                {
                    res["name"] = sr.Name;
                    res["desc"] = sr.Desc;
                }
                res["itemType"] = it.Type.ToString();
                res["cash"] = it.Cash;
                res["isPet"] = it.ItemID >= 5000000 && it.ItemID < 5010000;
                JObject propsObj = new JObject();
                foreach (KeyValuePair<ItemPropType, long> kv in it.Props)
                {
                    if (kv.Value != 0)
                        propsObj[kv.Key.ToString()] = kv.Value;
                }
                res["props"] = propsObj;
                JObject specsObj = new JObject();
                foreach (KeyValuePair<ItemSpecType, long> kv in it.Specs)
                {
                    if (kv.Value != 0)
                        specsObj[kv.Key.ToString()] = kv.Value;
                }
                res["specs"] = specsObj;
            }
            else if (obj is Skill)
            {
                Skill sk = (Skill)obj;
                res["id"] = sk.SkillID;
                StringResult sr;
                if (sl.StringSkill != null && sl.StringSkill.TryGetValue(sk.SkillID, out sr))
                {
                    res["name"] = sr.Name;
                    res["desc"] = sr.Desc;
                    StringResultSkill srs = sr as StringResultSkill;
                    if (srs != null)
                    {
                        res["h"] = srs["h"];
                        try
                        {
                            SkillPropertyResolver resolver = new SkillPropertyResolver(sk, sk.Level);
                            res["formattedSummary"] = SummaryParser.GetSkillSummary(sk, sk.Level, srs, resolver, SummaryParams.Default);
                        }
                        catch
                        {
                        }
                    }
                }
                res["level"] = sk.Level;
                res["maxLevel"] = sk.MaxLevel;
                res["masterLevel"] = sk.MasterLevel;
                res["invisible"] = sk.Invisible;
                res["combatOrders"] = sk.CombatOrders;
                res["vSkill"] = sk.VSkill;
                res["reqLevel"] = sk.ReqLevel;
                JObject commonObj = new JObject();
                foreach (KeyValuePair<string, string> kv in sk.Common)
                {
                    commonObj[kv.Key] = kv.Value;
                }
                res["common"] = commonObj;
                if (sk.Action != null && sk.Action.Count > 0)
                {
                    res["actions"] = new JArray(sk.Action.ToArray());
                }
            }
            else if (obj is Mob)
            {
                Mob mb = (Mob)obj;
                res["id"] = mb.ID;
                StringResult sr;
                if (sl.StringMob != null && sl.StringMob.TryGetValue(mb.ID, out sr))
                {
                    res["name"] = sr.Name;
                    res["desc"] = sr.Desc;
                }
                res["level"] = mb.Level;
                res["maxHP"] = mb.MaxHP;
                res["maxMP"] = mb.MaxMP;
                res["finalMaxHP"] = GetPropString(mb, "FinalMaxHP");
                res["speed"] = mb.Speed;
                res["paDamage"] = mb.PADamage;
                res["maDamage"] = mb.MADamage;
                res["pdRate"] = mb.PDRate;
                res["mdRate"] = mb.MDRate;
                res["exp"] = mb.Exp;
                res["boss"] = mb.Boss;
                res["undead"] = mb.Undead;
                res["firstAttack"] = mb.FirstAttack;
                res["bodyAttack"] = mb.BodyAttack;
                res["link"] = mb.Link;
                if (mb.Revive != null && mb.Revive.Count > 0)
                {
                    res["revive"] = new JArray(mb.Revive.ToArray());
                }
            }
            else if (obj is Npc)
            {
                Npc np = (Npc)obj;
                res["id"] = np.ID;
                StringResult sr;
                if (sl.StringNpc != null && sl.StringNpc.TryGetValue(np.ID, out sr))
                {
                    res["name"] = sr.Name;
                    res["func"] = sr.Desc;
                }
                res["shop"] = np.Shop;
                res["link"] = np.Link;
            }
            else if (obj is SetItem)
            {
                SetItem si = (SetItem)obj;
                res["id"] = si.SetItemID;
                res["setItemID"] = si.SetItemID;
                res["name"] = si.SetItemName;
                res["setItemName"] = si.SetItemName;
                res["completeCount"] = si.CompleteCount;
                res["expandToolTip"] = si.ExpandToolTip;
                if (si.ItemIDs != null && si.ItemIDs.Parts != null)
                {
                    JArray partsArr = new JArray();
                    foreach (KeyValuePair<int, SetItemIDPart> kv in si.ItemIDs.Parts)
                    {
                        JObject pObj = new JObject();
                        pObj["slotIndex"] = kv.Key;
                        if (!string.IsNullOrEmpty(kv.Value.RepresentName))
                            pObj["representName"] = kv.Value.RepresentName;
                        if (!string.IsNullOrEmpty(kv.Value.TypeName))
                            pObj["typeName"] = kv.Value.TypeName;
                        JArray itemList = new JArray();
                        foreach (int eqpId in kv.Value.ItemIDs.Keys)
                        {
                            StringResult eqpSr;
                            string eqpName = (sl.StringEqp != null && sl.StringEqp.TryGetValue(eqpId, out eqpSr)) ? eqpSr.Name : null;
                            JObject eqpEntry = new JObject();
                            eqpEntry["id"] = eqpId;
                            eqpEntry["name"] = eqpName;
                            itemList.Add(eqpEntry);
                        }
                        pObj["items"] = itemList;
                        partsArr.Add(pObj);
                    }
                    res["parts"] = partsArr;
                }
                if (si.Effects != null && si.Effects.Count > 0)
                {
                    JObject effectsObj = new JObject();
                    foreach (KeyValuePair<int, SetItemEffect> ekv in si.Effects)
                    {
                        JObject effProps = new JObject();
                        if (ekv.Value != null && ekv.Value.PropsV5 != null)
                        {
                            foreach (KeyValuePair<GearPropType, object> pkv in ekv.Value.PropsV5)
                            {
                                effProps[pkv.Key.ToString()] = FormatSimpleValue(pkv.Value);
                            }
                        }
                        effectsObj[ekv.Key.ToString()] = effProps;
                    }
                    res["effects"] = effectsObj;
                }
            }
            else if (obj != null)
            {
                if (resolvedType == "map")
                {
                    string idProp = GetPropString(obj, "ID");
                    int mapId;
                    if (!int.TryParse(idProp, out mapId))
                        mapId = resolvedId;
                    res["id"] = mapId;
                    StringResult mapSr;
                    if (sl.StringMap != null && sl.StringMap.TryGetValue(mapId, out mapSr))
                    {
                        res["name"] = mapSr.Name;
                        res["streetName"] = mapSr["streetName"];
                        res["mapName"] = mapSr["mapName"];
                        res["desc"] = mapSr.Desc;
                    }
                }
                foreach (PropertyInfo pi in obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (pi.GetIndexParameters().Length > 0)
                        continue;
                    Type pt = pi.PropertyType;
                    if (pt.IsPrimitive || pt == typeof(string) || pt.IsEnum || pt == typeof(int?) || pt == typeof(long?))
                    {
                        try
                        {
                            object v = pi.GetValue(obj, null);
                            if (v != null && res[pi.Name] == null)
                                res[pi.Name] = JToken.FromObject(v);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return res;
        }

        private JObject HandleRenderTooltip(JObject args)
        {
            EnsureWzLoaded(args);
            string reqType = GetString(args, "type", "auto");
            string idOrPath = GetString(args, "id_or_path", null) ?? GetString(args, "id", null) ?? GetString(args, "path", null);
            if (string.IsNullOrEmpty(idOrPath))
                throw new ArgumentException("Parameter 'id_or_path' is required.");

            int skillLevel = GetInt(args, "skill_level", GetInt(args, "level", 0));
            bool showId = GetBool(args, "show_id", true);
            bool enable22 = GetBool(args, "enable_22_ani_style", true);
            bool showInGui = GetBool(args, "show_in_gui", false);
            bool returnBase64 = GetBool(args, "return_base64", false);
            string outputPath = GetString(args, "output_path", null);

            string resolvedType;
            int resolvedId;
            Wz_Node node = ResolveObjectAndType(reqType, idOrPath, out resolvedType, out resolvedId);
            if (node == null)
            {
                throw new InvalidOperationException("Could not find WZ node for: " + idOrPath);
            }

            object obj = CreateCharaSimObject(resolvedType, node, skillLevel);
            if (obj == null)
            {
                throw new InvalidOperationException("Failed to create CharaSim object (" + resolvedType + ") from node: " + node.FullPathToFile);
            }

            EnsureStringLinkerLoaded();
            StringLinker sl = GetStringLinker();

            Bitmap bmp = null;

            if (resolvedType == "damage_skin")
            {
                Type dsRenderType = typeof(AfrmTooltip).Assembly.GetType("WzComparerR2.CharaSimControl.DamageSkinTooltipRenderer");
                if (dsRenderType != null)
                {
                    TooltipRender dsRender = (TooltipRender)Activator.CreateInstance(dsRenderType);
                    dsRender.StringLinker = sl;
                    dsRender.ShowObjectID = showId;
                    SetPropIfExists(dsRender, "DamageSkin", obj);
                    bmp = dsRender.Render();
                }
            }
            else
            {
                AfrmTooltip tooltip = GetTooltipWindow();
                tooltip.StringLinker = sl;
                tooltip.ShowID = showId;
                SetPropIfExists(tooltip, "Enable22AniStyle", enable22);
                tooltip.TargetItem = obj;
                tooltip.PreRender();
                if (tooltip.Bitmap != null)
                {
                    bmp = new Bitmap(tooltip.Bitmap);
                }
                if (showInGui && this.IsGuiMode)
                {
                    tooltip.Refresh();
                    tooltip.HideOnHover = false;
                    tooltip.Show();
                }
            }

            if (bmp == null)
            {
                throw new InvalidOperationException("Tooltip renderer returned null Bitmap for " + node.FullPathToFile);
            }

            try
            {
                if (string.IsNullOrEmpty(outputPath))
                {
                    string outDir = Path.Combine(Path.GetTempPath(), "wcr2_mcp_output", "tooltips");
                    Directory.CreateDirectory(outDir);
                    string safeName = string.Format("{0}_{1}.png", resolvedType, resolvedId != 0 ? resolvedId.ToString() : SanitizeFileName(node.Text));
                    outputPath = Path.Combine(outDir, safeName);
                }
                else
                {
                    outputPath = Path.GetFullPath(outputPath);
                    string dir = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);
                }

                bmp.Save(outputPath, ImageFormat.Png);

                JObject res = new JObject();
                res["outputPath"] = outputPath;
                res["width"] = bmp.Width;
                res["height"] = bmp.Height;
                res["objectType"] = resolvedType;
                res["wzPath"] = node.FullPathToFile;
                if (returnBase64)
                {
                    res["mimeType"] = "image/png";
                    res["base64"] = Convert.ToBase64String(File.ReadAllBytes(outputPath));
                }
                return res;
            }
            finally
            {
                bmp.Dispose();
            }
        }

        private JObject HandleExtractPng(JObject args)
        {
            EnsureWzLoaded(args);
            string path = GetString(args, "path", null);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Parameter 'path' is required.");

            string outputPath = GetString(args, "output_path", null);
            bool recursive = GetBool(args, "recursive", false);
            bool resolveLinks = GetBool(args, "resolve_links", GetBool(args, "resolve_link", true));
            bool returnBase64 = GetBool(args, "return_base64", false);
            int limit = GetInt(args, "limit", 500);

            Wz_Node node = ResolveWzNodeByPath(path, true);
            if (node == null)
                throw new InvalidOperationException("WZ node not found: " + path);

            if (!recursive && node.Value is Wz_Png)
            {
                Wz_Png png = ExtractResolvedPng(node, resolveLinks);
                if (png == null)
                    throw new InvalidOperationException("Failed to resolve Wz_Png at " + path);

                if (string.IsNullOrEmpty(outputPath))
                {
                    string outDir = Path.Combine(Path.GetTempPath(), "wcr2_mcp_output", "png");
                    Directory.CreateDirectory(outDir);
                    outputPath = Path.Combine(outDir, SanitizeFileName(node.FullPathToFile.Replace('\\', '.')) + ".png");
                }
                else
                {
                    outputPath = Path.GetFullPath(outputPath);
                    string dir = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);
                }

                using (Bitmap bmp = png.ExtractPng())
                {
                    bmp.Save(outputPath, ImageFormat.Png);
                }

                JObject singleRes = new JObject();
                singleRes["count"] = 1;
                singleRes["outputPath"] = outputPath;
                singleRes["width"] = png.Width;
                singleRes["height"] = png.Height;
                singleRes["wzPath"] = node.FullPathToFile;
                Wz_Node originNode = node.Nodes["origin"];
                if (originNode != null && originNode.Value is Wz_Vector)
                {
                    Wz_Vector v = (Wz_Vector)originNode.Value;
                    JObject orig = new JObject();
                    orig["x"] = v.X;
                    orig["y"] = v.Y;
                    singleRes["origin"] = orig;
                }
                if (returnBase64)
                {
                    singleRes["mimeType"] = "image/png";
                    singleRes["base64"] = Convert.ToBase64String(File.ReadAllBytes(outputPath));
                }
                return singleRes;
            }
            else
            {
                string outDir = outputPath;
                if (string.IsNullOrEmpty(outDir))
                {
                    outDir = Path.Combine(Path.GetTempPath(), "wcr2_mcp_output", "png", SanitizeFileName(node.Text));
                }
                outDir = Path.GetFullPath(outDir);
                Directory.CreateDirectory(outDir);

                JArray files = new JArray();
                ExtractPngRecursive(node, node.FullPathToFile, outDir, resolveLinks, limit, files);

                JObject batchRes = new JObject();
                batchRes["count"] = files.Count;
                batchRes["outputDir"] = outDir;
                batchRes["files"] = files;
                return batchRes;
            }
        }

        private void ExtractPngRecursive(Wz_Node current, string basePrefix, string outDir, bool resolveLinks, int limit, JArray files)
        {
            if (current == null || files.Count >= limit)
                return;

            if (current.Value is Wz_Png)
            {
                Wz_Png png = ExtractResolvedPng(current, resolveLinks);
                if (png != null && (png.Width > 1 || png.Height > 1))
                {
                    string rel = current.FullPathToFile;
                    if (rel.StartsWith(basePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        rel = rel.Substring(basePrefix.Length).TrimStart('\\', '/');
                    }
                    if (string.IsNullOrEmpty(rel))
                        rel = current.Text;

                    string fileName = SanitizeFileName(rel.Replace('\\', '.').Replace('/', '.')) + ".png";
                    string savePath = Path.Combine(outDir, fileName);
                    using (Bitmap bmp = png.ExtractPng())
                    {
                        bmp.Save(savePath, ImageFormat.Png);
                    }

                    JObject entry = new JObject();
                    entry["wzPath"] = current.FullPathToFile;
                    entry["outputPath"] = savePath;
                    entry["width"] = png.Width;
                    entry["height"] = png.Height;
                    Wz_Node originNode = current.Nodes["origin"];
                    if (originNode != null && originNode.Value is Wz_Vector)
                    {
                        Wz_Vector v = (Wz_Vector)originNode.Value;
                        JObject orig = new JObject();
                        orig["x"] = v.X;
                        orig["y"] = v.Y;
                        entry["origin"] = orig;
                    }
                    files.Add(entry);
                }
            }

            if (current.Nodes != null)
            {
                foreach (Wz_Node child in current.Nodes)
                {
                    ExtractPngRecursive(child, basePrefix, outDir, resolveLinks, limit, files);
                    if (files.Count >= limit)
                        break;
                }
            }
        }

        private static Wz_Png ExtractResolvedPng(Wz_Node node, bool resolveLinks)
        {
            if (node == null)
                return null;
            if (resolveLinks)
            {
                try
                {
                    Wz_Node linked = node.GetLinkedSourceNode(PluginManager.FindWz);
                    if (linked != null && linked.Value is Wz_Png)
                        return (Wz_Png)linked.Value;
                }
                catch
                {
                }
            }
            return node.Value as Wz_Png;
        }

        private JObject HandleExtractAnimation(JObject args)
        {
            EnsureWzLoaded(args);
            string path = GetString(args, "path", null);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Parameter 'path' is required.");

            string format = GetString(args, "format", "apng").ToLowerInvariant();
            string outputPath = GetString(args, "output_path", null);

            Wz_Node node = ResolveWzNodeByPath(path, true);
            if (node == null)
                throw new InvalidOperationException("WZ node not found: " + path);

            Delegate findDel = CreateFindWzDelegate(typeof(GlobalFindNodeFunction));
            MethodInfo createGifMethod = typeof(Gif).GetMethod("CreateFromNode", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(Wz_Node), typeof(GlobalFindNodeFunction) }, null);
            Gif gif = null;
            if (createGifMethod != null)
            {
                gif = createGifMethod.Invoke(null, new object[] { node, findDel }) as Gif;
            }

            if (gif == null || gif.Frames == null || gif.Frames.Count == 0)
            {
                throw new InvalidOperationException("No animation frames found at node: " + node.FullPathToFile);
            }

            try
            {
                Rectangle rect = gif.GetRect();
                string ext = format == "gif" ? ".gif" : ".apng";

                if (string.IsNullOrEmpty(outputPath))
                {
                    string outDir = Path.Combine(Path.GetTempPath(), "wcr2_mcp_output", "animations");
                    Directory.CreateDirectory(outDir);
                    outputPath = Path.Combine(outDir, SanitizeFileName(node.FullPathToFile.Replace('\\', '.')) + ext);
                }
                else
                {
                    outputPath = Path.GetFullPath(outputPath);
                    string dir = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(dir))
                        Directory.CreateDirectory(dir);
                }

                if (format == "frames")
                {
                    Directory.CreateDirectory(outputPath);
                    JArray frameFiles = new JArray();
                    for (int i = 0; i < gif.Frames.Count; i++)
                    {
                        IGifFrame frame = gif.Frames[i];
                        string fPath = Path.Combine(outputPath, string.Format("frame_{0:D3}.png", i));
                        using (Bitmap bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb))
                        {
                            using (Graphics g = Graphics.FromImage(bmp))
                            {
                                frame.Draw(g, rect);
                            }
                            bmp.Save(fPath, ImageFormat.Png);
                        }
                        JObject fInfo = new JObject();
                        fInfo["index"] = i;
                        fInfo["delay"] = frame.Delay;
                        fInfo["outputPath"] = fPath;
                        frameFiles.Add(fInfo);
                    }
                    JObject framesRes = new JObject();
                    framesRes["format"] = "frames";
                    framesRes["frameCount"] = gif.Frames.Count;
                    framesRes["width"] = rect.Width;
                    framesRes["height"] = rect.Height;
                    framesRes["outputDir"] = outputPath;
                    framesRes["frames"] = frameFiles;
                    return framesRes;
                }
                else
                {
                    Type encType = format == "gif" ? typeof(BuildInGifEncoder) : typeof(BuildInApngEncoder);
                    GifEncoder enc = null;
                    ConstructorInfo ctor3 = encType.GetConstructor(new Type[] { typeof(string), typeof(int), typeof(int) });
                    if (ctor3 != null)
                    {
                        enc = (GifEncoder)ctor3.Invoke(new object[] { outputPath, rect.Width, rect.Height });
                    }
                    else
                    {
                        enc = (GifEncoder)Activator.CreateInstance(encType);
                    }

                    using (enc)
                    {
                        MethodInfo init3 = encType.GetMethod("Init", new Type[] { typeof(string), typeof(int), typeof(int) });
                        if (init3 != null)
                        {
                            init3.Invoke(enc, new object[] { outputPath, rect.Width, rect.Height });
                        }
                        else
                        {
                            MethodInfo init0 = encType.GetMethod("Init", Type.EmptyTypes);
                            if (init0 != null)
                                init0.Invoke(enc, null);
                        }

                        for (int i = 0; i < gif.Frames.Count; i++)
                        {
                            IGifFrame frame = gif.Frames[i];
                            using (Bitmap bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb))
                            {
                                using (Graphics g = Graphics.FromImage(bmp))
                                {
                                    frame.Draw(g, rect);
                                }
                                enc.AppendFrame(bmp, Math.Max(30, frame.Delay));
                            }
                        }
                    }

                    JObject res = new JObject();
                    res["format"] = format;
                    res["frameCount"] = gif.Frames.Count;
                    res["width"] = rect.Width;
                    res["height"] = rect.Height;
                    res["outputPath"] = outputPath;
                    res["wzPath"] = node.FullPathToFile;
                    return res;
                }
            }
            finally
            {
                for (int i = 0; i < gif.Frames.Count; i++)
                {
                    GifFrame gf = gif.Frames[i] as GifFrame;
                    if (gf != null && gf.Bitmap != null)
                    {
                        gf.Bitmap.Dispose();
                    }
                }
            }
        }

        private JObject HandleExtractSound(JObject args)
        {
            EnsureWzLoaded(args);
            string path = GetString(args, "path", null);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Parameter 'path' is required.");

            string outputPath = GetString(args, "output_path", null);
            Wz_Node node = ResolveWzNodeByPath(path, true);
            if (node == null)
                throw new InvalidOperationException("WZ node not found: " + path);

            Wz_Sound sound = node.Value as Wz_Sound;
            if (sound == null)
                throw new InvalidOperationException("Node is not a Wz_Sound: " + node.FullPathToFile);

            byte[] data = sound.ExtractSound();
            if (data == null || data.Length == 0)
                throw new InvalidOperationException("Failed to extract sound bytes from: " + node.FullPathToFile);

            string ext = sound.SoundType == Wz_SoundType.Mp3 ? ".mp3" : (sound.SoundType == Wz_SoundType.Pcm ? ".wav" : ".bin");
            if (string.IsNullOrEmpty(outputPath))
            {
                string outDir = Path.Combine(Path.GetTempPath(), "wcr2_mcp_output", "sounds");
                Directory.CreateDirectory(outDir);
                outputPath = Path.Combine(outDir, SanitizeFileName(node.FullPathToFile.Replace('\\', '.')) + ext);
            }
            else
            {
                outputPath = Path.GetFullPath(outputPath);
                string dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
            }

            File.WriteAllBytes(outputPath, data);

            JObject res = new JObject();
            res["outputPath"] = outputPath;
            res["wzPath"] = node.FullPathToFile;
            res["ms"] = sound.Ms;
            res["frequency"] = sound.Frequency;
            res["channels"] = sound.Channels;
            res["soundType"] = sound.SoundType.ToString();
            res["byteLength"] = data.Length;
            return res;
        }

        private JObject HandleExportXml(JObject args)
        {
            EnsureWzLoaded(args);
            string path = GetString(args, "path", null);
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Parameter 'path' is required.");

            string outputPath = GetString(args, "output_path", null);
            int maxPreviewLen = GetInt(args, "max_preview_length", 16384);
            Wz_Node node = ResolveWzNodeByPath(path, false);
            if (node == null)
                throw new InvalidOperationException("WZ node not found: " + path);

            if (string.IsNullOrEmpty(outputPath))
            {
                string outDir = Path.Combine(Path.GetTempPath(), "wcr2_mcp_output", "xml");
                Directory.CreateDirectory(outDir);
                outputPath = Path.Combine(outDir, SanitizeFileName(node.FullPathToFile.Replace('\\', '.')) + ".xml");
            }
            else
            {
                outputPath = Path.GetFullPath(outputPath);
                string dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
            }

            XmlWriterSettings xsetting = new XmlWriterSettings();
            xsetting.CloseOutput = true;
            xsetting.Indent = true;
            xsetting.Encoding = Encoding.UTF8;
            xsetting.CheckCharacters = true;
            xsetting.NewLineChars = Environment.NewLine;

            using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            using (XmlWriter writer = XmlWriter.Create(fs, xsetting))
            {
                writer.WriteStartDocument(true);
                node.DumpAsXml(writer);
                writer.WriteEndDocument();
            }

            string fullXml = File.ReadAllText(outputPath, Encoding.UTF8);
            bool truncated = false;
            string previewXml = fullXml;
            if (maxPreviewLen > 0 && fullXml.Length > maxPreviewLen)
            {
                previewXml = fullXml.Substring(0, maxPreviewLen);
                truncated = true;
            }

            JObject res = new JObject();
            res["outputPath"] = outputPath;
            res["wzPath"] = node.FullPathToFile;
            res["xmlLength"] = fullXml.Length;
            res["truncated"] = truncated;
            res["xml"] = previewXml;
            return res;
        }

        private static string ResolveWzArchivePath(string rawPath)
        {
            if (string.IsNullOrEmpty(rawPath))
                return rawPath;
            if (File.Exists(rawPath))
                return Path.GetFullPath(rawPath);
            if (Directory.Exists(rawPath))
            {
                string[] candidates = new string[]
                {
                    Path.Combine(rawPath, "Base.wz"),
                    Path.Combine(rawPath, "Data", "Base", "Base.wz"),
                    Path.Combine(rawPath, "Base", "Base.wz"),
                    Path.Combine(rawPath, "Data.wz")
                };
                for (int i = 0; i < candidates.Length; i++)
                {
                    if (File.Exists(candidates[i]))
                        return Path.GetFullPath(candidates[i]);
                }
            }
            return rawPath;
        }

        private JObject HandleCompareWz(JObject args)
        {
            string newPath = ResolveWzArchivePath(GetString(args, "new_wz_path", null));
            string oldPath = ResolveWzArchivePath(GetString(args, "old_wz_path", null));
            if (string.IsNullOrEmpty(newPath) || !File.Exists(newPath))
                throw new FileNotFoundException("New WZ file not found: " + (newPath ?? "(null)"));
            if (string.IsNullOrEmpty(oldPath) || !File.Exists(oldPath))
                throw new FileNotFoundException("Old WZ file not found: " + (oldPath ?? "(null)"));

            string outputDir = GetString(args, "output_dir", null);
            bool outputPng = GetBool(args, "output_png", false);
            bool outputAdded = GetBool(args, "output_added", true);
            bool outputRemoved = GetBool(args, "output_removed", true);
            bool resolvePngLink = GetBool(args, "resolve_png_link", true);

            Wz_Structure wzNew = new Wz_Structure();
            Wz_Structure wzOld = new Wz_Structure();
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                if (wzNew.IsKMST1125WzFormat(newPath)) wzNew.LoadKMST1125DataWz(newPath);
                else wzNew.Load(newPath, true);

                if (wzOld.IsKMST1125WzFormat(oldPath)) wzOld.LoadKMST1125DataWz(oldPath);
                else wzOld.Load(oldPath, true);

                WzFileComparer comparer = new WzFileComparer();
                comparer.PngComparison = WzPngComparison.SizeAndDataLength;
                comparer.ResolvePngLink = resolvePngLink;

                List<CompareDifference> diffs = comparer.Compare(wzNew.WzNode, wzOld.WzNode).ToList();

                int addedCount = 0;
                int removedCount = 0;
                int changedCount = 0;
                JArray diffSamples = new JArray();

                for (int i = 0; i < diffs.Count; i++)
                {
                    CompareDifference d = diffs[i];
                    if (d.DifferenceType == DifferenceType.Append) addedCount++;
                    else if (d.DifferenceType == DifferenceType.Remove) removedCount++;
                    else if (d.DifferenceType == DifferenceType.Changed) changedCount++;

                    if (diffSamples.Count < 200)
                    {
                        JObject dObj = new JObject();
                        dObj["type"] = d.DifferenceType.ToString();
                        dObj["path"] = d.NodeNew != null ? d.NodeNew.FullPathToFile : (d.NodeOld != null ? d.NodeOld.FullPathToFile : null);
                        diffSamples.Add(dObj);
                    }
                }

                if (!string.IsNullOrEmpty(outputDir) && wzNew.wz_files.Count > 0 && wzOld.wz_files.Count > 0)
                {
                    outputDir = Path.GetFullPath(outputDir);
                    Directory.CreateDirectory(outputDir);
                    EasyComparer easy = new EasyComparer();
                    easy.OutputPng = outputPng;
                    easy.OutputAddedImg = outputAdded;
                    easy.OutputRemovedImg = outputRemoved;
                    easy.Comparer.PngComparison = WzPngComparison.SizeAndDataLength;
                    easy.Comparer.ResolvePngLink = resolvePngLink;
                    easy.EasyCompareWzFiles(wzNew.wz_files[0], wzOld.wz_files[0], outputDir);
                }

                sw.Stop();
                JObject res = new JObject();
                res["newWzPath"] = newPath;
                res["oldWzPath"] = oldPath;
                res["totalDifferences"] = diffs.Count;
                res["addedCount"] = addedCount;
                res["removedCount"] = removedCount;
                res["changedCount"] = changedCount;
                res["outputDir"] = outputDir;
                res["elapsedMs"] = sw.ElapsedMilliseconds;
                res["sampleDifferences"] = diffSamples;
                return res;
            }
            finally
            {
                wzNew.Clear();
                wzOld.Clear();
            }
        }

        private JObject HandleRunLua(JObject args)
        {
            EnsureWzLoaded(args);
            string code = GetString(args, "code", null);
            string scriptPath = GetString(args, "script_path", null);
            if (string.IsNullOrEmpty(code) && string.IsNullOrEmpty(scriptPath))
            {
                throw new ArgumentException("Either 'code' or 'script_path' must be provided.");
            }

            string baseDir = null;
            if (!string.IsNullOrEmpty(scriptPath))
            {
                scriptPath = Path.GetFullPath(scriptPath);
                if (!File.Exists(scriptPath))
                    throw new FileNotFoundException("Lua script not found: " + scriptPath);
                code = File.ReadAllText(scriptPath, Encoding.UTF8);
                baseDir = Path.GetDirectoryName(scriptPath);
            }

            LuaEnvCapture envCapture = new LuaEnvCapture(this.context);
            Stopwatch sw = Stopwatch.StartNew();

            using (LuaSandbox sandbox = new LuaSandbox(envCapture))
            {
                sandbox.InitLuaEnv(baseDir, true);
                object[] retVals = sandbox.Lua.DoString(code);
                sw.Stop();

                JArray retArr = new JArray();
                if (retVals != null)
                {
                    for (int i = 0; i < retVals.Length; i++)
                    {
                        retArr.Add(FormatSimpleValue(retVals[i]));
                    }
                }

                JObject res = new JObject();
                res["success"] = true;
                res["elapsedMs"] = sw.ElapsedMilliseconds;
                res["logs"] = new JArray(envCapture.Lines.ToArray());
                res["returnValues"] = retArr;
                return res;
            }
        }

        private JObject HandleAvatarRender(JObject args)
        {
            EnsureWzLoaded(args);
            JArray partsArr = args["parts"] as JArray;
            string actionName = GetString(args, "action", "stand1");
            string emotionName = GetString(args, "emotion", "default");
            int frameIndex = GetInt(args, "frame", 0);
            bool hairCover = GetBool(args, "hair_cover", false);
            bool showHairShade = GetBool(args, "show_hair_shade", false);
            int earType = GetInt(args, "ear_type", 0);
            bool returnBase64 = GetBool(args, "return_base64", false);
            string outputPath = GetString(args, "output_path", null);

            Type mgrType = typeof(AfrmTooltip).Assembly.GetType("WzComparerR2.AvatarCommon.AvatarCanvasManager");
            if (mgrType != null)
            {
                object mgr = Activator.CreateInstance(mgrType);
                MethodInfo mAddBody = mgrType.GetMethod("AddBodyFromSkin", new Type[] { typeof(int) });
                MethodInfo mAddHairFace = mgrType.GetMethod("AddHairOrFace", new Type[] { typeof(int), typeof(bool) });
                MethodInfo mAddGear = mgrType.GetMethod("AddGear", new Type[] { typeof(int) });
                MethodInfo mSetEar = mgrType.GetMethod("SetEarType", new Type[] { typeof(int) });
                MethodInfo mGetBmp = mgrType.GetMethod("GetBitmapOrigin", new Type[] { typeof(string), typeof(string), typeof(int), typeof(int), typeof(int) });

                List<int> partIds = new List<int>();
                bool hasSkin = false;
                if (partsArr != null)
                {
                    for (int i = 0; i < partsArr.Count; i++)
                    {
                        int pid;
                        if (int.TryParse(partsArr[i].ToString().Trim(), out pid))
                        {
                            partIds.Add(pid);
                            if (pid >= 2000 && pid < 3000)
                                hasSkin = true;
                            else if (pid >= 12000 && pid < 13000)
                            {
                                partIds.Insert(0, pid - 10000);
                                hasSkin = true;
                            }
                        }
                    }
                }
                if (!hasSkin)
                {
                    partIds.Insert(0, 2000);
                }
                if (partIds.Count == 1)
                {
                    partIds.Add(20000);
                    partIds.Add(30000);
                    partIds.Add(1040036);
                    partIds.Add(1060026);
                }

                JArray loadedParts = new JArray();
                for (int i = 0; i < partIds.Count; i++)
                {
                    int pid = partIds[i];
                    if (pid >= 2000 && pid < 3000)
                    {
                        mAddBody.Invoke(mgr, new object[] { pid });
                        loadedParts.Add(pid);
                    }
                    else if (pid >= 12000 && pid < 13000)
                    {
                        // Handled by AddBodyFromSkin
                    }
                    else if (pid >= 20000 && pid < 100000)
                    {
                        mAddHairFace.Invoke(mgr, new object[] { pid, false });
                        loadedParts.Add(pid);
                    }
                    else if (pid >= 1000000)
                    {
                        mAddGear.Invoke(mgr, new object[] { pid });
                        loadedParts.Add(pid);
                    }
                }

                if (mSetEar != null)
                    mSetEar.Invoke(mgr, new object[] { earType });

                BitmapOrigin rendered = (BitmapOrigin)mGetBmp.Invoke(mgr, new object[] { actionName, emotionName, frameIndex, 0, 0 });
                if (rendered.Bitmap == null)
                    throw new InvalidOperationException("AvatarCanvasManager.GetBitmapOrigin returned null Bitmap.");

                try
                {
                    if (string.IsNullOrEmpty(outputPath))
                    {
                        string outDir = Path.Combine(Path.GetTempPath(), "wcr2_mcp_output", "avatar");
                        Directory.CreateDirectory(outDir);
                        outputPath = Path.Combine(outDir, string.Format("avatar_{0}_{1}_{2}.png", actionName, emotionName, frameIndex));
                    }
                    else
                    {
                        outputPath = Path.GetFullPath(outputPath);
                        string dir = Path.GetDirectoryName(outputPath);
                        if (!string.IsNullOrEmpty(dir))
                            Directory.CreateDirectory(dir);
                    }

                    rendered.Bitmap.Save(outputPath, ImageFormat.Png);

                    JObject res = new JObject();
                    res["outputPath"] = outputPath;
                    res["width"] = rendered.Bitmap.Width;
                    res["height"] = rendered.Bitmap.Height;
                    res["origin"] = new JObject { { "x", rendered.Origin.X }, { "y", rendered.Origin.Y } };
                    res["action"] = actionName;
                    res["emotion"] = emotionName;
                    res["frame"] = frameIndex;
                    res["loadedParts"] = loadedParts;
                    if (returnBase64)
                    {
                        res["mimeType"] = "image/png";
                        res["base64"] = Convert.ToBase64String(File.ReadAllBytes(outputPath));
                    }
                    return res;
                }
                finally
                {
                    rendered.Bitmap.Dispose();
                }
            }

            throw new InvalidOperationException("AvatarCanvasManager is not available.");
        }

        private static string EscapeCsvField(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            if (value.IndexOfAny(new char[] { ',', '"', '\r', '\n' }) >= 0)
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }
            return value;
        }

        private JObject HandleExportDbCsv(JObject args)
        {
            EnsureWzLoaded(args);
            EnsureStringLinkerLoaded();
            string exportType = (GetString(args, "category", null) ?? GetString(args, "type", "eqp")).ToLowerInvariant();
            string outputPathArg = GetString(args, "output_path", null);
            string outputDir = GetString(args, "output_dir", null);

            if (!string.IsNullOrEmpty(outputPathArg) && outputPathArg.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                outputPathArg = Path.GetFullPath(outputPathArg);
                outputDir = Path.GetDirectoryName(outputPathArg);
            }
            if (string.IsNullOrEmpty(outputDir))
            {
                outputDir = Path.Combine(Path.GetTempPath(), "wcr2_mcp_output", "db_" + exportType);
            }
            outputDir = Path.GetFullPath(outputDir);
            Directory.CreateDirectory(outputDir);

            StringLinker sl = GetStringLinker();
            JArray files = new JArray();
            int totalRows = 0;

            if (exportType == "skill_option" || exportType == "skill_db")
            {
                DBConnection conn = new DBConnection(sl);
                if (exportType == "skill_option")
                {
                    conn.ExportSkillOption(outputDir);
                    string[] csvs = Directory.GetFiles(outputDir, "*.csv");
                    for (int i = 0; i < csvs.Length; i++)
                        files.Add(csvs[i]);
                }
                else
                {
                    DataSet ds = conn.GenerateSkillTable();
                    if (ds != null)
                    {
                        foreach (DataTable dt in ds.Tables)
                        {
                            string csvPath = Path.Combine(outputDir, dt.TableName + ".csv");
                            using (FileStream fs = new FileStream(csvPath, FileMode.Create, FileAccess.Write))
                            using (StreamWriter sw = new StreamWriter(fs, new UTF8Encoding(true)))
                            {
                                conn.OutputCsv(sw, dt);
                            }
                            totalRows += dt.Rows.Count;
                            files.Add(csvPath);
                        }
                    }
                }
            }
            else
            {
                List<KeyValuePair<string, Dictionary<int, StringResult>>> dicts = new List<KeyValuePair<string, Dictionary<int, StringResult>>>();
                if (exportType == "all" || exportType == "eqp" || exportType == "gear")
                    dicts.Add(new KeyValuePair<string, Dictionary<int, StringResult>>("eqp", sl.StringEqp));
                if (exportType == "all" || exportType == "item")
                    dicts.Add(new KeyValuePair<string, Dictionary<int, StringResult>>("item", sl.StringItem));
                if (exportType == "all" || exportType == "skill")
                    dicts.Add(new KeyValuePair<string, Dictionary<int, StringResult>>("skill", sl.StringSkill));
                if (exportType == "all" || exportType == "mob")
                    dicts.Add(new KeyValuePair<string, Dictionary<int, StringResult>>("mob", sl.StringMob));
                if (exportType == "all" || exportType == "map")
                    dicts.Add(new KeyValuePair<string, Dictionary<int, StringResult>>("map", sl.StringMap));
                if (exportType == "all" || exportType == "npc")
                    dicts.Add(new KeyValuePair<string, Dictionary<int, StringResult>>("npc", sl.StringNpc));

                AddExtraStringDictIfRequested(sl, dicts, exportType, "quest", "StringQuest");
                AddExtraStringDictIfRequested(sl, dicts, exportType, "set_item", "StringSetItem");
                AddExtraStringDictIfRequested(sl, dicts, exportType, "achievement", "StringAchievement");
                AddExtraStringDictIfRequested(sl, dicts, exportType, "reactor", "StringReactor");

                for (int d = 0; d < dicts.Count; d++)
                {
                    string catName = dicts[d].Key;
                    Dictionary<int, StringResult> dict = dicts[d].Value;
                    if (dict == null)
                        continue;

                    string csvPath = (dicts.Count == 1 && !string.IsNullOrEmpty(outputPathArg))
                        ? outputPathArg
                        : Path.Combine(outputDir, catName + ".csv");

                    using (FileStream fs = new FileStream(csvPath, FileMode.Create, FileAccess.Write))
                    using (StreamWriter sw = new StreamWriter(fs, new UTF8Encoding(true)))
                    {
                        sw.WriteLine("id,category,name,desc,stringPath");
                        foreach (KeyValuePair<int, StringResult> kv in dict)
                        {
                            StringResult sr = kv.Value;
                            string line = string.Format("{0},{1},{2},{3},{4}",
                                kv.Key,
                                EscapeCsvField(catName),
                                EscapeCsvField(sr != null ? sr.Name : ""),
                                EscapeCsvField(sr != null ? sr.Desc : ""),
                                EscapeCsvField(sr != null ? sr.FullPath : ""));
                            sw.WriteLine(line);
                            totalRows++;
                        }
                    }
                    files.Add(csvPath);
                }
            }

            JObject res = new JObject();
            res["category"] = exportType;
            res["rowCount"] = totalRows;
            res["outputDir"] = outputDir;
            res["outputPath"] = files.Count > 0 ? files[0].ToString() : null;
            res["files"] = files;
            return res;
        }

        private JObject HandleGuiControl(JObject args)
        {
            if (!this.IsGuiMode)
            {
                throw new InvalidOperationException("GUI control action requires WzComparerR2 GUI mode (port 21480). Use action='launch' in wcr2_gui_control to start WzComparerR2.exe.");
            }

            string action = GetString(args, "action", "status").ToLowerInvariant();
            Form mainForm = this.context.MainForm;

            switch (action)
            {
                case "status":
                    return HandleStatus(args);

                case "get_selected":
                    {
                        JObject selInfo = new JObject();
                        selInfo["selectedNode1"] = this.context.SelectedNode1 != null ? this.context.SelectedNode1.FullPathToFile : null;
                        selInfo["selectedNode2"] = this.context.SelectedNode2 != null ? this.context.SelectedNode2.FullPathToFile : null;
                        selInfo["selectedNode3"] = this.context.SelectedNode3 != null ? this.context.SelectedNode3.FullPathToFile : null;
                        selInfo["selectedTab"] = this.context.SelectedTab != null ? this.context.SelectedTab.Text : null;
                        return selInfo;
                    }

                case "open_wz":
                    return HandleOpenWz(args);

                case "select_node":
                    {
                        string path = GetString(args, "path", null);
                        if (string.IsNullOrEmpty(path))
                            throw new ArgumentException("Parameter 'path' is required for select_node.");

                        EnsureWzLoaded(args);
                        Wz_Node wzNode = ResolveWzNodeByPath(path, false);
                        bool selected = false;
                        if (wzNode != null)
                        {
                            MethodInfo mSelect = mainForm.GetType().GetMethod("OnSelectedWzNode", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                            if (mSelect != null)
                            {
                                object r = mSelect.Invoke(mainForm, new object[] { wzNode });
                                selected = r is bool && (bool)r;
                            }
                        }
                        if (!selected)
                        {
                            MethodInfo mSearchPath = mainForm.GetType().GetMethod("searchAdvTreeFullPath", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                            if (mSearchPath != null)
                            {
                                mSearchPath.Invoke(mainForm, new object[] { path.Replace('\\', '/') });
                                selected = this.context.SelectedNode2 != null || this.context.SelectedNode1 != null;
                            }
                        }

                        JObject selRes = new JObject();
                        selRes["selected"] = selected;
                        selRes["selectedNode1"] = this.context.SelectedNode1 != null ? this.context.SelectedNode1.FullPathToFile : null;
                        selRes["selectedNode2"] = this.context.SelectedNode2 != null ? this.context.SelectedNode2.FullPathToFile : null;
                        selRes["selectedNode3"] = this.context.SelectedNode3 != null ? this.context.SelectedNode3.FullPathToFile : null;
                        return selRes;
                    }

                case "quick_view":
                    {
                        string path = GetString(args, "path", null);
                        if (!string.IsNullOrEmpty(path))
                        {
                            JObject selArgs = new JObject();
                            selArgs["action"] = "select_node";
                            selArgs["path"] = path;
                            HandleGuiControl(selArgs);
                        }

                        MethodInfo mQv = mainForm.GetType().GetMethod("quickView", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null);
                        if (mQv != null)
                        {
                            mQv.Invoke(mainForm, null);
                        }

                        AfrmTooltip tip = GetTooltipWindow();
                        string savedPath = GetString(args, "output_path", null);
                        if (tip != null && tip.Bitmap != null && !string.IsNullOrEmpty(savedPath))
                        {
                            savedPath = Path.GetFullPath(savedPath);
                            string dir = Path.GetDirectoryName(savedPath);
                            if (!string.IsNullOrEmpty(dir))
                                Directory.CreateDirectory(dir);
                            tip.Bitmap.Save(savedPath, ImageFormat.Png);
                        }

                        JObject qvRes = new JObject();
                        qvRes["visible"] = tip != null && tip.Visible;
                        qvRes["imageFileName"] = tip != null ? tip.ImageFileName : null;
                        qvRes["outputPath"] = savedPath;
                        return qvRes;
                    }

                case "search_string":
                    {
                        string query = GetString(args, "query", "");
                        int categoryIndex = GetInt(args, "category_index", 0);
                        bool exact = GetBool(args, "exact", false);
                        bool regex = GetBool(args, "regex", false);

                        TextBoxItem txtSearch = GetPrivateField<TextBoxItem>(mainForm, "textBoxItemSearchString");
                        ComboBoxItem cmbCat = GetPrivateField<ComboBoxItem>(mainForm, "comboBoxItem2");
                        CheckBoxItem chkExact = GetPrivateField<CheckBoxItem>(mainForm, "checkBoxItemExact2");
                        CheckBoxItem chkRegex = GetPrivateField<CheckBoxItem>(mainForm, "checkBoxItemRegex2");

                        if (txtSearch != null) txtSearch.Text = query;
                        if (cmbCat != null && categoryIndex >= 0 && categoryIndex < cmbCat.Items.Count) cmbCat.SelectedIndex = categoryIndex;
                        if (chkExact != null) chkExact.Checked = exact;
                        if (chkRegex != null) chkRegex.Checked = regex;

                        MethodInfo mClick = mainForm.GetType().GetMethod("buttonItemSearchString_Click", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        if (mClick != null)
                        {
                            mClick.Invoke(mainForm, new object[] { null, EventArgs.Empty });
                        }

                        ListView listView = GetPrivateField<ListView>(mainForm, "listViewExString");
                        JObject ssRes = new JObject();
                        ssRes["query"] = query;
                        ssRes["resultCount"] = listView != null ? listView.Items.Count : 0;
                        return ssRes;
                    }

                case "switch_tab":
                    {
                        string tabName = GetString(args, "tab_name", null);
                        int tabIndex = GetInt(args, "tab_index", -1);
                        SuperTabControl tabCtrl = GetPrivateField<SuperTabControl>(mainForm, "superTabControl1");
                        if (tabCtrl != null)
                        {
                            if (tabIndex >= 0 && tabIndex < tabCtrl.Tabs.Count)
                            {
                                tabCtrl.SelectedTabIndex = tabIndex;
                            }
                            else if (!string.IsNullOrEmpty(tabName))
                            {
                                for (int i = 0; i < tabCtrl.Tabs.Count; i++)
                                {
                                    SuperTabItem ti = tabCtrl.Tabs[i] as SuperTabItem;
                                    if (ti != null && ti.Text.IndexOf(tabName, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        tabCtrl.SelectedTab = ti;
                                        break;
                                    }
                                }
                            }
                        }

                        JObject tabRes = new JObject();
                        tabRes["selectedTab"] = this.context.SelectedTab != null ? this.context.SelectedTab.Text : null;
                        return tabRes;
                    }

                case "screenshot":
                    {
                        string outputPath = GetString(args, "output_path", null);
                        if (string.IsNullOrEmpty(outputPath))
                        {
                            string outDir = Path.Combine(Path.GetTempPath(), "wcr2_mcp_output", "screenshots");
                            Directory.CreateDirectory(outDir);
                            outputPath = Path.Combine(outDir, string.Format("wcr2_gui_{0:yyyyMMdd_HHmmss}.png", DateTime.Now));
                        }
                        else
                        {
                            outputPath = Path.GetFullPath(outputPath);
                            string dir = Path.GetDirectoryName(outputPath);
                            if (!string.IsNullOrEmpty(dir))
                                Directory.CreateDirectory(dir);
                        }

                        using (Bitmap bmp = new Bitmap(mainForm.Width, mainForm.Height))
                        {
                            mainForm.DrawToBitmap(bmp, new Rectangle(0, 0, mainForm.Width, mainForm.Height));
                            bmp.Save(outputPath, ImageFormat.Png);
                        }

                        JObject scRes = new JObject();
                        scRes["outputPath"] = outputPath;
                        scRes["width"] = mainForm.Width;
                        scRes["height"] = mainForm.Height;
                        return scRes;
                    }

                case "close":
                    {
                        mainForm.BeginInvoke(new System.Action(delegate { mainForm.Close(); }));
                        JObject closeRes = new JObject();
                        closeRes["closing"] = true;
                        return closeRes;
                    }

                default:
                    throw new ArgumentException("Unsupported GUI action: " + action);
            }
        }

        private class LuaEnvCapture
        {
            private readonly PluginContext context;
            public readonly List<string> Lines = new List<string>();

            public LuaEnvCapture(PluginContext context)
            {
                this.context = context;
            }

            public PluginContext Context
            {
                get { return this.context; }
            }

            public void WriteLine()
            {
                this.Lines.Add("");
            }

            public void WriteLine(object value)
            {
                this.Lines.Add(value != null ? value.ToString() : "nil");
            }

            public void WriteLine(string format, params object[] args)
            {
                this.Lines.Add(args != null && args.Length > 0 ? string.Format(format, args) : format);
            }

            public void Write(object value)
            {
                if (this.Lines.Count == 0)
                    this.Lines.Add("");
                this.Lines[this.Lines.Count - 1] += (value != null ? value.ToString() : "nil");
            }
        }

        private static JToken GetArgToken(JObject args, string key)
        {
            if (args == null || string.IsNullOrEmpty(key))
                return null;
            JToken tok = args[key];
            if (tok != null && tok.Type != JTokenType.Null)
                return tok;
            if (key.IndexOf('_') >= 0)
            {
                string[] parts = key.Split('_');
                StringBuilder sb = new StringBuilder(parts[0]);
                for (int i = 1; i < parts.Length; i++)
                {
                    if (parts[i].Length > 0)
                    {
                        sb.Append(char.ToUpperInvariant(parts[i][0]));
                        if (parts[i].Length > 1)
                            sb.Append(parts[i].Substring(1));
                    }
                }
                tok = args[sb.ToString()];
                if (tok != null && tok.Type != JTokenType.Null)
                    return tok;
            }
            return null;
        }

        private static string GetString(JObject args, string key, string defaultValue)
        {
            JToken tok = GetArgToken(args, key);
            if (tok == null)
                return defaultValue;
            return tok.ToString();
        }

        private static int GetInt(JObject args, string key, int defaultValue)
        {
            JToken tok = GetArgToken(args, key);
            if (tok == null)
                return defaultValue;
            int val;
            if (int.TryParse(tok.ToString(), out val))
                return val;
            return defaultValue;
        }

        private static bool GetBool(JObject args, string key, bool defaultValue)
        {
            JToken tok = GetArgToken(args, key);
            if (tok == null)
                return defaultValue;
            bool val;
            if (bool.TryParse(tok.ToString(), out val))
                return val;
            return defaultValue;
        }

        private static T GetPrivateField<T>(object target, string fieldName) where T : class
        {
            if (target == null)
                return null;
            FieldInfo f = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (f != null)
                return f.GetValue(target) as T;
            return null;
        }

        private static void SetPropIfExists(object target, string propName, object value)
        {
            if (target == null)
                return;
            PropertyInfo p = target.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanWrite)
            {
                try
                {
                    p.SetValue(target, value, null);
                }
                catch
                {
                }
            }
        }

        private static string GetPropString(object target, string propName)
        {
            if (target == null)
                return null;
            PropertyInfo p = target.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanRead)
            {
                try
                {
                    object v = p.GetValue(target, null);
                    return v != null ? v.ToString() : null;
                }
                catch
                {
                }
            }
            return null;
        }

        private static void AddDictCountIfExists(StringLinker sl, JObject counts, string propName, string jsonKey)
        {
            PropertyInfo p = sl.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);
            if (p != null)
            {
                ICollection col = p.GetValue(sl, null) as ICollection;
                if (col != null)
                {
                    counts[jsonKey] = col.Count;
                }
            }
        }

        private static void AddExtraStringDictIfRequested(
            StringLinker sl,
            List<KeyValuePair<string, Dictionary<int, StringResult>>> dicts,
            string category,
            string catKey,
            string propName)
        {
            if (category == "all" || category == catKey)
            {
                PropertyInfo p = sl.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);
                if (p != null)
                {
                    Dictionary<int, StringResult> d = p.GetValue(sl, null) as Dictionary<int, StringResult>;
                    if (d != null)
                        dicts.Add(new KeyValuePair<string, Dictionary<int, StringResult>>(catKey, d));
                }
            }
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "item";
            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (Array.IndexOf(invalid, c) >= 0)
                    sb.Append('_');
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
