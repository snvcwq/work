---
name: design-vocabulary
description: Shared design vocabulary (deep modules, seams, adapters, leverage, locality) and ADR test for team architect and critic seats.
user-invocable: false
---

# Design vocabulary

Use these terms exactly.

- **Module**: anything with an interface and an implementation: a method, class, project or vertical slice.
- **Interface**: everything a caller must know: signature, invariants, ordering, error modes, configuration, performance.
- **Depth**: behavior a caller gets per unit of interface learned. **Deep** = small interface, a lot behind it. **Shallow** = interface about as complex as the body.
- **Seam**: where a module's interface lives; where behavior can change without editing that place. Tests cross the same seam callers do.
- **Adapter**: a concrete thing filling a seam (EF repository, HTTP client, in-memory fake).
- **Leverage** (for callers) and **locality** (for maintainers: change and bugs concentrate in one place) are what depth buys.

## Tests of a design
- **Deletion test**: delete the module in your head. Complexity vanishes → it was a pass-through. Complexity reappears across callers → it earns its place.
- **Adapter count**: one adapter is a hypothetical seam; two adapters (production + test fake counts) make it real.
- **Interface as test surface**: if the tests need to reach past the interface, the module has the wrong shape.

## Dependency categories (how a seam gets tested)
1. In-process (pure logic): test directly through the interface.
2. Local-substitutable (database via Testcontainers, in-memory file system): test with the stand-in.
3. Owned remote service: port at the seam, HTTP adapter in production, in-memory adapter in tests.
4. Third-party service: injected port, fake adapter in tests.

## Design it twice
Options are designed under different constraints, so they differ in shape rather than detail:
- **Minimal**: smallest diff, maximum reuse of existing modules.
- **Deep**: fewest entry points, most behavior hidden.
- **Common-caller**: the most frequent call is trivial.
- **Ports & adapters**: for cross-service dependencies (L track).

## ADR test
Write an ADR when all three hold: hard to reverse, surprising without context, the result of a real trade-off. Otherwise the reasoning lives in `04-design.md` only.
