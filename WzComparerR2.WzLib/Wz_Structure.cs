using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace WzComparerR2.WzLib
{
    public class Wz_Structure
    {
        public Wz_Structure()
        {
            this.wz_files = new List<Wz_File>();
            this.encryption = new Wz_Crypto();
            this.img_number = 0;
            this.has_basewz = false;
            this.TextEncoding = DefaultEncoding;
            this.AutoDetectExtFiles = DefaultAutoDetectExtFiles;
        }

        public List<Wz_File> wz_files;
        public Wz_Crypto encryption;
        public Wz_Node WzNode;
        public int img_number;
        public bool has_basewz;
        public bool sorted;

        public Encoding TextEncoding { get; set; }
        public bool AutoDetectExtFiles { get; set; }

        public void Clear()
        {
            foreach (Wz_File f in this.wz_files)
            {
                f.Close();
            }
            this.wz_files.Clear();
            this.encryption.Reset();
            this.img_number = 0;
            this.has_basewz = false;
            this.WzNode = null;
            this.sorted = false;
        }

        public void calculate_img_count()
        {
            foreach (Wz_File f in this.wz_files)
            {
                this.img_number += f.ImageCount;
            }
        }

        public void Load(string fileName)
        {
            this.Load(fileName, true);
        }

        public void Load(string fileName, bool useBaseWz)
        {
            //现在我们已经不需要list了
            this.WzNode = new Wz_Node(Path.GetFileName(fileName));
            if (Path.GetFileName(fileName).ToLower() == "list.wz")
            {
                this.encryption.LoadListWz(Path.GetDirectoryName(fileName));
                foreach (string list in this.encryption.List)
                {
                    WzNode.Nodes.Add(list);
                }
            }
            else
            {
                LoadFile(fileName, WzNode, useBaseWz);
            }
            calculate_img_count();
        }

        public void LoadFile(string fileName, Wz_Node node)
        {
            this.LoadFile(fileName, node, true);
        }

        public void LoadFile(string fileName, Wz_Node node, bool useBaseWz)
        {
            Wz_File file;

            try
            {
                file = new Wz_File(fileName, this);
                file.TextEncoding = this.TextEncoding;
                if (!this.encryption.encryption_detected)
                {
                    this.encryption.DetectEncryption(file);
                }
                this.wz_files.Add(file);
                node.Value = file;
                file.Node = node;
                file.FileStream.Position = 62;
                file.GetDirTree(node, useBaseWz);
                file.DetectWzType();
                file.DetectWzVersion();
            }
            catch
            {
                throw;
            }
        }

        #region Global Settings
        public static Encoding DefaultEncoding
        {
            get { return _defaultEncoding ?? Encoding.Default; }
            set { _defaultEncoding = value; }
        }

        private static Encoding _defaultEncoding;

        public static bool DefaultAutoDetectExtFiles { get; set; }
        #endregion
    }
}
