#!/usr/bin/env bash
# Installs the .NET delivery team into ~/.claude (user level, works in every repo).
# Run from anywhere: bash delivery-team/install.sh   (on Windows: from Git Bash)
# Re-running updates the files; settings.json is backed up and merged, never replaced.
set -euo pipefail
src="$(cd "$(dirname "$0")" && pwd)"
dst="$HOME/.claude"
py="$(command -v python3 || command -v python || true)"
[ -n "$py" ] || { echo "python3 is required (hooks use it too)"; exit 1; }
command -v python3 >/dev/null || echo "warning: hooks call python3, which is not on PATH"
command -v jq >/dev/null || echo "warning: jq is not on PATH; the guard, format, build-gate and session-context hooks need it"

mkdir -p "$dst/agents" "$dst/hooks" "$dst/skills"
rm -rf "$dst/agents/delivery" "$dst/hooks/delivery"
cp -r "$src/agents/delivery" "$dst/agents/"
cp -r "$src/hooks/delivery" "$dst/hooks/"
for s in "$src"/skills/*/; do
  n="$(basename "$s")"; rm -rf "$dst/skills/$n"; cp -r "$s" "$dst/skills/"
done
chmod +x "$dst"/hooks/delivery/*.sh "$dst"/hooks/delivery/*.py

settings="$dst/settings.json"
[ -f "$settings" ] && cp "$settings" "$settings.bak-$(date +%Y%m%d%H%M%S)"
"$py" - "$settings" "$src/settings.team.json" <<'EOF'
import json, os, sys
path, team_path = sys.argv[1], sys.argv[2]
s = json.load(open(path)) if os.path.exists(path) else {}
team = json.load(open(team_path))

hooks = s.setdefault("hooks", {})
for event, groups in team["hooks"].items():
    have = hooks.setdefault(event, [])
    for g in groups:
        if g not in have:
            have.append(g)

perms = s.setdefault("permissions", {})
for kind, rules in team["permissions"].items():
    have = perms.setdefault(kind, [])
    have.extend(r for r in rules if r not in have)

s.setdefault("env", {}).update(team["env"])
json.dump(s, open(path, "w"), indent=2)
EOF

echo "Installed: $(ls "$src/agents/delivery" | wc -l) agents, hooks, $(ls -d "$src"/skills/*/ | wc -l) skills -> $dst"
echo "Restart Claude Code to pick them up."
