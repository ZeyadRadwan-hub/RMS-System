"""Read-only authenticated UI contract test for status ID filters and labels."""

from playwright.sync_api import expect, sync_playwright


dates = [
    ("2026-09-28", "2026-10-03"),
    ("2026-10-01", "2026-10-01"),
    ("2026-10-31", "2026-11-02"),
    ("2026-10-20", "2026-10-20"),
    ("2026-11-01", "2026-11-01"),
    ("2026-09-01", "2026-09-01"),
]

rows = [
    {
        "id": 910000 + status_id,
        "statusID": status_id,
        "statusName": "اسم متغير" if status_id % 2 else None,
        "employeeId": 2,
        "employeeName": f"Fixture employee {status_id}",
        "employeeCode": f"FIXTURE_{status_id}",
        "departmentName": "HR",
        "transactionTypeName": "Annual Leave",
        "startDate": dates[status_id - 1][0] + "T00:00:00",
        "endDate": dates[status_id - 1][1] + "T00:00:00",
        "calculatedDays": 1,
        "leaveRationale": "Status fixture",
    }
    for status_id in range(1, 7)
]


def intercept(route):
    route.fulfill(json={"items": rows, "hasNext": False, "page": 1, "pageSize": 200, "totalCount": 6})


def select_custom(page, label):
    page.get_by_role("button", name="Status", exact=True).click()
    page.get_by_role("button", name=label, exact=True).click()


def verify_date_overlap(page, path, native):
    if native:
        page.locator('input[type="date"]').nth(0).fill("2026-10-01")
        page.locator('input[type="date"]').nth(1).fill("2026-10-31")
    else:
        start_label = "Start Date (From)" if path == "history" else "Start Date From"
        end_label = "End Date (To)" if path == "history" else "End Date To"
        page.get_by_role("button", name=start_label, exact=True).click()
        page.get_by_role("button", name="2026-10-01").click()
        page.get_by_role("button", name=end_label, exact=True).click()
        page.get_by_role("button", name="2026-10-31").click()
    expect(page.locator("tbody tr")).to_have_count(4)
    assert sorted(label.strip() for label in page.locator("tbody .badge").all_text_contents()) == sorted(
        ["Pending", "Pending HR", "Approved", "Rejected (Manager)"]
    ), path
    print(f"{path}_october_overlap_includes_crossing_boundaries=pass")


with sync_playwright() as playwright:
    browser = playwright.chromium.launch(channel="msedge", headless=True)
    try:
        for employee_id, role, path, endpoint, native in [
            (2, "Employee", "my-requests", "my-requests", False),
            (1, "HR", "all-requests", "all", False),
            (4, "Manager", "team-requests", "my-team-requests", False),
            (5, "Board", "hr-requests", "all", True),
            (5, "Board", "history", "all", False),
        ]:
            page = browser.new_page(viewport={"width": 1280, "height": 900})
            page.add_init_script("sessionStorage.setItem('rmsSessionToken', 'ui-contract-test')")
            page.route("**/api/auth/me", lambda route: route.fulfill(json={
                "id": employee_id,
                "code": f"FIXTURE_{employee_id}",
                "name": f"Fixture {role}",
                "role": role,
                "departmentID": 10,
                "departmentName": "HR",
                "isManager": role == "Manager",
            }))
            page.route(f"**/api/transactions/{endpoint}?*", intercept)
            page.goto(f"http://localhost:5173/{path}")
            expect(page.locator("tbody tr")).to_have_count(6)
            labels = [label.strip() for label in page.locator("tbody .badge").all_text_contents()]
            assert sorted(labels) == sorted([
                "Pending", "Pending HR", "Approved", "Rejected (Manager)", "Rejected (HR)", "Cancelled"
            ]), (path, labels)
            assert sorted(page.locator("tbody .badge").evaluate_all(
                "nodes => nodes.map(node => node.className)"
            )) == sorted([
                "badge badge-pending", "badge badge-pending-hr", "badge badge-approved",
                "badge badge-rejected", "badge badge-rejected", "badge badge-cancelled"
            ]), path

            if path == "history":
                for label, count in [("Pending", 2), ("Approved", 1), ("Rejected", 2), ("Cancelled", 1)]:
                    select_custom(page, label)
                    expect(page.locator("tbody tr")).to_have_count(count)
                select_custom(page, "All Statuses")
            else:
                for label in ["Pending", "Pending HR", "Approved", "Rejected (Manager)", "Rejected (HR)", "Cancelled"]:
                    if native:
                        page.locator("select").first.select_option(label=label)
                    else:
                        select_custom(page, label)
                    expect(page.locator("tbody tr")).to_have_count(1)
                    expect(page.locator("tbody .badge")).to_have_text(label)
                if native:
                    page.locator("select").first.select_option(label="All Statuses")
                else:
                    select_custom(page, "All Statuses")
            expect(page.locator("tbody tr")).to_have_count(6)
            print(f"{path}_status_id_filters_labels_badges=pass")
            verify_date_overlap(page, path, native)
            page.close()
    finally:
        browser.close()
