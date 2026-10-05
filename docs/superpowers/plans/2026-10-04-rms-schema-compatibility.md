# RMS schema compatibility Implementation Plan

> **For agentic workers:** Execute inline in this session with test-first checkpoints. Do not change any database other than the authorized test `RMS`.

**Goal:** Make the current Backend read the existing `RMS` schema without renaming tables, losing rows, or changing schema on API startup.

**Architecture:** Treat the five populated tables in `RMS` as the canonical first compatibility target. Correct EF entity/table/column mappings and remove unmapped properties; preserve primary keys and FKs. Remove the unversioned startup `ALTER`; later schema changes go through explicit reviewed SQL migrations after backup.

**Tech Stack:** ASP.NET Core 10, EF Core 10 SQL Server, xUnit, SQL Server LocalDB.

**Spec:** `C:/Users/workstation/OneDrive/Desktop/RMS_Full_Repair_Codex_Prompt.md`, APP-BT-009/025 and DB-BT-001/002/013/019.

## Global Constraints

- Authorized instance/database: `(localdb)\MSSQLLocalDB` / `RMS` only; Windows Authentication.
- Preserve all existing rows, IDs, relationships and history.
- Pre-repair backup: `C:/Users/workstation/AppData/Local/RMS-Repair-Backups/RMS_20261004_pre_repair.bak`; `RESTORE VERIFYONLY` passed.
- Do not log credentials or put them in test output.
- Do not equate `dotnet test` exit 0 without discovered tests with a passing test suite.

---

### Task 1: Add a live schema regression test

**Files:**
- Create: `RMS-BACKEND.Tests/RMS-BACKEND.Tests.csproj`
- Create: `RMS-BACKEND.Tests/SchemaCompatibilityTests.cs`
- Modify: `RMS-BACKEND.slnx`

**Interfaces:**
- Consumes: `ApplicationDbContext` and existing `DbSet` properties.
- Produces: `dotnet test` that queries `EmployeeLevels`, `Statuses`, `TransactionTypes`, `Employees`, and `Transactions` against the authorized RMS database.

- [ ] Write an xUnit test that opens `ApplicationDbContext` with `UseSqlServer("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=RMS;Integrated Security=True;Encrypt=False")` and asserts `CountAsync()` can run for each DbSet; assert the database name is `RMS` first.
- [ ] Run `dotnet test RMS-BACKEND.Tests/RMS-BACKEND.Tests.csproj --filter SchemaCompatibility` and verify it fails with the current `Invalid object name 'EmployeeLevel'` or another missing-column error, not setup failure.
- [ ] Keep the test in the project as regression proof; never redirect it to a different database.

### Task 2: Map EF to populated RMS tables and remove silent startup DDL

**Files:**
- Modify: `RMS-BACKEND/Models/EmployeeLevel.cs`
- Modify: `RMS-BACKEND/Models/Status.cs`
- Modify: `RMS-BACKEND/Models/TransactionType.cs`
- Modify: `RMS-BACKEND/Models/Employee.cs`
- Modify: `RMS-BACKEND/Models/Transaction.cs`
- Modify: `RMS-BACKEND/Services/LeaveBalanceService.cs`
- Modify: `RMS-BACKEND/Services/TransactionService.cs`
- Modify: `RMS-BACKEND/Program.cs`

**Interfaces:**
- Consumes: the schema test from Task 1 and live `sys.columns` inventory.
- Produces: entity properties mapped to existing SQL columns, no startup schema mutation.

- [ ] Map `EmployeeLevel` to `EmployeeLevels` with `LevelName` and `AnnualLeaveEntitlement`; replace `RegularLeaveperYear` consumer with `AnnualLeaveEntitlement`.
- [ ] Map `Status` to `Statuses` with `StatusName` and `StatusType`; remove EF-mapped `Entity` and `OrderNumber` because they have no source columns.
- [ ] Map `TransactionType.Name` to SQL `TransactionTypeName` and remove its unused `Description`; map `Unit` to `decimal(5,2)` while preserving the public numeric calculation contract.
- [ ] Match `EmployeeRole` storage to SQL `int`, model `IsDeleted`, and make `Transaction.ResponseMessage` nullable as in the live table.
- [ ] Remove the `ExecuteSqlRaw` startup block and its empty `catch`. Keep service startup free of DDL.
- [ ] Run the focused schema test and verify it passes, then run backend build and a second API startup. Confirm row counts remain 5 employees and 3 transactions and no new schema objects appear unexpectedly.

### Task 3: Establish migration discipline for later changes

**Files:**
- Create: `RMS-BACKEND/Database/MIGRATIONS.md`
- Create: `RMS-BACKEND/Database/001_baseline_validation.sql`

**Interfaces:**
- Consumes: canonical schema inventory and the verified backup.
- Produces: a read-only baseline validation that rejects incompatible schemas without recreating populated tables.

- [ ] Add a read-only SQL script that checks required table/column presence and rejects mismatches with `THROW`; it must not create `__EFMigrationsHistory` or mark an unapplied migration as complete.
- [ ] Run it against `RMS` and verify exit 0; run a known-bad copy of one predicate in a rollback-safe probe and verify nonzero exit.
- [ ] Document backup, `RESTORE VERIFYONLY` limitations, forward validation, and the rollback path before any future `ALTER TABLE` migration.

## Review

- [ ] Re-read APP-BT-009/025 and DB-BT-001/002/013/019; every claim maps to a test or remains explicitly open.
- [ ] Confirm no plaintext password, connection secret, or personal employee data entered source/test logs.
- [ ] Confirm no production or non-RMS database was touched.
