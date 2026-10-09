using System;
using Unity.Burst;
using Unity.Mathematics;

namespace TitanGlenelg.Core.Physics
{
    /// <summary>
    /// User Passing Trajectory Triad (Lob, Touch, Bullet) solver with left-stick placement leading.
    /// Computes launch velocity vectors, aerodynamic spin, and hang times for prolate spheroid RK4 integration.
    /// </summary>
    [BurstCompile]
    public static class PassingTrajectoryCalculator
    {
        public const float Gravity = 9.80665f;

        /// <summary>
        /// Solves the initial launch velocity vector for a given throw type and left-stick target placement lead.
        /// </summary>
        /// <param name="origin">Release point of the quarterback's hand (typically chest/shoulder elevation ~1.9m)</param>
        /// <param name="targetLandmark">Expected receiver position at the catch point</param>
        /// <param name="leftStickLeadOffset">User analog stick placement offset (leads receiver left, right, high, back-shoulder)</param>
        /// <param name="throwType">Lob, Touch, or Bullet</param>
        /// <param name="qbStaminaMultiplier">Multiplier from Two-Way fatigue (1.0 down to 0.85)</param>
        /// <param name="initialVelocity">Out parameter: launch velocity in m/s</param>
        /// <param name="spinAxis">Out parameter: spin axis unit vector</param>
        /// <param name="spinRateRadS">Out parameter: spin rate in rad/s</param>
        /// <param name="wobbleRad">Out parameter: initial wobble nutation angle in radians</param>
        /// <param name="estimatedFlightTime">Out parameter: estimated flight duration in seconds</param>
        [BurstCompile]
        public static void CalculateThrowTrajectory(
            float3 origin,
            float3 targetLandmark,
            float3 leftStickLeadOffset,
            ThrowType throwType,
            float qbStaminaMultiplier,
            out float3 initialVelocity,
            out float3 spinAxis,
            out float spinRateRadS,
            out float wobbleRad,
            out float estimatedFlightTime)
        {
            // Apply stick placement lead offset (clamped to realistic placement radius: max 2.5m)
            float leadMag = math.length(leftStickLeadOffset);
            if (leadMag > 2.5f)
            {
                leftStickLeadOffset = (leftStickLeadOffset / leadMag) * 2.5f;
            }

            float3 destination = targetLandmark + leftStickLeadOffset;
            float3 displacement = destination - origin;
            float horizontalDistance = math.length(new float2(displacement.x, displacement.z));
            float verticalDistance = displacement.y;

            // Clamping horizontal distance to physical field constraints
            horizontalDistance = math.clamp(horizontalDistance, 2.0f, 65.0f);

            float speed;
            float launchAngleDeg;

            switch (throwType)
            {
                case ThrowType.Lob:
                    // High arc, allows receiver to run under the ball (deep vertical shots)
                    // Launch angle 40 - 48 degrees, lower speed 18 - 24 m/s
                    launchAngleDeg = math.clamp(38.0f + (horizontalDistance * 0.15f), 40.0f, 48.0f);
                    speed = math.clamp(17.0f + (horizontalDistance * 0.28f), 18.0f, 26.0f);
                    spinRateRadS = 45.0f; // ~430 RPM
                    wobbleRad = math.radians(6.0f); // slight wobble on high lobs
                    break;

                case ThrowType.Touch:
                    // Intermediate arc, drops over second-level linebackers into zone voids
                    // Launch angle 28 - 35 degrees, moderate speed 22 - 28 m/s
                    launchAngleDeg = math.clamp(28.0f + (horizontalDistance * 0.12f), 28.0f, 35.0f);
                    speed = math.clamp(21.0f + (horizontalDistance * 0.22f), 22.0f, 29.0f);
                    spinRateRadS = 55.0f; // ~525 RPM
                    wobbleRad = math.radians(3.5f);
                    break;

                case ThrowType.Bullet:
                default:
                    // Fast flat trajectory, tight spiral, minimal flight time (curls, slants, seams)
                    // Launch angle 12 - 20 degrees, high speed 27 - 34 m/s
                    launchAngleDeg = math.clamp(12.0f + (horizontalDistance * 0.10f), 14.0f, 22.0f);
                    speed = math.clamp(26.0f + (horizontalDistance * 0.18f), 27.0f, 34.0f);
                    spinRateRadS = 65.0f; // ~620 RPM (tight spiral)
                    wobbleRad = math.radians(1.5f); // tightest release
                    break;
            }

            // Adjust launch angle for vertical elevation differences between origin and target
            float elevationTiltDeg = math.degrees(math.atan2(verticalDistance, horizontalDistance));
            launchAngleDeg = math.clamp(launchAngleDeg + elevationTiltDeg, 5.0f, 65.0f);

            // Apply quarterback stamina degradation from Two-Way Ironman fatigue
            speed *= math.clamp(qbStaminaMultiplier, 0.85f, 1.0f);

            float theta = math.radians(launchAngleDeg);
            float cosTheta = math.cos(theta);
            float sinTheta = math.sin(theta);

            // Planar horizontal unit direction
            float2 horizontalDir = math.normalize(new float2(displacement.x, displacement.z));

            // Initial velocity components
            float vHoriz = speed * cosTheta;
            float vVert = speed * sinTheta;

            initialVelocity = new float3(horizontalDir.x * vHoriz, vVert, horizontalDir.y * vHoriz);

            // Spin axis aligned with velocity direction (spiral along longitudinal axis)
            spinAxis = math.normalize(initialVelocity);

            // Estimated flight time T based on horizontal speed
            estimatedFlightTime = horizontalDistance / math.max(1.0f, vHoriz);
        }
    }
}
