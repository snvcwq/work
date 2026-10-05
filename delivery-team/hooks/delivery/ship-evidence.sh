#!/usr/bin/env bash
# ship-evidence.sh [--full] [--out FILE] [--base REF]
# One evidence run: build (warnings as errors), format check, floor guard since base,
# vulnerable packages, secret scan (if gitleaks is installed), and with --full the whole test suite.
# Prints a summary; writes everything to FILE. Exit 1 if any check fails.
set -u
here="$(cd "$(dirname "$0")" && pwd)"
. "$here/lib.sh"
full=0; out=""; base=""
while [ $# -gt 0 ]; do case "$1" in
  --full) full=1 ;; --out) out="$2"; shift ;; --base) base="$2"; shift ;; esac; shift; done
root="$(repo_root)"; [ -n "$root" ] || { echo "not in a git repo" >&2; exit 1; }
cd "$root"
target="$(find . -maxdepth 2 \( -name '*.slnx' -o -name '*.sln' \) -not -path '*/bin/*' | head -1)"
out="${out:-$root/.claude/work/evidence-$(date +%Y%m%d-%H%M%S).txt}"
mkdir -p "$(dirname "$out")"; : >"$out"
fails=0
run() { # name, command...
  local name="$1"; shift
  { echo "===== $name"; echo "\$ $*"; } >>"$out"
  if "$@" >>"$out" 2>&1; then echo "PASS  $name" | tee -a "$out"; else echo "FAIL  $name" | tee -a "$out"; fails=$((fails+1)); fi
}
run "build (warnings as errors)" dotnet build $target -warnaserror -nologo -v:q
run "format" dotnet format $target --verify-no-changes -v:q
if [ -n "$base" ]; then run "floor guard since $base" python3 "$here/floor-guard.py" --since-base "$base"
else run "floor guard since merge base" python3 "$here/floor-guard.py" --since-base; fi
vuln() { local o; o="$(dotnet list $target package --vulnerable --include-transitive 2>&1)"; printf '%s\n' "$o"; ! printf '%s' "$o" | grep -qiE 'has the following vulnerable|Severity'; }
run "vulnerable packages" vuln
if command -v gitleaks >/dev/null; then run "secret scan" gitleaks detect --redact --no-banner -s "$root"; else echo "SKIP  secret scan (gitleaks not installed)" | tee -a "$out"; fi
if [ $full -eq 1 ]; then
  run "tests (full suite)" dotnet test $target -nologo -v:q
  grep -E 'Passed!|Failed!|Total tests|Skipped' "$out" | tail -5 | sed 's/^/      /'
fi
echo "evidence: $out"
[ $fails -eq 0 ]
