-- Explicit rollback; never erase recorded decisions automatically.
:ON ERROR EXIT
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF DB_NAME() <> N'RMS' THROW 51072, 'Decision audit rollback is restricted to RMS.', 1;
IF OBJECT_ID(N'dbo.RequestDecisionAudit', N'U') IS NULL
    THROW 51073, 'RequestDecisionAudit is absent.', 1;
IF EXISTS(SELECT 1 FROM dbo.RequestDecisionAudit)
    THROW 51074, 'Decision history exists; archive it before rollback.', 1;
DROP TABLE dbo.RequestDecisionAudit;
COMMIT TRANSACTION;
