using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace TitanGlenelg.Core.AI
{
    /// <summary>
    /// Lineman Trench Warfare Kinematic System.
    /// Governs virtual kinematic lockups, pocket integrity drift (0.3-0.8 m/s backward arc),
    /// periodic 0.4s leverage checks (Delta M), DL sheds, and pancake physics.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct TrenchWarfareSystem : ISystem
    {
        public const float LeverageCheckInterval = 0.40f;
        public const float ShedThreshold = 18.0f;
        public const float PancakeThreshold = 26.0f;
        public const float AnchorThreshold = 18.0f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float dt = SystemAPI.Time.DeltaTime;

            var job = new EvaluateTrenchWarfareJob
            {
                DeltaTime = dt,
                CheckInterval = LeverageCheckInterval
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public static float CalculateLeverageDelta(in LinemanPhysicalAttributes attr, float timeElapsed)
        {
            return EvaluateTrenchWarfareJob.CalculateLeverageDelta(in attr, timeElapsed);
        }
    }

    [BurstCompile]
    public partial struct EvaluateTrenchWarfareJob : IJobEntity
    {
        public float DeltaTime;
        public float CheckInterval;

        public void Execute(
            Entity entity,
            ref TrenchLockupData lockup,
            ref LinemanPhysicalAttributes attributes,
            ref KinematicLocomotionData locomotion,
            ref LocalTransform transform)
        {
            // If lineman is executing a pulling guard assignment (e.g. Counter Trey)
            if (attributes.IsPullingAssignment)
            {
                float3 pullDiff = attributes.PullWaypoint - transform.Position;
                pullDiff.y = 0f;
                float pullDist = math.length(pullDiff);
                if (pullDist > 0.3f)
                {
                    locomotion.DesiredVelocity = (pullDiff / pullDist) * locomotion.MaxSprintSpeedMps;
                    transform.Position += locomotion.DesiredVelocity * DeltaTime;
                    return;
                }
                else
                {
                    // Reached kickout/seal waypoint: complete pull and engage block
                    attributes.IsPullingAssignment = false;
                    lockup.State = TrenchEngagementState.LockedInTrench;
                    lockup.LockupTimer = 0f;
                    lockup.NextLeverageCheckTime = CheckInterval;
                    locomotion.DesiredVelocity = float3.zero;
                    locomotion.CurrentVelocity = float3.zero;
                }
            }

            if (lockup.State == TrenchEngagementState.Unengaged)
            {
                return;
            }

            lockup.LockupTimer += DeltaTime;

            // 1. Pocket Integrity Drift: Conceding depth backward in pass protection
            if (lockup.State == TrenchEngagementState.LockedInTrench)
            {
                // Both engaged linemen drift together along pocket normal (0.3 - 0.8 m/s)
                float3 driftVector = lockup.PocketPocketNormal * lockup.PocketDriftSpeedMps;
                transform.Position += driftVector * DeltaTime;
            }

            // 2. Periodic 0.4s Leverage Check (Delta M)
            if (lockup.LockupTimer >= lockup.NextLeverageCheckTime)
            {
                lockup.NextLeverageCheckTime += CheckInterval;

                // Perform deterministic leverage calculation
                float deltaM = CalculateLeverageDelta(in attributes, lockup.LockupTimer);
                lockup.CurrentLeverageDeltaM = deltaM;

                if (attributes.IsOffensiveLineman)
                {
                    // OL Perspective: Negative deltaM is advantageous for OL
                    if (-deltaM > TrenchWarfareSystem.AnchorThreshold)
                    {
                        // OL Anchors down, halts pocket drift
                        lockup.State = TrenchEngagementState.AnchoredOL;
                        locomotion.DesiredVelocity = float3.zero;
                    }
                    else if (deltaM > TrenchWarfareSystem.PancakeThreshold)
                    {
                        // OL Pancaked to turf
                        lockup.State = TrenchEngagementState.Pancaked;
                        locomotion.DesiredVelocity = float3.zero;
                    }
                    else if (deltaM > TrenchWarfareSystem.ShedThreshold)
                    {
                        // DL shed the block
                        lockup.State = TrenchEngagementState.ShedAdvantageDL;
                    }
                }
                else
                {
                    // DL Perspective: Positive deltaM is advantageous for DL
                    if (deltaM > TrenchWarfareSystem.PancakeThreshold)
                    {
                        lockup.State = TrenchEngagementState.ShedAdvantageDL;
                        // DL charges into backfield
                        locomotion.DesiredVelocity = new float3(0f, 0f, -1f) * locomotion.MaxSprintSpeedMps;
                    }
                    else if (deltaM > TrenchWarfareSystem.ShedThreshold)
                    {
                        lockup.State = TrenchEngagementState.ShedAdvantageDL;
                        // DL rips/swims into backfield
                        locomotion.DesiredVelocity = new float3(0f, 0f, -1f) * (locomotion.MaxSprintSpeedMps * 0.8f);
                    }
                }
            }
        }

        [BurstCompile]
        public static float CalculateLeverageDelta(in LinemanPhysicalAttributes attr, float timeElapsed)
        {
            // Deterministic pseudo-random variation based on time
            float fuzz = math.sin(timeElapsed * 15.0f) * 6.0f;

            if (attr.IsOffensiveLineman)
            {
                // OL defensive score: Anchor + Strength * Stamina
                float olScore = (attr.AnchorRating * 0.6f + attr.StrengthRating * 0.4f) * math.clamp(attr.StaminaEfficiency, 0.65f, 1.0f);
                // Baseline neutral match vs average DL power 75
                float dlScore = 75.0f;
                return (dlScore - olScore) + fuzz;
            }
            else
            {
                // DL offensive score: Power + Leverage * Stamina
                float dlScore = (attr.PowerLeverage * 0.6f + attr.StrengthRating * 0.4f) * math.clamp(attr.StaminaEfficiency, 0.65f, 1.0f);
                float olScore = 75.0f;
                return (dlScore - olScore) + fuzz;
            }
        }
    }
}
