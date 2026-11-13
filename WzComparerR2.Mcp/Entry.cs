using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using DevComponents.DotNetBar;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WzComparerR2.PluginBase;

namespace WzComparerR2.Mcp
{
    public class Entry : PluginEntry
    {
        public Entry(PluginContext context)
            : base(context)
        {
            Instance = this;
        }

        public static Entry Instance { get; private set; }
        public Wcr2Engine Engine { get; private set; }
        public Wcr2BridgeServer Server { get; private set; }
        private ButtonItem btnStatus;

        private static void SaveUserMcpConfig(int guiPort)
        {
            try
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (string.IsNullOrEmpty(home))
                    return;
                string cfgPath = Path.Combine(home, ".wcr2-mcp.json");
                JObject cfg = new JObject();
                if (File.Exists(cfgPath))
                {
                    try
                    {
                        cfg = JObject.Parse(File.ReadAllText(cfgPath, Encoding.UTF8));
                    }
                    catch
                    {
                        cfg = new JObject();
                    }
                }
                cfg["installDir"] = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\', '/');
                cfg["guiPort"] = guiPort;
                cfg["updatedAt"] = DateTime.UtcNow.ToString("o");
                File.WriteAllText(cfgPath, cfg.ToString(Formatting.Indented), Encoding.UTF8);
            }
            catch
            {
            }
        }

        protected override void OnLoad()
        {
            int port = 21480;
            string envPort = Environment.GetEnvironmentVariable("WCR2_MCP_GUI_PORT");
            int parsedPort;
            if (!string.IsNullOrEmpty(envPort) && int.TryParse(envPort, out parsedPort) && parsedPort > 0)
            {
                port = parsedPort;
            }

            this.Engine = new Wcr2Engine(this.Context);
            this.Server = new Wcr2BridgeServer(this.Engine, port);

            try
            {
                this.Server.Start();
                SaveUserMcpConfig(this.Server.Port);
            }
            catch (Exception ex)
            {
                PluginManager.LogError("WzComparerR2.Mcp", ex, "Failed to start MCP bridge server on port {0}", port);
            }

            try
            {
                RibbonBar bar = this.Context.AddRibbonBar("Tools", "MCP Agent");
                this.btnStatus = new ButtonItem("btnMcpStatus", this.Server.IsRunning
                    ? string.Format("MCP ({0})", this.Server.Port)
                    : "MCP (Off)");
                this.btnStatus.Tooltip = this.Server.IsRunning
                    ? string.Format("WzComparerR2 MCP Bridge listening on http://127.0.0.1:{0}/", this.Server.Port)
                    : "WzComparerR2 MCP Bridge is stopped. Click to start.";
                this.btnStatus.Click += BtnStatus_Click;
                bar.Items.Add(this.btnStatus);
            }
            catch (Exception ex)
            {
                PluginManager.LogError("WzComparerR2.Mcp", ex, "Failed to add MCP RibbonBar");
            }
        }

        private void BtnStatus_Click(object sender, EventArgs e)
        {
            if (this.Server == null)
                return;

            if (this.Server.IsRunning)
            {
                string url = string.Format("http://127.0.0.1:{0}/", this.Server.Port);
                try
                {
                    Clipboard.SetText(url);
                }
                catch
                {
                }
                MessageBoxEx.Show(
                    string.Format("WzComparerR2 MCP Bridge is active.\r\nEndpoint: {0}\r\nRequests handled: {1}\r\n(URL copied to clipboard)", url, this.Server.RequestCount),
                    "WzComparerR2 MCP Agent",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else
            {
                try
                {
                    this.Server.Start();
                    if (this.btnStatus != null)
                    {
                        this.btnStatus.Text = string.Format("MCP ({0})", this.Server.Port);
                        this.btnStatus.Tooltip = string.Format("WzComparerR2 MCP Bridge listening on http://127.0.0.1:{0}/", this.Server.Port);
                    }
                }
                catch (Exception ex)
                {
                    MessageBoxEx.Show("Failed to start MCP server:\r\n" + ex.Message, "WzComparerR2 MCP Agent", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        protected override void OnUnload()
        {
            if (this.Server != null)
            {
                this.Server.Stop();
                this.Server = null;
            }
            base.OnUnload();
        }
    }
}
