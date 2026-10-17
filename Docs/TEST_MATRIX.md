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
| ANDROID-001 | Toolchain audit | SDK, NDK, OpenJDK and platform recorded | PASS |

## Future coverage

Work Orders 03 onward extend this matrix for state transitions, pressure, scoring, persistence, accessibility, ads, consent, builds and device performance.

Evidence on 2026-10-17: Unity 6000.3.11f1 isolated validation copy, 24/24 EditMode tests and 1/1 PlayMode test passed. A separate pure .NET smoke harness checked 10,000 seeds. The isolated copy was used because the owner's editor already had the repository open; source and project settings were copied without modifying the live editor session.

Work Order 02 evidence on 2026-10-17: all 21 `BoardResolverTests` executed and passed in a local reflection runner using the compiled NUnit test assembly. Core, tests and the playable Runtime controller compiled with zero C# errors against Unity-generated project files. A headless Unity Test Runner attempt did not reach test execution because the editor repeatedly lost its local Licensing Client connection, so `UNITY-003` remains `NOT RUN`.

