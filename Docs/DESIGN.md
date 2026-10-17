# Jelly Riot design and architecture

The complete product design is in `Assets/Jelly_Riot_Codex_Master_Plan.md`. This document records implementation-facing decisions.

## Runtime shape

```mermaid
flowchart LR
    B[Boot scene] --> R[Runtime initialisation]
    R --> M[Main scene]
    M --> C[Gameplay controller]
    C --> P[Pure deterministic Core]
    P --> S[Snapshots and semantic events]
    S --> C
    C --> V[UI, visuals, audio and VFX]
    C --> I[Persistence and platform interfaces]
```

- `Boot` is deliberately minimal: it establishes the 60 FPS target and loads `Main`.
- `Main` is currently a presentation shell. Screens and gameplay controllers belong to later work orders.
- Core owns board coordinates, placement, random state and snapshots. It has no Unity dependency.
- Unity ScriptableObjects expose tuning data, then adapt it to immutable Core configuration.
- Ads, analytics, audio, haptics, sharing and persistence are boundary interfaces. Implementations arrive only in their scheduled work orders.

## Determinism contract

Given the same seed, balance values and actions, Core must produce the same board, generated colour stream, turn count and random state. Invalid actions do not modify any of those values. Animation, elapsed time and frame rate are never inputs.

## Board contract

- 7 columns by 9 rows.
- `(0,0)` is top-left; row 8 is the bottom.
- Empty is an explicit `JellyColour.Empty` value.
- Initial boards contain 20 occupied cells (31.7%), use three active colours, remain in the bottom four rows, contain no connected group of four, and guarantee a one-drop opening match opportunity.

## Resolution contract

- Match connectivity is orthogonal only and is found with an iterative flood fill.
- Every qualifying connected component is recorded before any cell in that wave is removed.
- Gravity is vertical and stable: occupied cells compact toward row 8 without changing their relative order.
- Resolution repeats synchronously until stable. Each successful wave removes at least four cells, so a 63-cell board must terminate in at most 15 waves.
- `BoardResolutionResult` exposes ordered, read-only waves. Each wave records its zero-based index, cleared cells with colours, and every fall with source, destination and colour.
- Core owns the entire calculation and does not use Unity objects, transforms, time, coroutines or presentation state.

`GameTest.unity` is a development-only playable inspection scene and is intentionally excluded from build settings. It displays immediate Core state so current behaviour can be exercised before the production presentation and session layers are scheduled.

## Session and input contract

- A run exposes exactly three rack slots. Selecting a slot does not mutate the board.
- A legal placement consumes only the selected slot and moves the session from `Ready` to an observable `Resolving` state.
- Invalid columns and full columns preserve the selected slot and candidate. Inputs during `Resolving` cannot place another jelly.
- Resolution and scoring complete in Core before the consumed slot is refilled and the session returns to `Ready`.
- Semantic event ordering is deterministic and independent of presentation timing. Views, audio and analytics consume event data without calculating outcomes.
- Supported states are `Boot`, `Home`, `Ready`, `Resolving`, `PendingLoss`, `GameOver` and `Paused`. Pressure behaviour that enters `PendingLoss` remains Work Order 05 scope.

## Score contract

- Resolution wave indices remain zero-based; their scoring chain number is `wave.Index + 1`.
- Wave points are `cleared count × 10 × chain number` and accumulate in a saturating `long`.
- `Chain2`, `Riot3Plus`, `LargeGroup8Plus` and `PersonalBest` are metadata only and never change gameplay or score.
- Endless best and Daily best-by-key records are isolated. A new run resets current score and longest chain while retaining records.
- Run score and best records are finalised before the session publishes its transition to `GameOver`.

## UI policy

Future canvases use `Scale With Screen Size`, reference resolution 1080x1920, portrait orientation and safe-area containers. Layouts must remain usable at 16:9 and 19.5:9 without placing essential controls in gesture or cut-out areas.

