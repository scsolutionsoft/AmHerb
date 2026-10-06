using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AmHerb.Web.Data;
public class DesignTimeFactory : IDesignTimeDbContextFactory<AmHerbDbContext>
{
    public AmHerbDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().AddEnvironmentVariables().Build();
        var connection = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection in secrets/environment.");
        return new AmHerbDbContext(new DbContextOptionsBuilder<AmHerbDbContext>().UseSqlServer(connection).Options);
    }
}
