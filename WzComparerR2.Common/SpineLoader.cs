using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using WzComparerR2.WzLib;
using Spine;

namespace WzComparerR2.Common
{
    public static class SpineLoader
    {
        public static SkeletonData LoadSkeleton(Wz_Node atlasNode, SkeletonLoadType loadType, TextureLoader textureLoader)
        {
            string atlasData = atlasNode.GetValueEx<string>(null);
            if (string.IsNullOrEmpty(atlasData))
            {
                return null;
            }
            StringReader atlasReader = new StringReader(atlasData);

            Atlas atlas = new Atlas(atlasReader, "", textureLoader);
            SkeletonData skeletonData;
            //加载skeleton
            switch (loadType)
            {
                case SkeletonLoadType.Json:
                    if (!TryLoadSkeletonJson(atlasNode, atlas, out skeletonData))
                    {
                        goto _failed;
                    }
                    break;

                case SkeletonLoadType.Binary:
                    if (!TryLoadSkeletonBinary(atlasNode, atlas, out skeletonData))
                    {
                        goto _failed;
                    }
                    break;

                default:
                case SkeletonLoadType.Auto:
                    if (!TryLoadSkeletonJson(atlasNode, atlas, out skeletonData)
                        && !TryLoadSkeletonBinary(atlasNode, atlas, out skeletonData))
                    {
                        goto _failed;
                    }
                    break;
            }

            return skeletonData;

            _failed:
            if (atlas != null)
            {
                atlas.Dispose();
            }
            return null;
        }

        private static bool TryLoadSkeletonJson(Wz_Node atlasNode, Atlas atlas, out SkeletonData data)
        {
            data = null;

            if (atlasNode == null || atlasNode.ParentNode == null || atlas == null)
            {
                return false;
            }

            var m = Regex.Match(atlasNode.Text, @"^(.+)\.atlas$", RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                return false;
            }
            var skeletonSource = atlasNode.ParentNode.FindNodeByPath(m.Result("$1") + ".json").GetValueEx<string>(null);
            if (string.IsNullOrEmpty(skeletonSource))
            {
                return false;
            }

            StringReader skeletonReader = new StringReader(skeletonSource);
            SkeletonJson json = new SkeletonJson(atlas);
            data = json.ReadSkeletonData(skeletonReader);
            return true;
        }

        private static bool TryLoadSkeletonBinary(Wz_Node atlasNode, Atlas atlas, out SkeletonData data)
        {
            data = null;

            if (atlasNode == null || atlasNode.ParentNode == null || atlas == null)
            {
                return false;
            }

            var m = Regex.Match(atlasNode.Text, @"^(.+)\.atlas$", RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                return false;
            }

            var node = atlasNode.ParentNode.FindNodeByPath(m.Result("$1"));
            Wz_Uol uol;
            while ((uol = node.GetValueEx<Wz_Uol>(null)) != null)
            {
                node = uol.HandleUol(node);
            }
            var skeletonSource = node.GetValueEx<Wz_Sound>(null);
            if (skeletonSource == null || skeletonSource.SoundType != Wz_SoundType.Binary)
            {
                return false;
            }

            byte[] buffer = new byte[skeletonSource.DataLength];
            skeletonSource.WzFile.FileStream.Seek(skeletonSource.Offset, SeekOrigin.Begin);
            if (skeletonSource.WzFile.FileStream.Read(buffer, 0, buffer.Length) != buffer.Length)
            {
                return false;
            }
            MemoryStream ms = new MemoryStream(buffer);

            SkeletonBinary binary = new SkeletonBinary(atlas);
            data = binary.ReadSkeletonData(ms);
            return true;
        }
    }

    public enum SkeletonLoadType
    {
        Auto = 0,
        Json = 1,
        Binary = 2
    }
}
