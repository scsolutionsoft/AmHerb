namespace AmHerb.Web.Models;
public record DemoScenario(string Title, string Description, string Url, string Account, string Expected);
public record DemoAccount(string Email, string Name, string Roles);
public class DemoPage
{
    public string State { get; init; } = "";
    public int Members { get; init; }
    public int Products { get; init; }
    public int Orders { get; init; }
    public int Batches { get; init; }
    public List<DemoScenario> Scenarios { get; init; } = [];
    public List<DemoAccount> Accounts { get; init; } = [];
}
