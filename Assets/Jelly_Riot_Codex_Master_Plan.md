# JELLY RIOT — One-Week Android Production Plan

**Version:** 1.0  
**Prepared:** 17 October 2026 (Brisbane, Australia)  
**Format:** Executable, sequential Codex work orders + production specification  
**Owner:** Solo developer using ChatGPT for design, art/audio direction, review and iteration; Codex for implementation  
**Engine:** Unity 6.3 LTS, C#, Android, 2D portrait  
**Primary outcome:** A polished, playable, testable, advertising-ready Android App Bundle and a store launch kit **within seven development days**. Public release and real advertising approval are external milestones and cannot be guaranteed in that week.

> **Commercial reality:** A$500–A$2,000/month is a business goal, **not** a prediction. This plan optimises for a credible small-game launch, observable retention, organic discovery and ad revenue **without** compromising player trust. It cannot guarantee virality, installs, revenue or that a week of automated development will be free of manual fixes.

---

## 0. How to use this document

1. Create an empty **Unity 6.3 LTS** 2D project. Confirm Android Build Support + SDK/NDK/OpenJDK installed. Set project folder/repository to a short path without spaces where possible.
2. Read **Product Bible**, **Acceptance Gates**, and **Roles** before coding.
3. Give **Codex Work Order 00** to Codex first. Then execute **01, 02, ... 32 in sequence**. Each fenced work order is intended to be pasted *as a stand-alone task* into Codex; the instruction to read `AGENTS.md` and this master plan is intentional.
4. At each gate, **run the game or tests**, check Codex's final report, and verify actual device behaviour. Only mark completed work as done when demonstrably verified.
5. Use ChatGPT after each visual/audio milestone to critique screenshots, refine artwork prompts, assess player-facing polish and review results. **Codex should not silently invent a replacement feature or mark unverified external steps as complete.**
6. Keep a `Docs/PROGRESS.md` file. Every work order must add a dated status line, exact files changed, tests run, evidence of success, and blockers. If a task cannot be executed, record **BLOCKED**, preserve a compilable project, and describe the manual step precisely.
7. All player-facing language is English; code comments, documentation and interface labels use **Australian English** where appropriate.

### A realistic 7-day schedule

| Day | Codex work orders | Required deliverable | Non-negotiable gate |
|---|---|---|---|
| **1** | 00–07 | Deterministic, playable, testable game simulation | Player can complete full run, fail, restart, with no errors |
| **2** | 08–11 | Functional portrait UI + complete reusable jelly art | Readable and appealing on phone, no placeholder rectangles |
| **3** | 13–17 | Juice: animations, cascades, VFX, sound, music | Looks and sounds marketable in a 10-second capture |
| **4** | 18, deferred 12, 19–21 | Onboarding, daily challenge, saves, settings/accessibility | Never loses data or traps a player |
| **5** | 22–25 | Analytics instrumentation, ads abstraction, consent, Android build | Test ads only; no consent or ad-related crashes |
| **6** | 26–29 | Privacy/store materials, automated and phone QA, performance, screenshots | QA sign-off and working test AAB |
| **7** | 30–32 | Creative marketing kit, launch configuration, closed test, final audit | Uploaded/ready for test track, with all blockers declared |

**Cut rule:** If behind schedule, preserve simulation, essential polish, ads/consent, crash-free Android build, privacy and store readiness. Cut secondary themes, optional shader polish, experimental bonus modes, extensive analytics dashboards and extra marketing variants. **Never cut compliance, testability, consent, or performance to add cosmetic features.**

---

# PART A — PRODUCT BIBLE (SOURCE OF TRUTH)

## 1. Core promise, positioning and audience

- **Working title:** Jelly Riot (verify uniqueness, trademark risk, store availability and package name before publishing; never assume the name is available).
- **Tagline:** *One drop. Massive combos.*
- **Genre:** Free, one-thumb, endless casual colour-matching gravity puzzle.
- **Platform:** Android first; Google Play; phones first, tablet-safe as a stretch goal.
- **Look:** Premium, juicy, soft 2D candy/jelly visuals — NOT a cheap hyper-casual prototype. A clean deep indigo/plum background contrasts with bold translucent jelly blobs, soft highlights, tiny friendly facial expressions, warm cream typography, glitter and splash effects.
- **Player promise:** Understand the game in five seconds; finish or replay a run in 1–5 minutes; get satisfying feedback in the first ten seconds; believe a big combo is always one clever move away.
- **Target audience:** General casual puzzle players, primarily adults/teens. **Do not assume or market primarily to children**; accurately complete Play Console target-audience declarations and configure age-suitable ad settings according to the declared audience.
- **Organic content hook:** A board looks doomed, one drop causes 4–8 satisfying cascades, visible score accelerates, end card asks “WHERE WOULD YOU DROP?”; all promotional footage must be faithful to real gameplay.
- **Differentiation:** Selected next-piece rack (three choice slots), exceptionally strong audiovisual combo escalation, a distinct jelly identity, daily fixed-seed challenge, and score sharing. A cosmetic **RIOT!** moment after 3+ cascade waves makes a recognisable share-worthy payoff.

## 2. Launch scope — locked

### MUST ship (MVP)

1. Endless mode with deterministic rules and seeded generation.
2. 7-column × 9-row board; orthogonal groups of 4+ clear simultaneously; gravity and cascades.
3. A rack of three visible candidate jelly colours. Player taps one candidate, then any non-full column to drop it. Consumed rack slot refills deterministically after resolution.
4. New pressure row from bottom at periodic move counts; scoring, combo chain, game over, restart.
5. Snappy portrait UI, score, best score, next-row countdown, high-contrast selection, pause, settings.
6. First-time contextual tutorial; three optional colour-blind symbols; reduced motion; haptics/sound toggles.
7. Daily Challenge using one shared date-based seed and identical deterministic generation; no paid advantage, ad continues or rewarded rerolls in this mode.
8. Local save/recover; mute/pause/focus handling; basic event analytics.
9. Rewarded ad: **one optional continue per endless run** after a genuine game over. Interstitial: **only after finished runs**, gated conservatively. Fully functional offline/ad-unavailable gameplay.
10. UMP consent, privacy controls as required, accurate data disclosures, developer website and app-ads.txt readiness.
11. Stable release build (AAB), icon, feature graphic, screenshots, descriptions, at least six honest short video concepts.

### Explicitly OUT of scope for week 1

- Multiplayer, leaderboard server, account/login, friends, cloud save, push notifications, shop, currencies, battle pass, energy timers, battle royale, 3D models, user-generated levels, complex social SDKs, banners during gameplay, rewarded interstitial formats, live-ops backend, manipulation of device clock, deceptive ad placement, made-up ratings, fake urgency.
- Paid “remove ads” IAP at launch: add later only after Billing integration + purchase acknowledgement/restoration is carefully tested. Do **not** place an inert purchase button.
- Unverified “AI-generated 1000 levels” claims. The launch mode is endless and seed-based.

## 3. Exact gameplay rules (the engineering contract)

### 3.1 Coordinates and board

- Grid width **7**, height **9**; `(0,0)` is the **top-left**, `(8,6)` the bottom-right.
- Each cell stores `Empty` or one of up to five `JellyColour` enum entries. The board model has **no UnityEngine dependency**.
- Initial difficulty: **three active colours**; start with an attractive 25–35%-occupied setup concentrated in the bottom 3–5 rows and with a guaranteed reasonable first choice. Preserve exact deterministic seed reproduction.
- On first release, spawn four colours from a score/move milestone (tune in tests), and optionally five much later. All colours must remain visually distinguishable with colour-blind markers.

### 3.2 Turn sequence — no ambiguity

State graph: `Boot → Home → NewRun → Ready → PieceFalling → ResolveMatches → (Gravity → ResolveMatches)* → TurnResult → (PressureRise → ResolveMatches ...)? → Ready / GameOver → ContinueOffer / Results → Home / NewRun`.

1. From `Ready`, player chooses one rack slot and a non-full column; input during the remainder of a turn is ignored/queued only visually, **never applied twice**.
2. Place chosen colour in the **lowest empty cell** of selected column. If column full, show subtle negative feedback and do not consume candidate or move.
3. Animate falling piece; animation never controls game-state correctness.
4. Find all orthogonally connected **same-colour groups of at least four**. Identify **all qualifying groups before clearing any**.
5. Clear all qualifying cells together; award score; animate pops.
6. Gravity: for each column, stable-compact non-empty cells to bottom, preserving bottom-to-top relative order. Then rescan for another match wave.
7. Repeat until no matches. Chain number begins at one for the first clear wave; 3+ waves triggers “RIOT!” audiovisual treatment only — no surprise gameplay mutation.
8. Refill the consumed rack slot **after all clears and gravity are resolved**. Use a seedable RNG dedicated to game state.
9. Increment turn count, decide whether pressure row triggers, then apply row rise **atomically**. For v1, new row is a deterministic 7-wide mix of empty and coloured cells (see below); shift all cells up one row, insert new row at bottom. Resolve any resulting matches, gravity and score using same pipeline.
10. Finish turn; save snapshot; return to `Ready`, unless game-over condition.

### 3.3 Pressure row and loss

- For early onboarding: no rise in first **six turns**. Thereafter default rise cadence: every **six placed pieces**, then every **five** after ~20 turns, every **four** after ~45 turns; tune with tester results. Show number of placements until next rise clearly.
- Each generated pressure row contains **3–5 colours and 2–4 gaps** across seven columns. Generator must avoid seven identical colours, overly punishing repetitive patterns and “unavoidable immediate death” early on; it must be deterministic with the run seed.
- If shifting up would displace any occupied top cell, **pause before applying row rise** and set pending-loss state. This is a game over only if player does not validly continue. Do not silently discard jelly.
- A board with all columns full is also game over when no legal drop exists. Check after every settled turn.
- All pending state is serialisable; app suspension at the pending-loss boundary must not grant free continues or corrupt state.
- No hidden difficulty or fake-random tricks to force losses or increase ad viewing. Difficulty follows disclosed/tunable rules.

### 3.4 Scoring and combos

- Each popped jelly awards **10 × cascadeWaveIndex** points (wave indexes 1, 2, 3...). Example: first 4-pop wave = 40; second 5-pop wave = 100; combined = 140.
- Pressure-triggered cascade also scores using same rule; avoid repeatedly awarding for an already-popped cell.
- Set `RIOT!` feedback if cascade wave >=3; **visual/audio multiplier celebration only**. Score and best updates are deterministic and separate from view animations.
- Bonus treatment for large group (>=8) with *text/visual*, not extra hidden score until tested.
- `BestScore` is endless-mode-only; Daily Challenge score stored separately by date.

### 3.5 Rack fairness and reproducibility

- Start with three candidate colours, all visible. Tap to select, then tap to drop. Selected slot clearly scales/pulses and retains highlight until used.
- Use seeded `System.Random` or a documented pure deterministic PRNG. Persist generator state/counter with each snapshot. **Do not rely on UnityEngine.Random or global random state for gameplay**.
- Prioritise approximate colour distribution balance using limited weighted lookback (e.g., reduce excessive immediate repetition), but do not secretly alter odds based on monetisation/ads.
- Verify thousands of reproducible simulations. Seed + move decisions must regenerate the exact same grid, rack, score and row-rise sequence.

### 3.6 Optional rewarded continue (Endless mode only)

- Present a game-over screen with: score, best, Replay, Home, and optionally **“Watch an ad to continue”** if a rewarded ad is ready and this run has never continued.
- Ad is **entirely optional**; Replay and Home remain equally clear. Reward is granted **only after the verified rewarded callback**, never just by opening/closing ad.
- Continue effect: restore the **last pre-loss settled board** (or pending-loss state), clear up to two highest occupied rows from that board, cancel the pending pressure rise if present, reset the pressure countdown to 3, then resume. No extra score is awarded for cleared rescue cells. Keep `continuedThisRun = true` persistently before resuming.
- If the ad fails, is unavailable, is closed before reward, app backgrounds, or consent does not permit ad request, no reward is granted and Replay/Home still work. Do not show a fake button promising a reward.
- Daily Challenge has no continue; no ad-based advantage.

### 3.7 Daily Challenge

- UTC day key in format `yyyy-MM-dd` (document day boundary); same daily key gives same seed, board, pressure rows, rack and rules for every player running same game version. A local challenge score is not a globally verified leaderboard.
- One *scored* run per date. Allow free “practice” retries labelled **Practice — does not update daily result**; avoid real-money FOMO and complicated time gating.
- The daily seed derives from a stable documented hash of `date + rulesetVersion + fixedSalt`, not runtime-dependent `GetHashCode()`; never generate new randomness on scene reload.
- Offer score-copy/share text: `JELLY RIOT • Daily YYYY-MM-DD • 1,240 points • 5 RIOT chains — Can you beat it?` (actual values only). Include a store link **only after real URL exists**; otherwise omit it.
- No network requirement; prevent obvious clock-based data corruption but do not pretend local records are anti-cheat-secure.

## 4. Art direction — exact visual language

| Element | Direction |
|---|---|
| Palette | Night-plum `#14142B`, mid-indigo `#292A56`, cream `#FFF5E8`, aqua `#45DFF5`, coral `#FF668D`, lemon `#FFE16A`, lilac `#B88AFF`, mint `#57E6A6` |
| Jellies | Round-square glossy squishy drops; broad glassy gradient, translucent rim, bright off-centre highlight, soft inner shadow, slight surface sparkle; gentle little eyes only on large, readable pieces |
| Silhouette | 85–90% rounded-square footprint inside cell; safe padding for bounce; do not confuse occupied and empty cells |
| Board | Rounded indigo tray, subtly inset holes, low-contrast border; piece colours pop at thumbnail size |
| Typography | High-legibility rounded sans-serif with appropriate redistribution rights; fallback to legally distributable Unity/system font, not pirated/branded assets |
| UI | Large comfortable tap targets, minimal labels, crisp score numbers, restrained chrome, clear dominant action |
| VFX | Combos feel like squishy jelly implosions then juicy sparkling bursts; playful, not flashing strobe |
| Motion | 0.1–0.35s actions, anticipation + overshoot + squash; skippable/reduced-motion alternative |
| Mascot / logo | A surprised glossy jelly with three smaller blobs spraying outward; “JELLY RIOT” in legible bold cream/pink lettering, not a clone of an existing title |

**Artwork sources:** Preferred v1 path is deterministic, original 2D PNG sprites generated with code (Unity Editor Texture2D/PNG or Python Pillow), then imported as Sprites and packed. ChatGPT can separately generate original high-fidelity concept art and store graphics when explicitly requested; those can replace the procedurally built art, not block initial delivery. No third-party assets without verified licensing.

### Art asset manifest and resolutions

- `Jelly_{Coral,Lemon,Aqua,Lilac,Mint}.png` each **256×256 RGBA**, matching consistent shape, rim, highlight and shadows; face marks optional via separate overlay so colour-blind symbol overlays remain independent.
- `Jelly_Shadow.png` 256×256 soft alpha; `Jelly_Glint.png` 128×128; `Jelly_Splash_{01..04}.png` 256×256; 4–8 star/sparkle pieces.
- `Board_Backplate.png` **1024×1400** 9-sliced scalable frame; `Cell_Hole.png` **128×128**, `Rack_Slot.png` **256×256**, `Button_Pill.png` **512×192**, `Panel.png` **640×640** nine-slice edges; original art, no screenshots cut from competitors.
- UI glyphs `Pause`, `Replay`, `Sound`, `Haptics`, `Home`, `Info`, `Daily` as **96×96** source PNG or clean vector-generated PNG; selection outline and arrow.
- Logo raster **1600×900** transparent + compact square **1024×1024** for icon assembly; allow procedural art fallback.
- Source artwork must be retained under `Assets/_Project/Art/Source`; exported sprites under `Assets/_Project/Art/Sprites`; verify proper filter/mipmap/compression settings per usage.

## 5. Audio direction and asset manifest

Sound is a critical part of the value proposition, not an afterthought. Original generated sound assets must feel delightful at low volume on phone speakers. No unlicensed samples, copyrighted melodies or unsupported “AI music” claims.

| Asset | Design / trigger |
|---|---|
| `UI_Tap_01..03.wav` | Soft, light gummy tick when choosing a piece |
| `UI_Invalid.wav` | Gentle low wooden “bup” for full column, no harsh buzzer |
| `Jelly_Drop_01..04.wav` | Elastic plop with watery click; randomise variants |
| `Jelly_Land_01..03.wav` | Short mellow impact (may be mixed into Drop), slight pitch variation |
| `Jelly_Pop_01..05.wav` | Thick bright “blup/pop”, no painful transient |
| `Cascade_02..05.wav` | Ascending musical stingers for waves 2–5; never play a huge overlapping chord stack |
| `Riot_Stinger.wav` | Distinctive joyful half-second flourish, branded sonic signature |
| `Score_Tick.wav` | Soft incremental score tally at moderate pace |
| `Row_Rise.wav` | Low bubbling rumble + soft warning swell |
| `Game_Over.wav` | Friendly descending two-note chime, not frustrating |
| `Best_Score.wav` | Celebratory resolved sparkle with short tail |
| `Daily_Complete.wav` | Bright completion chime |
| `Ambient_Loop.wav` | Optional low-volume seamless 30–60 second mellow plucky ambient loop, **not** louder than SFX |

- Export uncompressed source `.wav` **44.1 kHz or 48 kHz, 16-bit PCM**; Unity import can compress on Android appropriately.
- Synthesise original SFX from layered tonal oscillators, filtered/decaying noise, frequency sweeps, short convolution-like tails, and ADSR envelopes; preview/waveform-check each sound. A small offline **Python audio-generation script** in `Tools/Audio/` is preferable. If dependencies unavailable, fall back to standard-library `wave`, `math`, `random`, `struct`.
- Avoid clipping, DC offset, excessive bass; peak <= -1 dBFS and aim for consistent perceived loudness. Volume groups: Master, Music, SFX (UI optional). Use 2D audio; no spatial audio.
- On cascades, pitch rises subtly per wave and music briefly ducks; cap simultaneous pops to prevent cacophony. Honor mute, phone audio focus, backgrounding and toggles.

## 6. VFX direction and budget

- **Drop:** 140–240ms fall with subtle elastic overshoot; first bounce 40–90ms; shadow squash.
- **Match pop:** 120ms squash/inflate anticipation, then 140ms outward stretch/fade; spawn **6–10** tiny splash/glint particles per clear group, not per cell on low devices.
- **Chain 2:** board accent flash 100ms, gentle UI pulse, text `CHAIN x2`.
- **Chain 3+:** `RIOT!` text (0.5–0.8s) with scale overshoot; colourful arc of particles, soft glow ring, eased camera/UI shake <= ~5 logical px / 120ms at full motion; frequency and accessibility limits.
- **Row rise:** board gently nudges up, amber progress accent, bubbling sound. Show warning at one move to rise.
- **Game over:** jelly board settles and darkens subtly; result panel springs in; no forced ad before showing result.
- **Performance:** Use object pooling for particles, floating labels and jelly visuals; avoid allocations during update, avoid unnecessary post-processing, no per-frame Instantiate/Destroy in bursts. 60 fps target on a mid-range Android phone; 30 fps acceptable fallback on older devices with stable input. Reduced-motion setting removes screen shake, heavy flash and large-scale movement.

---

# PART B — ENGINEERING AND QUALITY CONTRACT

## 7. Project organisation

```text
JellyRiot/
  AGENTS.md
  Docs/
    PROGRESS.md
    DESIGN.md
    TEST_MATRIX.md
    ART_BIBLE.md
    AUDIO_BIBLE.md
    RELEASE_CHECKLIST.md
    PRIVACY_DATA_INVENTORY.md
    ASO.md
    MARKETING.md
  Tools/
    Art/
    Audio/
    Marketing/
  Assets/_Project/
    Scenes/                 (Boot, Main; optional GameTest)
    Scripts/
      Core/                 (pure C# BoardModel, GameRules, RNG, MatchFinder)
      Gameplay/             (SessionController, GameState, PressureSystem)
      UI/
      Visuals/
      Audio/
      VFX/
      Ads/
      Analytics/
      Persistence/
      Platform/
      Editor/
    Tests/EditMode/
    Tests/PlayMode/
    Art/Source/
    Art/Sprites/
    Art/Materials/
    Audio/SFX/
    Audio/Music/
    Prefabs/
    Settings/               (ScriptableObjects)
    Resources/              (only if clearly justified)
  Builds/                    (gitignored)
  StoreAssets/
    Icon/
    FeatureGraphic/
    Screenshots/
    PromoVideos/
    Copy/
```

Architecture interfaces: `IGameRandom`, `IBoardRules`, `IGameEvents`, `ISaveRepository`, `IAdsService`, `IAnalyticsService`, `IHapticsService`, `IAudioService`, `IShareService`. Use interfaces when they make testing/swap-outs meaningful; do **not** build an overengineered dependency-injection framework.

### Engineering constraints for every Codex prompt

- Unity 6.3 LTS 2D; built-in render pipeline with sprites, Canvas and lightweight additive sprites **unless the already-created project uses 2D URP**. Do not migrate render pipelines merely for bloom. Maintain one render pipeline consistently.
- Portrait only, logical canvas baseline **1080×1920** with Canvas Scaler `Scale With Screen Size`, Match Width Or Height tuned for phone. Top/bottom safe-area padding for punch-hole/gesture UI; accommodate 19.5:9 and 16:9.
- `Assets/_Project/` for custom runtime work, including prefabs and generated assets. No scripts with arbitrary paths; no global project-wide destructive changes.
- Board rules/data cannot import UnityEngine. Only presentation layer creates transforms/sprites. Deterministic tests must run in EditMode.
- ScriptableObjects for **GameBalanceSettings**, **VisualSettings**, **AudioSettings**, **AdPolicySettings**; provide safe defaults and tooltips.
- Stable update loop; no coroutines that can race with a second move. Use cancellation/reset semantics when exiting scene or losing focus. All completion callbacks guarded once.
- Avoid deep inheritance; explicit state machine, null checks at boundaries, clear runtime assertion/logs; no Debug.Log spam in release.
- **Sane performance:** no per-cell physics Rigidbody2D; tween transforms for motion, no third-party tween package required; material/sprite sharing, pool VFX; portrait UI at 60 fps.
- Third-party packages must be compatible with the installed Unity version; only official trusted SDKs for ads, privacy and analytics, document versions.
- Never hardcode actual publisher keys, secret account credentials or fake store URLs. Development builds always use Google-provided test ad units/test devices.
- Any task must end with changed-files summary, how it was tested, remaining blockers, screenshots where supported, and the next work order suggestion **without automatically starting it**.

## 8. Quality gates

**G1 Core loop:** Valid moves, connected groups, gravity and pressure correct. 1,000+ seeded simulation games end deterministically without exception or infinite loops; EditMode suite passes.

**G2 Feel:** Within five seconds, first-time player can make a correct action; no dead-looking UI. Board and score fully visible at 360×800 logical-ish resolution and on common phone aspect ratios.

**G3 Polish:** Ten-second organic video contains an actual visible chain reaction and clear branded payoff. Audio/VFX do not clip or tank frame rate.

**G4 Reliability:** Fresh install, background/resume, kill/relaunch, offline use, low-memory resume, rapid taps, settings, daily-date rollover and interrupted ad callbacks never create a stuck state or duplicate reward.

**G5 Monetisation/compliance:** Ads only at allowed moments, never during active play; consent gating works; real ad IDs never used in testing; privacy and data declarations accurate; missing ad has graceful fallback.

**G6 Release:** No compiler errors, Android AAB meets current API target, signing configured securely by owner, store content honest, correct screenshots from actual game, license log, test track ready and external verification blockers listed.

---

# PART C — SEQUENTIAL CODEX WORK ORDERS

**Codex operation rule:** Paste **one entire work order at a time**. Each task refers to this spec as the authoritative design. If an implementation choice conflicts with this spec, follow the spec or identify the conflict explicitly. Steps marked with a human gate must be verified by the developer before continuing.

## Work Order 00 — Establish rules and audit the project (Day 1)

```text
You are implementing JELLY RIOT, a lean Android-first Unity 2D puzzle game. Read Jelly_Riot_Codex_Master_Plan.md, especially Product Bible, the Engineering Contract, and Quality Gates. First inspect the Unity project before changing files. Do not assume packages or Unity version. Create AGENTS.md containing project rules, Docs/PROGRESS.md, Docs/DESIGN.md, Docs/TEST_MATRIX.md, and the Assets/_Project folder structure described in the plan. Identify exactly what Unity Editor, Android SDK and package support is present. Keep source under Assets/_Project. Choose stable interfaces and an uncomplicated two-scene Boot/Main architecture. Set target frame rate to 60, portrait, safe-area UI policy, development/release separation, .gitignore for Library/Temp/Builds/user secrets. Create a minimal bootstrap scene if safe; do not break any existing project. Generate a short architecture diagram in Markdown. Record blockers requiring developer action. Do not claim to run Unity if it is unavailable. Deliver: paths created, architecture, tests/tooling verified, build blockers, next task readiness.
```

**Accept when:** Project opens, no import errors, folder hierarchy and AGENTS instructions exist; Unity version/repository confirmed; progress log created. **Owner action:** Choose/confirm final repository and available Unity Editor.

## Work Order 01 — Pure deterministic board simulation (Day 1)

```text
Read AGENTS.md and Jelly_Riot_Codex_Master_Plan.md. Implement pure C# grid state for a 7x9 board (row 0 top), enum JellyColour, immutable-ish MoveResult/event log, serialisable run snapshot, deterministic PRNG with serialisable state, GameBalanceSettings adapter, and clean unit-test hooks. Implement placement at the lowest empty row, rejection of full columns without consuming turns, complete board read access (read-only or copying), and Reset(seed). Initialise a visually interesting partial board in lower rows with no unintended immediate match and provide at least one sensible first-turn move, reproducible by seed. Do not implement art or ads. Keep BoardModel free of UnityEngine. Add EditMode tests for coordinates, edge columns, valid/invalid placement, no state changes after invalid moves, deterministic seeds, and bounds. Create a debug-only ASCII board printer. Report files and tests; if Unity unavailable, provide exact tests to run and say NOT RUN.
```

**Accept when:** Same seed yields same starting model and candidate stream; no invalid placement mutates model.

## Work Order 02 — Simultaneous match detection + stable gravity (Day 1)

```text
Read AGENTS.md and master plan. Implement orthogonal flood-fill of each same-colour connected component >=4; clear ALL qualifying components simultaneously per wave. After each wave, apply stable downward gravity within each column; repeat until no matches. Return a structured list of wave events: cleared cell positions/colours, falling cell from/to positions, wave index, cleared count. Never use rendered transforms to calculate gameplay. Prevent duplicate counting when groups touch. Include tests: horizontal 4, vertical 4, bent L, 5+, diagonals NOT connected, two groups clearing same wave, wave that creates second wave, stable column order, empty board and full board, no infinite recursion. Use iterative flood fill to avoid stack overflow. Animation presentation will consume events later. Preserve deterministic outputs. Report counts of tests and exact failures.
```

**Accept when:** Reference hand-created boards resolve exactly as expected, with no duplicates.

## Work Order 03 — Three-piece rack, actions and game state (Day 1)

```text
Read AGENTS.md and spec. Implement three visible upcoming-choice slots in the pure session model: select slot 0..2; choose a legal column; consume selected jelly exactly once; refill only selected slot after move completes. Define states Ready, Resolving, PendingLoss, GameOver, Paused, and sensible boot/menu transitions. A single session controller handles one input per turn; overlapping or double taps cannot make double moves. Expose observable semantic events for view/audio/analytics. A selected piece must remain selected after invalid column click. Add tests for all three slots, illegal indices, rapid duplicate action, events ordering, replay/reset. Provide an editor-only test controller if Unity UI is not ready yet. No visual Polish in this task.
```

**Accept when:** Exactly one candidate is consumed per valid turn, and no moves occur while resolving.

## Work Order 04 — Score, cascade feedback metadata and bests (Day 1)

```text
Read AGENTS.md and plan. Implement scoring: for each wave, points = clearedCellCount*10*waveIndex. Record current score, longest chain, best endless score, and a list of presentation metadata (not VFX implementation): Chain2, Riot3+, LargeGroup8+, PersonalBest. Score is calculated entirely in Core and cannot be affected by animation speed. Update final score before result state. Test 4-pop=40, 5-pop wave2=100, mixed simultaneous groups, multiple waves, overflow safety (long where appropriate), score resetting between runs, Daily and Endless results isolated. Add events for future animation and analytics, not hardcoded TMPro effects.
```

**Accept when:** Test-calculated scores agree for the same seed/moves; no animation-driven points.

## Work Order 05 — Pressure rises, difficulty and game over (Day 1)

```text
Read AGENTS.md and plan. Implement deterministic periodic row pressure. First rise after six valid placements, then period six / five after 20 turns / four after 45 (configurable GameBalanceSettings values). Each new bottom row has 3–5 filled coloured cells, 2–4 holes and no obviously impossible fixed repeated pattern. On a rise, atomically shift old cells toward row 0 and place new cells in row 8; if ANY cell would shift above row 0, preserve pre-rise settled board and enter PendingLoss without discarding anything. Also detect all-columns-full no-legal-move. Resolve post-rise matches using the same clear/gravity pipeline and score accordingly. Countdown shown as numeric next-row-in-N, generated from Core. Add tests for cadence transitions, row bounds, blocked shift, auto matches on rise, deterministic row, no silent loss, continue-ready checkpoint. No ad integration yet.
```

**Accept when:** The board rises predictably, warning can be displayed, losses can be reproduced exactly.

## Work Order 06 — Fair seeding and balance harness (Day 1)

```text
Read AGENTS.md and plan. Implement an optional deterministic weighted rack/row generator that avoids extreme colour streaks without secretly targeting ads or time played. Keep one seedable generator per run and save its state. Expose balance parameters as ScriptableObject, including initial colours, when extra colours activate, row density, pressure intervals, and retryable initial-board sanity criteria. Create fast headless/EditMode simulation harness able to run at least 1,000 seeds with an explicitly documented basic policy (e.g. choose a legal rack/column move at random) and output mean/median turn count, early-death frequency, cascade rates, reproducibility, and max-turn safety cutoff. Do not assert that random policy represents human skill. Tune only catastrophic problems (e.g., most games dying before four moves). Write Docs/BALANCE_RESULTS.md with actual results or NOT RUN.
```

**Accept when:** The simulator is deterministic and produces actionable stats; no frozen/infinite run.

## Work Order 07 — Unit tests and core hardening (Day 1) — GATE G1

```text
Read AGENTS.md and all Core changes. Add a comprehensive EditMode regression test suite and a deliberately minimal editor test harness, covering state machine transitions, high score separation, serialisation round-trip, seeded daily runs, pressure pending-loss preservation, replay/reset, invalid input, 1000 random deterministic simulations and replay equivalence for a list of moves. Fix actual bugs found; do not hide failures or alter tests to fit incorrect behaviour. Add a deterministic golden test case containing seed, moves and expected board/score after each turn. Provide a one-page G1 QA report with exact test result summary, evidence, failed checks and remaining risk. Do not proceed into ads/UI here.
```

**Accept when:** G1 tests pass in an actual runner. **Human gate:** Review results. If runner unavailable, gate remains NOT VERIFIED.

## Work Order 08 — Portrait layout and functional screens (Day 2)

```text
Read AGENTS.md, art direction and engineering contract. Build Unity Main scene using Canvas/UGUI (TextMeshPro only if present/imported correctly) with Boot, Home, Gameplay, Pause/Settings, GameOver/Results and Daily screen states. Create reusable safe-area anchoring, Canvas Scaler target 1080x1920, readable score and next-rise countdown, three selectable rack slots, 7x9 grid fitted to width, clear highlights for legal columns, current score, personal best, Replay/Home and unobtrusive pause. Implement full input wiring to Core with visual placeholders only temporarily; no duplicate click events, no invisible button overlays. Layout must work on 360x800, 1080x1920 and tall 1080x2400 test resolutions, with touch targets roughly 48dp or larger when practical. Support hardware Android Back through unambiguous navigation. Record screenshot or capture instructions. Do not add ads yet.
```

**Accept when:** Complete game playable with functional UI, layout no clipping, tapping piece then column works.

## Work Order 09 — Build the original reusable jelly sprite set (Day 2)

```text
Read AGENTS.md and art bible. Generate ORIGINAL 256x256 RGBA sprites for five glossy rounded-square jelly characters (Coral, Lemon, Aqua, Lilac, Mint) using deterministic procedural artwork. Use Python Pillow under Tools/Art if available; if not, Unity Editor Texture2D PNG generation with no external paid assets. Each jelly must have rounded silhouette, darker bottom edge, translucent/gradient-ish internal shading, off-centre glossy broad highlight, thin rim light, separate shadow underlay and optional cute tiny eye overlay. Ensure perceived brightness/shape remains consistent across palette; do not simply output flat discs, circles or squares. Create two face expressions and subtle small highlights if time permits. Make sprites properly importable in Unity (RGBA alpha, Pixels Per Unit standard, sensible filtering, mobile compression). Implement a gallery preview scene/editor screenshot helper displaying all five on light and dark backgrounds and at real grid size. Provide exact generated source code, actual PNG files, and dimensions. Report visual limitations honestly, do not claim photo-quality art.
```

**Accept when:** Sprites exist, import without pink/error, look polished and distinguishable on phone at ~40–60 px.

## Work Order 10 — Brand/UI art, original logo and icon components (Day 2)

```text
Read AGENTS.md and master art direction. Create polished reusable UI art: inset board tray/frame, cell hole, rounded pill buttons (nine-slice), score/result panel, rack slots, icons for pause/sound/haptics/replay/home/daily/info, selection arrow/glow, gentle background gradient/decorative bubbles. Generate original JELLY RIOT wordmark graphic and a square mascot-based icon COMPONENT suitable for later 512x512 store-export work. Prioritise legibility and high-quality hierarchy: title > score > board > rack > actions. Use the procedural sprite pipeline and retain editable source, export lossless assets. Where quality cannot be achieved procedurally, create specific files in Docs/ART_REQUESTS.md giving high-quality image-generation prompts, required transparency and sizes; never silently use generic clip art. Apply the best available original assets to Main scene and remove visible grey placeholders. Test at low phone resolution.
```

**Accept when:** Branding looks coherent, title legible at thumbnail size, functional UI is art-directed.

## Work Order 11 — Finished visual board and motion-safe binding (Day 2)

```text
Read AGENTS.md and plan. Replace placeholder board elements with pooled SpriteRenderer/UI.Image jelly visuals and cell sprites, keeping Core board completely separate. Create a GridViewPresenter that consumes logical snapshots + move event lists and guarantees visual-model sync after every action, restart, game over and resume. Falling pieces animate independently; resolve view by replaying move events without mutating Core. Add highlights for selected rack piece and hovered/touched target column, gentle invalid-column feedback, and correct pressure-row UI. Handle rapid taps, focus loss and skipped animations: final board must snap to authoritative model without ghost jelly objects. Create a visual-state debug overlay only in Development builds. Avoid physics bodies. Report animated move tests and memory allocations if available.
```

**Accept when:** No wrong jelly colour/position after 100 rapid moves; source of truth remains BoardModel.

## Work Order 13 — Jelly feel: drop/land/squash animations (Day 3)

```text
Read AGENTS.md and full VFX direction. Implement fast polished jelly animations: drop from rack/above board to target (~140–240 ms configurable by travel distance), easing acceleration, cushioned land, 45–90 ms squash/stretch with overshoot, optional tiny eye blink; retained highlights and soft shadow squash. Follow deterministic event queue and skip-to-end on scene changes or reduced motion. Keep cell position aligned during all tweens, no unexpected visual collisions. Ensure animations are well timed on 30 and 60 fps and use unscaled time only where deliberate. Pool animated jelly views. Add a developer slow-motion toggle (Development build only). No invasive third-party tween package. Verify 30 consecutive turns, rapid input and screen pause.
```

**Accept when:** Drops read as gummy and snappy rather than falling squares; never lag or drift.

## Work Order 14 — Pop/cascade/RIOT VFX and animation (Day 3)

```text
Read AGENTS.md and plan. Create high-quality lightweight 2D VFX pipeline for match pop, gravity cascade, chain text, score fly-up and distinctive RIOT! event at cascade wave >=3. Use pooled sprite particles/trails/glints and original generated splash sprites, not hundreds of spawned GameObjects. Pop: ~120 ms anticipation then ~140 ms radial splash, particles 6–10 per group and a clean sound/VFX trigger. Wave 2: gentle board accent flash and CHAIN x2 label; wave 3+: 0.5–0.8s large RIOT! display, soft non-strobe glow, a tiny controlled shake (<=5 logical px), spark arcs. Add row-rise animation and one-move warning. VFX do not alter score/model. Implement Reduced Motion path removing shakes/flashes with an equally informative static text. Test cascade of 50+ cells on a midrange phone or profiler; respect limits. Provide demonstration scene that triggers each effect on demand.
```

**Accept when:** A real recorded 10s chain clip looks exciting and remains readable, even muted.

## Work Order 15 — Original audio asset production and mixing (Day 3)

```text
Read AGENTS.md and Audio Bible. Produce ORIGINAL game audio assets, not placeholders: UI_Tap_01..03, UI_Invalid, Jelly_Drop_01..04, Jelly_Land_01..03, Jelly_Pop_01..05, Cascade_02..05, Riot_Stinger, Score_Tick, Row_Rise, Game_Over, Best_Score, Daily_Complete; optionally subtle Ambient_Loop (30–60 sec seamless) only if polished. Write reproducible Tools/Audio/generate_audio.py using Python standard library wave/math/struct/random (+ NumPy only if available) synthesising layered oscillators, filtered noise, envelopes, frequency sweeps and gentle tails. All 16-bit PCM 44.1/48kHz WAV. Each clip must vary with intentional sonic role; avoid ugly basic computer bleeps. Measure/print peak, duration and RMS, remove DC, prevent clipping <= -1dBFS. For audio difficult to synthesise convincingly, write explicit replacement sound briefs rather than claiming final quality. Import files into Unity with suitable 2D/memory/compression settings and organise categories. Include Tools/Audio/README for editing/rebuilding sounds and asset licensing (original procedural). Do not download/licence unspecified samples.
```

**Accept when:** Every promised required audio file exists and plays, no clipping; human listens via phone speakers and reviews quality.

## Work Order 16 — Audio manager, music ducking, haptics (Day 3)

```text
Read AGENTS.md and Audio Bible. Implement central AudioManager with adjustable Master/SFX/Music levels, persistent mute, pooled AudioSources (limit simultaneous voices), randomised pop/drop variants, pitch that subtly ascends each chain wave, smart scoring sound throttling and audio-to-animation timing. Background/ads/pause should pause/duck appropriately without leaving music muted after resume. Implement Android haptics abstraction with safe platform capability checks and limited use for drops, match and RIOT, plus a haptics toggle. Avoid intrusive vibration every cell, any forced OS setting changes, and audio focus conflict. Ensure AudioManager accepts Core events exactly once, never duplicates sounds on replay/restore, and no sound on a failed placement except gentle negative feedback. Implement PlayMode test harness for mute/toggles and manual test checklist.
```

**Accept when:** Sounds are cohesive, enjoyable on phone speaker at low level, mute and focus handling work.

## Work Order 17 — Performance pass for juice layer (Day 3) — GATE G3

```text
Read AGENTS.md and plan. Profile the jelly scene during the strongest possible chain reaction; verify or instrument allocations, active objects, canvas rebuilds, sprite batches, texture memory and frame spikes. Pool effects/text, remove per-frame allocations, cap splash count on low devices, avoid expensive full-screen shader passes, and ensure reduced motion is visually coherent. Provide a debug Combo Showcase mode (development builds only) that can trigger 1/2/3/5-wave examples without touching normal runs, so marketing capture is easy and always honest about showcase testing. Record FPS or explicit 'needs device measurement', screenshots/video capture instructions and failing cases. Preserve appearance where possible. Do not tune by unsubstantiated claim that performance is 60fps without measuring.
```

**Accept when:** G3 visual/audio demo verified on device, no hitch that destroys the combo moment.

## Work Order 18 — UX, interruption handling and optional settings (Day 4)

```text
Read AGENTS.md and product bible. Polish Home, Pause, Settings, Result and Daily screens. Home presents a single large Play button, one Daily button, one small Settings action, concise score teaser and visible branding. Results present exact score, best, top combo and immediate Replay, while ad-based Continue is optional/conditional. Settings: SFX, music, haptics, reduced motion, colour-blind markers, tutorial replay and Privacy Choices entry where required. Add pause/resume, Android Back handling, app background/foreground, low-memory resume and scene-reset cleanup. The app must never interpret the first tap after an ad as a gameplay move or place interactive controls beneath native ad overlays. Persist settings. Do not clutter UI with monetisation prompts. Build screenshot checkpoints for each screen and validate safe-area behaviour.
```

**Accept when:** All screens navigable, no accidental ads/taps, accessibility settings work instantly.

## Work Order 12 — First-time onboarding and microcopy (owner-deferred until after Work Order 18) — GATE G2

> **Owner scheduling decision — 19 October 2026:** Work Order 12 was explicitly deferred so visual
> animation work could proceed first. Execute this work order immediately after Work Order 18 and
> before Work Order 19. The tutorial scope and acceptance gate remain unchanged.

```text
Read AGENTS.md and plan. Implement a genuine play-through tutorial with maximum three short callouts, timed by player action rather than modal wall of text: (1) Select a jelly, (2) Tap a column to drop, (3) Match 4+ and trigger chains. For the very first run only, use a stable fixed approachable seed so the player's first valid guided move creates an obvious match within 1–2 moves (if practical); never simulate success or fake input. After success, release tutorial constraints and begin normal endless play. Persist tutorial completion; allow Skip and Replay Tutorial from Settings. Add understated help tooltip explaining pressure row. Use friendly concise Australian English; no lengthy paragraphs. Add optional manual screenshot sequence for reviewers. Verify new install first 15 seconds, replay tutorial, skip, and restore. Do not spam popups.
```

**Accept when:** A non-gamer can identify what to tap and clear a group, no ads or account screens block first play. **Human gate:** Ask someone unfamiliar to play without verbal help.

## Work Order 19 — Daily Challenge implementation (Day 4)

```text
Read AGENTS.md and Daily Challenge spec. Implement daily deterministic seed from UTC yyyy-MM-dd, fixedSalt and rulesetVersion using stable algorithm (not .NET GetHashCode). Daily run uses identical board/rack/pressure across installations of same build. Keep daily scores separate from Endless best. One official scored attempt per date and unlimited clearly labelled unscored Practice retries. No continues or rewarded benefits in Daily. Support complete offline gameplay, next-day rollover on re-entry, clock changes without crashes, and persistence across app restart. Provide native Android share intent for plain-text result if straightforward; fallback Copy to Clipboard plus unobtrusive toast; no invented Play Store URL. Tests must validate matching seeds on two mock devices, same dates, next day change, timezone independence and rulesetVersion change. Explain that no server means scores cannot be globally verified.
```

**Accept when:** Two installs with same daily seed/moves yield exact outcomes, no ad advantage.

## Work Order 20 — Save snapshots, replay and crash recovery (Day 4)

```text
Read AGENTS.md and plan. Implement local versioned JSON save repository for settings, best scores, current Endless run, Daily result, tutorial state and ad-continue flag. Store a settled, authoritative board snapshot, rack, RNG state, score, turn, row countdown, selected slot, pending loss and rulesetVersion; only commit after coherent state transitions or pause, never halfway through visual tween. Use temp-file then replace/rename when supported; keep last-known-good backup; handle malformed saves by isolating bad data and creating a fresh safe state with helpful logs in Development. On resume, rebuild visuals from Core model and skip stale animations; prevent duplicate event rewards. Create tests for migration from missing fields, corrupted files, reinstall/no file, save round trip, Android background kill, daily rollover and retry. Never persist third-party personal advertising data in this save.
```

**Accept when:** App killed mid-animation restarts into a coherent state; scores and settings preserved.

## Work Order 21 — Accessibility, small-phone UX and feedback polish (Day 4) — GATE G4

```text
Read AGENTS.md and product bible. Add unique colour-blind symbols for each jelly (e.g., circle/triangle/star/diamond/stripes overlay), toggle in Settings and apply uniformly to rack, board and previews. Provide high-contrast labels, non-colour-only affordances, readable text, touch areas, reduced motion and haptics/audio toggles. Validate common Android narrow and tall aspect ratios, dynamic system font-size changes if feasible, gesture insets, and physical device touch. First-time onboarding must work with audio disabled. Fix concrete bugs from rapid-tap, resume, repeated restart, screen rotate attempt (portrait lock), Back button and muted audio scenarios. Record accessibility and G4 sign-off report. Do not exaggerate formal WCAG certification.
```

**Accept when:** Major interactions remain understandable without hearing sounds or distinguishing colours.

## Work Order 22 — Privacy-conscious analytics and commercial measurement (Day 5)

```text
Read AGENTS.md and business KPI definitions in master plan. Add IAnalyticsService, NoOpAnalyticsService and an officially supported analytics implementation if SDK can be installed and verified (prefer Firebase Analytics; document exact official integration and dependency/version). Instrument only necessary events: first_open/session_start where available, tutorial_started/completed/skipped, run_started(mode), valid_move, first_pop, run_ended(mode, score, turns, duration, max_chain, continued), daily_started/completed, rewarded_offered/requested/granted/failed, interstitial_opportunity/shown, settings_accessibility_changed, app_version. Avoid logging every individual jelly tap to remote analytics; sample/aggregate as appropriate. No PII, no personal device identifiers in custom payloads, no secret tokens or fake IDs. Respect SDK/region consent requirements; document user data inventory and data deletion options. If remote SDK unavailable or cannot be configured by owner, keep NoOp implementation with an honest integration checklist, compile cleanly, and mark remote retention measurement BLOCKED. Add DEVELOPMENT-only event debug console. Define funnel dashboards/report spreadsheet queries in Docs/KPI_PLAN.md; do not fabricate data.
```

**Accept when:** Events use exact stable names/schemas, no double counting; real backend verification or BLOCKED clearly recorded.

## Work Order 23 — Ad policy engine, mocked services and rewarded continue (Day 5)

```text
Read AGENTS.md, consent/ad rules, and official AdMob Unity docs. Build IAdsService interface with MockAdsService for Editor/dev and a safely disabled RealAdsService adapter until official SDK is installed. Implement AdPolicySettings ScriptableObject defaults: no interstitial for first 2 completed runs; minimum 180 seconds since previous interstitial; max 3/session and 6 per UTC day; show only after result has settled and player initiates Replay/Home transition or at a stable results-to-menu break; never over results buttons, gameplay, tutorials, initial launch, pause/resume or a mid-run checkpoint. Rewarded: at most one Continue per Endless run, explicitly initiated by player, only if preloaded and available, reward applied only on reward-earned callback and exactly once. Implement robust mock scenarios success, skipped, failed load, ad opened then app backgrounds, duplicate callbacks, no-fill/offline; never soft-lock results. Make ad policy auditable by tests; record 'why shown/why suppressed' in dev log. Ad frequency values are initial conservative defaults subject to retention testing, not guaranteed optimal. No deceptive placements or forced engagement.
```

**Accept when:** Extensive mock tests pass and player can play endlessly without ads or network.

## Work Order 24 — Official AdMob SDK, UMP consent and app-ads readiness (Day 5)

```text
Read AGENTS.md and official Google Mobile Ads Unity Plugin, Rewarded, Interstitial, UMP privacy and Google Play ad policy documentation linked in master plan. Integrate officially supported Google Mobile Ads Unity plugin into this Unity version using a documented reproducible approach; include Android External Dependency Manager setup if required. Wire RealAdsService to actual SDK while keeping Editor MockAdsService. Use only Google's official TEST ad unit IDs and configured test devices during development; real IDs must be entered/configured by owner before a genuinely publishable release. Call UMP consent information Update on each app launch; display form when required, expose Privacy Options in Settings when required, gate ad requests on CanRequestAds according to current SDK guidance. Initialize SDK once, load/preload rewarded/interstitial, handle all callbacks and ad object cleanup, appropriate retry backoff, foreground/background audio handling and Unity main-thread dispatch. On reward callback apply a single verified continue; do not grant on closed. Do not silently suppress consent, request ads prematurely, or ship sample test ad units as production monetisation. Capture SDK version, permissions, data collection and manual setup steps in Docs/PRIVACY_DATA_INVENTORY.md. If plugin import not possible in current environment, create no compile-breaking Google namespaces: keep optional integration behind tested assembly definition/conditional compile and mark BLOCKED clearly, with import instructions.
```

**Accept when:** Official test ads and consent flow verified on real Android device, or genuinely BLOCKED (not faked). **Owner action:** Configure AdMob account, application and ad units, UMP messages, privacy policies, test devices.

## Work Order 25 — Android configuration and build pipeline (Day 5)

```text
Read AGENTS.md and Google Play current requirements. Configure Unity project for Android portrait, ARM64, IL2CPP, AAB, current Android 16 target SDK API 36 or later required by Google Play for new apps submitted October 2026, and pragmatic min SDK compatible with the currently integrated plugin (verify official minimum and SDK/Unity support; do not guess). Use proper application identifier placeholder under owner's namespace until confirmed, readable versionName and incremental versionCode, secure release keystore flow (never check private key or passwords into source). Configure appropriate icon, adaptive icon if possible, splash, safe area, Android back and network permissions only when required by SDKs. Add an Editor build script for repeatable Development APK and Release AAB generation with preflight validations and build manifest (Unity version, SDK target, git commit, scenes, ad config mode). If API36 unsupported by installed Unity SDK, report precise upgrade path; never secretly downgrade compliance. Test clean install, package size, startup, Android logs, and AAB export; record what was actually run.
```

**Accept when:** AAB exists and meets required API/platform constraints. **Owner action:** Signing details and publishing account access; protect credentials.

## Work Order 26 — Compliance pack, publisher website and app-ads.txt (Day 6)

```text
Read AGENTS.md, master references and actual integrated SDK package versions. Create a production-ready compliance pack in Docs/ and StoreAssets/: privacy policy DRAFT with accurately described data types/purposes and contact URL placeholders, terms/simple support page, a full SDK data inventory, Play Console Data safety questionnaire worksheet, ad declaration worksheet, target-audience review, advertising consent test plan, asset/voice/font licence attribution ledger, and a checklist for age-appropriate ratings. Include a simple static developer website ready to host, with actual app-ads.txt FILE TEMPLATE requiring the owner's real AdMob publisher ID; never put fabricated pub IDs as if production. Developer website must be entered in Play store listing so AdMob can verify; document app-ads.txt publication, crawl and review steps. Check no children's-directed claims, misrepresentation, misleading subscription/purchase designs or misleading screenshots. Clearly flag final privacy statements and legal accuracy for owner review, not auto-approve them. No network upload unless explicitly authorised and possible.
```

**Accept when:** Developer can fill Play Console forms with evidence; unresolved/owner-sensitive entries are marked and not invented.

## Work Order 27 — Full regression testing and crash-proofing (Day 6)

```text
Read AGENTS.md and TEST_MATRIX. Execute/prepare comprehensive EditMode, PlayMode and Android smoke tests: new install/old save, tutorial, rack, rows, collision/full column, large chains, rapid taps, pause, background/resume, exit mid-animation, orientation attempt, bad save, offline startup, denied/unknown consent, no-fill, invalid rewarded callback, restored result panel, audio focus, colour-blind symbols, daily timezone boundary, device low-memory, performance, 1000 seeded runs. Make a test case matrix with ID, setup, action, expected, actual, evidence and PASS/FAIL/NOT RUN. Fix bugs within existing scope only, prioritising crashes, reward duplication, data loss and input lock-ups. Include `adb logcat` instructions if emulator/phone not accessible, and a scripted deterministic smoke tour. Do not report PASS for anything not executed.
```

**Accept when:** G4/G5 verified or blockers openly documented, no known P0/P1 issue.

## Work Order 28 — Visual optimisation, screenshots and store icon (Day 6)

```text
Read AGENTS.md, brand direction and Play Store graphic requirements. Produce final original store assets: Google Play app icon PNG 512x512 (<1MB, alpha as permitted by rules); feature graphic JPEG or opaque PNG 1024x500; 6 genuine Android portrait screenshots from gameplay/menu/daily/RIOT moment; supporting captions clearly separated as editorial overlays only where store guidelines permit. Store screenshots must actually reflect achievable in-app gameplay and truthful UI; do not fabricate higher scores or fake game modes. Produce an asset index with dimensions, colour space, file type, purpose and build/version. Write review checklist for small thumbnail recognisability, colour contrast, screenshot sequence, possible trademark clashes and localisation. If no actual device capture is available, export ONLY mockups marked PREVIEW; leave real screenshot requirement NOT COMPLETE. Use existing generated brand assets rather than random unrelated stock art.
```

**Accept when:** Files are real, correct size, visually distinctive, accurately represent the playable build.

## Work Order 29 — Store listing + ASO copy (Day 6) — GATE G5

```text
Read AGENTS.md, game and store screenshots. Create StoreAssets/Copy/GooglePlay_EN_AU.md with a readable title <=30 characters (validate current limit), a concise short description <=80 characters, long description <=4000 characters, primary feature bullets, plausible relevant search terminology (colour sort / jelly puzzle / match / chain reaction / offline) without keyword stuffing, an honest clear 'contains ads' proposition and privacy links placeholders. Write alt text/caption plan and metadata suggestions. Suggest 2 alternative truthful store-positioning angles for later tests: 'satisfying endless match' and 'daily colour puzzle'. Do not assert rankings, fake downloads, “No. 1”, celebrity endorsements, or unsupported awards. Provide a production completeness checklist and all owner-required inputs. Confirm assets/screenshots match implemented features. Include app review responses for common bugs in friendly Australian English without asking for fake reviews.
```

**Accept when:** Listing is persuasive, concise, honest and ready to paste with owner-controlled details; no fake claims.

## Work Order 30 — Marketing capture and organic creative kit (Day 7)

```text
Read AGENTS.md, marketing plan and available build. Set up reproducible, truthful gameplay capture procedure at 1080x1920 portrait (Android recording or Unity Recorder if installed; no fake gameplay). Build six short-video scripts each with 0–2s hook, 2–8s setup, 8–14s authentic payout, 14–17s branded CTA. Ideas: 'Where would you drop?', 'One move clears 30 jellies', 'I thought I was done', 'Watch the combo multiply', 'Can you beat today's challenge?', 'From almost losing to RIOT!'. Create title cards and captions suitable for TikTok/YouTube Shorts/Reels (without UI overlays obscuring game). Use Combo Showcase test seeds only to record POSSIBLE actual live gameplay sequences: no fabricated score, no composited false features. Write 30-day posting/experimentation calendar, UTMs/attribution link strategy after real store URL exists, and low-effort account workflow. Generate preview assets if tools support it; if recording/rendering tools are missing, produce exact scripts, shot lists, seeds and editor capture commands rather than claiming MP4s exist. Document image/music licensing for promotional assets.
```

**Accept when:** At least six shootable storyboards, 3–5 real representative clips if environment permits, reliable hand-off to owner.

## Work Order 31 — Test-track submission kit and launch tasks (Day 7)

```text
Read AGENTS.md and current Google Play testing requirements. Prepare final release folder with signed-by-owner AAB (or an unsigned AAB marked REQUIRES SIGNING), store asset manifest, copy, changelog, privacy URL checklist, developer website/app-ads checklist, ad test-to-production switch checklist, accessibility/QA reports, versionCode and target API confirmation. Explain Google Play testing procedure exactly: internal smoke test first; new personal accounts created after Nov 13 2023 generally need >=12 closed-test testers opted in continuously for >=14 days before production-access application, followed by review/approval. Do not claim app publicly released during week 1. Prepare an onboarding message for testers asking for honest feedback and basic problem reports, not incentivised five-star reviews. Do not perform live publication, send invitations or enter private account credentials without explicit developer authorisation; record all steps that remain manual. Include instructions for AAB upload and policy declarations.
```

**Accept when:** Owner can submit to internal/closed track without asking Codex to rediscover project details.

## Work Order 32 — Final audit, hand-off and post-launch experiment backlog (Day 7) — GATE G6

```text
Read AGENTS.md, all master plan gates and Docs/PROGRESS.md. Conduct a final evidence-based audit comparing shipped features to MVP, tests to acceptance gates, generated art/audio asset counts, actual Android build outputs, ad/consent verification, store assets, privacy/developer website, and marketing files. Build a numbered RELEASE_BLOCKERS list with P0/P1/P2 and identify anything unimplemented, untested or only mocked. Produce Docs/FINAL_HANDOFF.md with: exactly which version is ready, build and signing status, all feature flags, required owner configuration, step-by-step Play Console and AdMob sequence, expected test procedure, playtime/retention/ad metrics to watch, screenshots and file inventory. Create no new gameplay mechanics. Commit final code only if repository permissions/environment permit and don't overwrite owner changes. Never declare finished unless every claimed gate has evidence. Propose a tiny week-2 backlog ranked by expected value, effort, and risk.
```

**Accept when:** Developer gets an honest complete inventory and testable build, rather than a vague “done” message.

---

# PART D — ARTWORK HAND-OFF PROMPTS AND VERIFICATION

## 9. ChatGPT art creation queue

Use these individual prompts when asking ChatGPT for an *actual generated image* in a later turn. Codex's procedural artwork path must ship even if premium generated images are not yet ready. All assets must be original, commercially usable under applicable terms, and inspected before use. **Important:** Artwork generation produces raster images, not layered sprites with guaranteed perfect alpha; Codex may need to isolate assets or re-create them procedurally if transparency is unreliable.

### A01. Character style sheet — five colours

**Image prompt:** “Create a polished 2D mobile game character asset reference sheet for an original puzzle game called Jelly Riot. Show five adorable, glossy, soft, squishy rounded-square jelly blobs in coral pink, lemon yellow, aqua, lilac, and mint. Consistent silhouette and scale; translucent body, thick wet shine, subtle rim lighting, bright broad highlight, soft underside shadow, minimal but expressive eyes. Premium candy-like mobile casual puzzle art, readable on a tiny phone grid. Arrange as evenly spaced sprite references on a plain neutral background. No words, watermarks, other objects, logos or interface.”

**Acceptance:** Every jelly has same visual mass, high colour contrast, no unintended hands/limbs or over-detailed face; clean shapes survive shrinking to 48px.

### A02. Expression overlays

**Image prompt:** “Create a tidy reference sheet for tiny jelly mascot face expressions only: neutral-happy, surprised, smug, delighted, sleepy, dramatic RIOT excitement. Each consists of simplified two eyes and small mouth suitable for 48px mobile sprite overlays. Clean white-on-black or dark ink, consistent proportions, no characters, no typography, no background texture.”

**Acceptance:** Can be translated to separate icon overlay; not required to bake expressions into each cell sprite.

### A03. Game visual key art / in-game UI guide

**Image prompt:** “Professional mobile casual puzzle game portrait UI key art for original game JELLY RIOT, 1080x1920. Premium vivid jelly blobs in a seven-column nine-row indigo puzzle tray, three jelly choices in large tray at bottom, clear score and next-row indicator above, uncluttered controls, beautifully readable spacing for a real Android phone safe area. Soft night-plum background with subtle sparkles, bright candy highlights, a joyful ‘RIOT!’ chain reaction with tasteful splash arcs. This is an implementable art-direction reference, not gameplay source of truth. No advertising panels, no fake app-store badges.”

**Acceptance:** Board topology and hierarchy actually match the 7x9 specification, not an impossible AI-drawn grid. Never present this concept as a real game screenshot.

### A04. Logo and standalone store icon concept

**Image prompt:** “Create an original bold commercial-quality wordmark for JELLY RIOT in fat rounded cream-to-peach lettering with playful jelly-like highlights and a coral mascot splashing small colourful jelly droplets, suitable for a casual mobile puzzle game. Strong silhouette, recognisable at small sizes, dark plum backdrop, no reference to existing game brands, no platform badges, no secondary text.”

**Acceptance:** Lettering exactly spelled JELLY RIOT, not garbled. Correct manually in SVG/Unity typography if image model misses letters; icon must not depend on tiny text.

### A05. Background and UI surface materials

**Image prompt:** “Produce a texture/material reference sheet for a premium jelly-themed mobile puzzle interface: indigo-plum soft gradient, pill-shaped cream button, translucent indigo UI panels, inset soft cell holes, faint bubble decoration, warm white score typography treatment. Minimal clean visuals, rounded shapes, readable, not overly glossy or noisy, no real screenshots or watermarks.”

**Acceptance:** Use individual clean assets or implement shapes with Unity UI; avoid baked-in typography that cannot be edited.

### A06. Splash VFX spritesheet reference

**Image prompt:** “Reference sheet of original stylised 2D game effects for glossy jelly popping: tiny six-point sparkle, juicy splash, ring ripple, drifting blob fragments, soft translucent glint, celebratory burst. Isolated individual elements, consistent mobile game visual language, intended for particle animation, no full scene, no text, on contrasting flat background.”

**Acceptance:** Codex extracts or remakes individual sprites; no giant full-screen texture as the only VFX solution.

## 10. Artwork import and replacement workflow

1. Codex exports a gallery screen and current low-res device screenshot.
2. Developer/ChatGPT reviews for readability, polish, recognisable brand and mobile legibility.
3. If premium art generated, place source files in `Art/Source/ExternalProposals/` with origin and licence note; **do not replace live sprites until tested**.
4. Codex reworks image to transparent RGBA, trims empty bounds consistently, checks alpha fringing, scales, names and imports; reuses materials and batching.
5. Re-run G2/G3 visual QA and low-end FPS capture. Keep procedural sprites as fallback for reliability.
6. Store marketing graphics are separate final exports; never pass concept art off as captured gameplay.

---

# PART E — AUDIO WORK AND REVIEW

## 11. Audio generation acceptance and human review

After Work Order 15, listen to each clip on **phone speakers** and headphones. Rate `1–5` for clarity, squishiness, distinctiveness, annoyance on repetition and clipping. Any sound under 3 in either clarity or repeatability must be revised before G3. **Especially important:** audio synthesised by simple wave oscillators can sound like arcade beeps rather than high-quality squishy jelly, so Codex's generated files must be judged, not automatically called polished.

Suggested sound construction recipes (starting points, not mandatory DSP formulas):

- **Drop:** 80–180Hz to 160–240Hz quick pitch bend, mellow body, filtered short liquid noise, 150–230ms envelope.
- **Pop:** 180–450Hz short upward blip plus high-frequency splash transient, 100–240ms, 20ms attack smear to avoid click, optional tiny reverb tail.
- **Cascade:** simple ascending pentatonic notes with 2–3 soft plucks at controlled loudness; ensure chord doesn't clip.
- **RIOT:** a distinctive three-note rising stinger mixed with gummy pop and sparkle, ~0.5–0.8 seconds, playful and not jarring.
- **Row rise:** muted 60–180Hz bubbling glide and tiny bubbles, short caution cue rather than stressful alarm.
- **Game over:** two soft descending marimba-like notes, <1.2 seconds, nonpunitive.
- **Ambient loop:** 50-second sparse marimba/synth bed with gentle low pulse and no recognisable copyrighted melody; use cross-fade/seamless loop, easily muteable.

### Audio-specific integration checks

- SFX do not accumulate dozens of sources when a large pop triggers.
- Audio output never continues under a full-screen ad unexpectedly; resumes properly.
- Backgrounding does not produce a loud burst of queued sounds on return.
- No autoplay music before consent or tutorial that traps the user (audio content itself doesn't require ad consent, but app should respect settings).
- Volume toggles save through restart; music defaults subtle (start around 20–30% of UI SFX perceived loudness).
- A user who is sensitive to audio can play silently with no lost gameplay information.

---

# PART F — USER ACQUISITION, MONETISATION, RETENTION

## 12. Revenue maths (A$)

**Formula:** `monthly gross ad revenue ~= average DAU × average paid ad impressions per DAU per day × blended eCPM (AUD) × 30 / 1000`.

*Example only: 2 impressions / DAU / day, A$5 blended eCPM*:

| Average daily active users | Illustrative monthly gross ad revenue |
|---:|---:|
| 100 | A$30 |
| 500 | A$150 |
| 1,000 | A$300 |
| 1,667 | A$500 |
| 3,000 | A$900 |
| 6,667 | A$2,000 |

**Cautions:** eCPM and filled impressions vary by country, ad type, season, age settings, consent and demand. DAU are *daily returners*, not cumulative installs. These numbers exclude tax, developer time, third-party fees and paid acquisition. With modest retention, getting >1,000 DAU organically may require sustained installs and continuous creative testing. Do not “optimise” impressions by making ads disruptive.

## 13. Monetisation UX, instrumentation and risk rules

### Initial ad policy matrix

| Placement | Initial rule | Why |
|---|---|---|
| Endless GameOver optional Continue | Rewarded only, once/run, only when loaded and opted in | Valuable choice without interrupting play |
| Between result/replay or result/home | Interstitial after 2 ad-free completed runs, >=180s from prior ad, <=3/session, <=6/day | Conservative revenue without immediate churn |
| Daily Challenge | No ad benefits, no interruptions during challenge | Fair, repeatable challenge |
| App open, first move, pause, active play | **Never show full-screen ad** | Trust/compliance |
| Home footer/banner | **No banner** in v1 | Simplifies UI and avoids accidental taps |
| IAP/remove ads | Do not ship until real Google Play Billing and correct entitlement handling | Avoid unfinished monetisation |

**What to measure:** ad opportunity vs impression fill, rewarded offer → request → completion → grant, full-screen ad close rate, next-run-start after impression, DAU, D1/D7 retention, 1- and 3-minute engagement, median run duration, tutorial completion, cohort revenue per DAU, crashes/ANRs, store visit-to-install rate.

### Data QA caveat

Remote analytics and an ad SDK each have independent privacy obligations. A Google Play Data safety form must account for **all** SDKs actually integrated, not only custom gameplay events. SDKs can collect IP, usage interactions, device identifiers and diagnostics; see official current docs. See source [S5] below.

## 14. Organic content: six ready-to-shoot short scripts

These are **storyboards**. Replace illustrative numbers with actual observed gameplay. Always label images as concept art if not in the product.

| Video | 0–2s hook | 2–10s gameplay | 10–17s payoff and CTA |
|---|---|---|---|
| V01 Impossible drop | “You have ONE move. Where?” | Show near-full board and hover two choices | Genuine cascade, real RIOT animation, “Jelly Riot — on Android” |
| V02 Combo ladder | “This combo just keeps going” | Capture wave 1/2/3 | Beat builds, end on real score + app icon |
| V03 Relaxing pop | “The sound is so satisfying” | Three pops with clean audio | Gentle looping ending, genuine Play Store CTA |
| V04 Wrong answer | “90% would choose red here” (use *only if substantiated*; otherwise “Would you choose red?”) | Hold on choices | Reveal a better move; invite comments |
| V05 Daily duel | “Can you beat today's board?” | Shared daily seed, show one attempt | Result card with honest score/share text |
| V06 Near-death comeback | “Surely this game is over…” | Near-full rising-row moment | Real comeback then high score, download info |

### Video capture rules

- Record gameplay at 1080×1920 (9:16), 30 or 60 fps, show finger indicator only if it helps comprehension, no fake UI or fake taps.
- The **first frame** needs a recognisable board and interesting state; skip logo animations until final 1–2 seconds.
- Keep most videos around 12–20 seconds; try both natural game audio and caption-led silent versions.
- Captions in safe central region to avoid platform overlays; readable, big and high contrast.
- Real store URL in profile/link once published; no misleading link or pre-launch broken store URL.
- Test which hooks produce **store visits and installs**, not which receive most likes. Use platform-native analytics and Play acquisition data; do not pretend 1M views = 1M installs.

## 15. Launch plan: first 30 days after test track is cleared

**Week A: First public version** — Launch in selected English-speaking markets with no paid campaign. Publish 1 short video/day across feasible platforms, respond genuinely to feedback, check crash reporting and support inbox. Keep changes minimal until basic cohort data arrives.

**Week B: Improve conversion** — Test two genuine store-listing messaging directions and better screenshot sequence. Publish more videos of formats with actual click-through. Fix first-run confusion and major drop-off, not add unrelated modes.

**Week C: Retention** — Use valid instrumentation: tutorial completion, median session length, D1/D7. Test balance settings in small staged updates with documented version differences. Compare before/after with adequate sample; don't over-interpret 20 users.

**Week D: Profitability decision** — Compare actual DAU, ad impressions, blended eCPM, session count and retention. Keep developing if the player base is returning and metrics improve; pivot or stop if organic discovery repeatedly fails. Avoid paid spend until a credible lifetime value estimate exists.

### Initial decision thresholds — hypotheses, not promises

- First **100 genuine installs**: check tutorial and crashes.
- First **300–500 installs**: start examining behaviour, acknowledging uncertainty.
- **D1 ~25–30%** or better and **D7 ~7%** or better would be encouraging internal targets for this project, not guaranteed genre norms.
- Median early run >=60–90s with repeat runs suggests the core loop has traction; verify players understand pressure mechanic.
- Rewarded-continue use should coexist with Replay; ads should not meaningfully increase immediate app exits.
- Do not expect A$500/month simply because cumulative installs hit 500 or 5,000.

## 16. One-week scope control and priority hierarchy

Prioritise in this order:

1. **Make one drop and one cascade joyful** — gameplay, feedback, audio, visually delicious result.
2. **Make the board replayable** — pressure, score, fairness, stable run length and daily seed.
3. **Make it reliable on Android** — no crashes, saves, real devices, high framerate.
4. **Monetise politely and correctly** — opt-in rewarded, conservative result-screen interstitials, consent/data safety.
5. **Make it discoverable** — visible store icon, honest screenshots, 10-second hook.
6. **Measure truthfully** — cohort retention, ad revenue, installs, no fake metrics.

If only four hours remain on Day 7, do **not** add a new mechanic. Finalise QA, release documents, captures and the closed-test AAB.

---

# PART G — OWNER CHECKLIST AND EXTERNAL DEPENDENCIES

## 17. What the owner must personally do or authorise

Code generation cannot responsibly finish these steps without your involvement/account access:

- [ ] Confirm name/trademark availability and choose public developer display name, package name, support email.
- [ ] Create/verify Google Play Console developer account and identity/payment profile as necessary.
- [ ] Own/control release signing key/keystore and back it up securely (never paste secret signing passwords into a repository).
- [ ] Open AdMob account, create correct app entry, configure app/placement IDs, consent messages, test devices and account/payment verification.
- [ ] Provide real privacy policy/support and developer website URLs, ensure site published and `app-ads.txt` exists at root with the **real** publisher ID.
- [ ] Decide accurate target audience/ad suitability, disclosures, app content and Data safety form answers based on actual SDKs/behaviour.
- [ ] Import/check official plugins if remote Codex cannot use Unity Editor; verify Android SDK 36 installed.
- [ ] Listen to all SFX/music and critique visual art; get screenshots on real Android hardware.
- [ ] Recruit genuine testers; if applicable, maintain >=12 opted-in for >=14 continuous days, then request production access.
- [ ] Review Google Play listing, sign/upload AAB, check pre-launch test reports and release only when ready.
- [ ] Make promotional accounts, post clips, reply to players and review actual analytics after release.

**Key distinction:** We can prepare a finished test build in a week; Play account access, the 14-day testing policy (if applicable), AdMob verification/readiness review and organic growth extend beyond that.

## 18. Concrete assets that must exist before calling the project “finished”

| Deliverable | Required count / form | Evidence |
|---|---|---|
| Pure gameplay model and tests | 7x9 deterministic board, 1000+ seeds | EditMode test report |
| Functional screens | Home, Gameplay, Settings, Result, Daily | Actual device screenshots |
| Unique jellies | 5 original 256x256 PNGs | Asset manifest + gallery |
| UI art | Board, cells, rack, button, panel, icons | Scene screenshots + import settings |
| VFX | Drop, match, chain, RIOT, rise, result | Real gameplay capture |
| SFX | 20+ original WAVs as specified | Asset list + listening review |
| Music | Optional one loop, shipped only if good | A/B listening note |
| Saves / daily | Stable offline reproducibility | Device kill/restore evidence |
| Advertising | Tested rewarded + interstitial through official SDK | Test-device test results / blockers |
| Privacy | UMP + data inventory + website planning | Compliance checklist |
| Release | API36 compatible signed-by-owner AAB | Build log, version/package evidence |
| Store assets | 512 icon, 1024x500 feature, real screenshots | Dimensions + image review |
| Organic marketing | 6 short scripts, 3 actual clips ideally | Shot list + verified video paths |
| Launch ops | Tester and Play Console handoff | FINAL_HANDOFF.md |

---

# PART H — OFFICIAL SOURCES, VERIFIED 17 OCTOBER 2026

Use these primary docs at implementation time and recheck for changes. Codex should **not assume an SDK API from memory when official docs differ**.

- **[S1] Google Play target API requirements.** As of 31 August 2026, new mainstream Android app submissions and updates must target Android 16 / API 36 or higher: https://support.google.com/googleplay/android-developer/answer/11926878
- **[S2] New personal accounts and closed testing.** New personal developer accounts created after 13 Nov 2023 generally need 12 testers opted in continuously for at least 14 days, then production-access application: https://support.google.com/googleplay/android-developer/answer/14151465
- **[S3] Google Mobile Ads Unity quick start.** Plugin requirements, current official APIs and SDK setup: https://developers.google.com/admob/unity/quick-start
- **[S4] Interstitial and rewarded Unity integrations.** Test ad units, lifecycle/cleanup, consent-aware loading: https://developers.google.com/admob/unity/interstitial and https://developers.google.com/admob/unity/rewarded
- **[S5] Google UMP Unity privacy SDK + Play data disclosure.** Update consent every launch, display form if required, CanRequestAds, privacy options, and real SDK data collection: https://developers.google.com/admob/unity/privacy and https://developers.google.com/admob/unity/privacy/play-data-disclosure
- **[S6] Play ads policy.** Avoid disruptive, unexpected or deceptive ad placement: https://support.google.com/googleplay/android-developer/answer/9857753
- **[S7] AdMob app-ads.txt verification and site requirements.** New app verification/readiness review and developer website: https://support.google.com/admob/answer/14538460 and https://support.google.com/admob/answer/9363762
- **[S8] Store preview assets.** Official icon, feature-graphic and screenshots technical rules: https://support.google.com/googleplay/android-developer/answer/9866151
- **[S9] Unity 6.3 LTS.** Release and support lifecycle: https://unity.com/releases/unity-6

**Source precedence:** official current Play Console/AdMob/Unity documentation > this historical plan for regulatory or SDK details. If requirements change, update `Docs/RELEASE_CHECKLIST.md` before shipping, without silently changing the product/gameplay contract.

---

# START HERE — FIRST MESSAGE TO CODEX

After placing this file into the repository root, copy/paste:

```text
We are making JELLY RIOT, an original Android casual puzzle game, and the document Jelly_Riot_Codex_Master_Plan.md is our authoritative product and implementation specification. The goal is a polished one-week test build with high replayability, joyful original art/audio/VFX, privacy-respecting ads and strong organic marketing assets. Start with Work Order 00 ONLY. Read the entire master plan; inspect the existing Unity project; create AGENTS.md and Docs/PROGRESS.md; report your findings, exact changes and actual tests. Do not begin Work Order 01 until I approve the setup. Do not overengineer, compromise performance, silently skip anything, or claim to run Unity/Android/AdMob when you cannot. Prioritise keeping the project compiling and work in Assets/_Project. Use Australian English in comments/UI text. When ready, tell me the precise next step.
```

**Completion rule:** “The work order has been implemented” means actual files exist and a test was run or explicitly marked **NOT RUN**. “Ready for Google Play closed testing” means the build and required store/compliance materials exist, but the developer still needs to do account-bound publishing work. **Profit is measured only after real people download and play the released game.**
