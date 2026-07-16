# repos

Clone your actual work repos here, e.g.:

```
git clone <azure-devops-repo-url> repos/my-service
```

Each repo is independent — Claude Code should treat this folder as a container, not a project itself. When working a ticket, point Claude at the specific `repos/<name>` folder; it will pick up that repo's own conventions (and its own CLAUDE.md if it has one) rather than the daily-driver workspace's.

This folder is gitignored from the `daily-driver` workspace repo, so cloned repos never become nested/embedded git repos.
