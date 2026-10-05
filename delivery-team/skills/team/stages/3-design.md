# Stage 3: Design → G2

0. **Research.** When the spec lists research questions, or the design needs facts about vendor APIs, new libraries or framework versions, dispatch `researcher` with the numbered questions → `03-research.md`. When the architect returns `NEEDS_CONTEXT` with research questions, run the researcher again (→ `03-research-2.md`) and dispatch a fresh architect.
1. Dispatch `solution-architect`, named `architect-<slug>` (on call during build; see Cost rules for resume vs fresh):
   - **S:** mode `brief-only`.
   - **M:** mode `options:2` (Minimal + Deep).
   - **L:** mode `options:3`.
2. **M/L:** dispatch `critic` mode `design-critique` with the spec, impact files, constraints and design, without the architect's "why this is right" argument. Classify findings, including over-testing. At most 2 revision rounds (a fresh architect `architect-<slug>-r2` reading `04-design.md` and `04-critique.md`; resume the original only if its run was short), then rule on what's left.
3. **L, optional design debate:** when the critique leaves substantive disagreement, offer the user a debate. It runs in a separate session started with `CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1 claude`: there, spawn the architect and critic as teammates (mode `debate`, each told to read `~/.claude/skills/design-vocabulary/SKILL.md` first) on the same work folder, let them argue until they agree or state the disagreement precisely in `04-critique.md`, then return to this session and continue. Nobody edits code in the debate.
4. **L, optional second opinion:** offer a review from a different model (Codex or Gemini CLI, read-only, prompt via stdin). Run it only on an explicit yes, after checking the CLI exists and confirming the exact command.
5. **G2 checkpoint (M/L).** Show the user the options with your recommendation, the **seams to test**, the test matrix (with the bug each test catches), and the task list (slices, tiers, independent and migration flags). Ask which option and whether seams, tests and slicing are right. Record it.

Done when `04-design.md` is final, every task brief exists, and the ledger has `Stage design: G2 PASS` (or `S: brief-only`).
