using System;
using System.Collections.Generic;
using System.Linq;

namespace WzComparerR2.Avatar
{
    public enum AvatarAnimationTrack
    {
        Character,
        Taming,
        Emotion
    }

    public sealed class AvatarAnimator
    {
        public AvatarAnimator(AvatarInstance avatar)
        {
            this.Avatar = avatar ?? throw new ArgumentNullException(nameof(avatar));
            this.tracks = new[] { new TrackState(), new TrackState(), new TrackState() };
            this.seeks = new int?[3];
        }

        private TrackState[] tracks;
        private readonly int?[] seeks;
        private bool reset;
        private AvatarInstance previousInputs;
        private object previousResources;

        public AvatarInstance Avatar { get; }

        public AvatarLayout CurrentLayout { get; private set; }

        public bool LayoutChanged { get; private set; }

        public void Seek(AvatarAnimationTrack track, int frame)
        {
            if (!Enum.IsDefined(typeof(AvatarAnimationTrack), track))
                throw new ArgumentOutOfRangeException(nameof(track));
            if (frame < 0)
                throw new ArgumentOutOfRangeException(nameof(frame));

            this.seeks[(int)track] = frame;
        }

        public void Reset()
        {
            this.reset = true;
            Array.Clear(this.seeks, 0, this.seeks.Length);
        }

        public void Update(TimeSpan elapsed)
        {
            if (elapsed < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(elapsed));

            AvatarInstance inputs = this.CaptureInputs();
            object resources = inputs.Loader.Actions;
            bool reload = !ReferenceEquals(resources, this.previousResources);
            TrackState[] next = this.tracks.Select(track => track.Copy()).ToArray();
            AvatarTamingSettings settings = inputs.Parts.TryGetValue(AvatarPartSlot.Taming, out AvatarPartInput taming)
                ? inputs.Loader.GetTamingSettings(taming, inputs.TamingAction) : null;
            foreach (AvatarAnimationTrack track in new[] { AvatarAnimationTrack.Taming, AvatarAnimationTrack.Character, AvatarAnimationTrack.Emotion })
            {
                GetRequest(inputs, track, out string action, out int frame, out AvatarPartInput part);
                bool restart = this.reset || reload || this.previousInputs == null;
                bool first = this.reset;
                if (this.previousInputs != null)
                {
                    GetRequest(this.previousInputs, track, out string oldAction, out int oldFrame, out AvatarPartInput oldPart);
                    bool changedAction = action != oldAction || !ReferenceEquals(part, oldPart);
                    restart |= changedAction || frame != oldFrame;
                    first |= changedAction && frame == oldFrame;
                }

                SelectTrack(inputs, next, track, settings, restart, first, this.seeks[(int)track]);
            }

            AdvanceTracks(inputs, next, settings, elapsed.Ticks);

            bool changed = this.CurrentLayout == null || this.reset || reload || this.seeks.Any(frame => frame.HasValue)
                || !SameInputs(inputs, this.previousInputs);
            for (int index = 0; index < next.Length; index++)
                changed |= !next[index].SameFrame(this.tracks[index]);

            AvatarLayout layout = changed ? inputs.BuildLayout(next[0].Selection, next[2].Selection, next[1].Selection) : this.CurrentLayout;
            this.tracks = next;
            this.previousInputs = inputs;
            this.previousResources = resources;
            this.CurrentLayout = layout;
            this.LayoutChanged = changed;
            this.reset = false;
            Array.Clear(this.seeks, 0, this.seeks.Length);
        }

        private AvatarInstance CaptureInputs()
        {
            AvatarInstance source = this.Avatar;
            var snapshot = new AvatarInstance(source.Loader)
            {
                CharacterAction = source.CharacterAction,
                CharacterFrame = source.CharacterFrame,
                TamingAction = source.TamingAction,
                TamingFrame = source.TamingFrame,
                Emotion = source.Emotion,
                EmotionFrame = source.EmotionFrame,
                FacingLeft = source.FacingLeft,
                EarType = source.EarType,
                WeaponType = source.WeaponType,
                WeaponIndex = source.WeaponIndex,
                IncludeEquipment = source.IncludeEquipment
            };
            foreach (var part in source.Parts)
                snapshot.SetPart(part.Key, part.Value);
            foreach (string token in source.ReservedSlots)
                snapshot.ReservedSlots.Add(token);
            return snapshot;
        }

        private static bool SameInputs(AvatarInstance current, AvatarInstance previous)
        {
            return previous != null && current.CharacterAction == previous.CharacterAction && current.CharacterFrame == previous.CharacterFrame
                && current.TamingAction == previous.TamingAction && current.TamingFrame == previous.TamingFrame
                && current.Emotion == previous.Emotion && current.EmotionFrame == previous.EmotionFrame
                && current.FacingLeft == previous.FacingLeft && current.EarType == previous.EarType
                && current.WeaponType == previous.WeaponType && current.WeaponIndex == previous.WeaponIndex
                && ReferenceEquals(current.IncludeEquipment, previous.IncludeEquipment)
                && current.ReservedSlots.SequenceEqual(previous.ReservedSlots) && current.Parts.Count == previous.Parts.Count
                && current.Parts.All(part => previous.Parts.TryGetValue(part.Key, out AvatarPartInput other) && ReferenceEquals(part.Value, other));
        }

        private static void GetRequest(AvatarInstance inputs, AvatarAnimationTrack track, out string action, out int frame, out AvatarPartInput part)
        {
            AvatarPartSlot slot;
            switch (track)
            {
                case AvatarAnimationTrack.Character:
                    action = inputs.CharacterAction; frame = inputs.CharacterFrame; slot = AvatarPartSlot.Body; break;
                case AvatarAnimationTrack.Taming:
                    action = inputs.TamingAction; frame = inputs.TamingFrame; slot = AvatarPartSlot.Taming; break;
                default:
                    action = inputs.Emotion; frame = inputs.EmotionFrame; slot = AvatarPartSlot.Face; break;
            }
            inputs.Parts.TryGetValue(slot, out part);
        }

        private static AvatarAnimationClip GetClip(AvatarInstance inputs, AvatarAnimationTrack track, string action, AvatarPartInput part)
        {
            switch (track)
            {
                case AvatarAnimationTrack.Character: return inputs.Loader.GetAction(action, part);
                case AvatarAnimationTrack.Taming: return inputs.Loader.GetTamingAction(part, action);
                default: return inputs.Loader.GetEmotion(part, action);
            }
        }

        private static void SelectTrack(AvatarInstance inputs, TrackState[] tracks, AvatarAnimationTrack track,
            AvatarTamingSettings settings, bool restart, bool first, int? seek)
        {
            GetRequest(inputs, track, out string action, out int frame, out AvatarPartInput part);
            AvatarAnimationFrame mount = tracks[(int)AvatarAnimationTrack.Taming].Frame;
            bool pinned = false;
            if (track == AvatarAnimationTrack.Character)
            {
                if (part == null)
                    throw new InvalidOperationException("A body resource is required to animate an avatar.");
                AvatarFrameSelection selection = AvatarInstance.SelectMountOverride(action, frame, settings?.CharacterAction,
                    mount?.ForceCharacterAction, mount?.ForceCharacterFrame);
                action = selection.Action;
                frame = selection.Frame;
                pinned = !string.IsNullOrEmpty(mount?.ForceCharacterAction);
                if (settings?.RemoveBody == true)
                    part = new AvatarPartInput(2000);
            }
            else if (track == AvatarAnimationTrack.Emotion)
            {
                if (mount?.ForceCharacterFaceHide == true)
                {
                    part = null;
                }
                else if (part != null)
                {
                    AvatarFrameSelection selection = AvatarInstance.SelectMountOverride(action, frame, settings?.CharacterEmotion,
                        mount?.ForceCharacterFace, mount?.ForceCharacterFaceFrame);
                    action = selection.Action;
                    frame = selection.Frame;
                    pinned = !string.IsNullOrEmpty(mount?.ForceCharacterFace);
                }
            }

            AvatarAnimationClip clip = part == null ? null : GetClip(inputs, track, action, part);
            tracks[(int)track].Select(clip, frame, pinned, restart, first, seek);
        }

        private static void AdvanceTracks(AvatarInstance inputs, TrackState[] tracks, AvatarTamingSettings settings, long ticks)
        {
            long cycle = tracks[(int)AvatarAnimationTrack.Taming].Cycle;
            if (cycle > 0 && ticks >= cycle)
            {
                long characterRevision = tracks[0].Revision;
                long emotionRevision = tracks[2].Revision;
                AdvanceSegment(inputs, tracks, settings, cycle);

                // A reset during a full mount cycle makes that track's end state periodic.
                long skipped = (ticks / cycle - 1) * cycle;
                if (tracks[0].Revision == characterRevision)
                    tracks[0].Advance(skipped);
                if (tracks[2].Revision == emotionRevision)
                    tracks[2].Advance(skipped);
                ticks %= cycle;
            }
            AdvanceSegment(inputs, tracks, settings, ticks);
        }

        private static void AdvanceSegment(AvatarInstance inputs, TrackState[] tracks, AvatarTamingSettings settings, long ticks)
        {
            if (ticks == 0)
                return;

            TrackState mount = tracks[(int)AvatarAnimationTrack.Taming];
            if (mount.Cycle == 0)
            {
                tracks[0].Advance(ticks);
                tracks[2].Advance(ticks);
                return;
            }

            mount.SkipZeroFrames();
            SelectTrack(inputs, tracks, AvatarAnimationTrack.Character, settings, false, false, null);
            SelectTrack(inputs, tracks, AvatarAnimationTrack.Emotion, settings, false, false, null);
            while (ticks > 0)
            {
                long step = Math.Min(ticks, TrackState.DurationTicks(mount.Frame) - mount.Elapsed);
                tracks[0].Advance(step);
                tracks[2].Advance(step);
                mount.Advance(step);
                ticks -= step;

                SelectTrack(inputs, tracks, AvatarAnimationTrack.Character, settings, false, false, null);
                SelectTrack(inputs, tracks, AvatarAnimationTrack.Emotion, settings, false, false, null);
            }
        }

        private sealed class TrackState
        {
            public AvatarAnimationClip Clip { get; private set; }
            public int Index { get; private set; }
            public long Elapsed { get; private set; }
            public long Cycle { get; private set; }
            public bool Pinned { get; private set; }
            public long Revision { get; private set; }
            public AvatarAnimationFrame Frame => this.Clip?.Frames[this.Index];
            public AvatarFrameSelection Selection => this.Clip == null ? null : new AvatarFrameSelection(this.Clip.Name, this.Frame.Number);

            public TrackState Copy()
            {
                return (TrackState)this.MemberwiseClone();
            }

            public bool SameFrame(TrackState other)
            {
                return ReferenceEquals(this.Clip, other.Clip) && this.Index == other.Index && this.Pinned == other.Pinned;
            }

            public void Select(AvatarAnimationClip clip, int frame, bool pinned, bool restart, bool first, int? seek)
            {
                if (clip == null)
                {
                    if (seek.HasValue)
                        throw new InvalidOperationException("Cannot seek an unavailable animation track.");
                    if (this.Clip != null)
                        this.Revision++;
                    this.Clip = null;
                    this.Index = 0;
                    this.Elapsed = 0;
                    this.Cycle = 0;
                    this.Pinned = false;
                    return;
                }
                if (pinned && seek.HasValue && seek.Value != frame)
                    throw new InvalidOperationException("Cannot seek away from a mount-forced frame.");

                bool newClip = !ReferenceEquals(this.Clip, clip);
                if (!newClip && !restart && !seek.HasValue && this.Pinned == pinned && (!pinned || this.Frame.Number == frame))
                    return;

                int number = seek ?? (first && !pinned ? clip.Frames[0].Number : frame);
                int index = -1;
                for (int candidate = 0; candidate < clip.Frames.Count; candidate++)
                {
                    if (clip.Frames[candidate].Number == number)
                    {
                        index = candidate;
                        break;
                    }
                }
                if (index < 0 && number == 0 && !seek.HasValue && !pinned)
                    index = 0;
                if (index < 0)
                    throw new KeyNotFoundException("Avatar animation frame not found: " + clip.Name + "/" + number);

                if (newClip)
                {
                    this.Cycle = 0;
                    foreach (AvatarAnimationFrame item in clip.Frames)
                        this.Cycle = checked(this.Cycle + DurationTicks(item));
                }
                this.Clip = clip;
                this.Index = index;
                this.Elapsed = 0;
                this.Pinned = pinned;
                this.Revision++;
            }

            public void Advance(long ticks)
            {
                if (ticks == 0 || this.Clip == null || this.Pinned || this.Cycle == 0)
                    return;

                ticks %= this.Cycle;
                while (true)
                {
                    long remaining = DurationTicks(this.Frame) - this.Elapsed;
                    if (ticks < remaining)
                    {
                        this.Elapsed += ticks;
                        return;
                    }
                    ticks -= remaining;
                    this.Index = (this.Index + 1) % this.Clip.Frames.Count;
                    this.Elapsed = 0;
                }
            }

            public void SkipZeroFrames()
            {
                if (this.Cycle == 0 || this.Pinned)
                    return;
                while (DurationTicks(this.Frame) == 0)
                    this.Index = (this.Index + 1) % this.Clip.Frames.Count;
            }

            public static long DurationTicks(AvatarAnimationFrame frame)
            {
                return frame.IsStatic ? 0 : Math.Abs((long)frame.Delay) * TimeSpan.TicksPerMillisecond;
            }
        }
    }
}