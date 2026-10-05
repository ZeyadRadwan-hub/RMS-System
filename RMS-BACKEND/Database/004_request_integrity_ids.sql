-- RMS-only additive migration. Requires verified backup and migration 003.
:ON ERROR EXIT
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF DB_NAME() <> N'RMS' THROW 51040, 'Request migration is restricted to RMS.', 1;
IF OBJECT_ID(N'dbo.trg_RMS_Transactions_NoOverlap', N'TR') IS NOT NULL
    THROW 51041, 'Request migration already applied.', 1;
IF EXISTS(SELECT 1 FROM dbo.Transactions a JOIN dbo.Transactions b
    ON a.Id < b.Id AND a.EmployeeId=b.EmployeeId
    AND a.StatusID IN (1,2,3) AND b.StatusID IN (1,2,3)
    AND a.StartDate < DATEADD(day,1,CAST(b.EndDate AS date))
    AND b.StartDate < DATEADD(day,1,CAST(a.EndDate AS date)))
    THROW 51042, 'Existing active transactions overlap; resolve before migration.', 1;

CREATE INDEX IX_RMS_Transactions_Employee_Dates
    ON dbo.Transactions(EmployeeId, StartDate, EndDate) INCLUDE(StatusID);
CREATE INDEX IX_RMS_Transactions_Status_Start
    ON dbo.Transactions(StatusID, StartDate) INCLUDE(EmployeeId, EndDate);

DECLARE @nextTransactionId int = (SELECT ISNULL(MAX(Id),0)+1 FROM dbo.Transactions);
DECLARE @nextEmployeeId int = (SELECT ISNULL(MAX(Id),0)+1 FROM dbo.Employees);
DECLARE @transactionSql nvarchar(max) = N'CREATE SEQUENCE dbo.RMS_TransactionIdSequence AS int START WITH '
    + CAST(@nextTransactionId AS nvarchar(12)) + N' INCREMENT BY 1;';
DECLARE @employeeSql nvarchar(max) = N'CREATE SEQUENCE dbo.RMS_EmployeeIdSequence AS int START WITH '
    + CAST(@nextEmployeeId AS nvarchar(12)) + N' INCREMENT BY 1;';
EXEC sys.sp_executesql @transactionSql;
EXEC sys.sp_executesql @employeeSql;
GO

CREATE TRIGGER dbo.trg_RMS_Transactions_NoOverlap ON dbo.Transactions
AFTER INSERT, UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(
        SELECT 1 FROM inserted i
        JOIN dbo.Transactions t WITH (UPDLOCK,HOLDLOCK,INDEX(IX_RMS_Transactions_Employee_Dates))
            ON t.EmployeeId=i.EmployeeId AND t.Id<>i.Id
        WHERE i.StatusID IN (1,2,3) AND t.StatusID IN (1,2,3)
          AND t.StartDate < DATEADD(day,1,CAST(i.EndDate AS date))
          AND i.StartDate < DATEADD(day,1,CAST(t.EndDate AS date)))
        THROW 51043, 'Active leave requests may not overlap for one employee.', 1;
END;
GO

COMMIT TRANSACTION;
GO
