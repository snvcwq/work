# dashboard

Personal work dashboard. Blazor Server + MongoDB, single user, local only — no auth, no deploy story, no migrations ceremony. Five tabs: **live** (what's happening right now), **tasks**, **prs**, **events**, **standup**.

## Running it

MongoDB must be up first (Docker Desktop running):
```
cd dashboard
docker compose up -d
dotnet run
```
App listens on **http://localhost:7404** (hardcoded in `Program.cs` via `UseUrls`, not the launchSettings port). The UI polls the database every 4s, so changes made via the API below show up in the open browser tab without a manual refresh. If the app isn't running, start it before calling any endpoint below.

## Data model

- `WorkTask` (`Models/WorkTask.cs`): Number, Url, Title, Assignee.
  - `Status` (`WorkStatus` enum) — the small, human-facing "what's going on" signal. Declared as `Undefined, New, Blocker, PrWaitingForReview, WaitingForRelease, Resolved, Done, Removed, PrApproved, PrMerged, Released` for append-only int-serialization safety (never reorder this list — see the comment in the enum), but shown in the UI in actual pipeline order: `Undefined, New, Blocker, PrWaitingForReview, PrApproved, PrMerged, WaitingForRelease, Released, Resolved, Done, Removed`.
  - `Type` (`WorkItemType` enum: `Undefined, Task, Bug, UserStory, Feature, Epic`) — mirrors the Azure Boards work item type. **Set this once, at creation.** The per-row UI badge is deliberately read-only (`WorkItemTypeDropdown ReadOnly="true"`) — the user asked for type to not be casually re-clickable after the fact. The API technically still allows changing it (`PUT .../type`); only use that to correct a genuine misclassification, not as routine workflow.
  - `Stage`/`StageDetail` (`PipelineStage?` enum, nullable) — **your own** granular progress on a task, separate from `Status`. `null` means no agent has claimed the task. This is what drives the `/live` page and the small chip on each task row.
  - `Priority` (long) — higher sorts to the top. Don't reorder tasks yourself unless explicitly asked; the user drags cards to say what's most urgent, and that ordering is for their attention, not yours.
- `PullRequest` (`Models/PullRequest.cs`): TaskId (FK), Number, Url, Title.
  - `Status` (`PrStatus` enum, declared `WaitingForReview, NeedsChanges, Completed, Approved` — same append-only rule — shown in the UI as `WaitingForReview, Approved, NeedsChanges, Completed`).

Named `WorkTask`/`WorkStatus`, not `Task`/`TaskStatus` — the latter would collide with `System.Threading.Tasks.TaskStatus`, a global using on the Web SDK.

## API reference

Don't touch MongoDB directly from a script — hit these endpoints so `UpdatedAt`, priority, and the event log all stay consistent, and the open browser tab reflects it within ~4s.

### Tasks
- `GET /api/tasks` — all tasks, sorted by priority desc
- `GET /api/tasks/{id}`, `GET /api/tasks/by-number/{number}`
- `GET /api/tasks/{id}/prs` — PRs for a task
- `POST /api/tasks` — `{ number, url, title, status?, type? }` (status/type default to `Undefined`) — pass `type` when you know the Azure Boards work item type
- `PUT /api/tasks/{id}/status` — `{ status }` (enum name, case-insensitive)
- `PUT /api/tasks/{id}/type` — `{ type }` — see the "set once" note above; don't call this as routine workflow
- `PUT /api/tasks/{id}/stage` — `{ stage?, detail?, agent? }` — `stage` omitted/null clears it (hands the task back out of the pipeline); `detail` is a short free-text note shown next to the stage (e.g. `"PR #123 awaiting review"`); `agent` names which automation made the change. **This is a single current-value field, not a list** — setting a new stage automatically replaces whatever stage was there before, so there's nothing separate to "clear" first. Every stage change is auto-logged as a `TaskStageChanged` event, visible on the task's own detail page.
- `PUT /api/tasks/{id}/assignee` — `{ assignee }`
- `POST /api/tasks/{id}/comments` — `{ author?, text }` — leaves a note on the task's own timeline (visible on `/tasks/{id}`). Use this for the *why*/detail behind a decision, not for what a stage/status change already says on its own.
- `PUT /api/tasks/reorder` — `{ orderedIds: [...] }`, first id = highest priority
- `DELETE /api/tasks/{id}` — also deletes its PRs

Each task has a detail page at `/tasks/{id}` (linked from its title in the list): status, stage, linked PRs, and the full chronological timeline of everything above, newest first.

### PRs
- `GET /api/prs` — all PRs, newest first
- `GET /api/prs/{id}`
- `POST /api/prs` — `{ taskId, url, number, title, status? }` — or pass `taskNumber` instead of `taskId` if you don't have the Mongo id handy
- `PUT /api/prs/{id}/status` — `{ status }` (`WaitingForReview`/`Approved`/`NeedsChanges`/`Completed`)
- `DELETE /api/prs/{id}`

### Events
- `GET /api/events?unacknowledged={bool}` — the full, unfiltered log (every event, internal and external) — this is for your own querying/debugging.
- `POST /api/events` — `{ type, summary?, taskId?, taskNumber?, prId?, prNumber?, context? }` — for things you observed **outside your own actions** that the automatic logging above can't detect (e.g. a reassignment or PR approval noticed while polling Gmail/Azure DevOps). This is the only path that marks an event `IsExternal = true`.
- `PUT /api/events/{id}/ack`, `PUT /api/events/ack-all`

The `/events` **tab** (and its unread badge) only shows `IsExternal` events — i.e. only what came in through `POST /api/events`, typically a Gmail-polling agent reporting something it saw outside this app's scope. Every action this app logs automatically on itself (task/PR created, status/stage/assignee changed, comments) is **not** external, so it never appears there, no matter how often tasks are created or updated — that routine activity still has a full history, just not in `/events`: it's on `/live` and on each task's own detail page (`GetEventsForTaskAsync`, which is unfiltered). Don't call `POST /api/events` for your own routine task bookkeeping — use the dedicated task/PR endpoints above, which log internally and correctly stay out of `/events`.

### Standup
Standups happen Tuesday/Thursday. `/standup` is one big free-text note **per day** (not a task list) — the user reads back through it to know what to report.
- `GET /api/standup` — all notes, most recent day first
- `PUT /api/standup/{date}` — `{ text }`, full overwrite of that day's note (date is `yyyy-MM-dd`)
- `POST /api/standup/{date}/append` — `{ text }`, adds a line without touching what's already there — **use this**, not `PUT`, for routine day-of entries
- `DELETE /api/standup/{date}`

### Notify
- `POST /api/notify` — `{ title?, message }` → `202 Accepted`. Fires a native Windows toast notification (in-process, via `Services/DesktopNotifier.cs` — no shelling out). `title` defaults to `"dashboard"` if omitted.

## Operating rules for you (Claude Code)

These four points are the actual point of the dashboard beyond being a data store — they're what make your work visible and interruptible without the user having to sit and watch you.

**1. Notify when you need the user and they aren't already watching.** If you're blocked on a decision, need permission for something consequential, or want advice, and you're working unattended (not in the middle of an active chat with them) — call `POST /api/notify` so it reaches them wherever they are. Don't notify for things already surfaced through your normal interactive tool-permission flow in a live session; that's redundant. Notify **once** per blocking event, not repeatedly while you wait. If the block is about a specific task, also set that task's stage — see rule 3.

**2. Log work at the right layer, not all three for the same fact.**
   - **Stage** (`PUT .../stage`) — machine state: what phase you're in right now. Drives `/live` and the row chip. Cheap, frequent, no prose needed beyond a short `detail`.
   - **Comment** (`POST .../comments`) — narrative: *why* you made a call, what you decided, what you're about to try. Lives on that task's own page for someone reading its history later.
   - **Standup** (`POST /api/standup/{date}/append`) — a terse, human-readable bullet for things that actually shipped or matter for the next standup: a PR opened, a PR merged, a task finished. Not a mirror of every stage change — the person reading it is preparing to tell a team lead what happened, not debugging your process.

**3. Update stage at the start of every phase, and status at the checkpoints that matter.** Starting to write code, run tests, review, fix comments, push, open a PR — each of those is a `PUT .../stage` call (`Coding`, `Testing`, `CodeReview`, `FixingReviewComments`, `PushingBranch`, `AwaitingPrReview`, ...). You don't need to "clear" the previous stage first — see the note in the API reference. `Status` changes less often, at the checkpoints a human actually cares about: PR opened → `PrWaitingForReview`, approved → `PrApproved`, merged → `PrMerged`, released → `Released` (or `Done` if there's no separate release step).

**4. When you finish a stage or a task, that's usually a standup-worthy moment.** PR created, PR merged, task done, a release went out — append a line to today's standup note when these happen, in addition to (not instead of) the stage/status update.

## Example end-to-end workflow

1. User (or a future Azure DevOps integration) hands you a work item → `POST /api/tasks` with `type` if known, status `New`.
2. You start working it → `PUT .../stage { stage: "Coding", detail: "implementing the retry logic" }`.
3. Tests pass, you open a PR → `POST /api/prs` (linked via `taskNumber`), `PUT tasks/{id}/status PrWaitingForReview`, `PUT tasks/{id}/stage { stage: "AwaitingPrReview" }`, and append a standup line: `"opened PR #123 for task #4790"`.
4. A reviewer leaves comments (you notice via Gmail/polling) → `PUT prs/{id}/status NeedsChanges`, `PUT tasks/{id}/stage { stage: "FixingReviewComments" }`.
5. You're unsure how to resolve one comment and it's a real judgment call → `PUT tasks/{id}/stage { stage: "BlockedOnHuman", detail: "retry-with-backoff vs skip-and-log?" }` **and** `POST /api/notify { title: "task #4790 needs you", message: "retry-with-backoff vs skip-and-log?" }`.
6. User answers, you resume → clear the block with a new stage (`FixingReviewComments` again, or whatever's next), push the fix, `PUT prs/{id}/status WaitingForReview`.
7. Approved and merged → `PUT prs/{id}/status Completed`, `PUT tasks/{id}/status PrMerged`, `PUT tasks/{id}/stage { stage: "Merging" }` then clear it (`stage: null`) once done, `PUT tasks/{id}/status Done` (or `WaitingForRelease` → `Released` later if it's batched). Append a standup line: `"merged PR #123, task #4790 done"`.

Azure DevOps and Gmail MCP servers are not wired up yet — that's a manual follow-up once credentials exist. Until then this workflow is driven by whatever tools *are* available in a given session (e.g. `az boards` CLI, `gh`/`az repos` for git, manual copy-paste from Gmail).
