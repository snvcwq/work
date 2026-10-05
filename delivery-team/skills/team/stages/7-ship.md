# Stage 7: Ship → G6

1. **Integrate.** Fetch the base branch. When it moved since the run started, rebase or merge it into the feature branch; on conflicts in task files, dispatch the implementer (opus) with the conflict list. A conflicting EF migration is re-created after the other branch's migration, never hand-merged in `ModelSnapshot.cs`.
2. **Evidence.** Run `~/.claude/hooks/delivery/ship-evidence.sh --full --out 10-evidence.txt`: build with warnings as errors, full test suite (executed count, pre-existing failures separate), format check, floor guard since base, every CONSTRAINTS row. Any failure → back to stage 4 with a fix task.
3. **Write `10-release.md`** yourself (you hold the ledger and rulings):
   ```
   ## Summary
   <the smallest picture that makes the change clear: call tree, pseudocode or shallow file tree>
   ## Evidence
   Before: <failing run, old response or behavior>   After: <passing run, new response>
   ## Merge danger
   Door: one-way | two-way   Blast radius: <one word>   <migrations, contract changes, rollback steps>
   ## Tests
   <executed count, new tests, constraints results>
   ## Traceability
   <must criterion → test name → passing>
   ## Rulings I made
   ## Follow-ups (deferred minors, parked findings, should-criteria not built)
   ```
   Also add the changelog entry in the repo's format. Docs pages whose described behavior changed go to the implementer as a small task.
4. **G6 checkpoint.** Show the user the evidence summary, the PR text (in `10-release.md`, ready to paste) and Rulings I made. Offer:
   1. Merge into the base branch locally
   2. Keep the branch as it is (the user opens the PR with the drafted text)
   3. Push the branch (only on an explicit yes; the PR itself is the user's to open)
   4. Discard the work (the user types `discard <slug>` to confirm)
5. Execute the choice. Release the migration lock, remove a finished worktree when the user agrees, and mark the ledger shipped.
6. Give the finish message from SKILL.md and offer a short retro.
