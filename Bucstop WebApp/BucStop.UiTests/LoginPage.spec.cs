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
        private const string LoginUrl = "http://localhost:7182/Account/Login";

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
        [Fact]
        public async Task Incorrect_Email_Does_Not_Login_And_Shows_Error()
        {
            // arrange: open the login page
            var page = await NewPageAsync(1280, 800);
            await page.GotoAsync(LoginUrl);

            // find email and login button
            var email = page.GetByRole(AriaRole.Textbox, new() { Name = "EMAIL" });
            var loginBtn = page.GetByRole(AriaRole.Button, new() { Name = "LOGIN" });

            // act: enter a non-existent / incorrect email with a valid-looking password
            await email.FillAsync("wrong.user.does.not.exist@example.com");
            await loginBtn.ClickAsync();

            // give the page a moment to process the postback / request
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // assert: still on login page (no successful navigation)
            Assert.Contains("/Account/Login", page.Url, StringComparison.OrdinalIgnoreCase);

            // assert: an error / validation message is shown
            // try common asp.net core identity selectors and a generic "login error" hook
            var errorLocator = page.Locator(
                ".validation-summary-errors, .field-validation-error, .text-danger, [data-test=\"login-error\"]"
            ).First;

            var errorVisible = await errorLocator.IsVisibleAsync()
                .ContinueWith(t => t.Status == TaskStatus.RanToCompletion && t.Result);

            Assert.True(errorVisible);

            // if visible, sanity-check it mentions invalid credentials
            if (errorVisible)
            {
                var errorText = await errorLocator.InnerTextAsync();
                // check for the real message your app shows
                Assert.Contains("Only ETSU students can play", errorText, StringComparison.OrdinalIgnoreCase);
            }
        }

    }
}
