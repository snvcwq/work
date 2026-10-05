# Playbook: Address review comments (the user's own PR)

1. **Fetch** the PR and all review comments and threads, read only: `gh pr view <n> --json ...`, `gh api repos/{owner}/{repo}/pulls/<n>/comments`, `gh api repos/{owner}/{repo}/pulls/<n>/reviews`. Check out the PR branch (in a worktree if another run is active). Save to `.claude/work/pr-<n>-review/comments.json`.
2. **Triage**: dispatch `reviewer` mode `comments` → `triage.md`. Each unresolved comment is classified, with evidence from the code: **valid** (fix it) · **disagree** (technically wrong for this codebase; a reasoned reply) · **question** (needs an answer, not a change) · **already done** · **out of scope** (propose a follow-up). Related comments are grouped.
3. **Checkpoint**: show the user the triage table. They confirm or change each classification. Disagreements and out-of-scope items are the user's call.
4. **Fix**: group the valid comments into task briefs (one per related group) and run each through **build → blind acceptance → review** (`stages/4-build.md`). Pure wording or comment-only fixes can be done Quick-style.
5. **Draft replies** (`replies.md`): one per thread, ready to paste. Fixed: what changed and the commit. Disagree: the reasoning with evidence, politely. Question: the answer. No performative agreement ("great catch!"); state what was done.
6. **Finish**: commit locally with messages that reference the comments. Show the evidence (`ship-evidence.sh --full`), the replies file path, and ask whether to push. Push only on an explicit yes; posting replies is the user's.
