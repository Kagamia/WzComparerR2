using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WzComparerR2.WzLib;
using WzComparerR2.Animation;

namespace WzComparerR2.MapRender
{
    public class ParticleDesc
    {
        public string Name { get; set; }
        public int TotalParticle { get; set; }
        public float Angle { get; set; }
        public float AngleVar { get; set; }
        public float Duration { get; set; }
        public ParticleBlendFunc BlendFuncSrc { get; set; }
        public ParticleBlendFunc BlendFuncDst { get; set; }
        public Color StartColor { get; set; }
        public Color StartColorVar { get; set; }
        public Color EndColor { get; set; }
        public Color EndColorVar { get; set; }
        public int MiddlePoint0 { get; set; }
        public int MiddlePointAlpha0 { get; set; }
        public int MiddlePoint1 { get; set; }
        public int MiddlePointAlpha1 { get; set; }
        public float StartSize { get; set; }
        public float StartSizeVar { get; set; }
        public float EndSize { get; set; }
        public float EndSizeVar { get; set; }
        public float PosX { get; set; }
        public float PosY { get; set; }
        public float PosVarX { get; set; }
        public float PosVarY { get; set; }
        public float StartSpin { get; set; }
        public float StartSpinVar { get; set; }
        public float EndSpin { get; set; }
        public float EndSpinVar { get; set; }
        public ParticleGravityDesc Gravity { get; set; }
        public ParticleRadiusDesc Radius { get; set; }
        public float Life { get; set; }
        public float LifeVar { get; set; }
        public bool OpacityModifyRGB { get; set; }
        public int PositionType { get; set; }
        public Frame Texture { get; set; }
    }

    public class ParticleGravityDesc
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Speed { get; set; }
        public float SpeedVar { get; set; }
        public float RadialAccel { get; set; }
        public float RadialAccelVar { get; set; }
        public float TangentialAccel { get; set; }
        public float TangentialAccelVar { get; set; }
        public bool RotationIsDir { get; set; }
    }

    public class ParticleRadiusDesc
    {
        public float StartRadius { get; set; }
        public float StartRadiusVar { get; set; }
        public float EndRadius { get; set; }
        public float EndRadiusVar { get; set; }
        public float RotatePerSecond { get; set; }
        public float RotatePerSecondVar { get; set; }
    }

    //same as d3d11blend
    public enum ParticleBlendFunc
    {
        ZERO = 1,
        ONE = 2,
        SRC_COLOR = 3,
        INV_SRC_COLOR = 4,
        SRC_ALPHA = 5,
        INV_SRC_ALPHA = 6,
        DEST_ALPHA = 7,
        INV_DEST_ALPHA = 8,
        DEST_COLOR = 9,
        INV_DEST_COLOR = 10,
    }
}
