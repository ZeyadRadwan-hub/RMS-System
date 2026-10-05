-- Rollback only when the ephemeral attempt table is empty; preserve live lockout state.
:ON ERROR EXIT
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF DB_NAME() <> N'RMS' THROW 51062, 'Rollback is restricted to RMS.', 1;
IF OBJECT_ID(N'dbo.LoginAttempts', N'U') IS NULL THROW 51063, 'LoginAttempts is absent.', 1;
IF EXISTS(SELECT 1 FROM dbo.LoginAttempts) THROW 51064, 'LoginAttempts contains live state; do not drop it.', 1;
DROP TABLE dbo.LoginAttempts;
COMMIT TRANSACTION;
