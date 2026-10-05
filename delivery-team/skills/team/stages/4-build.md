# Stage 4: Build → G3 (per task, in order)

The order inside a task: **build the behavior → blind acceptance tests → review**. The implementer never sees the acceptance tests before it has built the feature, so its goal stays the behavior the spec describes.

**Migration lock.** Before the first task whose brief says `Migration: yes`, claim `<main repo>/.claude/work/.migration-lock` (write your slug and time) when it's free. When another run holds it, finish the tasks without migrations first and tell the user which run holds the lock. Release it at ship or discard.

**Parallel tasks (L only).** Tasks marked `Independent: yes` may run at the same time, each in its own worktree from the feature branch. Merge each into the feature branch in task order after it passes G3, re-running its acceptance tests after the merge.

For each `tasks/task-NN.md`:

1. **Base.** Record `TASK_BASE=$(git rev-parse HEAD)` in the ledger.
2. **Build.** Delete any leftover `.claude/work/.build-gate-failed.txt` and `.claude/work/.floor-guard-findings.txt`, then dispatch `csharp-implementer` (name `impl-<slug>-NN`, model from the brief's tier) → code, developer tests, `reports/task-NN.md`.
   - When it returns, check those two files: a hook writes them when the implementer stopped with the build gate or floor guard still failing. Their content goes into the reviewer's dispatch as findings.
   - `DONE` → step 3.
   - `DONE_WITH_CONCERNS` → read them; correctness concerns go into the reviewer's dispatch.
   - `NEEDS_CONTEXT` → design files first, then the architect on call (resume `architect-<slug>` if its context is small, else a fresh architect on `04-design.md`/`04-answers.md`), then the user for product decisions. The answer is in `04-design.md`/`04-answers.md` before you resume the implementer.
   - `BLOCKED` → split the task, add context, raise the tier, or ask the user.
3. **Blind acceptance.** Create `.claude/work/.blind` (it turns on the tester's blind guard and records which test files are acceptance tests), dispatch `test-engineer` mode `accept` with `TASK_BASE`, and delete `.claude/work/.blind` when it returns. → acceptance tests + `reports/accept-NN.md`. Its hook blocks reading the files this task changed; it works from the spec, the design's seams and signatures, and the brief's Behavior section. Its report gives, per test: **red on base** (fails on `TASK_BASE`) and **green on head** (passes on the new code), or the failure output.
4. **Package.** `git diff $TASK_BASE > reviews/task-NN.diff` and append `git diff --stat $TASK_BASE`. Pass it by path.
5. **Review and triage.** Dispatch `reviewer` mode `task` (sonnet) with the brief, Global Constraints, `04-answers.md`, the implementer report, the acceptance report and the diff → `reviews/task-NN.md`. For every acceptance test that isn't green on head, the reviewer rules **code bug** or **test bug** against the spec, quoting the criterion. A test that passes on base too is a weak test.
6. **Fix loop** while spec is ❌, a Critical/Important finding is open, or an acceptance test is red:
   - **Code bugs and code findings**: rounds 1–3 resume `impl-<slug>-NN` with the findings and the failing acceptance output verbatim; rounds 4–5 a fresh implementer on opus ("a prior implementer attempted this N times; you own it now; read the report file"). From the first fix round on, the implementer sees the failing acceptance tests; the target is still the criterion the test checks.
   - **Test bugs and weak tests**: resume the test-engineer with the reviewer's ruling; it fixes the test blind and re-runs red-on-base / green-on-head.
   - Before each re-review, confirm the fix report names the covering tests, the command and the output, and that the acceptance tests were re-run.
   - Re-review is scoped: the reviewer gets its findings, the fix diff (`git diff <last reviewed head>`) and the new acceptance results.
   - Minors go to the ledger as deferred. Plan-mandated findings go to the user. A disagreement between code and test that the spec can't settle is a product question: the user.
7. **At the cap** with findings open: reviewer wrong → ruling; real but nothing builds on it → park with a ruling; real and load-bearing → rule the smallest change that unblocks the next task and carry it into that dispatch. Ask the user only when every path forward is a guess.
8. **G3:** every acceptance test red on base and green on head, the implementer's hooks clean, spec ✅, quality approved.
9. **Commit** `"<slug>: task NN <title>"` (code, developer tests and acceptance tests together) when commits per task are on. Ledger: `Task NN: complete (...)`.
10. **L track:** after task 01, send the user a three-line progress note and ask whether to continue.

Done when every task has a `complete` line in the ledger.
