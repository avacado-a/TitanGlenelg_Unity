using System;
using System.Collections.Generic;
using UnityEngine;

namespace TitanGlenelg.Core.Playbook
{
    /// <summary>
    /// Focused Defensive Playbook for the Titan-Glenelg Vertical Slice:
    /// 1. Cover 3 Sky (Single-high, deep 3-way zone, SS flat roll)
    /// 2. Cover 2 Man (Two-high deep halves, 5 underneath man-to-man shadow)
    /// 3. Cover 0 Blitz (Zero deep safeties, 6-man pass rush, aggressive man-to-man)
    /// </summary>
    public static class DefensivePlaybook
    {
        public static readonly PlayDefinition Cover3Sky = BuildCover3Sky();
        public static readonly PlayDefinition Cover2Man = BuildCover2Man();
        public static readonly PlayDefinition Cover0Blitz = BuildCover0Blitz();

        public static List<PlayDefinition> GetAllDefensivePlays()
        {
            return new List<PlayDefinition>
            {
                Cover3Sky,
                Cover2Man,
                Cover0Blitz
            };
        }

        private static PlayDefinition BuildCover3Sky()
        {
            var play = new PlayDefinition
            {
                PlayId = "DEF_COVER_3_SKY",
                PlayName = "Cover 3 Sky",
                Type = PlayType.Pass,
                Formation = FormationType.DefenseBase43,
                PlayDescription = "Classic single-high 3-deep zone: FS patrols deep middle 1/3, CBs take deep outside 1/3s, SS rolls to strong flat."
            };

            // Free Safety (Deep Middle 1/3)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.FS,
                PreSnapAlignment = new Vector3(0f, 0f, 14f),
                ZoneLandmark = new Vector3(0f, 0f, 22f), // Deep middle 1/3
                CushionDistance = 10f
            });

            // Left Cornerback (Deep Left Outside 1/3)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.CB_L,
                PreSnapAlignment = new Vector3(-19f, 0f, 7f),
                ZoneLandmark = new Vector3(-18f, 0f, 20f), // Deep outside left 1/3
                CushionDistance = 7f,
                LeverageAngle = -15f // Outside leverage protecting boundary
            });

            // Right Cornerback (Deep Right Outside 1/3)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.CB_R,
                PreSnapAlignment = new Vector3(19f, 0f, 7f),
                ZoneLandmark = new Vector3(18f, 0f, 20f), // Deep outside right 1/3
                CushionDistance = 7f,
                LeverageAngle = -15f
            });

            // Strong Safety (Sky call: rolls down to strong flat / curl)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.SS,
                PreSnapAlignment = new Vector3(8f, 0f, 8f),
                ZoneLandmark = new Vector3(12f, 0f, 6f), // Strong flat/curl
                CushionDistance = 5f
            });

            // Middle Linebacker (Middle Hook Zone)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.MLB,
                PreSnapAlignment = new Vector3(0f, 0f, 4.5f),
                ZoneLandmark = new Vector3(0f, 0f, 9f), // Hook-to-curl zone
                CushionDistance = 4f
            });

            return play;
        }

        private static PlayDefinition BuildCover2Man()
        {
            var play = new PlayDefinition
            {
                PlayId = "DEF_COVER_2_MAN",
                PlayName = "Cover 2 Man",
                Type = PlayType.Pass,
                Formation = FormationType.DefenseNickel335,
                PlayDescription = "2-high split safety zone over top, with 5 underneath defenders in tight trail-technique man coverage."
            };

            // Free Safety (Deep Left Half)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.FS,
                PreSnapAlignment = new Vector3(-10f, 0f, 15f),
                ZoneLandmark = new Vector3(-12f, 0f, 22f), // Deep left half
                CushionDistance = 12f
            });

            // Strong Safety (Deep Right Half)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.SS,
                PreSnapAlignment = new Vector3(10f, 0f, 15f),
                ZoneLandmark = new Vector3(12f, 0f, 22f), // Deep right half
                CushionDistance = 12f
            });

            // Left Cornerback (Man vs WR1 - Underneath Inside Trail)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.CB_L,
                PreSnapAlignment = new Vector3(-19f, 0f, 1.5f), // Press alignment
                ManAssignment = AI.PlayerPosition.WR1,
                CushionDistance = 1.2f,
                LeverageAngle = 20f // Inside leverage funneling receiver to deep safety
            });

            // Right Cornerback (Man vs WR3 - Underneath Inside Trail)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.CB_R,
                PreSnapAlignment = new Vector3(19f, 0f, 1.5f),
                ManAssignment = AI.PlayerPosition.WR3,
                CushionDistance = 1.2f,
                LeverageAngle = 20f
            });

            return play;
        }

        private static PlayDefinition BuildCover0Blitz()
        {
            var play = new PlayDefinition
            {
                PlayId = "DEF_COVER_0_BLITZ",
                PlayName = "Cover 0 Blitz",
                Type = PlayType.Pass,
                Formation = FormationType.DefenseBase43,
                PlayDescription = "Aggressive 6-man all-out pass rush with zero deep safety help. Tight press-man on all receivers."
            };

            // Free Safety (Press Man on TE / Slot)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.FS,
                PreSnapAlignment = new Vector3(3f, 0f, 3f),
                ManAssignment = AI.PlayerPosition.TE,
                CushionDistance = 1.0f,
                LeverageAngle = 0f
            });

            // Strong Safety (A-Gap Blitz)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.SS,
                PreSnapAlignment = new Vector3(1.5f, 0f, 3.5f),
                ZoneLandmark = new Vector3(0f, 0f, -4f), // Aggressive QB rush vector
                CushionDistance = 0f
            });

            // Middle Linebacker (B-Gap Blitz)
            play.Assignments.Add(new PlayerAssignment
            {
                Position = AI.PlayerPosition.MLB,
                PreSnapAlignment = new Vector3(-1.5f, 0f, 3.5f),
                ZoneLandmark = new Vector3(0f, 0f, -4f), // Aggressive QB rush vector
                CushionDistance = 0f
            });

            return play;
        }
    }
}
