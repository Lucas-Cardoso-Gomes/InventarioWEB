from playwright.sync_api import sync_playwright, expect

def run(playwright):
    browser = playwright.chromium.launch(headless=True)
    context = browser.new_context()
    page = context.new_page()

    # Go to login page
    page.goto("http://localhost:80/Account/Login")

    # Fill in login form
    page.get_by_label("Login").fill("Admin")
    page.get_by_label("Senha").fill("Admin")
    page.get_by_role("button", name="Entrar").click()

    # Go to user management page
    page.goto("http://localhost:80/Users")

    # Check if the button is visible
    expect(page.get_by_role("button", name="Criar Usuários de Colaboradores")).to_be_visible()

    # Take a screenshot
    page.screenshot(path="jules-scratch/verification/verification.png")

    browser.close()

with sync_playwright() as playwright:
    run(playwright)