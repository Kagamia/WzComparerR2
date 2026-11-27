using System;
using System.Collections.Generic;
using WzComparerR2.WzLib;

namespace WzComparerR2.UnityLib.Classic
{
    /// <summary>
    /// Represents a Unity dataset sound img (<c>Sound/&lt;img&gt;</c>) in the WZ
    /// <c>Sound/&lt;img&gt;.img</c> form: the content is nested directories plus <see cref="Wz_Sound"/> leaf nodes,
    /// matching the classic WZ layout.
    /// <para>
    /// The sound data (FSB5) is repackaged into Ogg Vorbis on first use,
    /// so it works directly with <see cref="Wz_Sound.ExtractSound"/>, playback, and export.
    /// </para>
    /// </summary>
    public sealed class UnitySoundImage : UnityAssetImage
    {
        public UnitySoundImage(ClassicWzSource source, string imageKey)
            : base(source, imageKey)
        {
        }

        /// <summary>Finds a node in the same dataset using a WZ-style path (the <c>.img</c> extension is optional).</summary>
        public Wz_Node FindNode(string wzPath)
        {
            return this.Source.FindNode(wzPath);
        }

        protected override void PopulateContent()
        {
            IReadOnlyList<string> soundKeys = this.Source.Catalog.GetSoundKeys(this.Name);
            string prefixLength = this.Name + "/";
            foreach (string soundKey in soundKeys)
            {
                if (!soundKey.StartsWith(prefixLength, StringComparison.Ordinal))
                {
                    continue;
                }

                string[] segments = soundKey.Substring(prefixLength.Length).Split('/');
                Wz_Node parent = this.Node;
                for (int i = 0; i < segments.Length - 1; i++)
                {
                    parent = GetOrAddFolder(parent, segments[i]);
                }

                string name = segments[segments.Length - 1];
                if (parent.Nodes[name] == null)
                {
                    Wz_Node soundNode = new Wz_Node(name);
                    soundNode.Value = this.Source.CreateSound(soundKey);
                    parent.Nodes.Add(soundNode);
                }
            }
        }

        private static Wz_Node GetOrAddFolder(Wz_Node parent, string name)
        {
            Wz_Node folder = parent.Nodes[name];
            if (folder == null)
            {
                folder = new Wz_Node(name);
                parent.Nodes.Add(folder);
            }
            return folder;
        }
    }
}
