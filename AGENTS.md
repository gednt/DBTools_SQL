# Agent Guidelines

## Testing Workflow

**All changes MUST be tested in worktrees before becoming branches.**

### Worktree Setup
- Create a new worktree for each feature/fix: `git worktree add ../feature-name`
- Implement and test changes in the worktree
- After testing passes, changes become branches (for PR/review)
- Do NOT merge directly to main from a worktree

### Docker-Based Testing
- After code changes are complete in the worktree, test in Docker containers
- Use `docker-compose` to spin up database containers (SQLite, SQL Server, PostgreSQL, MySQL)
- Run the test suite inside the container environment
- Only create a branch after successful container-based testing

### Docker Image Management
- Only pull images that are not already cached locally
- Before pulling, check existing images: `docker images`
- If disk space is needed, remove unused images: `docker image prune -a`
- Common images for this project:
  - `mcr.microsoft.com/mssql/server` (SQL Server)
  - `postgres` (PostgreSQL)
  - `mysql` (MySQL)
  - `alpine` or `ubuntu` (for SQLite/testing)

## Git Workflow

```
worktree (feature) → test in docker → create branch
```

1. Create worktree: `git worktree add ../feature-name`
2. Implement changes in worktree
3. Test locally in worktree
4. Test in Docker containers
5. Create branch: `git checkout -b feature-name` from main
6. Commit and push changes to origin: `git push -u origin feature-name`
7. Delete worktree after branching: `git worktree remove feature-name`
