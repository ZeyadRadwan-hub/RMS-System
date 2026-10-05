-- RMS-only additive storage for sensitive medical evidence.
:ON ERROR EXIT
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF DB_NAME() <> N'RMS' THROW 51050, 'Medical document migration is restricted to RMS.', 1;
IF OBJECT_ID(N'dbo.MedicalDocuments', N'U') IS NOT NULL
    THROW 51051, 'MedicalDocuments already exists.', 1;
CREATE TABLE dbo.MedicalDocuments
(
    Id uniqueidentifier NOT NULL CONSTRAINT PK_MedicalDocuments PRIMARY KEY,
    TransactionId int NOT NULL,
    UploadedByEmployeeId int NOT NULL,
    OriginalName nvarchar(255) NOT NULL,
    MimeType nvarchar(100) NOT NULL,
    SizeBytes int NOT NULL,
    Sha256 varbinary(32) NOT NULL,
    Content varbinary(max) NOT NULL,
    UploadedUtc datetime2(7) NOT NULL,
    CONSTRAINT FK_MedicalDocuments_Transactions FOREIGN KEY(TransactionId) REFERENCES dbo.Transactions(Id),
    CONSTRAINT FK_MedicalDocuments_Employees FOREIGN KEY(UploadedByEmployeeId) REFERENCES dbo.Employees(Id),
    CONSTRAINT CK_MedicalDocuments_Size CHECK(SizeBytes > 0 AND SizeBytes <= 10485760),
    CONSTRAINT CK_MedicalDocuments_ContentSize CHECK(DATALENGTH(Content)=SizeBytes)
);
CREATE INDEX IX_MedicalDocuments_TransactionId ON dbo.MedicalDocuments(TransactionId);
COMMIT TRANSACTION;
