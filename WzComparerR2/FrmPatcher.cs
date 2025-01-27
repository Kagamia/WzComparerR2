using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevComponents.AdvTree;
using DevComponents.DotNetBar;
using DevComponents.Editors;
using WzComparerR2.Comparer;
using WzComparerR2.Config;
using WzComparerR2.Patcher;
using WzComparerR2.WzLib;

namespace WzComparerR2
{
    public partial class FrmPatcher : DevComponents.DotNetBar.Office2007Form
    {
        public FrmPatcher()
        {
            InitializeComponent();
#if NET6_0_OR_GREATER
            // https://learn.microsoft.com/en-us/dotnet/core/compatibility/fx-core#controldefaultfont-changed-to-segoe-ui-9pt
            this.Font = new Font(new FontFamily("Microsoft Sans Serif"), 8f);
#endif
            panelEx1.AutoScroll = true;

            var settings = WcR2Config.Default.PatcherSettings;
            if (settings.Count <= 0)
            {
                settings.Add(new PatcherSetting("KMST", "http://maplestory.dn.nexoncdn.co.kr/PatchT/{1:d5}/{0:d5}to{1:d5}.patch", 2));
                settings.Add(new PatcherSetting("KMST-Minor", "http://maplestory.dn.nexoncdn.co.kr/PatchT/{0:d5}/Minor/{1:d2}to{2:d2}.patch", 3));
                settings.Add(new PatcherSetting("KMS", "http://maplestory.dn.nexoncdn.co.kr/Patch/{1:d5}/{0:d5}to{1:d5}.patch", 2));
                settings.Add(new PatcherSetting("KMS-Minor", "http://maplestory.dn.nexoncdn.co.kr/Patch/{0:d5}/Minor/{1:d2}to{2:d2}.patch", 3));
                settings.Add(new PatcherSetting("JMS", "http://webdown2.nexon.co.jp/maple/patch/patchdir/{1:d5}/{0:d5}to{1:d5}.patch", 2));
                settings.Add(new PatcherSetting("GMS", "http://download2.nexon.net/Game/MapleStory/patch/patchdir/{1:d5}/CustomPatch{0}to{1}.exe", 2));
                settings.Add(new PatcherSetting("TMS", "http://tw.cdnpatch.maplestory.beanfun.com/maplestory/patch/patchdir/{1:d5}/{0:d5}to{1:d5}.patch", 2));
                settings.Add(new PatcherSetting("MSEA", "http://patch.maplesea.com/sea/patch/patchdir/{1:d5}/{0:d5}to{1:d5}.patch", 2));
                settings.Add(new PatcherSetting("CMS", "http://mxd.clientdown.sdo.com/maplestory/patch/patchdir/{1:d5}/{0:d5}to{1:d5}.patch", 2));
            }

            foreach (PatcherSetting p in settings)
            {
                this.MigrateSetting(p);
                comboBoxEx1.Items.Add(p);
            }
            if (comboBoxEx1.Items.Count > 0)
                comboBoxEx1.SelectedIndex = 0;

            foreach (WzPngComparison comp in Enum.GetValues(typeof(WzPngComparison)))
            {
                cmbComparePng.Items.Add(comp);
            }
            cmbComparePng.SelectedItem = WzPngComparison.SizeAndDataLength;
        }

        public Encoding PatcherNoticeEncoding { get; set; }

        private bool isUpdating;
        private PatcherSession patcherSession;

        private PatcherSetting SelectedPatcherSetting => comboBoxEx1.SelectedItem as PatcherSetting;

        private void MigrateSetting(PatcherSetting patcherSetting)
        {
            if (patcherSetting.MaxVersion == 0 && patcherSetting.Versions == null)
            {
                patcherSetting.MaxVersion = 2;
                patcherSetting.Versions = new[] { patcherSetting.Version0 ?? 0, patcherSetting.Version1 ?? 0 };
                patcherSetting.Version0 = null;
                patcherSetting.Version1 = null;
            }
            if (patcherSetting.Versions != null && patcherSetting.Versions.Length < patcherSetting.MaxVersion)
            {
                var newVersions = new int[patcherSetting.MaxVersion];
                Array.Copy(patcherSetting.Versions, newVersions, patcherSetting.Versions.Length);
                patcherSetting.Versions = newVersions;
            }
        }

        private void ApplySetting(PatcherSetting p)
        {
            if (isUpdating)
            {
                return;
            }
            isUpdating = true;
            try
            {
                if (this.flowLayoutPanel1.Controls.Count < p.MaxVersion)
                {
                    var inputTemplate = this.integerInput1;
                    var preAddedControls = Enumerable.Range(0, p.MaxVersion - this.flowLayoutPanel1.Controls.Count)
                        .Select(_ =>
                        {
                            var input = new IntegerInput()
                            {
                                AllowEmptyState = inputTemplate.AllowEmptyState,
                                Size = inputTemplate.Size,
                                Value = 0,
                                MinValue = inputTemplate.MinValue,
                                MaxValue = inputTemplate.MaxValue,
                                DisplayFormat = inputTemplate.DisplayFormat,
                                ShowUpDown = inputTemplate.ShowUpDown,
                            };
                            input.BackgroundStyle.ApplyStyle(inputTemplate.BackgroundStyle);
                            input.ValueChanged += this.integerInput_ValueChanged;
                            return input;
                        }).ToArray();
                    this.flowLayoutPanel1.Controls.AddRange(preAddedControls);
                }
                for (int i = 0; i < this.flowLayoutPanel1.Controls.Count; i++)
                {
                    var input = (IntegerInput)this.flowLayoutPanel1.Controls[i];
                    if (i < p.MaxVersion)
                    {
                        input.Show();
                        input.Value = (p.Versions != null && i < p.Versions.Length) ? p.Versions[i] : 0;
                    }
                    else
                    {
                        input.Hide();
                        input.Value = 0;
                    }
                }
                this.txtUrl.Text = p.Url;
            }
            finally
            {
                isUpdating = false;
            }
        }

        private void combineUrl()
        {
            if (this.SelectedPatcherSetting is var p)
            {
                txtUrl.Text = p.Url;
            }
        }

        private void comboBoxEx1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.SelectedPatcherSetting is var p)
            {
                this.ApplySetting(p);
            }
        }

        private void integerInput_ValueChanged(object sender, EventArgs e)
        {
            if (this.SelectedPatcherSetting is var p && sender is IntegerInput input)
            {
                var i = this.flowLayoutPanel1.Controls.IndexOf(input);
                if (i > -1 && i < p.MaxVersion)
                {
                    if (p.Versions == null)
                    {
                        p.Versions = new int[p.MaxVersion];
                    }
                    p.Versions[i] = input.Value;
                }
                this.ApplySetting(p);
            }
        }

        private void buttonXCheck_Click(object sender, EventArgs e)
        {
            DownloadingItem item = new DownloadingItem(txtUrl.Text, null);
            try
            {
                item.GetFileLength();
                if (item.FileLength > 0)
                {
                    switch (MessageBoxEx.Show(string.Format("文件大小：{0:N0} bytes, 更新时间：{1:yyyy-MM-dd HH:mm:ss}\r\n是否立即开始下载文件？", item.FileLength, item.LastModified), "Patcher", MessageBoxButtons.YesNo))
                    {
                        case DialogResult.Yes:
#if NET6_0_OR_GREATER
                            Process.Start(new ProcessStartInfo
                            {
                                UseShellExecute = true,
                                FileName = txtUrl.Text,
                            });
#else
                            Process.Start(txtUrl.Text);
#endif
                            return;

                        case DialogResult.No:
                            return;
                    }
                }
                else
                {
                    MessageBoxEx.Show("文件不存在");
                }
            }
            catch (Exception ex)
            {
                MessageBoxEx.Show("出现错误：" + ex.Message);
            }
        }

        private void FrmPatcher_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (this.patcherSession != null && !this.patcherSession.IsCompleted)
            {
                this.patcherSession.Cancel();
            }
            ConfigManager.Reload();
            WcR2Config.Default.PatcherSettings.Clear();
            foreach (PatcherSetting item in comboBoxEx1.Items)
            {
                WcR2Config.Default.PatcherSettings.Add(item);
            }
            ConfigManager.Save();
        }

        private void NewFile(BinaryReader reader, string fileName, string patchDir)
        {
            string tmpFile = Path.Combine(patchDir, fileName);
            string dir = Path.GetDirectoryName(tmpFile);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private void buttonXOpen1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "请选择补丁文件路径";
            dlg.Filter = "*.patch;*.exe|*.patch;*.exe";
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                txtPatchFile.Text = dlg.FileName;
            }
        }

        private void buttonXOpen2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = "请选择冒险岛文件夹路径";
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                txtMSFolder.Text = dlg.SelectedPath;
            }
        }

        private void buttonXPatch_Click(object sender, EventArgs e)
        {
            if (this.patcherSession != null)
            {
                if (this.patcherSession.State == PatcherTaskState.WaitForContinue)
                {
                    this.patcherSession.Continue();
                    return;
                }
                else if (!this.patcherSession.PatchExecTask.IsCompleted)
                {
                    MessageBoxEx.Show("已经开始了一个补丁进程...");
                    return;
                }
            }
            string compareFolder = null;
            if (chkCompare.Checked)
            {
                FolderBrowserDialog dlg = new FolderBrowserDialog();
                dlg.Description = "请选择对比报告输出文件夹";
                if (dlg.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
                compareFolder = dlg.SelectedPath;
            }

            var session = new PatcherSession()
            {
                PatchFile = txtPatchFile.Text,
                MSFolder = txtMSFolder.Text,
                PrePatch = chkPrePatch.Checked,
                DeadPatch = chkDeadPatch.Checked,
            };
            session.LoggingFileName = Path.Combine(session.MSFolder, $"wcpatcher_{DateTime.Now:yyyyMMdd_HHmmssfff}.log");
            session.PatchExecTask = Task.Run(() => this.ExecutePatchAsync(session, session.CancellationToken));
            this.patcherSession = session;
        }

        private async Task ExecutePatchAsync(PatcherSession session, CancellationToken cancellationToken)
        {
            void AppendStateText(string text)
            {
                this.Invoke(new Action<string>(t => this.txtPatchState.AppendText(t)), text);
                if (session.LoggingFileName != null)
                {
                    File.AppendAllText(session.LoggingFileName, text, Encoding.UTF8);
                }
            }

            this.Invoke(() =>
            {
                this.advTreePatchFiles.Nodes.Clear();
                this.txtNotice.Clear();
                this.txtPatchState.Clear();
                this.panelEx2.Visible = true;
                this.expandablePanel2.Height = 340;
            });

            WzPatcher patcher = null;
            session.State = PatcherTaskState.Prepatch;

            try
            {
                patcher = new WzPatcher(session.PatchFile);
                patcher.NoticeEncoding = this.PatcherNoticeEncoding ?? Encoding.Default;
                patcher.PatchingStateChanged += (o, e) => this.patcher_PatchingStateChanged(o, e, session, AppendStateText);
                AppendStateText($"补丁文件：{session.PatchFile}\r\n");
                AppendStateText("正在检查补丁...");
                patcher.OpenDecompress(cancellationToken);
                AppendStateText("成功\r\n");
                if (session.PrePatch)
                {
                    AppendStateText("正在预读补丁...\r\n");
                    long decompressedSize = patcher.PrePatch(cancellationToken);
                    if (patcher.IsKMST1125Format.Value)
                    {
                        AppendStateText("补丁类型：KMST1125\r\n");
                        if (patcher.OldFileHash != null)
                        {
                            AppendStateText($"获取原文件信息：{patcher.OldFileHash.Count} 个\r\n");
                        }
                    }
                    AppendStateText(string.Format("补丁大小: {0:N0} bytes...\r\n", decompressedSize));
                    AppendStateText(string.Format("文件变动: {0} 个...\r\n", patcher.PatchParts.Count));

                    this.Invoke(() =>
                    {
                        this.advTreePatchFiles.BeginUpdate();
                        this.txtNotice.Text = patcher.NoticeText;
                        foreach (PatchPartContext part in patcher.PatchParts)
                        {
                            this.advTreePatchFiles.Nodes.Add(CreateFileNode(part));
                        }
                        this.advTreePatchFiles.Enabled = true;
                        this.advTreePatchFiles.EndUpdate();
                    });

                    AppendStateText("等待调整更新顺序...\r\n");
                    session.State = PatcherTaskState.WaitForContinue;
                    await session.WaitForContinueAsync();
                    this.Invoke(() =>
                    {
                        this.advTreePatchFiles.Enabled = false;
                    });
                    session.State = PatcherTaskState.Patching;
                    patcher.PatchParts.Clear();
                    foreach (Node node in this.advTreePatchFiles.Nodes)
                    {
                        if (node.Checked && node.Tag is PatchPartContext part)
                        {
                            patcher.PatchParts.Add(part);
                        }
                    }
                    if (patcher.IsKMST1125Format.Value && session.DeadPatch)
                    {
                        AppendStateText("生成deadPatch执行计划：\r\n");
                        session.deadPatchExecutionPlan = new();
                        session.deadPatchExecutionPlan.Build(patcher.PatchParts);
                        foreach (var part in patcher.PatchParts)
                        {
                            if (session.deadPatchExecutionPlan.Check(part.FileName, out var filesCanInstantUpdate))
                            {
                                AppendStateText($"+ 执行文件{part.FileName}\r\n");
                                foreach (var fileName in filesCanInstantUpdate)
                                {
                                    AppendStateText($"  - 应用文件{fileName}\r\n");
                                }
                            }
                            else
                            {
                                AppendStateText($"- 执行文件{part.FileName}，但延迟应用\r\n");
                            }
                        }
                        // disable force validation
                        patcher.ThrowOnValidationFailed = false;
                    }
                }
                AppendStateText("开始更新\r\n");
                var sw = Stopwatch.StartNew();
                patcher.Patch(session.MSFolder, cancellationToken);
                sw.Stop();
                AppendStateText("完成\r\n");
                session.State = PatcherTaskState.Complete;
                MessageBoxEx.Show(this, "补丁结束，用时" + sw.Elapsed, "Patcher");
            }
            catch (OperationCanceledException)
            {
                MessageBoxEx.Show(this.Owner, "补丁中止。", "Patcher");
            }
            catch (UnauthorizedAccessException ex)
            {
                // File IO permission error
                MessageBoxEx.Show(this, ex.ToString(), "Patcher");
            }
            catch (Exception ex)
            {
                AppendStateText(ex.ToString());
                MessageBoxEx.Show(this, ex.ToString(), "Patcher");
            }
            finally
            {
                session.State = PatcherTaskState.Complete;
                if (patcher != null)
                {
                    patcher.Close();
                    patcher = null;
                }
                GC.Collect();
                panelEx2.Visible = false;
                expandablePanel2.Height = 157;
            }
        }

        private void patcher_PatchingStateChanged(object sender, PatchingEventArgs e, PatcherSession session, Action<string> logFunc)
        {
            switch (e.State)
            {
                case PatchingState.PatchStart:
                    logFunc("开始更新" + e.Part.FileName + "\r\n");
                    break;
                case PatchingState.VerifyOldChecksumBegin:
                    logFunc("  检查旧文件checksum...");
                    break;
                case PatchingState.VerifyOldChecksumEnd:
                    logFunc("  结束\r\n");
                    break;
                case PatchingState.VerifyNewChecksumBegin:
                    logFunc("  检查新文件checksum...");
                    break;
                case PatchingState.VerifyNewChecksumEnd:
                    logFunc("  结束\r\n");
                    break;
                case PatchingState.TempFileCreated:
                    logFunc("  创建临时文件...\r\n");
                    progressBarX1.Maximum = e.Part.NewFileLength;
                    session.TemporaryFileMapping.Add(e.Part.FileName, e.Part.TempFilePath);
                    break;
                case PatchingState.TempFileBuildProcessChanged:
                    progressBarX1.Value = (int)e.CurrentFileLength;
                    progressBarX1.Text = string.Format("{0:N0}/{1:N0}", e.CurrentFileLength, e.Part.NewFileLength);
                    break;
                case PatchingState.TempFileClosed:
                    logFunc("  关闭临时文件...\r\n");
                    progressBarX1.Value = 0;
                    progressBarX1.Maximum = 0;
                    progressBarX1.Text = string.Empty;

                    if (!string.IsNullOrEmpty(session.CompareFolder)
                        && e.Part.Type == 1
                        && Path.GetExtension(e.Part.FileName).Equals(".wz", StringComparison.OrdinalIgnoreCase)
                        && !Path.GetFileName(e.Part.FileName).Equals("list.wz", StringComparison.OrdinalIgnoreCase))
                    {
                        Wz_Structure wznew = new Wz_Structure();
                        Wz_Structure wzold = new Wz_Structure();
                        try
                        {
                            logFunc("  (comparer)正在对比文件...\r\n");
                            EasyComparer comparer = new EasyComparer();
                            comparer.OutputPng = chkOutputPng.Checked;
                            comparer.OutputAddedImg = chkOutputAddedImg.Checked;
                            comparer.OutputRemovedImg = chkOutputRemovedImg.Checked;
                            comparer.EnableDarkMode = chkEnableDarkMode.Checked;
                            comparer.Comparer.PngComparison = (WzPngComparison)cmbComparePng.SelectedItem;
                            comparer.Comparer.ResolvePngLink = chkResolvePngLink.Checked;
                            wznew.Load(e.Part.TempFilePath, false);
                            wzold.Load(e.Part.OldFilePath, false);
                            comparer.EasyCompareWzFiles(wznew.wz_files[0], wzold.wz_files[0], session.CompareFolder);
                        }
                        catch (Exception ex)
                        {
                            txtPatchState.AppendText(ex.ToString());
                        }
                        finally
                        {
                            wznew.Clear();
                            wzold.Clear();
                            GC.Collect();
                        }
                    }

                    if (session.DeadPatch && e.Part.Type == 1 && sender is WzPatcher patcher)
                    {
                        if (patcher.IsKMST1125Format.Value)
                        {
                            if (session.deadPatchExecutionPlan?.Check(e.Part.FileName, out var filesCanInstantUpdate) ?? false)
                            {
                                foreach (string fileName in filesCanInstantUpdate)
                                {
                                    if (session.TemporaryFileMapping.TryGetValue(fileName, out var temporaryFileName))
                                    {
                                        logFunc($"  (deadpatch)正在应用文件{fileName}...\r\n");
                                        patcher.SafeMove(temporaryFileName, Path.Combine(session.MSFolder, fileName));
                                    }
                                }
                            }
                            else
                            {
                                logFunc("  (deadpatch)延迟应用文件...\r\n");
                            }
                        }
                        else
                        {
                            logFunc("  (deadpatch)正在应用文件...\r\n");
                            patcher.SafeMove(e.Part.TempFilePath, e.Part.OldFilePath);
                        }
                    }
                    break;
                case PatchingState.PrepareVerifyOldChecksumBegin:
                    logFunc($"预检查旧文件checksum: {e.Part.FileName}");
                    break;
                case PatchingState.PrepareVerifyOldChecksumEnd:
                    if (e.Part.OldChecksum != e.Part.OldChecksumActual)
                    {
                        logFunc(" 不一致\r\n");
                    }
                    else
                    {
                        logFunc(" 结束\r\n");
                    }
                    break;
                case PatchingState.ApplyFile:
                    logFunc($"应用文件: {e.Part.FileName}\r\n");
                    break;
                case PatchingState.FileSkipped:
                    logFunc("  跳过" + e.Part.FileName + "\r\n");
                    break;
            }
        }

        private Node CreateFileNode(PatchPartContext part)
        {
            Node node = new Node(part.FileName) { CheckBoxVisible = true, Checked = true };
            ElementStyle style = new ElementStyle();
            style.TextAlignment = eStyleTextAlignment.Far;
            node.Cells.Add(new Cell(part.Type.ToString(), style));
            node.Cells.Add(new Cell(part.NewFileLength.ToString("n0"), style));
            node.Cells.Add(new Cell(part.NewChecksum.ToString("x8"), style));
            node.Cells.Add(new Cell(part.OldChecksum?.ToString("x8"), style));
            if (part.Type == 1)
            {
                string text = string.Format("{0}|{1}|{2}|{3}", part.Action0, part.Action1, part.Action2, part.DependencyFiles.Count);
                node.Cells.Add(new Cell(text, style));
            }
            node.Tag = part;
            return node;
        }

        private void buttonXOpen3_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "请选择补丁文件路径";
            dlg.Filter = "*.patch;*.exe|*.patch;*.exe";
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                txtPatchFile2.Text = dlg.FileName;
            }
        }

        private void buttonXOpen4_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.Description = "请选择冒险岛文件夹路径";
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                txtMSFolder2.Text = dlg.SelectedPath;
            }
        }

        private void buttonXCreate_Click(object sender, EventArgs e)
        {
            MessageBoxEx.Show(@"> 这是一个测试功能...
> 还没完成 所以请选择patch文件  exe补丁暂时懒得分离
> 没有检查原客户端版本 为了正确执行请预先确认
> 暂时不提供文件块的筛选或文件缺失提示
> 没优化 于是可能生成文件体积较大 但是几乎可以保证完整性
> 对于KMST1125后无法正常工作", "声明");

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "*.patch|*.patch";
            dlg.Title = "选择输出文件";
            dlg.CheckFileExists = false;
            dlg.InitialDirectory = Path.GetDirectoryName(txtPatchFile2.Text);
            dlg.FileName = Path.GetFileNameWithoutExtension(txtPatchFile2.Text) + "_reverse.patch";

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    ReversePatcherBuilder builder = new ReversePatcherBuilder();
                    builder.msDir = txtMSFolder2.Text;
                    builder.patchFileName = txtPatchFile2.Text;
                    builder.outputFileName = dlg.FileName;
                    builder.Build();
                }
                catch (Exception ex)
                {
                }
            }
        }

        class PatcherSession
        {
            public PatcherSession()
            {
                this.cancellationTokenSource = new CancellationTokenSource();
            }

            public string PatchFile;
            public string MSFolder;
            public string CompareFolder;
            public bool PrePatch;
            public bool DeadPatch;

            public Task PatchExecTask;
            public string LoggingFileName;
            public PatcherTaskState State;

            public DeadPatchExecutionPlan deadPatchExecutionPlan;
            public Dictionary<string, string> TemporaryFileMapping = new ();

            public CancellationToken CancellationToken => this.cancellationTokenSource.Token;
            private CancellationTokenSource cancellationTokenSource;
            private TaskCompletionSource<bool> tcsWaiting;

            public bool IsCompleted => this.PatchExecTask?.IsCompleted ?? true;

            public void Cancel()
            {
                this.cancellationTokenSource.Cancel();
            }

            public async Task WaitForContinueAsync()
            {
                var tcs = new TaskCompletionSource<bool>();
                this.tcsWaiting = tcs;
                this.cancellationTokenSource.Token.Register(() => tcs.TrySetCanceled());
                await tcs.Task;
            }

            public void Continue()
            {
                if (this.tcsWaiting != null)
                {
                    this.tcsWaiting.SetResult(true);
                }
            }
        }

        enum PatcherTaskState
        {
            NotStarted = 0,
            Prepatch = 1,
            WaitForContinue = 2,
            Patching = 3,
            Complete = 4,
        }

        class DeadPatchExecutionPlan
        {
            public DeadPatchExecutionPlan()
            {
                this.FileUpdateDependencies = new Dictionary<string, List<string>>();
            }

            public Dictionary<string, List<string>> FileUpdateDependencies { get; private set; }

            public void Build(IEnumerable<PatchPartContext> orderedParts)
            {
                /*
                 *  for examle:
                 *    fileName   | type | dependencies               
                 *    -----------|------|---------------     
                 *    Mob_000.wz | 1    | Mob_000.wz   (self update)
                 *    Mob_001.wz | 1    | Mob_001.wz, Mob_002.wz  (merge data)
                 *    Mob_002.wz | 1    | Mob_001.wz, Mob_002.wz  (merge data)
                 *    Mob_003.wz | 1    | Mob_001.wz, Mob_002.wz  (balance size from other file)
                 *                                                 
                 *  fileLastDependecy:                             
                 *    key        | value                           
                 *    -----------|----------------                 
                 *    Mob_000.wz | Mob_000.wz
                 *    Mob_001.wz | Mob_003.wz
                 *    Mob_002.wz | Mob_003.wz
                 *    Mob_003.wz | Mob_003.wz
                 *    
                 *  FileUpdateDependencies:
                 *    key        | value
                 *    -----------|----------------
                 *    Mob_000.wz | Mob000.wz
                 *    Mob_003.wz | Mob001.wz, Mob002.wz, Mob003.wz
                 */

                // find the last dependency
                Dictionary<string, string> fileLastDependecy = new();
                foreach (var part in orderedParts)
                {
                    if (part.Type == 0)
                    {
                        fileLastDependecy[part.FileName] = part.FileName;
                    }
                    else if (part.Type == 1)
                    {
                        fileLastDependecy[part.FileName] = part.FileName;
                        foreach (var dep in part.DependencyFiles)
                        {
                            fileLastDependecy[dep] = part.FileName;
                        }
                    }
                }

                // reverse key and value
                this.FileUpdateDependencies.Clear();
                foreach (var grp in fileLastDependecy.GroupBy(kv => kv.Value, kv => kv.Key))
                {
                    this.FileUpdateDependencies.Add(grp.Key, grp.ToList());
                }
            }

            public bool Check(string fileName, out IReadOnlyList<string> filesCanInstantUpdate)
            {
                if (this.FileUpdateDependencies.TryGetValue(fileName, out var value) && value != null && value.Count > 0)
                {
                    filesCanInstantUpdate = value;
                    return true;
                }

                filesCanInstantUpdate = null;
                return false;
            }
        }
    }
}