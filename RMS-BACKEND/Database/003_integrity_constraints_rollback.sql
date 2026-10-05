-- Explicit schema rollback; no business rows are deleted.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF DB_NAME() <> N'RMS' THROW 51030, 'Integrity rollback is restricted to RMS.', 1;
DROP TRIGGER IF EXISTS dbo.trg_RMS_Statuses_ReferencedDomain;
DROP TRIGGER IF EXISTS dbo.trg_RMS_Transactions_StatusDomain;
DROP TRIGGER IF EXISTS dbo.trg_RMS_Employees_DepartmentDomain;
ALTER TABLE dbo.Employees DROP CONSTRAINT IF EXISTS CK_RMS_Employees_EmploymentDate;
ALTER TABLE dbo.Employees DROP CONSTRAINT IF EXISTS CK_RMS_Employees_PasswordNotEmpty;
ALTER TABLE dbo.Employees DROP CONSTRAINT IF EXISTS CK_RMS_Employees_Role;
ALTER TABLE dbo.Employees DROP CONSTRAINT IF EXISTS CK_RMS_Employees_ManagerNotSelf;
ALTER TABLE dbo.TransactionTypes DROP CONSTRAINT IF EXISTS CK_RMS_TransactionTypes_Sign;
ALTER TABLE dbo.TransactionTypes DROP CONSTRAINT IF EXISTS CK_RMS_TransactionTypes_Unit;
ALTER TABLE dbo.Transactions DROP CONSTRAINT IF EXISTS CK_RMS_Transactions_Dates;
COMMIT TRANSACTION;
