using NUnit.Framework;
using UnityEngine;
using TitanGlenelg.Core.AI;
using TitanGlenelg.Core.Simulation;

namespace TitanGlenelg.Tests
{
    [TestFixture]
    public class TwoWayFatigueTests
    {
        private GameObject _holder;
        private TwoWayFatigueSystem _fatigueSystem;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("FatigueHolder");
            _fatigueSystem = _holder.AddComponent<TwoWayFatigueSystem>();
            _fatigueSystem.InitializeSampleRosters();
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null)
            {
                Object.DestroyImmediate(_holder);
            }
        }

        [Test]
        public void Test_TwoWayIronman_DrainsStaminaFasterThanOneWay()
        {
            var twoWayAthlete = new RosterAthlete
            {
                AthleteId = "TEST_2WAY",
                PlaysBothWays = true,
                Stamina = 100f,
                MaxStamina = 100f
            };

            var oneWayAthlete = new RosterAthlete
            {
                AthleteId = "TEST_1WAY",
                PlaysBothWays = false,
                Stamina = 100f,
                MaxStamina = 100f
            };

            _fatigueSystem.ApplyLivePlayFatigue(twoWayAthlete, 5.0f, true);
            _fatigueSystem.ApplyLivePlayFatigue(oneWayAthlete, 5.0f, true);

            Assert.Less(twoWayAthlete.Stamina, oneWayAthlete.Stamina, "Two-Way Ironman athlete must deplete stamina faster than single-phase athlete");
        }

        [Test]
        public void Test_SprintAndThrowVelocityPenalties_ScaleLinearlyWithFatigue()
        {
            var athlete = new RosterAthlete
            {
                AthleteId = "TEST_PENALTIES",
                Stamina = 0f, // Fully depleted
                MaxStamina = 100f
            };

            float speedMul = _fatigueSystem.GetSpeedMultiplier(athlete);
            float throwMul = _fatigueSystem.GetThrowVelocityMultiplier(athlete);
            float shedMul = _fatigueSystem.GetBlockShedMultiplier(athlete);

            Assert.AreEqual(0.80f, speedMul, 0.001f, "Sprint speed must scale down to exactly 80% (-20% max penalty)");
            Assert.AreEqual(0.85f, throwMul, 0.001f, "Throw velocity must scale down to exactly 85% (-15% max penalty)");
            Assert.AreEqual(0.65f, shedMul, 0.001f, "Block shed multiplier must scale down to exactly 65% (-35% max penalty)");
        }

        [Test]
        public void Test_InjuryRiskEscalation_SpikesBelow40PercentStamina()
        {
            var freshAthlete = new RosterAthlete
            {
                Stamina = 80f,
                MaxStamina = 100f
            };
            _fatigueSystem.UpdatePenaltiesAndInjuryRisk(freshAthlete);

            var exhaustedAthlete = new RosterAthlete
            {
                Stamina = 15f,
                MaxStamina = 100f
            };
            _fatigueSystem.UpdatePenaltiesAndInjuryRisk(exhaustedAthlete);

            Assert.AreEqual(0.02f, freshAthlete.CurrentInjuryRisk, "Fresh athlete should only have 2% baseline injury risk");
            Assert.Greater(exhaustedAthlete.CurrentInjuryRisk, 0.15f, "Exhausted athlete below 40% stamina must exhibit escalated injury risk (>15%)");
        }

        [Test]
        public void Test_TacticalSubstitution_SwapsStarterAndBackup()
        {
            bool success = _fatigueSystem.PerformTacticalSubstitution("GLEN_02", "GLEN_BK_01");
            Assert.IsTrue(success, "Substitution between starter and backup should succeed");

            var starter = _fatigueSystem.GlenelgRoster.Find(a => a.AthleteId == "GLEN_02");
            var backup = _fatigueSystem.GlenelgRoster.Find(a => a.AthleteId == "GLEN_BK_01");

            Assert.IsTrue(starter.IsSubstitutedOut, "Starter must be flagged as substituted out");
            Assert.IsFalse(backup.IsSubstitutedOut, "Backup must be active on field");
        }
    }
}
