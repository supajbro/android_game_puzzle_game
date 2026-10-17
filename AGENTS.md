# Jelly Riot repository rules

`Assets/Jelly_Riot_Codex_Master_Plan.md` is the authoritative product and engineering specification. Work orders are implemented in order and must not silently include later scope.

## Architecture

- Put custom Unity assets beneath `Assets/_Project/`.
- Keep deterministic rules in `Assets/_Project/Scripts/Core/`. This assembly must not reference `UnityEngine`, scene objects, frame time, animation state or global random state.
- Put Unity-facing orchestration in `Scripts/Gameplay`, UI in `Scripts/UI`, visual presentation in `Scripts/Visuals`, and integrations in their named folders.
- Treat the Core model as authoritative. Views consume snapshots and semantic events; they never calculate or mutate gameplay outcomes.
- Use small interfaces at meaningful boundaries. Do not add a dependency-injection framework or speculative generic layers.
- Use ScriptableObjects only as inspector-facing configuration adapters. Convert their values to pure Core data before simulation.
- `Boot` performs application initialisation and opens `Main`. `Main` will host menu and gameplay presentation in later work orders.

```text
Boot scene -> Unity runtime adapters -> pure Core simulation
                       |                    |
                       v                    v
            UI / Visuals / Audio       snapshots + events
                       |
                       v
          Platform / Persistence / Ads / Analytics
```

## Coding conventions

- Use Australian English in comments, documentation and player-facing text.
- Prefer clear, explicit C# over deep inheritance or clever abstractions.
- Validate public boundaries and fail without partial mutation.
- Use `(column, row)`, with column zero at the left and row zero at the top.
- Gameplay randomness must come from `IGameRandom`; never use `UnityEngine.Random` in gameplay state.
- Keep allocations out of per-frame and per-cell hot paths when practical.
- Avoid `Debug.Log` noise in release builds. Development-only diagnostics must be conditionally compiled.
- Do not add third-party packages unless a work order requires one and compatibility/licensing have been verified.

## Testing and workflow

- Pure simulation tests live in `Assets/_Project/Tests/EditMode/` and reference `JellyRiot.Core`.
- Unity integration tests live in `Assets/_Project/Tests/PlayMode/`.
- Run the relevant tests after every gameplay change. Never report a test as passing unless it actually ran.
- Record each work order in `Docs/PROGRESS.md`: date, exact changed files, commands/tests, evidence, blockers and next work order.
- Keep the project compiling between work orders. Preserve existing assets and project settings unless the specification explicitly requires a change.
- Never commit generated Unity folders, builds, credentials, signing files, publisher IDs or local user settings.
- Do not start the next work order without the owner's request.

## Current technical baseline

- Unity 6000.3.11f1, 2D URP 17.3, portrait Android, logical UI reference 1080x1920.
- Target frame rate: 60 during play. Editor timing is not used for deterministic rules.
- Assemblies: `JellyRiot.Core` (no engine references), `JellyRiot.Runtime`, `JellyRiot.Editor`, `JellyRiot.EditModeTests`, and `JellyRiot.PlayModeTests`.

