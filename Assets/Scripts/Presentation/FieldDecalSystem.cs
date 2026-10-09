using System;
using UnityEngine;
using TitanGlenelg.Core.Simulation;

namespace TitanGlenelg.Core.Presentation
{
    /// <summary>
    /// Presentation system managing dynamic field projection markers:
    /// - Blue Line of Scrimmage (LOS)
    /// - Yellow Line to Gain (1st Down Marker)
    /// - Red Red-Zone boundary marker
    /// Converts NFHS yard lines (0 to 100) into 3D world space coordinates.
    /// </summary>
    public class FieldDecalSystem : MonoBehaviour
    {
        [Header("World Space Field Dimensions")]
        public float FieldLengthMeters = 91.44f; // 100 yards = 91.44m
        public float FieldWidthMeters = 48.8f;   // 53.33 yards = ~48.8m
        public float GoalLineOffsetMeters = 45.72f;

        [Header("Decal Transforms / Projectors")]
        public Transform BlueLineOfScrimmage;
        public Transform YellowLineToGain;
        public Transform RedZoneLine;

        [Header("Engine Reference")]
        public NFHSRulesAndClockEngine RulesEngine;

        private void Update()
        {
            if (RulesEngine == null) return;

            UpdateDecalPositions(RulesEngine.LineOfScrimmageYard, RulesEngine.LineToGainYard);
        }

        public void UpdateDecalPositions(float losYard, float lineToGainYard)
        {
            float losWorldZ = YardToWorldZ(losYard);
            float gainWorldZ = YardToWorldZ(lineToGainYard);

            if (BlueLineOfScrimmage != null)
            {
                BlueLineOfScrimmage.position = new Vector3(0f, 0.05f, losWorldZ);
            }

            if (YellowLineToGain != null)
            {
                YellowLineToGain.position = new Vector3(0f, 0.05f, gainWorldZ);
            }

            if (RedZoneLine != null)
            {
                // Red zone boundary at 20-yard line (yard line 80 toward opponent end zone)
                RedZoneLine.position = new Vector3(0f, 0.05f, YardToWorldZ(80.0f));
            }
        }

        public float YardToWorldZ(float yardLine)
        {
            // Maps 0-100 yard line to world coordinates centered at 50-yard line (z = 0)
            float yardsFrom50 = yardLine - 50.0f;
            float metersPerYard = 0.9144f;
            return yardsFrom50 * metersPerYard;
        }

        public float WorldZToYard(float worldZ)
        {
            float metersPerYard = 0.9144f;
            return 50.0f + (worldZ / metersPerYard);
        }
    }
}
