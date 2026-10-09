using NUnit.Framework;
using Unity.Mathematics;
using TitanGlenelg.Core.AI;
using TitanGlenelg.Core.Physics;

namespace TitanGlenelg.Tests
{
    [TestFixture]
    public class CatchResolutionTests
    {
        [Test]
        public void Test_CatchModeKnockoutProtection_PossessionCatchSuppressesRisk()
        {
            float catchRating = 85.0f;
            float heavyHitForce = 1000.0f; // High-impact hit
            float randomRoll = 0.30f;      // 30% dice roll

            // Evaluate knockout for RAC catch
            bool knockoutRAC = CatchResolutionSystem.EvaluateContestedKnockout(
                UserCatchMode.RAC, catchRating, heavyHitForce, randomRoll);

            // Evaluate knockout for Possession catch
            bool knockoutPossession = CatchResolutionSystem.EvaluateContestedKnockout(
                UserCatchMode.Possession, catchRating, heavyHitForce, randomRoll);

            Assert.IsTrue(knockoutRAC, "RAC catch must be susceptible to knockout on heavy contact");
            Assert.IsFalse(knockoutPossession, "Possession catch must protect the ball and prevent knockout on the same contact");
        }

        [Test]
        public void Test_CatchTriadOutcomes_RACvsPossession()
        {
            var catchTargetRAC = new CatchTargetData
            {
                CatchMode = UserCatchMode.RAC,
                CatchAttempted = true
            };
            var fsmRAC = new PlayerFSMState { OffensePhase = OffensiveFSMPhase.CatchWindow };
            var locoRAC = new KinematicLocomotionData { CurrentVelocity = new float3(0f, 0f, 6.0f) };

            CatchResolutionSystem.ApplyCatchOutcome(ref catchTargetRAC, ref fsmRAC, ref locoRAC);

            Assert.IsTrue(catchTargetRAC.CatchSecured, "Catch should be secured");
            Assert.AreEqual(OffensiveFSMPhase.RunAfterCatch, fsmRAC.OffensePhase, "RAC should transition to RunAfterCatch phase");
            Assert.Greater(locoRAC.CurrentVelocity.z, 6.0f, "RAC catch should provide forward momentum boost");

            var catchTargetPossession = new CatchTargetData
            {
                CatchMode = UserCatchMode.Possession,
                CatchAttempted = true
            };
            var fsmPossession = new PlayerFSMState { OffensePhase = OffensiveFSMPhase.CatchWindow };
            var locoPossession = new KinematicLocomotionData { CurrentVelocity = new float3(0f, 0f, 6.0f) };

            CatchResolutionSystem.ApplyCatchOutcome(ref catchTargetPossession, ref fsmPossession, ref locoPossession);

            Assert.IsTrue(catchTargetPossession.CatchSecured, "Possession catch should be secured");
            Assert.AreEqual(OffensiveFSMPhase.Down, fsmPossession.OffensePhase, "Possession catch transitions to Down (down by contact/toe tap)");
            Assert.AreEqual(0f, locoPossession.CurrentVelocity.z, "Possession catch should arrest forward momentum (zero YAC)");
        }

        [Test]
        public void Test_AggressiveCatch_OutcomesAndHighPointSecurity()
        {
            var catchTargetAgg = new CatchTargetData
            {
                CatchMode = UserCatchMode.Aggressive,
                CatchAttempted = true
            };
            var fsmAgg = new PlayerFSMState { OffensePhase = OffensiveFSMPhase.CatchWindow };
            var locoAgg = new KinematicLocomotionData { CurrentVelocity = new float3(0f, 0f, 6.0f) };

            CatchResolutionSystem.ApplyCatchOutcome(ref catchTargetAgg, ref fsmAgg, ref locoAgg);

            Assert.IsTrue(catchTargetAgg.CatchSecured, "Aggressive catch must be secured");
            Assert.AreEqual(OffensiveFSMPhase.RunAfterCatch, fsmAgg.OffensePhase, "Aggressive catch transitions to RunAfterCatch");
            Assert.Less(locoAgg.CurrentVelocity.z, 6.0f, "Aggressive catch landing decelerates momentum");

            // Evaluate contested knockout with medium hit (25% chance; 35% roll survives)
            bool knockout = CatchResolutionSystem.EvaluateContestedKnockout(
                UserCatchMode.Aggressive, receiverCatchRating: 90f, defenderImpactForce: 600f, randomSeed: 0.35f);
            Assert.IsFalse(knockout, "High-rated receiver on aggressive catch survives moderate contest");
        }
    }
}
