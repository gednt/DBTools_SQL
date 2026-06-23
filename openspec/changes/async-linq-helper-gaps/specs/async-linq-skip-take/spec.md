## ADDED Requirements

### Requirement: Async deferred Skip/Take pagination

The AsyncLinqHelper SHALL provide deferred pagination through the IQueryable<T> pattern.

#### Scenario: Skip and Take via AsAsyncQueryable
- **WHEN** a repository calls `helper.AsAsyncQueryable().Skip(10).Take(20).ToListAsync()`
- **THEN** the generated SQL includes appropriate LIMIT/OFFSET clauses for the configured provider

#### Scenario: Skip without Take (get all remaining)
- **WHEN** a repository calls `helper.AsAsyncQueryable().Skip(10).ToListAsync()`
- **THEN** the generated SQL includes OFFSET without LIMIT (provider-dependent)

#### Scenario: Take without Skip (limit only)
- **WHEN** a repository calls `helper.AsAsyncQueryable().Take(5).ToListAsync()`
- **THEN** the generated SQL includes LIMIT without OFFSET

#### Scenario: Skip and Take after Where
- **WHEN** a repository calls `helper.AsAsyncQueryable().Where(w => w.UserId == ownerId).Skip(10).Take(20).ToListAsync()`
- **THEN** the generated SQL includes WHERE clause followed by LIMIT/OFFSET

#### Scenario: Skip with value less than or equal to zero
- **WHEN** a repository calls `helper.AsAsyncQueryable().Skip(0).Take(10).ToListAsync()`
- **THEN** the generated SQL omits the OFFSET clause entirely

#### Scenario: Take with value less than or equal to zero
- **WHEN** a repository calls `helper.AsAsyncQueryable().Take(0).ToListAsync()`
- **THEN** the generated SQL includes `LIMIT 0` which returns empty result set

### Requirement: Multi-provider SQL translation for async path

The async query path SHALL translate Skip/Take to the correct SQL syntax for each database provider.

#### Scenario: SQLite translation
- **WHEN** using SQLite provider with `.Skip(10).Take(20)`
- **THEN** generates `ORDER BY ... LIMIT 20 OFFSET 10`

#### Scenario: SQL Server translation
- **WHEN** using SQL Server provider with `.Skip(10).Take(20)`
- **THEN** generates `ORDER BY ... OFFSET 10 ROWS FETCH NEXT 20 ROWS ONLY`

#### Scenario: PostgreSQL translation
- **WHEN** using PostgreSQL provider with `.Skip(10).Take(20)`
- **THEN** generates `ORDER BY ... LIMIT 20 OFFSET 10`

#### Scenario: MySQL translation
- **WHEN** using MySQL provider with `.Skip(10).Take(20)`
- **THEN** generates `ORDER BY ... LIMIT 20 OFFSET 10`

### Requirement: Non-literal Skip/Take argument rejection

The async query path SHALL reject non-literal Skip/Take arguments at translation time.

#### Scenario: Reject non-literal Skip argument
- **WHEN** parsing expression containing `.Skip(variable)` where variable is not a compile-time constant
- **THEN** parser throws ArgumentException with message indicating only literal values are supported

#### Scenario: Reject non-literal Take argument
- **WHEN** parsing expression containing `.Take(variable)` where variable is not a compile-time constant
- **THEN** parser throws ArgumentException with message indicating only literal values are supported
