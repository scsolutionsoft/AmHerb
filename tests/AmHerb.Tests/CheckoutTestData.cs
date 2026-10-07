using Microsoft.Playwright;
namespace AmHerb.Tests;
public static class CheckoutTestData
{
    public static byte[] Slip => Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aX1sAAAAASUVORK5CYII=");
    public static FilePayload SlipFile => new() { Name = "test-slip.png", MimeType = "image/png", Buffer = Slip };
    public static async Task Address(IPage page, bool selectVillage = false)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "ถัดไป: ที่อยู่จัดส่ง" }).ClickAsync();
        await page.Locator("#Checkout_HouseNumber").FillAsync("99/1");
        await page.Locator("[data-bs-target='#address-picker']").ClickAsync();
        await page.Locator("#address-province").SelectOptionAsync(selectVillage ? "20" : "10");
        await page.Locator("#address-district").SelectOptionAsync(selectVillage ? "2010" : "1001");
        await page.Locator("#Checkout_SubdistrictCode").SelectOptionAsync(selectVillage ? "201001" : "100101");
        if (selectVillage) await page.Locator("#Checkout_VillageCode").SelectOptionAsync("20100101");
        await page.Locator("[data-address-confirm]").ClickAsync();
        await page.Locator("#Checkout_ShippingProviderId").SelectOptionAsync("1");
        await page.GetByRole(AriaRole.Button, new() { Name = "ถัดไป: ตรวจสอบ" }).ClickAsync();
    }
}
