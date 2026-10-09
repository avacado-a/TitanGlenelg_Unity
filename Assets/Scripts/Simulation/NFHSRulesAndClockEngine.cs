using System;
using UnityEngine;
using TitanGlenelg.Core.AI;

namespace TitanGlenelg.Core.Simulation
{
    /// <summary>
    /// Full 4-Quarter NFHS Officiating and Clock Engine.
    /// Implements official National Federation of State High School Associations (NFHS) football rules:
    /// - 12-minute quarters (720 seconds)
    /// - Clock stoppages on first downs until ball ready for play
    /// - 15-yard Defensive Pass Interference (DPI) cap (NFHS Rule 7-5-10)
    /// - 25s / 40s Play Clock with 5-yard Delay of Game
    /// - Untimed Kansas Plan Overtime from the 10-yard line (NFHS Overtime Procedure)
    /// </summary>
    public class NFHSRulesAndClockEngine : MonoBehaviour
    {
        public const float QuarterDurationSeconds = 720.0f; // 12 minutes
        public const float PlayClockStandardSeconds = 40.0f;
        public const float PlayClockAdministrativeSeconds = 25.0f;
        public const float KansasOvertimeStartingYard = 10.0f; // 10-yard line
        public const float MaxDpiPenaltyYards = 15.0f;         // NFHS 15-yard cap

        [Header("Clock State")]
        public GamePeriod CurrentPeriod = GamePeriod.Quarter1;
        public float QuarterTimeRemaining = QuarterDurationSeconds;
        public float PlayClockRemaining = PlayClockStandardSeconds;
        public bool IsGameClockRunning = false;
        public bool IsPlayClockRunning = false;
        public bool ClockStoppedOnFirstDown = false;

        [Header("Drive State")]
        public DownState CurrentDown = DownState.First;
        public float LineOfScrimmageYard = 25.0f; // Glenelg 25 yard line
        public float LineToGainYard = 35.0f;
        public float DistanceToGainYards = 10.0f;
        public TeamSide PossessionTeam = TeamSide.HomeGlenelg;

        [Header("Scoreboard")]
        public int GlenelgScore = 0;
        public int TitansScore = 0;
        public int GlenelgTimeoutsRemaining = 3;
        public int TitansTimeoutsRemaining = 3;

        [Header("Kansas Overtime State")]
        public int OvertimeRound = 0;
        public int OvertimePossessionIndex = 0; // 0 = Team A, 1 = Team B
        public int OvertimeTeamAScore = 0;
        public int OvertimeTeamBScore = 0;

        public event Action<string> OnRefereeAnnouncement;
        public event Action OnFirstDownAchieved;
        public event Action<TeamSide, int> OnScoreChanged;
        public event Action OnQuarterEnded;
        public event Action OnGameEnded;

        private void Update()
        {
            UpdateClocks(Time.deltaTime);
        }

        public void UpdateClocks(float dt)
        {
            if (CurrentPeriod == GamePeriod.Final || CurrentPeriod == GamePeriod.Halftime)
            {
                return;
            }

            // Play clock countdown
            if (IsPlayClockRunning)
            {
                PlayClockRemaining -= dt;
                if (PlayClockRemaining <= 0f)
                {
                    PlayClockRemaining = 0f;
                    IsPlayClockRunning = false;
                    EnforcePenalty("Delay of Game", 5.0f, false, false);
                }
            }

            // Game clock countdown (Kansas OT is untimed)
            if (IsGameClockRunning && CurrentPeriod != GamePeriod.OvertimeKansasPlan)
            {
                QuarterTimeRemaining -= dt;
                if (QuarterTimeRemaining <= 0f)
                {
                    QuarterTimeRemaining = 0f;
                    IsGameClockRunning = false;
                    AdvanceQuarter();
                }
            }
        }

        public void StartPlayClock(bool isAdministrative = false)
        {
            PlayClockRemaining = isAdministrative ? PlayClockAdministrativeSeconds : PlayClockStandardSeconds;
            IsPlayClockRunning = true;
        }

        public void SignalBallReadyForPlay()
        {
            // NFHS Rule: If clock was stopped solely for a first down, restart game clock when ball marked ready
            if (ClockStoppedOnFirstDown)
            {
                ClockStoppedOnFirstDown = false;
                IsGameClockRunning = true;
            }
            StartPlayClock(true);
        }

        public void StopGameClock(string reason)
        {
            IsGameClockRunning = false;
            OnRefereeAnnouncement?.Invoke($"Clock Stopped: {reason}");
        }

        public void ProcessPlayResult(float yardsGained, bool outOfBounds, bool incompletePass, bool turnover, bool touchdown)
        {
            IsPlayClockRunning = false;

            if (touchdown)
            {
                AwardTouchdown(PossessionTeam);
                return;
            }

            if (incompletePass)
            {
                StopGameClock("Incomplete Pass");
                AdvanceDown(0f);
                return;
            }

            if (turnover)
            {
                StopGameClock("Turnover");
                ExecuteTurnover();
                return;
            }

            // Advance line of scrimmage
            LineOfScrimmageYard += yardsGained;
            DistanceToGainYards -= yardsGained;

            if (outOfBounds)
            {
                StopGameClock("Runner Out of Bounds");
            }

            // Check for First Down
            if (DistanceToGainYards <= 0f)
            {
                AchieveFirstDown();
            }
            else
            {
                AdvanceDown(yardsGained);
            }
        }

        private void AchieveFirstDown()
        {
            CurrentDown = DownState.First;
            DistanceToGainYards = 10.0f;
            LineToGainYard = LineOfScrimmageYard + 10.0f;

            // NFHS Rule: Stop clock on first down until referee signals ball ready for play
            ClockStoppedOnFirstDown = true;
            IsGameClockRunning = false;

            OnFirstDownAchieved?.Invoke();
            OnRefereeAnnouncement?.Invoke("First Down! Clock stopped pending ball placement.");
        }

        private void AdvanceDown(float yardsGained)
        {
            if (CurrentDown == DownState.Fourth)
            {
                // Turnover on downs
                OnRefereeAnnouncement?.Invoke("Turnover on Downs!");
                ExecuteTurnover();
                return;
            }

            CurrentDown = (DownState)((byte)CurrentDown + 1);
            OnRefereeAnnouncement?.Invoke($"{CurrentDown} & {Mathf.CeilToInt(DistanceToGainYards)}");
        }

        private void ExecuteTurnover()
        {
            PossessionTeam = PossessionTeam == TeamSide.HomeGlenelg ? TeamSide.AwayTitans : TeamSide.HomeGlenelg;
            CurrentDown = DownState.First;
            DistanceToGainYards = 10.0f;
            LineToGainYard = LineOfScrimmageYard + 10.0f;
            ClockStoppedOnFirstDown = false;
            StartPlayClock(true);
        }

        public void AwardTouchdown(TeamSide scoringTeam)
        {
            StopGameClock("TOUCHDOWN!");
            int points = 6;
            if (scoringTeam == TeamSide.HomeGlenelg)
            {
                GlenelgScore += points;
            }
            else
            {
                TitansScore += points;
            }

            OnScoreChanged?.Invoke(scoringTeam, points);
            OnRefereeAnnouncement?.Invoke($"TOUCHDOWN! {scoringTeam} scores 6 points.");

            // Automatic Point After Touchdown (PAT) setup
            LineOfScrimmageYard = 3.0f; // NFHS 3-yard line for PAT
        }

        /// <summary>
        /// Enforces penalties adhering strictly to NFHS rule caps (e.g. 15-yard DPI cap).
        /// </summary>
        public void EnforcePenalty(string penaltyName, float penaltyYards, bool againstOffense, bool autoFirstDown)
        {
            // NFHS Rule 7-5-10: Pass interference is capped at 15 yards from previous spot (not spot foul)
            if (penaltyName.Contains("Pass Interference") && penaltyYards > MaxDpiPenaltyYards)
            {
                penaltyYards = MaxDpiPenaltyYards;
            }

            if (againstOffense)
            {
                LineOfScrimmageYard = Mathf.Max(1.0f, LineOfScrimmageYard - penaltyYards);
                DistanceToGainYards += penaltyYards;
            }
            else
            {
                LineOfScrimmageYard = Mathf.Min(99.0f, LineOfScrimmageYard + penaltyYards);
                DistanceToGainYards -= penaltyYards;

                if (autoFirstDown || DistanceToGainYards <= 0f)
                {
                    AchieveFirstDown();
                    OnRefereeAnnouncement?.Invoke($"Penalty: {penaltyName}. {penaltyYards} yards. Automatic 1st Down!");
                    return;
                }
            }

            OnRefereeAnnouncement?.Invoke($"Penalty: {penaltyName}. {penaltyYards} yards.");
        }

        public void AdvanceQuarter()
        {
            switch (CurrentPeriod)
            {
                case GamePeriod.Quarter1:
                    CurrentPeriod = GamePeriod.Quarter2;
                    QuarterTimeRemaining = QuarterDurationSeconds;
                    OnRefereeAnnouncement?.Invoke("End of 1st Quarter.");
                    break;

                case GamePeriod.Quarter2:
                    CurrentPeriod = GamePeriod.Halftime;
                    OnRefereeAnnouncement?.Invoke("Halftime.");
                    break;

                case GamePeriod.Halftime:
                    CurrentPeriod = GamePeriod.Quarter3;
                    QuarterTimeRemaining = QuarterDurationSeconds;
                    OnRefereeAnnouncement?.Invoke("Start of 3rd Quarter.");
                    break;

                case GamePeriod.Quarter3:
                    CurrentPeriod = GamePeriod.Quarter4;
                    QuarterTimeRemaining = QuarterDurationSeconds;
                    OnRefereeAnnouncement?.Invoke("End of 3rd Quarter.");
                    break;

                case GamePeriod.Quarter4:
                    if (GlenelgScore == TitansScore)
                    {
                        InitializeKansasPlanOvertime();
                    }
                    else
                    {
                        CurrentPeriod = GamePeriod.Final;
                        OnRefereeAnnouncement?.Invoke("Final Whistle! Game Over.");
                        OnGameEnded?.Invoke();
                    }
                    break;

                case GamePeriod.OvertimeKansasPlan:
                    ProcessKansasOvertimeConclusion();
                    break;
            }

            OnQuarterEnded?.Invoke();
        }

        /// <summary>
        /// Initializes Kansas Plan untimed overtime:
        /// Both teams receive one possession starting at the opponent's 10-yard line with 4 downs.
        /// </summary>
        public void InitializeKansasPlanOvertime()
        {
            CurrentPeriod = GamePeriod.OvertimeKansasPlan;
            OvertimeRound = 1;
            OvertimePossessionIndex = 0;
            OvertimeTeamAScore = 0;
            OvertimeTeamBScore = 0;

            PossessionTeam = TeamSide.HomeGlenelg;
            LineOfScrimmageYard = KansasOvertimeStartingYard;
            DistanceToGainYards = 10.0f;
            CurrentDown = DownState.First;
            IsGameClockRunning = false; // Kansas OT is untimed

            OnRefereeAnnouncement?.Invoke("Kansas Plan Overtime: Glenelg takes possession at the 10-yard line untimed.");
        }

        public void ProcessKansasOvertimeConclusion()
        {
            if (OvertimePossessionIndex == 0)
            {
                // Team A finished drive; now Team B gets possession from the 10-yard line
                OvertimePossessionIndex = 1;
                PossessionTeam = TeamSide.AwayTitans;
                LineOfScrimmageYard = KansasOvertimeStartingYard;
                DistanceToGainYards = 10.0f;
                CurrentDown = DownState.First;
                OnRefereeAnnouncement?.Invoke("Kansas Overtime: Titans take possession at the 10-yard line.");
            }
            else
            {
                // Both teams had possession
                if (GlenelgScore != TitansScore)
                {
                    CurrentPeriod = GamePeriod.Final;
                    OnRefereeAnnouncement?.Invoke($"Kansas Overtime Final: {(GlenelgScore > TitansScore ? "Glenelg" : "Titans")} Wins!");
                    OnGameEnded?.Invoke();
                }
                else
                {
                    // Still tied: advance to next Kansas Overtime Round
                    OvertimeRound++;
                    OvertimePossessionIndex = 0;
                    PossessionTeam = TeamSide.AwayTitans; // Alternate first possession
                    LineOfScrimmageYard = KansasOvertimeStartingYard;
                    DistanceToGainYards = 10.0f;
                    CurrentDown = DownState.First;
                    OnRefereeAnnouncement?.Invoke($"Kansas Overtime Round {OvertimeRound}: Tied! Starting next possession.");
                }
            }
        }
    }
}
