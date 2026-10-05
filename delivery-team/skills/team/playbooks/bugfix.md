# Playbook: Bug fix

1. **Intake** (`stages/0-intake.md`).
2. **Reproduce first.** Dispatch `debugger` (opus) → `reports/debug-1.md`: a tight feedback loop that goes red on the user's exact symptom, a minimal reproduction, ranked hypotheses, the proven root cause, a regression test at a real seam, and the recommended fix. When the cause stays unclear after three hypotheses, offer the **debug team**.
3. **Checkpoint**: show the user the root cause in two lines and the recommended fix. On a yes:
4. **Size the fix**: one or two files and no contract change → a task brief like **Small change**, with AC-0 "the regression test passes and <symptom> no longer happens". Otherwise → **Feature** from stage 1, with the debug report as input.
5. **Build → blind acceptance → review** (`stages/4-build.md`). The debugger's regression test stays as written; the test-engineer adds blind acceptance tests for the corrected behavior.
6. **Ship** (`stages/7-ship.md`), with the root cause stated in the release notes so the next debugger learns from it.
