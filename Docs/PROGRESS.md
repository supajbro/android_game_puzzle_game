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

## 2026-10-19 — Work Orders 09 and 10 supplied-art integration

Status: IMPLEMENTED; UNITY VISUAL/PLAYMODE VALIDATION BLOCKED

- Integrated the supplied original `background`, logo, mascot, board overlay, panel/button sheet and icon sheet into a new static production `Main` presentation. No replacement artwork was generated.
- Added a repeatable Editor workflow at `Jelly Riot/Visuals/Configure Artwork` and `Jelly Riot/Visuals/Build Main Scene`. It applies RGBA Sprite import, 100 PPU, bilinear filtering, disabled mipmaps/read-write, ASTC 6x6 Android overrides and explicit named rectangles so neighbouring sprites are not included accidentally.
- Mapped Core colours exactly: `Coral`, `Lemon`, `Aqua`, `Lilac`, `Mint`. Occupied board cells and all three rack candidates use the same mapping, with `Jelly_Shadow` below each jelly.
- Added the 7-column × 9-row static board view, 63 empty-cell holes, separate seven-column hit targets and exactly three reconstructed rack slots. The supplied five-cavity rack art is retained as an available slice but is deliberately not used as the live rack.
- Added supplied-art Home, Gameplay, Pause, Settings, Results and Daily Challenge presentation with working Play/Daily/navigation, rack selection, column drop, pause/resume, Replay/Home, sound/haptics toggles and Android Back behaviour. Views issue Core commands and redraw from authoritative settled state; no Work Order 11+ animation or gameplay mutation was added.
- Added a 1080×1920 Canvas Scaler and runtime safe-area anchors. Layout uses centred fixed logical dimensions sized to remain within the 1080-wide safe area and scale through the Canvas Scaler.
- Added `VisualIntegrationPlayModeTests` covering scene creation, the 63-cell board, three rack slots, five distinct supplied colour sprites and Play navigation.
- Files changed: `Assets/_Project/Scenes/Main.unity`; `Assets/_Project/Scripts/JellyRiot.Runtime.asmdef`; `Assets/_Project/Scripts/UI/JellyRiotMainUI.cs`; `Assets/_Project/Scripts/UI/SafeAreaFitter.cs`; `Assets/_Project/Scripts/Editor/JellyRiotVisualSetup.cs`; `Assets/_Project/Tests/PlayMode/VisualIntegrationPlayModeTests.cs`; generated `.meta` files; `Docs/ART_BIBLE.md`; `Docs/PROGRESS.md`.
- Verification actually completed: `dotnet build JellyRiot.Runtime.csproj --no-restore`, `dotnet build JellyRiot.Editor.csproj --no-restore` and `dotnet build JellyRiot.PlayModeTests.csproj --no-restore` each passed with 0 warnings and 0 errors. A scene-reference audit found 13 unique serialised GUIDs and 0 missing asset GUIDs.
- NOT RUN: Unity PlayMode tests, Console inspection, interactive navigation, visual checks at 360×800 / 1080×1920 / 1080×2400, and screenshots. The project is locked by another background Unity instance, and two isolated Unity 6000.3.11f1 attempts lost the local Licensing Client connection before asset import/test execution. No visual or device result is claimed.
- Artwork issue: the source rack depicts five cavities, so using it directly would contradict the three-slot Core contract. The live rack is reconstructed with three supplied cell holes on the supplied score-panel styling. All requested art otherwise has a usable transparent source.
- Next work order: do not begin Work Order 11 without owner approval. First open the project in a licensed Unity Editor, run `Jelly Riot/Visuals/Build Main Scene`, execute the PlayMode suite, then inspect/capture Home and Gameplay at the three target resolutions.

## 2026-10-19 — Work Order 11 + animation sprite-sheet preparation

Status: IMPLEMENTED; UNITY PLAYMODE/VISUAL VALIDATION NOT RUN

- Added `GridViewPresenter` as the motion-safe presentation boundary. It consumes ordered
  `SessionActionResult` events plus placement/clear/fall/pressure data, uses fixed reusable UI
  jelly views, then reconciles every board/rack visual to an authoritative Core snapshot.
- Board rendering remains 7x9 and rack rendering remains three slots. Static colour mapping stays
  Coral/Lemon/Aqua/Lilac/Mint. Valid target feedback, short invalid-column feedback, corrected
  contiguous draw layers and independent `Jelly_Shadow` views were added.
- Pool return clears the jelly sprite and resets anchored position, size, rotation, scale, colour
  and alpha; shadow position, size, sprite and active state are restored. Focus loss, pause/resume,
  replay, restart, game over and home transitions cancel or reconcile presentation state.
- Added a development/editor-only visual-state overlay reporting snapshot sync, active views,
  duplicate references, queued actions and the last processed event sequence.
- Renamed the five supplied sources from `_animations` to `_Sheet` without touching Work Order 09
  sprites. `Tools/Art/prepare_jelly_animation_sheets.py` proportionally divides each 1402x1122
  sheet, conservatively removes alpha debris without colour keying, and exports exactly 100
  transparent 256x256 RGBA frames beneath
  `Assets/_Project/Art/Animations/Frames/<Colour>/<Category>/`.
- Added five checkerboard contact sheets in `Tools/Art/Previews`, a complete crop/fallback record in
  `Tools/Art/animation_cleanup_report.json`, and usage notes in `Tools/Art/README.md`. Visual review
  found no debris, clipping, neighbouring frames or baked shadows; the automated audit reported
  100/100 RGBA frames with 0..255 alpha and zero fallbacks.
- Added `JellyAnimationLibrary`, four animation categories and an Editor rebuild command. The
  command enforces 100 single centred sprites with 100 PPU, bilinear/clamp, no mipmaps or read/write,
  256 max size and ASTC 6x6 Android import, then creates the ordered five-colour library. Timing,
  tweening and the full Work Order 13 animation remain deliberately unimplemented.
- Added a PlayMode stress test for 100 rapid moves with duplicate input, periodic pause/resume,
  cancellation, restart, pressure and game-over/replay reconciliation.
- Exact code/document files changed: `Assets/_Project/Scripts/Visuals/GridViewPresenter.cs`,
  `Assets/_Project/Scripts/Visuals/JellyAnimationLibrary.cs`, their `.meta` files,
  `Assets/_Project/Scripts/UI/JellyRiotMainUI.cs`,
  `Assets/_Project/Scripts/Editor/JellyRiotVisualSetup.cs`,
  `Assets/_Project/Tests/PlayMode/VisualIntegrationPlayModeTests.cs`, `Docs/ART_BIBLE.md`,
  `Docs/PROGRESS.md`, `Tools/Art/prepare_jelly_animation_sheets.py`, `Tools/Art/README.md`,
  `Tools/Art/animation_cleanup_report.json`, all five `Tools/Art/Previews/*.png` contact sheets,
  the five renamed `Assets/_Project/Art/Animations/Jelly_*_Sheet.png` sources and their retained
  `.meta` files, plus all 100 PNGs enumerated exactly by the cleanup JSON report.
- Verification run: Pillow 12.0.0 export completed; PNG audit confirmed `validated=100 rgba=100
  alpha_range=0..255 fallbacks=0`. `dotnet build JellyRiot.Runtime.csproj --no-restore`,
  `dotnet build JellyRiot.Editor.csproj --no-restore` and
  `dotnet build JellyRiot.PlayModeTests.csproj --no-restore` each passed with 0 warnings/errors.
- Tool dependency review: Pillow 12.0.0 is source-art tooling only, requires Python 3.10+, and its
  installed package metadata declares the MIT-CMU licence; it is not included in the Unity player.
- NOT RUN: Unity PlayMode tests (including the 100-move visual stress test), Unity Console review,
  device/profile allocation measurement, gameplay screenshots and interactive focus-loss review.
  The repository is locked by an existing Unity process, and repeated attempts to launch an
  isolated Unity 6000.3.11f1 batch copy were stopped by the environment approval service before
  Unity started. No Unity/device result is claimed.
- Next work order: do not begin Work Order 12 or 13 without owner approval. First let Unity import,
  run `Jelly Riot/Visuals/Configure Animation Frames`, run the PlayMode suite, inspect the five
  contact sheets and capture Gameplay at the target phone resolutions.

## 2026-10-19 — Work Order 13: jelly drop, landing and squash animations

Status: COMPLETE

- The owner explicitly deferred Work Order 12 (first-time onboarding and microcopy) until after
  Work Order 18. The master-plan schedule and work-order sequence now record that decision; no
  onboarding implementation was included in this work order.
- Added a single, frame-independent presentation state machine for placement: each jelly spawns
  above the grid, accelerates into its authoritative Core destination, lands with a restrained
  squash, rebounds by 10 logical pixels and settles without changing simulation results.
- Fall time is row-sensitive and clamped to 140–240 ms. Landing squash is 70 ms, rebound is 85 ms
  and final settling is 70 ms. Falling, Landing and Squash/Stretch use the five ordered frames for
  the placed jelly's Core colour; incomplete libraries fall back safely to the static colour sprite.
- Shadows remain separate visual elements. They contract and lighten while the jelly is airborne,
  widen and darken on contact, then return to their baseline state after settling or cancellation.
- Added cosmetic idle blinks using an isolated `System.Random` stream, varied 1.8–4.5 second
  intervals and one jelly at a time. Blink scheduling neither reads nor advances deterministic Core
  random state, and action animation pre-empts any active blink.
- Turn input remains locked until visual settling. Pause freezes the active animation without
  advancing time; home/replay/restart/focus-loss cancellation snaps pooled views to the current
  authoritative snapshot and cannot leave stale transforms, sprites or shadows.
- Added a reduced-motion setting with a minimal 80 ms placement and no bounce, squash or idle blink.
  Added an Editor/development-only `ANIM 1x` / `ANIM 0.25x` control; it scales presentation time only.
- Files changed: `Assets/Jelly_Riot_Codex_Master_Plan.md`;
  `Assets/_Project/Scenes/Main.unity`; `Assets/_Project/Scripts/Visuals/GridViewPresenter.cs`;
  `Assets/_Project/Scripts/UI/JellyRiotMainUI.cs`;
  `Assets/_Project/Scripts/Editor/JellyRiotVisualSetup.cs`;
  `Assets/_Project/Tests/PlayMode/VisualIntegrationPlayModeTests.cs`; `Docs/PROGRESS.md`.
- Imported assets completed during validation: all 100 animation frame `.meta` files and
  `Assets/_Project/Settings/JellyAnimationLibrary.asset` plus its `.meta`. The library contains all
  five colours, all four categories and five ordered frames per category.
- Verification: `dotnet build JellyRiot.Runtime.csproj --no-restore`,
  `dotnet build JellyRiot.Editor.csproj --no-restore` and
  `dotnet build JellyRiot.PlayModeTests.csproj --no-restore` each passed with 0 warnings and
  0 errors. An isolated Unity 6000.3.11f1 PlayMode run passed 5/5 tests in 16.661 seconds. Coverage
  includes scene/library integrity, 30 animated turns and settled-model equality, pause freezing,
  reduced motion, 0.25x development timing, unchanged Core RNG state, and 100 rapid
  cancel/interruption recoveries with no duplicate or stale pooled views.
- Manual device profiling, screenshots and perceptual comparison at forced 30 FPS versus 60 FPS
  were not run. Animation timing is delta-time based, and the automated Unity suite completed at
  the editor's configured 60 FPS target, but no claim is made for device-specific visual quality.
- Next work order: do not begin Work Order 14 without owner approval. Work Order 12 remains
  explicitly deferred until after Work Order 18.

## 2026-10-19 — Work Order 16: complete audio integration

Status: IMPLEMENTED; AUTOMATED PLAYMODE PASS; DEVICE LISTENING/HAPTICS NOT RUN

- Added a central `AudioManager` with a fixed ten-voice 2D SFX pool, dormant functional music
  channel, persisted Master/SFX/Music/mute controls, cosmetic-only variant/pitch randomness,
  bounded voice stealing, short major-cue ducking and delayed-score/wave throttling.
- Wired Core event sequences exactly once: rack/UI taps, invalid actions, authoritative drop start,
  animation-timed landing, per-cleared-group pop batches, ascending cascades, one RIOT stinger per
  qualifying action, score ticks, pressure rise, game over, personal best and Daily completion.
  Invalid placements cannot emit drop or landing cues.
- Added presentation callbacks at first contact and settled turn boundaries. Pause, focus loss,
  Android backgrounding and the public advertisement-interruption hook clear delayed sounds and
  pooled voices; resume restores the optional music channel without retaining a duck.
- Added `AndroidHapticsService` with persisted opt-out, safe Android/device checks and restrained
  landing, match and RIOT durations. Unsupported platforms do nothing.
- Added `JellyRiotAudioSettings.asset`, referencing all 26 supplied WAVs. All 26 `.wav.meta` files
  under `Audio/SFX/{UI,Jelly,Cascades,Game}` now use mono, preloaded, decompress-on-load,
  optimised-rate Vorbis quality 0.76 settings for default and Android. Source WAV data, names and
  locations were not changed.
- Added `Jelly Riot/Audio/Configure SFX` and a Play-Mode SFX preview window with an individual
  button for every clip. Expanded `Docs/AUDIO_BIBLE.md` with mapping, mix and manual checks.
- Exact code/assets changed: `Assets/_Project/Scripts/Audio/{IAudioService.cs,AudioManager.cs,
  JellyRiotAudioSettings.cs}`; `Assets/_Project/Scripts/Platform/{IHapticsService.cs,
  AndroidHapticsService.cs}`; `Assets/_Project/Scripts/UI/JellyRiotMainUI.cs`;
  `Assets/_Project/Scripts/Visuals/GridViewPresenter.cs`;
  `Assets/_Project/Scripts/Editor/JellyRiotAudioSetup.cs`;
  `Assets/_Project/Resources/JellyRiotAudioSettings.asset`;
  `Assets/_Project/Tests/PlayMode/AudioIntegrationPlayModeTests.cs`; all associated new `.meta`
  files; all 26 existing `Assets/_Project/Audio/SFX/**/*.wav.meta` files;
  `Docs/AUDIO_BIBLE.md`; `Docs/PROGRESS.md`.
- Verification actually run in an isolated Unity 6000.3.11f1 project copy: audio configuration
  completed and logged exactly 26 WAVs; the final full PlayMode suite passed **9/9** in 17.452 seconds
  (new audio tests 4/4, foundation 1/1, visual regression tests 4/4). Unity exited code 0 with no
  test failures and no leaked weak pointers. Tests cover all clip references, bounded 2D pool,
  duplicate-event suppression, persistent settings, interruption clearing and silent gameplay.
- NOT RUN: subjective listening on phone/headphones, Android vibration feel/capability variants,
  real Android background/focus, real advertisement lifecycle, device clipping capture and Unity
  Console inspection in the owner's currently open project. These remain explicit manual gates.
- Next work order: do not begin Work Order 17 without owner approval. Work Order 12 remains deferred
  until after Work Order 18.

## 2026-10-21 — Focused update: 10 × 4 jelly sheets and grid/rack alignment

Status: COMPLETE; AUTOMATED PLAYMODE PASS; DEVICE SCREENSHOT NOT CAPTURED

- Updated all five newly supplied 1983 × 793 sheets to proportional 10 × 4 slicing. Each sheet now
  imports as exactly 40 centred slices, and the existing `JellyAnimationLibrary.asset` remains the
  stable scene reference while containing 10 Idle, Blink, Landing and Recovery frames per colour.
- Added the authoritative Unity preparation path for 200 cleaned 256 × 256 RGBA frames beneath
  `Assets/_Project/Art/Animations/Frames40/`. Cleanup removes detached/edge debris by alpha geometry,
  uses one scale across every colour/state, centres every body horizontally and registers every
  frame to the same bottom contact line. No frame has opaque pixels on its canvas edge.
- Replaced the five-frame animation contract with elapsed-time-based ten-frame playback. Idle loops
  continuously; independent cosmetic timers trigger expression/blink sequences; landing flows into
  recovery and then idle. Reduced Motion and Core random state remain unchanged.
- Fixed the board and rack hierarchy at the underlying cause: each jelly now has a cell/slot-centred
  stable root containing separate `Shadow` and animated `Artwork` children. Only `Artwork` moves or
  scales. Board artwork uses a shared 92% container (settled visible width approximately 80–83% of
  the 78 px cell hole); rack artwork uses a shared 96% container. No per-colour offsets exist.
- Pooled views reset sprite, child position, size, scale, rotation, colour, alpha, shadow and state.
  Falling now converts the top-row spawn point into the destination root's local space, so every
  landing finishes at the authoritative cell root after the hierarchy change.
- Pixel audit: 200/200 PNGs are 256 × 256, 200/200 contain transparency, zero invalid dimensions,
  zero opaque edge pixels, body bounds 197–236 px wide and 117–225 px high. Five refreshed 10 × 4
  checkerboard contact sheets visually confirm preserved expressions/squash and consistent baselines.
- Exact code/config files changed: `Assets/_Project/Scripts/Visuals/JellyAnimationLibrary.cs`,
  `Assets/_Project/Scripts/Visuals/AnimatedJellyView.cs`,
  `Assets/_Project/Scripts/Visuals/GridViewPresenter.cs`,
  `Assets/_Project/Scripts/UI/JellyRiotMainUI.cs`,
  `Assets/_Project/Scripts/Editor/JellyRiotVisualSetup.cs`,
  `Assets/_Project/Tests/PlayMode/VisualIntegrationPlayModeTests.cs`,
  `Assets/_Project/Settings/JellyAnimationLibrary.asset`, all five
  `Assets/_Project/Art/Animations/Jelly_*_Sheet.png.meta` files, the complete generated
  `Assets/_Project/Art/Animations/Frames40/` tree and metadata,
  `Tools/Art/prepare_jelly_animation_sheets.py`, `Tools/Art/README.md`,
  `Tools/Art/animation_cleanup_report.json`, all five `Tools/Art/Previews/*_ContactSheet.png`, and
  `Docs/PROGRESS.md`.
- Verification actually run: `dotnet build JellyRiot.Runtime.csproj --no-restore`,
  `dotnet build JellyRiot.Editor.csproj --no-restore`, and
  `dotnet build JellyRiot.PlayModeTests.csproj --no-restore` each passed with 0 warnings/errors.
  An isolated Unity 6000.3.11f1 PlayMode run passed 10/10 tests in 38.406 seconds. Coverage includes
  all 200 library references, 256 × 256 rects and centred pivots; root/hole centre equality at
  360 × 800, 1080 × 1920 and 1080 × 2400; 30 animated turns; pause and Reduced Motion; exact final
  model sync; and 100 rapid moves/cancellations without stale pooled views. The existing audio tests
  also remained green.
- Blockers/manual evidence: the owner's live Unity project was open, so import and tests ran in an
  isolated copy and only scoped generated assets/metadata were copied back. A gameplay screenshot,
  subjective 30-versus-60 FPS comparison and physical Android review were not captured; the five
  contact sheets are the available visual evidence. Let the open Editor finish its refresh, then
  inspect one live drop and rack selection before device sign-off.
- Next work order: unchanged; do not begin unrelated or later work without the owner's request.

## 2026-10-22 — Work Order 14: complete VFX integration

Status: IMPLEMENTED; C# COMPILE PASS; LIVE UNITY IMPORT/PLAYMODE VALIDATION PENDING

- Added a central `JellyVFXManager` in the presentation assembly. It consumes authoritative
  `ResolutionWave`, score and pressure-row results through the existing visual queue and never
  changes Core score, cells, randomness or row timing.
- Implemented the production sequence at 120 ms anticipation plus 140 ms pop/release per wave,
  a 240 ms visible pressure-row insertion, 680 ms RIOT reveal, short chain labels, authoritative
  score fly-ups and a readable one-move warning. Gravity state advances only at the pop transition.
- Added pooled UI presentation with 48 shared sprite slots and six TextMeshPro score-label slots.
  Each cleared group emits 6–10 moving particles (two minimal accents in Reduced Motion), with a
  hard 48-sprite concurrent ceiling; splashes, glints, trails and labels never instantiate per pop.
- Wave 2 displays `CHAIN x2` with a restrained board glow. Wave 3+ displays chain progression and
  triggers RIOT once per qualifying primary or pressure chain, including the original wordmark,
  glints/trails, non-strobe glow and board-only shake clamped to five logical pixels.
- Reduced Motion removes shake, moving RIOT trails, large bursts and flashing while retaining
  static TextMeshPro chain/RIOT feedback, compact score motion, warnings and all authoritative audio.
- Re-timed the existing audio scheduler to the 120 ms release point and 260 ms wave cadence. The
  established Work Order 16 manager remains the only sound owner, so pop variants, cascades,
  score tick, `Riot_Stinger` and `Row_Rise` cannot double-trigger from VFX.
- Added deliberate sprite preparation for the supplied originals: 16 `JellyPop`, 12 `Sparkle`,
  16 `Trail`, 12 `Glow`, six irregular `Warning` rectangles and one single `RIOT` sprite. Rectangles
  use centred pivots and fixed cell boundaries instead of automatic alpha/grid slicing; source PNG
  pixels are preserved. `JellyVFXLibrary.asset` is the reusable Resources-backed reference adapter.
- Added `Jelly Riot/VFX/Open Demonstration`, which drives the live production manager for normal
  pop, 50+ cell/large pop, CHAIN x2, CHAIN x3/RIOT, score fly-up, one-move warning and row rise.
- Cancellation now clears all pooled sprites, labels, transforms and delayed callbacks on restart,
  home, focus loss and advertisement interruption. Pause freezes VFX and resume continues it.
- Exact code/config files changed or added: `Assets/_Project/Scripts/VFX/{JellyVFXLibrary.cs,
  JellyVFXManager.cs}`; `Assets/_Project/Scripts/Editor/JellyRiotVFXSetup.cs`;
  `Assets/_Project/Scripts/{JellyRiot.Runtime.asmdef,Visuals/GridViewPresenter.cs,
  UI/JellyRiotMainUI.cs,Audio/AudioManager.cs}`; `Assets/_Project/Resources/JellyVFXLibrary.asset`;
  `Assets/_Project/Tests/PlayMode/{JellyRiot.PlayModeTests.asmdef,VFXIntegrationPlayModeTests.cs}`;
  associated `.meta` files; the six VFX texture `.meta` files after Unity preparation; this record.
- Verification actually run: generated-project `dotnet build` for `JellyRiot.Runtime`,
  `JellyRiot.Editor` and `JellyRiot.PlayModeTests` passed sequentially with zero warnings/errors.
  The focused tests compile and cover the exact timings, TextMeshPro use, library category counts,
  fixed pools, 50+ cell demonstration ceiling, cancellation and Reduced Motion RIOT path.
- Pending evidence: the owner's Unity Editor was open with deferred asset refresh. A standard refresh
  was started and the new scripts/sheets entered Unity's import queue, but import and domain reload
  had not completed when this entry was written. Therefore the PlayMode suite, profiler/device run,
  10-second chain capture, 30/60 FPS perceptual check and Android midrange profiling are not claimed.
- Next work order: do not begin Work Order 15 or any unrelated work without owner approval.

