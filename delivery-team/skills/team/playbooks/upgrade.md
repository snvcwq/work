# Playbook: Upgrade (.NET version or packages)

1. **Intake** with a worktree. `code-explorer` lens A records the baseline (build, full tests, warnings count).
2. **Plan**: use the `dotnet-upgrade` and `dotnet-nuget` plugin skills when installed (suggest installing them if not). `solution-architect` mode `structure` writes the upgrade plan: target versions, breaking changes that apply to this code (checked against Microsoft Learn), order of projects, package moves, risky areas. **Checkpoint**: the user approves.
3. **Upgrade in steps**: one task per project group or package family, each leaving the build green, through `stages/4-build.md`. Behavior must not change: the existing suite is the acceptance check; the test-engineer adds characterization tests only for risky areas the plan names.
4. **Board** (`stages/6-board.md`) with `reviewer` lens `standards` and `security-reviewer` (vulnerable packages), then **ship** with before/after warnings, test counts and package versions in the PR text.
