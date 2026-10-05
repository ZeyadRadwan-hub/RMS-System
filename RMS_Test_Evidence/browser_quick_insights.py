"""Read-only authenticated touch and keyboard regression for Quick Insights."""
import os
import re
from pathlib import Path
from playwright.sync_api import expect, sync_playwright

credentials = {}
for line in Path(r"C:\Users\workstation\AppData\Local\RMS-Repair-Backups\Provisioning\RMS_20261004_credentials.tsv").read_text(encoding="utf-8").splitlines():
    fields = line.split("\t")
    if len(fields) == 3 and fields[0].isdigit():
        credentials[int(fields[0])] = (fields[1], os.environ.get("RMS_TEST_PASSWORD") or fields[2])

with sync_playwright() as playwright:
    browser = playwright.chromium.launch(channel="msedge", headless=True)
    try:
        page = browser.new_page(viewport={"width": 1280, "height": 900}, has_touch=True, is_mobile=True)
        code, password = credentials[1]
        page.goto("http://localhost:5173/login")
        page.get_by_placeholder("Enter your employee code").fill(code)
        page.get_by_placeholder("Enter your password").fill(password)
        page.get_by_role("button", name="Sign In").click()
        expect(page).to_have_url(re.compile(r"/dashboard$"))
        page.goto("http://localhost:5173/all-requests")
        trigger = page.locator(".qi-trigger").first
        expect(trigger).to_be_visible()
        assert page.evaluate("matchMedia('(hover: none) and (pointer: coarse)').matches")
        trigger.tap()
        expect(trigger).to_have_attribute("aria-expanded", "true")
        popover = page.locator(".qi-popover")
        expect(popover).to_be_visible()
        expect(popover.get_by_text("Remaining Balance", exact=True)).to_be_visible()
        expect(popover).to_have_attribute("id", trigger.get_attribute("aria-controls"))
        expect(popover).to_have_attribute("role", "status")
        trigger.press("Escape")
        expect(popover).to_have_count(0)
        expect(trigger).to_have_attribute("aria-expanded", "false")
        trigger.focus()
        trigger.press("Enter")
        expect(popover).to_be_visible()
        trigger.press("Escape")
        expect(popover).to_have_count(0)
        print("quick_insights_touch_keyboard_aria=pass")
    finally:
        browser.close()
