-- Test RMS only. Run after a verified backup with sqlcmd -v RMS_TEST_PASSWORD=<secret>.
-- This is a data reset, not a schema migration. The value is supplied at execution,
-- so the shared test credential is not committed to source control.
:ON ERROR EXIT
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF DB_NAME() <> N'RMS' THROW 51100, 'Password reset is restricted to RMS.', 1;
DECLARE @desired nvarchar(200) = N'$(RMS_TEST_PASSWORD)';
IF LEN(@desired) NOT BETWEEN 9 AND 128 OR LEN(LTRIM(RTRIM(@desired))) = 0
    THROW 51101, 'Test password must contain 9-128 nonblank characters.', 1;
IF NOT EXISTS(SELECT 1 FROM dbo.Employees)
    THROW 51102, 'No employees found; no credential reset performed.', 1;
IF EXISTS(SELECT 1 FROM dbo.Employees
    WHERE Password <> @desired AND (Password NOT LIKE N'AQAAAA%' OR LEN(Password) < 60))
    THROW 51103, 'Unexpected credential format; inspect the affected employee before retry.', 1;

UPDATE dbo.Employees SET Password = @desired WHERE Password <> @desired;
IF EXISTS(SELECT 1 FROM dbo.Employees WHERE Password <> @desired)
    THROW 51104, 'Not all employee credentials were reset.', 1;
UPDATE dbo.AuthSessions SET RevokedUtc = SYSUTCDATETIME() WHERE RevokedUtc IS NULL;
COMMIT TRANSACTION;
PRINT 'RMS test passwords reset; active sessions revoked. No employee or transaction rows removed.';
