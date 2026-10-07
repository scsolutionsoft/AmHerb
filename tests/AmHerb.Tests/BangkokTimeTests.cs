using System.Globalization;
using AmHerb.Web.Services;
namespace AmHerb.Tests;

public class BangkokTimeTests
{
    [Theory]
    [InlineData("2026-10-07T08:00:00Z", "07/10/2026 15:00")]
    [InlineData("2026-12-31T20:30:00Z", "01/01/2027 03:30")]
    [InlineData("2026-01-01T00:00:00Z", "01/01/2026 07:00")]
    [InlineData("2026-07-01T00:00:00Z", "01/07/2026 07:00")]
    public void Database_UTC_dates_render_as_Bangkok_without_daylight_saving(string utc, string expected)
        => Assert.Equal(expected, Bangkok.Format(DateTime.Parse(utc, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal)));

    [Fact] public void Missing_date_uses_placeholder() => Assert.Equal("—", Bangkok.Format(null));

    [Fact] public void SQL_unspecified_kind_and_Thai_request_culture_preserve_business_date()
    {
        var previous = CultureInfo.CurrentCulture;
        try { CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("th-TH"); Assert.Equal("07/10/2026 15:00", Bangkok.Format(new DateTime(2026, 10, 7, 8, 0, 0, DateTimeKind.Unspecified))); }
        finally { CultureInfo.CurrentCulture = previous; }
    }
}
