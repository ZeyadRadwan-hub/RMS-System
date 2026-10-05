from playwright.sync_api import sync_playwright
from pathlib import Path

credential_file = Path(r"C:\Users\workstation\AppData\Local\RMS-Repair-Backups\Provisioning\RMS_20261004_credentials.tsv")
credentials = {}
for line in credential_file.read_text(encoding="utf-8").splitlines():
    fields = line.split("\t")
    if len(fields) == 3 and fields[0].isdigit():
        credentials[int(fields[0])] = (fields[1], fields[2])

with sync_playwright() as playwright:
    browser = playwright.chromium.launch(channel="msedge", headless=True)
    page = browser.new_page(viewport={"width": 1280, "height": 900})
    page.goto("http://localhost:5173/login")
    page.wait_for_load_state("networkidle")
    print("url=", page.url)
    print("headings=", page.get_by_role("heading").all_text_contents())
    print("inputs=", page.locator("input").evaluate_all("els => els.map(e => ({type:e.type,placeholder:e.placeholder,name:e.name}))"))
    print("buttons=", page.get_by_role("button").all_text_contents())
    code, password = credentials[2]
    page.get_by_placeholder("Enter your employee code").fill(code)
    page.get_by_placeholder("Enter your password").fill(password)
    page.get_by_role("button", name="Sign In").click()
    page.wait_for_url("**/dashboard")
    page.wait_for_load_state("networkidle")
    print("dashboard_url=", page.url)
    print("links=", page.get_by_role("link").all_text_contents())
    page.get_by_role("link", name="My Requests").click()
    page.wait_for_load_state("networkidle")
    page.get_by_role("heading").first.wait_for(state="visible")
    print("requests_url=", page.url)
    print("request_headings=", page.get_by_role("heading").all_text_contents())
    print("request_buttons=", page.get_by_role("button").all_text_contents()[:25])
    page.get_by_role("button", name="New Request").click()
    print("form_inputs=", page.locator("form input, form textarea").evaluate_all("els => els.map(e => ({type:e.type,name:e.name,placeholder:e.placeholder}))"))
    print("form_buttons=", page.locator("form button").all_text_contents())
    browser.close()
