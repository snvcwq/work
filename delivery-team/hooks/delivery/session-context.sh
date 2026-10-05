#!/usr/bin/env bash
# SessionStart: in .NET repos, add SDK version, branch and unfinished team ledgers to context.
set -u
. "$(dirname "$0")/lib.sh"
cat >/dev/null
root="$(repo_root)" || exit 0
[ -n "$root" ] && is_dotnet_repo "$root" || exit 0

sdk="$(dotnet --version 2>/dev/null || echo 'not found')"
branch="$(git -C "$root" branch --show-current 2>/dev/null)"
ctx=".NET repo: SDK ${sdk}, branch ${branch:-detached}."
[ -f "$root/CONSTRAINTS.md" ] || ctx="$ctx No CONSTRAINTS.md yet (the team offers setup on first code change)."

active=""
for l in "$root"/.claude/work/*/ledger.md; do
  [ -f "$l" ] || continue
  grep -q '^Stage ship: .*shipped\|discarded' "$l" && continue
  slug="$(basename "$(dirname "$l")")"
  last="$(grep -v '^#' "$l" | grep -v '^\s*$' | tail -n 1 | cut -c1-120)"
  active="${active}
- ${slug}: ${last}"
done
[ -n "$active" ] && ctx="${ctx}
Unfinished team work (say \"continue <slug>\" to resume; after a compaction, the Lead re-reads its ledger first):${active}"

jq -cn --arg c "$ctx" '{hookSpecificOutput:{hookEventName:"SessionStart",additionalContext:$c}}'
