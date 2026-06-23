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
3. Test changes in the worktree (see Testing in Docker below)
4. Commit in the worktree, push, and open a PR
5. After PR merge, remove the worktree: `git worktree remove ../feature-name`

### Enforcement Checklist (before every commit)
- [ ] Am I in a worktree, not on `dotnet-core` directly? (`git branch --show-current` must NOT be `dotnet-core`)
- [ ] Have tests passed inside Docker?
- [ ] Is the commit message descriptive?

## Testing Workflow

**All changes MUST be tested inside Docker containers before submitting a PR. Tests must never be run directly on the host machine.**

### Testing in Docker

All tests — unit and integration — run inside a Docker container that builds the solution and executes `dotnet test`. The container connects to database containers on the Docker network using service hostnames (e.g. `sqlserver` instead of `127.0.0.1`).

#### Step 1: Run the full test suite (unit + integration) against SQL Server

```bash
docker compose build test-runner
docker compose up -d sqlserver --wait
docker compose up sqlserver-setup --abort-on-container-exit --exit-code-from sqlserver-setup
docker compose run --rm test-runner
```

This builds the solution inside Docker, starts SQL Server, and runs all tests.

#### Step 2: Run against a specific provider

Set the `TEST_PROVIDER` environment variable to `SqlServer`, `PostgreSQL`, `MySQL`, or `SQLite`:

```bash
# PostgreSQL
docker compose build test-runner
docker compose up -d postgres --wait
docker compose up postgres-setup --abort-on-container-exit --exit-code-from postgres-setup
docker compose run --rm -e TEST_PROVIDER=PostgreSQL test-runner

# MySQL
docker compose build test-runner
docker compose up -d mysql --wait
docker compose up mysql-setup --abort-on-container-exit --exit-code-from mysql-setup
docker compose run --rm -e TEST_PROVIDER=MySQL test-runner

# SQLite (no database container needed)
docker compose build test-runner
docker compose run --rm -e TEST_PROVIDER=SQLite test-runner
```

#### Step 3: Run against all providers sequentially

```bash
docker compose build test-runner
docker compose up -d sqlserver postgres mysql --wait
docker compose up setup-all --abort-on-container-exit --exit-code-from setup-all

# SQL Server
docker compose run --rm -e TEST_PROVIDER=SqlServer test-runner

# PostgreSQL
docker compose run --rm -e TEST_PROVIDER=PostgreSQL test-runner

# MySQL
docker compose run --rm -e TEST_PROVIDER=MySQL test-runner

# SQLite
docker compose run --rm -e TEST_PROVIDER=SQLite test-runner
```

#### Step 4: Run only unit tests (no database containers needed)

```bash
docker compose build test-runner
docker compose run --rm test-runner --filter "TestCategory!=Integration"
```

#### Step 5: Clean up

```bash
docker compose down -v --remove-orphans
rm -f testDB.db
```

### How it works

The `test-runner` service in `docker-compose.yml`:
1. Builds from `Dockerfile.test` using the .NET 8.0 SDK image
2. Restores NuGet packages, copies source, and builds in Release mode
3. At runtime, runs `scripts/switch-provider.sh` which writes the correct `config.json` based on `TEST_PROVIDER` — using Docker service hostnames (`sqlserver`, `postgres`, `mysql`) instead of `127.0.0.1`
4. Executes `dotnet test` with all arguments passed after `--` in `docker compose run`

### Docker Image Management
- Only pull images that are not already cached locally
- Before pulling, check existing images: `docker images`
- If disk space is needed, remove unused images: `docker image prune -a`
- Common images for this project:
  - `mcr.microsoft.com/dotnet/sdk:8.0` (.NET SDK for test-runner)
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
3. Test in Docker: `docker compose build test-runner && docker compose run --rm test-runner`
4. Commit in worktree and push: `git push -u origin feature-name`
5. Open a PR targeting `dotnet-core`
6. After PR merge, delete worktree: `git worktree remove ../feature-name`

**Never commit directly on `dotnet-core`. Always use a worktree.**