-- Explicit rollback only after separately archiving every medical document.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF DB_NAME() <> N'RMS' THROW 51052, 'Medical rollback is restricted to RMS.', 1;
IF OBJECT_ID(N'dbo.MedicalDocuments', N'U') IS NULL THROW 51053, 'MedicalDocuments is absent.', 1;
IF EXISTS(SELECT 1 FROM dbo.MedicalDocuments)
    THROW 51054, 'Medical document history exists; archive it before rollback.', 1;
DROP TABLE dbo.MedicalDocuments;
COMMIT TRANSACTION;
