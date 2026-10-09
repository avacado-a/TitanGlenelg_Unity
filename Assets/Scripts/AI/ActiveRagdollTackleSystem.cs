using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace TitanGlenelg.Core.AI
{
    /// <summary>
    /// Kinematic locomotion blending into active ragdoll impulse and Two-Bone IK momentum on tackles.
    /// Bridges high-speed kinematic running with Havok physical momentum transfer upon wrap-up.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct ActiveRagdollTackleSystem : ISystem
    {
        public const float TackleContactDuration = 0.45f;
        public const float WhistleDownVelocityThreshold = 0.35f;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            var job = new EvaluateTackleInteractionsJob
            {
                DeltaTime = dt
            };
            job.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct EvaluateTackleInteractionsJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(
            Entity entity,
            ref TackleColliderData tackle,
            ref KinematicLocomotionData locomotion,
            ref LocalTransform transform,
            ref PhysicsVelocity physVel)
        {
            if (tackle.State == TackleState.None || tackle.State == TackleState.WhistleDown)
            {
                return;
            }

            tackle.ContactTime += DeltaTime;

            switch (tackle.State)
            {
                case TackleState.InitiatingContact:
                    // First frame of contact: compute inelastic collision momentum
                    tackle.RagdollBlendWeight = math.clamp(tackle.ContactTime / 0.15f, 0.0f, 0.5f);
                    tackle.State = TackleState.LockedInTackle;
                    break;

                case TackleState.LockedInTackle:
                    // Smoothly blend kinematic locomotion into Havok rigid body impulse (weight 0.5 -> 1.0)
                    tackle.RagdollBlendWeight = math.clamp(tackle.ContactTime / ActiveRagdollTackleSystem.TackleContactDuration, 0.0f, 1.0f);

                    // Dampen forward kinematic speed, inject Havok impulse
                    locomotion.CurrentVelocity = math.lerp(locomotion.CurrentVelocity, float3.zero, DeltaTime * 4.0f);
                    physVel.Linear = math.lerp(physVel.Linear, tackle.TackleImpactImpulse, tackle.RagdollBlendWeight);

                    // If forward motion halted and time elapsed -> down by contact / whistle blow
                    if (tackle.ContactTime >= ActiveRagdollTackleSystem.TackleContactDuration ||
                        math.length(locomotion.CurrentVelocity) < ActiveRagdollTackleSystem.WhistleDownVelocityThreshold)
                    {
                        tackle.State = TackleState.WhistleDown;
                        locomotion.CurrentVelocity = float3.zero;
                        locomotion.DesiredVelocity = float3.zero;
                        physVel.Linear = float3.zero;
                    }
                    break;

                case TackleState.PancakedToTurf:
                    tackle.RagdollBlendWeight = 1.0f;
                    tackle.State = TackleState.WhistleDown;
                    locomotion.CurrentVelocity = float3.zero;
                    physVel.Linear = float3.zero;
                    break;
            }
        }

        /// <summary>
        /// Computes momentum transfer between ball carrier and tackler.
        /// </summary>
        [BurstCompile]
        public static float3 ComputeInelasticCollisionVelocity(
            float mCarrier, float3 vCarrier,
            float mTackler, float3 vTackler)
        {
            float totalMass = mCarrier + mTackler;
            if (totalMass < 1.0f)
            {
                return float3.zero;
            }

            float3 compositeMomentum = (mCarrier * vCarrier) + (mTackler * vTackler);
            return compositeMomentum / totalMass;
        }
    }
}
