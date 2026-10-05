#!/usr/bin/env bash
# Shared helpers for the delivery team hooks. Sourced, not executed.

# Repo root of the current directory, or empty when not in a git repo.
repo_root() { git rev-parse --show-toplevel 2>/dev/null; }

# True when the directory looks like a .NET repo.
is_dotnet_repo() {
  local root="${1:-$(repo_root)}"
  [ -n "$root" ] || return 1
  find "$root" -maxdepth 4 \( -name '*.sln' -o -name '*.slnx' -o -name '*.csproj' \) -not -path '*/bin/*' -not -path '*/obj/*' -print -quit 2>/dev/null | grep -q .
}

# True when a path belongs to a test project or test folder.
is_test_path() {
  printf '%s' "$1" | grep -Eiq '(^|/)(tests?|specs?)(/|$)|(^|/)[^/]*\.(unit|integration|functional|acceptance|architecture|e2e)?tests?(/|$)|(^|/)[^/]*tests?\.cs$'
}

# Source files changed and not yet committed (tracked + untracked), relative to repo root.
changed_files() {
  local root; root="$(repo_root)" || return 0
  { git -C "$root" diff --name-only HEAD 2>/dev/null; git -C "$root" ls-files --others --exclude-standard 2>/dev/null; } | sort -u
}

# Nearest .csproj walking up from a file path (absolute), or empty.
nearest_csproj() {
  local d; d="$(dirname "$1")"
  while [ "$d" != "/" ] && [ -n "$d" ]; do
    local p; p="$(find "$d" -maxdepth 1 -name '*.csproj' -print -quit 2>/dev/null)"
    [ -n "$p" ] && { printf '%s' "$p"; return; }
    d="$(dirname "$d")"
  done
}

# Block a tool call with a reason (PreToolUse / Stop: exit 2 feeds stderr to Claude).
block() { printf '%s\n' "$*" >&2; exit 2; }
