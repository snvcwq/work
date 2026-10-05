#!/usr/bin/env python3
"""PreToolUse(Write|Edit): limit where a team seat may write.

Options (globs are relative to the repo root; * matches across folders):
  --only G...           allow only these paths
  --tests-only          allow test projects/folders (plus --also G...)
  --also G...           extra allowed paths for --tests-only
  --deny G...           deny these paths (everything else allowed)
  --deny-acceptance-tests   deny files recorded as acceptance tests by the test engineer
  --allow-debug-tags    with --tests-only: allow production edits whose new text carries a [DEBUG- tag
Exit 2 blocks the write and tells the agent why. Paths outside the repo are blocked for --only/--tests-only.
"""
import fnmatch, json, os, re, subprocess, sys

def is_test(path):
    return re.search(r'(^|/)(tests?|specs?)(/|$)|(^|/)[^/]*\.(unit|integration|functional|acceptance|architecture|e2e)?tests?(/|$)|tests?\.cs$', path, re.I) is not None

def parse(argv):
    opts = {"only": [], "also": [], "deny": [], "tests_only": False, "deny_accept": False, "debug_tags": False}
    key = None
    for a in argv:
        if a == "--only": key = "only"
        elif a == "--also": key = "also"
        elif a == "--deny": key = "deny"
        elif a == "--tests-only": opts["tests_only"] = True; key = None
        elif a == "--deny-acceptance-tests": opts["deny_accept"] = True; key = None
        elif a == "--allow-debug-tags": opts["debug_tags"] = True; key = None
        elif a.startswith("--"): key = None  # unknown flags are ignored
        elif key: opts[key].append(a)
    return opts

def match(rel, globs):
    return any(fnmatch.fnmatch(rel, g) or fnmatch.fnmatch(os.path.basename(rel), g) for g in globs)

def main():
    data = json.loads(sys.stdin.read() or "{}")
    opts = parse(sys.argv[1:])
    ti = data.get("tool_input", {})
    path = ti.get("file_path") or ti.get("notebook_path")
    if not path:
        return 0
    cwd = data.get("cwd") or os.getcwd()
    try:
        root = subprocess.run(["git", "-C", cwd, "rev-parse", "--show-toplevel"], capture_output=True, text=True).stdout.strip()
    except Exception:
        root = ""
    root = root or cwd
    ap = os.path.abspath(os.path.join(cwd, path))
    rel = os.path.relpath(ap, root)
    outside = rel.startswith("..")
    agent = data.get("agent_type", "") or ""
    acc_list = os.path.join(root, ".claude", "work", ".acceptance-files")

    def deny(msg):
        print(f"Blocked by path guard ({agent or 'this seat'}): {msg}\nPath: {rel}", file=sys.stderr)
        sys.exit(2)

    if opts["deny"] and match(rel, opts["deny"]):
        deny("this seat may not edit this file (it belongs to another role).")
    if opts["deny_accept"] and os.path.isfile(acc_list):
        with open(acc_list) as fh:
            if rel in {l.strip() for l in fh if l.strip()}:
                deny("acceptance tests belong to the test engineer. Fix the behavior they check, or report why the test looks wrong.")
    if opts["only"]:
        if outside or not match(rel, opts["only"]):
            deny("this seat may only write: " + ", ".join(opts["only"]))
    if opts["tests_only"]:
        ok = (not outside) and (is_test(rel) or match(rel, opts["also"]))
        if not ok and opts["debug_tags"]:
            new = (ti.get("new_string") or "") + (ti.get("content") or "")
            ok = "[DEBUG-" in new
        if not ok:
            deny("this seat writes tests only (test projects/folders" + (", " + ", ".join(opts["also"]) if opts["also"] else "") + ").")
        # Record acceptance tests written while the blind flag is up.
        if is_test(rel) and "test-engineer" in agent and os.path.exists(os.path.join(root, ".claude", "work", ".blind")):
            os.makedirs(os.path.dirname(acc_list), exist_ok=True)
            with open(acc_list, "a") as fh:
                fh.write(rel + "\n")
    return 0

if __name__ == "__main__":
    sys.exit(main())
