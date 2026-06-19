-- Integration test schema for DBTools (SQL Server).
-- Applied by docker-compose sqlserver-setup and IntegrationTestBase.

IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Orders (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId INT NOT NULL,
        Product NVARCHAR(100) NULL,
        Amount DECIMAL(18, 2) NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users (
        Id INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        Name NVARCHAR(100) NULL,
        Email NVARCHAR(100) NULL,
        Age INT NOT NULL,
        Status NVARCHAR(50) NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Users)
BEGIN
    SET IDENTITY_INSERT dbo.Users ON;
    INSERT INTO dbo.Users (Id, Name, Email, Age, Status) VALUES
        (1, N'Alice', N'alice@test.com', 25, N'active'),
        (2, N'Bob', N'bob@test.com', 30, N'active'),
        (3, N'Charlie', N'charlie@test.com', 16, N'inactive');
    SET IDENTITY_INSERT dbo.Users OFF;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Orders)
BEGIN
    INSERT INTO dbo.Orders (UserId, Product, Amount) VALUES
        (1, N'Widget', 10.00),
        (1, N'Gadget', 20.00),
        (2, N'Service', 15.50);
END;
