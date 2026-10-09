using System;
using System.Collections.Generic;
using UnityEngine;
using TitanGlenelg.Core.AI;

namespace TitanGlenelg.Core.Simulation
{
    [System.Serializable]
    public class RosterAthlete
    {
        public string AthleteId;
        public string FullName;
        public int JerseyNumber;
        public PlayerPosition PrimaryPosition;
        public PlayerPosition SecondaryPosition;
        public bool PlaysBothWays;
        public float Stamina; // 0.0 to 100.0
        public float MaxStamina;
        public float SpeedRating;
        public float StrengthRating;
        public float PassThrowPower;
        public int SnapsPlayed;
        public bool IsSubstitutedOut;
        public float CurrentInjuryRisk;
        public float CurrentSpeedMultiplier = 1.0f;
        public float CurrentThrowMultiplier = 1.0f;
        public float CurrentShedMultiplier = 1.0f;
    }

    /// <summary>
    /// Two-Way Ironman Biomechanical Fatigue & Tactical Substitutions Controller.
    /// Simulates continuous physical attrition for high school two-way starters,
    /// enforcing sprint, throw velocity, and block shed penalties, plus depth chart rotations.
    /// </summary>
    public class TwoWayFatigueSystem : MonoBehaviour
    {
        [Header("Fatigue Coefficients")]
        public float SprintDrainPerSecond = 2.4f;
        public float TrenchCollisionDrain = 3.8f;
        public float HuddleRecoveryPerSecond = 0.85f;
        public float BenchRestRecoveryPerSecond = 2.50f;

        [Header("Ironman Multipliers")]
        public float TwoWayDrainMultiplier = 1.65f;

        [Header("Roster Profiles")]
        public List<RosterAthlete> GlenelgRoster = new List<RosterAthlete>();
        public List<RosterAthlete> TitansRoster = new List<RosterAthlete>();

        private void Awake()
        {
            InitializeSampleRosters();
        }

        public void InitializeSampleRosters()
        {
            GlenelgRoster.Clear();

            // Glenelg Two-Way Starters
            GlenelgRoster.Add(new RosterAthlete
            {
                AthleteId = "GLEN_01",
                FullName = "Marcus Vance",
                JerseyNumber = 7,
                PrimaryPosition = PlayerPosition.QB,
                SecondaryPosition = PlayerPosition.FS,
                PlaysBothWays = true,
                Stamina = 100f,
                MaxStamina = 100f,
                SpeedRating = 88f,
                StrengthRating = 76f,
                PassThrowPower = 92f
            });

            GlenelgRoster.Add(new RosterAthlete
            {
                AthleteId = "GLEN_02",
                FullName = "Trenton Miller",
                JerseyNumber = 22,
                PrimaryPosition = PlayerPosition.RB,
                SecondaryPosition = PlayerPosition.MLB,
                PlaysBothWays = true,
                Stamina = 100f,
                MaxStamina = 100f,
                SpeedRating = 91f,
                StrengthRating = 85f,
                PassThrowPower = 50f
            });

            GlenelgRoster.Add(new RosterAthlete
            {
                AthleteId = "GLEN_03",
                FullName = "Jamal Cole",
                JerseyNumber = 11,
                PrimaryPosition = PlayerPosition.WR1,
                SecondaryPosition = PlayerPosition.CB_L,
                PlaysBothWays = true,
                Stamina = 100f,
                MaxStamina = 100f,
                SpeedRating = 94f,
                StrengthRating = 72f,
                PassThrowPower = 45f
            });

            // Fresh Backups
            GlenelgRoster.Add(new RosterAthlete
            {
                AthleteId = "GLEN_BK_01",
                FullName = "Darren Woods",
                JerseyNumber = 24,
                PrimaryPosition = PlayerPosition.RB,
                PlaysBothWays = false,
                Stamina = 100f,
                MaxStamina = 100f,
                SpeedRating = 86f,
                StrengthRating = 79f,
                PassThrowPower = 40f
            });

            GlenelgRoster.Add(new RosterAthlete
            {
                AthleteId = "GLEN_BK_02",
                FullName = "Caleb Stone",
                JerseyNumber = 18,
                PrimaryPosition = PlayerPosition.WR1,
                PlaysBothWays = false,
                Stamina = 100f,
                MaxStamina = 100f,
                SpeedRating = 87f,
                StrengthRating = 68f,
                PassThrowPower = 40f
            });

            // Away Titans Roster
            TitansRoster.Clear();
            TitansRoster.Add(new RosterAthlete
            {
                AthleteId = "TITAN_01",
                FullName = "DeAndre King",
                JerseyNumber = 3,
                PrimaryPosition = PlayerPosition.QB,
                SecondaryPosition = PlayerPosition.SS,
                PlaysBothWays = true,
                Stamina = 100f,
                MaxStamina = 100f,
                SpeedRating = 90f,
                StrengthRating = 78f,
                PassThrowPower = 90f
            });

            TitansRoster.Add(new RosterAthlete
            {
                AthleteId = "TITAN_02",
                FullName = "Xavier Wright",
                JerseyNumber = 21,
                PrimaryPosition = PlayerPosition.RB,
                SecondaryPosition = PlayerPosition.WLB,
                PlaysBothWays = true,
                Stamina = 100f,
                MaxStamina = 100f,
                SpeedRating = 92f,
                StrengthRating = 83f,
                PassThrowPower = 45f
            });

            TitansRoster.Add(new RosterAthlete
            {
                AthleteId = "TITAN_BK_01",
                FullName = "Jordan Hayes",
                JerseyNumber = 33,
                PrimaryPosition = PlayerPosition.RB,
                PlaysBothWays = false,
                Stamina = 100f,
                MaxStamina = 100f,
                SpeedRating = 85f,
                StrengthRating = 77f,
                PassThrowPower = 40f
            });
        }

        /// <summary>
        /// Updates continuous fatigue for a player during live play execution.
        /// </summary>
        public void ApplyLivePlayFatigue(RosterAthlete athlete, float sprintSeconds, bool hadTrenchCollision)
        {
            if (athlete.IsSubstitutedOut)
            {
                return;
            }

            float multiplier = athlete.PlaysBothWays ? TwoWayDrainMultiplier : 1.0f;
            float totalDrain = (sprintSeconds * SprintDrainPerSecond) * multiplier;

            if (hadTrenchCollision)
            {
                totalDrain += TrenchCollisionDrain * multiplier;
            }

            athlete.Stamina = Mathf.Clamp(athlete.Stamina - totalDrain, 0.0f, athlete.MaxStamina);
            athlete.SnapsPlayed++;
            UpdatePenaltiesAndInjuryRisk(athlete);
        }

        /// <summary>
        /// Restores stamina between plays or while resting on the bench.
        /// </summary>
        public void RecoverBetweenPlays(RosterAthlete athlete, float restDurationSeconds)
        {
            float rate = athlete.IsSubstitutedOut ? BenchRestRecoveryPerSecond : HuddleRecoveryPerSecond;
            athlete.Stamina = Mathf.Clamp(athlete.Stamina + (restDurationSeconds * rate), 0.0f, athlete.MaxStamina);
            UpdatePenaltiesAndInjuryRisk(athlete);
        }

        /// <summary>
        /// Updates sprint, throw velocity, block shed penalties, and injury risk curve.
        /// </summary>
        public void UpdatePenaltiesAndInjuryRisk(RosterAthlete athlete)
        {
            float staminaRatio = athlete.Stamina / athlete.MaxStamina;

            // Sprint penalty: 1.0 down to 0.80 (-20%)
            athlete.CurrentSpeedMultiplier = Mathf.Lerp(0.80f, 1.0f, staminaRatio);

            // Pass velocity penalty: 1.0 down to 0.85 (-15%)
            athlete.CurrentThrowMultiplier = Mathf.Lerp(0.85f, 1.0f, staminaRatio);

            // Block shed penalty: 1.0 down to 0.65 (-35%)
            athlete.CurrentShedMultiplier = Mathf.Lerp(0.65f, 1.0f, staminaRatio);

            // Non-linear injury risk: escalates exponentially when stamina drops below 40%
            if (staminaRatio < 0.40f)
            {
                float deficit = (0.40f - staminaRatio) / 0.40f; // 0.0 to 1.0
                athlete.CurrentInjuryRisk = 0.05f + (Mathf.Pow(deficit, 2.2f) * 0.45f); // Up to 50% risk
            }
            else
            {
                athlete.CurrentInjuryRisk = 0.02f; // Baseline 2% incidental contact risk
            }
        }

        public float GetSpeedMultiplier(RosterAthlete athlete)
        {
            UpdatePenaltiesAndInjuryRisk(athlete);
            return athlete.CurrentSpeedMultiplier;
        }

        public float GetThrowVelocityMultiplier(RosterAthlete athlete)
        {
            UpdatePenaltiesAndInjuryRisk(athlete);
            return athlete.CurrentThrowMultiplier;
        }

        public float GetBlockShedMultiplier(RosterAthlete athlete)
        {
            UpdatePenaltiesAndInjuryRisk(athlete);
            return athlete.CurrentShedMultiplier;
        }

        /// <summary>
        /// Tactical substitution: swaps out a fatigued starter for a fresh backup.
        /// </summary>
        public bool PerformTacticalSubstitution(string starterId, string backupId)
        {
            var starter = GlenelgRoster.Find(a => a.AthleteId == starterId) ?? TitansRoster.Find(a => a.AthleteId == starterId);
            var backup = GlenelgRoster.Find(a => a.AthleteId == backupId) ?? TitansRoster.Find(a => a.AthleteId == backupId);

            if (starter != null && backup != null)
            {
                starter.IsSubstitutedOut = true;
                backup.IsSubstitutedOut = false;
                return true;
            }

            return false;
        }
    }
}
