-- RMS-only additive integrity migration. Run with sqlcmd -b after verified backup.
:ON ERROR EXIT
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF DB_NAME() <> N'RMS' THROW 51010, 'Integrity migration is restricted to RMS.', 1;
IF OBJECT_ID(N'dbo.trg_RMS_Employees_DepartmentDomain', N'TR') IS NOT NULL
    THROW 51011, 'Integrity migration already applied.', 1;
IF EXISTS(SELECT 1 FROM dbo.Transactions WHERE EndDate < StartDate)
    THROW 51012, 'Existing reversed transaction dates require remediation.', 1;
IF EXISTS(SELECT 1 FROM dbo.TransactionTypes WHERE Unit <= 0 OR Sign NOT IN (-1,0,1))
    THROW 51013, 'Existing transaction types violate Unit or Sign domain.', 1;
IF EXISTS(SELECT 1 FROM dbo.Employees WHERE ManagerId=Id OR EmployeeRole NOT IN (0,1)
    OR LEN(LTRIM(RTRIM(Password)))=0 OR DateOfEmployment > CONVERT(date,GETDATE()))
    THROW 51014, 'Existing employee rows violate integrity rules.', 1;
IF EXISTS(SELECT 1 FROM dbo.Employees e JOIN dbo.Statuses s ON s.Id=e.DepartmentID
    WHERE s.StatusType <> N'Department')
    THROW 51015, 'Existing department references violate lookup domain.', 1;
IF EXISTS(SELECT 1 FROM dbo.Transactions t JOIN dbo.Statuses s ON s.Id=t.StatusID
    WHERE s.StatusType <> N'TransactionStatus')
    THROW 51016, 'Existing transaction status references violate lookup domain.', 1;

ALTER TABLE dbo.Transactions WITH CHECK ADD CONSTRAINT CK_RMS_Transactions_Dates
    CHECK(EndDate >= StartDate);
ALTER TABLE dbo.TransactionTypes WITH CHECK ADD CONSTRAINT CK_RMS_TransactionTypes_Unit
    CHECK(Unit > 0);
ALTER TABLE dbo.TransactionTypes WITH CHECK ADD CONSTRAINT CK_RMS_TransactionTypes_Sign
    CHECK(Sign IN (-1,0,1));
ALTER TABLE dbo.Employees WITH CHECK ADD CONSTRAINT CK_RMS_Employees_ManagerNotSelf
    CHECK(ManagerId IS NULL OR ManagerId <> Id);
ALTER TABLE dbo.Employees WITH CHECK ADD CONSTRAINT CK_RMS_Employees_Role
    CHECK(EmployeeRole IN (0,1));
ALTER TABLE dbo.Employees WITH CHECK ADD CONSTRAINT CK_RMS_Employees_PasswordNotEmpty
    CHECK(LEN(LTRIM(RTRIM(Password))) > 0);
ALTER TABLE dbo.Employees WITH CHECK ADD CONSTRAINT CK_RMS_Employees_EmploymentDate
    CHECK(DateOfEmployment <= CONVERT(date,GETDATE()));
GO

CREATE TRIGGER dbo.trg_RMS_Employees_DepartmentDomain ON dbo.Employees
AFTER INSERT, UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.Statuses s ON s.Id=i.DepartmentID
        WHERE s.StatusType <> N'Department')
        THROW 51017, 'DepartmentID must reference a Department status.', 1;
END;
GO

CREATE TRIGGER dbo.trg_RMS_Transactions_StatusDomain ON dbo.Transactions
AFTER INSERT, UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.Statuses s ON s.Id=i.StatusID
        WHERE s.StatusType <> N'TransactionStatus')
        THROW 51018, 'StatusID must reference a TransactionStatus.', 1;
END;
GO

CREATE TRIGGER dbo.trg_RMS_Statuses_ReferencedDomain ON dbo.Statuses
AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.Employees e ON e.DepartmentID=i.Id
        WHERE i.StatusType <> N'Department') OR
       EXISTS(SELECT 1 FROM inserted i JOIN dbo.Transactions t ON t.StatusID=i.Id
        WHERE i.StatusType <> N'TransactionStatus')
        THROW 51019, 'Referenced status cannot change to a conflicting domain.', 1;
END;
GO

COMMIT TRANSACTION;
GO
