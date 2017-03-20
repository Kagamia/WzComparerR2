using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

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
                    if (node.value is Wz_File)
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
            if (typeof(Wz_Image) == typeof(T) && this is Wz_Image.Wz_ImageNode)
                return (T)(object)(((Wz_Image.Wz_ImageNode)this).Image);
            if (this.value == null)
                return defaultValue;
            if (this.value.GetType() == typeof(T))
                return (T)this.value;

            IConvertible iconvertible = this.value as IConvertible;
            if (iconvertible != null)
            {
                try
                {
                    T result = (T)iconvertible.ToType(typeof(T), null);
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
        public class WzNodeCollection : System.Collections.ObjectModel.KeyedCollection<string, Wz_Node>
        {
            public WzNodeCollection(Wz_Node baseNode)
                : base()
            {
                this.parentNode = baseNode;
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
                    int index = item.parentNode.nodes.Items.IndexOf(item);
                    if (index > -1)
                    {
                        item.parentNode.nodes.RemoveItem(index);
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

            private Wz_Node parentNode;

            public void Sort()
            {
                List<Wz_Node> lst = base.Items as List<Wz_Node>;
                if (lst != null)
                    lst.Sort();
            }

            public new Wz_Node this[string key]
            {
                get
                {
                    Wz_Node node;
                    if (key != null && this.Dictionary != null && this.Dictionary.TryGetValue(key, out node))
                        return node;
                    return null;
                }
            }

            protected override string GetKeyForItem(Wz_Node item)
            {
                if (item != null)
                    return item.text;
                return null;
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
                return string.Compare(this.Text, other.Text, StringComparison.InvariantCulture);
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

        /// <summary>
        /// 搜索node所属的wz_file，若搜索不到则返回null。
        /// </summary>
        /// <param Name="node">要搜索的wznode。</param>
        /// <returns></returns>
        public static Wz_File GetNodeWzFile(this Wz_Node node)
        {
            Wz_File wzfile = null;
            Wz_Image wzImg = null;
            while (node != null)
            {
                if ((wzfile = node.Value as Wz_File) != null)
                {
                    break;
                }
                if ((wzImg = node.Value as Wz_Image) != null
                    || (wzImg = (node as Wz_Image.Wz_ImageNode)?.Image) != null)
                {
                    wzfile = wzImg.WzFile;
                    break;
                }
                node = node.ParentNode;
            }
            return wzfile;
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
    }
}