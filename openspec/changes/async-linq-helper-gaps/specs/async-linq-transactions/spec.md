## ADDED Requirements

### Requirement: Public transaction API

The AsyncLinqHelper SHALL expose a public BeginTransactionAsync method that returns an ITransaction for managing atomic operations.

#### Scenario: Begin transaction with default isolation level
- **WHEN** a repository calls `helper.BeginTransactionAsync()`
- **THEN** returns an ITransaction with default IsolationLevel.ReadCommitted

#### Scenario: Begin transaction with specified isolation level
- **WHEN** a repository calls `helper.BeginTransactionAsync(IsolationLevel.Serializable)`
- **THEN** returns an ITransaction with IsolationLevel.Serializable

#### Scenario: Commit transaction
- **WHEN** a repository calls `tx.CommitAsync()`
- **THEN** the transaction is committed and IsCommitted is true

#### Scenario: Rollback transaction on exception
- **WHEN** a repository calls `tx.RollbackAsync()`
- **THEN** the transaction is rolled back and IsRolledBack is true

#### Scenario: ITransaction implements IAsyncDisposable
- **WHEN** a repository disposes an ITransaction without calling Commit
- **THEN** the transaction is automatically rolled back

### Requirement: Serializable isolation level for SQLite

The AsyncLinqHelper SHALL map IsolationLevel.Serializable to SQLite's BEGIN IMMEDIATE for proper locking behavior.

#### Scenario: Serializable isolation begins immediate transaction in SQLite
- **WHEN** using SQLite provider with `BeginTransactionAsync(IsolationLevel.Serializable)`
- **THEN** executes `BEGIN IMMEDIATE` instead of `BEGIN TRANSACTION`

### Requirement: Transaction usage pattern

The AsyncLinqHelper transaction API SHALL support the token rotation pattern with chained operations.

#### Scenario: Token rotation with transaction
- **WHEN** a repository uses a transaction to: select predecessor token, insert new token, update predecessor
- **THEN** all operations execute within the same transaction and commit atomically

#### Scenario: Transaction rolled back on exception
- **WHEN** an exception occurs during transaction operations
- **THEN** transaction is rolled back and exception is re-thrown
