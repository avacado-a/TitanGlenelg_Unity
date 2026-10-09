using NUnit.Framework;
using UnityEngine;
using TitanGlenelg.Core.AI;
using TitanGlenelg.Core.Simulation;

namespace TitanGlenelg.Tests
{
    [TestFixture]
    public class NFHSRulesTests
    {
        private GameObject _holder;
        private NFHSRulesAndClockEngine _rulesEngine;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("RulesEngineHolder");
            _rulesEngine = _holder.AddComponent<NFHSRulesAndClockEngine>();
            _rulesEngine.CurrentPeriod = GamePeriod.Quarter1;
            _rulesEngine.QuarterTimeRemaining = 720.0f; // 12-minute quarter
            _rulesEngine.LineOfScrimmageYard = 25.0f;
            _rulesEngine.DistanceToGainYards = 10.0f;
            _rulesEngine.CurrentDown = DownState.First;
            _rulesEngine.IsGameClockRunning = true;
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
        public void Test_QuarterDuration_IsNFHSStandard12Minutes()
        {
            Assert.AreEqual(720.0f, NFHSRulesAndClockEngine.QuarterDurationSeconds, "NFHS quarters must be exactly 720 seconds (12 minutes)");
        }

        [Test]
        public void Test_FirstDownAchieved_StopsClockUntilBallReady()
        {
            // Simulate 12-yard run (First Down achieved)
            _rulesEngine.ProcessPlayResult(12.0f, outOfBounds: false, incompletePass: false, turnover: false, touchdown: false);

            Assert.AreEqual(DownState.First, _rulesEngine.CurrentDown, "Current down must reset to 1st Down");
            Assert.AreEqual(10.0f, _rulesEngine.DistanceToGainYards, "Distance to gain must be reset to 10 yards");
            Assert.IsFalse(_rulesEngine.IsGameClockRunning, "Game clock must stop on first down per NFHS rules");
            Assert.IsTrue(_rulesEngine.ClockStoppedOnFirstDown, "First down clock stop flag must be true");

            // Referee marks ball ready for play
            _rulesEngine.SignalBallReadyForPlay();
            Assert.IsTrue(_rulesEngine.IsGameClockRunning, "Clock must restart when referee signals ready for play");
            Assert.IsFalse(_rulesEngine.ClockStoppedOnFirstDown, "First down flag must reset after whistle");
        }

        [Test]
        public void Test_DpiPenaltyCap_NeverExceeds15Yards()
        {
            float initialYard = _rulesEngine.LineOfScrimmageYard;

            // Attempt to enforce 30-yard DPI (e.g. deep NFL style foul)
            _rulesEngine.EnforcePenalty("Defensive Pass Interference", 30.0f, againstOffense: false, autoFirstDown: true);

            float enforcedYards = _rulesEngine.LineOfScrimmageYard - initialYard;
            Assert.AreEqual(15.0f, enforcedYards, "NFHS DPI penalty must be strictly capped at 15 yards (Rule 7-5-10)");
            Assert.AreEqual(DownState.First, _rulesEngine.CurrentDown, "DPI must result in 1st down");
        }

        [Test]
        public void Test_DelayOfGame_EnforcesFiveYardPenalty()
        {
            _rulesEngine.StartPlayClock();
            _rulesEngine.PlayClockRemaining = 0.5f;

            // Advance clock past expiration
            _rulesEngine.UpdateClocks(1.0f);

            Assert.AreEqual(0.0f, _rulesEngine.PlayClockRemaining, "Play clock should be zeroed");
            Assert.AreEqual(20.0f, _rulesEngine.LineOfScrimmageYard, "Delay of Game must back up offense by 5 yards (25 -> 20)");
            Assert.AreEqual(15.0f, _rulesEngine.DistanceToGainYards, "Distance to gain must increase to 15 yards");
        }

        [Test]
        public void Test_KansasPlanOvertime_StartsAt10YardLineUntimed()
        {
            _rulesEngine.GlenelgScore = 21;
            _rulesEngine.TitansScore = 21;
            _rulesEngine.CurrentPeriod = GamePeriod.Quarter4;

            _rulesEngine.AdvanceQuarter();

            Assert.AreEqual(GamePeriod.OvertimeKansasPlan, _rulesEngine.CurrentPeriod, "Tied game after Q4 must enter Kansas Plan Overtime");
            Assert.AreEqual(10.0f, _rulesEngine.LineOfScrimmageYard, "Kansas Plan overtime must start at the opponent's 10-yard line");
            Assert.IsFalse(_rulesEngine.IsGameClockRunning, "Kansas Plan overtime is untimed; game clock must not run");
        }

        [Test]
        public void Test_HalfDistanceToGoalPenalty_CappedAtHalfDistance()
        {
            // Ball at own 4-yard line; 10-yard penalty on offense
            _rulesEngine.LineOfScrimmageYard = 4.0f;
            _rulesEngine.DistanceToGainYards = 10.0f;

            _rulesEngine.EnforcePenalty("Holding", 10.0f, againstOffense: true, autoFirstDown: false);

            Assert.AreEqual(2.0f, _rulesEngine.LineOfScrimmageYard, "Penalty inside 10-yard line must be capped at half the distance to goal (4 -> 2)");
            Assert.AreEqual(12.0f, _rulesEngine.DistanceToGainYards, "Distance to gain must increase by 2 yards");
        }

        [Test]
        public void Test_KansasPlanOvertime_AlternatesPossessionAcrossRounds()
        {
            _rulesEngine.InitializeKansasPlanOvertime();
            Assert.AreEqual(TeamSide.HomeGlenelg, _rulesEngine.PossessionTeam, "Round 1 begins with Glenelg possession");

            // Glenelg finishes drive; Titans get possession
            _rulesEngine.ProcessKansasOvertimeConclusion();
            Assert.AreEqual(TeamSide.AwayTitans, _rulesEngine.PossessionTeam, "Round 1 second turn is Titans");

            // Both tied -> advance to Round 2
            _rulesEngine.ProcessKansasOvertimeConclusion();
            Assert.AreEqual(2, _rulesEngine.OvertimeRound, "Should advance to Round 2");
            Assert.AreEqual(TeamSide.AwayTitans, _rulesEngine.PossessionTeam, "Round 2 alternates to Titans first");

            // Titans finish drive; Glenelg second turn
            _rulesEngine.ProcessKansasOvertimeConclusion();
            Assert.AreEqual(TeamSide.HomeGlenelg, _rulesEngine.PossessionTeam, "Round 2 second turn is Glenelg");

            // Still tied -> advance to Round 3
            _rulesEngine.ProcessKansasOvertimeConclusion();
            Assert.AreEqual(3, _rulesEngine.OvertimeRound, "Should advance to Round 3");
            Assert.AreEqual(TeamSide.HomeGlenelg, _rulesEngine.PossessionTeam, "Round 3 alternates back to Glenelg first");
        }
    }
}
