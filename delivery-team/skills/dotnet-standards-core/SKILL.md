---
name: dotnet-standards-core
description: Short C#/.NET build rules for team implementers. Preloaded into implementer seats.
user-invocable: false
---

# .NET standards (build edition)

Write code the reviewers will pass. The full rulebook lives with them; these are the rules you apply while building.

- **Follow the example** the brief cites. Same layering, naming, validation, mapping and error style.
- **Async all the way**, `CancellationToken` passed down every I/O call.
- **Expected failures** return the repo's result type or ProblemDetails; catches handle, rethrow, or convert at a boundary.
- **Time** from `TimeProvider`; files, environment and randomness behind injectable seams when tests need them.
- **Endpoints**: correct status codes, ProblemDetails errors, explicit authorization with ownership checks, validated input, OpenAPI metadata, DTOs out (entities stay in).
- **EF Core**: `AsNoTracking` + `Select` for reads, query count independent of row count, parameterized SQL, one `SaveChanges` or a transaction for writes that belong together, bounded lists.
- **Logging**: message templates with named placeholders; no secrets or PII.
- **Nullable**: resolve warnings in code; a `!` carries a comment proving safety.
- **Smallest change** that meets the brief, in the glossary's terms.
