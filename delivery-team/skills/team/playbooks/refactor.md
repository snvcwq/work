# Playbook: Refactor

Restructure code without changing behavior. Here tests come **first**, because the target is "nothing changed".

1. **Intake** (`stages/0-intake.md`); explorers ×1–2 (`stages/2-explore.md`) to map the area, its consumers and the baseline.
2. **Design**: dispatch `solution-architect` mode `options:2` with the user's goal (for example "split OrderService", "remove the static helpers"). The design states the target structure, the public surface that must stay identical, and the steps as small tasks that each leave the build green. Use `critic` mode `design-critique` on anything bigger than one task. **Checkpoint**: the user approves the target and the steps.
3. **Pin behavior**: dispatch `test-engineer` mode `characterize` → tests that pin today's behavior at the seams the refactor must not change. They pass now and must pass after every task.
4. **Tasks**: for each step, dispatch `csharp-implementer` with the brief and the instruction "behavior must not change; characterization tests must stay green unchanged". Use `dotnet:csharp-refactoring` for renames and moves. Then `reviewer` mode `task`: spec compliance means "no behavior change, public surface identical"; any characterization test edit is Critical.
5. **Ship** (`stages/7-ship.md`). The PR's evidence section shows the characterization tests green before and after.
