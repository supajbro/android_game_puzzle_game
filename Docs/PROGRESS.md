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
- Work Orders 03 and 04 were subsequently authorised and are recorded below.

## 2026-10-17 — Work Order 03

Status: COMPLETE

- Added pure-Core `GameSession` orchestration with three deterministic visible rack slots, explicit selection, two-phase turn input and refill of only the consumed slot after resolution.
- Added `Boot`, `Home`, `Ready`, `Resolving`, `PendingLoss`, `GameOver` and `Paused` states with guarded transitions for boot, new run, pause/resume, loss finalisation, replay, reset and return home.
- Invalid slot/column/full-column actions do not consume a jelly or clear the current selection. Inputs received during `Resolving` are rejected, preventing rapid duplicate placements.
- Added ordered, immutable semantic session events for state, rack, placement, rejection, waves, score, presentation cues, turn completion and run finalisation.
- Converted `GameTestController` into an editor-only three-slot input harness; it remains isolated in the development scene and adds no production visual polish.
- Added `GameSessionTests.cs`, including all three rack slots, illegal indices, invalid-column selection retention, full columns, duplicate input, state/event order, pause/resume, pending loss, replay, reset and immutable event data.
- Files added/changed: `GameSession.cs`, `SessionTypes.cs`, `GameSessionTests.cs`, `GameTestController.cs` and associated `.meta` files.

## 2026-10-17 — Work Order 04

Status: COMPLETE

- Added pure-Core one-based cascade scoring: `cleared cells × 10 × chain number`, while retaining zero-based resolution event indices.
- Added `long` current score, longest chain, endless best and per-day Daily best records. New runs reset current values without mixing or erasing mode-specific records.
- Added deterministic `Chain2`, `Riot3Plus`, `LargeGroup8Plus` and `PersonalBest` presentation metadata, with no TMPro, animation-speed or VFX dependency.
- Extended resolution waves with deterministic component-size metadata so `LargeGroup8Plus` represents one actual group of at least eight rather than the sum of simultaneous smaller groups.
- Added saturating score arithmetic for overflow safety and finalises score/best metadata before entering `GameOver`.
- Added `ScoreModelTests.cs` covering 4-pop = 40, second-wave 5-pop = 100, mixed simultaneous groups, two- and three-wave chains, large groups, overflow, resets, personal bests, Daily/Endless isolation and session event order.
- Verification: the explicit validation projects compiled every Core source and the editor-only controller with 0 warnings and 0 errors. The NUnit reflection runner executed 53 relevant tests: 21 resolver regressions, 20 expanded session cases and 12 score cases; all 53 passed. These tests did not run through Unity Test Runner, so no Unity-run result is claimed.
- Files added/changed: `ScoreModel.cs`, `BoardResolver.cs`, `BoardResolutionResult.cs`, `BoardResolverTests.cs`, `ScoreModelTests.cs`, documentation and associated `.meta` files.
- Next task when authorised: Work Order 05 — deterministic pressure rises and loss checks.

