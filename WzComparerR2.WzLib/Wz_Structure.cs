using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

namespace WzComparerR2.WzLib
{
    public class Wz_Structure
    {
        public Wz_Structure()
        {
            this.wz_files = new List<Wz_File>();
            this.ms_files = new List<Ms_File>();
            this.encryption = new Wz_Crypto();
            this.img_number = 0;
            this.has_basewz = false;
            this.TextEncoding = Wz_Structure.DefaultEncoding;
            this.AutoDetectExtFiles = Wz_Structure.DefaultAutoDetectExtFiles;
            this.ImgCheckDisabled = Wz_Structure.DefaultImgCheckDisabled;
            this.WzVersionVerifyMode = Wz_Structure.DefaultWzVersionVerifyMode;
        }

        public List<Wz_File> wz_files;
        public List<Ms_File> ms_files;
        public Wz_Crypto encryption;
        public Wz_Node WzNode;
        public int img_number;
        public bool has_basewz;
        public bool sorted; //暂时弃用

        public Encoding TextEncoding { get; set; }
        public bool AutoDetectExtFiles { get; set; }
        public bool ImgCheckDisabled { get; set; }
        public WzVersionVerifyMode WzVersionVerifyMode {get;set;}

        public void Clear()
        {
            foreach (Wz_File f in this.wz_files)
            {
                f.Close();
            }
            this.wz_files.Clear();
            foreach (Ms_File f in this.ms_files)
            {
                f.Close();
            }
            this.ms_files.Clear();
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

        public void Load(string fileName, bool useBaseWz = false)
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

        public Wz_File LoadFile(string fileName, Wz_Node node, bool useBaseWz = false, bool loadWzAsFolder = false)
        {
            Wz_File file = null;

            try
            {
                file = new Wz_File(fileName, this);
                if (!file.Loaded)
                {
                    throw new Exception("The file is not a valid wz file.");
                }
                this.wz_files.Add(file);
                file.TextEncoding = this.TextEncoding;
                if (!this.encryption.encryption_detected)
                {
                    this.encryption.DetectEncryption(file);
                }
                node.Value = file;
                file.Node = node;
                file.FileStream.Position = file.Header.DataStartPosition;
                file.GetDirTree(node, useBaseWz, loadWzAsFolder);
                file.Header.DirEndPosition = file.FileStream.Position;
                file.DetectWzType();
                file.DetectWzVersion();
                return file;
            }
            catch
            {
                if (file != null)
                {
                    file.Close();
                    this.wz_files.Remove(file);
                }
                throw;
            }
        }

        public void LoadImg(string fileName)
        {
            this.WzNode = new Wz_Node(Path.GetFileName(fileName));
            this.LoadImg(fileName, WzNode);
        }

        public void LoadImg(string fileName, Wz_Node node)
        {
            Wz_File file = null;

            try
            {
                file = new Wz_File(fileName, this);
                file.TextEncoding = this.TextEncoding;
                file.Node = node;
                var imgNode = new Wz_Node(node.Text);
                //跳过checksum检测
                var img = new Wz_Image(node.Text, (int)file.FileStream.Length, 0, 0, 0, file)
                {
                    OwnerNode = imgNode,
                    Offset = 0,
                    IsChecksumChecked = true
                };
                imgNode.Value = img;

                node.Nodes.Add(imgNode);
                node.Value = file;
                this.wz_files.Add(file);
            }
            catch
            {
                file?.Close();
                throw;
            }
        }

        public void LoadKMST1125DataWz(string fileName)
        {
            LoadWzFolder(Path.GetDirectoryName(fileName), ref this.WzNode, true);
            calculate_img_count();
        }

        public bool IsKMST1125WzFormat(string fileName)
        {
            if (!string.Equals(Path.GetExtension(fileName), ".wz", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string iniFile = Path.ChangeExtension(fileName, ".ini");
            if (!File.Exists(iniFile))
            {
                return false;
            }

            // check if the file is an empty wzfile
            using (var file = new Wz_File(fileName, this))
            {
                if (!file.Loaded)
                {
                    return false;
                }
                var tempNode = new Wz_Node();
                if (!this.encryption.encryption_detected)
                {
                    this.encryption.DetectEncryption(file);
                }
                file.FileStream.Position = file.Header.DataStartPosition;
                file.GetDirTree(tempNode);
                return file.ImageCount == 0;
            }
        }

        public void LoadWzFolder(string folder, ref Wz_Node node, bool useBaseWz = false)
        {
            string baseName = Path.Combine(folder, Path.GetFileName(folder));
            string entryWzFileName = Path.ChangeExtension(baseName, ".wz");
            string iniFileName = Path.ChangeExtension(baseName, ".ini");
            Func<int, string> extraWzFileName = _index => Path.ChangeExtension($"{baseName}_{_index:D3}", ".wz");

            // load iniFile
            int? lastWzIndex = null;
            if (File.Exists(iniFileName))
            {
                var iniConf = File.ReadAllLines(iniFileName).Select(row =>
                {
                    string[] columns = row.Split('|');
                    string key = columns.Length > 0 ? columns[0] : null;
                    string value = columns.Length > 1 ? columns[1] : null;
                    return new KeyValuePair<string, string>(key, value);
                });
                if (int.TryParse(iniConf.FirstOrDefault(kv => kv.Key == "LastWzIndex").Value, out var indexFromIni))
                {
                    lastWzIndex = indexFromIni;
                }
            }

            // ini file missing or unexpected format
            if (lastWzIndex == null)
            {
                for (int i = 0; ; i++)
                {
                    string extraFile = extraWzFileName(i);
                    if (!File.Exists(extraFile))
                    {
                        break;
                    }
                    lastWzIndex = i;
                }
            }

            // load entry file
            if (node == null)
            {
                node = new Wz_Node(Path.GetFileName(entryWzFileName));
            }
            var entryWzf = this.LoadFile(entryWzFileName, node, useBaseWz, true);

            // load extra file
            if (lastWzIndex != null)
            {
                for (int i = 0, j = lastWzIndex.Value; i <= j; i++)
                {
                    string extraFile = extraWzFileName(i);
                    var tempNode = new Wz_Node(Path.GetFileName(extraFile));
                    var extraWzf = this.LoadFile(extraFile, tempNode, false, true);

                    /*
                     * there is a little hack here, we'll move all img to the entry file, and each img still refers to the original wzfile.
                     * before:
                     *   base.wz (Wz_File)
                     *   |- a.img (Wz_Image)
                     *   base_000.wz (Wz_File)
                     *   |- b.img (Wz_Image) { wz_f = base_000.wz }
                     *   
                     * after:
                     *   base.wz (Wz_File) { mergedFiles = [base_000.wz] }
                     *   |- a.img (Wz_Image)  { wz_f = base.wz }
                     *   |- b.img (Wz_Image)  { wz_f = base_000.wz }
                     *   
                     * this.wz_files references all opened files so they can be closed correctly.
                     */

                    entryWzf.MergeWzFile(extraWzf);
                }
            }
        }

        public void LoadMsFile(string fileName)
        {
            this.LoadMsFile(fileName, ref this.WzNode);
        }

        private void LoadMsFile(string fileName, ref Wz_Node node)
        {
            Ms_File file = null;
            if (node == null)
            {
                node = new Wz_Node(Path.GetFileName(fileName));
            }
            try
            {
                file = new Ms_File(fileName, this);
                file.ReadEntries();
                file.GetDirTree(node);
                this.ms_files.Add(file);
            }
            catch
            {
                if (file != null)
                {
                    file.Close();
                    this.ms_files.Remove(file);
                }
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

        public static bool DefaultImgCheckDisabled { get; set; }

        public static WzVersionVerifyMode DefaultWzVersionVerifyMode { get; set; }
        #endregion
    }
}
