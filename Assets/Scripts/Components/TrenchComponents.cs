using System;
using Unity.Entities;
using Unity.Mathematics;

namespace TitanGlenelg.Core.AI
{
    public enum TrenchEngagementState : byte
    {
        Unengaged = 0,
        LockedInTrench = 1,
        ShedAdvantageDL = 2,
        AnchoredOL = 3,
        Pancaked = 4
    }

    public enum LinemanTechnique : byte
    {
        ZeroTechNose = 0,
        OneTechShade = 1,
        ThreeTechGuard = 2,
        FiveTechTackle = 3,
        NineTechWideGhost = 4
    }

    public struct TrenchLockupData : IComponentData
    {
        public TrenchEngagementState State;
        public Entity EngagedOpponent;
        public float LockupTimer;
        public float NextLeverageCheckTime; // Periodic 0.4s check
        public float CurrentLeverageDeltaM; // Delta M
        public float PocketDriftSpeedMps;   // 0.3 to 0.8 m/s backwards pocket drift
        public float3 EngagementAnchorPoint;
        public float3 PocketPocketNormal;
    }

    public struct LinemanPhysicalAttributes : IComponentData
    {
        public float StrengthRating;    // 0 to 100
        public float PowerLeverage;     // 0 to 100 (DL swim/bull rush or OL anchor)
        public float AnchorRating;      // 0 to 100 (OL anchor capability)
        public float MassKg;            // e.g. 115 - 145 kg
        public float StaminaEfficiency; // Multiplier affected by two-way fatigue
        public LinemanTechnique Technique;
        public bool IsOffensiveLineman;
        public bool IsPullingAssignment; // True for pulling guard in Counter Trey
        public float3 PullWaypoint;
    }
}
