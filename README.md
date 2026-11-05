# StrikePoint: Tactical Time-Attack FPS & Target Course

**StrikePoint** is a fast-paced 3D first-person tactical target shooting and parkour obstacle course built in **Unity 6 (`6000.6.3f1`)**. Players navigate modular industrial sci-fi facilities using precision movement, momentum-based grappling, and multi-mode firearm systems to neutralize static bullseyes, moving drone targets, and explosive barrels while competing for high-score rankings on a persistent local JSON leaderboard.

---

## 👥 Student Engineering Team & Contributions (4-Student Project)

| Member | Primary Role | Core Engineering Contributions |
| :--- | :--- | :--- |
| **Team Member 1** | **Gameplay Physics & Movement** | First-person character motor (`PlayerMovement.cs`), momentum air control, grappling hook web shooter (`GrapplingGun.cs`), and dynamic surface footsteps (`PlayerFootsteps.cs`). |
| **Team Member 2** | **Weapon & Combat Systems** | Multi-mode firearm framework (`GunSystem.cs`) with 3 selective fire modes (Automatic, 3-Round Burst, Semi-Auto toggled via `B`), raycast bullet ballistics, procedural crosshair hitmarkers, and accuracy telemetry. |
| **Team Member 3** | **Level Design & Procedural Spawners** | Half-length course re-engineering, procedural spawner engine (`AutoLevelPopulator.cs`), dynamic hovering target drones, reactive bullseyes (`Target.cs`), AOE explosive barrels (`ExplosiveProp.cs`), and glowing extraction finish gates (`CourseFinishTrigger.cs`). |
| **Team Member 4** | **Backend & Data Persistence** | Scoring engine formula, real-time live score HUD (`GameManager.cs`), end-of-stage evaluation, and persistent file I/O local high-score leaderboard (`LeaderboardManager.cs` saving to `leaderboard.json`). |

---

## 🎯 Gameplay Features & Mechanics

1. **Selective Fire Modes (`GunSystem.cs`)**:
   - **[AUTO]**: High fire-rate fully automatic fire (rapid clearance).
   - **[BURST]**: 3-round burst precision mode with reduced recoil.
   - **[SEMI]**: Single-shot marksman mode for long-distance bullseye targets.
   - Cycle modes at any time during gameplay by pressing **`B`**.

2. **Dynamic Target Shooting & Interactive Props**:
   - **Static Bullseye Targets**: Red/white tiered targets that flash red on bullet impact and award 200 score points.
   - **Moving Target Drones**: Oscillating targets on horizontal and vertical patrol axes testing tracking and timing.
   - **Explosive Hazard Barrels**: Reactive physics barrels that detonate on impact, launching physical debris and triggering AOE chain reactions to nearby targets.
   - **Dynamic Hitmarkers**: Crosshair hit feedback confirming bullet impacts in real-time.

3. **Map Length Optimization (Halved Course Sizes)**:
   - **Medium Level (`MidLevelScene`)**: Reduced from over 650m to **~220m** (ends at Room 2 platform with 15 targets/props). Eliminates 300+ meters of tedious lateral bridges so runs finish cleanly in 60–90 seconds.
   - **Hard Level (`HardLevelScene`)**: Reduced from over 700m to **~250m** (ends at Section 2 Landing Arena with 15 targets/props). Eliminates the grueling 60-meter vertical tower climb and endless fortress roofs for an intense 2-minute run.
   - **Easy Level (`EasyLevelScene`)**: Streamlined ~380m tutorial run with 14 targets/props and visual Extraction Finish Gate.

4. **Persistent Local JSON High-Score Leaderboard (`leaderboard.json`)**:
   - Live top-right HUD tracking current Score, Targets Hit, and Accuracy %.
   - Weighted score formula:
     $$\text{Total Score} = (\text{Targets Hit} \times 200) + (\text{Accuracy } \% \times 15) - (\text{Time in Seconds} \times 3)$$
   - Press **`TAB`** at any point during gameplay to toggle the Leaderboard overlay.
   - Instant score submission on course completion with rank tracking saved persistently to `leaderboard.json`.

---

## 🎮 Controls

| Action | Key / Input |
| :--- | :--- |
| **Move** | `W`, `A`, `S`, `D` |
| **Look Around** | `Mouse` |
| **Jump** | `Space` |
| **Crouch / Slide** | `Left Ctrl` |
| **Grappling Hook / Web Shooter** | `Right Mouse Button (RMB)` |
| **Fire Weapon** | `Left Mouse Button (LMB)` |
| **Switch Fire Mode (Auto / Burst / Semi)** | `B` |
| **Reload** | `R` |
| **Toggle Leaderboard HUD** | `TAB` |
| **Pause / Resume** | `Escape` |

---

## 📦 Third-Party Asset Attribution & Source Links

All third-party assets utilized in this project are free, open-source, or educational prototyping packages attributed below:

| Asset Name | Creator / Publisher | Purpose in Project | Official Store / Source Link |
| :--- | :--- | :--- | :--- |
| **Snaps Prototype \| Sci-Fi / Industrial** | Unity Technologies | Modular sci-fi environment tiles, walls, catwalks, stairs, and industrial structures. | [Unity Asset Store](https://assetstore.unity.com/packages/3d/environments/sci-fi/snaps-prototype-sci-fi-industrial-138278) |
| **Sci-Fi Gun Light** | HYPERION Studio | 3D sci-fi firearm model and textures for player weapon mechanics. | [Unity Asset Store](https://assetstore.unity.com/packages/3d/props/guns/sci-fi-gun-light-87916) |
| **Tech Menu - Modern UI Pack** | SlimUI Solutions | Futuristic canvas UI layout, main menus, pause menus, buttons, and settings panels. | [Unity Asset Store](https://assetstore.unity.com/packages/2d/gui/tech-menu-150247) / [SlimUI Portal](https://slimui.com) |
| **Particle Pack (Starter Assets)** | Unity Technologies | VFX particles for bullet impacts, muzzle flashes, sparks, explosion bursts, and smoke. | [Unity Asset Store](https://assetstore.unity.com/packages/vfx/particles/particle-pack-127320) |
| **ColorSkies** | Pulsar Bytes | Stylized gradient skyboxes and ambient environment cubemaps. | [Unity Asset Store](https://assetstore.unity.com/packages/2d/textures-materials/sky/colorskies-154942) |
| **FREE Footsteps System** | Olivier Girardot / Community | Dynamic surface audio footsteps library and foley sound effects. | [Unity Asset Store](https://assetstore.unity.com/packages/audio/sound-fx/foley/footsteps-essentials-114421) |
| **ProBuilder & Polybrush** | Unity Technologies | In-editor mesh modification and geometric prototyping tools. | [Unity Documentation](https://unity.com/features/probuilder) |
| **TextMesh Pro** | Unity Technologies | Advanced SDF text rendering for high-resolution in-game UI and 3D world signage. | [Unity Documentation](https://unity.com/features/textmesh-pro) |
| **New Input System** | Unity Technologies | Modern event-driven cross-platform input management. | [Unity Documentation](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest) |
| **BIL424 Parkour Project** | Erdem Akz, Hasan Erdem Ak, İzlem Deniz Kaya | Academic base framework and initial player controller concepts (Hacettepe University). | [GitHub Repository](https://github.com/ErdemAkz/FreeRun-424_project-) |

---

## 🛠️ Editor & Build Setup

1. **Unity Version**: Open using **Unity 6 (`6000.6.3f1`)**.
2. **Main Menu Scene**: Navigate to `Assets/Scenes/Tech Demo.unity` or `Assets/Scenes/SecondScene.unity` and press **Play**.
3. **Level Direct Launch**:
   - `Assets/Scenes/EasyLevelScene.unity` (Tutorial & Introductory Target Course)
   - `Assets/Scenes/MidLevelScene.unity` (Industrial Facility - Halved Course, Room 2 Extraction)
   - `Assets/Scenes/HardLevelScene.unity` (Abyssal Void & Chasm - Halved Course, Section 2 Extraction)
4. **Editor Tools**: Use the top menu bar **`Tools > Populate Scene with Targets & Finish Gate`** to bake or refresh targets and extraction gates at any time.
