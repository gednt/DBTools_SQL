#!/bin/bash
# Switches DBToolsUnitTest/config.json to the provider specified by TEST_PROVIDER.
# Used by the test-runner Docker container.
# Valid values: SqlServer (default), PostgreSQL, MySQL, SQLite.

set -euo pipefail

PROVIDER="${TEST_PROVIDER:-SqlServer}"
CONFIG="/src/DBToolsUnitTest/config.json"

echo "Switching test provider to: ${PROVIDER}"

case "${PROVIDER}" in
  SqlServer)
    cat > "${CONFIG}" <<'EOF'
{
  "Provider": "SqlServer",
  "Host": "sqlserver",
  "Database": "testDB",
  "Uid": "testUser",
  "Password": "Integration!123",
  "Port": "1433"
}
EOF
    ;;
  PostgreSQL)
    cat > "${CONFIG}" <<'EOF'
{
  "Provider": "PostgreSQL",
  "Host": "postgres",
  "Database": "testDB",
  "Uid": "testUser",
  "Password": "Integration!123",
  "Port": "5432"
}
EOF
    ;;
  MySQL)
    cat > "${CONFIG}" <<'EOF'
{
  "Provider": "MySQL",
  "Host": "mysql",
  "Database": "testDB",
  "Uid": "testUser",
  "Password": "Integration!123",
  "Port": "3306"
}
EOF
    ;;
  SQLite)
    cat > "${CONFIG}" <<'EOF'
{
  "Provider": "SQLite",
  "Host": "",
  "Database": "testDB.db",
  "Uid": "",
  "Password": "",
  "Port": ""
}
EOF
    # Create SQLite database from schema
    apt-get update -qq && apt-get install -y -qq sqlite3 > /dev/null 2>&1
    sqlite3 /src/testDB.db < /src/scripts/sqlite-integration-setup.sql
    ;;
  *)
    echo "ERROR: Unknown provider '${PROVIDER}'. Valid values: SqlServer, PostgreSQL, MySQL, SQLite"
    exit 1
    ;;
esac

echo "Config written to ${CONFIG}:"
cat "${CONFIG}"