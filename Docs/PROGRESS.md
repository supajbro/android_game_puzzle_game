# Jelly Riot progress

## 2026-10-17 — Project organisation and Work Order 00

Status: COMPLETE

- Audited Unity version, packages, render pipeline, scenes, scripts, Android tooling and project settings.
- Confirmed Unity 6000.3.11f1, URP 17.3.0 with 2D Renderer, Unity Test Framework 1.6.0 and Android Build Support with bundled SDK/NDK/OpenJDK.
- Local Android platforms found: API 31, 33, 34 and 35. API 36 is not installed and is a later release-build blocker.
- Existing custom content was limited to the Unity template assets, `SampleScene` and the master plan; no pre-existing gameplay scripts or conflicts found.
- Created the planned root and `Assets/_Project` hierarchy, documentation baseline and assembly architecture.
- Added `Boot` and `Main` scenes, placed them in build order, locked portrait defaults to 1080x1920, and added runtime 60 FPS initialisation.
- Added inspector-backed `GameBalanceSettings` and small future integration boundaries for persistence, ads, analytics, audio, haptics and sharing without implementing those later systems.
- Created and retained Unity-generated `.meta` files for the new asset hierarchy.
- Files added/changed: `AGENTS.md`; `.gitignore`; `Docs/*.md`; `Assets/_Project/Scenes/*`; `Assets/_Project/Scripts/{Core,Gameplay,Editor,Ads,Analytics,Audio,Persistence,Platform}/*`; `Assets/_Project/Tests/*`; `Assets/_Project/Settings/GameBalanceSettings.asset`; planned placeholder directories under `Assets/_Project`, `Tools` and `StoreAssets`; `ProjectSettings/ProjectSettings.asset`; `ProjectSettings/EditorBuildSettings.asset`; associated `.meta` files.
- Verification: Unity 6000.3.11f1 imported and compiled the isolated validation copy without C# errors. PlayMode foundation smoke test passed 1/1 (Boot loaded Main; target frame rate was 60).
- Blockers: Android API 36 is not installed (platforms end at API 35). This is a Work Order 25 release-build prerequisite, not a blocker for current Core work. Final company name, application identifier and signing remain owner inputs.

## 2026-10-17 — Work Order 01

Status: COMPLETE

- Implemented a Unity-independent 7x9 `BoardModel`, five-colour enum, bounds validation, lowest-empty-cell placement and failure results that preserve board, turn and RNG state.
- Implemented fixed-algorithm PCG random generation with explicit serialisable state, Unity-JSON-compatible run snapshots and exact restoration.
- Initial generation produces exactly 20 occupied cells (31.7%) in the bottom four rows, uses three colours, prevents connected groups of four and guarantees a legal one-drop opening. Deterministic backtracking prevents rare greedy-generation dead ends.
- Added immutable-ish move/event results, safe board reads/copies, reset/snapshot hooks, balance adapter and development-only ASCII diagnostics.
- Files added/changed: `Assets/_Project/Scripts/Core/*`; `Assets/_Project/Scripts/Gameplay/Configuration/GameBalanceSettings.cs`; `Assets/_Project/Settings/GameBalanceSettings.asset`; `Assets/_Project/Tests/EditMode/BoardModelTests.cs`; associated assembly definitions and `.meta` files.
- Verification: 24/24 Unity EditMode tests passed in 0.268 seconds; deterministic standalone smoke validation passed across 10,000 seeds; compilation succeeded with no C# errors.
- No match clearing, gravity, scoring, pressure rows, rack/session state or other Work Order 02+ behaviour was implemented.
- Work Order 02 was subsequently authorised and is recorded below.

## 2026-10-17 — Work Order 02

Status: COMPLETE

- Implemented iterative orthogonal flood-fill match detection for every connected shape of four or more jellies. Diagonal cells do not connect and each component is visited once.
- Implemented simultaneous per-wave clearing followed by deterministic, stable vertical gravity. Jellies retain their order, never move horizontally and cannot remain floating.
- Implemented repeated resolution waves until stable, with a mathematical termination guard based on the 63-cell board and the four-cell minimum clear.
- Added immutable structured results for wave indices, cleared positions/colours/counts and falling source/destination/colour records. Empty and already-stable boards return a valid zero-wave result.
- Added 21 focused `BoardResolverTests` covering every Work Order 02 scenario, deterministic ordering and read-only result collections while preserving all Work Order 00/01 tests.
- Added `Assets/_Project/Scenes/GameTest.unity`, an excluded-from-build development playground for the currently implemented placement and resolution flow. It deliberately contains no scoring, pressure, rack, animation, VFX, audio, ads or production UI work.
- Files added/changed: Core board resolver and result types; `BoardModel.ResolveMatches`; EditMode resolver tests; development playground controller and scene; associated `.meta` files; this progress record, design notes and test matrix.
- Verification: Core, EditMode test and Runtime assemblies compiled with zero C# errors against Unity 6000.3.11f1-generated project files. All 21 resolver NUnit test methods executed in a local deterministic runner and passed. The Unity Test Runner itself was not completed because the headless editor repeatedly lost its local Licensing Client connection before test execution; no Unity-run result is claimed for this work order.
- Known limitations: the playground applies resolution immediately and presents final board state only; animation and full session flow intentionally belong to later work orders. The previously recorded Android API 36 release prerequisite remains unchanged.
- Next task when authorised: Work Order 03.

