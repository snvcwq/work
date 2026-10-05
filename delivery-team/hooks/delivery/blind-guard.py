#!/usr/bin/env python3
"""PreToolUse(Read|Grep|Glob|Bash|LSP) on the test engineer: while .claude/work/.blind exists,
block reading the production files the current task changed, so acceptance tests come from the spec.
Exit 2 blocks the call and explains why. Inactive (exit 0) when the flag file is absent.
"""
import json, os, re, subprocess, sys

def is_test(path):
    return re.search(r'(^|/)(tests?|specs?)(/|$)|(^|/)[^/]*\.(unit|integration|functional|acceptance|architecture|e2e)?tests?(/|$)|tests?\.cs$', path, re.I) is not None

def main():
    data = json.loads(sys.stdin.read() or "{}")
    cwd = data.get("cwd") or os.getcwd()
    root = subprocess.run(["git", "-C", cwd, "rev-parse", "--show-toplevel"], capture_output=True, text=True).stdout.strip()
    if not root or not os.path.exists(os.path.join(root, ".claude", "work", ".blind")):
        return 0
    out = subprocess.run(["git", "-C", root, "diff", "--name-only", "HEAD"], capture_output=True, text=True).stdout.splitlines()
    out += subprocess.run(["git", "-C", root, "ls-files", "--others", "--exclude-standard"], capture_output=True, text=True).stdout.splitlines()
    hidden = {f for f in out if f and not is_test(f) and not f.startswith(".claude/") and f.endswith((".cs", ".razor", ".cshtml"))}
    if not hidden:
        return 0
    tool = data.get("tool_name", "")
    ti = data.get("tool_input", {})

    def deny(what):
        print("Blind mode: you are writing acceptance tests from the spec, so the code this task changed is hidden.\n"
              f"Blocked: {what}\nUse the spec, the design's seams and signatures, the brief's Behavior section, existing tests, "
              "and compiler or test output. If the spec doesn't determine an expected value, return NEEDS_CONTEXT.", file=sys.stderr)
        sys.exit(2)

    def rel(p):
        return os.path.relpath(os.path.abspath(os.path.join(cwd, p)), root)

    if tool == "Read":
        if rel(ti.get("file_path", "")) in hidden:
            deny(f"reading {rel(ti.get('file_path',''))}")
    elif tool == "Grep":
        p = ti.get("path") or "."
        rp = rel(p)
        if rp in hidden:
            deny(f"searching inside {rp}")
        if ti.get("output_mode") == "content" and not (is_test(rp) or rp.startswith(".claude")):
            deny("content search outside test folders (use output_mode files_with_matches, or search test folders)")
    elif tool == "Bash":
        c = ti.get("command", "")
        if re.search(r'git\s+(diff|show|log\s+[^|;&]*-p|stash\s+show|blame)', c):
            deny("git commands that show the change")
        for h in hidden:
            if h in c or os.path.basename(h) in c:
                deny(f"a command touching {h}")
    return 0

if __name__ == "__main__":
    sys.exit(main())
