
# Playbook: retro

Improve the environment so the next run goes better. Sources: the ledger and work folder of the run the user names (default: the latest), and this session's transcript.

## Look for
- **Fix loops**: which tasks needed 3+ rounds, and why? Ambiguity in the design is the usual cause; propose the spec/design template change that would have prevented it.
- **Mechanical mistakes** a reviewer caught: each becomes a deterministic check (an analyzer rule, a `BannedSymbols.txt` entry, a floor-guard pattern, a CI step), never a new prose rule.
- **Judgment mistakes** a reviewer caught: a `dotnet-standards` rule, a CONSTRAINTS.md row, or a sharper line in the agent prompt that missed it.
- **Navigation**: files agents struggled to find; add a one-line pointer to CLAUDE.md or GLOSSARY.md.
- **No-ops and sediment**: CLAUDE.md or prompt lines that changed nothing; propose deleting them.
- **Tool economy**: expensive repeated calls (re-reading the same files, full test runs where a filter would do).
- **Missing information**: logs, docs or services an agent needed and couldn't reach.
- **Ratchets**: metrics that improved; propose raising the recorded value.

## Output
A list, most severe first: finding → evidence (ledger line or transcript moment) → exact change (file and text) → expected effect. Apply only what the user approves.
