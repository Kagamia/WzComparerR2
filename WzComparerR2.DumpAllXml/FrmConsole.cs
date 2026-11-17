using DevComponents.DotNetBar;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using WzComparerR2.Config;

namespace WzComparerR2.DumpAllXml {
    public partial class FrmConsole : DevComponents.DotNetBar.Office2007Form {
        private readonly WzXmlExporter exporter;
        private readonly ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
        private bool isRunning = false;

        private CancellationTokenSource cancellationTokenSource;

        public FrmConsole() {
            InitializeComponent();
            Log("使用说明：请先在 WzComparerR2 中打开 Base.wz，再点击「开始一键解析」。");
            exporter = new WzXmlExporter(Log);
            uiTimer.Start();
            loadConfig();
        }


        private void FrmConsole_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason == CloseReason.UserClosing && isRunning) {
                if (DialogResult.Yes != MessageBoxEx.Show(this, "任务还在运行中，是否关闭？", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)) {
                    e.Cancel = true;
                    return;
                }
            }
        }

        private void loadConfig() {
            ConfigManager.Reload();
            var config = DumpConfig.Default;
            // 未配置过时给一个与用户环境无关的默认目录
            textBox导出文件夹路径.Text = !string.IsNullOrEmpty(config.OutputDirectory)
                ? config.OutputDirectory
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WzDump");
            if (!string.IsNullOrEmpty(config.FilterTypes)) {
                textBox过滤数据类型.Text = config.FilterTypes;
            }
            if (!string.IsNullOrEmpty(config.ExportWzTypes)) {
                textBox待导出WZ类型.Text = config.ExportWzTypes;
            }
            if (config.Concurrency > 0) {
                numericUpDown导出并发数.Value = Math.Min(config.Concurrency.Value, (int)numericUpDown导出并发数.Maximum);
            }
        }
        private DumpConfig SaveConfig() {
            var config = DumpConfig.Default;
            config.OutputDirectory = textBox导出文件夹路径.Text;
            config.FilterTypes = textBox过滤数据类型.Text;
            config.ExportWzTypes = textBox待导出WZ类型.Text;
            config.Concurrency = (int)numericUpDown导出并发数.Value;
            ConfigManager.Save();
            return config;
        }

        private void buttonBrowse_Click(object sender, EventArgs e) {
            using (var dialog = new FolderBrowserDialog()) {
                dialog.Description = "选择导出文件夹";
                dialog.ShowNewFolderButton = true;
                string current = textBox导出文件夹路径.Text;
                if (!string.IsNullOrEmpty(current) && Directory.Exists(current)) {
                    dialog.SelectedPath = current;
                }
                if (dialog.ShowDialog(this) == DialogResult.OK) {
                    textBox导出文件夹路径.Text = dialog.SelectedPath;
                }
            }
        }

        private async void buttonStart_Click(object sender, System.EventArgs e) {
            if (isRunning) return;

            isRunning = true;
            this.buttonStart.Enabled = false;
            logBox.Clear();
            var sw = System.Diagnostics.Stopwatch.StartNew();  // 开始计时

            try {
                DumpConfig config = SaveConfig();
                string outputDir = config.OutputDirectory;
                IList<string> filters = config.FilterTypes.Value.Split(',').Select(s => s.Trim()).ToList();
                string[] wzList = config.ExportWzTypes.Value.Split(',').Select(s => s.Trim()).ToArray();
                int fileWriteSemaphoreCount = config.Concurrency;

                await exporter.ExportAllAsync(outputDir, filters, wzList, fileWriteSemaphoreCount);
            } catch (Exception ex) {
                Log($"\r\n[致命错误] {ex}\r\n");
            } finally {
                sw.Stop();  // 停止计时
                Log($"[总耗时] {sw.ElapsedMilliseconds} ms\r\n");  // 输出耗时

                isRunning = false;
                this.buttonStart.Enabled = true;
            }
        }

        // 线程安全的日志方法
        public void Log(string message) {
            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string line = $"[{timestamp}] {message}";
            if (line.EndsWith("\r\n")) line = line.Substring(0, line.Length - 2);
            logQueue.Enqueue(line + "\r\n");
        }

        private void AppendTextSafe(string text) {
            if (logBox.TextLength + text.Length > 500_000) {
                // 防止爆炸：保留后半部分
                logBox.Text = logBox.Text.Substring(200_000);
            }

            logBox.AppendText(text);
            logBox.SelectionStart = logBox.TextLength;
            logBox.ScrollToCaret();
        }

        private void uiTimer_Tick(object sender, EventArgs e) {
            if (logQueue.IsEmpty) return;

            // 如果队列超过 100 条，丢弃较旧的条目，只保留最后的 100 条
            const int maxBatch = 100;
            int currentCount = logQueue.Count;
            if (currentCount > maxBatch) {
                int toDrop = currentCount - maxBatch;
                // 丢弃较旧的条目
                for (int i = 0; i < toDrop; i++) {
                    if (!logQueue.TryDequeue(out _)) break;
                }
            }

            // 每次最多刷新 100 条
            var sb = new System.Text.StringBuilder();
            int count = 0;
            while (count < maxBatch && logQueue.TryDequeue(out string line)) {
                sb.Append(line);
                count++;
            }

            if (sb.Length > 0) {
                // 线程安全：InvokeRequired
                if (logBox.InvokeRequired) {
                    logBox.BeginInvoke(new Action(() => {
                        AppendTextSafe(sb.ToString());
                    }));
                } else {
                    AppendTextSafe(sb.ToString());
                }
            }
        }

        private void FrmConsole_FormClosed(object sender, FormClosedEventArgs e) {
            uiTimer.Stop();
            uiTimer.Dispose();
        }
    }
}
