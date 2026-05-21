using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using Xunit;

namespace FakeInfo.IntegrationTests;

public class FrontendTests : PageTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true
        };
    }
    
    //Aa

    private const string BaseUrl = "http://localhost:5028/index.html";
    [Fact]
    public async Task GeneratePerson_Works()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.ClickAsync("#generatePersonBtn");

        var result = Page.Locator("#singleResult");
        await Expect(result).Not.ToBeEmptyAsync();
        await Expect(result).ToContainTextAsync("CPR");
    }

    [Fact]
    public async Task GenerateBulk_Works()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.FillAsync("#bulkCount", "5");
        await Page.ClickAsync("#generateBulkBtn");

        await Expect(Page.Locator("#bulkResult")).Not.ToBeEmptyAsync();
    }

    [Fact]
    public async Task Bulk_MinBoundary_2_Works()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.FillAsync("#bulkCount", "2");
        await Page.ClickAsync("#generateBulkBtn");

        await Expect(Page.Locator("#bulkResult")).Not.ToBeEmptyAsync();
    }

    [Fact]
    public async Task Bulk_MaxBoundary_100_Works()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.FillAsync("#bulkCount", "100");
        await Page.ClickAsync("#generateBulkBtn");

        await Expect(Page.Locator("#bulkResult")).Not.ToBeEmptyAsync();
    }

    [Fact]
    public async Task Bulk_Invalid_1_Shows_Error()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.FillAsync("#bulkCount", "1");
        await Page.ClickAsync("#generateBulkBtn");

        await Expect(Page.Locator("#errorMessage")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Bulk_OverMax_Shows_Error()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.FillAsync("#bulkCount", "101");
        await Page.ClickAsync("#generateBulkBtn");

        await Expect(Page.Locator("#errorMessage")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Bulk_EmptyInput_Shows_Error()
    {
        await Page.GotoAsync(BaseUrl);
        await Page.FillAsync("#bulkCount", "");
        await Page.ClickAsync("#generateBulkBtn");

        await Expect(Page.Locator("#errorMessage")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task GeneratePerson_ApiFails_Shows_Error()
    {
        await Page.RouteAsync("**/api/person/full", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 500,
                ContentType = "application/json",
                Body = "{}"
            });
        });

        await Page.GotoAsync(BaseUrl);
        await Page.ClickAsync("#generatePersonBtn");

        await Expect(Page.Locator("#errorMessage")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task GenerateBulk_ApiFails_Shows_Error()
    {
        await Page.RouteAsync("**/api/person/bulk?count=5", async route =>
        {
            await route.FulfillAsync(new RouteFulfillOptions
            {
                Status = 500,
                ContentType = "application/json",
                Body = "[]"
            });
        });

        await Page.GotoAsync(BaseUrl);
        await Page.FillAsync("#bulkCount", "5");
        await Page.ClickAsync("#generateBulkBtn");

        await Expect(Page.Locator("#errorMessage")).ToBeVisibleAsync();
    }
}