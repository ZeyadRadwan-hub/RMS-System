-- sqlcmd -S '(localdb)\MSSQLLocalDB' -d RMS -E -b -i RMS_Test_Evidence/lookup_recovery.sql
-- Run from repository root; every mutation is rolled back on this connection.
:ON ERROR EXIT
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() <> N'RMS' THROW 51200, 'Only RMS is authorized.', 1;
IF EXISTS (SELECT 1 FROM dbo.Transactions WHERE StatusID=6 OR TransactionTypesID=3)
    OR EXISTS (SELECT 1 FROM dbo.RequestDecisionAudit WHERE FromStatusId=6 OR ToStatusId=6)
    THROW 51201, 'Chosen lookup rows are in use; test must not remove them.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.Statuses WHERE Id=6)
    OR NOT EXISTS (SELECT 1 FROM dbo.TransactionTypes WHERE Id=3)
    THROW 51202, 'Required baseline lookup rows are missing before the test.', 1;

SELECT * INTO #OriginalEmployees FROM dbo.Employees;
SELECT * INTO #OriginalTransactions FROM dbo.Transactions;
SELECT * INTO #OriginalStatuses FROM dbo.Statuses;
SELECT * INTO #OriginalTypes FROM dbo.TransactionTypes;
SELECT * INTO #OriginalLevels FROM dbo.EmployeeLevels;

BEGIN TRANSACTION;
DELETE FROM dbo.Statuses WHERE Id=6;
DELETE FROM dbo.TransactionTypes WHERE Id=3;
UPDATE dbo.TransactionTypes SET TransactionTypeName=N'RMS_AUTOTEST_PRESERVE_LOOKUP' WHERE Id=2;
GO

:r database-setup.sql

IF NOT EXISTS (SELECT 1 FROM dbo.Statuses WHERE Id=6 AND StatusName=N'Cancelled by Employee' AND StatusType=N'TransactionStatus')
    THROW 51203, 'Missing status was not restored.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.TransactionTypes WHERE Id=3 AND TransactionTypeName=N'Half Day' AND Unit=0.5 AND Sign=-1)
    THROW 51204, 'Missing transaction type was not restored.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.TransactionTypes WHERE Id=2 AND TransactionTypeName=N'RMS_AUTOTEST_PRESERVE_LOOKUP')
    THROW 51205, 'Setup overwrote an existing lookup value.', 1;
SELECT * INTO #AfterFirstStatuses FROM dbo.Statuses;
SELECT * INTO #AfterFirstTypes FROM dbo.TransactionTypes;
SELECT * INTO #AfterFirstLevels FROM dbo.EmployeeLevels;
GO

:r database-setup.sql

IF EXISTS (SELECT * FROM dbo.Statuses EXCEPT SELECT * FROM #AfterFirstStatuses)
    OR EXISTS (SELECT * FROM #AfterFirstStatuses EXCEPT SELECT * FROM dbo.Statuses)
    OR EXISTS (SELECT * FROM dbo.TransactionTypes EXCEPT SELECT * FROM #AfterFirstTypes)
    OR EXISTS (SELECT * FROM #AfterFirstTypes EXCEPT SELECT * FROM dbo.TransactionTypes)
    OR EXISTS (SELECT * FROM dbo.EmployeeLevels EXCEPT SELECT * FROM #AfterFirstLevels)
    OR EXISTS (SELECT * FROM #AfterFirstLevels EXCEPT SELECT * FROM dbo.EmployeeLevels)
    THROW 51206, 'Second setup changed lookup rows.', 1;
ROLLBACK TRANSACTION;

IF EXISTS (SELECT * FROM dbo.Employees EXCEPT SELECT * FROM #OriginalEmployees)
    OR EXISTS (SELECT * FROM #OriginalEmployees EXCEPT SELECT * FROM dbo.Employees)
    OR EXISTS (SELECT * FROM dbo.Transactions EXCEPT SELECT * FROM #OriginalTransactions)
    OR EXISTS (SELECT * FROM #OriginalTransactions EXCEPT SELECT * FROM dbo.Transactions)
    OR EXISTS (SELECT * FROM dbo.Statuses EXCEPT SELECT * FROM #OriginalStatuses)
    OR EXISTS (SELECT * FROM #OriginalStatuses EXCEPT SELECT * FROM dbo.Statuses)
    OR EXISTS (SELECT * FROM dbo.TransactionTypes EXCEPT SELECT * FROM #OriginalTypes)
    OR EXISTS (SELECT * FROM #OriginalTypes EXCEPT SELECT * FROM dbo.TransactionTypes)
    OR EXISTS (SELECT * FROM dbo.EmployeeLevels EXCEPT SELECT * FROM #OriginalLevels)
    OR EXISTS (SELECT * FROM #OriginalLevels EXCEPT SELECT * FROM dbo.EmployeeLevels)
    THROW 51207, 'Baseline rows differ after rollback.', 1;
PRINT 'PASS: missing lookups recovered; existing values preserved; second setup unchanged; baseline rows restored exactly.';
