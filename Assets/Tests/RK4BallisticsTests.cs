using NUnit.Framework;
using Unity.Mathematics;
using TitanGlenelg.Core.Physics;

namespace TitanGlenelg.Tests
{
    [TestFixture]
    public class RK4BallisticsTests
    {
        [Test]
        public void Test_PassingTrajectoryCalculator_LobTouchBullet_VelocityAndAngleOrdering()
        {
            float3 origin = new float3(0f, 2f, 0f);
            float3 target = new float3(0f, 1.8f, 25f);
            float3 zeroLead = float3.zero;

            // Lob
            PassingTrajectoryCalculator.CalculateThrowTrajectory(
                origin, target, zeroLead, ThrowType.Lob, 1.0f,
                out float3 vLob, out float3 spinLob, out float rateLob, out float wobbleLob, out float timeLob);

            // Touch
            PassingTrajectoryCalculator.CalculateThrowTrajectory(
                origin, target, zeroLead, ThrowType.Touch, 1.0f,
                out float3 vTouch, out float3 spinTouch, out float rateTouch, out float wobbleTouch, out float timeTouch);

            // Bullet
            PassingTrajectoryCalculator.CalculateThrowTrajectory(
                origin, target, zeroLead, ThrowType.Bullet, 1.0f,
                out float3 vBullet, out float3 spinBullet, out float rateBullet, out float wobbleBullet, out float timeBullet);

            // Assert: Bullet velocity should be higher than Touch, and Touch higher than Lob
            float speedLob = math.length(vLob);
            float speedTouch = math.length(vTouch);
            float speedBullet = math.length(vBullet);

            Assert.Greater(speedBullet, speedTouch, "Bullet speed must be strictly greater than Touch speed");
            Assert.Greater(speedTouch, speedLob, "Touch speed must be strictly greater than Lob speed");

            // Assert: Lob has highest vertical launch velocity
            Assert.Greater(vLob.y, vTouch.y, "Lob must have higher vertical arc than Touch");
            Assert.Greater(vTouch.y, vBullet.y, "Touch must have higher vertical arc than Bullet");

            // Assert: Bullet has shortest flight time
            Assert.Less(timeBullet, timeTouch, "Bullet flight time must be less than Touch");
            Assert.Less(timeTouch, timeLob, "Touch flight time must be less than Lob");
        }

        [Test]
        public void Test_LeftStickPlacementLead_ShiftsTargetDisplacement()
        {
            float3 origin = new float3(0f, 2f, 0f);
            float3 target = new float3(0f, 1.8f, 20f);
            float3 rightLead = new float3(2.0f, 0f, 0f);

            PassingTrajectoryCalculator.CalculateThrowTrajectory(
                origin, target, rightLead, ThrowType.Bullet, 1.0f,
                out float3 vLead, out _, out _, out _, out _);

            Assert.Greater(vLead.x, 0f, "Right-stick placement lead must impart positive horizontal X velocity");
        }

        [Test]
        public void Test_RK4AerodynamicAcceleration_ProlateSpheroidDragAndMagnus()
        {
            var aero = new ProlateSpheroidAerodynamics
            {
                MassKg = 0.425f,
                MinAreaM2 = 0.019f,
                MaxAreaM2 = 0.045f,
                MinDragCoeff = 0.08f,
                MaxDragCoeff = 0.70f,
                LiftCoeffMagnus = 0.15f,
                AirDensityKgM3 = 1.225f
            };

            var job = new RK4BallisticsJob
            {
                DeltaTime = 1.0f / 120.0f,
                GravityAcc = new float3(0f, -9.80665f, 0f)
            };

            float3 velocity = new float3(0f, 5f, 25f);
            float3 spinAxis = new float3(0f, 0f, 1f);
            float spinRate = 50.0f; // rad/s
            float wobbleZero = 0.0f; // Perfect spiral nose-on
            float wobbleTumble = math.radians(45.0f); // Wobbling spiral

            float3 accNoseOn = job.ComputeAcceleration(velocity, spinAxis, spinRate, wobbleZero, in aero);
            float3 accWobble = job.ComputeAcceleration(velocity, spinAxis, spinRate, wobbleTumble, in aero);

            // Drag decelerates forward Z velocity. Wobbling ball has greater cross-section, thus greater drag deceleration.
            Assert.Less(accWobble.z, accNoseOn.z, "Wobbling ball must experience greater drag deceleration than clean spiral");
        }
    }
}
