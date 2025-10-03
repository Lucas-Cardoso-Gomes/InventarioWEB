from playwright.sync_api import sync_playwright, expect

def run(playwright):
    browser = playwright.chromium.launch(headless=True)
    context = browser.new_context()
    page = context.new_page()

    # Go to the create ticket page directly
    page.goto("http://localhost:5000/Chamados/Create")

    # Expect the collaborator dropdown to NOT be visible
    expect(page.locator('select[name="ColaboradorCPF"]')).not_to_be_visible()

    # Expect the status dropdown to NOT be visible
    expect(page.locator('select[name="Status"]')).not_to_be_visible()

    # Expect the hidden status input to be present and have the value "Aberto"
    expect(page.locator('input[type="hidden"][name="Status"]')).to_have_value("Aberto")

    page.screenshot(path="jules-scratch/verification/non_admin_create_ticket.png")

    browser.close()

with sync_playwright() as playwright:
    run(playwright)