---
name: dashboard-standup
description: Write today's entry on the dashboard standup log (localhost:7404) — a terse, report-to-team-lead summary of what actually got done. Use at the end of a work session, or whenever something ships (a PR merged, a task finished) that's worth a standup mention. Standups are Tuesday/Thursday, but the note is per calendar day.
---

Base URL `http://localhost:7404`, date is today in `yyyy-MM-dd`.

1. `GET /api/standup` and check today's entry (if any), so you don't repeat something already logged earlier this session.
2. Write 1–4 short bullet lines for what you actually finished — pull from what you did this session, not a plan for later. Report-ready phrasing: "fixed the flaky CheckoutFlowTests repro", "opened PR #123 for task #4790", "merged and released #5071" — not "started investigating" or internal process narration (stage changes already cover that, on the task's own page — this is the human-facing rollup).
3. Append, don't overwrite: `POST /api/standup/{date}/append` — `{ "text": "- line one\n- line two" }`. Using `PUT` instead would erase anything already written today (by you earlier, or by the user directly).
4. If literally nothing shipped this session, don't append a placeholder — an empty/missing day is more honest than "no updates."
