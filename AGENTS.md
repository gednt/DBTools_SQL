# Agent Guidelines

## Worktree-First Rule

**ALL changes MUST be made in a git worktree — never commit directly on `dotnet-core` or any protected branch.**

If you find yourself on a protected branch with uncommitted changes, stash them, create a worktree, and continue there. No exceptions.

### Why Worktrees?
- Keeps `dotnet-core` clean and deployable at all times
- Isolates feature/fix changes for review before merge
- Enables parallel work without conflicts

### Worktree Setup
1. Create a worktree from the target branch: `git worktree add ../feature-name dotnet-core`
2. Implement all changes in the worktree directory
3. Test changes in the worktree (see Docker-Based Testing below)
4. Commit in the worktree, push, and open a PR
5. After PR merge, remove the worktree: `git worktree remove ../feature-name`

### Enforcement Checklist (before every commit)
- [ ] Am I in a worktree, not on `dotnet-core` directly? (`git branch --show-current` must NOT be `dotnet-core`)
- [ ] Have tests passed locally?
- [ ] Is the commit message descriptive?

## Testing Workflow

**All changes MUST be tested in worktrees before becoming branches.**

### Docker-Based Testing
- After code changes are complete in the worktree, test in Docker containers
- Use `docker compose` to spin up database containers (SQLite, SQL Server, PostgreSQL, MySQL)
- Run the test suite against each provider before submitting a PR
- Only create a PR after successful container-based testing

### Running Tests in Docker
Start all database containers and run the full suite:
```bash
# Start all providers
docker compose up -d sqlserver postgres mysql --wait
docker compose up setup-all --abort-on-container-exit --exit-code-from setup-all

# Run unit tests (no database needed)
dotnet test DBTools.sln --no-build --configuration Release --filter "TestCategory!=Integration"

# Run integration tests (requires running containers)
dotnet test DBTools.sln --no-build --configuration Release --filter "TestCategory=Integration"

# Clean up
docker compose down -v --remove-orphans
```

To test a specific provider, change `Provider` in `DBToolsUnitTest/config.json` (`SqlServer`, `PostgreSQL`, `MySQL`, or `SQLite`) and run integration tests.

### Docker Image Management
- Only pull images that are not already cached locally
- Before pulling, check existing images: `docker images`
- If disk space is needed, remove unused images: `docker image prune -a`
- Common images for this project:
  - `mcr.microsoft.com/mssql/server:2022-latest` (SQL Server)
  - `postgres:16-alpine` (PostgreSQL)
  - `mysql:8.0` (MySQL)
  - `alpine:3.19` (setup coordination)

## Git Workflow

```
worktree (feature) → test in docker → push branch → open PR
```

1. Create worktree: `git worktree add ../feature-name dotnet-core`
2. Implement changes in worktree
3. Test locally in worktree
4. Test in Docker containers
5. Commit in worktree and push: `git push -u origin feature-name`
6. Open a PR targeting `dotnet-core`
7. After PR merge, delete worktree: `git worktree remove ../feature-name`

**Never commit directly on `dotnet-core`. Always use a worktree.**
