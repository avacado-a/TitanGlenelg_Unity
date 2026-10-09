using System;
using UnityEngine;
using TitanGlenelg.Core.AI;
using TitanGlenelg.Core.Playbook;
using TitanGlenelg.Core.Simulation;

namespace TitanGlenelg.Core.Presentation
{
    /// <summary>
    /// Tactical HUD Controller managing UI display for:
    /// - Scoreboard & NFHS 12-minute quarter clock
    /// - Down & Distance / Line of Scrimmage indicators
    /// - Play Caller HUD (3 Offense / 3 Defense)
    /// - Two-Way Depth Chart & Stamina Bars with injury risk alerts
    /// - User Catch Triad Prompts (RAC, Aggressive, Possession)
    /// </summary>
    public class TacticalHUDController : MonoBehaviour
    {
        [Header("System References")]
        public NFHSRulesAndClockEngine RulesEngine;
        public TwoWayFatigueSystem FatigueSystem;
        public GamePhaseCoordinator Coordinator;

        [Header("HUD Display Settings")]
        public bool ShowDebugOverlay = true;

        private void OnGUI()
        {
            if (!ShowDebugOverlay) return;

            DrawScoreboardHUD();
            DrawDownAndDistanceHUD();

            if (Coordinator != null)
            {
                if (Coordinator.CurrentPhase == PlayCallPhase.PlayCallingHUD)
                {
                    DrawPlayCallerHUD();
                    DrawTwoWayFatigueHUD();
                }
                else if (Coordinator.CurrentPhase == PlayCallPhase.LivePlayActive && Coordinator.BallThrown && !Coordinator.BallCaught)
                {
                    DrawCatchTriadHUD();
                }
            }
        }

        private void DrawScoreboardHUD()
        {
            GUILayout.BeginArea(new Rect(20, 20, 420, 100), GUI.skin.box);
            GUILayout.BeginHorizontal();

            // Glenelg Red & Black
            GUI.color = new Color(0.9f, 0.2f, 0.2f);
            GUILayout.Label($"GLENELG (HOME): {RulesEngine?.GlenelgScore ?? 0}", GUILayout.Width(180));

            // Titans White & Blue
            GUI.color = new Color(0.3f, 0.6f, 1.0f);
            GUILayout.Label($"TITANS (AWAY): {RulesEngine?.TitansScore ?? 0}", GUILayout.Width(180));
            GUI.color = Color.white;

            GUILayout.EndHorizontal();

            // Period & Clock
            if (RulesEngine != null)
            {
                int minutes = Mathf.FloorToInt(RulesEngine.QuarterTimeRemaining / 60.0f);
                int seconds = Mathf.FloorToInt(RulesEngine.QuarterTimeRemaining % 60.0f);
                string periodStr = RulesEngine.CurrentPeriod == GamePeriod.OvertimeKansasPlan
                    ? $"OT (Kansas Plan R{RulesEngine.OvertimeRound})"
                    : $"{RulesEngine.CurrentPeriod}";

                GUILayout.Label($"Period: {periodStr} | Clock: {minutes:D2}:{seconds:D2} | Play Clock: {Mathf.CeilToInt(RulesEngine.PlayClockRemaining)}s");
            }

            GUILayout.EndArea();
        }

        private void DrawDownAndDistanceHUD()
        {
            if (RulesEngine == null) return;

            GUILayout.BeginArea(new Rect(460, 20, 320, 100), GUI.skin.box);
            GUILayout.Label($"DOWN & DISTANCE: {RulesEngine.CurrentDown} & {Mathf.CeilToInt(RulesEngine.DistanceToGainYards)}");
            GUILayout.Label($"Ball on: {RulesEngine.LineOfScrimmageYard:F1} YD | Possession: {RulesEngine.PossessionTeam}");
            if (RulesEngine.ClockStoppedOnFirstDown)
            {
                GUI.color = Color.yellow;
                GUILayout.Label(">>> CLOCK STOPPED ON FIRST DOWN (NFHS) <<<");
                GUI.color = Color.white;
            }
            GUILayout.EndArea();
        }

        private void DrawPlayCallerHUD()
        {
            GUILayout.BeginArea(new Rect(20, 130, 360, 260), "TACTICAL PLAY CALLER", GUI.skin.window);

            GUILayout.Label("<b>OFFENSIVE PLAYS:</b>");
            if (GUILayout.Button("1. Pistol Verticals (Four Verts)"))
            {
                Coordinator?.SelectOffensivePlay(OffensivePlaybook.PistolVerticals);
            }
            if (GUILayout.Button("2. Pistol Gladiator Counter Trey (Pull LG)"))
            {
                Coordinator?.SelectOffensivePlay(OffensivePlaybook.PistolGladiatorCounterTrey);
            }
            if (GUILayout.Button("3. Pistol Quick Slants (3-Step Rhythm)"))
            {
                Coordinator?.SelectOffensivePlay(OffensivePlaybook.PistolQuickSlants);
            }

            GUILayout.Space(10);
            GUILayout.Label("<b>DEFENSIVE SHELLS:</b>");
            if (GUILayout.Button("A. Cover 3 Sky (Single-High 3-Deep)"))
            {
                Coordinator?.SelectDefensivePlay(DefensivePlaybook.Cover3Sky);
            }
            if (GUILayout.Button("B. Cover 2 Man (Two-High Underneath Trail)"))
            {
                Coordinator?.SelectDefensivePlay(DefensivePlaybook.Cover2Man);
            }
            if (GUILayout.Button("C. Cover 0 Blitz (6-Man All-Out Blitz)"))
            {
                Coordinator?.SelectDefensivePlay(DefensivePlaybook.Cover0Blitz);
            }

            GUILayout.Space(10);
            if (GUILayout.Button("BREAK HUDDLE & APPROACH LINE"))
            {
                Coordinator?.SetPhase(PlayCallPhase.HuddleToLine);
            }

            GUILayout.EndArea();
        }

        private void DrawTwoWayFatigueHUD()
        {
            if (FatigueSystem == null) return;

            GUILayout.BeginArea(new Rect(400, 130, 480, 260), "TWO-WAY IRONMAN FATIGUE & SUBS", GUI.skin.window);
            GUILayout.Label("Active Two-Way Starters & Backup Rotations:");

            for (int i = 0; i < FatigueSystem.GlenelgRoster.Count; i++)
            {
                var athlete = FatigueSystem.GlenelgRoster[i];
                GUILayout.BeginHorizontal();

                string role = athlete.PlaysBothWays ? "[2-WAY IRONMAN]" : "[BACKUP]";
                GUILayout.Label($"#{athlete.JerseyNumber} {athlete.FullName} {role}", GUILayout.Width(220));

                // Stamina color gradient
                float ratio = athlete.Stamina / athlete.MaxStamina;
                GUI.color = ratio > 0.65f ? Color.green : (ratio > 0.40f ? Color.yellow : Color.red);
                GUILayout.Label($"Stamina: {athlete.Stamina:F0}%", GUILayout.Width(90));
                GUI.color = Color.white;

                if (athlete.CurrentInjuryRisk > 0.05f)
                {
                    GUI.color = Color.red;
                    GUILayout.Label($"Risk: {(athlete.CurrentInjuryRisk * 100f):F0}%", GUILayout.Width(70));
                    GUI.color = Color.white;
                }
                else
                {
                    GUILayout.Label($"Risk: Low", GUILayout.Width(70));
                }

                if (athlete.PlaysBothWays)
                {
                    if (GUILayout.Button(athlete.IsSubstitutedOut ? "SUB IN" : "REST BENCH"))
                    {
                        athlete.IsSubstitutedOut = !athlete.IsSubstitutedOut;
                    }
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }

        private void DrawCatchTriadHUD()
        {
            GUILayout.BeginArea(new Rect(Screen.width / 2 - 180, Screen.height - 130, 360, 100), "USER CATCH TRIAD", GUI.skin.box);
            GUILayout.Label("Ball in Flight! Choose Catch Technique:");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("[X] RAC Catch\n(Speed / Run)"))
            {
                Coordinator?.ExecuteCatchAttempt(Physics.UserCatchMode.RAC);
            }
            if (GUILayout.Button("[Y] Aggressive\n(High Point)"))
            {
                Coordinator?.ExecuteCatchAttempt(Physics.UserCatchMode.Aggressive);
            }
            if (GUILayout.Button("[A] Possession\n(Secure / Down)"))
            {
                Coordinator?.ExecuteCatchAttempt(Physics.UserCatchMode.Possession);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }
    }
}
