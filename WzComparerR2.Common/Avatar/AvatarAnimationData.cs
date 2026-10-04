using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace WzComparerR2.Avatar
{
    public sealed record AvatarAnimationFrame
    {
        public int Number { get; init; }
        public int Delay { get; init; }
        public TimeSpan Duration => TimeSpan.FromMilliseconds(Math.Abs(this.Delay));
        public bool IsStatic { get; init; }

        public AvatarFrameSelection Reference { get; init; }
        public AvatarFrameSelection ResolvedFrame { get; init; }
        public bool Face { get; init; }

        public bool Flip { get; init; }
        public int Rotate { get; init; }
        public int RotateProp { get; init; }
        public Point Move { get; init; }
        public int JustDir { get; init; }

        public string ForceCharacterAction { get; init; }
        public int? ForceCharacterFrame { get; init; }
        public string ForceCharacterFace { get; init; }
        public int? ForceCharacterFaceFrame { get; init; }
        public bool ForceCharacterFaceHide { get; init; }
        public bool ForceCharacterFlip { get; init; }
        public int ForceCharacterDirection { get; init; }
        public bool RemoveBody { get; init; }
    }

    public sealed record AvatarTamingSettings
    {
        public string CharacterAction { get; init; }
        public string CharacterEmotion { get; init; }
        public bool RemoveBody { get; init; }
    }

    public sealed class AvatarAnimationClip
    {
        internal AvatarAnimationClip(string name, IList<AvatarAnimationFrame> frames)
        {
            this.Name = name;
            this.Frames = frames.ToArray();
        }

        public string Name { get; }
        public IReadOnlyList<AvatarAnimationFrame> Frames { get; }

        public bool TryGetFrame(int number, out AvatarAnimationFrame frame)
        {
            foreach (AvatarAnimationFrame candidate in this.Frames)
            {
                if (candidate.Number == number)
                {
                    frame = candidate;
                    return true;
                }
            }

            frame = null;
            return false;
        }
    }
}