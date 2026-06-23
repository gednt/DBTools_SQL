## ADDED Requirements

### Requirement: ValueConverter extensibility on PropertyMapping

The library SHALL expose a `ValueConverter` delegate on `PropertyMapping` that converts a raw database column value to the property's CLR type during hydration. The delegate is invoked by `AsyncDbQueryProvider` and `AsyncLinqHelper` when populating property values from a `DataRow`.

#### Scenario: PropertyMapping exposes ValueConverter
- **WHEN** code inspects a `PropertyMapping` instance
- **THEN** the `ValueConverter` property is null by default
- **AND** the property can be assigned a `Func<object, object>` delegate

#### Scenario: ValueConverter is honored during hydration
- **WHEN** an entity has a `PropertyMapping` with a non-null `ValueConverter`
- **AND** the entity is materialized from a `DataTable` via `AsyncDbQueryProvider` or `AsyncLinqHelper`
- **THEN** the raw column value is passed through the converter
- **AND** the converter's return value is set on the property

#### Scenario: Primitive conversion fallback when no converter is set
- **WHEN** an entity has a `PropertyMapping` with a null `ValueConverter`
- **AND** the raw column type does not match the property type
- **THEN** the existing `Convert.ChangeType` path is used (preserves current behavior)

#### Scenario: Null / DBNull handling
- **WHEN** the raw column value is `null` or `DBNull.Value`
- **AND** a `ValueConverter` is configured for that property
- **THEN** the converter is NOT invoked
- **AND** the property is left at its default value (or null for reference types / Nullable<T>)

### Requirement: ModelFactory extensibility on EntityMapping

The library SHALL expose a `ModelFactory` delegate on `EntityMapping` that creates the entity directly from an `IDataRecord`, bypassing the `new TModel()` constraint. This enables hydration of entities with private constructors or factory-only construction.

#### Scenario: EntityMapping exposes ModelFactory
- **WHEN** code inspects an `EntityMapping` instance
- **THEN** the `ModelFactory` property is null by default
- **AND** the property can be assigned a `Func<IDataRecord, object>` delegate

#### Scenario: ModelFactory is honored during hydration
- **WHEN** an entity has an `EntityMapping` with a non-null `ModelFactory`
- **AND** the entity is materialized from a `DataTable` via `AsyncDbQueryProvider` or `AsyncLinqHelper`
- **THEN** `ModelFactory` is called once per row, receiving an `IDataRecord` view of the row
- **AND** the returned object is added to the result list
- **AND** the `new TModel()` path is NOT used

#### Scenario: Fallback to new TModel() when no factory is set
- **WHEN** an entity has an `EntityMapping` with a null `ModelFactory`
- **AND** the entity is materialized
- **THEN** the existing `new TModel()` + property-setter path is used

### Requirement: Fluent configuration of ModelFactory + ValueConverter

The library SHALL expose fluent methods on `EntityBuilder<TModel>` and `PropertyBuilder` to register `ModelFactory` and `ValueConverter` respectively. Configurations SHALL flow into `EntityMapping` / `PropertyMapping` via the existing `EntityMappingResolver.Register<T>` path.

#### Scenario: HasModelFactory on EntityBuilder
- **WHEN** a configuration calls `entityBuilder.HasModelFactory(record => MyEntity.Hydrate(record))`
- **THEN** the delegate is stored on the `EntityBuilder`
- **AND** when `EntityTypeConfiguration<T>.Apply` is invoked, the delegate is copied onto the `EntityMapping.ModelFactory` property

#### Scenario: HasConversion on PropertyBuilder populates ValueConverter
- **WHEN** a configuration calls `propertyBuilder.HasConversion(write, read)`
- **THEN** the `read` converter is stored on `PropertyBuilder.ReadConverter`
- **AND** when `EntityTypeConfiguration<T>.Apply` is invoked, the read converter is copied onto the matching `PropertyMapping.ValueConverter`

### Requirement: DataRowRecordAdapter — DataRow as IDataRecord

The library SHALL provide a `DataRowRecordAdapter` that implements `System.Data.IDataRecord` over a wrapped `DataRow`. This adapter is the bridge that lets `ModelFactory` consume a `DataRow` directly without depending on `DataTable.Load`.

#### Scenario: DataRowRecordAdapter exposes row fields as IDataRecord
- **WHEN** code wraps a `DataRow` in `DataRowRecordAdapter`
- **THEN** `GetName(i)` returns the column name at index `i`
- **AND** `GetOrdinal(name)` returns the column index for the given name
- **AND** `this[i]` and `this[name]` return the column value
- **AND** `IsDBNull(i)` returns true when the value is `null` or `DBNull.Value`
- **AND** `FieldCount` returns the column count

#### Scenario: DataRowRecordAdapter typed accessors delegate correctly
- **WHEN** code calls `GetString(i)`, `GetInt32(i)`, `GetGuid(i)`, `GetDateTime(i)`, `GetDecimal(i)`, `GetBoolean(i)` on the adapter
- **THEN** the value is read from the underlying `DataRow` and converted via `Convert.ChangeType` (or returned as-is if types match)

### Requirement: AsyncDbQueryProvider / AsyncLinqHelper do not require public parameterless constructor

The library SHALL NOT require `TModel` to have a public parameterless constructor. The `new()` constraint on `AsyncDbQueryProvider<TModel>` and `AsyncLinqHelper<TModel>` SHALL be relaxed to `where TModel : class`. Hydration SHALL rely on `ModelFactory` (when configured) or `new TModel()` reflection for fallback only — and `RuntimeHelpers.GetUninitializedObject` is NOT used by the library.

#### Scenario: Entities with private constructors can be hydrated
- **WHEN** `TModel` has a private parameterless constructor
- **AND** an `IEntityConfiguration<TModel>` registers a `ModelFactory`
- **THEN** `AsyncLinqHelper<T>.AsAsyncQueryable()` and `SelectAsync()` work without throwing `MissingMethodException`

#### Scenario: Entities without ModelFactory still require a public constructor
- **WHEN** `TModel` has no `ModelFactory` configured
- **THEN** `TModel` SHALL still have a public parameterless constructor (existing behavior)
- **AND** `AsyncLinqHelper<T>` SHALL throw a clear error if the constructor is missing