#!/usr/bin/env bash
# PreToolUse(Bash): block destructive commands. Global, plus --deny-db-writes for the data specialist.
# Input: hook JSON on stdin. Exit 2 blocks the call and tells Claude why.
set -u
input="$(cat)"
cmd="$(printf '%s' "$input" | jq -r '.tool_input.command // empty')"
[ -n "$cmd" ] || exit 0
deny_db=0; [ "${1:-}" = "--deny-db-writes" ] && deny_db=1
# Global use: only act inside .NET repos (agent-scoped use with --deny-db-writes always acts).
if [ $deny_db -eq 0 ]; then . "$(dirname "$0")/lib.sh"; is_dotnet_repo || exit 0; fi

say() { printf 'Blocked by delivery guard: %s\nCommand: %s\n' "$1" "$cmd" >&2; exit 2; }

# Normalise whitespace for matching.
c="$(printf '%s' "$cmd" | tr -s '[:space:]' ' ')"

echo "$c" | grep -Eq 'git push( [^|;&]*)? (-f|--force|--force-with-lease)( |$)|git push .*\+[^ ]' && say "force push is not allowed; push normally after the user approves."
echo "$c" | grep -Eq 'git reset( [^|;&]*)? --hard' && say "git reset --hard discards work; ask the user first."
echo "$c" | grep -Eq 'git clean( [^|;&]*)? -[a-zA-Z]*f' && say "git clean -f deletes untracked files; ask the user first."
echo "$c" | grep -Eq 'git (checkout|restore) ( ?-- )?\.( |$)' && say "discarding all working-tree changes needs the user's OK."
# rm -rf: every target must be inside .claude/work, /tmp, or a bin/obj/TestResults folder.
if ! printf '%s' "$c" | python3 -c '
import re, shlex, sys
c = sys.stdin.read()
ok = re.compile(r"(^|/)(bin|obj|TestResults)/?$|(^|/)\.claude/work(/|$)|^/tmp/")
for seg in re.split(r"[;&|]+", c):
    try: words = shlex.split(seg)
    except ValueError: words = seg.split()
    if not words or words[0] != "rm": continue
    flags = "".join(w[1:] for w in words[1:] if w.startswith("-") and not w.startswith("--"))
    if not ("r" in flags.lower() and "f" in flags): continue
    for t in (w for w in words[1:] if not w.startswith("-")):
        if not ok.search(t.rstrip("/")):
            sys.exit(1)
'; then say "rm -rf outside .claude/work, /tmp, bin, obj or TestResults needs the user's OK."; fi
if echo "$c" | grep -Eq 'dotnet[- ]ef .*database (update|drop)'; then
  [ $deny_db -eq 1 ] && say "this seat never changes databases; generate a script with 'dotnet ef migrations script' instead."
  echo "$c" | grep -Eiq 'localhost|127\.0\.0\.1|\(localdb\)|Data Source=[^;]*\.db' || say "database update/drop against a non-local target needs the user to run it."
fi
exit 0
