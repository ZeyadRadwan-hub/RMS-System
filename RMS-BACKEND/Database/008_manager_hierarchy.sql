-- Reject multi-level manager cycles while preserving the existing hierarchy.
:ON ERROR EXIT
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF DB_NAME() <> N'RMS' THROW 51090, 'Hierarchy migration is restricted to RMS.', 1;
IF OBJECT_ID(N'dbo.trg_RMS_Employees_NoManagerCycle', N'TR') IS NOT NULL
    THROW 51091, 'Manager cycle trigger already exists.', 1;
DECLARE @invalid bit = 0;
;WITH chain AS
(
    SELECT Id AS RootId, ManagerId, 0 AS Depth FROM dbo.Employees WHERE ManagerId IS NOT NULL
    UNION ALL
    SELECT c.RootId, e.ManagerId, c.Depth + 1
    FROM chain c JOIN dbo.Employees e ON e.Id = c.ManagerId
    WHERE c.ManagerId <> c.RootId AND c.Depth < 100
)
SELECT @invalid = 1 FROM chain WHERE ManagerId = RootId OR Depth = 100
OPTION (MAXRECURSION 100);
IF @invalid = 1 THROW 51092, 'Existing manager hierarchy has a cycle or exceeds 100 levels.', 1;
GO
CREATE TRIGGER dbo.trg_RMS_Employees_NoManagerCycle ON dbo.Employees
AFTER INSERT, UPDATE AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @invalid bit = 0;
    ;WITH chain AS
    (
        SELECT Id AS RootId, ManagerId, 0 AS Depth FROM inserted WHERE ManagerId IS NOT NULL
        UNION ALL
        SELECT c.RootId, e.ManagerId, c.Depth + 1
        FROM chain c JOIN dbo.Employees e ON e.Id = c.ManagerId
        WHERE c.ManagerId <> c.RootId AND c.Depth < 100
    )
    SELECT @invalid = 1 FROM chain WHERE ManagerId = RootId OR Depth = 100
    OPTION (MAXRECURSION 100);
    IF @invalid = 1 THROW 51093, 'Manager hierarchy cannot contain a cycle or exceed 100 levels.', 1;
END;
GO
COMMIT TRANSACTION;
GO
