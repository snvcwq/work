# Playbook: Test infrastructure

1. **Intake**; `code-explorer` lens C maps current tests, frameworks, fixtures, CI test steps, and the baseline.
2. **Plan**: dispatch `solution-architect` mode `structure` focused on tests → `04-design.md`: test project layout (unit / integration / architecture), framework and assertion library (keep the repo's existing choice unless the user wants to change it), a shared `WebApplicationFactory` fixture with test authentication, Testcontainers for the real database (and the container lifetime strategy), data builders, `TimeProvider` fakes, coverage collection (coverlet) and the changed-lines report, the CI step (Docker availability for Testcontainers), and one example test per kind. **Checkpoint**: the user approves.
3. **Build**: task briefs run through `stages/4-build.md`. The test-engineer's blind acceptance step checks the infrastructure works through its own seams: an example integration test boots the app, hits an endpoint and reads back through the API.
4. **Document**: a short `tests/README.md` (how to run each kind, how to add a test) and update CONSTRAINTS.md coverage rows when the user wants them.
5. **Prove it**: full test run with coverage, and the CI test step validated. Show the user the evidence.
