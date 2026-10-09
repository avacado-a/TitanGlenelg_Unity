using System;
using Unity.Entities;
using Unity.Mathematics;

namespace TitanGlenelg.Core.AI
{
    public enum TeamSide : byte
    {
        HomeGlenelg = 0, // Red & Black
        AwayTitans = 1   // White & Blue
    }

    public enum PlayerPosition : byte
    {
        QB = 0,
        RB = 1,
        WR1 = 2,
        WR2 = 3,
        WR3 = 4,
        TE = 5,
        LT = 6,
        LG = 7,
        C = 8,
        RG = 9,
        RT = 10,
        DE_L = 11,
        DT_L = 12,
        DT_R = 13,
        DE_R = 14,
        WLB = 15,
        MLB = 16,
        SLB = 17,
        CB_L = 18,
        CB_R = 19,
        FS = 20,
        SS = 21
    }

    public enum OffensiveFSMPhase : byte
    {
        PreSnap = 0,
        SnapAndDrop = 1,
        Stem = 2,
        Cut = 3,
        CatchWindow = 4,
        RunAfterCatch = 5,
        Blocking = 6,
        Down = 7
    }

    public enum DefensiveFSMPhase : byte
    {
        PreSnap = 0,
        ZoneDrop = 1,
        ManShadow = 2,
        PassRush = 3,
        BreakOnBall = 4,
        TacklePursuit = 5,
        Tackling = 6,
        Down = 7
    }

    public enum TackleState : byte
    {
        None = 0,
        InitiatingContact = 1,
        LockedInTackle = 2,
        PancakedToTurf = 3,
        WhistleDown = 4
    }

    public struct PlayerIdentity : IComponentData
    {
        public int JerseyNumber;
        public TeamSide Team;
        public PlayerPosition Position;
        public bool IsUserControlled;
    }

    public struct PlayerFSMState : IComponentData
    {
        public OffensiveFSMPhase OffensePhase;
        public DefensiveFSMPhase DefensePhase;
        public float PhaseTimer;
        public float StateTimeElapsed;
    }

    public struct RouteSplineData : IComponentData
    {
        public float3 Point0; // Origin (Line of scrimmage / release point)
        public float3 Point1; // Stem apex / plant point
        public float3 Point2; // Cut exit direction
        public float3 Point3; // Final boundary or route finish point
        public float StemDurationSeconds;
        public float CutSharpnessFactor;
        public float RouteProgressT; // 0.0 to 1.0
        public float TargetSpeedMps;
    }

    public struct DefensiveCoverageData : IComponentData
    {
        public float3 ZoneLandmark;
        public float ZoneRadiusMeters;
        public Entity ManAssignmentEntity;
        public float CushionDistanceMeters;
        public float BreakReactionDelaySeconds;
        public float LeverageAngleDeg; // Inside/outside leverage
    }

    public struct TackleColliderData : IComponentData
    {
        public TackleState State;
        public Entity TargetEntity;
        public float MassKg;
        public float WrapUpStrength;
        public float ContactTime;
        public float RagdollBlendWeight; // 0.0 = pure kinematic locomotion, 1.0 = full active ragdoll impulse
        public float3 TackleImpactImpulse;
    }

    public struct KinematicLocomotionData : IComponentData
    {
        public float3 DesiredVelocity;
        public float3 CurrentVelocity;
        public float MaxSprintSpeedMps;
        public float AccelerationMps2;
        public float AngularTurnRateRadS;
        public float HeadingAngleRad;
    }
}
