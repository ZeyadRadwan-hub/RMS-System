# RMS database changes

The populated `RMS` database on `(localdb)\MSSQLLocalDB` is the compatibility baseline. The backend does not alter the database when it starts. Run `001_baseline_validation.sql` before starting it or before applying any later migration. This script is read-only and intentionally does not add a `__EFMigrationsHistory` row: no EF migration has yet been applied to this existing database.

Before a schema/data migration, take a `COPY_ONLY` backup with `CHECKSUM`, note employee/transaction counts and key relationships, and verify the backup. The pre-repair backup is stored outside OneDrive at `C:/Users/workstation/AppData/Local/RMS-Repair-Backups/RMS_20261004_pre_repair.bak`; `RESTORE VERIFYONLY WITH CHECKSUM` succeeded. That check validates backup readability, **not** a complete restore. A restore drill needs an isolated authorized database, which is outside the current `RMS`-only test authorization.

Run the validation against the only authorized test database:

```powershell
sqlcmd -S '(localdb)\MSSQLLocalDB' -d RMS -E -b -i 'RMS-BACKEND\Database\001_baseline_validation.sql'
```

After changing mappings, run the live `SchemaCompatibility` test. Do not use `EnsureCreated` or `Migrate` against populated `RMS`: either could misrepresent the existing schema as a newly applied migration. Future changes should have a versioned forward script, preflight data checks, postflight assertions, an idempotence rule, and an explicit rollback script or backup-restore procedure. Destructive contraction is a separate reviewed step. Never silently retry or hide a migration error on application startup.

## Applied additive changes on the authorized test database

Run every script with `sqlcmd -S '(localdb)\MSSQLLocalDB' -d RMS -E -b -i '<script>'`, after a fresh verified `COPY_ONLY, CHECKSUM` backup. Every forward script refuses a second application and non-`RMS` databases. Never rerun a failed step without inspecting whether it committed.

| Script | Change | Rollback rule |
| --- | --- | --- |
| `002_auth_sessions.sql` | Expiring, revocable bearer sessions | Remove only after archiving/removing sessions |
| `003_integrity_constraints.sql` | Domain/date/password/self-manager checks and triggers | Inspect dependent data before dropping constraints |
| `004_request_integrity_ids.sql` | Overlap protection, ID sequences and indexes | Keep sequence values and transaction history before rollback |
| `005_medical_documents.sql` | Medical evidence with transaction/employee FKs | Refuses rollback with documents present |
| `006_login_attempt_guard.sql` | Shared account-rate-limit counters | Refuses rollback with counters present |
| `007_request_decision_audit.sql` | Approver/rejecter identity and status transition trail | Refuses rollback with decisions present |
| `008_manager_hierarchy.sql` | Rejects multi-level manager cycles (preflight and trigger) | Drops only the new trigger; keeps employee relationships |

The verified backup immediately before `007` is `C:/Users/workstation/AppData/Local/RMS-Repair-Backups/RMS_20261005_pre_decision_audit.bak`. Its `RESTORE VERIFYONLY WITH CHECKSUM` succeeded. Existing historical approvals cannot be attributed to an actor retrospectively, so `007` intentionally leaves them unchanged; newly processed decisions receive audit rows. A `RESTORE VERIFYONLY` is not a restore drill. A complete restore drill still requires a separately authorized isolated database.

The verified backup immediately before `008` is `C:/Users/workstation/AppData/Local/RMS-Repair-Backups/RMS_20261005_pre_hierarchy.bak`. The preflight found zero existing manager cycles. The API also returns a validation error for cycles; the database trigger protects direct SQL writes and races.

## User-requested test-only password reset (data, not schema)

`009_test_plaintext_password_reset.sql` updates only credential values in the existing `Employees.Password` column and revokes active sessions. It does not alter a table definition. The user explicitly requested plaintext and a shared value in the disposable `RMS` test database; do not use this policy for production. Supply the password through SQLCMD variable `RMS_TEST_PASSWORD` at execution and keep the value out of the repository. The script refuses a non-RMS database, unexpected credential formats, and blank/short values; rerunning it with the same value makes no further credential changes.

Before applying it, `C:/Users/workstation/AppData/Local/RMS-Repair-Backups/RMS_20261005_pre_plaintext_passwords.bak` was created with `COPY_ONLY, CHECKSUM` and passed `RESTORE VERIFYONLY WITH CHECKSUM`. Immediately after application, five of five employees had the requested value, 242 active sessions were revoked, and the transaction count remained four. The backup is the rollback source; restoring the entire database later would also discard later test changes, so any rollback must account for subsequent data first. See `RMS_Database_Schema_Review.md` for the team-facing schema summary.
