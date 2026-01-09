# TestDBTools

This directory contains unit tests for the DBTools library.

## Test Framework

The tests use [NUnit 3.13.2](https://nunit.org/) as the testing framework.

## Test Coverage

The test suite includes 45 unit tests covering:

### UtilsTests (26 tests)
- Query builder helper methods (Select_Query, Insert_Query, Update_Query, Delete_Query)
- SQL injection prevention validation
- Parameterized query support
- Edge cases (null values, numeric values, special characters)

### DataExportTests (15 tests)
- CSV export functionality (ToCsv method)
- CSV import functionality (ToDataTable method)
- Custom separators
- Type specifications
- NULL value handling
- Special character handling (backslashes, newlines)

### ModelTests (4 tests)
- GenericObject property setting and retrieval
- GenericObject_Simple property setting and retrieval
- NULL value handling in model objects

## Building and Running Tests

### Prerequisites
- Mono runtime (for Linux/Mac) or .NET Framework 4.5+ (for Windows)
- NuGet package manager

### Restore NuGet Packages
```bash
nuget restore DBTools.sln
```

### Build the Solution
On Linux/Mac with Mono:
```bash
xbuild /p:Configuration=Debug DBTools.sln
```

On Windows:
```bash
msbuild /p:Configuration=Debug DBTools.sln
```

### Run the Tests
```bash
# Using NUnit Console Runner
mono packages/NUnit.ConsoleRunner.3.16.3/tools/nunit3-console.exe TestDBTools/bin/Debug/TestDBTools.dll
```

## Test Results

All 45 tests pass successfully. The tests validate:
- Correct SQL query generation
- SQL injection prevention
- Data export/import functionality
- Model object behavior
- Edge case handling

## Notes

- These tests focus on unit testing the public API without requiring a database connection
- Integration tests that require a live SQL Server database are not included in this test suite
- The tests use NUnit's constraint-based assertions for better readability
