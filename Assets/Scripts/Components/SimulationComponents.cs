using System;
using Unity.Entities;
using Unity.Mathematics;

namespace TitanGlenelg.Core.Simulation
{
    public enum GamePeriod : byte
    {
        PreGame = 0,
        Quarter1 = 1,
        Quarter2 = 2,
        Halftime = 3,
        Quarter3 = 4,
        Quarter4 = 5,
        OvertimeKansasPlan = 6,
        Final = 7
    }

    public enum DownState : byte
    {
        First = 1,
        Second = 2,
        Third = 3,
        Fourth = 4
    }

    public enum PlayCallPhase : byte
    {
        PlayCallingHUD = 0,
        HuddleToLine = 1,
        PreSnapAudibles = 2,
        LivePlayActive = 3,
        TackleWhistleBlow = 4,
        PenaltyEnforcement = 5
    }

    public struct NFHSClockState : IComponentData
    {
        public GamePeriod CurrentPeriod;
        public float QuarterTimeRemainingSeconds; // 12-minute quarters = 720.0s
        public float PlayClockSeconds;            // 25.0s or 40.0s
        public bool IsGameClockRunning;
        public bool IsPlayClockRunning;
        public bool ClockStoppedOnFirstDown;      // NFHS rule: clock stops on 1st down until ball ready
        public int KansasOvertimeRound;           // Round count for 10-yard untimed OT
        public byte KansasPossessionTurn;         // 0 = Team A, 1 = Team B
    }

    public struct NFHSDriveState : IComponentData
    {
        public DownState CurrentDown;
        public float LineOfScrimmageYard;   // 0 to 100 yards
        public float LineToGainYard;        // 0 to 100 yards
        public float DistanceToGainYards;
        public AI.TeamSide PossessionTeam;
        public int GlenelgScore;
        public int TitansScore;
        public int GlenelgTimeoutsRemaining;
        public int TitansTimeoutsRemaining;
    }

    public struct TwoWayFatigueData : IComponentData
    {
        public float CurrentStamina;          // 0.0 to 100.0 (starts at 100.0)
        public float MaxStamina;              // Typically 100.0
        public float StaminaDepletionRate;    // Per second of sprinting / contact
        public float StaminaRecoveryRate;     // In huddle / bench rest
        public int TotalSnapsPlayed;          // Increments every play
        public bool PlaysBothWays;            // Ironman two-way starter flag
        public float SprintPenaltyMultiplier; // 1.0 down to 0.80 (-20% speed)
        public float ThrowVelocityMultiplier; // 1.0 down to 0.85 (-15% velocity)
        public float BlockShedMultiplier;     // 1.0 down to 0.65 (-35% shed leverage)
        public float InjuryRiskPercentage;    // Escalates steeply below 40% stamina
        public bool IsSubstitutedOut;         // On bench for tactical backup
    }
}
