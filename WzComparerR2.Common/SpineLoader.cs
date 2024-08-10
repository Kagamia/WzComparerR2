using System;
using System.Collections.Generic;
using System.IO;
using WzComparerR2.WzLib;

namespace WzComparerR2.Common
{
    public static class SpineLoader
    {
        private const string AtlasExtension = ".atlas";
        private const string JsonExtension = ".json";
        private const string SkelExtension = ".skel";
        private const string SharedAtlasNodeName = "atlas";

        public static SpineDetectionResult Detect(Wz_Node wzNode)
        {
            if (wzNode == null || wzNode.ParentNode == null)
            {
                return SpineDetectionResult.Failed("WzNode or its parent cannot be null.");
            }
           
            Wz_Node parentNode = wzNode.ParentNode;
            Wz_Node atlasNode = null;
            Wz_Node skelNode = null;
            SkeletonLoadType loadType;
            SpineVersion spineVersion;

            if (wzNode.Text.EndsWith(AtlasExtension)) // detect from atlasNode
            {
                atlasNode = wzNode;
                string spineName = atlasNode.Text.Substring(0, atlasNode.Text.Length - AtlasExtension.Length);

                // find skel node in sibling nodes
                if ((skelNode = parentNode.Nodes[spineName + JsonExtension]) != null)
                {
                    loadType = SkeletonLoadType.Json;
                }
                else if ((skelNode = parentNode.Nodes[spineName] ?? parentNode.Nodes[spineName + SkelExtension]) != null)
                {
                    loadType = SkeletonLoadType.Binary;
                }
                else
                {
                    return SpineDetectionResult.Failed("Failed to find skel node.");
                }
            }
            else // detect from skel node
            {
                skelNode = wzNode;
                string spineName = null;
                if (skelNode.Text.EndsWith(JsonExtension))
                {
                    spineName = skelNode.Text.Substring(0, skelNode.Text.Length - JsonExtension.Length);
                    loadType = SkeletonLoadType.Json;
                }
                else if (skelNode.Text.EndsWith(SkelExtension))
                {
                    spineName = skelNode.Text.Substring(0, skelNode.Text.Length - SkelExtension.Length);
                    loadType = SkeletonLoadType.Binary;
                }
                else
                {
                    switch (skelNode.ResolveUol()?.Value)
                    {
                        case Wz_Sound sound when sound.SoundType == Wz_SoundType.Binary:
                        case Wz_RawData rawData:
                            spineName = skelNode.Text;
                            loadType = SkeletonLoadType.Binary;
                            break;

                        default:
                            return SpineDetectionResult.Failed("Failed to infer the wzNode as atlasNode or skelNode.");
                    }
                }

                if (spineName != null)
                {
                    // find atlas node in sibling nodes
                    // KMST 1172: the atlas node name could be constant
                    atlasNode = parentNode.Nodes[spineName + AtlasExtension] ?? parentNode.Nodes[SharedAtlasNodeName];
                    if (atlasNode == null)
                    {
                        return SpineDetectionResult.Failed("Failed to find atlas node.");
                    }
                }
            }

            // resolve uols
            if ((atlasNode = atlasNode.ResolveUol()) == null)
            {
                return SpineDetectionResult.Failed("Failed to resolve uol for atlasNode.");
            }
            if ((skelNode = skelNode.ResolveUol()) == null)
            {
                return SpineDetectionResult.Failed("Failed to resolve uol for skelNode.");
            }

            // check atlas data type
            if (atlasNode.Value is not string)
            {
                return SpineDetectionResult.Failed("AtlasNode does not contain a string value.");
            }

            // inference spine version
            string versionStr = null;
            switch (loadType)
            {
                case SkeletonLoadType.Json when skelNode.Value is string json:
                    versionStr = ReadSpineVersionFromJson(json);
                    break;

                case SkeletonLoadType.Binary when skelNode.Value is Wz_Sound wzSound && wzSound.SoundType == Wz_SoundType.Binary:
                case SkeletonLoadType.Binary when skelNode.Value is Wz_RawData wzRawData:
                    var blob = skelNode.Value as IMapleStoryBlob;
                    var data = new byte[blob.Length];
                    blob.CopyTo(data, 0);
                    var ms = new MemoryStream(data);
                    versionStr = ReadSpineVersionFromBinary(ms, 0, blob.Length);
                    break;
            }

            if (versionStr == null)
            {
                return SpineDetectionResult.Failed($"Failed to read version string from skel {loadType}.");
            }
            if (!Version.TryParse(versionStr, out var version))
            {
                return SpineDetectionResult.Failed($"Failed to parse version '{versionStr}'.");
            }

            switch (version.Major)
            {
                case 2: spineVersion = SpineVersion.V2; break;
                case 4: spineVersion = SpineVersion.V4; break;
                default: return SpineDetectionResult.Failed($"Spine version '{versionStr}' is not supported."); ;
            }

            return SpineDetectionResult.Create(wzNode, atlasNode, skelNode, loadType, spineVersion);
        }

        private static string ReadSpineVersionFromJson(string jsonText)
        {
            // { "skeleton": { "spine": "2.1.27" } }
            using var sr = new StringReader(jsonText);
            object skelObj = Spine.Json.Deserialize(sr);
            if (skelObj is IDictionary<string, object> jRootDict
                && jRootDict.TryGetValue("skeleton", out var jSkeleton)
                && jSkeleton is IDictionary<string, object> jSkeletonDict
                && jSkeletonDict.TryGetValue("spine", out var jSpine)
                && Version.TryParse(jSpine as string, out var spineVer))
            {
                return jSpine as string;
            }
            return null;
        }

        private static string ReadSpineVersionFromBinary(Stream stream, uint offset, int length)
        {
            /* 
             * v4 format:
             * 00-07 hash
             * 08    version len
             * 09-XX version (len-1 bytes)
             * 
             * v2 format:
             * 00        hash len
             * 01-XX     hash (len-1 bytes)
             * (XX+1)    version len
             * (XX+2)-YY version (len-1 bytes) 
             */

            long oldPos = stream.Position;
            try
            {
                stream.Position = offset;
                // this method can detect version from v4 and pre-v3 file format.
                string version = Spine.SkeletonBinary.GetVersionString(stream);
                return version;
            }
            catch 
            {
                // ignore error;
                return null;
            }
            finally 
            { 
                stream.Position = oldPos;
            }
        }

        public static Spine.V2.SkeletonData LoadSkeletonV2(Wz_Node wzNode, Spine.V2.TextureLoader textureLoader)
        {
            var detectionResult = Detect(wzNode);
            if (detectionResult.Success && detectionResult.Version == SpineVersion.V2)
            {
                return LoadSkeletonV2(detectionResult, textureLoader);
            }
            return null;
        }

        public static Spine.V2.SkeletonData LoadSkeletonV2(SpineDetectionResult detectionResult, Spine.V2.TextureLoader textureLoader)
        {
            using var atlasReader = new StringReader((string)detectionResult.ResolvedAtlasNode.Value);
            var atlas = new Spine.V2.Atlas(atlasReader, "", textureLoader);

            switch (detectionResult.LoadType)
            {
                case SkeletonLoadType.Json:
                    using (var skeletonReader = new StringReader((string)detectionResult.ResolvedSkelNode.Value))
                    {
                        var skeletonJson = new Spine.V2.SkeletonJson(atlas);
                        return skeletonJson.ReadSkeletonData(skeletonReader);
                    }

                case SkeletonLoadType.Binary when detectionResult.ResolvedSkelNode.Value is IMapleStoryBlob blob:
                    byte[] data = new byte[blob.Length];
                    blob.CopyTo(data, 0);
                    var ms = new MemoryStream(data);
                    var skeletonBinary = new Spine.V2.SkeletonBinary(atlas);
                    return skeletonBinary.ReadSkeletonData(ms);

                default:
                    return null;
            }
        }

        public static Spine.SkeletonData LoadSkeletonV4(Wz_Node atlasOrSkelNode, Spine.TextureLoader textureLoader)
        {
            var detectionResult = Detect(atlasOrSkelNode);
            if (detectionResult.Success && detectionResult.Version == SpineVersion.V4)
            {
                return LoadSkeletonV4(detectionResult, textureLoader);
            }
            return null;
        }

        public static Spine.SkeletonData LoadSkeletonV4(SpineDetectionResult detectionResult, Spine.TextureLoader textureLoader)
        {
            using var atlasReader = new StringReader((string)detectionResult.ResolvedAtlasNode.Value);
            var atlas = new Spine.Atlas(atlasReader, "", textureLoader);

            switch (detectionResult.LoadType)
            {
                case SkeletonLoadType.Json:
                    using (var skeletonReader = new StringReader((string)detectionResult.ResolvedSkelNode.Value))
                    {
                        var skeletonJson = new Spine.SkeletonJson(atlas);
                        return skeletonJson.ReadSkeletonData(skeletonReader);
                    }

                case SkeletonLoadType.Binary when detectionResult.ResolvedSkelNode.Value is IMapleStoryBlob blob:
                    byte[] data = new byte[blob.Length];
                    blob.CopyTo(data, 0);
                    var ms = new MemoryStream(data);
                    var skeletonBinary = new Spine.SkeletonBinary(atlas);
                    return skeletonBinary.ReadSkeletonData(ms);

                default:
                    return null;
            }
        }
    }

    public enum SkeletonLoadType
    {
        None = 0,
        Json = 1,
        Binary = 2,
    }

    public enum SpineVersion
    {
        Unknown = 0,
        V2 = 2,
        V4 = 4
    }

    public sealed class SpineDetectionResult
    {
        internal SpineDetectionResult()
        {
        }

        public bool Success { get; internal set; }
        public string ErrorDetail { get; internal set; }
        public Wz_Node SourceNode { get; internal set; }
        public Wz_Node ResolvedAtlasNode { get; internal set; }
        public Wz_Node ResolvedSkelNode { get; internal set; }
        public SkeletonLoadType LoadType { get; internal set; }
        public SpineVersion Version { get; internal set; }

        public static SpineDetectionResult Failed(string error = null) => new SpineDetectionResult
        {
            Success = false,
            ErrorDetail = error,
        };

        public static SpineDetectionResult Create(Wz_Node sourceNode, Wz_Node atlasNode, Wz_Node skelNode, SkeletonLoadType loadType, SpineVersion version) => new SpineDetectionResult
        {
            Success = true,
            ErrorDetail = null,
            SourceNode = sourceNode,
            ResolvedAtlasNode = atlasNode,
            ResolvedSkelNode = skelNode,
            LoadType = loadType,
            Version = version
        };
    }
}
