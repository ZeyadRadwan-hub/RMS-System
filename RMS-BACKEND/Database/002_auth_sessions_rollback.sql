-- Explicit rollback only. Existing sessions must be revoked/expired and archived first.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF DB_NAME() <> N'RMS' THROW 51002, 'Rollback is restricted to RMS.', 1;
IF OBJECT_ID(N'dbo.AuthSessions', N'U') IS NULL THROW 51003, 'AuthSessions does not exist.', 1;
IF EXISTS(SELECT 1 FROM dbo.AuthSessions) THROW 51004, 'AuthSessions contains history; archive it before rollback.', 1;
DROP TABLE dbo.AuthSessions;
COMMIT TRANSACTION;
