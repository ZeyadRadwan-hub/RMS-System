-- Roll back only the additional cycle guard; employee relationships are untouched.
:ON ERROR EXIT
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF DB_NAME() <> N'RMS' THROW 51094, 'Hierarchy rollback is restricted to RMS.', 1;
IF OBJECT_ID(N'dbo.trg_RMS_Employees_NoManagerCycle', N'TR') IS NULL
    THROW 51095, 'Manager cycle trigger is absent.', 1;
DROP TRIGGER dbo.trg_RMS_Employees_NoManagerCycle;
COMMIT TRANSACTION;
