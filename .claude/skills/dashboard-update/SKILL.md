---
name: dashboard-update
description: Report progress to the dashboard (localhost:7404) while working a task — from any repo, not just dashboard/. Use whenever you start a new phase (coding/testing/reviewing/...), hit a real checkpoint (PR opened/approved/merged), need the user's input and they aren't watching, or something ships worth a standup mention. Self-contained — doesn't require dashboard/CLAUDE.md to be loaded.
---

Base URL `http://localhost:7404`. If unreachable, skip logging rather than block — this is a visibility layer, not a dependency for the actual work. You need the task's Mongo `{id}` to address most of these: `GET /api/tasks/by-number/{number}` if you only have the work-item number.

**Phase changed** (every time you start coding/testing/reviewing/fixing/pushing/etc.):
`PUT /api/tasks/{id}/stage` — `{ "stage": "Coding", "detail": "short specific note", "agent": "your name" }`
Valid stages: `Queued, SyncingToAzure, Coding, CodeReview, Testing, PushingBranch, AwaitingPrReview, FixingReviewComments, Merging, Closing, BlockedOnHuman`. This replaces whatever stage was there — no separate clear step. `stage: null` hands the task back out of the pipeline.

**Real checkpoint reached** (less often than stage — only at points a human actually cares about):
`PUT /api/tasks/{id}/status` — `{ "status": "PrWaitingForReview" }` (also: `PrApproved`, `PrMerged`, `WaitingForRelease`, `Released`, `Done`, `Blocker`)
`PUT /api/prs/{id}/status` — `{ "status": "WaitingForReview" }` (also: `Approved`, `NeedsChanges`, `Completed`)

**Need the user and they're not in an active chat with you right now:**
`POST /api/notify` — `{ "title": "task #4790 needs you", "message": "specific question or decision" }`
If it's about a specific task, also set that task's stage to `BlockedOnHuman` with a `detail` explaining what's needed — do both, they're the same event from two angles. Notify once per block, not repeatedly while waiting.

**Explaining a decision or what you're doing** (for that task's own history, not a global feed):
`POST /api/tasks/{id}/comments` — `{ "author": "your name", "text": "..." }`

**Something shipped** (PR opened, PR merged, task done): also see the `dashboard-standup` skill — append a terse line, don't just rely on the stage/status trail.

Don't reorder task priority — that's the user's own signal for what's urgent, drag-order in the UI. Don't change a task's `type` after creation — it's set once (see `dashboard-triage`).
