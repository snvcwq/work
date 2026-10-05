---
name: test-discipline
description: How team agents write and judge .NET tests - developer vs blind acceptance tests, seams, naming the break, hand-derived expectations, red on base / green on head, mutation check. Preloaded into test-writing and reviewing seats.
user-invocable: false
---

# Test discipline

## Two kinds of tests
- **Developer tests** are written by the implementer while building, red to green, for the logic it's writing. They're its feedback loop.
- **Acceptance tests** are written by the test engineer **after** the code exists, **blind**: from the spec, the design's seams and public signatures, never from the implementation. They're the independent check that the feature does what the user asked. The implementer can't edit them.

## Where tests go
- Acceptance tests live at the **seams** approved in `04-design.md § Seams to test`: usually the HTTP endpoint (`WebApplicationFactory`) or a public service interface. The interface is the test surface; a test that reaches past it (private methods, internal collaborators, querying the database instead of reading back through the interface) is the wrong shape.
- Developer tests go where the logic is, through that logic's own public members.

## How many tests
Write the tests the behavior needs and only those. A test earns its place by naming a realistic bug it catches; a test written to satisfy process costs maintenance forever. Trivial code (pass-through properties, plain mapping with no logic) gets no test of its own. Low-risk edge cases share a test or get none, with the reason written in the design.

## What a test is
- **Name the break.** Before writing the body, state which production change should make it fail. The test name states the behavior: `CancelOrder_WhenAlreadyShipped_Returns409`.
- **Derive expectations by hand.** Literal inputs and literal expected outputs, from the spec or a worked example. An expected value computed by the code under test (or its helpers) is a mirror assertion and proves nothing.
- **Exercise the real thing.** Mock only at system boundaries: external services, time, randomness, sometimes the database (prefer Testcontainers or the repo's test database). An assertion on a mock proves the mock exists.
- **One behavior per test**, Arrange / Act / Assert, no loops or branches in the test.
- **Deterministic**: fake `TimeProvider`, fixed data, wait on conditions (polling with a timeout) instead of `Task.Delay`/`Thread.Sleep`, no shared mutable state, no order dependence.

## Proof a test can fail
- **Developer tests**: watch each one fail before the code that makes it pass.
- **Acceptance tests**: **red on base, green on head**. The test fails on the code from before the task (by assertion, or a compile error naming only the new symbols the design introduces) and passes on the new code. A test green on both proves nothing new.

## Mutation check before you finish
For the code under test, imagine: wrong constant or argument, wrong branch, missing side effect, default or empty return, missing validation for null/empty/zero/unauthorized/malformed. At least one test fails for each realistic mutation. A mutation nothing catches marks an unprotected behavior or a tautological test.

## The bar stays where it is
Two kinds of test change:
- **Acceptance tests** belong to the test engineer and change only through a reviewer's ruling ("test bug") or the Lead's. The implementer can't edit them (hook).
- **Existing tests**: mechanical fixes (compile errors, fixture or DI wiring, namespaces) are fine and listed in the report; changes to an assertion, expected value or scenario need a ruling in the ledger.

A failing test gets fixed code, a mechanical fix, or a ruling. Skips (`[Fact(Skip=…)]`, `[Ignore]`, `Assert.Inconclusive`), removed assertions, deleted test files and `[ExcludeFromCodeCoverage]` are flagged by the floor guard and reviewed as bar-lowering.

| Excuse | Reality |
|---|---|
| "The acceptance test is wrong, I'll work around it" | Report it. The reviewer rules code bug or test bug against the spec. |
| "I'll verify through the database, it's simpler" | Read back through the interface. The database check breaks on refactors and misses API bugs. |
| "Mocking the service makes the test fast" | Mock the boundary below it. A mocked collaborator tests the mock. |
| "One test with five asserts covers it" | Five behaviors, five tests. The failing name should tell you what broke. |
