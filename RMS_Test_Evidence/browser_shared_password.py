"""Read-only role/login smoke for the user-requested RMS test credential reset.

Set RMS_TEST_PASSWORD in the invoking process. Never print or persist its value.
"""

import os
import re
from pathlib import Path

from playwright.sync_api import expect, sync_playwright


password = os.environ.get("RMS_TEST_PASSWORD")
assert password and len(password) >= 9, "Set RMS_TEST_PASSWORD for the local RMS test database"

credential_file = Path(r"C:\Users\workstation\AppData\Local\RMS-Repair-Backups\Provisioning\RMS_20261004_credentials.tsv")
codes = {}
for line in credential_file.read_text(encoding="utf-8").splitlines():
    fields = line.split("\t")
    if len(fields) == 3 and fields[0].isdigit():
        codes[int(fields[0])] = fields[1]

with sync_playwright() as playwright:
    browser = playwright.chromium.launch(channel="msedge", headless=True)
    try:
        for employee_id in (1, 2, 3, 4, 5):
            page = browser.new_page(viewport={"width": 1280, "height": 900})
            page.goto("http://localhost:5173/login")
            page.get_by_placeholder("Enter your employee code").fill(codes[employee_id])
            page.get_by_placeholder("Enter your password").fill(password)
            page.get_by_role("button", name="Sign In").click()
            expect(page).to_have_url(re.compile(r"/dashboard$"))
            token = page.evaluate("sessionStorage.getItem('rmsSessionToken')")
            assert token, f"No session token for account {employee_id}"
            response = page.request.get("http://localhost:5190/api/auth/me",
                headers={"Authorization": f"Bearer {token}"})
            assert response.status == 200 and response.json()["id"] == employee_id
            logout = page.request.post("http://localhost:5190/api/auth/logout",
                headers={"Authorization": f"Bearer {token}"})
            assert logout.status == 204
            page.close()
            print(f"employee_{employee_id}_shared_password_login_me_logout=pass")
    finally:
        browser.close()
