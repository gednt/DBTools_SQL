<!-- generated-by: gsd-doc-writer -->
# Deployment

How DBTools_SQL is built, published, and consumed. This repository ships a **.NET 8 class library** as a NuGet package — there is no standalone application or hosted service to deploy. Docker Compose in this repo exists only for integration testing.

## Table of Contents

1. [Deployment targets](#deployment-targets)
2. [Build pipeline](#build-pipeline)
3. [Environment setup](#environment-setup)
4. [Rollback procedure](#rollback-procedure)
5. [Monitoring](#monitoring)

---

## Deployment targets

| Target | Config / workflow | Purpose |
|--------|-------------------|---------|
| **NuGet.org** | `.github/workflows/release.yml` (job `publish-nuget-org`) | Primary public distribution for `dotnet add package DBTools` |
| **GitHub Packages (NuGet)** | `.github/workflows/release.yml`, `nuget.config.github-packages.example` | Alternate feed for GitHub-authenticated consumers |
| **GitHub Releases** | `.github/workflows/release.yml` (job `github-release`) | Attaches `.nupkg` files to version tags for download |
| **Local NuGet feed** | `dotnet pack` (see [README.md](../README.md#from-source)) | Developer or offline consumption from a local folder |
| **Docker Compose** | `docker-compose.yml` | **Integration testing only** — SQL Server 2022 container for CI and local integration tests, not runtime deployment |

There is no `Dockerfile`, `vercel.json`, `fly.toml`, or other application hosting configuration in this repository. Applications that reference `DBTools` deploy independently using their own platform (IIS, Azure App Service, containers, etc.).

### Publishing a release (maintainers)

Releases are triggered in three ways:

1. **Automatic on merge** — Merging a version bump into `dotnet-core` runs the **Release** workflow directly (branch push on `DBTools.csproj` / `AssemblyInfo.cs`). The **Auto Release Tag** workflow also creates the `v{version}` git tag for GitHub Releases.
2. **Tag push** — Pushing a semver tag matching `v*` (for example `v1.4.1`) also triggers Release. Tags pushed by GitHub Actions using `GITHUB_TOKEN` do **not** re-trigger workflows; use manual tags or rely on the branch push trigger above.
3. **Manual dispatch** — Run the **Release** workflow from the GitHub Actions UI (`workflow_dispatch`) to publish an existing version (for example if `v1.4.1` was tagged but Release did not run).

Before merging a version bump, update `DBTools/DBTools.csproj` (`<Version>`, `<PackageVersion>`, `<AssemblyVersion>`, `<FileVersion>`) and `DBTools/Properties/AssemblyInfo.cs` so the packed artifact matches the tag.

**Required secret for NuGet.org:** Add `NUGET_API_KEY` in the repository secrets ([nuget.org account API keys](https://www.nuget.org/account/apikeys)). Without it, the Release workflow skips NuGet.org publish but still publishes to GitHub Packages.

### Consuming the package (downstream applications)

Install from NuGet.org (recommended):

```bash
dotnet add package DBTools --version 1.4.1
```

Or from GitHub Packages as documented in [README.md](../README.md#from-github-packages). Copy `nuget.config.github-packages.example` to your solution and authenticate with a GitHub PAT (`read:packages` scope).

Package source URL (GitHub): `https://nuget.pkg.github.com/gednt/index.json` (see `nuget.config.github-packages.example`).

---

## Build pipeline

### Continuous integration (`.github/workflows/ci.yml`)

**Triggers:** Push and pull request to `dotnet-core`, `main`, `master`, or `develop`.

| Job | What it runs |
|-----|--------------|
| **Unit Tests** | `dotnet restore` → `dotnet build` (Release) → deterministic build check → `dotnet test --filter "TestCategory!=Integration"` |
| **Integration Tests** | Same restore/build → `docker compose up` (SQL Server + setup) → wait for SQL Server → `dotnet test --filter "TestCategory=Integration"` → `docker compose down -v` |
| **All Tests (Full)** | Same as integration job but runs the full test suite with no filter. Runs only on push to `main` or on `v*` tags. |

All jobs use **.NET 8.0.x** (`actions/setup-dotnet@v4`) on `ubuntu-latest`. Configuration files are prepared by copying `config.json.example` templates before build.

### Release pipeline (`.github/workflows/release.yml`)

**Triggers:** Push of tags matching `v*`, or manual `workflow_dispatch`.

1. **Build, test, and pack** (`build-test-pack` job)
   - Checkout repository
   - Setup .NET 8.0.x
   - Copy `config.json.example` files
   - `dotnet restore DBTools.sln`
   - `dotnet build DBTools.sln --configuration Release -p:ContinuousIntegrationBuild=true`
   - `dotnet test` with `--filter "TestCategory!=Integration"` (unit tests only)
   - `dotnet pack DBTools/DBTools.csproj -c Release -o ./artifacts --no-build`
   - Upload `artifacts/*.nupkg` as a workflow artifact

2. **Publish to GitHub Packages** (`publish-github-packages` job)
   - Runs when triggered by tag push or manual dispatch
   - Downloads the artifact and pushes to `https://nuget.pkg.github.com/{owner}/index.json`

3. **Publish to NuGet.org** (`publish-nuget-org` job)
   - Runs when triggered by tag push or manual dispatch
   - Downloads the artifact and pushes to `https://api.nuget.org/v3/index.json` using the `NUGET_API_KEY` repository secret
   - Skips gracefully if the secret is not configured

4. **Create GitHub release** (`github-release` job)
   - Runs only on tag push (`refs/tags/v*`)
   - Creates a release with `softprops/action-gh-release@v2`, attaches `.nupkg` files, and generates release notes

### Local build and pack

```bash
cp DBTools/config.json.example DBTools/config.json
cp DBToolsUnitTest/config.json.example DBToolsUnitTest/config.json
dotnet restore DBTools.sln
dotnet build DBTools.sln --configuration Release
dotnet test DBTools.sln --configuration Release --filter "TestCategory!=Integration"
dotnet pack DBTools/DBTools.csproj -c Release -o ./artifacts
```

For integration tests locally, start the test database with Docker Compose (see [CONFIGURATION.md](CONFIGURATION.md#4-local-sql-server-for-tests)).

---

## Environment setup

### CI / release (GitHub Actions)

| Variable / secret | Required | Description |
|-------------------|----------|-------------|
| `GITHUB_TOKEN` | Automatic | Used by the release workflow to push packages to GitHub Packages and create releases. Provided by GitHub Actions; no manual secret configuration needed for standard releases. |
| `NUGET_API_KEY` | **Yes** (for nuget.org) | API key from [nuget.org/account/apikeys](https://www.nuget.org/account/apikeys). Required for the `publish-nuget-org` job; skipped if unset. |
| `ContinuousIntegrationBuild=true` | Set in release build | MSBuild property passed during Release configuration builds for reproducible CI output. |

### Docker Compose (integration tests only)

| Variable | Required | Default (in repo) | Description |
|----------|----------|-------------------|-------------|
| `ACCEPT_EULA` | **Yes** | `"Y"` in `docker-compose.yml` | Microsoft SQL Server EULA acceptance |
| `MSSQL_SA_PASSWORD` | **Yes** | `TestStrong!Passw0rd` | SA password for the test SQL Server container |

These variables are defined in `docker-compose.yml` and are **not** used when consuming the NuGet package in production applications.

### Downstream application configuration

DBTools_SQL does not define production environment variables. Consuming applications configure database connectivity through `config.json`, `IConfiguration`, or `DbToolsOptions` as described in [CONFIGURATION.md](CONFIGURATION.md).

For production deployments of **your** application:

- Keep credentials out of source control (`config.json` is gitignored in this repo as a pattern to follow).
- Load secrets from your host's secret manager or environment-specific configuration.
- See [SECURITY.md](SECURITY.md#deployment) for the deployment security checklist.

---

## Rollback procedure

No automated rollback step exists in the CI/CD workflows. Use the approach that matches your situation:

### Consumers pinned to a package version

Pin a known-good version in your `.csproj`:

```xml
<PackageReference Include="DBTools" Version="1.3.0" />
```

Then restore and redeploy your application. This is the fastest rollback path for downstream teams.

### GitHub Packages (maintainers)

1. Identify the last good release tag and `.nupkg` from [GitHub Releases](https://github.com/gednt/DBTools_SQL/releases).
2. If the faulty version must remain unavailable, consumers should avoid referencing it; GitHub Packages does not support trivial "unpublish" in the same way as nuget.org.
3. To re-publish a previous version after a fix, bump to a new patch version (for example `1.4.1`) rather than overwriting an existing package version — the release workflow uses `--skip-duplicate` and will not overwrite existing packages.
4. For a bad GitHub Release only (not the package feed), delete or edit the release from the GitHub repository **Releases** page.

### CI / integration test failures

If Docker-based integration tests fail in CI:

```bash
docker compose down -v --remove-orphans
docker compose up -d sqlserver --wait
docker compose up sqlserver-setup --abort-on-container-exit --exit-code-from sqlserver-setup
```

Re-run tests locally before pushing a fix.

---

## Monitoring

This repository does **not** include application monitoring libraries (no Sentry, Datadog, New Relic, or OpenTelemetry dependencies). Observability for the library itself is limited to:

| Mechanism | What it covers |
|-----------|----------------|
| **GitHub Actions CI** | Build, unit test, and integration test status on every push and PR (`.github/workflows/ci.yml`) |
| **GitHub Actions Release** | Pack and publish success on tag push or manual dispatch |
| **CI badge** | README links to the CI workflow status badge |

For **consuming applications**, [SECURITY.md](SECURITY.md#monitoring) recommends operational practices (database access logging, failed authentication monitoring, dependency vulnerability scanning). Those are implemented in your host application, not in DBTools_SQL.

<!-- VERIFY: Production monitoring dashboards, alert channels, and hosting URLs for downstream applications are defined outside this repository. -->

---

## Related documentation

- [CONFIGURATION.md](CONFIGURATION.md) — Connection settings and per-environment overrides for consuming apps
- [SECURITY.md](SECURITY.md) — Credential handling and deployment security checklist
- [README.md](../README.md) — Package installation from GitHub Packages
- [QUICKSTART.md](QUICKSTART.md) — Local setup and first run
