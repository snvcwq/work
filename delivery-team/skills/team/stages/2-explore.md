# Stage 2: Explore → G1

1. Dispatch `code-explorer` seats in one message so they run in parallel: S ×1 (lens A), M ×2 (A, B), L ×3 (A, B, C).
   - A: entry points and execution flow for each criterion; also the baseline.
   - B: data, contracts, configuration and their consumers.
   - C: tests, conventions and the closest existing features.
2. **G1:** `02-baseline.md` holds the build and full test run from before any change, with pre-existing failures listed by name. A failing build stops the run: show the user.
3. Specialists from the roster run next, in parallel, in `advise` mode (`03-<role>.md`). On S track, skip them unless the roster insists.

Done when every criterion has a traced flow and a before-state in some `02-impact-*.md`, and the baseline is recorded.
