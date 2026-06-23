## ADDED Requirements

### Requirement: Async deferred OrderBy sorting

The AsyncLinqHelper SHALL provide deferred sorting through the IQueryable<T> pattern.

#### Scenario: Single OrderBy
- **WHEN** a repository calls `helper.AsAsyncQueryable().Where(w => w.UserId == ownerId).OrderByDescending(w => w.CreatedAt).ToListAsync()`
- **THEN** the generated SQL includes `ORDER BY created_at DESC`

#### Scenario: Multiple ThenBy calls
- **WHEN** a repository calls `helper.AsAsyncQueryable().Where(w => w.UserId == ownerId).OrderByDescending(w => w.CreatedAt).ThenBy(w => w.Id).ToListAsync()`
- **THEN** the generated SQL includes `ORDER BY created_at DESC, id ASC`

#### Scenario: OrderBy without Where
- **WHEN** a repository calls `helper.AsAsyncQueryable().OrderBy(w => w.Name).ToListAsync()`
- **THEN** the generated SQL includes `ORDER BY name ASC` without WHERE clause

#### Scenario: OrderBy with Skip/Take
- **WHEN** a repository calls `helper.AsAsyncQueryable().Where(w => w.ArchivedAt == null).OrderByDescending(w => w.CreatedAt).Skip(10).Take(20).ToListAsync()`
- **THEN** the generated SQL includes `ORDER BY created_at DESC` before LIMIT/OFFSET

### Requirement: OrderBy/OrderByDescending/ThenBy/ThenByDescending

The AsyncLinqHelper SHALL provide all four sorting methods.

#### Scenario: OrderByDescending generates DESC
- **WHEN** a repository calls `helper.AsAsyncQueryable().OrderByDescending(w => w.Amount).ToListAsync()`
- **THEN** the generated SQL includes `ORDER BY amount DESC`

#### Scenario: ThenBy after OrderBy
- **WHEN** a repository calls `helper.AsAsyncQueryable().Where(w => w.UserId == ownerId).OrderBy(w => w.LastName).ThenBy(w => w.FirstName).ToListAsync()`
- **THEN** the generated SQL includes `ORDER BY last_name ASC, first_name ASC`

#### Scenario: ThenByDescending after OrderBy
- **WHEN** a repository calls `helper.AsAsyncQueryable().Where(w => w.UserId == ownerId).OrderBy(w => w.Priority).ThenByDescending(w => w.CreatedAt).ToListAsync()`
- **THEN** the generated SQL includes `ORDER BY priority ASC, created_at DESC`

### Requirement: Multi-provider SQL translation for ORDER BY

The AsyncLinqHelper SHALL translate OrderBy/OrderByDescending to correct SQL syntax for all database providers (standard SQL ORDER BY is provider-independent).

#### Scenario: All providers use standard ORDER BY syntax
- **WHEN** using any supported provider with `.OrderBy(w => w.Name)`
- **THEN** generates `ORDER BY name ASC` (provider-independent syntax)
