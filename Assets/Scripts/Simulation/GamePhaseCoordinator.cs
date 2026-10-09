using System;
using UnityEngine;
using Unity.Mathematics;
using TitanGlenelg.Core.AI;
using TitanGlenelg.Core.Physics;
using TitanGlenelg.Core.Playbook;

namespace TitanGlenelg.Core.Simulation
{
    /// <summary>
    /// Hybrid Bridge Coordinator connecting DOTS ECS simulation with managed Game State,
    /// NFHS Officiating Engine, Two-Way Fatigue Controller, and Presentation.
    /// Handles user control routing: QB pre-snap & pass -> targeted receiver at release (Catch Triad) -> ball carrier.
    /// </summary>
    public class GamePhaseCoordinator : MonoBehaviour
    {
        [Header("System References")]
        public NFHSRulesAndClockEngine RulesEngine;
        public TwoWayFatigueSystem FatigueSystem;

        [Header("Current Play Session")]
        public PlayCallPhase CurrentPhase = PlayCallPhase.PlayCallingHUD;
        public PlayDefinition SelectedOffensivePlay;
        public PlayDefinition SelectedDefensivePlay;
        public ThrowType QueuedThrowType = ThrowType.Touch;
        public UserCatchMode QueuedCatchMode = UserCatchMode.RAC;
        public Vector3 UserAimPlacementLead = Vector3.zero;

        [Header("Live Play Tracking")]
        public float PlayDurationSeconds = 0f;
        public bool BallThrown = false;
        public bool BallCaught = false;
        public bool TackleWhistleBlown = false;

        public event Action<PlayCallPhase> OnPhaseChanged;
        public event Action<string> OnPlaySummaryAnnounced;

        private void Start()
        {
            if (RulesEngine == null) RulesEngine = GetComponent<NFHSRulesAndClockEngine>();
            if (FatigueSystem == null) FatigueSystem = GetComponent<TwoWayFatigueSystem>();

            SetPhase(PlayCallPhase.PlayCallingHUD);
        }

        public void SetPhase(PlayCallPhase newPhase)
        {
            CurrentPhase = newPhase;
            OnPhaseChanged?.Invoke(newPhase);

            switch (newPhase)
            {
                case PlayCallPhase.PlayCallingHUD:
                    PlayDurationSeconds = 0f;
                    BallThrown = false;
                    BallCaught = false;
                    TackleWhistleBlown = false;
                    break;

                case PlayCallPhase.HuddleToLine:
                    RulesEngine.StartPlayClock(false);
                    break;

                case PlayCallPhase.LivePlayActive:
                    RulesEngine.IsGameClockRunning = true;
                    break;

                case PlayCallPhase.TackleWhistleBlow:
                    RulesEngine.StopGameClock("Tackle Down by Contact");
                    break;
            }
        }

        public void SelectOffensivePlay(PlayDefinition play)
        {
            SelectedOffensivePlay = play;
        }

        public void SelectDefensivePlay(PlayDefinition play)
        {
            SelectedDefensivePlay = play;
        }

        public void SnapBall()
        {
            if (CurrentPhase != PlayCallPhase.HuddleToLine && CurrentPhase != PlayCallPhase.PreSnapAudibles)
            {
                return;
            }

            SetPhase(PlayCallPhase.LivePlayActive);
            OnPlaySummaryAnnounced?.Invoke($"Ball Snapped! Offense: {SelectedOffensivePlay?.PlayName} vs Defense: {SelectedDefensivePlay?.PlayName}");
        }

        public void ExecutePassThrow(ThrowType throwType, Vector3 aimLead)
        {
            if (CurrentPhase != PlayCallPhase.LivePlayActive || BallThrown)
            {
                return;
            }

            BallThrown = true;
            QueuedThrowType = throwType;
            UserAimPlacementLead = aimLead;

            OnPlaySummaryAnnounced?.Invoke($"Pass thrown! Type: {throwType}. Left-Stick placement: {aimLead}");
        }

        public void ExecuteCatchAttempt(UserCatchMode mode)
        {
            if (!BallThrown || BallCaught)
            {
                return;
            }

            QueuedCatchMode = mode;
            BallCaught = true;

            OnPlaySummaryAnnounced?.Invoke($"Receiver triggered catch mode: {mode}");
        }

        public void BlowWhistle(float yardGained, bool outOfBounds, bool incomplete, bool turnover, bool touchdown)
        {
            if (TackleWhistleBlown)
            {
                return;
            }

            TackleWhistleBlown = true;
            SetPhase(PlayCallPhase.TackleWhistleBlow);

            // Process officiating rules
            RulesEngine.ProcessPlayResult(yardGained, outOfBounds, incomplete, turnover, touchdown);

            // Apply snap fatigue to roster
            if (FatigueSystem != null)
            {
                foreach (var athlete in FatigueSystem.GlenelgRoster)
                {
                    FatigueSystem.ApplyLivePlayFatigue(athlete, Mathf.Max(2.5f, PlayDurationSeconds), true);
                    FatigueSystem.RecoverBetweenPlays(athlete, 12f);
                }
            }

            OnPlaySummaryAnnounced?.Invoke($"Play over. Gain: {yardGained:F1} yds. Next: {RulesEngine.CurrentDown} & {Mathf.CeilToInt(RulesEngine.DistanceToGainYards)}");
        }
    }
}
