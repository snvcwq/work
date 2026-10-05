---
name: dotnet-standards
description: Full C#/.NET review rulebook with citable rule IDs. Preloaded into team reviewers and designers.
user-invocable: false
---

# .NET standards (review edition)

Order of authority: the repo's `CONSTRAINTS.md`, `CLAUDE.md`, `.editorconfig` and analyzers, then this file. Where the repo has an established pattern that differs, the repo wins; note the difference. Cite rule IDs in findings (`ASYNC-2`).

Mechanical rules (marked ⚙) belong in analyzers or the floor guard. When you find one violated that tooling didn't catch, also propose the check in "Out of scope" so a retro can add it.

## Design
- **DES-1** Smallest change that meets every acceptance criterion.
- **DES-2** Reuse the repo's patterns (handlers, result types, validation, mapping) before introducing one. A new pattern needs an ADR.
- **DES-3** Deep modules: small interface, real behavior behind it. Deletion test: removing a pass-through module makes complexity vanish, so it shouldn't exist.
- **DES-4** One adapter means a hypothetical seam. Introduce an interface when two implementations exist (production + test fake counts) or the repo's DI convention requires it.
- **DES-5** Public surface changes (public types, HTTP contracts, message schemas, config keys) are declared in the design.

## C#
- **CS-1** ⚙ Nullable enabled; each `!` suppression carries a comment proving safety.
- **CS-2** `record` for immutable data, `sealed` for classes not designed for inheritance.
- **CS-3** Guard public entry points with `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrEmpty`.
- **CS-4** Names say what a thing does, in the glossary's terms.

## Async
- **ASYNC-1** ⚙ Async all the way; `.Result`, `.Wait()`, `.GetAwaiter().GetResult()` are banned.
- **ASYNC-2** Every async I/O method takes a `CancellationToken` and passes it down; endpoints take the framework's token.
- **ASYNC-3** ⚙ `async void` only for event handlers.
- **ASYNC-4** No `Task.Run` around synchronous I/O in ASP.NET Core.

## Errors
- **ERR-1** ⚙ Catch blocks handle, log-and-rethrow, or convert at a boundary. Empty catches and swallowed `Exception` are defects.
- **ERR-2** Expected failures use the repo's result pattern or ProblemDetails; exceptions mark unexpected states.
- **ERR-3** A fallback value on failure is logged and justified by the spec.

## Seams for tests
- **SEAM-1** ⚙ Time comes from `TimeProvider` (or the repo's clock).
- **SEAM-2** Files, environment, processes and randomness go through injectable abstractions where tests need control.

## HTTP (ASP.NET Core)
- **API-1** Status codes: 201 + Location on create, 204 empty success, 400 validation, 401/403 auth, 404 missing, 409 conflict.
- **API-2** Errors are ProblemDetails (`TypedResults.Problem` / `ValidationProblem` or the repo's equivalent).
- **API-3** Every endpoint has explicit authorization or a justified `AllowAnonymous`.
- **API-4** Input validated at the boundary with the repo's validator.
- **API-5** Existing contracts change only with a version bump or an approved migration path.
- **API-6** New endpoints carry OpenAPI metadata in the repo's style.
- **API-7** ⚙ Outbound HTTP through `IHttpClientFactory` or typed clients.
- **API-8** Response DTOs are explicit types; entities never leave the API.

## EF Core and data
- **EF-1** Read queries use `AsNoTracking()` (or the repo default) and project with `Select`.
- **EF-2** Query count is independent of row count (no N+1, no queries in loops).
- **EF-3** ⚙ SQL is parameterized; `FromSqlRaw`/`ExecuteSqlRaw` with concatenation is a defect.
- **EF-4** Migrations expand before they contract; destructive steps are called out and reversible where possible.
- **EF-5** Writes that must succeed together share one `SaveChanges` or transaction.
- **EF-6** Contested updates use a concurrency token or an explicit strategy.
- **EF-7** Lists are bounded: pagination with a maximum page size.

## Security
- **SEC-1** ⚙ No secrets in code, committed config, logs or test data.
- **SEC-2** Authorization checks resource ownership (no IDOR).
- **SEC-3** No PII or tokens in logs.
- **SEC-4** Untrusted input is validated before SQL, paths, process arguments, redirects, outbound URLs or deserialization.

## Logging
- **LOG-1** ⚙ Message templates (`LogInformation("Order {OrderId} created", id)`), never interpolation.
- **LOG-2** Log at boundaries and failures with debugging context; nothing per item in hot loops.

## Smell baseline (judgment calls; repo rules override)
Mysterious Name · Duplicated Code · Feature Envy · Data Clumps · Primitive Obsession · Repeated Switches · Shotgun Surgery · Divergent Change · Speculative Generality · Message Chains · Middle Man · Refused Bequest. Report as "possible <smell>" with the hunk and the fix.

## Definition of done
Build passes with warnings as errors for changed projects · affected tests pass, full suite before ship · `dotnet format --verify-no-changes` passes · every acceptance criterion maps to a passing test · CONSTRAINTS.md thresholds hold · docs, ADR and changelog updated when behavior or contracts change.
