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
| ANDROID-001 | Toolchain audit | SDK, NDK, OpenJDK and platform recorded | PASS |

## Future coverage

Work Orders 02 onward extend this matrix for matches, gravity, state transitions, pressure, scoring, persistence, accessibility, ads, consent, builds and device performance.

Evidence on 2026-10-17: Unity 6000.3.11f1 isolated validation copy, 24/24 EditMode tests and 1/1 PlayMode test passed. A separate pure .NET smoke harness checked 10,000 seeds. The isolated copy was used because the owner's editor already had the repository open; source and project settings were copied without modifying the live editor session.

