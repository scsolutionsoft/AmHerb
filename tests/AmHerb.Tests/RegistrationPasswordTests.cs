using AmHerb.Web.Domain;
using AmHerb.Web.Services;
namespace AmHerb.Tests;
public class RegistrationPasswordTests
{
    [Theory]
    [InlineData("abcde", true)]
    [InlineData("12345", true)]
    [InlineData("abcdefghij", true)]
    [InlineData("abcd", false)]
    [InlineData("abcdefghijk", false)]
    [InlineData("     ", false)]
    [InlineData(null, false)]
    public async Task Password_length_and_simple_values(string? password, bool accepted)
    {
        var result = await new SimplePasswordValidator().ValidateAsync(null!, new AppUser(), password);
        Assert.Equal(accepted, result.Succeeded);
    }
}
