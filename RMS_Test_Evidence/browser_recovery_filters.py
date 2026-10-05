"""RMS balance recovery and employee filters, with real authenticated reads.

Only the balance failure and inactive employee display fixture are intercepted.
No employee or transaction records are created or changed by this suite.
"""

import os
import re
from pathlib import Path

from playwright.sync_api import expect, sync_playwright


CREDENTIAL_FILE = Path(r"C:\Users\workstation\AppData\Local\RMS-Repair-Backups\Provisioning\RMS_20261004_credentials.tsv")
credentials = {}
for line in CREDENTIAL_FILE.read_text(encoding="utf-8").splitlines():
    fields = line.split("\t")
    if len(fields) == 3 and fields[0].isdigit():
        credentials[int(fields[0])] = (fields[1], os.environ.get("RMS_TEST_PASSWORD") or fields[2])


def login(page, employee_id):
    code, password = credentials[employee_id]
    page.goto("http://localhost:5173/login")
    page.get_by_placeholder("Enter your employee code").fill(code)
    page.get_by_placeholder("Enter your password").fill(password)
    page.get_by_role("button", name="Sign In").click()
    expect(page).to_have_url(re.compile(r"/dashboard$"))
    page.wait_for_load_state("networkidle")


def select(page, label, option):
    page.get_by_role("button", name=label, exact=True).click()
    page.get_by_role("button", name=option, exact=True).click()


with sync_playwright() as playwright:
    browser = playwright.chromium.launch(channel="msedge", headless=True)
    try:
        hr = browser.new_page(viewport={"width": 1280, "height": 900})
        login(hr, 1)
        token = hr.evaluate("sessionStorage.getItem('rmsSessionToken')")
        employees_response = hr.request.get("http://localhost:5190/api/employees",
            headers={"Authorization": f"Bearer {token}"})
        assert employees_response.status == 200
        employees = [item for item in employees_response.json() if item["departmentID"] != 11]
        hr.get_by_role("link", name="Employees", exact=True).click()
        expect(hr.locator("tbody tr")).to_have_count(len(employees))

        # Check every declared department against the server's DTO values.
        for department_id, label in [(7, "Quality"), (8, "Production"), (9, "IT"), (10, "HR")]:
            select(hr, "Department", label)
            expect(hr.locator("tbody tr")).to_have_count(sum(
                item["departmentID"] == department_id and item["isActive"] for item in employees))
        select(hr, "Department", "All Departments")
        for level_id, label in [(1, "A (15 days)"), (2, "B (24 days)")]:
            select(hr, "Employee Level", label)
            expect(hr.locator("tbody tr")).to_have_count(sum(
                item["employeeLevelId"] == level_id and item["isActive"] for item in employees))
        select(hr, "Employee Level", "All Levels")
        target = next(item for item in employees if item["id"] == 2)
        search = hr.get_by_placeholder("Enter name or code...")
        for value in (target["code"], target["name"].upper()):
            search.fill(value)
            expect(hr.locator("tbody tr")).to_have_count(1)
            expect(hr.locator("tbody tr").first).to_contain_text(target["code"])
        search.fill("")
        select(hr, "Status", "Inactive Only")
        expect(hr.locator("tbody tr")).to_have_count(sum(not item["isActive"] for item in employees))
        select(hr, "Status", "All Statuses")
        expect(hr.locator("tbody tr")).to_have_count(len(employees))
        print("employee_filters_real_departments_levels_name_code_status=pass")

        # Prove inactive rows are positively included without archiving real data.
        inactive = dict(target, id=900001, code="RMS_UI_FIXTURE_INACTIVE", name="UI inactive fixture", isActive=False)
        hr.route("**/api/employees", lambda route: route.fulfill(json=employees + [inactive]))
        hr.reload()
        select(hr, "Status", "Inactive Only")
        expect(hr.locator("tbody tr").filter(has_text=inactive["code"])).to_be_visible()
        select(hr, "Status", "Active Only")
        expect(hr.locator("tbody tr").filter(has_text=inactive["code"])).to_have_count(0)
        hr.unroute("**/api/employees")
        hr.reload()
        expect(hr.locator("tbody tr")).to_have_count(sum(item["isActive"] for item in employees))
        print("employee_filters_inactive_fixture=pass")

        # Failure must show an error and must never open a detail modal with zero balances.
        pattern = f"**/api/leavebalance/{target['id']}"
        for failure in ("http500", "connectionfailed"):
            def fail(route):
                if failure == "http500":
                    route.fulfill(status=500, json={"message": "Synthetic balance failure"})
                else:
                    route.abort("connectionfailed")
            hr.route(pattern, fail)
            row = hr.locator("tbody tr").filter(has_text=target["code"])
            row.get_by_role("cell", name=target["code"], exact=True).click()
            expect(hr.locator(".notification-overlay")).to_be_visible()
            expect(hr.locator(".employee-modal")).to_have_count(0)
            hr.get_by_role("button", name="OK", exact=True).click()
            hr.unroute(pattern)
            row.get_by_role("cell", name=target["code"], exact=True).click()
            expect(hr.locator(".employee-modal")).to_be_visible()
            expect(hr.locator(".employee-modal")).to_contain_text(target["code"])
            hr.locator(".employee-modal").get_by_role("button", name="Close", exact=True).click()
            print(f"employee_balance_{failure}_and_recovery=pass")

        employee = browser.new_page(viewport={"width": 1280, "height": 900})
        login(employee, 2)
        for failure in ("http500", "connectionfailed"):
            def fail(route):
                if failure == "http500":
                    route.fulfill(status=500, json={"message": "Synthetic balance failure"})
                else:
                    route.abort("connectionfailed")
            employee.route("**/api/leavebalance/my-balance", fail)
            employee.goto("http://localhost:5173/leave-balance")
            employee.get_by_role("button", name="OK", exact=True).click()
            expect(employee.get_by_role("button", name="Retry", exact=True)).to_be_visible()
            expect(employee.get_by_role("heading", name="My Leave Balance", exact=True)).to_have_count(0)
            employee.unroute("**/api/leavebalance/my-balance")
            employee.get_by_role("button", name="Retry", exact=True).click()
            expect(employee.get_by_role("heading", name="My Leave Balance", exact=True)).to_be_visible()
            expect(employee.get_by_role("button", name="Retry", exact=True)).to_have_count(0)
            print(f"leave_balance_{failure}_retry=pass")
    finally:
        browser.close()
