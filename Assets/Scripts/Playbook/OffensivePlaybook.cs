using System;
using System.Collections.Generic;
using UnityEngine;

namespace TitanGlenelg.Core.Playbook
{
    /// <summary>
    /// Focused Offensive Playbook for the Titan-Glenelg Vertical Slice:
    /// 1. Pistol Verticals
    /// 2. Pistol Gladiator Counter Trey (with pulling guard kickout assignment)
    /// 3. Pistol Quick Slants
    /// </summary>
    public static class OffensivePlaybook
    {
        public static readonly PlayDefinition PistolVerticals = BuildPistolVerticals();
        public static readonly PlayDefinition PistolGladiatorCounterTrey = BuildPistolGladiatorCounterTrey();
        public static readonly PlayDefinition PistolQuickSlants = BuildPistolQuickSlants();

        public static List<PlayDefinition> GetAllOffensivePlays()
        {
            return new List<PlayDefinition>
            {
                PistolVerticals,
                PistolGladiatorCounterTrey,
                PistolQuickSlants
            };
        }

        private static PlayDefinition BuildPistolVerticals()
        {
            var play = new PlayDefinition
            {
                PlayId = "OFF_PISTOL_VERTS",
                PlayName = "Pistol Verticals",
                Type = PlayType.Pass,
                Formation = FormationType.PistolTripsRight,
                PlayDescription = "Deep vertical attack challenging deep safeties with 4 vertical stems and an RB flat check-release."
            };

            // WR1 (Outside Left - Boundary Fade)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.WR1,
                PreSnapAlignment = new Vector3(-20f, 0f, 0f),
                Route = new RoutePointDefinition
                {
                    Point0 = new Vector3(-20f, 0f, 0f),
                    Point1 = new Vector3(-20f, 0f, 15f),
                    Point2 = new Vector3(-21f, 0f, 25f),
                    Point3 = new Vector3(-21.5f, 0f, 40f),
                    StemDuration = 2.2f,
                    CutSharpness = 0.2f
                },
                IsPrimaryReceiver = false
            });

            // WR2 (Slot Right - Seam)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.WR2,
                PreSnapAlignment = new Vector3(10f, 0f, 0f),
                Route = new RoutePointDefinition
                {
                    Point0 = new Vector3(10f, 0f, 0f),
                    Point1 = new Vector3(9f, 0f, 12f),
                    Point2 = new Vector3(8f, 0f, 24f),
                    Point3 = new Vector3(8f, 0f, 38f),
                    StemDuration = 1.9f,
                    CutSharpness = 0.3f
                },
                IsPrimaryReceiver = true // Primary seam shot
            });

            // WR3 (Outside Right - Deep Go)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.WR3,
                PreSnapAlignment = new Vector3(22f, 0f, 0f),
                Route = new RoutePointDefinition
                {
                    Point0 = new Vector3(22f, 0f, 0f),
                    Point1 = new Vector3(22f, 0f, 15f),
                    Point2 = new Vector3(22f, 0f, 30f),
                    Point3 = new Vector3(22f, 0f, 42f),
                    StemDuration = 2.4f,
                    CutSharpness = 0.1f
                },
                IsPrimaryReceiver = false
            });

            // TE (In-Line Seam)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.TE,
                PreSnapAlignment = new Vector3(4f, 0f, 0f),
                Route = new RoutePointDefinition
                {
                    Point0 = new Vector3(4f, 0f, 0f),
                    Point1 = new Vector3(3f, 0f, 10f),
                    Point2 = new Vector3(0f, 0f, 22f),
                    Point3 = new Vector3(0f, 0f, 35f),
                    StemDuration = 2.1f,
                    CutSharpness = 0.4f
                },
                IsPrimaryReceiver = false
            });

            // RB (Pistol backfield check-and-release to flat)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.RB,
                PreSnapAlignment = new Vector3(0f, 0f, -4f),
                Route = new RoutePointDefinition
                {
                    Point0 = new Vector3(0f, 0f, -4f),
                    Point1 = new Vector3(-3f, 0f, -1f),
                    Point2 = new Vector3(-8f, 0f, 2f),
                    Point3 = new Vector3(-14f, 0f, 4f),
                    StemDuration = 1.4f,
                    CutSharpness = 0.7f
                },
                IsPrimaryReceiver = false
            });

            return play;
        }

        private static PlayDefinition BuildPistolGladiatorCounterTrey()
        {
            var play = new PlayDefinition
            {
                PlayId = "OFF_PISTOL_COUNTER_TREY",
                PlayName = "Pistol Gladiator Counter Trey",
                Type = PlayType.Run,
                Formation = FormationType.PistolGladiator,
                PlayDescription = "Heavy gap-scheme power run featuring pulling backside Left Guard kickout and Right Tackle seal."
            };

            // Left Guard (Pulls across formation behind center to kick out frontside edge defender)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.LG,
                PreSnapAlignment = new Vector3(-1.2f, 0f, 0f),
                IsPullingLineman = true,
                PullWaypoint = new Vector3(4.5f, 0f, 1.2f) // Kick out target point
            });

            // Right Tackle (Wraps through C-gap to seal second level linebacker)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.RT,
                PreSnapAlignment = new Vector3(2.8f, 0f, 0f),
                IsPullingLineman = true,
                PullWaypoint = new Vector3(3.2f, 0f, 3.5f) // Second-level LB seal point
            });

            // RB (Counter steps left, plants foot, follows pulling LG and RT through right off-tackle gap)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.RB,
                PreSnapAlignment = new Vector3(0f, 0f, -4f),
                Route = new RoutePointDefinition
                {
                    Point0 = new Vector3(0f, 0f, -4f),
                    Point1 = new Vector3(-1.5f, 0f, -3.5f), // Counter step
                    Point2 = new Vector3(1.0f, 0f, -1.0f),  // Mesh through backfield
                    Point3 = new Vector3(3.8f, 0f, 6.0f),   // Cut through C-gap lane
                    StemDuration = 1.6f,
                    CutSharpness = 0.9f
                },
                IsPrimaryReceiver = true // Primary ball carrier
            });

            return play;
        }

        private static PlayDefinition BuildPistolQuickSlants()
        {
            var play = new PlayDefinition
            {
                PlayId = "OFF_PISTOL_QUICK_SLANTS",
                PlayName = "Pistol Quick Slants",
                Type = PlayType.Pass,
                Formation = FormationType.PistolSpread2x2,
                PlayDescription = "3-step rhythm pass: outside receivers drive 3 steps upfield and cut sharp 45 degrees inside."
            };

            // WR1 (Left Slant - 3 steps and 45 degree plant)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.WR1,
                PreSnapAlignment = new Vector3(-18f, 0f, 0f),
                Route = new RoutePointDefinition
                {
                    Point0 = new Vector3(-18f, 0f, 0f),
                    Point1 = new Vector3(-18f, 0f, 5.5f), // 3-step vertical stem (~6 yards)
                    Point2 = new Vector3(-14f, 0f, 8.5f), // Sharp 45 degree plant inside
                    Point3 = new Vector3(-5f, 0f, 13f),   // Drive across the face of the safety
                    StemDuration = 0.95f,
                    CutSharpness = 0.85f
                },
                IsPrimaryReceiver = true
            });

            // WR2 (Slot Left - Flat/Arrow to clear curl window)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.WR2,
                PreSnapAlignment = new Vector3(-9f, 0f, 0f),
                Route = new RoutePointDefinition
                {
                    Point0 = new Vector3(-9f, 0f, 0f),
                    Point1 = new Vector3(-9f, 0f, 2.5f),
                    Point2 = new Vector3(-13f, 0f, 3.5f),
                    Point3 = new Vector3(-17f, 0f, 4.0f),
                    StemDuration = 0.8f,
                    CutSharpness = 0.8f
                },
                IsPrimaryReceiver = false
            });

            // WR3 (Right Slant)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.WR3,
                PreSnapAlignment = new Vector3(18f, 0f, 0f),
                Route = new RoutePointDefinition
                {
                    Point0 = new Vector3(18f, 0f, 0f),
                    Point1 = new Vector3(18f, 0f, 5.5f),
                    Point2 = new Vector3(14f, 0f, 8.5f),
                    Point3 = new Vector3(5f, 0f, 13f),
                    StemDuration = 0.95f,
                    CutSharpness = 0.85f
                },
                IsPrimaryReceiver = false
            });

            return play;
        }
    }
}
