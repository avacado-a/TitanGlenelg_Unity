using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace TitanGlenelg.Core.Physics
{
    /// <summary>
    /// Burst-compiled 120 Hz 4th-Order Runge-Kutta (RK4) ballistics system.
    /// Governs prolate spheroid aerodynamics (cross-sectional drag, Magnus lift, wobble decay),
    /// continuously updating PhysicsVelocity and LocalTransform, handing off to Havok on contact.
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    [BurstCompile]
    public partial struct RK4BallisticsSystem : ISystem
    {
        public const float FixedDeltaTime120Hz = 1.0f / 120.0f;
        public static readonly float3 Gravity = new float3(0f, -9.80665f, 0f);

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FootballTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;
            // Use sub-stepping to guarantee 120Hz or higher precision
            int steps = math.max(1, (int)math.round(dt / FixedDeltaTime120Hz));
            float stepDt = dt / steps;

            for (int i = 0; i < steps; i++)
            {
                var job = new RK4BallisticsJob
                {
                    DeltaTime = stepDt,
                    GravityAcc = Gravity
                };
                job.ScheduleParallel();
            }
        }
    }

    [BurstCompile]
    public partial struct RK4BallisticsJob : IJobEntity
    {
        public float DeltaTime;
        public float3 GravityAcc;

        public void Execute(
            Entity entity,
            ref BallisticsState ballistics,
            in ProlateSpheroidAerodynamics aero,
            ref PhysicsVelocity physVel,
            ref LocalTransform transform,
            ref HavokContactData contactData)
        {
            // Only simulate aerodynamics when ball is actively in flight
            if (ballistics.Phase != BallFlightPhase.InFlightAerodynamic)
            {
                return;
            }

            // Check if Havok contact has been flagged by collision event
            if (contactData.HasCollided)
            {
                ballistics.Phase = BallFlightPhase.HavokRigidBodyContact;
                return;
            }

            // Turf contact check (ground height = 0.11m ball radius)
            if (transform.Position.y <= 0.11f && ballistics.Velocity.y <= 0f)
            {
                ballistics.Phase = BallFlightPhase.HavokRigidBodyContact;
                contactData.HasCollided = true;
                contactData.ContactPoint = new float3(transform.Position.x, 0.11f, transform.Position.z);
                contactData.ContactNormal = new float3(0f, 1f, 0f);
                return;
            }

            // Update flight time and gyroscopic wobble decay
            ballistics.FlightTime += DeltaTime;
            ballistics.WobbleAngleRad *= math.exp(-ballistics.WobbleDecayRate * DeltaTime);

            // Execute 4th-Order Runge-Kutta numerical integration
            float3 x0 = transform.Position;
            float3 v0 = ballistics.Velocity;
            float3 spin = ballistics.SpinAxis;
            float spinRate = ballistics.SpinRateRadS;
            float wobble = ballistics.WobbleAngleRad;

            // k1
            float3 k1_x = v0;
            float3 k1_v = ComputeAcceleration(v0, spin, spinRate, wobble, in aero);

            // k2
            float3 v_half1 = v0 + 0.5f * DeltaTime * k1_v;
            float3 k2_x = v_half1;
            float3 k2_v = ComputeAcceleration(v_half1, spin, spinRate, wobble, in aero);

            // k3
            float3 v_half2 = v0 + 0.5f * DeltaTime * k2_v;
            float3 k3_x = v_half2;
            float3 k3_v = ComputeAcceleration(v_half2, spin, spinRate, wobble, in aero);

            // k4
            float3 v_full = v0 + DeltaTime * k3_v;
            float3 k4_x = v_full;
            float3 k4_v = ComputeAcceleration(v_full, spin, spinRate, wobble, in aero);

            // Integrate state
            float3 nextPos = x0 + (DeltaTime / 6.0f) * (k1_x + 2.0f * k2_x + 2.0f * k3_x + k4_x);
            float3 nextVel = v0 + (DeltaTime / 6.0f) * (k1_v + 2.0f * k2_v + 2.0f * k3_v + k4_v);

            // Update internal state
            ballistics.Position = nextPos;
            ballistics.Velocity = nextVel;

            // Align ball orientation with flight tangent and spin axis
            if (math.lengthsq(nextVel) > 0.01f)
            {
                float3 flightDir = math.normalize(nextVel);
                // Apply wobble nutation offset
                float3 wobbleDir = math.cross(flightDir, new float3(0f, 1f, 0f));
                if (math.lengthsq(wobbleDir) > 0.001f)
                {
                    wobbleDir = math.normalize(wobbleDir);
                    flightDir = math.normalize(flightDir + wobbleDir * math.sin(wobble));
                }
                transform.Rotation = quaternion.LookRotationSafe(flightDir, new float3(0f, 1f, 0f));
            }

            transform.Position = nextPos;

            // Write to PhysicsVelocity so Havok Physics retains continuous linear momentum
            physVel.Linear = nextVel;
            physVel.Angular = ballistics.SpinAxis * ballistics.SpinRateRadS;
        }

        /// <summary>
        /// Computes total acceleration = Gravity + Drag Force / Mass + Magnus Lift Force / Mass
        /// </summary>
        [BurstCompile]
        public float3 ComputeAcceleration(
            float3 velocity,
            float3 spinAxis,
            float spinRate,
            float wobbleRad,
            in ProlateSpheroidAerodynamics aero)
        {
            float speedSq = math.lengthsq(velocity);
            if (speedSq < 0.0001f)
            {
                return GravityAcc;
            }

            float speed = math.sqrt(speedSq);
            float3 vDir = velocity / speed;

            // 1. Prolate spheroid cross-sectional area and drag coefficient interpolation
            // The angle of attack alpha varies with wobble; when aligned nose-on, alpha = 0.
            float alpha = math.clamp(wobbleRad, 0.0f, math.PI * 0.5f);
            float sinAlpha = math.sin(alpha);
            float cosAlpha = math.cos(alpha);

            // Cross section: Elliptical projection A = A_min * cos(alpha) + A_max * sin(alpha)
            float crossSection = aero.MinAreaM2 * cosAlpha + aero.MaxAreaM2 * sinAlpha;
            // Drag coefficient: Cd = Cd_min + (Cd_max - Cd_min) * (sinAlpha ^ 2)
            float cd = aero.MinDragCoeff + (aero.MaxDragCoeff - aero.MinDragCoeff) * (sinAlpha * sinAlpha);

            // Drag force: F_d = -0.5 * rho * crossSection * Cd * |v| * v
            float dragMag = 0.5f * aero.AirDensityKgM3 * crossSection * cd * speedSq;
            float3 dragForce = -dragMag * vDir;

            // 2. Magnus effect lift force: F_M = 0.5 * rho * Cl * crossSection * (omega x v)
            // Spin vector = spinAxis * spinRate
            float3 omega = spinAxis * spinRate;
            float3 magnusVector = math.cross(omega, velocity);
            float3 magnusForce = 0.5f * aero.AirDensityKgM3 * aero.LiftCoeffMagnus * crossSection * magnusVector;

            // Total force / mass + gravity
            float3 aeroForce = dragForce + magnusForce;
            float3 totalAcc = GravityAcc + (aeroForce / aero.MassKg);

            return totalAcc;
        }
    }
}
