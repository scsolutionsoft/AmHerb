using AmHerb.Web.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AmHerb.Tests;
[Collection("SQL")]
public class DeploymentTests(SqlFixture fixture)
{
    [SqlFact] public async Task Idempotent_SQL_migration_script_replays_without_errors_or_duplicate_seed_data()
    {
        await using var db = fixture.Db(); var migrator = db.GetService<IMigrator>();
        var sql = migrator.GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        var batches = System.Text.RegularExpressions.Regex.Split(sql, @"^GO\s*$", System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        // SSMS/sqlcmd use a non-MARS session; a SQL BEGIN TRAN spans GO batches.
        var connectionBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(fixture.Connection) { MultipleActiveResultSets = false };
        await using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionBuilder.ConnectionString);
        await connection.OpenAsync();
        try
        {
            foreach (var batch in batches.Where(x => !string.IsNullOrWhiteSpace(x)))
            { await using var command = connection.CreateCommand(); command.CommandText = batch; await command.ExecuteNonQueryAsync(); }
        }
        finally { await connection.CloseAsync(); }
        Assert.Equal(9, await db.Products.CountAsync(x => x.Id <= 9)); Assert.Equal(db.Database.GetMigrations().Count(), (await db.Database.GetAppliedMigrationsAsync()).Count());
    }
}

