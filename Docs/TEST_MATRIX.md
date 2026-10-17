# Jelly Riot test matrix

Statuses are `PASS`, `FAIL`, `BLOCKED` or `NOT RUN`. Only executed checks may be marked `PASS`.

| ID | Scope | Expected | Status |
|---|---|---|---|
| CORE-001 | Board dimensions and coordinate origin | 7x9, top-left `(0,0)` | PASS |
| CORE-002 | Empty-column placement | Piece occupies row 8 | PASS |
| CORE-003 | Stacking | Pieces stack bottom-up | PASS |
| CORE-004 | Edge columns | Columns 0 and 6 accept legal moves | PASS |
| CORE-005 | Full column | Rejected without mutation | PASS |
| CORE-006 | Invalid column | Rejected without mutation | PASS |
| CORE-007 | Invalid action determinism | Board, turn and RNG unchanged | PASS |
| CORE-008 | Same seed | Same board and future colour stream | PASS |
| CORE-009 | Different seeds | At least one board or stream difference | PASS |
| CORE-010 | RNG reproduction | Same state produces same sequence | PASS |
| CORE-011 | RNG restore and JSON round trip | Serialised state resumes exact sequence | PASS |
| CORE-012 | Reset | Original seed restores original state | PASS |
| CORE-013 | Read access | No mutable backing array is exposed | PASS |
| CORE-014 | Initial board | 25-35% occupied, lower rows, 3 colours | PASS |
| CORE-015 | Initial matches | No connected same-colour group of 4+ | PASS |
| CORE-016 | Opening opportunity | At least one legal drop can form 4+ | PASS |
| UNITY-001 | Script compilation | No compiler errors | PASS |
| UNITY-002 | Boot/Main flow | Boot opens Main, target is 60 FPS | PASS |
| RES-001 | Horizontal group | Four orthogonally connected cells clear | PASS |
| RES-002 | Vertical group | Four orthogonally connected cells clear | PASS |
| RES-003 | Non-linear shape | L-shaped component clears | PASS |
| RES-004 | Large component | Every cell in a 5+ component clears | PASS |
| RES-005 | Diagonal cells | Diagonal-only cells remain | PASS |
| RES-006 | Simultaneous groups | Independent groups clear in one wave | PASS |
| RES-007 | Chain reaction | Gravity-created match clears in wave 1 | PASS |
| RES-008 | Stable gravity | Relative vertical order is preserved | PASS |
| RES-009 | Empty board | Valid zero-wave result | PASS |
| RES-010 | Full board | Resolves without recursion failure | PASS |
| RES-011 | Component uniqueness | No duplicate clear records | PASS |
| RES-012 | Exact clear | Every qualifying cell clears once | PASS |
| RES-013 | Non-match retention | Non-qualifying cells remain | PASS |
| RES-014 | Vertical-only gravity | Source and destination columns match | PASS |
| RES-015 | Settled board | No gaps beneath occupied cells | PASS |
| RES-016 | Event fidelity | Positions and colours match mutations | PASS |
| RES-017 | Wave metadata | Indices and aggregate counts are exact | PASS |
| RES-018 | Resolution determinism | Identical boards produce identical ordered results | PASS |
| RES-019 | Stable board | No mutation and zero waves | PASS |
| RES-020 | Termination | Dense board remains within mathematical wave bound | PASS |
| RES-021 | Result immutability | Result lists reject mutation | PASS |
| UNITY-003 | Work Order 02 Unity Test Runner pass | Run resolver suite inside Unity | NOT RUN |
| SESSION-001 | Three-slot rack | All candidates are visible and non-empty | PASS |
| SESSION-002 | Slot consumption | Only selected slot is consumed and refilled | PASS |
| SESSION-003 | Invalid selection | Illegal indices preserve prior selection | PASS |
| SESSION-004 | Invalid placement | Invalid/full column preserves selected jelly | PASS |
| SESSION-005 | Duplicate input | Resolving state rejects a second placement | PASS |
| SESSION-006 | State transitions | Boot, menu, ready, pause, loss and replay are guarded | PASS |
| SESSION-007 | Semantic events | Placement, resolution and completion order is stable | PASS |
| SESSION-008 | Replay/reset | Seeded board and rack reproduce exactly | PASS |
| SCORE-001 | First wave | Four cells award 40 points | PASS |
| SCORE-002 | Second wave | Five cells award 100 points | PASS |
| SCORE-003 | Simultaneous groups | Combined cleared count scores once | PASS |
| SCORE-004 | Chain records | Longest chain and Chain2 metadata are correct | PASS |
| SCORE-005 | RIOT metadata | Third wave produces Riot3Plus | PASS |
| SCORE-006 | Large component | Actual 8+ component produces LargeGroup8Plus | PASS |
| SCORE-007 | Overflow | Arithmetic saturates safely in `long` | PASS |
| SCORE-008 | Run reset | Current score resets while records remain | PASS |
| SCORE-009 | Mode records | Daily and Endless results remain isolated | PASS |
| SCORE-010 | Result ordering | Final score precedes GameOver state | PASS |
| UNITY-004 | Work Orders 03/04 Unity Test Runner pass | Run session and score suites inside Unity | NOT RUN |
| ANDROID-001 | Toolchain audit | SDK, NDK, OpenJDK and platform recorded | PASS |

## Future coverage

Work Orders 05 onward extend this matrix for pressure, persistence, accessibility, ads, consent, builds and device performance.

Evidence on 2026-10-17: Unity 6000.3.11f1 isolated validation copy, 24/24 EditMode tests and 1/1 PlayMode test passed. A separate pure .NET smoke harness checked 10,000 seeds. The isolated copy was used because the owner's editor already had the repository open; source and project settings were copied without modifying the live editor session.

Work Order 02 evidence on 2026-10-17: all 21 `BoardResolverTests` executed and passed in a local reflection runner using the compiled NUnit test assembly. Core, tests and the playable Runtime controller compiled with zero C# errors against Unity-generated project files. A headless Unity Test Runner attempt did not reach test execution because the editor repeatedly lost its local Licensing Client connection, so `UNITY-003` remains `NOT RUN`.

Work Orders 03/04 evidence on 2026-10-17: explicit projects compiled every Core source plus the editor-only test controller with 0 warnings and 0 errors. The local NUnit reflection runner executed 53 tests (21 resolver regression, 20 expanded session cases and 12 score cases), all passing. Unity Test Runner was not executed for this batch, so `UNITY-004` remains `NOT RUN`.

