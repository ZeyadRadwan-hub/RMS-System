"""Local RMS browser smoke against the authorized RMS test database only."""

import re
import subprocess
import uuid
from pathlib import Path

from playwright.sync_api import expect, sync_playwright

CREDENTIAL_FILE = Path(r"C:\Users\workstation\AppData\Local\RMS-Repair-Backups\Provisioning\RMS_20261004_credentials.tsv")
credentials = {}
for line in CREDENTIAL_FILE.read_text(encoding="utf-8").splitlines():
    fields = line.split("\t")
    if len(fields) == 3 and fields[0].isdigit():
        credentials[int(fields[0])] = (fields[1], fields[2])
assert 1 in credentials and 2 in credentials


def login(page, employee_id=None, account=None):
    code, password = account if account else credentials[employee_id]
    page.goto("http://localhost:5173/login")
    page.wait_for_load_state("networkidle")
    page.get_by_placeholder("Enter your employee code").fill(code)
    page.get_by_placeholder("Enter your password").fill(password)
    page.get_by_role("button", name="Sign In").click()
    expect(page).to_have_url(re.compile(r"/dashboard$"))
    page.get_by_role("link", name="Dashboard").wait_for(state="visible")


marker = "RMS_AUTOTEST_BROWSER_" + uuid.uuid4().hex
employee_code = "RMS_AUTOTEST_" + uuid.uuid4().hex
employee_password = uuid.uuid4().hex
try:
    with sync_playwright() as playwright:
        browser = playwright.chromium.launch(channel="msedge", headless=True)
        employee = browser.new_page(viewport={"width": 1280, "height": 900})
        page_errors = []
        employee.on("pageerror", lambda error: page_errors.append(type(error).__name__))
        login(employee, 2)
        expect(employee.get_by_role("link", name="My Requests")).to_be_visible()
        expect(employee.get_by_role("link", name="Employees")).to_have_count(0)
        employee.get_by_role("link", name="My Requests").click()
        employee.get_by_role("button", name="New Request").wait_for(state="visible")
        employee.get_by_role("button", name="New Request").click()
        form = employee.locator("form.request-form")
        form.get_by_role("button", name="Start Date *").click()
        form.get_by_role("button", name="2026-10-20").click()
        form.get_by_role("button", name="End Date *").click()
        form.get_by_role("button", name="2026-10-21").click()
        form.get_by_role("textbox", name="Reason for Leave").fill(marker)
        with employee.expect_response(lambda response:
            response.url.endswith("/api/transactions") and
            response.request.method == "POST") as created_response:
            form.get_by_role("button", name="Create Request").click()
        response = created_response.value
        assert response.status == 200, f"Browser create returned HTTP {response.status}"
        created = response.json()
        assert created["leaveRationale"] == marker and created["statusID"] == 1
        print("employee_login=pass, role_navigation=pass, browser_create=pass")

        # Exercise the actual multipart UI and document endpoint on a second request.
        employee.get_by_role("button", name="New Request").click()
        sick_form = employee.locator("form.request-form")
        sick_form.locator(".custom-select-trigger").first.focus()
        sick_form.locator(".custom-select-trigger").first.press("Enter")
        expect(sick_form.locator(".custom-select-option", has_text="Sick Leave")).to_be_visible()
        sick_form.locator(".custom-select-trigger").first.press("Escape")
        expect(sick_form.locator(".custom-select-option")).to_have_count(0)
        sick_form.locator(".custom-select-trigger").first.click()
        sick_form.locator(".custom-select-option", has_text="Sick Leave").click()
        sick_form.locator('input[type="file"]').set_input_files({
            "name": "test-medical-note.pdf",
            "mimeType": "application/pdf",
            "buffer": b"%PDF-1.4\n1 0 obj << /Type /Catalog >> endobj\n%%EOF",
        })
        sick_form.get_by_role("button", name="Start Date *").click()
        sick_form.get_by_role("button", name="2026-10-22").click()
        sick_form.get_by_role("button", name="End Date *").click()
        sick_form.get_by_role("button", name="2026-10-23").click()
        sick_form.get_by_role("textbox", name="Reason for Leave").fill(marker)
        with employee.expect_response(lambda item:
            item.url.endswith("/api/transactions") and item.request.method == "POST") as sick_response:
            sick_form.get_by_role("button", name="Create Request").click()
        sick_result = sick_response.value
        assert sick_result.status == 200, f"Sick request returned HTTP {sick_result.status}: {sick_result.text()}"
        sick = sick_result.json()
        assert sick["transactionTypesID"] == 1 and sick["leaveRationale"] == marker
        token = employee.evaluate("sessionStorage.getItem('rmsSessionToken')")
        assert token
        documents = employee.request.get(
            f"http://localhost:5190/api/transactions/{sick['id']}/medical-documents",
            headers={"Authorization": f"Bearer {token}"},
        )
        assert documents.status == 200 and len(documents.json()) == 1
        document_id = documents.json()[0]["id"]
        download = employee.request.get(
            f"http://localhost:5190/api/transactions/{sick['id']}/medical-documents/{document_id}",
            headers={"Authorization": f"Bearer {token}"},
        )
        assert download.status == 200 and download.body().startswith(b"%PDF-1.4")
        print("keyboard_select=pass, browser_sick_upload=pass, document_list_download=pass")

        mobile = browser.new_page(viewport={"width": 390, "height": 844}, is_mobile=True)
        login(mobile, 2)
        mobile.get_by_role("button", name="Open navigation").click()
        expect(mobile.get_by_role("link", name="My Requests")).to_be_visible()
        mobile.get_by_role("link", name="My Requests").click()
        expect(mobile).to_have_url(re.compile(r"/my-requests$"))
        print("mobile_navigation=pass")

        hr = browser.new_page(viewport={"width": 1280, "height": 900})
        login(hr, 1)
        expect(hr.get_by_role("link", name="Employees")).to_be_visible()
        hr.get_by_role("link", name="Employees").click()
        expect(hr).to_have_url(re.compile(r"/employees$"))
        hr_token = hr.evaluate("sessionStorage.getItem('rmsSessionToken')")
        assert hr_token
        employee_list = hr.request.get("http://localhost:5190/api/employees",
            headers={"Authorization": f"Bearer {hr_token}"})
        assert employee_list.status == 200
        manager_names = {item["id"]: item["name"] for item in employee_list.json()}

        hr.get_by_role("button", name="Add Employee").click()
        employee_form = hr.locator("form.employee-form")
        employee_form.locator('input[name="code"]').fill(employee_code)
        employee_form.locator('input[name="name"]').fill("RMS Test Employee")
        employee_form.locator('input[name="password"]').fill(employee_password)
        employee_form.get_by_role("button", name="Department *").click()
        employee_form.locator(".custom-select-option", has_text="Quality").click()
        employee_form.get_by_role("button", name="Date of Employment *").click()
        employee_form.get_by_role("button", name="2026-10-01").click()
        employee_form.get_by_role("button", name="Manager *").click()
        employee_form.locator(".custom-select-option", has_text=manager_names[1]).click()
        employee_form.get_by_role("button", name="Employee Level *").click()
        employee_form.locator(".custom-select-option", has_text="A (15 days)").click()
        with hr.expect_response(lambda item:
            item.url.endswith("/api/employees") and item.request.method == "POST") as employee_created:
            employee_form.get_by_role("button", name="Add Employee").click()
        assert employee_created.value.status == 200, employee_created.value.text()
        test_employee = employee_created.value.json()
        assert test_employee["code"] == employee_code and test_employee["employeeLevel"] == "A"

        employee_row = hr.locator("tbody tr").filter(has_text=employee_code)
        expect(employee_row).to_be_visible()
        expect(employee_row.get_by_role("cell", name="A", exact=True)).to_be_visible()
        employee_row.get_by_title("Edit Employee").click()
        employee_form = hr.locator("form.employee-form")
        employee_form.locator('input[name="name"]').fill("RMS Test Transferred")
        employee_form.get_by_role("button", name="Department *").click()
        employee_form.locator(".custom-select-option", has_text="Production").click()
        employee_form.get_by_role("button", name="Manager *").click()
        employee_form.locator(".custom-select-option", has_text=manager_names[4]).click()
        employee_form.get_by_role("button", name="Employee Level *").click()
        employee_form.locator(".custom-select-option", has_text="B (24 days)").click()
        with hr.expect_response(lambda item:
            item.url.endswith(f"/api/employees/{test_employee['id']}") and item.request.method == "PUT") as employee_updated:
            employee_form.get_by_role("button", name="Save Changes").click()
        assert employee_updated.value.status == 200, employee_updated.value.text()
        transferred = employee_updated.value.json()
        assert transferred["departmentID"] == 8 and transferred["managerId"] == 4
        assert transferred["employeeLevel"] == "B"

        direct_report = browser.new_page(viewport={"width": 1280, "height": 900})
        login(direct_report, account=(employee_code, employee_password))
        direct_report.get_by_role("link", name="My Requests").click()
        direct_report.get_by_role("button", name="New Request").click()
        report_form = direct_report.locator("form.request-form")
        report_form.locator(".custom-select-trigger").first.click()
        report_form.locator(".custom-select-option", has_text="Sick Leave").click()
        report_form.locator('input[type="file"]').set_input_files({
            "name": "manager-flow.pdf", "mimeType": "application/pdf",
            "buffer": b"%PDF-1.4\n1 0 obj << /Type /Catalog >> endobj\n%%EOF",
        })
        report_form.get_by_role("button", name="Start Date *").click()
        report_form.get_by_role("button", name="2026-10-26").click()
        report_form.get_by_role("button", name="End Date *").click()
        report_form.get_by_role("button", name="2026-10-26").click()
        report_form.get_by_role("textbox", name="Reason for Leave").fill(marker)
        with direct_report.expect_response(lambda item:
            item.url.endswith("/api/transactions") and item.request.method == "POST") as report_response:
            report_form.get_by_role("button", name="Create Request").click()
        assert report_response.value.status == 200, report_response.value.text()
        team_request = report_response.value.json()
        assert team_request["statusID"] == 1

        manager = browser.new_page(viewport={"width": 1280, "height": 900})
        login(manager, 4)
        manager.get_by_role("link", name="Team Requests").click()
        team_row = manager.locator("tbody tr").filter(
            has=manager.locator("td", has_text=str(team_request["id"]))).first
        expect(team_row).to_be_visible()
        with manager.expect_response(lambda item:
            item.url.endswith(f"/api/transactions/{team_request['id']}/approve") and
            item.request.method == "POST") as manager_approval:
            team_row.get_by_title("Approve Request").click()
            manager.get_by_role("button", name="Confirm").click()
        assert manager_approval.value.status == 200, manager_approval.value.text()
        assert manager_approval.value.json()["statusID"] == 2

        hr.get_by_role("link", name="All Requests").click()
        pending_hr_row = hr.locator("tbody tr").filter(
            has=hr.locator("td", has_text=str(team_request["id"]))).first
        expect(pending_hr_row).to_be_visible()
        with hr.expect_response(lambda item:
            item.url.endswith(f"/api/transactions/{team_request['id']}/approve") and
            item.request.method == "POST") as final_approval:
            pending_hr_row.get_by_title("Approve Request").click()
            hr.get_by_role("button", name="Confirm").click()
        assert final_approval.value.status == 200, final_approval.value.text()
        assert final_approval.value.json()["statusID"] == 3
        print("manager_then_hr_approval=pass")

        hr.get_by_role("link", name="Employees").click()

        employee_row = hr.locator("tbody tr").filter(has_text=employee_code)
        expect(employee_row).to_be_visible()
        employee_row.get_by_title("Deactivate Employee").click()
        with hr.expect_response(lambda item:
            item.url.endswith(f"/api/employees/{test_employee['id']}") and item.request.method == "DELETE") as employee_archived:
            hr.get_by_role("button", name="Confirm").click()
        assert employee_archived.value.status == 200
        archived = hr.request.get(f"http://localhost:5190/api/employees/{test_employee['id']}",
            headers={"Authorization": f"Bearer {hr_token}"})
        assert archived.status == 200 and archived.json()["isActive"] is False
        print("browser_employee_create_edit_transfer_archive=pass")
        hr.get_by_role("link", name="All Requests").click()
        row = hr.locator("tbody tr").filter(has=hr.locator("td", has_text=str(created["id"]))).first
        expect(row).to_be_visible()
        with hr.expect_response(lambda item:
            item.request.method in ("PUT", "POST") and
            f"/api/transactions/{created['id']}/approve" in item.url) as approval:
            row.get_by_title("Approve Request").click()
            hr.get_by_role("button", name="Confirm").click()
        approval_response = approval.value
        assert approval_response.status == 200, f"Browser approve returned HTTP {approval_response.status}"
        print("hr_login=pass, hr_navigation=pass, browser_hr_approval=pass")

        hr.get_by_role("link", name="My Requests").click()
        hr.get_by_role("button", name="New Request").click()
        hr_form = hr.locator("form.request-form")
        hr_form.locator(".custom-select-trigger").first.click()
        hr_form.locator(".custom-select-option", has_text="Sick Leave").click()
        hr_form.locator('input[type="file"]').set_input_files({
            "name": "board-flow.pdf", "mimeType": "application/pdf",
            "buffer": b"%PDF-1.4\n1 0 obj << /Type /Catalog >> endobj\n%%EOF",
        })
        hr_form.get_by_role("button", name="Start Date *").click()
        hr_form.get_by_role("button", name="2026-10-27").click()
        hr_form.get_by_role("button", name="End Date *").click()
        hr_form.get_by_role("button", name="2026-10-27").click()
        hr_form.get_by_role("textbox", name="Reason for Leave").fill(marker)
        with hr.expect_response(lambda item:
            item.url.endswith("/api/transactions") and item.request.method == "POST") as hr_created:
            hr_form.get_by_role("button", name="Create Request").click()
        assert hr_created.value.status == 200, hr_created.value.text()
        hr_request = hr_created.value.json()

        board = browser.new_page(viewport={"width": 1280, "height": 900}, accept_downloads=True)
        login(board, 5)
        board.get_by_role("link", name="HR Requests").click()
        board_row = board.locator("tbody tr").filter(
            has=board.locator("td", has_text=str(hr_request["id"]))).first
        expect(board_row).to_be_visible()
        with board.expect_response(lambda item:
            item.url.endswith(f"/api/transactions/{hr_request['id']}/approve") and
            item.request.method == "POST") as board_approval:
            board_row.get_by_title("Approve", exact=True).click()
            board.locator('#dialog-prompt-input').fill('Board approval')
            board.get_by_role("button", name="Submit").click()
        assert board_approval.value.status == 200, board_approval.value.text()
        assert board_approval.value.json()["statusID"] == 3

        board.get_by_role("link", name="History").click()
        expect(board.get_by_role("heading", name="Request History")).to_be_visible()
        with board.expect_download() as history_download:
            board.get_by_role("button", name="Export CSV").click()
        export = history_download.value
        assert export.suggested_filename.endswith('.csv')
        exported_text = Path(export.path()).read_text(encoding='utf-8-sig')
        assert '"Employee Name"' in exported_text and '2026-10-27' in exported_text
        # History export reports success through the shared modal; dismiss it
        # before exercising the next navigation link so the overlay cannot
        # intercept the click.
        board.get_by_role("button", name="OK").click()
        expect(board.locator(".notification-overlay")).to_have_count(0)
        board.get_by_role("link", name="Profile").click()
        expect(board.get_by_role("heading", name="Profile")).to_be_visible()
        print("board_hr_approval=pass, history_csv_export=pass, board_profile=pass")
        assert not page_errors, f"Browser JavaScript errors: {len(page_errors)}"
        browser.close()
finally:
    statement = ("SET XACT_ABORT ON; BEGIN TRAN; "
        "DELETE a FROM dbo.RequestDecisionAudit a JOIN dbo.Transactions t ON t.Id=a.TransactionId "
        f"WHERE t.LeaveRationale=N'{marker}'; "
        "DELETE d FROM dbo.MedicalDocuments d JOIN dbo.Transactions t ON t.Id=d.TransactionId "
        f"WHERE t.LeaveRationale=N'{marker}'; "
        f"DELETE FROM dbo.Transactions WHERE LeaveRationale=N'{marker}'; COMMIT;")
    statement = statement.replace("COMMIT;",
        "DELETE FROM dbo.AuthSessions WHERE EmployeeId IN "
        f"(SELECT Id FROM dbo.Employees WHERE Code=N'{employee_code}'); "
        f"DELETE FROM dbo.Employees WHERE Code=N'{employee_code}'; COMMIT;")
    cleanup = subprocess.run(["sqlcmd", "-S", r"(localdb)\MSSQLLocalDB", "-d", "RMS",
        "-E", "-b", "-Q", statement], capture_output=True, text=True, check=False)
    if cleanup.returncode != 0:
        raise RuntimeError("Exact-marker browser test cleanup failed; inspect RMS_AUTOTEST_BROWSER_ rows")
