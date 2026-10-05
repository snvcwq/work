# Stage 1: Spec → G0

1. Dispatch `task-analyst` (name `analyst-<slug>`) → `01-spec.md`.
2. On `NEEDS_CONTEXT`: put the analyst's question round to the user, each question with the analyst's recommended answer. Write the answers into `01-spec.md § Answers` and resume the analyst. Repeat until `DONE`.
3. **M/L:** dispatch `critic` mode `spec-review` → `01-spec-review.md`. Classify findings; send actionable ones (including over-sized criteria) back to the analyst once.
4. **G0 checkpoint.** Show the user: the intent (outcome, user, success, constraint, out of scope), the criteria with must/should, the edge cases considered and skipped, the track, the roster. Ask for an explicit yes or corrections. Record it.

Done when the ledger has `Stage spec: G0 PASS` and the approved track.
