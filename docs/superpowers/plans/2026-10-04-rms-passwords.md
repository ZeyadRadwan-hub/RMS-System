# RMS password storage repair

1. Add a test that asserts current stored password strings are not acceptable hashes and a unit test for correct/wrong password verification.
2. Use ASP.NET Core `PasswordHasher<Employee>` for login and employee creation; never compare SQL password text or return it from an API.
3. Take a fresh checksum backup of authorized `RMS`, verify it, then run a one-time, explicitly invoked migration tool limited to that database. Give each existing demo account a distinct high-entropy password; hash it with a per-record salt. Store the one-time credentials only in an access-restricted file outside the repository/OneDrive, not in logs or reports. Preserve employee IDs/rows and transaction history.
4. Remove reusable plaintext seed credentials from the bootstrap script/docs and document safe provisioning/reset. Add a DB check rejecting empty password values. Test login with correct/incorrect credential and inspect only aggregate hash properties, never reveal secrets.
5. Add a password-change/reset path so temporary credentials are not permanent. Keep all password and authorization findings open until that path and its tests pass.
