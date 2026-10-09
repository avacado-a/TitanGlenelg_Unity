using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace TitanGlenelg.Core.Physics
{
    /// <summary>
    /// User Catch Triad (RAC, Aggressive, Possession) and Contested Knockout System.
    /// Evaluates arrival of ball at receiver, catch mode modifiers, defender contest proximity,
    /// and tackle impact knockout probability.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct CatchResolutionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FootballTag>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new EvaluateCatchTriadJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            };
            job.ScheduleParallel();
        }
    }

    [BurstCompile]
    public partial struct EvaluateCatchTriadJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(
            Entity receiverEntity,
            ref CatchTargetData catchTarget,
            ref AI.PlayerFSMState fsmState,
            ref AI.KinematicLocomotionData locomotion,
            in LocalTransform transform,
            in AI.PlayerIdentity identity)
        {
            if (catchTarget.CatchSecured || catchTarget.KnockoutOccurred)
            {
                return;
            }

            // Only evaluate during active catch window phase
            if (fsmState.OffensePhase != AI.OffensiveFSMPhase.CatchWindow)
            {
                return;
            }

            fsmState.StateTimeElapsed += DeltaTime;

            // Base catch radius adjusted by user catch mode
            float effectiveCatchRadius = catchTarget.CatchRadius > 0.1f ? catchTarget.CatchRadius : 1.6f;
            float verticalCatchBonus = 0.0f;

            switch (catchTarget.CatchMode)
            {
                case UserCatchMode.RAC:
                    // Focused on forward running momentum; moderate radius, zero vertical bonus
                    break;

                case UserCatchMode.Aggressive:
                    // High-pointing jump: elevates catch radius vertically by +0.75m
                    verticalCatchBonus = 0.75f;
                    effectiveCatchRadius += 0.35f;
                    break;

                case UserCatchMode.Possession:
                    // Tight body catch: slightly tighter catch radius (-0.2m), but high security
                    effectiveCatchRadius = math.max(1.0f, effectiveCatchRadius - 0.2f);
                    break;
            }

            // In full game loop, position of ball is compared to receiver head/hands
            // When in range, determine outcome based on catch mode:
            if (catchTarget.CatchAttempted && !catchTarget.CatchSecured && !catchTarget.KnockoutOccurred)
            {
                ApplyCatchOutcome(ref catchTarget, ref fsmState, ref locomotion);
            }
        }

        [BurstCompile]
        public static void ApplyCatchOutcome(
            ref CatchTargetData catchTarget,
            ref AI.PlayerFSMState fsmState,
            ref AI.KinematicLocomotionData locomotion)
        {
            switch (catchTarget.CatchMode)
            {
                case UserCatchMode.RAC:
                    // Retain forward momentum, burst into YAC phase
                    catchTarget.CatchSecured = true;
                    fsmState.OffensePhase = AI.OffensiveFSMPhase.RunAfterCatch;
                    locomotion.CurrentVelocity *= 1.10f; // +10% burst speed
                    break;

                case UserCatchMode.Aggressive:
                    // Secured high-point, transition to run or contest
                    catchTarget.CatchSecured = true;
                    fsmState.OffensePhase = AI.OffensiveFSMPhase.RunAfterCatch;
                    locomotion.CurrentVelocity *= 0.85f; // Slight deceleration from landing
                    break;

                case UserCatchMode.Possession:
                    // Fall to turf or secure toe-tap; zero YAC, guaranteed down
                    catchTarget.CatchSecured = true;
                    fsmState.OffensePhase = AI.OffensiveFSMPhase.Down;
                    locomotion.DesiredVelocity = float3.zero;
                    locomotion.CurrentVelocity = float3.zero;
                    break;

                default:
                    catchTarget.CatchSecured = true;
                    fsmState.OffensePhase = AI.OffensiveFSMPhase.RunAfterCatch;
                    break;
            }
        }

        /// <summary>
        /// Evaluates contested hit knockout probability based on catch mode and defender impact.
        /// </summary>
        [BurstCompile]
        public static bool EvaluateContestedKnockout(
            UserCatchMode catchMode,
            float receiverCatchRating,
            float defenderImpactForce,
            float randomSeed)
        {
            // Base knockout risk from defender impact
            float baseKnockoutRisk = math.clamp(defenderImpactForce / 1200.0f, 0.15f, 0.85f);

            // Catch rating mitigation (e.g. 85 catch rating mitigates risk)
            float skillMitigation = math.clamp(receiverCatchRating / 100.0f, 0.5f, 1.0f);
            baseKnockoutRisk *= (1.4f - skillMitigation);

            float modeMultiplier;
            switch (catchMode)
            {
                case UserCatchMode.RAC:
                    // Receiver exposed while looking upfield: +40% knockout vulnerability
                    modeMultiplier = 1.40f;
                    break;

                case UserCatchMode.Aggressive:
                    // High-point battle: moderate resistance
                    modeMultiplier = 1.0f;
                    break;

                case UserCatchMode.Possession:
                    // Tucked against body: suppresses knockout probability by 75%
                    modeMultiplier = 0.25f;
                    break;

                default:
                    modeMultiplier = 1.0f;
                    break;
            }

            float finalProbability = math.clamp(baseKnockoutRisk * modeMultiplier, 0.05f, 0.95f);
            return randomSeed < finalProbability;
        }
    }
}
