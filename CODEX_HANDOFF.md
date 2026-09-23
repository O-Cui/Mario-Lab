# Codex Unity Lab Handoff

Last updated: 2026-09-24 (Asia/Singapore)

## Current state

The Super Mario Bros Unity Basics lab has been rebuilt to match the supplied handout.

- Main scene: `Assets/Scenes/MarioLab.unity`
- Unity version: 6000.3.24f1
- Unity MCP was unavailable, so work was completed and verified with Unity batch mode on a temporary project copy.
- Final batch scene build passed with `MARIO_LAB_SETUP_COMPLETE`.
- Final play-mode smoke test passed with `MARIO_LAB_PLAYMODE_TEST_PASSED`.

## Implemented specification

- Mario uses the supplied sliced Mario sprite, Rigidbody2D, BoxCollider2D, `PlayerMovement`, and `JumpOverGoomba`.
- Movement uses the handout values: speed 10, max speed 20, jump impulse 10, gravity scale 1, linear damping 3, and 30 FPS target.
- `a` and `d` control facing; Space jumps only while grounded.
- Ground is tagged and layered `Ground`, uses a tiled source sprite, and has BoxCollider2D.
- Goomba uses `brown_goomba_1`, sorting order 2, a kinematic Rigidbody2D, trigger collider, and a 5-unit/two-second FixedUpdate patrol.
- Collision with the Enemy trigger freezes time; Restart resets Mario, Goomba, score, facing, velocity, and time scale.
- BoxCast scoring, gizmo visualization, TMP score UI, pixel font, restart callback, and Navigation=None are wired.
- PlayerMovement executes before JumpOverGoomba, which executes before EnemyMovement.
- World scenery and objects use named slices from `Assets/Sprites/misc-3.gif`: hills, shrubs, clouds, ground, brick, question block, coin, mushroom, and separate pipe head/body sprites.

## Relevant implementation files

- `Assets/Scripts/PlayerMovement.cs`
- `Assets/Scripts/EnemyMovement.cs`
- `Assets/Scripts/JumpOverGoomba.cs`
- `Assets/Editor/LabSceneBuilder.cs`
- `Assets/Editor/LabPlayModeSmokeTest.cs`

## Verification note

The optional preview renderer is skipped in batch mode because Unity 6.3 can crash when a URP camera is rendered under `-nographics`. This does not affect the scene or Play Mode; both completed successfully.
