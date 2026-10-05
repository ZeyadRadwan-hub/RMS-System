-- RMS-only account-rate-limit migration. Run after verified backup with sqlcmd -b.
:ON ERROR EXIT
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF DB_NAME() <> N'RMS' THROW 51060, 'Login guard migration is restricted to RMS.', 1;
IF OBJECT_ID(N'dbo.LoginAttempts', N'U') IS NOT NULL THROW 51061, 'LoginAttempts already exists.', 1;
CREATE TABLE dbo.LoginAttempts
(
    AccountHash varbinary(32) NOT NULL CONSTRAINT PK_LoginAttempts PRIMARY KEY,
    WindowStartUtc datetime2(7) NOT NULL,
    AttemptCount int NOT NULL CONSTRAINT CK_LoginAttempts_Count CHECK(AttemptCount BETWEEN 0 AND 6)
);
COMMIT TRANSACTION;
