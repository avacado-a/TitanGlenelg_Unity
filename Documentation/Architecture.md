# Titan-Glenelg Native AAA Football Simulation: Architecture Manual

## 1. Engine & Runtime Stack
- **Engine Version:** Unity 6 (6000.0.32f1 LTS)
- **Render Pipeline:** High Definition Render Pipeline (HDRP 17.0.3)
- **Physics Engine:** Havok Physics for DOTS / Unity Physics (1.2.4)
- **Target Language:** C# 12 / .NET 8
- **Compiler:** Unity Burst Compiler (1.8.18)

---

## 2. Hybrid DOTS ECS / Managed Architecture
The architecture cleanly separates low-latency deterministic spatial simulation from high-level officiating, UI, and tactical presentation.

```
+---------------------------------------------------------------------------------------------------+
|                                TITAN-GLENELG HYBRID ARCHITECTURE                                  |
+---------------------------------------------------------------------------------------------------+
|  [ MANAGED LAYER: MonoBehaviour ]      |  [ UNMANAGED LAYER: DOTS ECS / Burst ]                   |
|  * NFHSRulesAndClockEngine             |  * RK4BallisticsSystem (120 Hz numerical integrator)     |
|  * TwoWayFatigueSystem                 |  * OffensiveRouteSystem (22-man FSM Bezier evaluator)   |
|  * GamePhaseCoordinator                |  * DefensiveCoverageSystem (Zone landmarks & shadows)    |
|  * TacticalHUDController               |  * TrenchWarfareSystem (Virtual lockup & 0.4s Delta M)   |
|  * FieldDecalSystem                    |  * ActiveRagdollTackleSystem (Momentum transfer & IK)    |
+---------------------------------------------------------------------------------------------------+
```

### 2.1 120 Hz 4th-Order Runge-Kutta (RK4) Ballistics System
Governs aerodynamic flight of the prolate spheroid football with physical parameters:
- **Mass:** 0.425 kg (NFHS spec: 14–15 oz)
- **Air Density:** $\rho = 1.225\text{ kg/m}^3$
- **Cross-Sectional Area:** Elliptical variation between nose-on ($0.019\text{ m}^2$) and broadside ($0.045\text{ m}^2$) based on angle of attack $\alpha$:
  $$A(\alpha) = A_{\min} \cos(\alpha) + A_{\max} \sin(\alpha)$$
- **Drag Coefficient:** $C_d(\alpha) = C_{d,\min} + (C_{d,\max} - C_{d,\min}) \sin^2(\alpha)$
- **Magnus Effect:** Lift vector $\vec{F}_M = \frac{1}{2} \rho C_L A (\vec{\omega} \times \vec{v})$
- **Wobble Decay:** Gyroscopic precession nutation decays exponentially: $\theta(t) = \theta_0 e^{-\lambda t}$
- **Havok Hand-off:** Writes directly to `PhysicsVelocity.Linear` and `PhysicsVelocity.Angular`. When contact with a player or turf is detected, switches state from `InFlightAerodynamic` to `HavokRigidBodyContact`, letting Havok physics handle collisions.

### 2.2 User Passing Trajectory Triad
- **Lob:** Launch angle 40°–48°, speed 18–26 m/s, high vertical apex (allows receiver to run under the ball).
- **Touch:** Launch angle 28°–35°, speed 22–29 m/s, drops over second-level linebackers into zone pockets.
- **Bullet:** Launch angle 14°–22°, speed 27–34 m/s, high velocity, tight spiral, shortest air travel time.
- **Left-Stick Placement:** Imparts directional lead offset up to 2.5 meters.

### 2.3 User Catch Triad & Knockout Physics
- **RAC (Run After Catch):** Preserves forward running momentum (+10% burst exit speed), high knockout probability under heavy contact (+40% vulnerability).
- **Aggressive (High-Point / Jump):** +0.75m vertical catch radius bonus, neutral knockout resistance.
- **Possession (Secure / Fall to Turf):** Two-handed tuck against chest, suppresses knockout probability by 75%, halts forward momentum (zero YAC, guarantees secure catch at boundary).

### 2.4 Trench Warfare Kinematic System
- **Pocket Integrity Drift:** Offensive linemen slowly yield depth backwards along the pocket normal at 0.3–0.8 m/s, forming a dynamic semi-elliptical pocket.
- **0.4s Leverage Checks:**
  $$\Delta M = \text{Score}_{DL} - \text{Score}_{OL} + \text{Fuzz}$$
  - $\Delta M > +25$: DL sheds block (swim/bull-rush).
  - $\Delta M > +45$: Lineman pancaked to turf.
  - $-\Delta M > +25$: OL anchors firmly, halting drift.
