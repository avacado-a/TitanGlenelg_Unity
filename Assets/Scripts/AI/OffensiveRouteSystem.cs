using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace TitanGlenelg.Core.AI
{
    /// <summary>
    /// Burst-compiled 22-man FSM system for offensive skill positions (WR, TE, RB).
    /// Evaluates route progress along cubic Bezier splines through Stem, Cut, and Catch Window phases.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct OffensiveRouteSystem : ISystem
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

            var job = new EvaluateOffensiveRoutesJob
            {
                DeltaTime = dt
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        [BurstCompile]
        public static void IntegrateLocomotion(ref KinematicLocomotionData loco, ref LocalTransform transform, float dt)
        {
            EvaluateOffensiveRoutesJob.IntegrateLocomotion(ref loco, ref transform, dt);
        }
    }

    [BurstCompile]
    public partial struct EvaluateOffensiveRoutesJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(
            Entity entity,
            ref PlayerFSMState fsm,
            ref RouteSplineData route,
            ref KinematicLocomotionData locomotion,
            ref LocalTransform transform,
            in PlayerIdentity identity)
        {
            // Linemen are handled by TrenchWarfareSystem; ignore skill route splines
            if (identity.Position >= PlayerPosition.LT && identity.Position <= PlayerPosition.RT)
            {
                return;
            }

            fsm.StateTimeElapsed += DeltaTime;

            switch (fsm.OffensePhase)
            {
                case OffensiveFSMPhase.PreSnap:
                    // Waiting for snap; locked in stance
                    locomotion.DesiredVelocity = float3.zero;
                    locomotion.CurrentVelocity = float3.zero;
                    break;

                case OffensiveFSMPhase.SnapAndDrop:
                    // QB 3-step or 5-step dropback or RB initial mesh
                    if (identity.Position == PlayerPosition.QB)
                    {
                        // Dropback backwards 4-6 yards
                        float3 dropDir = new float3(0f, 0f, -1f);
                        locomotion.DesiredVelocity = dropDir * 4.5f;
                        if (fsm.StateTimeElapsed >= 1.2f)
                        {
                            fsm.OffensePhase = OffensiveFSMPhase.CatchWindow; // Set feet to pass
                            locomotion.DesiredVelocity = float3.zero;
                        }
                    }
                    else
                    {
                        // Release off line
                        fsm.OffensePhase = OffensiveFSMPhase.Stem;
                    }
                    break;

                case OffensiveFSMPhase.Stem:
                    // Driving vertical upfield to threaten defender's cushion
                    route.RouteProgressT = math.clamp(route.RouteProgressT + (DeltaTime / math.max(0.1f, route.StemDurationSeconds)), 0.0f, 0.5f);
                    float3 stemTarget = EvaluateCubicBezier(route.Point0, route.Point1, route.Point2, route.Point3, route.RouteProgressT);
                    UpdateLocomotionTowardTarget(stemTarget, ref locomotion, transform.Position);

                    // Reached top of route stem -> trigger Cut phase
                    if (route.RouteProgressT >= 0.5f)
                    {
                        fsm.OffensePhase = OffensiveFSMPhase.Cut;
                        fsm.StateTimeElapsed = 0f;
                    }
                    break;

                case OffensiveFSMPhase.Cut:
                    // Planting outside foot and accelerating sharply in new direction
                    route.RouteProgressT = math.clamp(route.RouteProgressT + (DeltaTime * route.CutSharpnessFactor * 0.8f), 0.5f, 0.85f);
                    float3 cutTarget = EvaluateCubicBezier(route.Point0, route.Point1, route.Point2, route.Point3, route.RouteProgressT);
                    UpdateLocomotionTowardTarget(cutTarget, ref locomotion, transform.Position);

                    // Cut complete -> enter Catch Window
                    if (route.RouteProgressT >= 0.85f || fsm.StateTimeElapsed >= 0.6f)
                    {
                        fsm.OffensePhase = OffensiveFSMPhase.CatchWindow;
                        fsm.StateTimeElapsed = 0f;
                    }
                    break;

                case OffensiveFSMPhase.CatchWindow:
                    // Tracking the ball, adjusting stride, looking back to QB
                    route.RouteProgressT = math.clamp(route.RouteProgressT + (DeltaTime * 0.15f), 0.85f, 1.0f);
                    float3 finalTarget = EvaluateCubicBezier(route.Point0, route.Point1, route.Point2, route.Point3, route.RouteProgressT);
                    UpdateLocomotionTowardTarget(finalTarget, ref locomotion, transform.Position);
                    break;

                case OffensiveFSMPhase.RunAfterCatch:
                    // Ball caught; navigating upfield away from nearest pursuit
                    float3 endzoneDir = new float3(0f, 0f, 1f);
                    locomotion.DesiredVelocity = endzoneDir * locomotion.MaxSprintSpeedMps;
                    break;

                case OffensiveFSMPhase.Down:
                    locomotion.DesiredVelocity = float3.zero;
                    locomotion.CurrentVelocity = float3.zero;
                    break;
            }

            // Integrate kinematic locomotion into position and orientation
            IntegrateLocomotion(ref locomotion, ref transform, DeltaTime);
        }

        [BurstCompile]
        public static float3 EvaluateCubicBezier(float3 p0, float3 p1, float3 p2, float3 p3, float t)
        {
            float u = 1.0f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            float3 p = uuu * p0;
            p += 3.0f * uu * t * p1;
            p += 3.0f * u * tt * p2;
            p += ttt * p3;

            return p;
        }

        [BurstCompile]
        public static void UpdateLocomotionTowardTarget(float3 targetPos, ref KinematicLocomotionData loco, float3 currentPos)
        {
            float3 diff = targetPos - currentPos;
            diff.y = 0f;
            float dist = math.length(diff);
            if (dist > 0.05f)
            {
                float3 dir = diff / dist;
                float targetSpeed = math.min(loco.MaxSprintSpeedMps, dist / 0.2f);
                loco.DesiredVelocity = dir * targetSpeed;
            }
            else
            {
                loco.DesiredVelocity = float3.zero;
            }
        }

        [BurstCompile]
        public static void IntegrateLocomotion(ref KinematicLocomotionData loco, ref LocalTransform transform, float dt)
        {
            // Smooth acceleration
            loco.CurrentVelocity = math.lerp(loco.CurrentVelocity, loco.DesiredVelocity, math.saturate(loco.AccelerationMps2 * dt));
            transform.Position += loco.CurrentVelocity * dt;

            // Turn heading toward velocity direction
            if (math.lengthsq(loco.CurrentVelocity) > 0.1f)
            {
                float3 forward = math.normalize(new float3(loco.CurrentVelocity.x, 0f, loco.CurrentVelocity.z));
                transform.Rotation = quaternion.LookRotationSafe(forward, new float3(0f, 1f, 0f));
            }
        }
    }
}
