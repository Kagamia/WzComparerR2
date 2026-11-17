using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;

namespace WzComparerR2
{
    internal sealed class WzQueryControl : UserControl
    {
        private readonly SplitContainer querySplit;
        private readonly FlowLayoutPanel header;
        private readonly FlowLayoutPanel footer;
        private readonly TableLayoutPanel resultPanel;
        private readonly TreeView queryTree;
        private readonly Panel editor;
        private readonly DataGridView resultGrid;
        private readonly ProgressBar progressBar;
        private readonly Label lblStatus;
        private readonly Label lblPage;
        private readonly Label lblEditorType;
        private readonly Label lblValuePath;
        private readonly Button btnAddTarget;
        private readonly Button btnAddExclude;
        private readonly Button btnAddValue;
        private readonly Button btnRemove;
        private readonly Button btnReset;
        private readonly Button btnRun;
        private readonly Button btnCancel;
        private readonly Button btnPreviousPage;
        private readonly Button btnNextPage;
        private readonly Button btnExportTxt;
        private readonly Button btnExportJson;
        private readonly Button btnExportXml;
        private readonly ComboBox cmbRoot;
        private readonly ComboBox cmbTextComparison;
        private readonly ComboBox cmbTargetOutput;
        private readonly ComboBox cmbValueType;
        private readonly ComboBox cmbValueComparison;
        private readonly ComboBox cmbValueOutput;
        private readonly CheckBox chkSearchDescendants;
        private readonly CheckBox chkValueRequired;
        private readonly TextBox txtTextPattern;
        private readonly TextBox txtValuePath;
        private readonly TextBox txtValuePattern;
        private readonly ToolTip globalToolTip;
        private Form resizeForm;

        private CancellationTokenSource cancellation;

        private readonly Action<string> navigateToPath;

        private List<QueryResult> allResults;
        private int currentPage;
        private const int PageSize = 3000;
        private const int MaxResultCount = 300000;

        private bool updatingEditor;
        private bool updatingLayout;

        public WzQueryControl(Action<string> navigateToPath)
        {
            this.navigateToPath = navigateToPath;
            this.globalToolTip = new ToolTip { AutoPopDelay = 60000, InitialDelay = 500, ReshowDelay = 100 };

            this.header = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, Padding = new Padding(4), WrapContents = false };
            this.cmbRoot = CreateCombo(100, Enum.GetValues(typeof(Wz_Type)).Cast<Wz_Type>().Where(type => type != Wz_Type.Unknown).Select(type => type.ToString()).ToArray());
            this.btnAddTarget = new Button { Text = "Add Target Node", Width = 110, Height = 24 };
            this.btnAddExclude = new Button { Text = "Add Exclude Node", Width = 110, Height = 24 };
            this.btnAddValue = new Button { Text = "Add Value Condition", Width = 115, Height = 24 };
            this.btnRemove = new Button { Text = "Delete", Width = 50, Height = 24 };
            this.btnReset = new Button { Text = "Reset", Width = 60, Height = 24 };
            this.btnRun = new Button { Text = "Run", Width = 50, Height = 24 };
            this.btnCancel = new Button { Text = "Cancel", Width = 50, Height = 24, Enabled = false };
            this.header.Controls.AddRange(new Control[]
            {
                new Label
                {
                    Text = "Top", AutoSize = true, Padding = new Padding(0, 7, 0, 0)
                },
                this.cmbRoot,
                this.btnAddTarget,
                this.btnAddExclude,
                this.btnAddValue,
                this.btnRemove,
                this.btnReset,
                this.btnRun,
                this.btnCancel
            });

            this.querySplit = new SplitContainer { Dock = DockStyle.Top, Height = 150, FixedPanel = FixedPanel.Panel1 };

            this.queryTree = new TreeView { Dock = DockStyle.Fill, HideSelection = false, ShowPlusMinus = false, ShowRootLines = true, ShowLines = true };
            this.querySplit.Panel1.Controls.Add(this.queryTree);

            this.editor = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            this.querySplit.Panel2.Controls.Add(this.editor);

            this.lblEditorType = new Label { Left = 6, Top = 7, Width = 72 };
            this.cmbTextComparison = CreateCombo(72, "Starts", "Contains", "Ends", "Equals", "Regex");
            this.cmbTextComparison.SetBounds(82, 3, 72, 24);
            this.txtTextPattern = new TextBox { Left = 160, Top = 4, Width = 150 };
            this.cmbTargetOutput = CreateCombo(72, "Output X", "Output O");
            this.cmbTargetOutput.SetBounds(82, 33, 72, 24);
            this.chkSearchDescendants = new CheckBox { Text = "Search All Sub Nodes", AutoSize = true, Left = 160, Top = 36 };
            this.globalToolTip.SetToolTip(this.chkSearchDescendants, "When checked, the selected node and all of its sub nodes are included in the search.");
            this.lblValuePath = new Label { Text = "Value Path", Left = 6, Top = 7, Width = 72 };
            this.txtValuePath = new TextBox { Left = 82, Top = 4, Width = 180 };
            this.cmbValueType = CreateCombo(65, "Numeric", "String", "Vector");
            this.cmbValueType.SetBounds(82, 33, 65, 24);
            this.cmbValueComparison = CreateCombo(72, "Starts", "Contains", "Ends", "Equals", "Regex");
            this.cmbValueComparison.SetBounds(153, 33, 72, 24);
            this.txtValuePattern = new TextBox { Left = 231, Top = 34, Width = 150 };
            this.cmbValueOutput = CreateCombo(72, "Output X", "Output O");
            this.cmbValueOutput.SelectedIndex = 1;
            this.cmbValueOutput.SetBounds(82, 63, 72, 24);
            this.chkValueRequired = new CheckBox { Text = "Required", AutoSize = true, Left = 160, Top = 66 };
            this.globalToolTip.SetToolTip(this.chkValueRequired, "When checked, this condition is required.\r\nOnly nodes that satisfy all required conditions pass.\r\nIf optional conditions exist, nodes must also satisfy at least one of them.");
            this.lblStatus = new Label { AutoSize = false, AutoEllipsis = true, Left = 82, Top = 93, Height = 18, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            this.progressBar = new ProgressBar { Left = 82, Top = 116, Width = 200, Height = 18, Minimum = 0, Maximum = 1, Visible = false };
            this.editor.Controls.AddRange(new Control[]
            {
                this.lblEditorType,
                this.cmbTextComparison,
                this.txtTextPattern,
                this.cmbTargetOutput,
                this.chkSearchDescendants,
                this.lblValuePath,
                this.txtValuePath,
                this.cmbValueType,
                this.cmbValueComparison,
                this.txtValuePattern,
                this.cmbValueOutput,
                this.chkValueRequired,
                this.lblStatus,
                this.progressBar
            });

            this.resultPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = Padding.Empty, Padding = Padding.Empty };
            this.resultPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.resultPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.resultPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));

            this.footer = new FlowLayoutPanel { Dock = DockStyle.Fill, Margin = Padding.Empty, Padding = new Padding(4), WrapContents = false };
            this.btnPreviousPage = new Button { Text = "Prev", Width = 60, Height = 24, Enabled = false };
            this.btnNextPage = new Button { Text = "Next", Width = 60, Height = 24, Enabled = false };
            this.lblPage = new Label { AutoSize = true, Padding = new Padding(8, 9, 8, 0), Text = "0 / 0" };
            this.btnExportTxt = new Button { Text = "txt", Width = 60, Height = 24 };
            this.btnExportTxt.Margin = new Padding(20, this.btnExportTxt.Margin.Top, this.btnExportTxt.Margin.Right, this.btnExportTxt.Margin.Bottom);
            this.btnExportJson = new Button { Text = "json", Width = 60, Height = 24 };
            this.btnExportXml = new Button { Text = "xml", Width = 60, Height = 24 };
            this.footer.Controls.AddRange(new Control[] { this.btnPreviousPage, this.lblPage, this.btnNextPage, this.btnExportTxt, this.btnExportJson, this.btnExportXml });

            this.resultGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            this.resultGrid.Columns.Add("Path", "Path");
            this.resultGrid.Columns.Add("Result", "Result");
            this.resultPanel.Controls.Add(this.resultGrid, 0, 0);
            this.resultPanel.Controls.Add(this.footer, 0, 1);

            this.Controls.Add(this.resultPanel);
            this.Controls.Add(this.querySplit);
            this.Controls.Add(this.header);

            this.queryTree.BeforeCollapse += (sender, e) => e.Cancel = true;
            this.queryTree.AfterSelect += (sender, e) => this.LoadEditor();
            this.queryTree.KeyDown += (sender, e) =>
            {
                if (e.KeyCode == Keys.Delete)
                {
                    this.RemoveSelected();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
            this.resultGrid.CellDoubleClick += this.ResultGrid_CellDoubleClick;
            this.btnPreviousPage.Click += (sender, e) => this.ChangePage(-1);
            this.btnNextPage.Click += (sender, e) => this.ChangePage(1);
            this.Load += (sender, e) =>
            {
                this.UpdateTreeWidth();
                this.AttachResizeEvents();
            };
            this.btnAddTarget.Click += (sender, e) => this.AddRule(QueryRuleKind.Target);
            this.btnAddExclude.Click += (sender, e) => this.AddRule(QueryRuleKind.Exclude);
            this.btnAddValue.Click += (sender, e) => this.AddValueRule();
            this.btnRemove.Click += (sender, e) => this.RemoveSelected();
            this.btnRun.Click += async (sender, e) => await this.RunQueryAsync();
            this.btnCancel.Click += (sender, e) => this.cancellation?.Cancel();
            this.btnReset.Click += (sender, e) => this.ResetTree();
            this.btnExportTxt.Click += (sender, e) => this.Export("txt");
            this.btnExportJson.Click += (sender, e) => this.Export("json");
            this.btnExportXml.Click += (sender, e) => this.Export("xml");
            this.chkSearchDescendants.CheckedChanged += (sender, e) => this.SaveEditor();
            this.chkValueRequired.CheckedChanged += (sender, e) => this.SaveEditor();
            this.cmbValueType.SelectedIndexChanged += (sender, e) =>
            {
                if (!this.updatingEditor)
                {
                    this.UpdateValueComparisons();
                }
            };

            this.allResults = new List<QueryResult>();
            foreach (Control control in this.editor.Controls)
            {
                if (control is ComboBox combo)
                {
                    combo.SelectedIndexChanged += (sender, e) => this.SaveEditor();
                }
                else if (control is TextBox text)
                {
                    text.TextChanged += (sender, e) => this.SaveEditor();
                }
            }
            this.ResetTree();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hwnd, UInt32 wMsg, IntPtr wParam, IntPtr lParam);
        private const int WM_SETREDRAW = 0xB;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.globalToolTip.Dispose();
                if (this.resizeForm != null)
                {
                    this.resizeForm.ResizeBegin -= this.ResizeForm_ResizeBegin;
                    this.resizeForm.ResizeEnd -= this.ResizeForm_ResizeEnd;
                    this.resizeForm = null;
                }
            }
            base.Dispose(disposing);
        }

        private void AttachResizeEvents()
        {
            Form form = this.FindForm();
            if (form == null || form == this.resizeForm)
            {
                return;
            }
            if (this.resizeForm != null)
            {
                this.resizeForm.ResizeBegin -= this.ResizeForm_ResizeBegin;
                this.resizeForm.ResizeEnd -= this.ResizeForm_ResizeEnd;
            }
            this.resizeForm = form;
            this.resizeForm.ResizeBegin += this.ResizeForm_ResizeBegin;
            this.resizeForm.ResizeEnd += this.ResizeForm_ResizeEnd;
        }

        private void ResizeForm_ResizeBegin(object sender, EventArgs e)
        {
            if (this.updatingLayout)
            {
                return;
            }
            this.updatingLayout = true;
            SetRedraw(this, false);
        }

        private void ResizeForm_ResizeEnd(object sender, EventArgs e)
        {
            if (!this.updatingLayout)
            {
                return;
            }
            this.updatingLayout = false;
            SetRedraw(this, true);
            this.Invalidate(true);
            this.Update();
        }

        private static void SetRedraw(Control control, bool enabled)
        {
            if (enabled && control.IsHandleCreated)
            {
                SendMessage(control.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
            }
            foreach (Control child in control.Controls)
            {
                SetRedraw(child, enabled);
            }
            if (!enabled && control.IsHandleCreated)
            {
                SendMessage(control.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
            }
        }

        private void ResultGrid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }
            string fullPath = Convert.ToString(this.resultGrid.Rows[e.RowIndex].Cells["Path"].Value);
            if (!string.IsNullOrEmpty(fullPath))
            {
                this.navigateToPath?.Invoke(fullPath);
            }
        }

        private void Export(string extension)
        {
            if (this.allResults.Count == 0)
            {
                MessageBox.Show("There are no query results to export.", "Wz Query", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.AddExtension = true;
                dialog.DefaultExt = extension;
                dialog.FileName = $"WzQueryResults.{extension}";
                dialog.Filter = $"{extension.ToUpperInvariant()} Files (*.{extension})|*.{extension}|All Files (*.*)|*.*";
                if (dialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                try
                {
                    if (extension == "txt")
                    {
                        this.ExportTxt(dialog.FileName);
                    }
                    else if (extension == "json")
                    {
                        this.ExportJson(dialog.FileName);
                    }
                    else if (extension == "xml")
                    {
                        this.ExportXml(dialog.FileName);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to export results.\r\n{ex.Message}", "Wz Query", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExportTxt(string fileName)
        {
            using (var writer = new StreamWriter(fileName, false, new UTF8Encoding(true)))
            {
                foreach (QueryResult result in this.allResults)
                {
                    writer.Write(result.Path);
                    writer.Write("\t\t\t");
                    writer.WriteLine(result.Value);
                }
            }
        }

        private void ExportJson(string fileName)
        {
            using (var streamWriter = new StreamWriter(fileName, false, new UTF8Encoding(true)))
            using (var jsonWriter = new JsonTextWriter(streamWriter) { Formatting = Newtonsoft.Json.Formatting.Indented })
            {
                jsonWriter.WriteStartArray();
                foreach (QueryResult result in this.allResults)
                {
                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName("Path");
                    jsonWriter.WriteValue(result.Path);
                    jsonWriter.WritePropertyName("Result");
                    jsonWriter.WriteValue(result.Value);
                    jsonWriter.WriteEndObject();
                }
                jsonWriter.WriteEndArray();
            }
        }

        private void ExportXml(string fileName)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(true),
                Indent = true
            };
            using (XmlWriter writer = XmlWriter.Create(fileName, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Results");
                foreach (QueryResult result in this.allResults)
                {
                    writer.WriteStartElement("Item");
                    writer.WriteElementString("Path", result.Path);
                    writer.WriteElementString("Result", result.Value);
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        private void ChangePage(int offset)
        {
            int page = this.currentPage + offset;
            if (page >= 0 && page < this.GetPageCount())
            {
                this.currentPage = page;
                this.ShowResultPage();
            }
        }

        private int GetPageCount()
        {
            return this.allResults.Count == 0 ? 0 : (this.allResults.Count + PageSize - 1) / PageSize;
        }

        private void ShowResultPage()
        {
            this.resultGrid.Rows.Clear();
            int pageCount = this.GetPageCount();
            if (pageCount == 0)
            {
                this.currentPage = 0;
                this.lblPage.Text = "0 / 0";
                this.btnPreviousPage.Enabled = false;
                this.btnNextPage.Enabled = false;
                return;
            }

            int startIndex = this.currentPage * PageSize;
            int endIndex = Math.Min(startIndex + PageSize, this.allResults.Count);
            for (int i = startIndex; i < endIndex; i++)
            {
                QueryResult result = this.allResults[i];
                this.resultGrid.Rows.Add(result.Path, result.Value);
            }

            this.lblPage.Text = $"{this.currentPage + 1} / {pageCount}";
            this.btnPreviousPage.Enabled = this.currentPage > 0;
            this.btnNextPage.Enabled = this.currentPage + 1 < pageCount;
        }

        private void UpdateTreeWidth()
        {
            int maxWidth = this.querySplit.ClientSize.Width - this.querySplit.Panel2MinSize - this.querySplit.SplitterWidth;
            if (maxWidth >= this.querySplit.Panel1MinSize)
            {
                this.querySplit.SplitterDistance = Math.Min(250, maxWidth);
            }
        }

        private void ResetTree()
        {
            this.queryTree.Nodes.Clear();
            var root = new TreeNode($"Top: {this.cmbRoot.Text}")
            {
                Tag = new QueryRoot()
            };
            this.queryTree.Nodes.Add(root);
            root.Nodes.Add(this.CreateRuleNode(new QueryRule(QueryRuleKind.Target)));
            root.ExpandAll();
            this.queryTree.SelectedNode = root.Nodes[0];
            this.cmbRoot.SelectedIndexChanged += (sender, e) => this.queryTree.Nodes[0].Text = $"Top: {this.cmbRoot.Text}";
        }

        private void AddRule(QueryRuleKind kind)
        {
            TreeNode parent = this.GetRuleParent();
            if (parent == null)
            {
                MessageBox.Show("Select a node to add the condition to.", "Wz Query", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            TreeNode node = this.CreateRuleNode(new QueryRule(kind));
            parent.Nodes.Add(node);
            if (parent.Tag is QueryRule parentRule)
            {
                parentRule.Children.Add((QueryRule)node.Tag);
                if (kind == QueryRuleKind.Target)
                {
                    parentRule.SearchDescendants = false;
                    parent.Text = this.GetRuleText(parentRule);
                }
            }
            parent.ExpandAll();
            this.queryTree.SelectedNode = node;
        }

        private TreeNode GetRuleParent()
        {
            TreeNode selected = this.queryTree.SelectedNode;
            if (selected?.Tag is QueryRoot)
            {
                return selected;
            }
            if (selected?.Tag is QueryRule rule && rule.Kind == QueryRuleKind.Target)
            {
                return selected;
            }
            if (selected?.Tag is ValueRule)
            {
                return selected.Parent;
            }
            return null;
        }

        private void AddValueRule()
        {
            TreeNode ruleNode = this.queryTree.SelectedNode;
            if (ruleNode?.Tag is ValueRule)
            {
                ruleNode = ruleNode.Parent;
            }
            if (!(ruleNode?.Tag is QueryRule rule))
            {
                MessageBox.Show("Select a target or exclude node to add a Value condition to.", "Wz Query", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var valueRule = new ValueRule
            {
                Output = rule.Kind == QueryRuleKind.Target
            };
            var node = this.CreateValueRuleNode(valueRule);
            ruleNode.Nodes.Insert(rule.Values.Count, node);
            rule.Values.Add((ValueRule)node.Tag);
            ruleNode.ExpandAll();
            this.queryTree.SelectedNode = node;
        }

        private void RemoveSelected()
        {
            TreeNode selected = this.queryTree.SelectedNode;
            if (selected == null || selected.Tag is QueryRoot)
            {
                return;
            }
            if (selected.Tag is ValueRule valueRule && selected.Parent?.Tag is QueryRule parentRule)
            {
                parentRule.Values.Remove(valueRule);
            }
            else if (selected.Tag is QueryRule queryRule && selected.Parent?.Tag is QueryRule ownerRule)
            {
                ownerRule.Children.Remove(queryRule);
            }
            TreeNode parent = selected.Parent;
            selected.Remove();
            this.queryTree.SelectedNode = parent;
        }

        private TreeNode CreateRuleNode(QueryRule rule)
        {
            return new TreeNode(this.GetRuleText(rule))
            {
                Tag = rule
            };
        }

        private string GetRuleText(QueryRule rule)
        {
            string kind = rule.Kind == QueryRuleKind.Target ? "Target" : "Exclude";
            string cond = string.IsNullOrEmpty(rule.TextPattern) ?
                ": All" :
                $": {GetComparisonText(rule.TextComparison)} \"{rule.TextPattern}\"";
            string searchRange = rule.SearchDescendants ? " (All Sub Nodes)" : string.Empty;
            return kind + cond + searchRange;
        }

        private TreeNode CreateValueRuleNode(ValueRule rule)
        {
            return new TreeNode(this.GetValueRuleText(rule))
            {
                Tag = rule
            };
        }

        private string GetValueRuleText(ValueRule value)
        {
            string cond = string.IsNullOrEmpty(value.Path) ?
                "All" :
                $"{(value.Path.Length == 0 ? "Current Node" : value.Path)} {(string.IsNullOrEmpty(value.Pattern) ? "==" : this.cmbValueComparison.Text)} \"{value.Pattern}\"";
            return (value.Required ? "Value(Required): " : "Value(Optional): ") + cond;
        }

        private void LoadEditor()
        {
            this.updatingEditor = true;
            object tag = this.queryTree.SelectedNode?.Tag;
            bool isRule = tag is QueryRule;
            bool isValue = tag is ValueRule;
            this.lblEditorType.Visible = isRule;
            this.lblValuePath.Visible = isValue;
            this.cmbTextComparison.Visible = isRule;
            this.txtTextPattern.Visible = isRule;
            this.cmbTargetOutput.Visible = isRule;
            this.chkSearchDescendants.Visible = false;
            this.txtValuePath.Visible = isValue;
            this.cmbValueType.Visible = isValue;
            this.cmbValueComparison.Visible = isValue;
            this.txtValuePattern.Visible = isValue;
            this.cmbValueOutput.Visible = isValue;
            this.chkValueRequired.Visible = isValue;
            if (tag is QueryRule rule)
            {
                this.lblEditorType.Text = rule.Kind == QueryRuleKind.Target ? "Target Text" : "Exclude Text";
                this.cmbTextComparison.SelectedIndex = rule.TextComparison;
                this.txtTextPattern.Text = rule.TextPattern;
                this.cmbTargetOutput.SelectedIndex = rule.Output ? 1 : 0;
                this.cmbTargetOutput.Enabled = rule.Kind == QueryRuleKind.Target;
                bool isLeafTarget = rule.Kind == QueryRuleKind.Target && !rule.Children.Any(child => child.Kind == QueryRuleKind.Target);
                this.chkSearchDescendants.Visible = isLeafTarget;
                this.chkSearchDescendants.Checked = isLeafTarget && rule.SearchDescendants;
            }
            else if (tag is ValueRule value)
            {
                bool isExcludeValue = this.queryTree.SelectedNode.Parent?.Tag is QueryRule parentRule && parentRule.Kind == QueryRuleKind.Exclude;
                this.txtValuePath.Text = value.Path;
                this.cmbValueType.SelectedIndex = (int)value.ValueType;
                this.UpdateValueComparisons();
                this.cmbValueComparison.SelectedIndex = value.Comparison;
                this.txtValuePattern.Text = value.Pattern;
                this.cmbValueOutput.SelectedIndex = value.Output ? 1 : 0;
                this.cmbValueOutput.Enabled = !isExcludeValue;
                this.chkValueRequired.Checked = value.Required;
            }
            this.updatingEditor = false;
        }

        private void SaveEditor()
        {
            if (this.updatingEditor || this.queryTree.SelectedNode == null)
            {
                return;
            }
            if (this.queryTree.SelectedNode.Tag is QueryRule rule)
            {
                rule.TextComparison = this.cmbTextComparison.SelectedIndex;
                rule.TextPattern = this.txtTextPattern.Text;
                rule.Output = rule.Kind == QueryRuleKind.Target && this.cmbTargetOutput.SelectedIndex == 1;
                bool isLeafTarget = rule.Kind == QueryRuleKind.Target && !rule.Children.Any(child => child.Kind == QueryRuleKind.Target);
                rule.SearchDescendants = isLeafTarget && this.chkSearchDescendants.Checked;
                this.queryTree.SelectedNode.Text = this.GetRuleText(rule);
            }
            else if (this.queryTree.SelectedNode.Tag is ValueRule value)
            {
                bool isExcludeValue = this.queryTree.SelectedNode.Parent?.Tag is QueryRule parentRule && parentRule.Kind == QueryRuleKind.Exclude;
                value.Path = this.txtValuePath.Text.Trim().Replace('/', '\\');
                value.ValueType = (QueryValueType)this.cmbValueType.SelectedIndex;
                value.Comparison = this.cmbValueComparison.SelectedIndex;
                value.Pattern = this.txtValuePattern.Text;
                value.Output = !isExcludeValue && this.cmbValueOutput.SelectedIndex == 1;
                value.Required = this.chkValueRequired.Checked;
                this.queryTree.SelectedNode.Text = this.GetValueRuleText(value);
            }
        }

        private void UpdateValueComparisons()
        {
            if (this.cmbValueType.SelectedIndex < 0)
            {
                return;
            }
            string[] items;
            switch ((QueryValueType)this.cmbValueType.SelectedIndex)
            {
                case QueryValueType.Number:
                    items = new[] { "==", "!=", "<", ">", "<=", ">=" };
                    break;
                case QueryValueType.Vector:
                    items = new[] { "==", "!=" };
                    break;
                default:
                    items = new[] { "Starts", "Contains", "Ends", "Equals", "Regex" };
                    break;
            }
            int selected = Math.Max(0, this.cmbValueComparison.SelectedIndex);
            bool wasUpdating = this.updatingEditor;
            this.updatingEditor = true;
            this.cmbValueComparison.Items.Clear();
            this.cmbValueComparison.Items.AddRange(items);
            this.cmbValueComparison.SelectedIndex = Math.Min(selected, items.Length - 1);
            this.updatingEditor = wasUpdating;
            if (!wasUpdating)
            {
                this.SaveEditor();
            }
        }

        private async Task RunQueryAsync()
        {
            var root = PluginManager.FindWz((Wz_Type)Enum.Parse(typeof(Wz_Type), this.cmbRoot.Text));
            if (root == null)
            {
                MessageBox.Show("The selected Wz_Type is not open.", "Wz Query", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var rootRules = this.queryTree.Nodes[0].Nodes.Cast<TreeNode>().Where(node => node.Tag is QueryRule).Select(node => (QueryRule)node.Tag).ToList();
            this.cancellation = new CancellationTokenSource();
            this.btnRun.Enabled = false;
            this.btnCancel.Enabled = true;
            this.allResults = new List<QueryResult>();
            this.currentPage = 0;
            this.ShowResultPage();
            this.lblStatus.Text = "Running...";
            this.progressBar.Value = 0;
            this.progressBar.Visible = true;
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var progress = new Progress<QueryProgress>(value =>
                {
                    this.progressBar.Maximum = Math.Max(1, value.Total);
                    this.progressBar.Value = Math.Min(value.Completed, this.progressBar.Maximum);
                    this.lblStatus.Text = $"Running...: {value.Path}";
                });
                QueryExecutionResult executionResult = await Task.Run(() => Execute(root, rootRules, progress, this.cancellation.Token));
                this.allResults = executionResult.Results;
                this.currentPage = 0;
                this.ShowResultPage();
                stopwatch.Stop();
                string limitText = executionResult.LimitReached ? " (result limit reached)" : string.Empty;
                this.lblStatus.Text = $"{this.allResults.Count:N0} results{limitText}: Elapsed {stopwatch.Elapsed.TotalSeconds:F2} seconds";
            }
            catch (OperationCanceledException)
            {
                this.lblStatus.Text = "Canceled";
            }
            catch (Exception ex)
            {
                this.lblStatus.Text = "Error";
                MessageBox.Show(ex.Message, "Wz Query", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                stopwatch.Stop();
                this.cancellation.Dispose();
                this.cancellation = null;
                this.btnRun.Enabled = true;
                this.btnCancel.Enabled = false;
                this.progressBar.Visible = false;
            }
        }

        private static QueryExecutionResult Execute(Wz_Node root, IList<QueryRule> rules, IProgress<QueryProgress> progress, CancellationToken token)
        {
            var results = new List<QueryResult>();
            var rootChildren = GetChildren(root).ToList();
            bool limitReached = ExecuteChildren(root, root.Text, new List<string>(), rules, results, token, rootChildren, progress);
            return new QueryExecutionResult(results, limitReached);
        }

        private static bool ExecuteChildren(Wz_Node parent, string parentPath, List<string> parentOutputs, IList<QueryRule> rules, List<QueryResult> results, CancellationToken token, IList<Wz_Node> directChildren = null, IProgress<QueryProgress> progress = null)
        {
            List<QueryRule> targetRules = rules.Where(rule => rule.Kind == QueryRuleKind.Target).ToList();
            List<QueryRule> excludeRules = rules.Where(rule => rule.Kind == QueryRuleKind.Exclude).ToList();
            IList<Wz_Node> children = directChildren ?? GetChildren(parent).ToList();

            for (int childIndex = 0; childIndex < children.Count; childIndex++)
            {
                token.ThrowIfCancellationRequested();

                Wz_Node child = children[childIndex];
                progress?.Report(new QueryProgress(childIndex, children.Count, parentPath + "\\" + child.Text));

                // 제외 노드 텍스트 조건 확인
                List<QueryRule> textMatchedExcludeRules = excludeRules.Where(rule => rule.IsTextMatch(child)).ToList();
                if (textMatchedExcludeRules.Any(rule => rule.Values.Count == 0))
                {
                    continue;
                }
                // 타겟 노드 텍스트 조건 확인
                List<QueryRule> textMatchedTargetRules = targetRules.Where(rule => rule.IsTextMatch(child)).ToList();
                if (targetRules.Count > 0 && textMatchedTargetRules.Count == 0)
                {
                    continue;
                }

                Wz_Image image = child.GetValue<Wz_Image>();
                bool extractedByQuery = false;
                bool needsImgExtract = image != null &&
                    (textMatchedExcludeRules.Any(rule => rule.Values.Count > 0) ||
                    textMatchedTargetRules.Any(rule => rule.Values.Count > 0 || rule.Children.Count > 0 || rule.SearchDescendants));
                try
                {
                    if (needsImgExtract)
                    {
                        extractedByQuery = !image.IsExtracted;
                        if (!image.TryExtract())
                        {
                            continue;
                        }
                    }
                    // 제외 노드 Value 조건 확인
                    if (textMatchedExcludeRules.Any(rule => rule.AreValuesMatch(child)))
                    {
                        continue;
                    }

                    string path = parentPath + "\\" + child.Text;
                    // 타겟 노드 없으면 검색 성공, 하위 노드 반복 X
                    if (targetRules.Count == 0)
                    {
                        results.Add(new QueryResult(path, parentOutputs.Count > 0 ? string.Join(", ", parentOutputs) : string.Empty));
                        if (results.Count >= MaxResultCount)
                        {
                            return true;
                        }
                        continue;
                    }

                    // 모든 하위 노드 검색
                    foreach (QueryRule target in textMatchedTargetRules.Where(rule => rule.SearchDescendants))
                    {
                        var outputs = new List<string>(parentOutputs);
                        if (target.Output)
                        {
                            outputs.Add(child.Text ?? string.Empty);
                        }
                        if (target.AreValuesMatch(child))
                        {
                            var curOutputs = new List<string>(outputs);
                            curOutputs.AddRange(target.Values.Where(value => value.Output && value.IsMatch(child)).Select(value => value.GetOutput(child)));
                            results.Add(new QueryResult(path, curOutputs.Count > 0 ? string.Join(", ", curOutputs) : string.Empty));
                            if (results.Count >= MaxResultCount)
                            {
                                return true;
                            }
                        }
                        if (ExecuteDescendants(child, path, outputs, target, target.Children, results, token))
                        {
                            return true;
                        }
                    }

                    // 타겟 노드 Value 조건 확인, 하위 쿼리 진행
                    foreach (QueryRule target in textMatchedTargetRules.Where(rule => !rule.SearchDescendants && rule.AreValuesMatch(child)))
                    {
                        var outputs = new List<string>(parentOutputs);
                        if (target.Output)
                        {
                            outputs.Add(child.Text ?? string.Empty);
                        }
                        outputs.AddRange(target.Values.Where(value => value.Output && value.IsMatch(child)).Select(value => value.GetOutput(child)));

                        if (target.Children.Count > 0)
                        {
                            if (ExecuteChildren(child, path, outputs, target.Children, results, token))
                            {
                                return true;
                            }
                        }
                        else
                        {
                            results.Add(new QueryResult(path, outputs.Count > 0 ? string.Join(", ", outputs) : string.Empty));
                            if (results.Count >= MaxResultCount)
                            {
                                return true;
                            }
                        }
                    }
                }
                finally
                {
                    if (extractedByQuery)
                    {
                        image.Unextract();
                    }
                }
            }
            return false;
        }

        private static bool ExecuteDescendants(Wz_Node parent, string parentPath, List<string> parentOutputs, QueryRule target, IList<QueryRule> rules, List<QueryResult> results, CancellationToken token)
        {
            List<QueryRule> excludeRules = rules.Where(rule => rule.Kind == QueryRuleKind.Exclude).ToList();
            IList<Wz_Node> children = GetChildren(parent).ToList();

            foreach (Wz_Node child in children)
            {
                token.ThrowIfCancellationRequested();

                List<QueryRule> textMatchedExcludeRules = excludeRules.Where(rule => rule.IsTextMatch(child)).ToList();
                if (textMatchedExcludeRules.Any(rule => rule.Values.Count == 0))
                {
                    continue;
                }

                Wz_Image image = child.GetValue<Wz_Image>();
                bool extractedByQuery = false;
                try
                {
                    if (image != null)
                    {
                        extractedByQuery = !image.IsExtracted;
                        if (!image.TryExtract())
                        {
                            continue;
                        }
                    }
                    if (textMatchedExcludeRules.Any(rule => rule.AreValuesMatch(child)))
                    {
                        continue;
                    }

                    string path = parentPath + "\\" + child.Text;
                    if (target.AreValuesMatch(child))
                    {
                        var outputs = new List<string>(parentOutputs);
                        outputs.AddRange(target.Values.Where(value => value.Output && value.IsMatch(child)).Select(value => value.GetOutput(child)));
                        results.Add(new QueryResult(path, outputs.Count > 0 ? string.Join(", ", outputs) : string.Empty));
                        if (results.Count >= MaxResultCount)
                        {
                            return true;
                        }
                    }
                    if (ExecuteDescendants(child, path, parentOutputs, target, rules, results, token))
                    {
                        return true;
                    }
                }
                finally
                {
                    if (extractedByQuery)
                    {
                        image.Unextract();
                    }
                }
            }
            return false;
        }

        private static IEnumerable<Wz_Node> GetChildren(Wz_Node node)
        {
            Wz_Node contentNode = node?.GetValue<Wz_Image>()?.Node ?? node;
            return contentNode?.Nodes ?? Enumerable.Empty<Wz_Node>();
        }

        private static ComboBox CreateCombo(int width, params string[] items)
        {
            var combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = width
            };
            combo.Items.AddRange(items);
            if (items.Length > 0)
            {
                combo.SelectedIndex = 0;
            }
            return combo;
        }

        private static string GetComparisonText(int comparison)
        {
            string[] items = { "Starts", "Contains", "Ends", "Equals", "Regex" };
            return items[Math.Max(0, Math.Min(comparison, items.Length - 1))];
        }

        private sealed class QueryRoot
        {

        }

        private sealed class QueryResult
        {
            public QueryResult(string path, string value)
            {
                this.Path = path;
                this.Value = value;
            }

            public string Path { get; private set; }
            public string Value { get; private set; }
        }

        private sealed class QueryExecutionResult
        {
            public QueryExecutionResult(List<QueryResult> results, bool limitReached)
            {
                this.Results = results;
                this.LimitReached = limitReached;
            }

            public List<QueryResult> Results { get; private set; }
            public bool LimitReached { get; private set; }
        }

        private sealed class QueryProgress
        {
            public QueryProgress(int completed, int total, string path)
            {
                this.Completed = completed;
                this.Total = total;
                this.Path = path;
            }

            public int Completed { get; private set; }
            public int Total { get; private set; }
            public string Path { get; private set; }
        }
    }

    internal enum QueryRuleKind
    {
        Target,
        Exclude
    }

    internal enum QueryValueType
    {
        Number,
        String,
        Vector
    }

    internal sealed class QueryRule
    {
        public QueryRule(QueryRuleKind kind)
        {
            this.Kind = kind;
            this.TextPattern = string.Empty;
            this.Values = new List<ValueRule>();
            this.Children = new List<QueryRule>();
        }

        public QueryRuleKind Kind { get; private set; }
        public int TextComparison { get; set; }
        public string TextPattern { get; set; }
        public bool Output { get; set; }
        public bool SearchDescendants { get; set; }
        public List<ValueRule> Values { get; private set; }
        public List<QueryRule> Children { get; private set; }

        public bool IsTextMatch(Wz_Node node)
        {
            return StringMatcher.IsMatch(node.Text ?? string.Empty, this.TextPattern, this.TextComparison);
        }

        public bool AreValuesMatch(Wz_Node node)
        {
            IEnumerable<ValueRule> requiredValues = this.Values.Where(value => value.Required);
            IEnumerable<ValueRule> optionalValues = this.Values.Where(value => !value.Required);
            return requiredValues.All(value => value.IsMatch(node)) &&
                (!optionalValues.Any() || optionalValues.Any(value => value.IsMatch(node)));
        }

        public bool IsMatch(Wz_Node node)
        {
            return this.IsTextMatch(node) && this.AreValuesMatch(node);
        }
    }

    internal sealed class ValueRule
    {
        public ValueRule()
        {
            this.Path = string.Empty;
            this.Pattern = string.Empty;
        }

        public string Path { get; set; }
        public QueryValueType ValueType { get; set; }
        public int Comparison { get; set; }
        public string Pattern { get; set; }
        public bool Output { get; set; }
        public bool Required { get; set; }

        public bool IsMatch(Wz_Node node)
        {
            Wz_Node valueNode = Resolve(node, this.Path);
            if (valueNode == null)
            {
                return false;
            }
            if (string.IsNullOrEmpty(this.Pattern))
            {
                string text = valueNode.GetValueEx<string>(string.Empty);
                return text.Length == 0;
            }

            switch (this.ValueType)
            {
                case QueryValueType.Number:
                    return NumericMatcher.IsMatch(valueNode.Value, this.Pattern, this.Comparison);
                case QueryValueType.Vector:
                    return VectorMatcher.IsMatch(valueNode.Value, this.Pattern, this.Comparison);
                default:
                    return StringMatcher.IsMatch(valueNode.GetValueEx<string>(string.Empty), this.Pattern, this.Comparison);
            }
        }

        public string GetOutput(Wz_Node node)
        {
            Wz_Node valueNode = Resolve(node, this.Path);
            if (valueNode != null)
            {
                if (valueNode.Value is Wz_Vector vector)
                {
                    return $"{valueNode.Text}: ({vector.X}, {vector.Y})";
                }

                string value = valueNode.GetValueEx<string>(null);
                if (value != null)
                {
                    return $"{valueNode.Text}: {value}";
                }
                else return valueNode.Text;
            }
            return string.Empty;
        }

        private static Wz_Node Resolve(Wz_Node node, string path)
        {
            Wz_Node current = node?.GetValue<Wz_Image>()?.Node ?? node;
            if (current == null || string.IsNullOrEmpty(path))
            {
                return current;
            }
            foreach (string segment in path.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries))
            {
                current = current.Nodes[segment];
                if (current == null)
                {
                    return null;
                }
            }
            return current;
        }
    }

    internal static class StringMatcher
    {
        public static bool IsMatch(string target, string pattern, int comparison)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return true;
            }

            switch (comparison)
            {
                case 0: return target.StartsWith(pattern, StringComparison.OrdinalIgnoreCase);
                case 1: return target.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
                case 2: return target.EndsWith(pattern, StringComparison.OrdinalIgnoreCase);
                case 3: return string.Equals(target, pattern, StringComparison.OrdinalIgnoreCase);
                default: return new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1)).IsMatch(target);
            }
        }
    }

    internal static class NumericMatcher
    {
        public static bool IsMatch(object value, string pattern, int comparison)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return true;
            }
            if (!TryParse(Convert.ToString(value), out decimal actual) || !TryParse(pattern, out decimal expected))
            {
                return false;
            }

            switch (comparison)
            {
                case 0: return actual == expected;
                case 1: return actual != expected;
                case 2: return actual < expected;
                case 3: return actual > expected;
                case 4: return actual <= expected;
                default: return actual >= expected;
            }
        }

        private static bool TryParse(string text, out decimal value)
        {
            return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value) ||
                decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value);
        }
    }

    internal static class VectorMatcher
    {
        public static bool IsMatch(object value, string pattern, int comparison)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return true;
            }
            if (!(value is Wz_Vector vector) || !TryParse(pattern, out int x, out int y))
            {
                return false;
            }

            bool equals = vector.X == x && vector.Y == y;
            return comparison == 0 ? equals : !equals;
        }

        private static bool TryParse(string pattern, out int x, out int y)
        {
            string[] values = pattern.Trim().Trim('(', ')').Split(',');
            if (values.Length == 2 && int.TryParse(values[0].Trim(), out x) && int.TryParse(values[1].Trim(), out y))
            {
                return true;
            }

            x = 0;
            y = 0;
            return false;
        }
    }
}
