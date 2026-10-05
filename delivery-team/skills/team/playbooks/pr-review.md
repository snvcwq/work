# Playbook: PR review (someone else's PR)

Read only. You produce a review for the user; nothing is posted to GitHub.

1. **Fetch**: `gh pr view <n> --json title,body,author,baseRefName,headRefName,files,closingIssuesReferences`, the linked issues (`gh issue view`), and `gh pr diff <n>` → `.claude/work/pr-<n>/branch.diff`. When the user wants it run locally, check it out in a worktree (`gh pr checkout` inside `git worktree add`) and run `ship-evidence.sh --full`; otherwise review the diff only and say so.
2. **The spec** is the PR description plus linked issues, written to `01-spec.md` as criteria (draft them yourself; mark anything you inferred). If there's no description or issue, the spec axis reviews intent stated in commits and says the PR lacks a stated goal.
3. **Reviewers in parallel**, one message:
   - `critic` mode `board-spec` → `spec.md`
   - `reviewer` mode `board` lens `correctness` (opus) → `code.md`
   - `reviewer` mode `board` lens `standards` → `standards.md`
   - `security-reviewer` mode `review` → `security.md`
   - specialists when the diff touches their area (`data-specialist` for migrations and queries, `api-designer` for endpoints, `performance-engineer` for hot paths)
4. **Summary for the user** (`review.md`): verdict (approve / approve with comments / request changes), one heading per axis with its findings, and what's good.
5. **Draft comments** (`comments.md`): one block per finding, ready to paste, with `path:line`, severity label (Critical / Required / Optional / Nit), the problem, why it matters and a suggested fix, written in a collegial tone. Order by severity. Tell the user the file path; posting is theirs.
