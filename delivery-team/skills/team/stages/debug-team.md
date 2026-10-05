# team debug: competing-hypotheses team

For hard bugs where the cause is unclear: several investigators each own a theory and try to disprove the others'. A single investigator tends to stop at the first plausible theory.

1. **Check** that agent teams are enabled (`CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1`) and the session is interactive. If not, tell the user to start a separate session with `CLAUDE_CODE_EXPERIMENTAL_AGENT_TEAMS=1 claude` for the debug team, or fall back to one `debugger` subagent. Never run a normal team task in a session with teams on: named subagents would start as teammates without their skills and hooks.
2. **Frame**: work folder `.claude/work/debug-<slug>/`. Write `00-symptom.md` with the user's exact symptom, when it started, environment, and what's already been ruled out. Ask the user one round of questions if the symptom is vague.
3. **Hypotheses**: list 3–5 plausible, mutually exclusive causes from the symptom and recent changes (`git log`). Show them to the user; they often re-rank instantly.
4. **Spawn** one teammate per hypothesis using the `debugger` agent type, mode `team`, named `inv-<n>`, each with its hypothesis and the work folder. Teammates load skills from project and user settings, not from their agent definitions, so start each one's prompt with: "Read ~/.claude/skills/delivery-protocol/SKILL.md and ~/.claude/skills/test-discipline/SKILL.md first."
5. **Run**: teammates message each other directly to challenge evidence. Check in every few minutes; redirect anyone stuck on a theory others have disproved.
6. **Converge** when two investigators agree they can disprove the rest. The surviving investigator writes the regression test and recommended fix in `reports/debug-final.md`.
7. **Shut the team down**, then continue as a normal team task (S track) with the root cause as AC-0, or hand the report to the user.

Nobody edits production code in this mode: investigation and tests only.
