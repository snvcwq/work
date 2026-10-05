# Stage 6: Review board → G5

1. Write `09-board/branch.diff` (`git diff <base commit>` plus `--stat`). Run `~/.claude/hooks/delivery/ship-evidence.sh --out 09-board/ship-evidence.txt` (build with warnings as errors, format check, floor guard since base, CONSTRAINTS rows that run at board).
2. Dispatch in one message, so they run in parallel, each on its own axis:
   - `critic` mode `board-spec` → `09-board/spec.md`
   - `reviewer` mode `board` lens `correctness` (opus) → `09-board/code.md`
   - `reviewer` mode `board` lens `standards` (sonnet) → `09-board/standards.md`
   - `security-reviewer` mode `review` → `09-board/security.md` (every M/L run; on S only when on the roster)
   - roster specialists in `review` mode (`api-designer`, `data-specialist`, `performance-engineer`)
   Point each at the ledger's deferred minors and rulings.
3. Write `09-board/summary.md`: one heading per axis, its findings and its worst issue. Axes stay separate; a clean axis never offsets a failing one.
4. **One fix wave:** a single implementer dispatch (opus) with every Critical/Important across axes. Then one scoped re-review by each seat that raised findings. Residuals get rulings.
5. **G5:** no open Critical/Important on any axis; every parked item has a ruling.

Done when the ledger has `Stage board: G5 PASS`.
