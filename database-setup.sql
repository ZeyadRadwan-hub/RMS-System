-- RMS-only base schema and lookup setup. Select the existing RMS database first.
-- This script never creates accounts, passwords, or sample transactions.
-- Run with sqlcmd -b so errors stop execution.
:ON ERROR EXIT
IF DB_NAME() <> N'RMS' THROW 51080, 'Select the RMS database before setup.', 1;
GO

-- Create tables (if they don't exist)

-- EmployeeLevels Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmployeeLevels')
BEGIN
    CREATE TABLE EmployeeLevels (
        Id INT PRIMARY KEY,
        LevelName NVARCHAR(50) NOT NULL,
        AnnualLeaveEntitlement INT NOT NULL
    );
END
GO

-- Statuses Table (for both Transaction Statuses and Departments)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Statuses')
BEGIN
    CREATE TABLE Statuses (
        Id INT PRIMARY KEY IDENTITY(1,1),
        StatusName NVARCHAR(100) NOT NULL,
        StatusType NVARCHAR(50) NOT NULL -- 'TransactionStatus' or 'Department'
    );
END
GO

-- TransactionTypes Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TransactionTypes')
BEGIN
    CREATE TABLE TransactionTypes (
        Id INT PRIMARY KEY,
        TransactionTypeName NVARCHAR(100) NOT NULL,
        Unit DECIMAL(5,2) NOT NULL,
        Sign INT NOT NULL
    );
END
GO

-- Employees Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Employees')
BEGIN
    CREATE TABLE Employees (
        Id INT PRIMARY KEY,
        Code NVARCHAR(50) NOT NULL UNIQUE,
        Name NVARCHAR(200) NOT NULL,
        Password NVARCHAR(200) NOT NULL,
        DepartmentID INT NOT NULL,
        EmployeeLevelId INT NOT NULL,
        ManagerId INT NULL,
        EmployeeRole INT NOT NULL DEFAULT 0, -- 0 = Employee, 1 = Manager
        DateOfEmployment DATETIME2 NOT NULL,
        IsDeleted BIT NOT NULL DEFAULT 0,
        FOREIGN KEY (DepartmentID) REFERENCES Statuses(Id),
        FOREIGN KEY (EmployeeLevelId) REFERENCES EmployeeLevels(Id),
        FOREIGN KEY (ManagerId) REFERENCES Employees(Id)
    );
END
GO

-- Transactions Table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Transactions')
BEGIN
    CREATE TABLE Transactions (
        Id INT PRIMARY KEY,
        EmployeeId INT NOT NULL,
        TransactionTypesID INT NOT NULL,
        StartDate DATETIME2 NOT NULL,
        EndDate DATETIME2 NOT NULL,
        SubstituteEmployeeId INT NULL,
        LeaveRationale NVARCHAR(500),
        StatusID INT NOT NULL,
        ResponseMessage NVARCHAR(500),
        CreationDate DATETIME2 NOT NULL DEFAULT GETDATE(),
        ResponseDate DATETIME2 NULL,
        FOREIGN KEY (EmployeeId) REFERENCES Employees(Id),
        FOREIGN KEY (TransactionTypesID) REFERENCES TransactionTypes(Id),
        FOREIGN KEY (SubstituteEmployeeId) REFERENCES Employees(Id),
        FOREIGN KEY (StatusID) REFERENCES Statuses(Id)
    );
END
GO

-- Insert only missing lookup rows. Never overwrite existing business data.
INSERT INTO EmployeeLevels (Id, LevelName, AnnualLeaveEntitlement)
SELECT v.Id, v.LevelName, v.AnnualLeaveEntitlement
FROM (VALUES (1, N'A', 15), (2, N'B', 24)) v(Id, LevelName, AnnualLeaveEntitlement)
WHERE NOT EXISTS (SELECT 1 FROM EmployeeLevels e WHERE e.Id = v.Id);
GO

SET IDENTITY_INSERT Statuses ON;
INSERT INTO Statuses (Id, StatusName, StatusType)
SELECT v.Id, v.StatusName, v.StatusType
FROM (VALUES
    (1, N'Pending', N'TransactionStatus'),
    (2, N'Pending HR', N'TransactionStatus'),
    (3, N'Approved by HR', N'TransactionStatus'),
    (4, N'Rejected by Manager', N'TransactionStatus'),
    (5, N'Rejected by HR', N'TransactionStatus'),
    (6, N'Cancelled by Employee', N'TransactionStatus'),
    (7, N'Quality', N'Department'),
    (8, N'Production', N'Department'),
    (9, N'IT', N'Department'),
    (10, N'HR', N'Department'),
    (11, N'Board', N'Department')) v(Id, StatusName, StatusType)
WHERE NOT EXISTS (SELECT 1 FROM Statuses s WHERE s.Id = v.Id);
SET IDENTITY_INSERT Statuses OFF;
GO

INSERT INTO TransactionTypes (Id, TransactionTypeName, Unit, Sign)
SELECT v.Id, v.TransactionTypeName, v.Unit, v.Sign
FROM (VALUES
    (1, N'Sick Leave', CAST(1.0 AS decimal(5,2)), 0),
    (2, N'Annual Leave', CAST(1.0 AS decimal(5,2)), -1),
    (3, N'Half Day', CAST(0.5 AS decimal(5,2)), -1),
    (4, N'Bonus Leave', CAST(1.0 AS decimal(5,2)), 1),
    (5, N'Unpaid Leave', CAST(1.0 AS decimal(5,2)), -1)) v(Id, TransactionTypeName, Unit, Sign)
WHERE NOT EXISTS (SELECT 1 FROM TransactionTypes t WHERE t.Id = v.Id);
GO

PRINT 'RMS base schema and lookups initialized. Provision users through a secure administrator workflow.';
GO
