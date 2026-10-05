-- Explicit schema rollback; no employee or transaction rows are deleted.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF DB_NAME() <> N'RMS' THROW 51044, 'Request rollback is restricted to RMS.', 1;
DROP TRIGGER IF EXISTS dbo.trg_RMS_Transactions_NoOverlap;
DROP INDEX IF EXISTS IX_RMS_Transactions_Status_Start ON dbo.Transactions;
DROP INDEX IF EXISTS IX_RMS_Transactions_Employee_Dates ON dbo.Transactions;
DROP SEQUENCE IF EXISTS dbo.RMS_EmployeeIdSequence;
DROP SEQUENCE IF EXISTS dbo.RMS_TransactionIdSequence;
COMMIT TRANSACTION;
