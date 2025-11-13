using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace BucStop.UiTests
{
    public class LoginPageSpec : IAsyncLifetime
    {
        // Change this if your local port/URL differs when BucStop runs
        private const string LoginUrl = "https://localhost:7182/Account/Login";

        private IPlaywright _playwright = default!;
        private IBrowser _browser = default!;

        public async Task InitializeAsync()
        {
            // Auto-install browsers on first test run (avoids separate playwright install step)
            _ = Microsoft.Playwright.Program.Main(new[] { "install" }); // no await


            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
        }

        public async Task DisposeAsync()
        {
            await _browser.DisposeAsync();
            _playwright.Dispose();
        }

        private async Task<IPage> NewPageAsync(int width, int height)
        {
            var context = await _browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = width, Height = height }
            });
            return await context.NewPageAsync();
        }

        [Fact]
        public async Task LoginPage_Renders_And_Key_Elements_Exist()
        {
            var page = await NewPageAsync(1280, 800);
            await page.GotoAsync(LoginUrl);

            // Title contains "BucStop" (adjust if your title differs)
            var title = await page.TitleAsync();
            Assert.Matches(new Regex("BucStop", RegexOptions.IgnoreCase), title);

            // Find the Email input and Login button by accessible role + label
            var email = page.GetByRole(AriaRole.Textbox, new() { Name = "EMAIL" });
            var loginBtn = page.GetByRole(AriaRole.Button, new() { Name = "LOGIN" });

            Assert.True(await email.IsVisibleAsync());
            Assert.True(await loginBtn.IsVisibleAsync());
            Assert.True(await loginBtn.IsEnabledAsync());
        }

        [Fact]
        public async Task Empty_Submit_Stays_On_Login_And_Shows_Validation_If_Available()
        {
            var page = await NewPageAsync(1280, 800);
            await page.GotoAsync(LoginUrl);

            var loginBtn = page.GetByRole(AriaRole.Button, new() { Name = "LOGIN" });
            await loginBtn.ClickAsync();

            // Expect no successful navigation; still on Login
            Assert.Contains("/Account/Login", page.Url, StringComparison.OrdinalIgnoreCase);

            // Try common ASP.NET validation selectors if present (optional)
            var hasValidation =
                await page.Locator(".validation-summary-errors, .field-validation-error").First.IsVisibleAsync()
                    .ContinueWith(t => t.Status == TaskStatus.RanToCompletion && t.Result);

            // Pass if we either saw validation OR simply remained on the login page
            Assert.True(hasValidation || page.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase));
        }

        // TEST CASE: Verify successful login with valid credentials
        // NOTE: This test includes a graceful skip if the login page or backend is unavailable.
        // Future devs should remove the skip once the login system and test credentials are stable.
        [Fact]
        public async Task Login_With_Valid_Email_Shows_Success_Message_Or_Navigates()
        {
            // Create a new page instance in the browser
            var page = await NewPageAsync(1280, 800);

            try
            {
                // Try to reach the login page; If it's down, skip instead of failing
                var response = await page.GotoAsync(LoginUrl);
                if (response == null || !response.Ok)
                {
                    // Gracefully skip the test if the page can't be reached
                    throw new SkipException("Skipping test: Login page is not reachable (local server may be down).");
                }

                // Fill in valid login credentials
                // Replace with valid test credentials once available
                await page.GetByRole(AriaRole.Textbox, new() { Name = "EMAIL" }).FillAsync("testuser@example.com");
                await page.GetByRole(AriaRole.Textbox, new() { Name = "PASSWORD" }).FillAsync("TestPassword123!");

                // Click the login button
                await page.GetByRole(AriaRole.Button, new() { Name = "LOGIN" }).ClickAsync();

                // Wait briefly for navigation or success message
                await page.WaitForTimeoutAsync(2000);

                // Check if login succeeded:
                // Either the URL changed (navigated away from login)
                // OR a success message or dashboard element is visible
                bool loggedIn =
                    !page.Url.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase) ||
                    await page.Locator("text=Welcome").IsVisibleAsync() ||
                    await page.Locator("text=Dashboard").IsVisibleAsync();

                // Assert that login was successful
                Assert.True(loggedIn, "Expected successful login or redirect after valid credentials.");
            }
            catch (SkipException)
            {
                // SkipException allows xUnit to skip this test gracefully
                throw;
            }
            catch (Exception ex)
            {
                // Catch-all for unexpected errors (useful for debugging)
                throw new Xunit.Sdk.XunitException($"Login test failed due to unexpected error: {ex.Message}");
            }
        }

        // Custom exception to allow graceful skipping in xUnit
        private class SkipException : Xunit.Sdk.XunitException
        {
            public SkipException(string message) : base(message) { }
        }

    }
}
