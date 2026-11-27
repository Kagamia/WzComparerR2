using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using WzComparerR2.UnityLib.Unity;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// Basic Unity client information: name (PlayerSettings.productName), version
    /// (PlayerSettings.bundleVersion), and engine version (UnityFS header of the AssetBundle).
    /// <para>
    /// Data sources are used in this order: PlayerSettings in <c>&lt;Client&gt;_Data/globalgamemanagers</c>,
    /// <c>&lt;Client&gt;_Data/app.info</c>, and the Addressables <c>catalog_&lt;version&gt;.hash</c>
    /// in <c>StreamingAssets/aa/settings.json</c>. If a source is missing, the corresponding field is null.
    /// </para>
    /// </summary>
    public sealed class UnityClientInfo
    {
        private static readonly Regex CatalogVersionPattern = new Regex(
            @"catalog_([0-9][0-9A-Za-z._-]*)\.hash", RegexOptions.CultureInvariant);

        private static readonly Regex VersionTextPattern = new Regex(
            @"^[0-9]+(\.[0-9]+)+$", RegexOptions.CultureInvariant);

        private UnityClientInfo()
        {
        }

        /// <summary>Developer name (PlayerSettings.companyName).</summary>
        public string CompanyName { get; private set; }

        /// <summary>Client name (PlayerSettings.productName).</summary>
        public string ProductName { get; private set; }

        /// <summary>Client version (PlayerSettings.bundleVersion).</summary>
        public string Version { get; private set; }

        /// <summary>
        /// Engine version (AssetBundle unityRevision, e.g. <c>6000.3.11f1</c>).
        /// The first string in the UnityFS header is a formal placeholder (<c>5.x.x</c>); the real version is the second string.
        /// </summary>
        public string EngineVersion { get; private set; }

        /// <summary>Resolved <c>&lt;Client&gt;_Data</c> directory. Null if not found.</summary>
        public string DataFolder { get; private set; }

        /// <summary>Display string in the form "name version". Null when both parts are missing.</summary>
        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(this.ProductName))
                {
                    return string.IsNullOrEmpty(this.Version) ? null : this.Version;
                }
                return string.IsNullOrEmpty(this.Version)
                    ? this.ProductName
                    : this.ProductName + " " + this.Version;
            }
        }

        /// <summary>
        /// Resolves client information from the bundle directory (<c>StreamingAssets/aa/StandaloneWindows64</c>).
        /// Fields that cannot be found stay null; no exceptions are thrown.
        /// </summary>
        /// <param name="bundleDirectory">Directory containing the bundles (used to search upward for <c>*_Data</c> and <c>aa/settings.json</c>).</param>
        /// <param name="engineVersion">Engine version string (<see cref="UnityFsBundle.UnityRevision"/>; may be null).</param>
        public static UnityClientInfo Resolve(string bundleDirectory, string engineVersion)
        {
            UnityClientInfo info = new UnityClientInfo();
            info.EngineVersion = string.IsNullOrEmpty(engineVersion) ? null : engineVersion;

            try
            {
                string dataFolder = FindDataFolder(bundleDirectory);
                info.DataFolder = dataFolder;
                if (dataFolder != null)
                {
                    info.ApplyPlayerSettings(Path.Combine(dataFolder, "globalgamemanagers"));
                    if (string.IsNullOrEmpty(info.ProductName))
                    {
                        info.ApplyAppInfo(Path.Combine(dataFolder, "app.info"));
                    }
                }

                if (string.IsNullOrEmpty(info.Version))
                {
                    // The Addressables catalog file name contains Application.version, so this is a literal value, not a guess.
                    info.Version = ReadAddressablesVersion(bundleDirectory);
                }
                if (string.IsNullOrEmpty(info.Version) && dataFolder != null)
                {
                    info.ApplyPlayerSettingsVersion(Path.Combine(dataFolder, "globalgamemanagers"));
                }
            }
            catch (Exception)
            {
                // This information is display-only; a parse failure does not affect loading the dataset.
            }

            return info;
        }

        /// <summary>Opens a single bundle file, reads its engine version, and resolves the client information.</summary>
        public static UnityClientInfo ResolveFromBundle(string bundlePath)
        {
            string engineVersion = null;
            string directory = null;
            try
            {
                if (!string.IsNullOrEmpty(bundlePath) && File.Exists(bundlePath))
                {
                    directory = Path.GetDirectoryName(bundlePath);
                    using (UnityFsBundle bundle = UnityFsBundle.Open(bundlePath))
                    {
                        engineVersion = bundle.UnityRevision;
                    }
                }
            }
            catch (Exception)
            {
            }
            return Resolve(directory, engineVersion);
        }

        /// <summary>Walks upward from the <c>*_Data</c> directory to find the client folder that the bundle directory belongs to.</summary>
        private static string FindDataFolder(string bundleDirectory)
        {
            if (string.IsNullOrEmpty(bundleDirectory))
            {
                return null;
            }
            string current = bundleDirectory;
            for (int i = 0; i < 5 && !string.IsNullOrEmpty(current); i++)
            {
                if (File.Exists(Path.Combine(current, "globalgamemanagers"))
                    || File.Exists(Path.Combine(current, "app.info")))
                {
                    return current;
                }
                DirectoryInfo parent = Directory.GetParent(current);
                current = parent?.FullName;
                if (current != null && current.EndsWith("_Data", StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(Path.Combine(current, "globalgamemanagers"))
                        || File.Exists(Path.Combine(current, "app.info")))
                    {
                        return current;
                    }
                }
            }
            return null;
        }

        /// <summary>Reads PlayerSettings (class 129) to obtain companyName / productName / bundleVersion.</summary>
        private void ApplyPlayerSettings(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            byte[] data = File.ReadAllBytes(path);
            UnitySerializedFile file = UnitySerializedFile.Parse(data);
            List<string> strings = null;
            foreach (UnityObjectInfo obj in file.Objects)
            {
                if (obj.ClassId != UnityClassIds.PlayerSettings)
                {
                    continue;
                }
                long start = file.DataOffset + obj.ByteStart;
                if (start < 0 || start + obj.ByteSize > data.Length)
                {
                    continue;
                }
                strings = ReadAlignedStrings(data, (int)start, (int)obj.ByteSize);
                break;
            }
            if (strings == null || strings.Count == 0)
            {
                return;
            }

            // The first two strings in PlayerSettings are companyName and productName.
            if (strings.Count >= 2)
            {
                this.CompanyName = strings[0];
                this.ProductName = strings[1];
            }
            else
            {
                this.ProductName = strings[0];
            }
        }

        private void ApplyPlayerSettingsVersion(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }
            try
            {
                byte[] data = File.ReadAllBytes(path);
                UnitySerializedFile file = UnitySerializedFile.Parse(data);
                foreach (UnityObjectInfo obj in file.Objects)
                {
                    if (obj.ClassId != UnityClassIds.PlayerSettings)
                    {
                        continue;
                    }
                    long start = file.DataOffset + obj.ByteStart;
                    if (start < 0 || start + obj.ByteSize > data.Length)
                    {
                        continue;
                    }
                    ApplyVersionFromStrings(
                        ReadAlignedStrings(data, (int)start, (int)obj.ByteSize), 0);
                    break;
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// bundleVersion is the last of several version strings in PlayerSettings (the earlier ones are
        /// per-platform version fields for iPhone/Android, etc.), so the "most complete" version string is chosen.
        /// </summary>
        private void ApplyVersionFromStrings(List<string> strings, int skip)
        {
            if (!string.IsNullOrEmpty(this.Version))
            {
                return;
            }
            string best = null;
            for (int i = skip; i < strings.Count; i++)
            {
                string text = strings[i];
                if (string.IsNullOrEmpty(text) || !VersionTextPattern.IsMatch(text))
                {
                    continue;
                }
                if (best == null || CountSegments(text) >= CountSegments(best))
                {
                    best = text;
                }
            }
            this.Version = best;
        }

        private static int CountSegments(string version)
        {
            int count = 1;
            foreach (char c in version)
            {
                if (c == '.')
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>app.info: line 1 is the company name, line 2 is the product name (UTF-8).</summary>
        private void ApplyAppInfo(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            if (lines.Length >= 1 && this.CompanyName == null)
            {
                this.CompanyName = lines[0].Trim();
            }
            if (lines.Length >= 2 && this.ProductName == null)
            {
                this.ProductName = lines[1].Trim();
            }
        }

        /// <summary>
        /// The Addressables remote catalog file name is <c>catalog_&lt;Application.version&gt;.hash</c>,
        /// so the client version can be obtained from <c>aa/settings.json</c>.
        /// </summary>
        private static string ReadAddressablesVersion(string bundleDirectory)
        {
            if (string.IsNullOrEmpty(bundleDirectory))
            {
                return null;
            }
            string settings = Path.Combine(bundleDirectory, "..", "settings.json");
            if (!File.Exists(settings))
            {
                settings = Path.Combine(bundleDirectory, "settings.json");
            }
            if (!File.Exists(settings))
            {
                return null;
            }

            string text = File.ReadAllText(settings, Encoding.UTF8);
            Match match = CatalogVersionPattern.Match(text);
            return match.Success ? match.Groups[1].Value : null;
        }

        /// <summary>Reads all printable strings laid out as "u32 length + content + 4-byte alignment" in the object.</summary>
        private static List<string> ReadAlignedStrings(byte[] data, int offset, int size)
        {
            List<string> strings = new List<string>();
            int end = offset + size;
            for (int pos = offset + 8; pos + 5 <= end; pos++)
            {
                string text;
                int next;
                if (TryReadString(data, pos, end, out text, out next) && text.Length > 0)
                {
                    strings.Add(text);
                    pos = next - 1;
                }
            }
            return strings;
        }

        /// <summary>Tries to read a length-prefixed string at <paramref name="pos"/>. On success, <paramref name="next"/> is the position after alignment.</summary>
        private static bool TryReadString(byte[] data, int pos, int end, out string text, out int next)
        {
            text = null;
            next = pos;
            int length = BitConverter.ToInt32(data, pos);
            if (length <= 0 || length > 512 || pos + 4 + length > end)
            {
                return false;
            }

            for (int i = 0; i < length; i++)
            {
                byte b = data[pos + 4 + i];
                if (b < 0x20 && b != '\r' && b != '\n' && b != '\t')
                {
                    return false;
                }
            }

            try
            {
                text = Utf8Strict.GetString(data, pos + 4, length);
            }
            catch (ArgumentException)
            {
                return false;
            }

            next = (pos + 4 + length + 3) & ~3;
            return true;
        }

        private static readonly UTF8Encoding Utf8Strict = new UTF8Encoding(false, true);
    }
}
