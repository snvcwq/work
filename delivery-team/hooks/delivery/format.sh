#!/usr/bin/env bash
# PostToolUse(Edit|Write) on implementer seats: whitespace-format the edited .cs/.razor file only.
set -u
. "$(dirname "$0")/lib.sh"
input="$(cat)"
f="$(printf '%s' "$input" | jq -r '.tool_input.file_path // empty')"
case "$f" in *.cs|*.razor) ;; *) exit 0 ;; esac
[ -f "$f" ] || exit 0
proj="$(nearest_csproj "$f")"
[ -n "$proj" ] || exit 0
root="$(repo_root)"; [ -n "$root" ] || exit 0
# dotnet format matches --include against paths relative to the working directory.
( cd "$root" && timeout 60 dotnet format whitespace "${proj#$root/}" --include "${f#$root/}" >/dev/null 2>&1 ) || true
exit 0
