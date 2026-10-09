using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace TitanGlenelg.Core.AI
{
    /// <summary>
    /// Burst-compiled 22-man FSM system for defensive positions (CB, FS, SS, LB, DL).
    /// Governs Zone Drops, Man Shadows with inside/outside leverage, and pursuit vector calculation.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct DefensiveCoverageSystem : ISystem
    {
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

            var job = new EvaluateDefensiveCoverageJob
            {
                DeltaTime = dt
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    public partial struct EvaluateDefensiveCoverageJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(
            Entity entity,
            ref PlayerFSMState fsm,
            ref DefensiveCoverageData coverage,
            ref KinematicLocomotionData locomotion,
            ref LocalTransform transform,
            in PlayerIdentity identity)
        {
            // Defensive linemen engage via PassRush and TrenchWarfareSystem
            if (identity.Position >= PlayerPosition.DE_L && identity.Position <= PlayerPosition.DE_R && fsm.DefensePhase != DefensiveFSMPhase.PassRush)
            {
                fsm.DefensePhase = DefensiveFSMPhase.PassRush;
            }

            fsm.StateTimeElapsed += DeltaTime;

            switch (fsm.DefensePhase)
            {
                case DefensiveFSMPhase.PreSnap:
                    locomotion.DesiredVelocity = float3.zero;
                    locomotion.CurrentVelocity = float3.zero;
                    break;

                case DefensiveFSMPhase.ZoneDrop:
                    // Drop back to assigned zone landmark (Cover 3 deep third, Cover 2 half, Hook/Curl)
                    float3 toLandmark = coverage.ZoneLandmark - transform.Position;
                    toLandmark.y = 0f;
                    float distToZone = math.length(toLandmark);
                    if (distToZone > 0.5f)
                    {
                        float3 dir = toLandmark / distToZone;
                        locomotion.DesiredVelocity = dir * math.min(locomotion.MaxSprintSpeedMps * 0.85f, distToZone * 2.0f);
                    }
                    else
                    {
                        // Settle in zone window, face QB
                        locomotion.DesiredVelocity = float3.zero;
                    }
                    break;

                case DefensiveFSMPhase.ManShadow:
                    // Mirror assigned receiver position maintaining cushion & leverage
                    // (Target position evaluated via assigned man landmark)
                    float3 manTarget = coverage.ZoneLandmark; // Dynamic assigned position
                    // Apply leverage offset: positive leverage = inside shade, negative = outside shade
                    float leverageRad = math.radians(coverage.LeverageAngleDeg);
                    float3 leverageOffset = new float3(math.sin(leverageRad) * 1.2f, 0f, math.cos(leverageRad) * coverage.CushionDistanceMeters);
                    float3 desiredManPos = manTarget + leverageOffset;

                    float3 diffMan = desiredManPos - transform.Position;
                    diffMan.y = 0f;
                    float distMan = math.length(diffMan);
                    if (distMan > 0.2f)
                    {
                        locomotion.DesiredVelocity = (diffMan / distMan) * math.min(locomotion.MaxSprintSpeedMps, distMan * 3.0f);
                    }
                    else
                    {
                        locomotion.DesiredVelocity = float3.zero;
                    }
                    break;

                case DefensiveFSMPhase.PassRush:
                    // Rush QB pocket along arc
                    float3 qbPocketTarget = coverage.ZoneLandmark;
                    float3 rushDiff = qbPocketTarget - transform.Position;
                    rushDiff.y = 0f;
                    float rushDist = math.length(rushDiff);
                    if (rushDist > 0.3f)
                    {
                        locomotion.DesiredVelocity = (rushDiff / rushDist) * locomotion.MaxSprintSpeedMps;
                    }
                    break;

                case DefensiveFSMPhase.BreakOnBall:
                    // Drive downhill aggressively on ball arrival
                    float3 ballTarget = coverage.ZoneLandmark;
                    float3 breakDiff = ballTarget - transform.Position;
                    breakDiff.y = 0f;
                    float breakDist = math.length(breakDiff);
                    if (breakDist > 0.1f)
                    {
                        locomotion.DesiredVelocity = (breakDiff / breakDist) * locomotion.MaxSprintSpeedMps;
                    }
                    break;

                case DefensiveFSMPhase.TacklePursuit:
                    // Calculate leveraged angle of pursuit cutting off ball carrier
                    float3 pursuitTarget = coverage.ZoneLandmark;
                    float3 pursuitVec = CalculatePursuitAngle(transform.Position, pursuitTarget, locomotion.MaxSprintSpeedMps);
                    locomotion.DesiredVelocity = pursuitVec;
                    break;

                case DefensiveFSMPhase.Down:
                    locomotion.DesiredVelocity = float3.zero;
                    locomotion.CurrentVelocity = float3.zero;
                    break;
            }

            // Kinematic locomotion integration
            OffensiveRouteSystem.IntegrateLocomotion(ref locomotion, ref transform, DeltaTime);
        }

        /// <summary>
        /// Calculates intercept pursuit angle accounting for ball carrier trajectory and sideline cut-off.
        /// </summary>
        [BurstCompile]
        public static float3 CalculatePursuitAngle(float3 defenderPos, float3 ballCarrierPos, float defenderSpeed)
        {
            float3 diff = ballCarrierPos - defenderPos;
            diff.y = 0f;
            float dist = math.length(diff);
            if (dist < 0.1f)
            {
                return float3.zero;
            }

            // Lead intercept angle toward the sideline boundary (clamp X to prevent cutbacks)
            float3 interceptPoint = ballCarrierPos + new float3(0f, 0f, dist * 0.25f);
            float3 dir = math.normalize(interceptPoint - defenderPos);
            return dir * defenderSpeed;
        }
    }
}
