using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Text.RegularExpressions;

namespace WzComparerR2.WzLib
{
    public class Wz_Node : ICloneable, IComparable, IComparable<Wz_Node>
    {
        public Wz_Node()
        {
            this.nodes = new WzNodeCollection(this);
        }

        public Wz_Node(string nodeText)
            : this()
        {
            this.text = nodeText;
        }

        //fields
        private object value;
        private string text;
        private WzNodeCollection nodes;
        private Wz_Node parentNode;

        //properties
        public object Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public string Text
        {
            get { return this.text; }
            set { this.text = value; }
        }

        public string FullPath
        {
            get
            {
                Stack<string> path = new Stack<string>();
                Wz_Node node = this;
                do
                {
                    path.Push(node.text);
                    node = node.parentNode;
                } while (node != null);
                return string.Join("\\", path.ToArray());
            }
        }

        public string FullPathToFile
        {
            get
            {
                Stack<string> path = new Stack<string>();
                Wz_Node node = this;
                do
                {
                    if (node.value is Wz_File wzf && !wzf.IsSubDir)
                    {
                        if (node.text.EndsWith(".wz", StringComparison.OrdinalIgnoreCase))
                        {
                            path.Push(node.text.Substring(0, node.text.Length - 3));
                        }
                        else
                        {
                            path.Push(node.text);
                        }
                        break;
                    }

                    path.Push(node.text);

                    var img = node.GetValue<Wz_Image>();
                    if (img != null)
                    {
                        node = img.OwnerNode;
                    }

                    if (node != null)
                    {
                        node = node.parentNode;
                    }
                } while (node != null);
                return string.Join("\\", path.ToArray());
            }
        }

        public WzNodeCollection Nodes
        {
            get { return this.nodes; }
        }

        public Wz_Node ParentNode
        {
            get { return parentNode; }
            private set { parentNode = value; }
        }

        //methods
        public override string ToString()
        {
            return this.Text + " " + (this.value != null ? this.value.ToString() : "-") + " " + this.nodes.Count;
        }

        public Wz_Node FindNodeByPath(string fullPath)
        {
            return FindNodeByPath(fullPath, false);
        }

        public Wz_Node FindNodeByPath(string fullPath, bool extractImage)
        {
            string[] patten = fullPath.Split('\\');
            return FindNodeByPath(extractImage, patten);
        }

        public Wz_Node FindNodeByPath(bool extractImage, params string[] fullPath)
        {
            return FindNodeByPath(extractImage, false, fullPath);
        }

        public Wz_Node FindNodeByPath(bool extractImage, bool ignoreCase, params string[] fullPath)
        {
            Wz_Node node = this;

            Wz_Image img;

            //首次解压
            if (extractImage && (img = this.GetValue<Wz_Image>()) != null)
            {
                if (img.TryExtract())
                {
                    node = img.Node;
                }
            }

            foreach (string txt in fullPath)
            {
                if (ignoreCase)
                {
                    bool find = false;

                    foreach (Wz_Node subNode in node.nodes)
                    {
                        if (string.Equals(subNode.text, txt, StringComparison.OrdinalIgnoreCase))
                        {
                            find = true;
                            node = subNode;
                        }
                    }
                    if (!find)
                        node = null;
                }
                else
                {
                    node = node.nodes[txt];
                }

                if (node == null)
                    return null;

                if (extractImage)
                {
                    img = node.GetValue<Wz_Image>();
                    if (img != null && img.TryExtract()) //判断是否是img
                    {
                        node = img.Node;
                    }
                }
            }
            return node;
        }

        public T GetValue<T>(T defaultValue)
        {
            var typeT = typeof(T);
            if (typeof(Wz_Image) == typeT)
            {
                if (this is Wz_Image.Wz_ImageNode)
                {
                    return (T)(object)(((Wz_Image.Wz_ImageNode)this).Image);
                }
                else
                {
                    return (this.value is T) ? (T)this.value : default(T);
                }
            }
            if (this.value == null)
                return defaultValue;
            if (this.value is T)
                return (T)this.value;


            if (this.value is string s)
            {
                if (ObjectConverter.TryParse<T>(s, out T result, out bool hasTryParse))
                {
                    return result;
                }
                if (hasTryParse)
                {
                    return defaultValue;
                }
            }

            if (this.value is IConvertible iconvertible)
            {
                if (typeT.IsGenericType && typeT.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    typeT = typeT.GetGenericArguments()[0];
                }

                try
                {
                    T result = (T)iconvertible.ToType(typeT, null);
                    return result;
                }
                catch
                {
                }
            }
            return defaultValue;
        }

        public T GetValue<T>()
        {
            return GetValue<T>(default(T));
        }

        //innerClass
        public class WzNodeCollection : IEnumerable<Wz_Node>
        {
            public WzNodeCollection(Wz_Node owner)

            {
                this.owner = owner;
                this.innerCollection = null;
            }

            private readonly Wz_Node owner;
            private InnerCollection innerCollection;

            public Wz_Node this[int index]
            {
                get { return this.innerCollection?[index]; }
            }

            public Wz_Node this[string key]
            {
                get { return this.innerCollection?[key]; }
            }

            public int Count
            {
                get { return this.innerCollection?.Count ?? 0; }
            }

            public Wz_Node Add(string nodeText)
            {
                this.EnsureInnerCollection();
                return this.innerCollection.Add(nodeText);
            }

            public void Add(Wz_Node item)
            {
                this.EnsureInnerCollection();
                this.innerCollection.Add(item);
            }

            public void Sort()
            {
                this.innerCollection?.Sort();
            }

            public void Sort<T>(Func<Wz_Node, T> getKeyFunc) where T : IComparable<T>
            {
                if (getKeyFunc == null)
                {
                    this.Sort();
                }
                else if (this.innerCollection != null)
                {
                    this.innerCollection.Sort(getKeyFunc);
                }
            }

            public void Trim()
            {
                this.innerCollection?.Trim();
            }

            public void Clear()
            {
                this.innerCollection?.Clear();
            }

            public IEnumerator<Wz_Node> GetEnumerator()
            {
                return this.innerCollection?.GetEnumerator() ?? System.Linq.Enumerable.Empty<Wz_Node>().GetEnumerator();
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            private void EnsureInnerCollection()
            {
                if (this.innerCollection == null)
                {
                    this.innerCollection = new InnerCollection(this.owner);
                }
            }

            private class InnerCollection : KeyedCollection<string, Wz_Node>
            {
                public InnerCollection(Wz_Node owner)
                    : base(null, 12)
                {
                    this.parentNode = owner;
                }

                public Wz_Node Add(string nodeText)
                {
                    Wz_Node newNode = new Wz_Node(nodeText);
                    this.Add(newNode);
                    return newNode;
                }

                public new void Add(Wz_Node item)
                {
                    base.Add(item);
                    if (item.parentNode != null)
                    {
                        int index = item.parentNode.nodes.innerCollection.Items.IndexOf(item);
                        if (index > -1)
                        {
                            item.parentNode.nodes.innerCollection.RemoveItem(index);
                        }
                    }
                    item.parentNode = this.parentNode;
                }

                protected override void RemoveItem(int index)
                {
                    var item = this[index];
                    if (item != null)
                    {
                        item.parentNode = null;
                    }
                    base.RemoveItem(index);
                }

                private readonly Wz_Node parentNode;

                public void Sort()
                {
                    (base.Items as List<Wz_Node>)?.Sort();
                }

                public void Sort<T>(Func<Wz_Node, T> getKeyFunc) where T : IComparable<T>
                {
                    ListSorter.Sort(base.Items as List<Wz_Node>, getKeyFunc);
                }

                public void Trim()
                {
                    (base.Items as List<Wz_Node>)?.TrimExcess();
                }

                public new Wz_Node this[string key]
                {
                    get
                    {
                        if (key == null)
                        {
                            return null;
                        }
                        if (this.Dictionary != null)
                        {
                            Wz_Node node;
                            this.Dictionary.TryGetValue(key, out node);
                            return node;
                        }
                        else
                        {
                            List<Wz_Node> list = this.Items as List<Wz_Node>;
                            foreach (var node in list)
                            {
                                if (this.Comparer.Equals(this.GetKeyForItem(node), key))
                                {
                                    return node;
                                }
                            }
                            return null;
                        }
                    }
                }

                protected override string GetKeyForItem(Wz_Node item)
                {
                    return item.text;
                }
            }

            internal static class ListSorter
            {
                public static void Sort<T, TKey>(List<T> list, Func<T, TKey> getKeyFunc)
                {
                    T[] innerArray = list.GetType()
                        .GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)
                        .GetValue(list) as T[];

                    TKey[] keys = new TKey[list.Count];

                    for (int i = 0; i < keys.Length; i++)
                    {
                        keys[i] = getKeyFunc(innerArray[i]);
                    }

                    Array.Sort(keys, innerArray, 0, keys.Length);
                }
            }
        }

        public object Clone()
        {
            Wz_Node newNode = new Wz_Node(this.text);
            newNode.value = this.value;
            foreach (Wz_Node node in this.nodes)
            {
                Wz_Node newChild = node.Clone() as Wz_Node;
                newNode.nodes.Add(newChild);
            }
            return newNode;
        }

        int IComparable.CompareTo(object obj)
        {
            return ((IComparable<Wz_Node>)this).CompareTo(obj as Wz_Node);
        }

        int IComparable<Wz_Node>.CompareTo(Wz_Node other)
        {
            if (other != null)
            {
                return string.Compare(this.Text, other.Text, StringComparison.Ordinal);
            }
            else
            {
                return 1;
            }
        }
    }

    public static class Wz_NodeExtension
    {
        public static T GetValueEx<T>(this Wz_Node node, T defaultValue)
        {
            if (node == null)
                return defaultValue;
            return node.GetValue<T>(defaultValue);
        }

        public static T? GetValueEx<T>(this Wz_Node node) where T : struct
        {
            if (node == null)
                return null;
            return node.GetValue<T>();
        }

        public static Wz_Node ResolveUol(this Wz_Node node)
        {
            if (node == null)
                return null;
            Wz_Uol uol;
            while ((uol = node?.GetValueEx<Wz_Uol>(null)) != null)
            {
                node = uol.HandleUol(node);
            }
            return node;
        }

        /// <summary>
        /// 搜索node所属的wz_file，若搜索不到则返回null。
        /// </summary>
        /// <param Name="node">要搜索的wznode。</param>
        /// <returns></returns>
        public static Wz_File GetNodeWzFile(this Wz_Node node, bool returnClosestWzFile = false)
        {
            Wz_File wzfile = null;
            while (node != null)
            {
                if ((wzfile = node.Value as Wz_File) != null)
                {
                    if (wzfile.OwnerWzFile != null)
                    {
                        wzfile = wzfile.OwnerWzFile;
                        node = wzfile.Node;
                    }
                    if (!wzfile.IsSubDir || returnClosestWzFile)
                    {
                        break;
                    }
                }
                else if (node.Value is Wz_Image wzImg
                    || (wzImg = (node as Wz_Image.Wz_ImageNode)?.Image) != null)
                {
                    wzfile = GetImageWzFile(wzImg, returnClosestWzFile);
                    break;
                }
                node = node.ParentNode;
            }
            return wzfile;
        }

        public static Wz_File GetImageWzFile(this Wz_Image wzImg, bool returnClosestWzFile = false)
        {
            if (!returnClosestWzFile && wzImg.WzFile != null)
            {
                return GetNodeWzFile(wzImg.WzFile.Node, returnClosestWzFile);
            }

            return wzImg.WzFile;
        }

        public static int GetMergedVersion(this Wz_File wzFile)
        {
            if (wzFile.Header.WzVersion != 0)
            {
                return wzFile.Header.WzVersion;
            }
            foreach (var subFile in wzFile.MergedWzFiles)
            {
                if (subFile.Header.WzVersion != 0)
                {
                    return subFile.Header.WzVersion;
                }
            }
            return 0;
        }

        public static Wz_Image GetNodeWzImage(this Wz_Node node)
        {
            Wz_Image wzImg = null;
            while (node != null)
            {
                if ((wzImg = node.Value as Wz_Image) != null
                    || (wzImg = (node as Wz_Image.Wz_ImageNode)?.Image) != null)
                {
                    break;
                }
                node = node.ParentNode;
            }
            return wzImg;
        }

        public static void DumpAsXml(this Wz_Node node, XmlWriter writer)
        {
            object value = node.Value;

            if (value == null || value is Wz_Image)
            {
                writer.WriteStartElement("dir");
                writer.WriteAttributeString("name", node.Text);
            }
            else if (value is Wz_Png)
            {
                var png = (Wz_Png)value;
                writer.WriteStartElement("png");
                writer.WriteAttributeString("name", node.Text);
                using (var bmp = png.ExtractPng())
                {
                    using (var ms = new MemoryStream())
                    {
                        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        byte[] data = ms.ToArray();
                        writer.WriteAttributeString("value", Convert.ToBase64String(data));
                    }
                }
            }
            else if (value is Wz_Uol)
            {
                var uol = (Wz_Uol)value;
                writer.WriteStartElement("uol");
                writer.WriteAttributeString("name", node.Text);
                writer.WriteAttributeString("value", uol.Uol);
            }
            else if (value is Wz_Vector)
            {
                var vector = (Wz_Vector)value;
                writer.WriteStartElement("vector");
                writer.WriteAttributeString("name", node.Text);
                writer.WriteAttributeString("value", $"{vector.X}, {vector.Y}");
            }
            else if (value is Wz_Sound)
            {
                var sound = (Wz_Sound)value;
                writer.WriteStartElement("sound");
                writer.WriteAttributeString("name", node.Text);
                byte[] data = sound.ExtractSound();
                if (data == null)
                {
                    data = new byte[sound.DataLength];
                    sound.WzFile.FileStream.Seek(sound.Offset, SeekOrigin.Begin);
                    sound.WzFile.FileStream.Read(data, 0, sound.DataLength);
                }
                writer.WriteAttributeString("value", Convert.ToBase64String(data));
            }
            else
            {
                var tag = value.GetType().Name.ToLower();
                writer.WriteStartElement(tag);
                writer.WriteAttributeString("name", node.Text);
                writer.WriteAttributeString("value", value.ToString());
            }

            //输出子节点
            foreach (var child in node.Nodes)
            {
                DumpAsXml(child, writer);
            }

            //结束标识
            writer.WriteEndElement();
        }

        public static void SortByImgID(this Wz_Node.WzNodeCollection nodes)
        {
            if (regexImgID == null)
            {
                regexImgID = new Regex(@"^(\d+)\.img$", RegexOptions.Compiled);
            }

            nodes.Sort(GetKey);
        }

        private static Regex regexImgID;

        private static SortKey GetKey(Wz_Node node)
        {
            var key = new SortKey();
            var m = regexImgID.Match(node.Text);
            if (m.Success)
            {
                key.HasID = Int32.TryParse(m.Result("$1"), out key.ImgID);
            }
            key.Text = node.Text;
            return key;
        }

        private struct SortKey : IComparable<SortKey>
        {
            public bool HasID;
            public int ImgID;
            public string Text;

            public int CompareTo(SortKey other)
            {
                if (this.HasID && other.HasID) return this.ImgID.CompareTo(other.ImgID);
                return StringComparer.Ordinal.Compare(this.Text, other.Text);
            }
        }
    }

    public static class ObjectConverter
    {
        private static readonly Dictionary<Type, Delegate> cache = new Dictionary<Type, Delegate>();
        private delegate bool TryParseFunc<T>(string s, out T value);

        public static bool TryParse<T>(string s, out T value, out bool hasTryParse)
        {
            var typeT = typeof(T);

            TryParseFunc<T> tryParseFunc = null;
            if (!cache.TryGetValue(typeT, out var dele))
            {
                bool isNullable = false;
                Type innerType;
                if (typeT.IsGenericType && typeT.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    isNullable = true;
                    innerType = typeT.GetGenericArguments()[0];
                }
                else
                {
                    innerType = typeT;
                }

                var methodInfo = innerType.GetMethod("TryParse",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(string), innerType.MakeByRefType() },
                    null);

                if (methodInfo != null && methodInfo.ReturnType == typeof(bool))
                {
                    if (isNullable)
                    {
                        dele = Delegate.CreateDelegate(typeof(TryParseFunc<>).MakeGenericType(innerType), methodInfo);
                        var proxyType = typeof(NullableTryParse<>).MakeGenericType(innerType);
                        var proxyInstance = Activator.CreateInstance(proxyType, dele);
                        var proxyParseFunc = proxyType.GetMethod("TryParse", BindingFlags.Public | BindingFlags.Instance);
                        cache[typeT] = tryParseFunc = (TryParseFunc<T>)Delegate.CreateDelegate(typeof(TryParseFunc<T>), proxyInstance, proxyParseFunc);
                    }
                    else
                    {
                        cache[typeT] = tryParseFunc = (TryParseFunc<T>)Delegate.CreateDelegate(typeof(TryParseFunc<T>), methodInfo);
                    }
                }
                else
                {
                    cache[typeT] = null;
                }
            }
            else
            {
                tryParseFunc = dele as TryParseFunc<T>;
            }

            if (tryParseFunc != null)
            {
                hasTryParse = true;
                return tryParseFunc(s, out value);
            }
            else
            {
                hasTryParse = false;
                value = default(T);
                return false;
            }
        }

        private class NullableTryParse<T> where T : struct
        {
            public NullableTryParse(TryParseFunc<T> func)
            {
                this.func = func;
            }

            private readonly TryParseFunc<T> func;

            public bool TryParse(string s, out T? value)
            {
                if (this.func(s, out var v))
                {
                    value = v;
                    return true;
                }
                else
                {
                    value = default(T?);
                    return false;
                }
            }
        }
    }
}