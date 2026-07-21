---
name: dashboard-triage
description: Pull work items into the dashboard (localhost:7404) — from Azure DevOps, a manual list, or wherever the user names a batch of tickets to track. Use when the user hands you new work to start tracking, not for updating a task already on the board (see dashboard-update for that).
---

Base URL `http://localhost:7404`.

1. Check it's not already tracked: `GET /api/tasks/by-number/{number}`. Skip creating a duplicate if found — if it needs updating instead, use `dashboard-update`.
2. Create it: `POST /api/tasks` — `{ "number": "...", "url": "...", "title": "...", "status": "New", "type": "..." }`
   - `type` (set once — this cannot be casually changed later): `Task`, `Bug`, `UserStory`, `Feature`, or `Epic`, matching the source system's work item type. Omit/`Undefined` if genuinely unknown.
   - `status`: `New` if it's ready to work, `Undefined` if it still needs the user to triage/prioritize it first.
3. Don't touch priority. New tasks land on top automatically (highest priority); don't reorder — that ordering is the user's own signal for what's urgent, set by dragging in the UI.
4. If the user is bulk-importing a sprint's worth, do all the `GET` existence-checks and `POST` creates, then report a short summary (created N, skipped M already-tracked) rather than narrating each call.
