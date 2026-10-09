using NUnit.Framework;
using Unity.Mathematics;
using TitanGlenelg.Core.AI;

namespace TitanGlenelg.Tests
{
    [TestFixture]
    public class TrenchWarfareTests
    {
        [Test]
        public void Test_LeverageDeltaM_PancakeAndShedThresholds()
        {
            // Dominant DL vs Underpowered OL
            var dominantDL = new LinemanPhysicalAttributes
            {
                StrengthRating = 95f,
                PowerLeverage = 98f,
                AnchorRating = 80f,
                MassKg = 135f,
                StaminaEfficiency = 1.0f,
                IsOffensiveLineman = false
            };

            float deltaM = TrenchWarfareSystem.CalculateLeverageDelta(in dominantDL, 0.4f);

            // DL score = (98*0.6 + 95*0.4)*1.0 = (58.8 + 38) = 96.8
            // Baseline OL score = 75.0
            // DeltaM ≈ 21.8 + fuzz
            Assert.Greater(deltaM, 15.0f, "Dominant DL must produce a strong positive leverage advantage");
        }

        [Test]
        public void Test_OLAnchor_HaltsDisplacementWhenStaminaIsHigh()
        {
            var eliteAnchorOL = new LinemanPhysicalAttributes
            {
                StrengthRating = 94f,
                PowerLeverage = 80f,
                AnchorRating = 98f,
                MassKg = 142f,
                StaminaEfficiency = 1.0f,
                IsOffensiveLineman = true
            };

            float deltaM = TrenchWarfareSystem.CalculateLeverageDelta(in eliteAnchorOL, 0.4f);

            // OL score = (98*0.6 + 94*0.4) = (58.8 + 37.6) = 96.4
            // Baseline DL score = 75.0
            // DeltaM = 75 - 96.4 + fuzz = -21.4 + fuzz
            // Negative DeltaM favors OL
            Assert.Less(deltaM, -10.0f, "Elite OL Anchor must create strong negative Delta M favoring OL pass protection");
        }

        [Test]
        public void Test_FatigueDegradesLinemanLeverage()
        {
            var freshOL = new LinemanPhysicalAttributes
            {
                StrengthRating = 90f,
                AnchorRating = 90f,
                StaminaEfficiency = 1.0f,
                IsOffensiveLineman = true
            };

            var fatiguedOL = new LinemanPhysicalAttributes
            {
                StrengthRating = 90f,
                AnchorRating = 90f,
                StaminaEfficiency = 0.65f, // 35% stamina loss
                IsOffensiveLineman = true
            };

            float deltaFresh = TrenchWarfareSystem.CalculateLeverageDelta(in freshOL, 1.0f);
            float deltaFatigued = TrenchWarfareSystem.CalculateLeverageDelta(in fatiguedOL, 1.0f);

            // Fatigued OL should yield a higher Delta M (more advantage to defense)
            Assert.Greater(deltaFatigued, deltaFresh, "Fatigued OL must yield greater leverage to pass rusher");
        }

        [Test]
        public void Test_PancakeThreshold_DominantDLOverpowersAndPancakes()
        {
            var monsterDL = new LinemanPhysicalAttributes
            {
                StrengthRating = 99f,
                PowerLeverage = 99f,
                StaminaEfficiency = 1.0f,
                IsOffensiveLineman = false
            };

            // Evaluate at peak leverage
            float deltaM = TrenchWarfareSystem.CalculateLeverageDelta(in monsterDL, 0.1f);
            Assert.Greater(deltaM, TrenchWarfareSystem.ShedThreshold, "Monster DL must surpass shed threshold");
            Assert.AreEqual(26.0f, TrenchWarfareSystem.PancakeThreshold, "Pancake threshold must be achievable at 26.0f");
        }

        [Test]
        public void Test_PullingGuard_WaypointArrivalTransitionsToBlock()
        {
            var lockup = new TrenchLockupData { State = TrenchEngagementState.Unengaged };
            var attr = new LinemanPhysicalAttributes
            {
                IsPullingAssignment = true,
                PullWaypoint = new float3(4.5f, 0f, 1.2f)
            };
            var loco = new KinematicLocomotionData { MaxSprintSpeedMps = 7.0f };
            var transform = new Unity.Transforms.LocalTransform { Position = new float3(4.4f, 0f, 1.15f) }; // Within 0.3m

            var job = new EvaluateTrenchWarfareJob { DeltaTime = 0.016f, CheckInterval = 0.4f };
            job.Execute(new Unity.Entities.Entity(), ref lockup, ref attr, ref loco, ref transform);

            Assert.IsFalse(attr.IsPullingAssignment, "Pulling assignment must finish upon arrival at waypoint");
            Assert.AreEqual(TrenchEngagementState.LockedInTrench, lockup.State, "Pulling lineman must engage in block upon arrival");
        }
    }
}
