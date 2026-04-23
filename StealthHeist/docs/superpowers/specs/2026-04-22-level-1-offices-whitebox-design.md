# Level 1 — Offices (Whitebox) Design

Status: **Approved by user, ready to implement.**

## Goal

Build the whitebox geometry for Level 1 of StealthHeist in Unity. Primitive
cubes only. No guards, camera, props, or gameplay logic yet — just the map.

## High-level decisions

| Decision | Value |
|---|---|
| Objective | Spawn → Exit (pure movement/stealth tutorial) |
| Size | Medium — 6 rooms, ~2–3 min playthrough |
| Floor plan style | Hybrid: Reception → Cubicles → Corridor → Offices → Exit |
| Layout shape | **Loop** — corridor connects both offices and the exit |
| Construction tool | Unity primitive cubes (not ProBuilder) |
| Scale | Slightly oversized realism (1 Unity unit = 1 m) |
| Walls | 3 m tall, 0.2 m thick |
| Doorways | 2 m wide gaps (no doors yet) |
| Ceiling | None (whitebox convention) |
| Mechanic placement | Deferred — not part of this pass |

## Origin and axes

- World origin `(0, 0, 0)` = Reception's southwest corner
- +X = east, +Z = north, +Y = up
- Floor plane sits at Y=0

## Room-by-room spec

Coordinates are min/max corners `(x, z)`.

| # | Room | X range | Z range | Size |
|---|---|---|---|---|
| 1 | Reception (spawn) | 0 → 8 | 0 → 6 | 8 × 6 |
| 2 | Cubicles (open plan) | 0 → 15 | 6 → 18 | 15 × 12 |
| 3 | Corridor | 15 → 18.5 | 4.5 → 19.5 | 3.5 × 15 |
| 4 | Office 1 | 13.25 → 20.25 | -2.5 → 4.5 | 7 × 7 |
| 5 | Office 2 | 13.25 → 20.25 | 19.5 → 26.5 | 7 × 7 |
| 6 | Exit Vestibule | 18.5 → 24.5 | 9.5 → 14.5 | 6 × 5 |

**Overall footprint:** ~25 m east-west × ~29 m north-south.

## Doorways (2 m wide)

| From | To | Wall | Gap range |
|---|---|---|---|
| Reception | Cubicles | z = 6 | x 3 → 5 |
| Cubicles | Corridor | x = 15 | z 11 → 13 |
| Corridor | Office 1 | z = 4.5 | x 15.75 → 17.75 |
| Corridor | Office 2 | z = 19.5 | x 15.75 → 17.75 |
| Corridor | Exit Vestibule | x = 18.5 | z 11 → 13 |

Exit Vestibule east wall (x = 24.5) has no doorway — the exit itself is a
marker placed inside the vestibule.

## Wall segments

All walls: primitive cube, scale `(length, 3, 0.2)` or `(0.2, 3, length)`,
center at y = 1.5. Shared walls are created **once**, not per-room.

### Horizontal walls (constant z)

| # | z | x start | x end |
|---|---|---|---|
| 1 | -2.5 | 13.25 | 20.25 |
| 2 | 0 | 0 | 8 |
| 3 | 4.5 | 13.25 | 15.75 |
| 4 | 4.5 | 17.75 | 20.25 |
| 5 | 6 | 0 | 3 |
| 6 | 6 | 5 | 8 |
| 7 | 6 | 8 | 15 |
| 8 | 18 | 0 | 15 |
| 9 | 19.5 | 13.25 | 15.75 |
| 10 | 19.5 | 17.75 | 20.25 |
| 11 | 26.5 | 13.25 | 20.25 |
| 12 | 9.5 | 18.5 | 24.5 |
| 13 | 14.5 | 18.5 | 24.5 |

### Vertical walls (constant x)

| # | x | z start | z end |
|---|---|---|---|
| 14 | 0 | 0 | 18 |
| 15 | 8 | 0 | 6 |
| 16 | 13.25 | -2.5 | 4.5 |
| 17 | 13.25 | 19.5 | 26.5 |
| 18 | 15 | 4.5 | 11 |
| 19 | 15 | 13 | 19.5 |
| 20 | 18.5 | 4.5 | 11 |
| 21 | 18.5 | 13 | 19.5 |
| 22 | 20.25 | -2.5 | 4.5 |
| 23 | 20.25 | 19.5 | 26.5 |
| 24 | 24.5 | 9.5 | 14.5 |

**Total: 24 wall segments.**

## Floor

Single cube covering the full footprint with a small margin.
- Center: `(12.25, -0.05, 12)`
- Scale: `(26, 0.1, 31)`

## Markers

- **Spawn** — empty GameObject named `Spawn` at `(4, 0, 3)` (inside
  Reception). The FirstPersonController (or player prefab) spawns here. For
  now, just place the FirstPersonController directly at this position.
- **Exit** — cube or cylinder named `Exit` at `(21.5, 0.5, 12)` (center of
  Exit Vestibule). Scale small, e.g. `(1, 1, 1)`. Give it a distinct color
  (red) so it's easy to spot. No trigger logic yet.

## Lighting

- One `Directional Light` at rotation `(50, -30, 0)` for basic scene
  lighting.
- Ambient light default.

## Scene setup

- New scene: `Assets/Scenes/Level1_Offices.unity`
- Add to build settings.
- Root hierarchy:
  ```
  Level1_Offices (empty parent)
  ├── Floor            (cube)
  ├── Walls            (empty parent)
  │   ├── Wall_01 ... Wall_24   (24 cubes)
  ├── Markers          (empty parent)
  │   ├── Spawn        (empty)
  │   └── Exit         (cube, red material)
  └── Directional Light
  ```

## Implementation approach

Use the Unity MCP's `execute_script` (or equivalent) to run a single C#
editor script that:

1. Creates the scene if it doesn't exist.
2. Builds the hierarchy above from the tables in this doc.
3. Assigns a simple white/grey default material to floor + walls, red to
   Exit.
4. Saves the scene.

A ready-to-run script generator is left as a trivial task — just iterate
the wall tables above and instantiate one `GameObject.CreatePrimitive(
PrimitiveType.Cube)` per row.

## Existing project context (for the next session)

- Repo: `/Users/alimatar/Desktop/StealthHeist-GamesProject/StealthHeist`
- Existing scenes: `MainMenu`, `SampleScene`, `GameScene`, `TutorialScene`.
  **Do not modify** — the tutorial is another teammate's work.
- Player controller: `Assets/Scripts/Player/FirstPersonController.cs`
- Unity MCP is registered in project-scoped `.mcp.json` as `unity-mcp`
  (Unity's official relay binary at
  `/Users/alimatar/.unity/relay/relay_mac_arm64.app/Contents/MacOS/relay_mac_arm64 --mcp`).
- Current git branch: `feat/level-1-basics`.
