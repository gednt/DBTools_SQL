#!/usr/bin/env bash
# Configure GitHub Actions to publish DBTools packages to NuGet.org on tag push (v*).
#
# Prerequisites:
#   1. NuGet.org account with permission to push package id "DBTools"
#   2. API key from https://www.nuget.org/account/apikeys
#      (scope: Push, glob: DBTools, expiration as you prefer)
#   3. GitHub CLI authenticated: gh auth login
#
# Usage:
#   ./scripts/configure-nuget-deploy.sh
#   NUGET_API_KEY=your-key ./scripts/configure-nuget-deploy.sh

set -euo pipefail

REPO="${GITHUB_REPOSITORY:-gednt/DBTools_SQL}"
SECRET_NAME="NUGET_API_KEY"

if ! command -v gh >/dev/null 2>&1; then
  echo "Error: GitHub CLI (gh) is required. Install from https://cli.github.com/" >&2
  exit 1
fi

if ! gh auth status >/dev/null 2>&1; then
  echo "Error: Run 'gh auth login' first." >&2
  exit 1
fi

if [[ -z "${NUGET_API_KEY:-}" ]]; then
  read -rsp "Paste NuGet.org API key (input hidden): " NUGET_API_KEY
  echo
fi

if [[ -z "$NUGET_API_KEY" ]]; then
  echo "Error: NUGET_API_KEY cannot be empty." >&2
  exit 1
fi

echo "Setting repository secret ${SECRET_NAME} on ${REPO}..."
printf '%s' "$NUGET_API_KEY" | gh secret set "$SECRET_NAME" --repo "$REPO"

echo "Done. Pushing a tag like v1.4.0 triggers .github/workflows/release.yml:"
echo "  git tag v1.4.0"
echo "  git push origin v1.4.0"
