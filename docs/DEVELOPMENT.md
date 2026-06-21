<!-- generated-by: gsd-doc-writer -->
# Development Guide

Local development setup, build commands, and contribution workflow for the DBTools_SQL library.

## Local Setup

### Prerequisites

- [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0) or later (CI uses `8.0.x`)
- [Git](https://git-scm.com/)
- [Docker](https://www.docker.com/) and Docker Compose — required only for integration tests (SQL Server container)

### Clone and install

1. Clone the repository:

```bash
git clone https://github.com/gednt/DBTools_SQL.git
cd DBTools_SQL
```

2. Restore NuGet packages:

```bash
dotnet restore DBTools.sln
```

3. Create local configuration files (not tracked in git):

```bash
cp DBTools/config.json.example DBTools/config.json
cp DBToolsUnitTest/config.json.example DBToolsUnitTest/config.json
```

Edit `DBTools/config.json` with your local database connection details. For integration tests, the defaults in `DBToolsUnitTest/config.json.example` match the Docker Compose SQL Server setup (`127.0.0.1`, database `testDB`, user `testUser`).

4. Build the solution:

```bash
dotnet build DBTools.sln --configuration Release
```

5. Run unit tests (no database required):

```bash
dotnet test DBTools.sln --no-build --configuration Release --filter "TestCategory!=Integration"
```

For integration tests, start SQL Server via Docker Compose first:

```bash
docker compose up -d sqlserver --wait
docker compose up sqlserver-setup --abort-on-container-exit --exit-code-from sqlserver-setup
dotnet test DBTools.sln --no-build --configuration Release --filter "TestCategory=Integration"
docker compose down -v
```

See [docs/CONFIGURATION.md](CONFIGURATION.md) for connection settings, dependency injection options, and per-environment overrides. See [docs/QUICKSTART.md](QUICKSTART.md) for a first-run tutorial using the library.

### Pack locally

To produce a NuGet package without publishing:

```bash
dotnet pack DBTools/DBTools.csproj -c Release -o ./artifacts
```

The repository's `nuget.config` points to nuget.org only. To consume packages from GitHub Packages during development, copy `nuget.config.github-packages.example` and add credentials as described in that file.

## Build Commands

| Command | Description |
|---------|-------------|
| `dotnet restore DBTools.sln` | Restore NuGet dependencies for the library and test projects |
| `dotnet build DBTools.sln` | Build both `DBTools` and `DBToolsUnitTest` (Debug by default) |
| `dotnet build DBTools.sln --configuration Release` | Release build (used in CI) |
| `dotnet build DBTools/DBTools.csproj -c Release --no-incremental` | Rebuild the library project only; CI uses this to verify deterministic output |
| `dotnet test DBTools.sln --no-build --configuration Release --filter "TestCategory!=Integration"` | Run unit tests only (fast path, no Docker) |
| `dotnet test DBTools.sln --no-build --configuration Release --filter "TestCategory=Integration"` | Run integration tests (requires SQL Server via Docker Compose) |
| `dotnet test DBTools.sln --no-build --configuration Release` | Run the full test suite |
| `dotnet pack DBTools/DBTools.csproj -c Release -o ./artifacts` | Create a `.nupkg` in `./artifacts` |
| `docker compose up -d sqlserver --wait` | Start the SQL Server container for integration tests |
| `docker compose up sqlserver-setup --abort-on-container-exit --exit-code-from sqlserver-setup` | Create `testDB`, `testUser`, and apply the integration schema |
| `docker compose down -v` | Stop and remove integration-test containers and volumes |

CI runs on pushes and pull requests to `dotnet-core`, `main`, `master`, and `develop`. The default branch is `dotnet-core`. Release builds are triggered by `v*` tags via `.github/workflows/release.yml`.

## Code Style

No automated linter or formatter is configured in this repository. There is no `.editorconfig`, StyleCop, or `dotnet format` configuration committed to the repo.

Contributors should follow the conventions observed in the existing codebase:

- **Language:** C# with `LangVersion=latest`, target framework `net8.0`
- **Nullable reference types:** Disabled project-wide (`<Nullable>disable</Nullable>`) — do not add `?` annotations to match existing code
- **Braces:** Allman style (opening brace on its own line); always use braces for single-statement blocks
- **Indentation:** 4 spaces, no tabs
- **Naming:** PascalCase for types, methods, and public members; camelCase for locals and parameters; `_underscore` prefix for private fields; `I` prefix for interfaces; `Async` suffix for async methods
- **Documentation:** XML doc comments (`<summary>`, `<param>`, `<returns>`) on public types and methods
- **Legacy API:** Do not introduce new camelCase public methods; legacy names like `getHost` and `connectDB` are preserved for backward compatibility only

New public APIs should use PascalCase and include XML documentation. Match the patterns in `DBTools/Core/SqlClient.cs`, `DBTools/Abstractions/`, and existing test files under `DBToolsUnitTest/`.

## Branch Conventions

No formal branch naming policy is documented in the repository. Recent branches follow these patterns:

| Pattern | Example | Typical use |
|---------|---------|-------------|
| `feature/<name>` | `feature/docs-update` | New features or documentation work |
| `feat/<name>` | `feat/future-enhancements` | Feature development |
| `fix/<name>` | — | Bug fixes |
| `chore/<name>` | — | Tooling, packaging, maintenance |
| `ci/<name>` | — | CI pipeline changes |
| `copilot/<name>` | `copilot/update-to-dotnet-8` | Automated or assisted changes |

The default branch is **`dotnet-core`**. CI also accepts pull requests against `main`, `master`, and `develop`.

## PR Process

There is no `CONTRIBUTING.md` or pull request template in this repository. When submitting a pull request:

1. **Branch from `dotnet-core`** (or the branch you are targeting) using a descriptive name such as `feature/my-change` or `fix/issue-description`.
2. **Keep changes focused** — one logical change per pull request when possible.
3. **Run unit tests locally** before opening the PR:

```bash
cp DBTools/config.json.example DBTools/config.json
cp DBToolsUnitTest/config.json.example DBToolsUnitTest/config.json
dotnet build DBTools.sln --configuration Release
dotnet test DBTools.sln --no-build --configuration Release --filter "TestCategory!=Integration"
```

4. **Mark integration tests appropriately** — tests that require a live database must use `[TestCategory("Integration")]` so CI can filter them. See `DBToolsUnitTest/IntegrationTestBase.cs` for the integration test pattern.
5. **Ensure CI passes** — the `unit-tests` job runs on every PR; the `integration-tests` job runs SQL Server via Docker Compose and executes integration-category tests.
6. **Write clear commit messages** — recent history uses conventional prefixes such as `fix:`, `ci:`, `chore:`, and `docs:` (for example, `fix: seed Users/Orders schema for integration tests in CI`).

Releases are published to GitHub Packages when a `v*` tag is pushed. Package version is defined in `DBTools/DBTools.csproj` (`<Version>`, `<PackageVersion>`).
