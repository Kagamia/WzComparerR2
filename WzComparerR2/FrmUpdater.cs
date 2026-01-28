using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using WzComparerR2.Config;

namespace WzComparerR2
{
    public partial class FrmUpdater : DevComponents.DotNetBar.Office2007Form
    {
        public FrmUpdater()
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font(new FontFamily("Microsoft Sans Serif"), 8f);
#endif

            this.lblCurrentVer.Text = $"{versionNumbers()[2]}.{versionNumbers()[3]}";
            Task.Run(() => this.ExecuteUpdateAsync());
        }

        public bool EnableAutoUpdate
        {
            get { return chkEnableAutoUpdate.Checked; }
            set { chkEnableAutoUpdate.Checked = value; }
        }

        private string net462url;
        private string net60url;
        private string net80url;
        private string fileurl;

        private static string checkUpdateURL = "https://api.github.com/repos/Kagamia/WzComparerR2/releases/latest";

        private string GetFileVersion()
        {
            return this.GetAsmAttr<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? this.GetAsmAttr<AssemblyFileVersionAttribute>()?.Version;
        }

        private int[] versionNumbers()
        {
            string[] parts = GetFileVersion().ToString().Split('.');
            int[] numbers = new int[4];
            for (int i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out int num))
                {
                    numbers[i] = num;
                }
                else
                {
                    numbers[i] = 0;
                }
            }
            return numbers;
        }

        private static int[] getCiVersionNumbers(string version)
        {
            string[] parts = version.Split('-')[2].Split('.');
            int[] numbers = new int[2];
            for (int i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out int num))
                {
                    numbers[i] = num;
                }
                else
                {
                    numbers[i] = 0;
                }
            }
            return numbers;
        }

        public async Task<bool> QueryUpdate()
        {
#if DEBUG
            // Disable update check in debug builds
            return false;
#endif
            var request = (HttpWebRequest)WebRequest.Create(checkUpdateURL);
            request.Accept = "application/json";
            request.UserAgent = "WzComparerR2/1.0";
            try
            {
                using (WebResponse response = await request.GetResponseAsync())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    string responseString = reader.ReadToEnd();
                    JObject UpdateContent = JObject.Parse(responseString);
                    string BuildNumber = UpdateContent.SelectToken("name").ToString();
                    int[] latestCiVersion = getCiVersionNumbers(BuildNumber);
                    int[] currentVersion = versionNumbers();
                    return (latestCiVersion[0] > currentVersion[2]) || (latestCiVersion[0] == currentVersion[2] && latestCiVersion[1] > currentVersion[3]);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task ExecuteUpdateAsync()
        {
            var request = (HttpWebRequest)WebRequest.Create(checkUpdateURL);
            request.Accept = "application/json";
            request.UserAgent = "WzComparerR2/1.0";
            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                JObject UpdateContent = JObject.Parse(responseString);
                // string BuildNumber = UpdateContent.SelectToken("tag_name").ToString();
                string ChangeTitle = UpdateContent.SelectToken("name").ToString();
                string Changelog = UpdateContent.SelectToken("body").ToString();
                JArray assets = (JArray)UpdateContent["assets"];
                string[] downloadUrls = new string[assets.Count];
                for (int i = 0; i < assets.Count; i++)
                {
                    downloadUrls[i] = assets[i]["browser_download_url"]?.ToString();
                }
                // This part is for builds that are separated into 3 packages
                foreach (string url in downloadUrls)
                {
                    if (url.Contains("net6")) net60url = url;
                    else if (url.Contains("net8")) net80url = url;
                    else net462url = url;
                }

                // fileurl = downloadUrls[0];
                int[] latestCiVersion = getCiVersionNumbers(ChangeTitle);
                int[] currentVersion = versionNumbers();
                this.lblLatestVer.Text = $"{latestCiVersion[0]}.{latestCiVersion[1]}";
                // AppendText(ChangeTitle + "\r\n", Color.Red);
                AppendText(Changelog, Color.Black);
                this.richTextBoxEx1.SelectionStart = 0;

                if ((latestCiVersion[0] > currentVersion[2]) || (latestCiVersion[0] == currentVersion[2] && latestCiVersion[1] > currentVersion[3]))
                {
                    buttonX1.Enabled = true;
                    this.lblUpdateContent.Text = "更新可用";
                }
                else
                {
                    this.lblUpdateContent.Text = "已是最新版本";
                }
            }
            catch (Exception ex)
            {
                this.lblUpdateContent.Text = "更新检查失败";
                AppendText(ex.Message + "\r\n" + ex.StackTrace, Color.Red);
            }
        }

        private async Task DownloadUpdateAsync(string url)
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string savePath = Path.Combine(currentDirectory, "update.zip");
            try
            {
                buttonX1.Enabled = false;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            using (FileStream fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                            {
                                responseStream.CopyTo(fileStream);
                            }
                        }
                    }
                }
                ExtractResource("WzComparerR2.WzComparerR2.Updater.exe", Path.Combine(currentDirectory, "WzComparerR2.Updater.exe"));
#if NET6_0_OR_GREATER
                ExtractResource("WzComparerR2.WzComparerR2.Updater.deps.json", Path.Combine(currentDirectory, "WzComparerR2.Updater.deps.json"));
                ExtractResource("WzComparerR2.WzComparerR2.Updater.dll", Path.Combine(currentDirectory, "WzComparerR2.Updater.dll"));
                ExtractResource("WzComparerR2.WzComparerR2.Updater.dll.config", Path.Combine(currentDirectory, "WzComparerR2.Updater.dll.config"));
                ExtractResource("WzComparerR2.WzComparerR2.Updater.runtimeconfig.json", Path.Combine(currentDirectory, "WzComparerR2.Updater.runtimeconfig.json"));
#else
                ExtractResource("WzComparerR2.WzComparerR2.Updater.exe.config", Path.Combine(currentDirectory, "WzComparerR2.Updater.exe.config"));
#endif
                RunProgram("WzComparerR2.Updater.exe", "\"" + savePath + "\"");
            }
            catch (Exception ex)
            {
                this.lblUpdateContent.Text = "更新下载失败";
                AppendText(ex.Message + "\r\n" + ex.StackTrace, Color.Red);
            }
            finally
            {
                buttonX1.Enabled = true;
            }
        }

        private void buttonX1_Click(object sender, EventArgs e)
        {
            this.lblUpdateContent.Text = "正在下载更新...";
            buttonX1.Enabled = false;
            string selectedURL = "";
            switch (Environment.Version.Major)
            {
                default:
                case 4:
                    selectedURL = net462url;
                    break;
                case 6:
                    selectedURL = net60url;
                    break;
                case 8:
                    selectedURL = net80url;
                    break;
            }
            Task.Run(() => this.DownloadUpdateAsync(selectedURL));
        }

        private void RunProgram(string url, string argument)
        {
#if NET6_0_OR_GREATER
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = url,
                Arguments = argument,
            });
#else
            Process.Start(url, argument);
#endif
        }

        private void AppendText(string text, Color color)
        {
            this.richTextBoxEx1.SelectionStart = this.richTextBoxEx1.TextLength;
            this.richTextBoxEx1.SelectionLength = 0;

            this.richTextBoxEx1.SelectionColor = color;
            this.richTextBoxEx1.AppendText(text);
            this.richTextBoxEx1.SelectionColor = this.richTextBoxEx1.ForeColor;
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

        private void ExtractResource(string resourceName, string outputPath)
        {
            var assembly = Assembly.GetExecutingAssembly();

            using Stream? resourceStream = assembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null)
                throw new InvalidOperationException($"Resource not found: {resourceName}");

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            using FileStream fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write);
            resourceStream.CopyTo(fileStream);
        }

        private void chkEnableAutoUpdate_CheckedChanged(object sender, EventArgs e)
        {
            var config = WcR2Config.Default;
            config.EnableAutoUpdate = chkEnableAutoUpdate.Checked;
            ConfigManager.Save();
        }

        public void Load(WcR2Config config)
        {
            this.EnableAutoUpdate = config.EnableAutoUpdate;
        }
    }
}