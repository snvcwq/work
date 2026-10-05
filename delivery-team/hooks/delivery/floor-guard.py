#!/usr/bin/env python3
"""Floor guard: scan a diff for moves that lower the quality bar (.NET edition).

Usage:
  floor-guard.py --since-task-base      uncommitted changes vs HEAD (+ untracked)   [Stop hook]
  floor-guard.py --since-base [REF]     everything since the merge base with REF (default: origin/HEAD, main, master)

Exit codes: 0 clean, 2 violations (blocks a Stop hook and tells the agent), 1 could not run.
Reports the rule and location, never secret values.
"""
import json, os, re, select, subprocess, sys

def git(*args, check=True):
    r = subprocess.run(["git", *args], capture_output=True, text=True)
    if check and r.returncode not in (0, 1):
        raise RuntimeError(r.stderr.strip() or "git failed")
    return r.stdout

def read_hook_input():
    try:
        if select.select([sys.stdin], [], [], 0.0)[0]:
            data = sys.stdin.read()
            return json.loads(data) if data.strip() else {}
    except Exception:
        pass
    return {}

def is_test(path):
    return re.search(r'(^|/)(tests?|specs?)(/|$)|(^|/)[^/]*\.(unit|integration|functional|acceptance|architecture|e2e)?tests?(/|$)|tests?\.cs$', path, re.I) is not None

SUPPRESS = re.compile(r'#pragma\s+warning\s+disable|\[\s*(assembly:\s*)?SuppressMessage|<NoWarn>|//\s*ReSharper\s+disable|#nullable\s+disable|\[\s*ExcludeFromCodeCoverage|<TreatWarningsAsErrors>\s*false|<Nullable>\s*disable')
SKIP = re.compile(r'\[\s*(Fact|Theory|Test|TestMethod)\s*\([^)]*Skip\s*=|\[\s*Ignore\b|Assert\.Inconclusive|\[\s*Explicit\b|\.Skip\(\s*"|Skip\.If')
STUB = re.compile(r'throw\s+new\s+NotImplementedException|catch\s*(\([^)]*\))?\s*\{\s*\}|\bTODO\b(?!.*(#\d+|[A-Z]+-\d+))')
ASSERT = re.compile(r'\bAssert\.|\.Should\(\)|\bVerify\(|\bExpect\(|Assert\.That')

def main():
    hook = read_hook_input()
    args = sys.argv[1:]
    try:
        root = git("rev-parse", "--show-toplevel").strip()
    except Exception:
        return 0  # not a git repo: nothing to guard
    if not root:
        return 0
    os.chdir(root)
    try:
        if "--since-base" in args:
            i = args.index("--since-base")
            ref = args[i + 1] if i + 1 < len(args) and not args[i + 1].startswith("--") else None
            candidates = [ref] if ref else ["origin/HEAD", "origin/main", "main", "origin/master", "master"]
            base = None
            for c in candidates:
                b = git("merge-base", c, "HEAD", check=False).strip()
                if b:
                    base = b; break
            if not base:
                print("floor-guard: could not find a merge base", file=sys.stderr); return 1
            diff = git("diff", "--unified=0", base, "--")
        else:
            diff = git("diff", "--unified=0", "HEAD", "--")
        untracked = [f for f in git("ls-files", "--others", "--exclude-standard").splitlines() if f]
    except Exception as e:
        print(f"floor-guard: could not run: {e}", file=sys.stderr); return 1

    added, removed, deleted = [], [], []
    cur = old = ""; in_header = False
    for line in diff.splitlines():
        if line.startswith("diff "):
            in_header = True
        elif line.startswith("@@"):
            in_header = False
        elif in_header and line.startswith("--- "):
            old = re.sub(r'^[ab]/', '', line[4:])
        elif in_header and line.startswith("+++ "):
            new = re.sub(r'^[ab]/', '', line[4:])
            cur = old if new == "/dev/null" else new
            if new == "/dev/null":
                deleted.append(old)
        elif not in_header and line.startswith("+"):
            added.append((cur, line[1:]))
        elif not in_header and line.startswith("-"):
            removed.append((cur, line[1:]))
    for f in untracked:
        if f.endswith((".cs", ".csproj", ".props", ".targets", ".razor")) and os.path.isfile(f):
            with open(f, encoding="utf-8", errors="replace") as fh:
                for l in fh:
                    added.append((f, l.rstrip("\n")))

    findings = []
    def flag(rule, f, text):
        findings.append((rule, f, text.strip()[:120]))

    code_exts = (".cs", ".csproj", ".props", ".targets", ".razor", ".editorconfig")
    for f, t in added:
        if f.endswith(code_exts) or f.endswith("Directory.Build.props"):
            if SUPPRESS.search(t): flag("silenced-checker", f, t)
            if f.endswith((".cs", ".razor")):
                if SKIP.search(t): flag("test-made-easier", f, t)
                if STUB.search(t) and not is_test(f): flag("unfinished-work", f, t)
        if f.endswith("BannedSymbols.txt"):
            pass  # adding bans tightens: silent
    for f in deleted:
        if f.endswith(".cs") and is_test(f): flag("test-deleted", f, "file deleted")
    for f, t in removed:
        if f.endswith(".cs") and is_test(f) and f not in deleted and ASSERT.search(t):
            flag("assertion-removed", f, t)
        if f.endswith("BannedSymbols.txt") and t.strip() and not t.strip().startswith("#"):
            flag("ban-removed", f, t)
        if os.path.basename(f) == "CONSTRAINTS.md" and t.strip() and not t.startswith("Last reviewed"):
            flag("constraints-loosened-or-changed", f, t)

    if not findings:
        print("floor-guard: clean")
        return 0
    stop_active = bool(hook.get("stop_hook_active"))
    if stop_active:
        # Second stop attempt: let the seat stop (no loop) and leave the findings for the Lead.
        os.makedirs(os.path.join(root, ".claude", "work"), exist_ok=True)
        with open(os.path.join(root, ".claude", "work", ".floor-guard-findings.txt"), "w") as fh:
            for rule, f, text in findings:
                fh.write(f"[{rule}] {f}: {text}\n")
        return 0
    out = sys.stderr
    print(f"floor-guard: {len(findings)} move(s) that lower the quality bar:", file=out)
    for rule, f, text in findings:
        print(f"  [{rule}] {f}: {text}", file=out)
    print("\nFix the code instead, or report it in your output with the reason so the reviewer and the Lead can rule on it"
          + (" (this is the second stop attempt: report it as DONE_WITH_CONCERNS now)." if stop_active else "."), file=out)
    return 2

if __name__ == "__main__":
    sys.exit(main())
