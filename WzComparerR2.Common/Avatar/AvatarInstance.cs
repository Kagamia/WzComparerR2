using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using WzComparerR2.WzLib;

namespace WzComparerR2.Avatar
{
    public sealed class AvatarInstance
    {
        public AvatarInstance(AvatarLoader loader)
        {
            this.Loader = loader ?? throw new ArgumentNullException(nameof(loader));
            this.parts = new Dictionary<AvatarPartSlot, AvatarPartInput>();
            this.Parts = new ReadOnlyDictionary<AvatarPartSlot, AvatarPartInput>(this.parts);
        }

        private readonly Dictionary<AvatarPartSlot, AvatarPartInput> parts;

        public AvatarLoader Loader { get; }
        public IReadOnlyDictionary<AvatarPartSlot, AvatarPartInput> Parts { get; }

        public string CharacterAction { get; set; } = "stand1";
        public int CharacterFrame { get; set; }
        public string TamingAction { get; set; } = "stand";
        public int TamingFrame { get; set; }
        public string Emotion { get; set; } = "default";
        public int EmotionFrame { get; set; }

        public AvatarEarType EarType { get; set; } = AvatarEarType.Human;
        public bool FacingLeft { get; set; } = true;
        public int? WeaponType { get; set; }
        public int WeaponIndex { get; set; }
        public Func<AvatarPartSlot, AvatarFrameSelection, bool> IncludeEquipment { get; set; }
        public IList<string> ReservedSlots { get; } = new List<string>();

        public IReadOnlyList<string> AvailableActions => this.Loader.Actions;
        public IReadOnlyList<string> AvailableEmotions => this.parts.TryGetValue(AvatarPartSlot.Face, out var face)
            ? this.Loader.GetEmotions(face) : Array.Empty<string>();
        public IReadOnlyList<string> AvailableTamingActions => this.parts.TryGetValue(AvatarPartSlot.Taming, out var taming)
            ? this.Loader.GetTamingActions(taming) : Array.Empty<string>();

        public void SetPart(AvatarPartSlot slot, int itemId)
        {
            this.SetPart(slot, new AvatarPartInput(itemId));
        }

        public void SetPart(AvatarPartSlot slot, Wz_Node imageNode)
        {
            this.SetPart(slot, new AvatarPartInput(imageNode));
        }

        public void SetPart(AvatarPartSlot slot, AvatarPartInput part)
        {
            ValidateSlot(slot);
            this.parts[slot] = part ?? throw new ArgumentNullException(nameof(part));
        }

        public bool RemovePart(AvatarPartSlot slot)
        {
            ValidateSlot(slot);
            return this.parts.Remove(slot);
        }

        public void ClearParts()
        {
            this.parts.Clear();
        }

        public AvatarLayout BuildLayout()
        {
            return this.BuildLayout(null, null, null);
        }

        internal AvatarLayout BuildLayout(AvatarFrameSelection sampledCharacter, AvatarFrameSelection sampledEmotion,
            AvatarFrameSelection sampledTaming)
        {
            var zMap = this.Loader.ZMap;
            if (!this.parts.TryGetValue(AvatarPartSlot.Body, out AvatarPartInput bodyInput))
                throw new InvalidOperationException("A body resource is required to build an avatar layout.");
            if (!Enum.IsDefined(typeof(AvatarEarType), this.EarType))
                throw new ArgumentOutOfRangeException(nameof(this.EarType));
            if (this.WeaponIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(this.WeaponIndex));

            // Mount state is evaluated first, without overwriting requested tracks or shared frames.
            var diagnostics = new List<string>();
            var resolved = new Dictionary<AvatarPartSlot, AvatarPartContext>();
            AvatarTamingSettings tamingSettings = null;
            AvatarAnimationFrame tamingFrame = null;
            AvatarFrameSelection tamingSelection = null;
            if (this.parts.TryGetValue(AvatarPartSlot.Taming, out var tamingInput))
            {
                resolved.Add(AvatarPartSlot.Taming, this.Loader.GetPart(AvatarPartSlot.Taming, tamingInput));
                var requestedTaming = sampledTaming ?? new AvatarFrameSelection(this.TamingAction, this.TamingFrame);
                tamingFrame = SelectFrame(this.Loader.GetTamingAction(tamingInput, requestedTaming.Action), requestedTaming.Frame);
                tamingSelection = tamingFrame.ResolvedFrame;
                tamingSettings = this.Loader.GetTamingSettings(tamingInput, requestedTaming.Action);
            }

            bool removeBody = tamingSettings?.RemoveBody == true;
            bool hideFace = tamingFrame?.ForceCharacterFaceHide == true;
            AvatarFrameSelection actualCharacter = sampledCharacter ?? SelectMountOverride(this.CharacterAction, this.CharacterFrame,
                tamingSettings?.CharacterAction, tamingFrame?.ForceCharacterAction, tamingFrame?.ForceCharacterFrame);

            // removeBody filters the character equipment copy, not the independent mount inputs.
            foreach (var entry in this.parts.OrderBy(entry => (int)entry.Key))
            {
                if (entry.Key == AvatarPartSlot.Taming)
                    continue;
                if (removeBody && !KeepWhenBodyRemoved(entry.Key))
                    continue;
                if (entry.Key == AvatarPartSlot.Coat && this.parts.ContainsKey(AvatarPartSlot.Pants)
                    && IsLongcoat(entry.Value))
                {
                    continue;
                }
                if ((int)entry.Key >= 0 && !AvatarLoader.IsTamingSlot(entry.Key)
                    && this.IncludeEquipment?.Invoke(entry.Key, actualCharacter) == false)
                {
                    continue;
                }

                var input = removeBody && entry.Key == AvatarPartSlot.Body ? new AvatarPartInput(2000) : entry.Value;
                resolved.Add(entry.Key, this.Loader.GetPart(entry.Key, input));
            }

            if (!resolved.ContainsKey(AvatarPartSlot.Taming)
                && (resolved.ContainsKey(AvatarPartSlot.Saddle) || resolved.ContainsKey(AvatarPartSlot.TamingEquipment)))
            {
                throw new InvalidOperationException("Saddle and taming equipment require a taming resource.");
            }
            // Head and retained equipment keep the effective character action, even with a blink body.
            var sources = new List<AvatarLayerSource>();
            var actionBody = removeBody ? new AvatarPartInput(2000) : bodyInput;
            AvatarAnimationFrame character = SelectFrame(this.Loader.GetAction(actualCharacter.Action, actionBody), actualCharacter.Frame);
            AvatarAnimationFrame rawCharacter = character;
            AvatarFrameSelection characterSelection = character.ResolvedFrame;
            AvatarFrameSelection bodySelection = removeBody
                ? SelectFrame(this.Loader.GetAction("blink"), 0).ResolvedFrame : characterSelection;
            AvatarFrameSelection emotionSelection = null;

            if (!hideFace && this.parts.TryGetValue(AvatarPartSlot.Face, out var faceInput))
            {
                AvatarFrameSelection actualEmotion = sampledEmotion ?? SelectMountOverride(this.Emotion, this.EmotionFrame,
                    tamingSettings?.CharacterEmotion, tamingFrame?.ForceCharacterFace, tamingFrame?.ForceCharacterFaceFrame);
                emotionSelection = SelectFrame(this.Loader.GetEmotion(faceInput, actualEmotion.Action), actualEmotion.Frame).ResolvedFrame;
            }

            // Produce effective transform controls as a private copy; raster transforms remain separate.
            if (tamingFrame != null)
            {
                character = character with
                {
                    Flip = character.Flip ^ tamingFrame.ForceCharacterFlip,
                    JustDir = tamingFrame.ForceCharacterDirection > 0 ? tamingFrame.ForceCharacterDirection : character.JustDir
                };
            }

            bool showFace = character.Face;
            if (!string.IsNullOrEmpty(tamingSettings?.CharacterAction) && tamingSettings.CharacterAction != actualCharacter.Action)
            {
                AvatarAnimationClip mapped = this.Loader.GetAction(tamingSettings.CharacterAction, actionBody);
                showFace = SelectFrame(mapped, 0).Face;
            }
            showFace &= !hideFace;
            if (resolved.TryGetValue(AvatarPartSlot.FaceAccessory, out var faceAccessory))
                showFace &= faceAccessory.SourceNode.FindNode("info/invisibleFace").ReadInt(0) == 0;

            // Resource submission order controls anchor merging, not final drawing order.
            foreach (var entry in resolved.OrderBy(entry => SubmissionOrder(entry.Key)))
            {
                AvatarPartSlot slot = entry.Key;
                AvatarPartContext part = entry.Value;
                bool expression = slot == AvatarPartSlot.Face || slot == AvatarPartSlot.FaceAccessory;
                if (expression && !showFace)
                    continue;

                AvatarFrameSelection selection = AvatarLoader.IsTamingSlot(slot) ? tamingSelection
                    : expression ? emotionSelection ?? sampledEmotion ?? SelectMountOverride(this.Emotion, this.EmotionFrame,
                        tamingSettings?.CharacterEmotion, tamingFrame?.ForceCharacterFace, tamingFrame?.ForceCharacterFaceFrame)
                    : slot == AvatarPartSlot.Body ? bodySelection : characterSelection;

                // Cash weapons and saddles store actions under resource-specific branches.
                string variant = null;
                if (part.Type == AvatarPartType.CashWeapon)
                {
                    if (!this.WeaponType.HasValue || this.WeaponType.Value <= 0)
                        throw new InvalidOperationException("Cash weapon frame selection requires WeaponType.");
                    variant = this.WeaponType.Value.ToString(CultureInfo.InvariantCulture);
                }

                if (slot == AvatarPartSlot.Saddle)
                {
                    int? mountId = resolved[AvatarPartSlot.Taming].Id;
                    if (!mountId.HasValue)
                        throw new InvalidOperationException("Saddle frame selection requires a numeric taming ID.");
                    variant = mountId.Value.ToString(CultureInfo.InvariantCulture);
                }

                if (slot == AvatarPartSlot.Body && part.SourceNode.FindNode(selection.Action) == null)
                {
                    part = this.Loader.GetPart(AvatarPartSlot.Body, new AvatarPartInput(2000));
                    diagnostics.Add("Body action uses the base body resource: " + selection.Action);
                }

                // Optional missing frames omit the part; head fallback does not hide broken references.
                AvatarFramePart frame;
                if (slot == AvatarPartSlot.Body || slot == AvatarPartSlot.Taming)
                {
                    frame = this.Loader.GetFrame(part, selection, variant);
                }
                else if (!this.Loader.TryGetFrame(part, selection, out frame, variant))
                {
                    if (slot == AvatarPartSlot.Head)
                    {
                        var headSelection = new AvatarFrameSelection(character.Face ? "front" : "back", 0);
                        if (!this.Loader.TryGetFrame(part, headSelection, out frame))
                            continue;
                    }
                    else
                    {
                        continue;
                    }
                }

                // Filter canvas variants before submitting their anchors.
                IReadOnlyList<AvatarLayerSource> layers = this.Loader.GetLayers(frame);
                bool weaponVariants = layers.Any(layer => layer.Name == "weapon1");
                string weaponName = "weapon" + (this.WeaponIndex == 0 ? "" : this.WeaponIndex.ToString(CultureInfo.InvariantCulture));
                foreach (AvatarLayerSource layer in layers)
                {
                    if (!MatchesEar(layer.Name, this.EarType))
                        continue;
                    if (weaponVariants && layer.Name.StartsWith("weapon", StringComparison.Ordinal) && layer.Name != weaponName)
                        continue;
                    sources.Add(layer);
                }

                if (layers.Count == 0)
                    diagnostics.Add("No canvas layers in " + part.SourceNode.Text + "/" + selection.Action + "/" + selection.Frame);
            }

            // Build fresh placements, then resolve visibility in stable z order.
            var info = new AvatarLayoutFrameInfo(characterSelection, tamingSelection, emotionSelection, bodySelection)
            {
                CharacterFrame = character,
                RawCharacterFrame = rawCharacter,
                TamingFrame = tamingFrame,
                FacingLeft = this.FacingLeft,
                RemoveBody = removeBody,
                FrameRemoveBody = tamingFrame?.RemoveBody == true,
                FaceAllowed = showFace
            };
            AvatarLayout layout = new AvatarLayoutBuilder(zMap).Build(sources, tamingFrame != null, info, diagnostics);

            var reserved = new List<string>(this.ReservedSlots);
            if (!resolved.ContainsKey(AvatarPartSlot.Cap))
                reserved.AddRange(AvatarSlotTokens.Parse("H4H5"));
            new AvatarOcclusionResolver(zMap, this.Loader.SMap).Resolve(layout, reserved);

            return layout;
        }

        internal static AvatarFrameSelection SelectMountOverride(string requestedAction, int requestedFrame,
            string mappedAction, string forcedAction, int? forcedFrame)
        {
            if (!string.IsNullOrEmpty(forcedAction))
                return new AvatarFrameSelection(forcedAction, forcedFrame ?? 0);
            if (!string.IsNullOrEmpty(mappedAction) && mappedAction != requestedAction)
                return new AvatarFrameSelection(mappedAction, 0);
            return new AvatarFrameSelection(requestedAction, requestedFrame);
        }

        private static bool KeepWhenBodyRemoved(AvatarPartSlot slot)
        {
            return slot == AvatarPartSlot.Body || slot == AvatarPartSlot.Head || slot == AvatarPartSlot.Face
                || slot == AvatarPartSlot.Hair || slot == AvatarPartSlot.Cap || slot == AvatarPartSlot.EyeAccessory
                || slot == AvatarPartSlot.Earrings || AvatarLoader.IsTamingSlot(slot);
        }

        private static bool IsLongcoat(AvatarPartInput input)
        {
            int? id = input.ItemId;
            if (!id.HasValue && input.ImageNode != null)
            {
                string name = input.ImageNode.Text;
                if (name.EndsWith(".img", StringComparison.OrdinalIgnoreCase))
                    name = name.Substring(0, name.Length - 4);
                if (int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedId))
                    id = parsedId;
            }

            return id.HasValue && AvatarLoader.GetPartCategory(id.Value).Type == AvatarPartType.Longcoat;
        }

        private static AvatarAnimationFrame SelectFrame(AvatarAnimationClip clip, int number)
        {
            if (!clip.TryGetFrame(number, out AvatarAnimationFrame frame))
                throw new KeyNotFoundException("Avatar frame not found: " + clip.Name + "/" + number);
            return frame;
        }

        private static int SubmissionOrder(AvatarPartSlot slot)
        {
            if (slot == AvatarPartSlot.Face)
                return 32;
            if (slot == AvatarPartSlot.FaceAccessory)
                return 33;
            if (AvatarLoader.IsTamingSlot(slot))
                return 34 + (int)slot - (int)AvatarPartSlot.Taming;
            return (int)slot;
        }

        private static bool MatchesEar(string name, AvatarEarType ear)
        {
            switch (name)
            {
                case "humanEar": return ear == AvatarEarType.Human;
                case "ear": return ear == AvatarEarType.Elf;
                case "lefEar": return ear == AvatarEarType.Lef;
                case "highlefEar": return ear == AvatarEarType.HighLef;
                default: return true;
            }
        }

        private static void ValidateSlot(AvatarPartSlot slot)
        {
            if (!Enum.IsDefined(typeof(AvatarPartSlot), slot))
                throw new ArgumentOutOfRangeException(nameof(slot));
        }
    }
}