# Playbook: Small change

A clear behavior change in 1–2 files, with no public contract or schema change. Same gates as a feature task, without the spec and design stages.

1. **Intake** (`stages/0-intake.md`), then write a five-line `tasks/task-01.md` yourself: Goal · Behavior (the change in observable terms, one or two criteria) · Files · Done when · Tier. Show it to the user in the announcement.
2. **Build → blind acceptance → review** exactly as `stages/4-build.md` describes for one task. The test-engineer works from your brief's Behavior section as the spec.
3. Run `~/.claude/hooks/delivery/ship-evidence.sh --out 10-evidence.txt`.
4. Show the user the diff summary, the evidence and the review verdict. Offer: commit / keep uncommitted / discard. If it grew beyond 2 files or touches a contract, stop and propose switching to **Feature**.
