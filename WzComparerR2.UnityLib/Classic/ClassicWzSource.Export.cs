using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using WzComparerR2.UnityLib.Unity;
using WzComparerR2.UnityLib.Wzjs;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>Statistics of a subtree export.</summary>
    public sealed class ClassicExportStats
    {
        internal ClassicExportStats(string outDir, int jsonFiles, int pngFiles, int nodes)
        {
            this.OutDir = outDir;
            this.JsonFiles = jsonFiles;
            this.PngFiles = pngFiles;
            this.Nodes = nodes;
        }

        public string OutDir { get; private set; }
        public int JsonFiles { get; private set; }
        public int PngFiles { get; private set; }
        public int Nodes { get; private set; }
    }

    /// <summary>Result of a single-file export.</summary>
    public sealed class ClassicFileExportResult
    {
        internal ClassicFileExportResult(string outPath, int nodes)
        {
            this.OutPath = outPath;
            this.Nodes = nodes;
        }

        public string OutPath { get; private set; }
        public int Nodes { get; private set; }
    }

    public sealed partial class ClassicWzSource
    {
        /// <summary>
        /// Exports the subtree the path points to (either an img root or a node inside the tree). TextAssets are
        /// written out directly as plain JSON.
        /// <para>
        /// Output layout (using <c>TamingMob/0004</c> as an example):
        /// <c>&lt;out&gt;/TamingMob/0004.json</c> and
        /// <c>&lt;out&gt;/TamingMob/0004/walk1/0.png</c> (referenced from the JSON via <c>$png</c>).
        /// </para>
        /// </summary>
        public ClassicExportStats ExportSubtree(string path, string outRoot)
        {
            string imageKey;
            string inner;
            if (!this.Catalog.TrySplitImagePrefix(path, out imageKey, out inner))
            {
                throw new ClassicAssetException("The path does not exist or cannot be parsed: " + path);
            }
            UnityEntryLocation location;
            if (!this.Catalog.Entries.TryGetValue(imageKey, out location))
            {
                throw new ClassicAssetException("The path does not exist or cannot be parsed: " + imageKey);
            }

            if (location.ClassId == UnityClassIds.TextAsset)
            {
                if (inner.Length > 0)
                {
                    throw new ClassicAssetException("The path does not exist or cannot be parsed: " + path);
                }
                byte[] text = this.ReadTextAsset(imageKey).Script;
                string textPath = AppendExtension(Path.Combine(outRoot, SafeRelativePath(path)), "json");
                EnsureParentDirectory(textPath);
                File.WriteAllBytes(textPath, text);
                return new ClassicExportStats(outRoot, 1, 0, 1);
            }

            if (location.ClassId == UnityClassIds.Sprite)
            {
                if (inner.Length > 0)
                {
                    throw new ClassicAssetException("The path does not exist or cannot be parsed: " + path);
                }
                byte[] png = this.GetImage(imageKey);
                if (png == null)
                {
                    throw new ClassicAssetException(path + " has no exportable image.");
                }
                string pngPath = AppendExtension(Path.Combine(outRoot, SafeRelativePath(path)), "png");
                EnsureParentDirectory(pngPath);
                File.WriteAllBytes(pngPath, png);
                return new ClassicExportStats(outRoot, 0, 1, 1);
            }

            WzjsNode root = this.GetTree(imageKey);
            WzjsNode node = inner.Length == 0 ? root : root.Find(inner);
            if (node == null)
            {
                throw new ClassicAssetException("The path does not exist or cannot be parsed: " + path);
            }

            string safePath = SafeRelativePath(path);
            string jsonPath = AppendExtension(Path.Combine(outRoot, safePath), "json");
            EnsureParentDirectory(jsonPath);

            ExportContext context = new ExportContext
            {
                SourcePath = path,
                PngBase = Path.Combine(outRoot, safePath),
            };
            string json = this.NodeToJson(node, context, 0);
            File.WriteAllBytes(jsonPath, Encoding.UTF8.GetBytes(json));
            return new ClassicExportStats(outRoot, 1, context.PngFiles, context.Nodes);
        }

        /// <summary>
        /// Exports the currently selected node as a single JSON, PNG, or XML file.
        /// <para>JSON and XML contain only the data structure; canvases keep just a type marker and do not embed images.</para>
        /// </summary>
        public ClassicFileExportResult ExportFile(string path, string outputPath, string format)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }
            EnsureParentDirectory(outputPath);

            if (string.Equals(format, "png", StringComparison.OrdinalIgnoreCase))
            {
                ClassicNodeDetail detail = this.GetNode(path);
                if (!IsImageCapableTag(detail.TypeTag))
                {
                    throw new ClassicAssetException(string.Format(
                        "{0} is not a node that can export an image (must be a Canvas or UOL).", path));
                }
                byte[] png = this.GetImage(path);
                if (png == null)
                {
                    throw new ClassicAssetException(path + " has no exportable image.");
                }
                File.WriteAllBytes(outputPath, png);
                return new ClassicFileExportResult(outputPath, 1);
            }

            string imageKey;
            string inner;
            if (!this.Catalog.TrySplitImagePrefix(path, out imageKey, out inner))
            {
                throw new ClassicAssetException("The path does not exist or cannot be parsed: " + path);
            }
            UnityEntryLocation location;
            if (!this.Catalog.Entries.TryGetValue(imageKey, out location))
            {
                throw new ClassicAssetException("The path does not exist or cannot be parsed: " + imageKey);
            }

            if (location.ClassId == UnityClassIds.TextAsset)
            {
                string text = Encoding.UTF8.GetString(this.ReadTextAsset(imageKey).Script);
                string output;
                if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
                {
                    output = QuoteJsonString(text);
                }
                else if (string.Equals(format, "xml", StringComparison.OrdinalIgnoreCase))
                {
                    output = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                        + "<text-asset path=\"" + EscapeXmlAttribute(path) + "\">"
                        + EscapeXmlText(text) + "</text-asset>\n";
                }
                else
                {
                    throw new ClassicAssetException("Unsupported export format: " + format);
                }
                File.WriteAllBytes(outputPath, Encoding.UTF8.GetBytes(output));
                return new ClassicFileExportResult(outputPath, 1);
            }

            if (location.ClassId == UnityClassIds.Sprite)
            {
                if (inner.Length > 0)
                {
                    throw new ClassicAssetException("The path does not exist or cannot be parsed: " + path);
                }
                string output;
                if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
                {
                    output = "{\"$type\":\"canvas\"}\n";
                }
                else if (string.Equals(format, "xml", StringComparison.OrdinalIgnoreCase))
                {
                    output = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n"
                        + "<canvas path=\"" + EscapeXmlAttribute(path) + "\"/>\n";
                }
                else
                {
                    throw new ClassicAssetException("Unsupported export format: " + format);
                }
                File.WriteAllBytes(outputPath, Encoding.UTF8.GetBytes(output));
                return new ClassicFileExportResult(outputPath, 1);
            }

            WzjsNode root = this.GetTree(imageKey);
            WzjsNode node = inner.Length == 0 ? root : root.Find(inner);
            if (node == null)
            {
                throw new ClassicAssetException("The path does not exist or cannot be parsed: " + path);
            }

            int nodes = 0;
            string content;
            if (string.Equals(format, "json", StringComparison.OrdinalIgnoreCase))
            {
                content = NodeToDataJson(node, ref nodes, 0);
            }
            else if (string.Equals(format, "xml", StringComparison.OrdinalIgnoreCase))
            {
                content = ExportXml(path, node, ref nodes);
            }
            else
            {
                throw new ClassicAssetException("Unsupported export format: " + format);
            }
            File.WriteAllBytes(outputPath, Encoding.UTF8.GetBytes(content));
            return new ClassicFileExportResult(outputPath, nodes);
        }

        private static bool IsImageCapableTag(int typeTag)
        {
            return WzjsImage.IsCanvasTag(typeTag) || typeTag == WzjsImage.TypeUol;
        }

        /// <summary>Node → nested JSON; canvases also emit their PNG along the way.</summary>
        private string NodeToJson(WzjsNode node, ExportContext context, int depth)
        {
            context.Nodes++;
            bool isCanvas = node.IsCanvas;

            if (node.Children.Count == 0 && !isCanvas)
            {
                return LeafJson(node.Value);
            }

            List<KeyValuePair<string, string>> members = new List<KeyValuePair<string, string>>();
            if (isCanvas)
            {
                members.Add(new KeyValuePair<string, string>("\"$type\"", "\"canvas\""));
                string sourceRelative = string.Join("/", context.SourceStack);
                string fileRelative = string.Join("/", context.FileStack);
                string fullPath = sourceRelative.Length == 0
                    ? context.SourcePath
                    : context.SourcePath + "/" + sourceRelative;
                byte[] png;
                try
                {
                    png = this.GetImage(fullPath);
                }
                catch (Exception ex) when (ex is UnityException || ex is ClassicAssetException)
                {
                    png = null;
                }
                if (png != null)
                {
                    ulong hash = Fnv1a(png);
                    string existing = context.FindIdenticalPng(hash, png);
                    if (existing != null)
                    {
                        members.Add(new KeyValuePair<string, string>("\"$png\"", QuoteJsonString(existing)));
                    }
                    else
                    {
                        string name = fileRelative.Length == 0 ? "_root" : fileRelative;
                        string relativeFile = name + ".png";
                        string file = Path.Combine(
                            context.PngBase, relativeFile.Replace('/', Path.DirectorySeparatorChar));
                        EnsureParentDirectory(file);
                        File.WriteAllBytes(file, png);
                        context.PngFiles++;
                        context.RegisterPng(hash, relativeFile);
                        members.Add(new KeyValuePair<string, string>(
                            "\"$png\"", QuoteJsonString(relativeFile)));
                    }
                }
                else
                {
                    members.Add(new KeyValuePair<string, string>("\"$png\"", "null"));
                }
            }

            if (node.Value.HasValue)
            {
                members.Add(new KeyValuePair<string, string>("\"$value\"", LeafJson(node.Value)));
            }

            Dictionary<string, int> seen = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (WzjsNode child in node.Children)
            {
                int count;
                seen.TryGetValue(child.Name, out count);
                count++;
                seen[child.Name] = count;
                string key = count == 1
                    ? child.Name
                    : child.Name + "#" + count.ToString(CultureInfo.InvariantCulture);

                context.SourceStack.Add(child.Name);
                context.FileStack.Add(SafeSegment(key));
                string value = this.NodeToJson(child, context, depth + 1);
                context.SourceStack.RemoveAt(context.SourceStack.Count - 1);
                context.FileStack.RemoveAt(context.FileStack.Count - 1);
                members.Add(new KeyValuePair<string, string>(QuoteJsonString(key), value));
            }

            return FormatJsonObject(members, depth);
        }

        /// <summary>Data tree used by the single-file JSON export. Canvases do not emit images, only <c>$type</c>.</summary>
        private static string NodeToDataJson(WzjsNode node, ref int nodes, int depth)
        {
            nodes++;
            bool isCanvas = node.IsCanvas;
            if (node.Children.Count == 0 && !isCanvas)
            {
                return LeafJson(node.Value);
            }

            List<KeyValuePair<string, string>> members = new List<KeyValuePair<string, string>>();
            if (isCanvas)
            {
                members.Add(new KeyValuePair<string, string>("\"$type\"", "\"canvas\""));
            }
            else if (node.Value.HasValue)
            {
                members.Add(new KeyValuePair<string, string>("\"$value\"", LeafJson(node.Value)));
            }

            Dictionary<string, int> seen = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (WzjsNode child in node.Children)
            {
                int count;
                seen.TryGetValue(child.Name, out count);
                count++;
                seen[child.Name] = count;
                string name = count == 1
                    ? child.Name
                    : child.Name + "#" + count.ToString(CultureInfo.InvariantCulture);
                members.Add(new KeyValuePair<string, string>(
                    QuoteJsonString(name), NodeToDataJson(child, ref nodes, depth + 1)));
            }
            return FormatJsonObject(members, depth);
        }

        private static string FormatJsonObject(List<KeyValuePair<string, string>> members, int depth)
        {
            if (members.Count == 0)
            {
                return "{}";
            }
            string indent = new string(' ', (depth + 1) * 2);
            string closing = new string(' ', depth * 2);
            StringBuilder builder = new StringBuilder();
            builder.Append("{\n");
            for (int i = 0; i < members.Count; i++)
            {
                builder.Append(indent).Append(members[i].Key).Append(": ").Append(members[i].Value);
                if (i < members.Count - 1)
                {
                    builder.Append(',');
                }
                builder.Append('\n');
            }
            builder.Append(closing).Append('}');
            return builder.ToString();
        }

        /// <summary>Leaf value → JSON scalar (vectors / Float2 are flattened into [x, y]).</summary>
        private static string LeafJson(WzjsValue value)
        {
            switch (value.Kind)
            {
                case WzjsValueKind.SubTree:
                    return "null";
                case WzjsValueKind.Canvas:
                    return "{\"$type\": \"canvas\"}";
                case WzjsValueKind.U8:
                case WzjsValueKind.U16:
                case WzjsValueKind.Int:
                case WzjsValueKind.Long:
                    return value.Integer.ToString(CultureInfo.InvariantCulture);
                case WzjsValueKind.Double:
                    return FormatDouble(value.Real);
                case WzjsValueKind.Float2:
                case WzjsValueKind.Vector:
                    return "[" + FormatFloat(value.X) + ", " + FormatFloat(value.Y) + "]";
                case WzjsValueKind.Int2:
                    return "[" + value.Int2A.ToString(CultureInfo.InvariantCulture)
                        + ", " + value.Int2B.ToString(CultureInfo.InvariantCulture) + "]";
                case WzjsValueKind.String:
                case WzjsValueKind.Uol:
                    return QuoteJsonString(value.Text);
                default:
                    return "null";
            }
        }

        private static string FormatFloat(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return "null";
            }
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string FormatDouble(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                return "null";
            }
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        private static string QuoteJsonString(string value)
        {
            StringBuilder builder = new StringBuilder(value.Length + 2);
            builder.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\b':
                        builder.Append("\\b");
                        break;
                    case '\f':
                        builder.Append("\\f");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        if (c < 0x20)
                        {
                            builder.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(c);
                        }
                        break;
                }
            }
            builder.Append('"');
            return builder.ToString();
        }

        private static string ExportXml(string path, WzjsNode node, ref int nodes)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
            builder.Append("<msclassic-export path=\"").Append(EscapeXmlAttribute(path)).Append("\">\n");
            WriteXmlNode(node, 1, ref nodes, builder);
            builder.Append("</msclassic-export>\n");
            return builder.ToString();
        }

        private static void WriteXmlNode(WzjsNode node, int depth, ref int nodes, StringBuilder output)
        {
            nodes++;
            string indent = new string(' ', depth * 2);
            string name = EscapeXmlAttribute(node.Name);
            string typeName = ValueTypeName(node.Value);
            bool hasValue = node.Value.HasValue;

            if (node.Children.Count == 0 && !hasValue)
            {
                output.Append(indent).Append("<node name=\"").Append(name)
                    .Append("\" type=\"").Append(typeName).Append("\" />\n");
                return;
            }

            output.Append(indent).Append("<node name=\"").Append(name)
                .Append("\" type=\"").Append(typeName).Append("\">\n");
            if (hasValue)
            {
                string text = LeafJson(node.Value);
                if (text.Length > 0 && text[0] == '"' && text[text.Length - 1] == '"')
                {
                    text = UnquoteJsonString(text);
                }
                output.Append(indent).Append("  <value>").Append(EscapeXmlText(text)).Append("</value>\n");
            }
            foreach (WzjsNode child in node.Children)
            {
                WriteXmlNode(child, depth + 1, ref nodes, output);
            }
            output.Append(indent).Append("</node>\n");
        }

        private static string UnquoteJsonString(string quoted)
        {
            // Only reverses strings produced by QuoteJsonString (which is sufficient for node values).
            StringBuilder builder = new StringBuilder(quoted.Length);
            for (int i = 1; i < quoted.Length - 1; i++)
            {
                char c = quoted[i];
                if (c != '\\' || i + 1 >= quoted.Length - 1)
                {
                    builder.Append(c);
                    continue;
                }
                char escape = quoted[++i];
                switch (escape)
                {
                    case 'n':
                        builder.Append('\n');
                        break;
                    case 'r':
                        builder.Append('\r');
                        break;
                    case 't':
                        builder.Append('\t');
                        break;
                    case 'b':
                        builder.Append('\b');
                        break;
                    case 'f':
                        builder.Append('\f');
                        break;
                    case 'u':
                        if (i + 4 < quoted.Length)
                        {
                            int code;
                            if (int.TryParse(quoted.Substring(i + 1, 4), NumberStyles.HexNumber,
                                CultureInfo.InvariantCulture, out code))
                            {
                                builder.Append((char)code);
                                i += 4;
                            }
                        }
                        break;
                    default:
                        builder.Append(escape);
                        break;
                }
            }
            return builder.ToString();
        }

        private static string ValueTypeName(WzjsValue value)
        {
            switch (value.Kind)
            {
                case WzjsValueKind.SubTree:
                    return "subtree";
                case WzjsValueKind.Canvas:
                    return "canvas";
                case WzjsValueKind.U8:
                    return "u8";
                case WzjsValueKind.U16:
                    return "u16";
                case WzjsValueKind.Int:
                    return "int";
                case WzjsValueKind.Long:
                    return "long";
                case WzjsValueKind.Float2:
                    return "float2";
                case WzjsValueKind.Int2:
                    return "int2";
                case WzjsValueKind.Double:
                    return "double";
                case WzjsValueKind.Vector:
                    return "vector";
                case WzjsValueKind.String:
                    return "string";
                case WzjsValueKind.Uol:
                    return "uol";
                default:
                    return "unknown";
            }
        }

        private static string EscapeXmlText(string value)
        {
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        private static string EscapeXmlAttribute(string value)
        {
            return EscapeXmlText(value)
                .Replace("\"", "&quot;")
                .Replace("\r", "&#13;")
                .Replace("\n", "&#10;")
                .Replace("\t", "&#9;");
        }

        private static void EnsureParentDirectory(string path)
        {
            string parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                Directory.CreateDirectory(parent);
            }
        }

        private static string AppendExtension(string path, string extension)
        {
            return path + "." + extension;
        }

        private static string SafeRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ClassicAssetException("The export path must not be empty.");
            }
            string[] segments = path.Split('/');
            StringBuilder builder = new StringBuilder();
            foreach (string segment in segments)
            {
                if (segment.Length == 0)
                {
                    throw new ClassicAssetException("The export path contains an empty segment: " + path);
                }
                if (builder.Length > 0)
                {
                    builder.Append(Path.DirectorySeparatorChar);
                }
                builder.Append(SafeSegment(segment));
            }
            return builder.ToString();
        }

        /// <summary>Encodes an arbitrary asset name into a single, safe, reversibly identifiable Windows file name segment.</summary>
        internal static string SafeSegment(string segment)
        {
            StringBuilder output = new StringBuilder();
            byte[] bytes = Encoding.UTF8.GetBytes(segment);
            foreach (byte b in bytes)
            {
                bool safe = (b >= 'a' && b <= 'z') || (b >= 'A' && b <= 'Z') || (b >= '0' && b <= '9')
                    || b == '-' || b == '_' || b == '.';
                if (safe)
                {
                    output.Append((char)b);
                }
                else
                {
                    output.Append('%').Append(b.ToString("X2", CultureInfo.InvariantCulture));
                }
            }

            string result = output.ToString();
            bool reserved = result == "." || result == "..";
            if (!reserved)
            {
                string[] deviceNames = new[]
                {
                    "CON", "PRN", "AUX", "NUL",
                    "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
                    "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
                };
                foreach (string device in deviceNames)
                {
                    if (string.Equals(result, device, StringComparison.OrdinalIgnoreCase))
                    {
                        reserved = true;
                        break;
                    }
                }
            }
            if (reserved)
            {
                StringBuilder escaped = new StringBuilder();
                foreach (byte b in bytes)
                {
                    escaped.Append('%').Append(b.ToString("X2", CultureInfo.InvariantCulture));
                }
                return escaped.ToString();
            }
            return result;
        }

        /// <summary>FNV-1a 64-bit (deduplicates PNG content, no external dependencies).</summary>
        private static ulong Fnv1a(byte[] data)
        {
            ulong hash = 0xcbf29ce484222325UL;
            foreach (byte b in data)
            {
                hash ^= b;
                hash *= 0x100000001b3UL;
            }
            return hash;
        }

        private sealed class ExportContext
        {
            public ExportContext()
            {
                this.SourceStack = new List<string>();
                this.FileStack = new List<string>();
                this.SeenPng = new Dictionary<ulong, List<string>>();
            }

            public string SourcePath { get; set; }
            public string PngBase { get; set; }
            public int PngFiles { get; set; }
            public int Nodes { get; set; }
            public List<string> SourceStack { get; private set; }
            public List<string> FileStack { get; private set; }
            public Dictionary<ulong, List<string>> SeenPng { get; private set; }

            public void RegisterPng(ulong hash, string relativeFile)
            {
                List<string> files;
                if (!this.SeenPng.TryGetValue(hash, out files))
                {
                    files = new List<string>();
                    this.SeenPng[hash] = files;
                }
                files.Add(relativeFile);
            }

            public string FindIdenticalPng(ulong hash, byte[] png)
            {
                List<string> files;
                if (!this.SeenPng.TryGetValue(hash, out files))
                {
                    return null;
                }
                foreach (string relative in files)
                {
                    string existing = Path.Combine(
                        this.PngBase, relative.Replace('/', Path.DirectorySeparatorChar));
                    try
                    {
                        if (File.Exists(existing) && ByteEquals(File.ReadAllBytes(existing), png))
                        {
                            return relative;
                        }
                    }
                    catch (IOException)
                    {
                        // Treat an unreadable file as absent (writing a duplicate does not affect correctness).
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }
                return null;
            }

            private static bool ByteEquals(byte[] left, byte[] right)
            {
                if (left.Length != right.Length)
                {
                    return false;
                }
                for (int i = 0; i < left.Length; i++)
                {
                    if (left[i] != right[i])
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
