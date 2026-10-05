# Playbook: Quick

For edits that don't change behavior: comments, XML docs, wording, formatting, a rename inside one file, a typo. You do it directly, like normal Claude Code: no work folder, no agents.

1. Confirm it really is behavior-neutral. If the edit changes what the code does (even one condition), switch to **Small change** and say so.
2. Make the edit. For comments: match the file's existing style; comments explain *why*, not what the code already says. When removing comments, keep license headers, `// TODO(#issue)` items and anything explaining a non-obvious decision unless the user said otherwise; list what you kept.
3. If a `.cs` file changed, build the containing project (`dotnet build <project>`) and run `dotnet format whitespace --include <files> --verify-no-changes`. Quote the result.
4. Show the user the diff summary (files, lines). Nothing is committed unless they ask.
