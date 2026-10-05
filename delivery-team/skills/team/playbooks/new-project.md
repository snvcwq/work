# Playbook: New project or solution

1. **Intake** and work folder. Then interview in rounds via `task-analyst` (it drafts, the user decides): what the project is for, project types (Web API, worker, library, Blazor), target framework, architecture style (layered, vertical slices, modular monolith), data access, auth, hosting and CI, test approach. Each question has a recommended answer.
2. **Research**: dispatch `researcher` (sonnet) with the numbered questions on the stack and any vendor APIs → `03-research.md`. Specialists advise only on risks the spec names; otherwise the critic covers them as lenses.
3. **Structure**: dispatch `solution-architect` mode `structure` → `04-design.md`: solution layout (`.slnx`, `src/`, `tests/`), projects and references, `Directory.Build.props` (nullable, warnings as errors, analysis level), Central Package Management (`Directory.Packages.props`), `.editorconfig`, `BannedSymbols.txt`, `global.json`, test projects and their kinds, the CI workflow, the first vertical slice that proves the skeleton works end to end. **Checkpoint**: the user approves the structure.
4. **Scaffold**: task briefs per area, run through `stages/4-build.md`: `devops-engineer` for CI and containers, `csharp-implementer` for projects and the first slice. Prefer `dotnet new` templates and `dotnet sln add`.
5. **Wire the team**: run the **setup** playbook (CONSTRAINTS.md, CLAUDE.md, permissions).
6. **Prove it**: `ship-evidence.sh --full`: restore, build, tests, format, CI file syntax. Show the user the tree and the evidence. Commit on a yes.
