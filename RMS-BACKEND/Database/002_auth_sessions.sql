-- Apply only after a verified backup. Additive; preserves all existing rows.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF DB_NAME() <> N'RMS' THROW 51000, 'AuthSessions migration is restricted to RMS.', 1;
IF OBJECT_ID(N'dbo.AuthSessions', N'U') IS NOT NULL THROW 51001, 'AuthSessions already exists.', 1;
CREATE TABLE dbo.AuthSessions
(
    Id uniqueidentifier NOT NULL CONSTRAINT PK_AuthSessions PRIMARY KEY,
    TokenHash varbinary(32) NOT NULL,
    EmployeeId int NOT NULL,
    CreatedUtc datetime2(7) NOT NULL,
    ExpiresUtc datetime2(7) NOT NULL,
    RevokedUtc datetime2(7) NULL,
    CONSTRAINT FK_AuthSessions_Employees FOREIGN KEY(EmployeeId) REFERENCES dbo.Employees(Id),
    CONSTRAINT CK_AuthSessions_Expiry CHECK(ExpiresUtc > CreatedUtc)
);
CREATE UNIQUE INDEX UX_AuthSessions_TokenHash ON dbo.AuthSessions(TokenHash);
CREATE INDEX IX_AuthSessions_EmployeeId_ExpiresUtc ON dbo.AuthSessions(EmployeeId, ExpiresUtc);
COMMIT TRANSACTION;
