-- RMS-only additive audit of approval/rejection identity and status transitions.
-- Historical decisions cannot be attributed retroactively and remain unchanged.
:ON ERROR EXIT
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF DB_NAME() <> N'RMS' THROW 51070, 'Decision audit migration is restricted to RMS.', 1;
IF OBJECT_ID(N'dbo.RequestDecisionAudit', N'U') IS NOT NULL
    THROW 51071, 'RequestDecisionAudit already exists.', 1;
CREATE TABLE dbo.RequestDecisionAudit
(
    Id uniqueidentifier NOT NULL CONSTRAINT PK_RequestDecisionAudit PRIMARY KEY,
    TransactionId int NOT NULL,
    ActorEmployeeId int NOT NULL,
    Action nvarchar(16) NOT NULL,
    FromStatusId int NOT NULL,
    ToStatusId int NOT NULL,
    OccurredUtc datetime2(7) NOT NULL,
    ResponseMessage nvarchar(1000) NULL,
    CONSTRAINT FK_RequestDecisionAudit_Transactions FOREIGN KEY(TransactionId) REFERENCES dbo.Transactions(Id),
    CONSTRAINT FK_RequestDecisionAudit_Employees FOREIGN KEY(ActorEmployeeId) REFERENCES dbo.Employees(Id),
    CONSTRAINT CK_RequestDecisionAudit_Action CHECK(Action IN (N'Approve', N'Reject')),
    CONSTRAINT CK_RequestDecisionAudit_Transition CHECK(FromStatusId <> ToStatusId)
);
CREATE INDEX IX_RequestDecisionAudit_TransactionId ON dbo.RequestDecisionAudit(TransactionId, OccurredUtc);
COMMIT TRANSACTION;
