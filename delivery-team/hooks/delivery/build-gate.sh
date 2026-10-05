#!/usr/bin/env bash
# Stop (implementer seats): build the projects touched by uncommitted changes with warnings as errors,
# then run the test projects among them plus test projects that reference them.
# Exit 2 sends the failure back so the seat keeps working.
set -u
. "$(dirname "$0")/lib.sh"
input="$(cat)"
# Avoid loops: if Claude is already continuing because of this hook, let it stop after two tries.
active="$(printf '%s' "$input" | jq -r '.stop_hook_active // false')"
root="$(repo_root)" || exit 0
[ -n "$root" ] && is_dotnet_repo "$root" || exit 0
cd "$root" || exit 0

mapfile -t files < <(changed_files | grep -E '\.(cs|razor|csproj|props|targets|json)$')
[ ${#files[@]} -gt 0 ] || exit 0

declare -A projs=()
for f in "${files[@]}"; do
  [ -e "$f" ] || continue
  case "$f" in *.csproj) projs["$root/$f"]=1; continue ;; esac
  p="$(nearest_csproj "$root/$f")"; [ -n "$p" ] && projs["$p"]=1
done
[ ${#projs[@]} -gt 0 ] || exit 0

# Test projects: touched ones, plus any test project that references a touched project.
declare -A tests=()
for p in "${!projs[@]}"; do is_test_path "${p#$root/}" && tests["$p"]=1; done
while IFS= read -r tp; do
  for p in "${!projs[@]}"; do
    grep -q "$(basename "$p")" "$tp" 2>/dev/null && tests["$tp"]=1
  done
done < <(find "$root" -name '*.csproj' -not -path '*/bin/*' -not -path '*/obj/*' | while read -r x; do is_test_path "${x#$root/}" && echo "$x"; done)

log="$(mktemp)"; fail=""
for p in "${!projs[@]}"; do
  if ! timeout 300 dotnet build "$p" -warnaserror -nologo -clp:NoSummary -v:q >"$log" 2>&1; then
    fail="build failed: ${p#$root/}
$(grep -E 'error|warning' "$log" | sort -u | head -40)"; break
  fi
done
if [ -z "$fail" ]; then
  for t in "${!tests[@]}"; do
    if ! timeout 600 dotnet test "$t" -nologo -v:q --logger "console;verbosity=normal" >"$log" 2>&1; then
      fail="tests failed: ${t#$root/}
$(grep -E '\[FAIL\]|Failed!|error CS|Error Message|Assert\.|Expected:|Actual:|^\s+at .*Tests' "$log" | head -40)"; break
    fi
  done
fi
rm -f "$log"
[ -z "$fail" ] && exit 0
if [ "$active" = "true" ]; then
  # Second stop attempt: let the seat stop (no loop) and leave the failure for the Lead to see.
  mkdir -p "$root/.claude/work" && printf 'Build gate FAILED at %s\n%s\n' "$(date -Is)" "$fail" > "$root/.claude/work/.build-gate-failed.txt"
  exit 0
fi
printf 'Build gate (warnings as errors + affected tests) failed. Fix it before reporting:\n%s\n' "$fail" >&2
exit 2
