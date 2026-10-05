# Playbook: Investigate

The user wants to understand something: why a bug happens, how a feature works, where time goes, what a change would affect. No production code changes.

1. Work folder `.claude/work/investigate-<slug>/`, short ledger.
2. Choose the seat by question:
   - "Why does X happen / X is broken" → `debugger` (opus): feedback loop, minimal repro, hypotheses, root cause. Offer the **debug team** when it's hard and unclear.
   - "How does X work / where is X / what would changing X affect" → `code-explorer` ×1–2 with fitting lenses.
   - "Why is X slow" → `performance-engineer` mode `advise`, measuring before claiming.
3. Write `report.md` for the user: the answer in three lines first, then evidence (`file:line`, command output), what's still unknown, and options for next steps (for example "fix as a Bug fix playbook", "leave as is").
4. Diagnostic tests or tagged debug logs the seats added are removed unless the user wants to keep a regression test.
