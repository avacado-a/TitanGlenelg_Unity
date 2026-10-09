# NFHS Officiating & Two-Way Biomechanical Fatigue Guide

## 1. NFHS Officiating Engine (`NFHSRulesAndClockEngine`)

The engine strictly implements the National Federation of State High School Associations (NFHS) football rulebook:

1. **12-Minute Quarters:**
   - 4 quarters of 720 seconds each.
2. **First Down Clock Stoppages:**
   - Whenever the offense earns a 1st down, the game clock halts immediately. It remains stopped while the chain crew advances and restarts only when the referee gives the ready-for-play signal.
3. **15-Yard Pass Interference Cap (Rule 7-5-10):**
   - Unlike NFL spot-foul pass interference, high school NFHS rules strictly cap defensive pass interference at 15 yards from the previous spot (or half the distance if inside the 30).
4. **Play Clock & Delay of Game:**
   - Standard 40-second play clock following downed plays; 25-second administrative clock following timeouts, penalties, and measurements. Expiration results in a 5-yard Delay of Game penalty.
5. **Kansas Plan Untimed Overtime:**
   - When regulation ends in a tie, Kansas Plan rules activate:
     - Each team receives one possession from the opponent's 10-yard line with 4 downs.
     - The game clock is turned off (untimed).
     - Possessions alternate until the tie is broken after an equal number of turns.

---

## 2. Two-Way Ironman Biomechanical Fatigue System (`TwoWayFatigueSystem`)

In high school powerhouse football, star athletes play both sides of the ball (Offense + Defense). The simulation accurately models this physical toll:

1. **Continuous Stamina Depletion:**
   - Sprinting drains stamina at 2.4% per second.
   - Trench collisions and tackle impacts drain 3.8% burst stamina.
   - Two-Way athletes suffer a **1.65x multiplier** on all stamina drains due to doubled snap volume.
2. **Physical Penalties:**
   - **Sprint Speed:** Degrades down to 80% (-20% maximum penalty).
   - **Throw Velocity:** Degrades down to 85% (-15% maximum penalty).
   - **Block Shed Leverage:** Degrades down to 65% (-35% maximum penalty).
3. **Non-Linear Injury Escalation:**
   - When player stamina drops below 40%, injury risk escalates exponentially:
     $$\text{Risk} = 0.05 + 0.45 \times \left(\frac{0.40 - \text{StaminaRatio}}{0.40}\right)^{2.2}$$
   - Can escalate up to 50% injury hazard if an exhausted starter is kept on the field.
4. **Tactical Depth Chart Substitutions:**
   - Between plays in the huddle HUD, coaches can view athlete stamina percentages, stat deltas, and injury alerts, subbing in fresh backups to allow starters to recover on the bench.
