# .NET delivery team for Claude Code

A Lead skill (`team`) that runs 15 agents (analyst, explorer, architect, critic, implementer, test engineer, reviewer, researcher and specialists) for C#/.NET work: features, bug fixes, investigations, refactors, PR reviews, review comments, upgrades.

Design and full prompts: https://claude.ai/artifact/JQdC8Npmpv5Rhm86MY8RNJ

## Install (per computer)

```bash
git clone https://github.com/snvcwq/work.git
bash work/delivery-team/install.sh
```

On Windows, run it from **Git Bash** (Claude Code runs hooks through Git Bash too). Then restart Claude Code.

The script copies everything into `~/.claude` and merges `settings.team.json` (hooks, permissions, env) into `~/.claude/settings.json`, making a timestamped backup first. Re-run it after `git pull` to update.

## Requirements

- Claude Code, git, .NET SDK
- `python3` on PATH (several hooks use it). On Windows, if only `python` exists, add a `python3` alias or install Python from python.org with the "python3" launcher.
- `jq` on PATH (guard, format, build-gate and session-context hooks). Windows: `winget install jqlang.jq`.
- Optional: `gh` (PR reviews), `gitleaks` (secret scan in ship evidence)

## Use

Just describe the work in a .NET repo; the `team` skill triggers by itself.

## Layout

| Folder | Installs to |
|---|---|
| `agents/delivery/` | `~/.claude/agents/delivery/` |
| `hooks/delivery/` | `~/.claude/hooks/delivery/` |
| `skills/*` | `~/.claude/skills/*` |
| `settings.team.json` | merged into `~/.claude/settings.json` |

## Editing

Edit the files here, commit, push, then run `install.sh` on each computer. If you edit the installed copies in `~/.claude` instead, copy them back here before committing.
