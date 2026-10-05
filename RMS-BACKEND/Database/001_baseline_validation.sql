-- Read-only compatibility baseline for the populated RMS test database.
-- Run with: sqlcmd -S "(localdb)\MSSQLLocalDB" -d RMS -E -b -i 001_baseline_validation.sql
-- This script deliberately does not create a migrations-history row or change schema.
SET NOCOUNT ON;

IF DB_NAME() <> N'RMS'
    THROW 51000, 'This validation is restricted to RMS.', 1;

IF OBJECT_ID(N'dbo.Employees', N'U') IS NULL
    OR OBJECT_ID(N'dbo.EmployeeLevels', N'U') IS NULL
    OR OBJECT_ID(N'dbo.Statuses', N'U') IS NULL
    OR OBJECT_ID(N'dbo.TransactionTypes', N'U') IS NULL
    OR OBJECT_ID(N'dbo.Transactions', N'U') IS NULL
    THROW 51001, 'RMS baseline table is missing.', 1;

IF COL_LENGTH(N'dbo.EmployeeLevels', N'AnnualLeaveEntitlement') IS NULL
    OR COL_LENGTH(N'dbo.Statuses', N'StatusType') IS NULL
    OR COL_LENGTH(N'dbo.TransactionTypes', N'TransactionTypeName') IS NULL
    OR COL_LENGTH(N'dbo.Employees', N'IsDeleted') IS NULL
    OR COL_LENGTH(N'dbo.Transactions', N'ResponseMessage') IS NULL
    OR COL_LENGTH(N'dbo.Transactions', N'SubstituteEmployeeId') IS NULL
    THROW 51002, 'RMS baseline column is missing.', 1;

IF EXISTS (
    SELECT 1
    FROM sys.columns AS c
    WHERE c.object_id = OBJECT_ID(N'dbo.Transactions')
      AND c.name IN (N'ResponseMessage', N'SubstituteEmployeeId')
      AND c.is_nullable = 0
)
    THROW 51003, 'Pending response or substitute column must be nullable.', 1;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns AS c
    JOIN sys.types AS t ON t.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.TransactionTypes')
      AND c.name = N'Unit' AND t.name = N'decimal'
      AND c.precision = 5 AND c.scale = 2
)
    THROW 51004, 'TransactionTypes.Unit is not decimal(5,2).', 1;

PRINT 'RMS baseline schema compatible with current EF mappings.';
