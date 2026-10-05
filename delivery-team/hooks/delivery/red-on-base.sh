#!/usr/bin/env bash
# red-on-base.sh <BASE_SHA> <test file>...   (paths relative to the repo root)
# Runs the given new test files against the code at BASE_SHA in a temporary worktree.
# Expected for acceptance tests: they FAIL there (assertion, or compile error naming only new symbols).
set -u
. "$(dirname "$0")/lib.sh"
base="${1:?usage: red-on-base.sh <base> <test files...>}"; shift
[ $# -gt 0 ] || { echo "no test files given" >&2; exit 1; }
root="$(repo_root)"; [ -n "$root" ] || { echo "not in a git repo" >&2; exit 1; }
wt="$(mktemp -d /tmp/red-on-base.XXXXXX)"
cleanup() { git -C "$root" worktree remove --force "$wt" >/dev/null 2>&1; rm -rf "$wt"; }
trap cleanup EXIT
git -C "$root" worktree add --detach "$wt" "$base" >/dev/null 2>&1 || { echo "could not create worktree at $base" >&2; exit 1; }

declare -A projs=(); classes=()
for f in "$@"; do
  [ -f "$root/$f" ] || { echo "missing: $f" >&2; continue; }
  mkdir -p "$wt/$(dirname "$f")"; cp "$root/$f" "$wt/$f"
  p="$(nearest_csproj "$wt/$f")"
  [ -z "$p" ] && { src="$(nearest_csproj "$root/$f")"; [ -n "$src" ] && mkdir -p "$wt/$(dirname "${src#$root/}")" && cp "$src" "$wt/${src#$root/}" && p="$wt/${src#$root/}"; }
  [ -n "$p" ] && projs["$p"]=1
  classes+=("$(basename "$f" .cs)")
done
filter="$(printf 'FullyQualifiedName~%s|' "${classes[@]}")"; filter="${filter%|}"

echo "== red on base ($base) for: ${classes[*]}"
status=0
for p in "${!projs[@]}"; do
  log="$(mktemp)"
  if timeout 900 dotnet test "$p" --filter "$filter" -nologo -v:q >"$log" 2>&1; then
    echo "PASSED on base (expected FAIL): ${p#$wt/}"; status=3
    grep -E 'Passed!|Total tests|Passed ' "$log" | head -20
  else
    if grep -q 'error CS' "$log"; then
      echo "COMPILE ERROR on base: check that every error names only symbols the design introduces:"
      grep -oE "error CS[0-9]+: [^[]+" "$log" | sort -u | head -30
    else
      echo "FAILED on base (expected):"
      grep -E 'Failed |Passed |Total tests|Error Message' "$log" | head -40
    fi
  fi
  rm -f "$log"
done
exit $status
