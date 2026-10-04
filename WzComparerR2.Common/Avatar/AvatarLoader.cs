using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Drawing;
using WzComparerR2.CharaSim;
using WzComparerR2.WzLib;

namespace WzComparerR2.Avatar
{
    public sealed class AvatarLoader
    {
        public AvatarLoader(GlobalFindNodeFunction findNode)
        {
            this.findNode = findNode ?? throw new ArgumentNullException(nameof(findNode));
        }

        private readonly GlobalFindNodeFunction findNode;
        private Resources resources;

        public bool IsLoaded => this.resources != null;

        public IReadOnlyDictionary<string, int> ZMap => this.GetResources().ZMap;

        public IReadOnlyDictionary<string, IReadOnlyList<string>> SMap => this.GetResources().SMap;

        public IReadOnlyList<string> Actions => this.GetResources().BaseBody.Names;

        public void Load()
        {
            Wz_Node zNode = this.findNode("Base\\zmap.img").RequireImageNode("Base\\zmap.img");
            Wz_Node sNode = this.findNode("Base\\smap.img").RequireImageNode("Base\\smap.img");
            Wz_Node body = this.findNode("Character\\00002000.img").RequireImageNode("Character\\00002000.img");

            // Build shared tables without changing the source WZ nodes.
            var zMap = new Dictionary<string, int>(StringComparer.Ordinal);
            int z = 0;
            foreach (Wz_Node child in zNode.Nodes)
                zMap[child.Text] = --z;

            var sMap = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            foreach (Wz_Node child in sNode.Nodes)
                sMap[child.Text] = AvatarSlotTokens.Parse(child.ReadString());

            Dictionary<string, Wz_Node> actions = ReadActions(body);
            if (actions.Count == 0)
                throw new InvalidDataException("The base body contains no action frames.");

            // Publish only after all resources have been prepared successfully.
            this.resources = new Resources(zMap, sMap, body, actions);
        }

        public IReadOnlyList<string> GetEmotions(AvatarPartInput face)
        {
            if (face == null)
                throw new ArgumentNullException(nameof(face));
            return this.GetCatalog(face, AvatarAnimationTrack.Emotion).Names;
        }

        public IReadOnlyList<string> GetTamingActions(AvatarPartInput taming)
        {
            if (taming == null)
                throw new ArgumentNullException(nameof(taming));
            return this.GetCatalog(taming, AvatarAnimationTrack.Taming).Names;
        }

        public AvatarAnimationClip GetAction(string action, AvatarPartInput body = null)
        {
            Resources current = this.GetResources();
            Catalog catalog = body == null ? current.BaseBody : this.GetCatalog(body, AvatarAnimationTrack.Character);
            return this.GetClip(catalog, action, current.BaseBody);
        }

        public AvatarAnimationClip GetEmotion(AvatarPartInput face, string emotion)
        {
            if (face == null)
                throw new ArgumentNullException(nameof(face));
            return this.GetClip(this.GetCatalog(face, AvatarAnimationTrack.Emotion), emotion, null);
        }

        public AvatarAnimationClip GetTamingAction(AvatarPartInput taming, string action)
        {
            if (taming == null)
                throw new ArgumentNullException(nameof(taming));
            return this.GetClip(this.GetCatalog(taming, AvatarAnimationTrack.Taming), action, null);
        }

        public AvatarInstance CreateInstance()
        {
            return new AvatarInstance(this);
        }

        public AvatarTamingSettings GetTamingSettings(AvatarPartInput taming, string action)
        {
            if (taming == null)
                throw new ArgumentNullException(nameof(taming));
            if (string.IsNullOrEmpty(action))
                throw new ArgumentException("A taming action is required.", nameof(action));

            Catalog catalog = this.GetCatalog(taming, AvatarAnimationTrack.Taming);
            if (catalog.TamingSettings.TryGetValue(action, out AvatarTamingSettings cached))
                return cached;

            Wz_Node root = this.GetImage(taming, "TamingMob\\", true);
            var settings = new AvatarTamingSettings
            {
                CharacterAction = root.FindNode("characterAction/" + action).ReadString(),
                CharacterEmotion = root.FindNode("characterEmotion/" + action).ReadString(),
                RemoveBody = root.FindNode("info/removeBody").ReadInt(0) != 0
            };
            catalog.TamingSettings.Add(action, settings);
            return settings;
        }

        public AvatarPartContext GetPart(AvatarPartSlot slot, AvatarPartInput part)
        {
            this.GetResources();
            if (part == null)
                throw new ArgumentNullException(nameof(part));
            if (!Enum.IsDefined(typeof(AvatarPartSlot), slot))
                throw new ArgumentOutOfRangeException(nameof(slot));

            // Normalize node inputs before applying the same slot checks as ID inputs.
            Wz_Node node = part.ImageNode;
            int? id = part.ItemId;
            if (node != null)
            {
                node = this.GetImage(part, string.Empty, false);
                id = AvatarPartContext.FromNode(node).Id;
            }

            AvatarPartType type;
            string category = string.Empty;
            if (id.HasValue)
            {
                var classification = GetPartCategory(id.Value);
                if (!IsCompatibleSlot(slot, classification.Slot, id.Value))
                    throw new ArgumentException("Item " + id + " is incompatible with avatar slot " + slot + ".", nameof(part));

                type = classification.Type;
                category = classification.Folder.Length == 0 ? string.Empty : classification.Folder + "\\";
            }
            else
            {
                type = GetAnonymousPartType(slot);
            }
            if (node == null)
                node = this.GetImage(part, category, IsTamingSlot(slot));

            return AvatarPartContext.FromNode(node, type, IsTamingSlot(slot) ? AvatarLayerGroup.Taming : AvatarLayerGroup.Character);
        }

        public AvatarFramePart GetFrame(AvatarPartContext part, AvatarFrameSelection selection, string variant = null)
        {
            if (this.TryGetFrame(part, selection, out AvatarFramePart frame, variant))
                return frame;
            throw new KeyNotFoundException("Avatar frame not found: " + part.SourceNode.Text + "/" + selection.Action + "/" + selection.Frame);
        }

        public bool TryGetFrame(AvatarPartContext part, AvatarFrameSelection selection, out AvatarFramePart result, string variant = null)
        {
            if (part == null)
                throw new ArgumentNullException(nameof(part));
            if (selection == null)
                throw new ArgumentNullException(nameof(selection));

            result = null;
            Wz_Node root = variant == null ? part.SourceNode : part.SourceNode.FindNode(variant);
            string action = selection.Action;
            int number = selection.Frame;
            var visited = new HashSet<Wz_Node>();

            while (true)
            {
                // Select the exact frame, or an unnumbered static frame at index zero.
                Wz_Node actionNode = root.FindNode(action);
                Wz_Node frame = actionNode.FindNumberedChild(number, out bool hasNumberedChildren);
                if (frame == null && number == 0 && actionNode != null
                    && !hasNumberedChildren && actionNode.IsFrame())
                {
                    frame = actionNode;
                }
                frame = frame.ResolveOptionalUol();

                // Initial absence is normal; a missing reference target is invalid data.
                if (frame == null)
                {
                    if (visited.Count == 0)
                        return false;
                    throw new InvalidDataException("Missing referenced avatar frame: " + part.SourceNode.Text + "/" + action + "/" + number);
                }
                if (!visited.Add(frame))
                    throw new InvalidDataException("Cyclic avatar action/frame reference: " + action);

                string reference = frame.Nodes["action"].ReadString();
                if (reference == null)
                {
                    result = new AvatarFramePart(part, frame);
                    return true;
                }

                number = frame.Nodes["frame"].ReadInt(0);
                if (reference.Length == 0 || number < 0)
                    throw new InvalidDataException("Invalid avatar action/frame reference: " + action);
                action = reference;
            }
        }

        public IReadOnlyList<AvatarLayerSource> GetLayers(AvatarFramePart frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            var layers = new List<AvatarLayerSource>();
            Wz_Node frameNode = frame.FrameNode.ResolveUolChecked();
            foreach (Wz_Node child in frameNode.Nodes)
            {
                Wz_Node canvas = child.ResolveOptionalUol();
                if (child.Text == "hairShade")
                    canvas = canvas?.Nodes["0"].ResolveOptionalUol();
                if (!(canvas?.Value is Wz_Png))
                    continue;

                // Links supply pixels; geometry and z stay with the current canvas.
                Wz_Png png = this.ResolveCanvas(canvas, frame.Part.SourceNode);
                canvas.Nodes["origin"].TryReadPoint(out Point origin);
                var layer = new AvatarLayerSource(frame.Part, child.Text, canvas, png, origin);

                Wz_Node z = canvas.Nodes["z"].ResolveOptionalUol();
                if (z?.Value is string name)
                    layer.ZSlot = name;
                else
                    layer.ExplicitZ = z.ReadInt();

                // Preserve anchor order and duplicate names for group merging.
                Wz_Node map = canvas.Nodes["map"].ResolveOptionalUol();
                if (map != null)
                {
                    foreach (Wz_Node anchor in map.Nodes)
                    {
                        Wz_Node resolvedAnchor = anchor.ResolveOptionalUol();
                        if (resolvedAnchor == null)
                            continue;
                        if (!resolvedAnchor.TryReadPoint(out Point point))
                            throw new InvalidDataException("Invalid avatar map vector: " + canvas.FullPath + "/map/" + anchor.Text);
                        layer.MapAnchors.Add(new AvatarMapPoint(anchor.Text, point));
                    }
                }

                layers.Add(layer);
            }

            return layers.AsReadOnly();
        }

        private Wz_Node GetImage(AvatarPartInput part, string category, bool tamingFallback)
        {
            Resources current = this.GetResources();
            if (part.ImageNode != null)
                return part.ImageNode.RequireImageNode(part.ImageNode.Text);

            string fileName = part.ItemId.Value.ToString("D8", CultureInfo.InvariantCulture) + ".img";
            string path = "Character\\" + category + fileName;
            if (!current.Images.TryGetValue(path, out Wz_Node node))
            {
                node = this.findNode(path);
                if (node == null)
                    node = FindCharacterImage(this.findNode("Character"), fileName);
                if (node == null && tamingFallback)
                    node = this.findNode("TamingMob\\" + fileName);
                node = node.RequireImageNode(path);
                current.Images.Add(path, node);
            }

            return node;
        }

        private Wz_Png ResolveCanvas(Wz_Node canvas, Wz_Node imageRoot)
        {
            var visited = new HashSet<Wz_Node>();
            while (true)
            {
                canvas = canvas.ResolveUolChecked();
                if (canvas == null || !visited.Add(canvas))
                    throw new InvalidDataException("Broken or cyclic avatar canvas link.");

                // Follow source, then _inlink, then _outlink; repeat for chained links.
                string source = canvas.Nodes["source"].ReadString();
                string inlink = canvas.Nodes["_inlink"].ReadString();
                string outlink = canvas.Nodes["_outlink"].ReadString();

                if (!string.IsNullOrEmpty(source) || !string.IsNullOrEmpty(outlink) && string.IsNullOrEmpty(inlink))
                {
                    string path = !string.IsNullOrEmpty(source) ? source : outlink;
                    canvas = this.findNode(path.Replace('/', '\\'));
                    if (canvas == null)
                        throw new InvalidDataException("Avatar canvas link not found: " + path);
                    imageRoot = FindImageRoot(canvas) ?? imageRoot;
                }
                else if (!string.IsNullOrEmpty(inlink))
                {
                    // Relative links belong to the image containing the current canvas.
                    imageRoot = FindImageRoot(canvas) ?? imageRoot;
                    canvas = imageRoot.FindNode(inlink);
                    if (canvas == null)
                        throw new InvalidDataException("Avatar canvas inlink not found: " + inlink);
                }
                else
                {
                    return canvas.Value as Wz_Png ?? throw new InvalidDataException("Avatar canvas link target is not a PNG: " + canvas.FullPath);
                }
            }
        }

        private Resources GetResources()
        {
            return this.resources ?? throw new InvalidOperationException("Call AvatarLoader.Load before querying resources.");
        }

        private Catalog GetCatalog(AvatarPartInput part, AvatarAnimationTrack track)
        {
            Resources current = this.GetResources();
            string category = track == AvatarAnimationTrack.Emotion ? "Face\\"
                : track == AvatarAnimationTrack.Taming ? "TamingMob\\" : string.Empty;
            Wz_Node node = this.GetImage(part, category, track == AvatarAnimationTrack.Taming);

            var catalogs = current.Catalogs[track];
            if (!catalogs.TryGetValue(node, out Catalog catalog))
            {
                catalog = new Catalog(ReadActions(node, track));
                catalogs.Add(node, catalog);
            }

            return catalog;
        }

        private AvatarAnimationClip GetClip(Catalog catalog, string name, Catalog fallback)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("An action or emotion name is required.", nameof(name));

            // Use the fallback catalog only when this resource lacks the action.
            if (!catalog.ActionNodes.TryGetValue(name, out Wz_Node action))
            {
                if (fallback != null && !ReferenceEquals(catalog, fallback))
                    return this.GetClip(fallback, name, null);
                throw new KeyNotFoundException("Avatar action not found: " + name);
            }
            if (catalog.Clips.TryGetValue(name, out AvatarAnimationClip cached))
                return cached;

            // Read a static default expression or the action's actual numbered frames.
            var frames = new List<AvatarAnimationFrame>();
            List<Wz_Node> numbered = action.NumberedChildren().ToList();
            if (numbered.Count == 0 && name == "default" && action.IsFrame())
            {
                frames.Add(ReadFrame(catalog, fallback, name, 0, action, true, new HashSet<Wz_Node>()));
            }
            else
            {
                foreach (Wz_Node child in numbered)
                {
                    if (child.ResolveOptionalUol() == null)
                        continue;
                    int number = int.Parse(child.Text, CultureInfo.InvariantCulture);
                    frames.Add(ReadFrame(catalog, fallback, name, number, child, false, new HashSet<Wz_Node>()));
                }
            }

            if (frames.Count == 0)
                throw new InvalidDataException("Avatar action contains no frames: " + name);

            var clip = new AvatarAnimationClip(name, frames);
            catalog.Clips.Add(name, clip);
            return clip;
        }

        internal static bool IsTamingSlot(AvatarPartSlot slot)
        {
            return slot == AvatarPartSlot.Taming || slot == AvatarPartSlot.Saddle || slot == AvatarPartSlot.TamingEquipment;
        }

        private static Wz_Node FindImageRoot(Wz_Node node)
        {
            for (Wz_Node current = node; current != null; current = current.ParentNode)
            {
                if (current.Text.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
                    return current.RequireImageNode(current.Text);
            }
            return null;
        }

        private static Wz_Node FindCharacterImage(Wz_Node directory, string fileName)
        {
            if (directory == null)
                return null;

            Wz_Node result = null;
            foreach (Wz_Node child in directory.Nodes)
            {
                if (child.Text == "_Canvas" || !ReferenceEquals(child, directory.Nodes[child.Text]))
                    continue;

                Wz_Node candidate;
                if (child.Text == fileName)
                    candidate = child;
                else if (child.Value is Wz_Image || child.Text.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
                    continue;
                else
                    candidate = FindCharacterImage(child, fileName);

                if (candidate == null)
                    continue;
                if (result != null)
                    throw new InvalidDataException("Ambiguous avatar resource " + fileName + ": " + result.FullPath + "; " + candidate.FullPath);
                result = candidate;
            }

            return result;
        }

        public static (AvatarPartSlot Slot, AvatarPartType Type, string Folder) GetPartCategory(int id)
        {
            if (id < 0)
                throw new ArgumentOutOfRangeException(nameof(id));

            GearType gearType = Gear.GetGearType(id);
            switch (gearType)
            {
                case GearType.body when id >= 2000 && id < 3000:
                    return (AvatarPartSlot.Body, AvatarPartType.Body, "");
                case GearType.head when id >= 12000 && id < 13000:
                    return (AvatarPartSlot.Head, AvatarPartType.Head, "");
                case GearType.face:
                case GearType.face2:
                case GearType.face3:
                    return (AvatarPartSlot.Face, AvatarPartType.Face, "Face");
                case GearType.hair:
                case GearType.hair2:
                case GearType.hair3:
                case GearType.hair4:
                    return (AvatarPartSlot.Hair, AvatarPartType.Hair, "Hair");
                case GearType.cap:
                    return (AvatarPartSlot.Cap, AvatarPartType.Cap, "Cap");
                case GearType.faceAccessory:
                    return (AvatarPartSlot.FaceAccessory, AvatarPartType.FaceAccessory, "Accessory");
                case GearType.eyeAccessory:
                    return (AvatarPartSlot.EyeAccessory, AvatarPartType.EyeAccessory, "Accessory");
                case GearType.earrings:
                    return (AvatarPartSlot.Earrings, AvatarPartType.Earrings, "Accessory");
                case GearType.coat:
                    return (AvatarPartSlot.Coat, AvatarPartType.Coat, "Coat");
                case GearType.longcoat:
                    return (AvatarPartSlot.Coat, AvatarPartType.Longcoat, "Longcoat");
                case GearType.pants:
                    return (AvatarPartSlot.Pants, AvatarPartType.Pants, "Pants");
                case GearType.shoes:
                    return (AvatarPartSlot.Shoes, AvatarPartType.Shoes, "Shoes");
                case GearType.glove:
                    return (AvatarPartSlot.Glove, AvatarPartType.Glove, "Glove");
                case GearType.cape:
                    return (AvatarPartSlot.Cape, AvatarPartType.Cape, "Cape");
                case GearType.shield:
                case GearType.soulShield:
                case GearType.demonShield:
                    return (AvatarPartSlot.SubWeapon, AvatarPartType.Shield, "Shield");
                case GearType.cashWeapon:
                    return (AvatarPartSlot.Weapon, AvatarPartType.CashWeapon, "Weapon");
                case GearType.ring:
                    return (AvatarPartSlot.Ring1, AvatarPartType.Ring, "Ring");
                case GearType.pendant:
                    return (AvatarPartSlot.Pendant1, AvatarPartType.Pendant, "Accessory");
                case GearType.petEquip:
                    return (AvatarPartSlot.PetEquipment1, AvatarPartType.PetEquipment, "PetEquip");
                case GearType.taming:
                case GearType.taming2:
                case GearType.tamingChair:
                case GearType.taming3:
                    return (AvatarPartSlot.Taming, AvatarPartType.Taming, "TamingMob");
                case GearType.saddle:
                    return (AvatarPartSlot.Saddle, AvatarPartType.Saddle, "TamingMob");
                case (GearType)192:
                    return (AvatarPartSlot.TamingEquipment, AvatarPartType.Taming, "TamingMob");
                case GearType.medal:
                    return (AvatarPartSlot.Medal, AvatarPartType.Medal, "Accessory");
                case GearType.belt:
                    return (AvatarPartSlot.Belt, AvatarPartType.Belt, "Accessory");
                case GearType.shoulderPad:
                    return (AvatarPartSlot.Shoulder, AvatarPartType.Shoulder, "Accessory");
                case GearType.pocket:
                    return (AvatarPartSlot.Pocket, AvatarPartType.Pocket, "Accessory");
                case GearType.android:
                    return (AvatarPartSlot.Android, AvatarPartType.Android, "Android");
                case GearType.machineHeart:
                    return (AvatarPartSlot.Heart, AvatarPartType.Heart, "Android");
                case GearType.badge:
                    return (AvatarPartSlot.Badge, AvatarPartType.Badge, "Accessory");
            }

            if (gearType == GearType.emblem || (int)gearType / 1000 == (int)GearType.emblem)
                return (AvatarPartSlot.Emblem, AvatarPartType.Emblem, "Accessory");
            if (Gear.IsSubWeapon(gearType))
                return (AvatarPartSlot.SubWeapon, AvatarPartType.Weapon, "Weapon");
            if (Gear.IsWeapon(gearType))
                return (AvatarPartSlot.Weapon, AvatarPartType.Weapon, "Weapon");

            throw new ArgumentException("Unsupported avatar item ID: " + id + ".", nameof(id));
        }

        private static bool IsCompatibleSlot(AvatarPartSlot slot, AvatarPartSlot preferred, int id)
        {
            if (slot == preferred)
                return true;

            switch (preferred)
            {
                case AvatarPartSlot.Ring1:
                    return slot == AvatarPartSlot.Ring2 || slot == AvatarPartSlot.Ring3 || slot == AvatarPartSlot.Ring4;
                case AvatarPartSlot.Pendant1:
                    return slot == AvatarPartSlot.Pendant2;
                case AvatarPartSlot.PetEquipment1:
                    return slot == AvatarPartSlot.PetEquipment2 || slot == AvatarPartSlot.PetEquipment3;
                case AvatarPartSlot.Heart:
                    return slot == AvatarPartSlot.Emblem;
                case AvatarPartSlot.Weapon:
                    return slot == AvatarPartSlot.SubWeapon && id / 10000 == (int)GearType.swordZB;
                default:
                    return false;
            }
        }

        private static AvatarPartType GetAnonymousPartType(AvatarPartSlot slot)
        {
            switch (slot)
            {
                case AvatarPartSlot.Body: return AvatarPartType.Body;
                case AvatarPartSlot.Head: return AvatarPartType.Head;
                case AvatarPartSlot.Face: return AvatarPartType.Face;
                case AvatarPartSlot.Hair: return AvatarPartType.Hair;
                case AvatarPartSlot.Cap: return AvatarPartType.Cap;
                case AvatarPartSlot.Coat: return AvatarPartType.Coat;
                case AvatarPartSlot.Pants: return AvatarPartType.Pants;
                case AvatarPartSlot.Shoes: return AvatarPartType.Shoes;
                case AvatarPartSlot.Glove: return AvatarPartType.Glove;
                case AvatarPartSlot.Cape: return AvatarPartType.Cape;
                case AvatarPartSlot.SubWeapon:
                case AvatarPartSlot.Weapon: return AvatarPartType.Weapon;
                case AvatarPartSlot.FaceAccessory: return AvatarPartType.FaceAccessory;
                case AvatarPartSlot.EyeAccessory: return AvatarPartType.EyeAccessory;
                case AvatarPartSlot.Earrings: return AvatarPartType.Earrings;
                case AvatarPartSlot.Taming:
                case AvatarPartSlot.TamingEquipment: return AvatarPartType.Taming;
                case AvatarPartSlot.Saddle: return AvatarPartType.Saddle;
                default: return AvatarPartType.Unknown;
            }
        }

        private static AvatarAnimationFrame ReadFrame(Catalog catalog, Catalog fallback, string action, int number,
            Wz_Node frameNode, bool isStatic, HashSet<Wz_Node> path)
        {
            Wz_Node frame = frameNode.ResolveUolChecked();
            if (frame == null || !path.Add(frame) || path.Count > 128)
                throw new InvalidDataException("Broken, cyclic or excessively deep avatar frame reference: " + action);

            try
            {
                var selection = new AvatarFrameSelection(action, number);
                AvatarFrameSelection reference = null;
                bool face = frame.Nodes["face"].ReadInt(0) != 0;

                // Resolve the terminal action/frame and inherit its face flag.
                Wz_Node actionReference = frame.Nodes["action"].ResolveUolChecked();
                Wz_Node frameReference = frame.Nodes["frame"].ResolveUolChecked();
                if (actionReference != null)
                {
                    string referenceAction = actionReference.GetValue<string>(null);
                    int referenceNumber = frameReference.GetValueEx<int>(0);
                    if (string.IsNullOrEmpty(referenceAction) || referenceNumber < 0)
                        throw new InvalidDataException("Invalid avatar frame reference in " + action);

                    reference = new AvatarFrameSelection(referenceAction, referenceNumber);
                    Catalog targetCatalog = catalog;
                    if (!catalog.ActionNodes.TryGetValue(referenceAction, out Wz_Node targetAction))
                    {
                        if (fallback == null || !fallback.ActionNodes.TryGetValue(referenceAction, out targetAction))
                            throw new InvalidDataException("Missing referenced avatar action: " + referenceAction);
                        targetCatalog = fallback;
                    }

                    Wz_Node target = targetAction.FindNumberedChild(referenceNumber, out _);
                    if (target == null)
                        throw new InvalidDataException("Missing referenced avatar frame: " + referenceAction + "/" + referenceNumber);

                    AvatarAnimationFrame resolved = ReadFrame(targetCatalog, fallback, referenceAction, referenceNumber, target, false, path);
                    selection = resolved.ResolvedFrame;
                    face = resolved.Face;
                }

                // Timing, transforms and mount overrides still come from this frame.
                Wz_Vector move = frame.Nodes["move"].ResolveUolChecked().GetValueEx<Wz_Vector>(null);
                return new AvatarAnimationFrame
                {
                    Number = number,
                    Delay = frame.Nodes["delay"].ReadInt(isStatic ? 0 : 120),
                    IsStatic = isStatic,
                    Reference = reference,
                    ResolvedFrame = selection,
                    Face = face,

                    Flip = frame.Nodes["flip"].ReadInt(0) != 0,
                    Rotate = frame.Nodes["rotate"].ReadInt(0),
                    RotateProp = frame.Nodes["rotateProp"].ReadInt(0),
                    Move = move == null ? Point.Empty : new Point(move.X, move.Y),
                    JustDir = frame.Nodes["justDir"].ReadInt(0),

                    ForceCharacterAction = frame.Nodes["forceCharacterAction"].ReadString(),
                    ForceCharacterFrame = frame.Nodes["forceCharacterActionFrameIndex"].ReadInt(),
                    ForceCharacterFace = frame.Nodes["forceCharacterFace"].ReadString(),
                    ForceCharacterFaceFrame = frame.Nodes["forceCharacterFaceFrameIndex"].ReadInt(),
                    ForceCharacterFaceHide = frame.Nodes["forceCharacterFaceHide"].ReadInt(0) != 0,
                    ForceCharacterFlip = frame.Nodes["forceCharacterFlip"].ReadInt(0) != 0,
                    ForceCharacterDirection = frame.Nodes["forceCharacterDirection"].ReadInt(0),
                    RemoveBody = frame.Nodes["removeBody"].ReadInt(0) != 0
                };
            }
            finally
            {
                path.Remove(frame);
            }
        }

        private static Dictionary<string, Wz_Node> ReadActions(Wz_Node root, AvatarAnimationTrack track = AvatarAnimationTrack.Character)
        {
            var actions = new Dictionary<string, Wz_Node>(StringComparer.Ordinal);
            foreach (Wz_Node entry in root.Nodes)
            {
                // Ignore shadowed nodes and metadata before resolving action resources.
                if (!ReferenceEquals(entry, root.Nodes[entry.Text]))
                    continue;
                if (entry.Text == "info" || entry.Text == "characterAction" || entry.Text == "characterEmotion"
                    || entry.Text == "property" || entry.Text == "forcingItem")
                {
                    continue;
                }

                Wz_Node action = entry.ResolveOptionalUol();
                if (action == null)
                    continue;

                if (action.NumberedChildren().Any(frame => frame.IsFrame())
                    || track == AvatarAnimationTrack.Emotion && entry.Text == "default" && action.IsFrame())
                {
                    actions.Add(entry.Text, action);
                }
                else if (track == AvatarAnimationTrack.Character)
                {
                    // Nested numeric variants are separate action names, such as shoot\1.
                    foreach (Wz_Node variant in action.NumberedChildren())
                    {
                        Wz_Node nested = variant.ResolveOptionalUol();
                        if (nested != null && nested.NumberedChildren().Any(frame => frame.IsFrame()))
                            actions.Add(entry.Text + "\\" + variant.Text, nested);
                    }
                }
            }

            return actions;
        }

        private sealed class Resources
        {
            public Resources(Dictionary<string, int> zMap, Dictionary<string, IReadOnlyList<string>> sMap,
                Wz_Node body, Dictionary<string, Wz_Node> actions)
            {
                this.ZMap = new ReadOnlyDictionary<string, int>(zMap);
                this.SMap = new ReadOnlyDictionary<string, IReadOnlyList<string>>(sMap);
                this.BaseBody = new Catalog(actions);
                this.Catalogs = new Dictionary<AvatarAnimationTrack, Dictionary<Wz_Node, Catalog>>
                {
                    { AvatarAnimationTrack.Character, new Dictionary<Wz_Node, Catalog> { { body, this.BaseBody } } },
                    { AvatarAnimationTrack.Emotion, new Dictionary<Wz_Node, Catalog>() },
                    { AvatarAnimationTrack.Taming, new Dictionary<Wz_Node, Catalog>() }
                };
                this.Images = new Dictionary<string, Wz_Node>(StringComparer.Ordinal) { { "Character\\00002000.img", body } };
            }

            public IReadOnlyDictionary<string, int> ZMap { get; }
            public IReadOnlyDictionary<string, IReadOnlyList<string>> SMap { get; }
            public Catalog BaseBody { get; }
            public Dictionary<AvatarAnimationTrack, Dictionary<Wz_Node, Catalog>> Catalogs { get; }
            public Dictionary<string, Wz_Node> Images { get; }
        }

        private sealed class Catalog
        {
            public Catalog(Dictionary<string, Wz_Node> actions)
            {
                this.ActionNodes = actions;
                this.Names = actions.Keys.ToList().AsReadOnly();
                this.Clips = new Dictionary<string, AvatarAnimationClip>(StringComparer.Ordinal);
            }

            public Dictionary<string, Wz_Node> ActionNodes { get; }
            public IReadOnlyList<string> Names { get; }
            public Dictionary<string, AvatarAnimationClip> Clips { get; }
            public Dictionary<string, AvatarTamingSettings> TamingSettings { get; } = new Dictionary<string, AvatarTamingSettings>(StringComparer.Ordinal);
        }
    }
}