using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WzComparerR2.Mcp
{
    public static class HostProgram
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        private static void InitNativeDirectories()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string arch = IntPtr.Size == 8 ? "x64" : "x86";
                string libArchDir = Path.Combine(baseDir, "Lib", arch);
                if (Directory.Exists(libArchDir))
                {
                    SetDllDirectory(libArchDir);
                    string[] dlls = Directory.GetFiles(libArchDir, "*.dll");
                    for (int i = 0; i < dlls.Length; i++)
                    {
                        LoadLibrary(dlls[i]);
                    }
                }
            }
            catch
            {
            }
        }

        [STAThread]
        public static int Main(string[] args)
        {
            try
            {
                Console.InputEncoding = Encoding.UTF8;
                Console.OutputEncoding = Encoding.UTF8;
            }
            catch
            {
            }
            InitNativeDirectories();

            int port = 21481;
            string cliMethod = null;
            string cliJson = "{}";

            for (int i = 0; i < args.Length; i++)
            {
                if ((args[i] == "--port" || args[i] == "-p") && i + 1 < args.Length)
                {
                    int.TryParse(args[++i], out port);
                }
                else if ((args[i] == "--call" || args[i] == "-c") && i + 1 < args.Length)
                {
                    cliMethod = args[++i];
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                    {
                        cliJson = args[++i];
                    }
                }
            }

            Wcr2Engine engine = new Wcr2Engine(null);

            if (!string.IsNullOrEmpty(cliMethod))
            {
                try
                {
                    JObject reqArgs = string.IsNullOrEmpty(cliJson) ? new JObject() : JObject.Parse(cliJson);
                    JObject result = engine.Execute(cliMethod, reqArgs);
                    JObject wrap = new JObject();
                    wrap["ok"] = true;
                    wrap["result"] = result;
                    Console.WriteLine(wrap.ToString(Formatting.Indented));
                    return 0;
                }
                catch (Exception ex)
                {
                    JObject err = new JObject();
                    err["ok"] = false;
                    err["error"] = ex.Message;
                    err["stackTrace"] = ex.ToString();
                    Console.WriteLine(err.ToString(Formatting.Indented));
                    return 1;
                }
            }

            using (Wcr2BridgeServer server = new Wcr2BridgeServer(engine, port))
            {
                server.Start();
                JObject started = new JObject();
                started["status"] = "listening";
                started["mode"] = "headless";
                started["port"] = server.Port;
                started["url"] = string.Format("http://127.0.0.1:{0}/", server.Port);
                started["pid"] = System.Diagnostics.Process.GetCurrentProcess().Id;
                Console.WriteLine(started.ToString(Formatting.None));

                ManualResetEvent quitEvent = new ManualResetEvent(false);
                try
                {
                    Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e)
                    {
                        e.Cancel = true;
                        quitEvent.Set();
                    };
                }
                catch
                {
                }
                quitEvent.WaitOne();
            }

            return 0;
        }
    }
}
