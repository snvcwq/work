# Stage 5: QA → G4

1. Dispatch `test-engineer` mode `qa` → `08-qa.md` and new scenario tests.
2. Each reported bug becomes `tasks/task-NN-fix.md` (repro test, expected, actual) and goes through the stage 4 loop.
3. **G4:** full suite green with pre-existing failures listed separately, every `must` criterion verified against the before/after table, CONSTRAINTS.md checks hold, and the QA verdict is PASS.

Done when the ledger has `Stage qa: G4 PASS`.
