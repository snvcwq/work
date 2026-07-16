# dashboard

Personal work dashboard. Blazor Server + MongoDB, single user, local only — no auth, no deploy story, no migrations ceremony. Two tabs: **tasks** and **prs**, related one-to-many (a task can have many PRs).

## Running it

MongoDB must be up first (Docker Desktop running):
```
cd dashboard
docker compose up -d
dotnet run
```
App listens on **http://localhost:7404** (hardcoded in `Program.cs` via `UseUrls`, not the launchSettings port). The UI polls the database every 4s, so changes made via the API below show up in the open browser tab without a manual refresh.

## Data model

- `WorkTask` (`Models/WorkTask.cs`): Number, Url, Title, `Status` (`WorkStatus` enum: `Undefined, New, Blocker, PrWaitingForReview, WaitingForRelease, Resolved, Done`), `Priority` (long — higher sorts to the top; new tasks get `max+1` so they land on top; drag-reorder in the UI rewrites priorities).
- `PullRequest` (`Models/PullRequest.cs`): TaskId (FK), Number, Url, Title, `Status` (`PrStatus` enum: `WaitingForReview, NeedsChanges, Completed`).

Named `WorkTask`/`WorkStatus`, not `Task`/`TaskStatus` — the latter would collide with `System.Threading.Tasks.TaskStatus`, which is a global using on the Web SDK.

## API — this is how you (Claude Code) should update the board

Don't touch MongoDB directly from a script; hit these endpoints so `UpdatedAt`/priority stay consistent and the open browser tab reflects it within ~4s. If the app isn't running, start it (`docker compose up -d` then `dotnet run --project dashboard`) before calling these.

**Tasks**
- `GET /api/tasks` — all tasks, sorted by priority desc
- `GET /api/tasks/{id}`, `GET /api/tasks/by-number/{number}`
- `GET /api/tasks/{id}/prs` — PRs for a task
- `POST /api/tasks` — `{ number, url, title, status? }` (status defaults to `Undefined`)
- `PUT /api/tasks/{id}/status` — `{ status }` (enum name, case-insensitive)
- `PUT /api/tasks/reorder` — `{ orderedIds: [...] }`, first id = highest priority
- `DELETE /api/tasks/{id}` — also deletes its PRs

**PRs**
- `GET /api/prs` — all PRs, newest first
- `GET /api/prs/{id}`
- `POST /api/prs` — `{ taskId, url, number, title, status? }` — or pass `taskNumber` instead of `taskId` if you don't have the Mongo id handy (e.g. you only know the Azure DevOps work item number)
- `PUT /api/prs/{id}/status` — `{ status }` (`WaitingForReview`/`NeedsChanges`/`Completed`)
- `DELETE /api/prs/{id}`

## Intended workflow

1. User asks you to pull work items from Azure DevOps → `POST /api/tasks` for each (status `Undefined` or `New` until triaged).
2. User asks you to work a ticket → you create a branch, push, open a PR → `POST /api/prs` linked to the task via `taskNumber`, then `PUT /api/tasks/{id}/status` to `PrWaitingForReview`.
3. Checking Gmail for review comments → update the PR's status (`NeedsChanges` / back to `WaitingForReview`) as the conversation progresses. The user watches this happen live in the browser and talks you through fixes.
4. PR approved and merged → `PUT /api/prs/{id}/status Completed`, then `PUT /api/tasks/{id}/status Done` (or `WaitingForRelease` if it's batched into a release first).

Task/PR priority ordering is for the human's attention — don't reorder it yourself unless explicitly asked; the user drags cards in the UI to say what's most urgent.

Azure DevOps and Gmail MCP servers are not wired up yet — that's a manual follow-up once credentials exist. Until then this workflow is driven by whatever tools *are* available in a given session (e.g. `az boards` CLI, `gh`/`az repos` for git, manual copy-paste from Gmail).
