using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace TitanGlenelg.Core.Physics
{
    public enum BallFlightPhase : byte
    {
        HeldByPlayer = 0,
        InFlightAerodynamic = 1,
        HavokRigidBodyContact = 2,
        DeadBall = 3
    }

    public enum ThrowType : byte
    {
        Lob = 0,
        Touch = 1,
        Bullet = 2
    }

    public enum UserCatchMode : byte
    {
        None = 0,
        RAC = 1,        // Run After Catch (high momentum, high knockout risk)
        Aggressive = 2, // High-Point / Jump (maximum vertical catch radius, vulnerable in air)
        Possession = 3  // Secure / Protect (minimize knockout risk, collapse to turf, 0 YAC)
    }

    public struct FootballTag : IComponentData
    {
    }

    public struct BallisticsState : IComponentData
    {
        public BallFlightPhase Phase;
        public float3 Position;
        public float3 Velocity;
        public float3 SpinAxis;         // Normalized axis of rotation
        public float SpinRateRadS;      // Angular speed in radians/sec (e.g. 50 rad/s = ~477 RPM)
        public float WobbleAngleRad;    // Nutation/precession deviation from velocity tangent
        public float WobbleDecayRate;   // Exponential damping factor (lambda)
        public float FlightTime;
        public Entity CarrierEntity;
        public Entity TargetReceiverEntity;
        public ThrowType ActiveThrowType;
        public float3 TargetLeadOffset;
    }

    public struct ProlateSpheroidAerodynamics : IComponentData
    {
        public float MassKg;             // ~0.425 kg (NFHS standard 14-15 oz)
        public float MinAreaM2;          // Nose-on cross section ~0.019 m^2
        public float MaxAreaM2;          // Broadside cross section ~0.045 m^2
        public float MinDragCoeff;       // Cd nose-on ~0.08
        public float MaxDragCoeff;       // Cd broadside ~0.70
        public float LiftCoeffMagnus;    // Cl Magnus lift multiplier ~0.15
        public float AirDensityKgM3;     // ~1.225 kg/m^3 (sea level / standard atmosphere)
    }

    public struct HavokContactData : IComponentData
    {
        public bool HasCollided;
        public float3 ContactPoint;
        public float3 ContactNormal;
        public float ImpulseMagnitude;
        public Entity CollidedEntity;
    }

    public struct CatchTargetData : IComponentData
    {
        public Entity BallEntity;
        public UserCatchMode CatchMode;
        public float CatchRadius;
        public float CatchWindowOpenTime;
        public float CatchWindowCloseTime;
        public bool CatchAttempted;
        public bool CatchSecured;
        public bool KnockoutOccurred;
    }
}
