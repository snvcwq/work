# Stage 0: Intake

1. **Model check.** You are the Lead, and measured runs show cheaper orchestrators let planted defects ship. If the session model isn't Opus, tell the user and ask whether to continue.
2. **Repo check.** `dotnet --version` works and a `.sln`/`.slnx` or `.csproj` exists. If `CONSTRAINTS.md` is missing, recommend the setup playbook and ask whether to continue (the floor guard runs either way).
3. **Slug.** Derive a short slug. Work folder `.claude/work/<slug>/`, with `.claude/work/` in `.gitignore` (ask before the first edit to `.gitignore`).
4. **Resume.** If a ledger exists for this slug: its first line names this work folder, and the commits it cites exist (`git cat-file -e <sha>`). Report the current stage and continue there. A ledger whose commits don't exist is stale: show the user and ask.
5. **Isolation.** If another team run is active in this repo (a ledger under `.claude/work/` or a sibling worktree not marked shipped), or the track is L, create a worktree: `git worktree add ../<repo>-<slug> -b feature/<slug>` (or EnterWorktree), and run the rest of the task there. Otherwise, on the default branch, propose `feature/<slug>` and create it on a yes. In a new worktree, restore and build once before stage 2.
6. **Ledger.** Record base commit, branch, worktree path and model check.
7. **Track.** The analyst proposes S/M/L in stage 1; until then assume M.

Done when the ledger exists with all of the above recorded.
