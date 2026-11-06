using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using WzComparerR2.PluginBase;
using WzComparerR2.WzLib;

namespace WzComparerR2.DumpAllXml {
    public class WzXmlExporter {
        // 配置区（原 Lua 脚本参数）
        private string outputDir;
        private IList<string> filters;
        private string[] wzList;
        // 控制导出并发度（避免文件句柄爆炸）
        private SemaphoreSlim fileWriteSemaphore;

        // 打印日志
        private Action<string> logger;

        // 日志节流：成功导出的文件不再逐条打日志，最多每 1 秒输出一条进度。
        // 全量导出动辄十几万个文件，逐条日志会把 UI 日志面板拖死。
        private static readonly long ProgressIntervalTicks = System.Diagnostics.Stopwatch.Frequency;
        private long savedCount;
        private long lastProgressTimestamp;

        // 避免输出的xml带BOM，导致Java无法识别
        private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false);

        // **修改点：Log 方法，加入线程 ID**
        private void Log(string msg) {
            // 获取当前线程 ID
            int threadId = Thread.CurrentThread.ManagedThreadId;
            // 格式化日志信息
            logger?.Invoke($"[TID:{threadId:D2}] {msg}");
        }

        /// <summary>
        /// 每成功导出一个文件调用一次；进度日志按 1 秒节流。
        /// </summary>
        private void ReportProgress(string path) {
            long count = Interlocked.Increment(ref this.savedCount);
            long now = System.Diagnostics.Stopwatch.GetTimestamp();
            long last = Interlocked.Read(ref this.lastProgressTimestamp);
            if (now - last < ProgressIntervalTicks) {
                return;
            }
            if (Interlocked.CompareExchange(ref this.lastProgressTimestamp, now, last) != last) {
                return;
            }
            Log($"[进度] 已导出 {count} 个文件，当前: {path}");
        }

        public WzXmlExporter(Action<string> logAction = null) {
            logger = logAction ?? (s => { /* fallback */ });
        }

        public async Task ExportAllAsync(string outputDir, IList<string> filters, string[] wzList, int fileWriteSemaphoreCount) {
            // 传入参数
            this.outputDir = outputDir;
            this.filters = filters;
            this.wzList = wzList;
            fileWriteSemaphore = new SemaphoreSlim(fileWriteSemaphoreCount);

            this.savedCount = 0;
            this.lastProgressTimestamp = 0;
            Log("======== 开始导出所有 WZ ========");

            // 第一层：并行处理每个 WZ 文件（顶层文件夹）
            var exportTasks = wzList.Select(wzName => Task.Run(() => ExportSingleWz(wzName))).ToList();

            await Task.WhenAll(exportTasks);

            Log($"======== 所有 WZ 解析任务已完成，共导出 {this.savedCount} 个文件 ========");
        }

        private void ExportSingleWz(string wzName) {
            Log($"-------- 开始解析 {wzName} ---------");

            var topNode = PluginManager.FindWz(wzName);
            if (topNode == null) {
                Log($"未能找到顶层节点：{wzName}，请确认已在 WzComparerR2 中打开 Base.wz。");
                return;
            }

            // 收集所有可提取的 image 节点（Wz_Image / Ms_Image）
            var allImgNodes = listAllImages(topNode)
                              .Where(n => n.Value is Wz_Image || n.Value is Ms_Image)
                              .ToList();

            // 按所属 IMapleStoryFile（实际持有底层流的对象）聚合：
            // 这样能把真正共享同一个底层流的节点分在同一组，从而对同一流做组级串行
            var groups = allImgNodes
                         .GroupBy(n => {
                             // 尝试通过节点的 Value 直接拿到关联文件（Wz_Image/Ms_Image -> WzFile）
                             var val = n.Value;
                             if (val is Wz_Image wzImg) return wzImg.WzFile;
                             if (val is Ms_Image msImg) return msImg.WzFile;

                             // 兜底：向上查找祖先中是否有 Wz_File 节点（历史情况）
                             var cur = n;
                             while (cur != null && !(cur.Value is Wz_File)) {
                                 cur = cur.ParentNode;
                             }
                             return cur?.Value as IMapleStoryFile;
                         })
                         .ToList();

            // 并行处理不同 IMapleStoryFile（不同底层流）的组；每组内部串行处理其图像节点
            Parallel.ForEach(groups, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, group => {
                var ownerFile = group.Key; // IMapleStoryFile, 可能为 null
                string ownerName = null;
                try {
                    if (ownerFile is Wz_File wf) ownerName = wf.Node?.Text;
                    else if (ownerFile is Ms_File mf) ownerName = mf.Header?.FullFileName ?? "(ms-file)";
                } catch { ownerName = "(unknown)"; }

                Log($"[并行任务] 处理文件组: {ownerName ?? "(no-file)"} 下 {group.Count()} 个节点");

                // 对每个组：若能拿到 ReadLock，则把整个组处理包在该锁内，保证对底层流的所有操作串行
                if (ownerFile != null) {
                    var readLock = ownerFile.ReadLock ?? ownerFile.FileStream as object;
                    if (readLock != null) {
                        lock (readLock) {
                            foreach (var node in group) {
                                // 同步等待异步方法，避免 fire-and-forget任务泛滥
                                ExportImageNode(node).GetAwaiter().GetResult();
                            }
                        }
                    } else {
                        // 没有可锁对象（非常罕见），仍串行执行组内节点
                        foreach (var node in group) {
                            ExportImageNode(node).GetAwaiter().GetResult();
                        }
                    }
                } else {
                    // 没有找到关联文件的节点，按组内串行处理
                    foreach (var node in group) {
                        ExportImageNode(node).GetAwaiter().GetResult();
                    }
                }

                Log($"[并行任务] 完成文件组: {ownerName ?? "(no-file)"}");
            });

            Log($"-------- 解析完成：{wzName} ---------");
        }

        private IEnumerable<Wz_Node> listAllImages(Wz_Node root) {
            var stack = new Stack<Wz_Node>();
            stack.Push(root);

            while (stack.Count > 0) {
                var node = stack.Pop();
                yield return node;

                // 过滤 _Canvas：祖先一旦命中就不再入栈，所以只需判断当前节点的名字，
                // 不必为每个节点拼一次 FullPath（那是 stack + string.Join，全量导出时是热点）。
                if (node.Text?.Contains("_Canvas") == true) {
                    continue;
                }

                foreach (var child in node.Nodes) {
                    stack.Push(child);
                }
            }
        }

        private async Task WriteFileAsync(string path, string content) {
#if NET6_0_OR_GREATER || NET8_0
            // 使用真正的异步 API
            await File.WriteAllTextAsync(path, content, Utf8WithoutBom).ConfigureAwait(false);
#else
            // .NET Framework 回退：在线程池中执行同步写
            await Task.Run(() => File.WriteAllText(path, content, Utf8WithoutBom)).ConfigureAwait(false);
#endif
        }

        private async Task ExportImageNode(Wz_Node imgNode) {
            // Ms_Image 继承自 Wz_Image，统一按 Wz_Image 处理（不再需要 dynamic）
            if (!(imgNode.Value is Wz_Image img)) {
                return;
            }

            if (!img.TryExtract()) {
                return;
            }

            string xmlFileName = Path.Combine(outputDir, imgNode.FullPathToFile + ".xml");
            try {
                string dir = Path.GetDirectoryName(xmlFileName);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var sb = new StringBuilder();
                using (var sw = new StringWriter(sb))
                using (var writer = XmlWriter.Create(sw, GetXmlWriterSettings(false))) {
                    writer.WriteStartDocument(true);
                    Wz_NodeExtension.DumpAsXml(img.Node, writer, filters);
                    writer.WriteEndDocument();
                }

                string fullXml = sb.ToString()
                    // 修正声明中的错误编码（有时 DumpAsXml 会输出 utf-16）
                    .Replace("encoding=\"utf-16\"", "encoding=\"utf-8\"")
                    // 替换换行符
                    .Replace("&#xD;&#xA;", "\\n")
                    .Replace("&#xA;", "\\n")
                    .Replace("&#xD;", "\\n");

                // 控制并发：异步等待信号量，避免太多并发写入
                await fileWriteSemaphore.WaitAsync().ConfigureAwait(false);
                try {
                    await WriteFileAsync(xmlFileName, fullXml).ConfigureAwait(false);
                    ReportProgress(xmlFileName);
                } catch (Exception ex) {
                    Log($"[错误] {xmlFileName}: {ex.Message}");
                } finally {
                    fileWriteSemaphore.Release();
                }
            } catch (Exception e) {
                Log(e.Message);
            } finally {
                img.Unextract();
            }
        }

        // 获取 XML 写入设置
        private XmlWriterSettings GetXmlWriterSettings(bool closeOutput) {
            return new XmlWriterSettings {
                CloseOutput = closeOutput,
                Indent = true,
                Encoding = Utf8WithoutBom,
                CheckCharacters = false,
                NewLineChars = "\r\n",
                // 设置为 Replace，确保属性中的 \r\n 换行符被转义为 &#xD;&#xA; 实体
                NewLineHandling = NewLineHandling.Replace,
                NewLineOnAttributes = false
            };
        }
    }
}