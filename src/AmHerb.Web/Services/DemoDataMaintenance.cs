using System.Data;
using System.Data.Common;
using System.Text.Json;
using AmHerb.Web.Data;
using AmHerb.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AmHerb.Web.Services;

// Development maintenance only. A baseline is permitted before any business transactions exist.
public class DemoDataMaintenance(AmHerbDbContext db, IWebHostEnvironment environment)
{
    private void Guard()
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing")) throw new BusinessException("ระบบข้อมูลจำลองใช้งานได้เฉพาะเครื่องพัฒนาและทดสอบ");
        var name = db.Database.GetDbConnection().Database;
        if (name != "AmHerb" && !name.StartsWith("AmHerb_Test_", StringComparison.Ordinal)) throw new BusinessException("ฐานข้อมูลนี้ไม่ได้รับอนุญาตให้จัดการข้อมูลจำลอง");
    }
    private Dictionary<string, Type> NumericTables() => db.Model.GetEntityTypes()
        .Where(x => x.FindPrimaryKey()?.Properties.Count == 1)
        .Where(x => x.FindPrimaryKey()!.Properties[0].Name == "Id" && x.FindPrimaryKey()!.Properties[0].ClrType is var type && (type == typeof(long) || type == typeof(int)))
        .GroupBy(x => x.GetTableName()!).ToDictionary(g => g.Key, g => g.First().ClrType);
    private static string Quote(string name) => "[" + name.Replace("]", "]]") + "]";
    private static string Backup(string table) => "[dbo]." + Quote("AmHerbDemoBaseline_" + table);
    private List<string> Tables() => db.Model.GetEntityTypes().Select(x => x.GetTableName()!).Distinct().ToList();
    private DbCommand Command(string sql)
    {
        var command = db.Database.GetDbConnection().CreateCommand(); command.CommandText = sql; command.CommandTimeout = 120;
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction(); return command;
    }
    private async Task Execute(string sql, params (string Name, object Value)[] parameters)
    {
        await using var command = Command(sql);
        foreach (var item in parameters) { var parameter = command.CreateParameter(); parameter.ParameterName = item.Name; parameter.Value = item.Value; command.Parameters.Add(parameter); }
        await command.ExecuteNonQueryAsync();
    }
    public async Task CreateBaselineAsync()
    {
        Guard();
        if (await db.SystemSettings.AnyAsync(x => x.Key == "Demo.State")) throw new BusinessException("มีชุดข้อมูลจำลองอยู่แล้ว กรุณารีเซ็ตก่อนสร้างชุดใหม่");
        if (await db.Orders.AnyAsync() || await db.InventoryBatches.AnyAsync() || await db.TokenLedger.AnyAsync() || await db.JournalEntries.AnyAsync()
            || await db.CreditInvoices.AnyAsync() || await db.StockTransfers.AnyAsync() || await db.Conversations.AnyAsync() || await db.Promotions.AnyAsync())
            throw new BusinessException("มีข้อมูลธุรกิจอยู่แล้ว ไม่สามารถเริ่มชุดข้อมูลจำลองและระบบรีเซ็ตในฐานนี้ได้");
        await db.Database.OpenConnectionAsync();
        var maxima = new Dictionary<string, long>();
        foreach (var table in NumericTables().Keys)
        {
            await using var command = Command($"SELECT COALESCE(MAX([Id]),0) FROM [dbo].{Quote(table)}");
            maxima[table] = Convert.ToInt64(await command.ExecuteScalarAsync());
        }
        var settings = await db.SystemSettings.AsNoTracking().ToListAsync();
        var users = await db.Users.Select(x => x.Id).ToListAsync();
        foreach (var (table, max) in maxima) db.SystemSettings.Add(new SystemSetting { Key = "Demo.Baseline." + table, Value = max.ToString() });
        foreach (var setting in settings) db.SystemSettings.Add(new SystemSetting { Key = "Demo.Original." + setting.Key, Value = setting.Value });
        db.SystemSettings.Add(new SystemSetting { Key = "Demo.Baseline.Users", Value = JsonSerializer.Serialize(users) });
        foreach (var snapshot in await db.ReportSnapshots.AsNoTracking().ToListAsync())
            db.SystemSettings.Add(new SystemSetting { Key = "Demo.Snapshot." + snapshot.Id, Value = JsonSerializer.Serialize(snapshot) });
        db.SystemSettings.Add(new SystemSetting { Key = "Demo.State", Value = "Preparing" });
        await db.SaveChangesAsync();
        await CompleteBaselineSnapshotAsync();
    }
    public async Task CompleteBaselineSnapshotAsync()
    {
        Guard();
        var settings = await db.SystemSettings.AsNoTracking().ToDictionaryAsync(x => x.Key, x => x.Value);
        if (!settings.ContainsKey("Demo.State")) throw new BusinessException("ไม่มีจุดเริ่มต้นของชุดข้อมูลจำลอง");
        var tables = Tables(); var numeric = NumericTables();
        if (numeric.Keys.Any(table => !settings.ContainsKey("Demo.Baseline." + table))) throw new BusinessException("ข้อมูลจุดเริ่มต้นไม่ครบ");
        var baselineUsers = JsonSerializer.Deserialize<List<string>>(settings["Demo.Baseline.Users"])!;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var existing = await BackupCount();
        if (existing == tables.Count) { await transaction.CommitAsync(); return; }
        if (existing != 0) throw new BusinessException("พบสำเนาจุดเริ่มต้นไม่ครบ หยุดดำเนินการเพื่อรักษาข้อมูล");
        var userNames = baselineUsers.Select((_, i) => "@u" + i).ToArray();
        var userParameters = baselineUsers.Select((value, i) => (Name: "@u" + i, Value: (object)value)).ToArray();
        foreach (var table in tables)
        {
            string predicate = "1=1"; (string Name, object Value)[] parameters = [];
            if (numeric.ContainsKey(table)) { predicate = "[Id]<=@max"; parameters = [("@max", long.Parse(settings["Demo.Baseline." + table]))]; }
            else if (table == "MemberClosures") { predicate = "[AncestorMemberId]<=@max AND [DescendantMemberId]<=@max"; parameters = [("@max", long.Parse(settings["Demo.Baseline.Members"]))]; }
            else if (table is "AspNetUsers" or "AspNetUserRoles" or "AspNetUserLogins" or "AspNetUserTokens")
            {
                predicate = baselineUsers.Count == 0 ? "1=0" : (table == "AspNetUsers" ? "[Id]" : "[UserId]") + " IN (" + string.Join(',', userNames) + ")";
                parameters = userParameters;
            }
            await Execute($"SELECT * INTO {Backup(table)} FROM [dbo].{Quote(table)} WHERE {predicate}", parameters);
        }
        foreach (var setting in settings.Where(x => x.Key.StartsWith("Demo.Original.", StringComparison.Ordinal)))
            await Execute($"UPDATE {Backup("SystemSettings")} SET [Value]=@value WHERE [Key]=@key", ("@value", setting.Value), ("@key", setting.Key[14..]));
        foreach (var entry in settings.Where(x => x.Key.StartsWith("Demo.Snapshot.", StringComparison.Ordinal)))
        {
            var snapshot = JsonSerializer.Deserialize<ReportSnapshot>(entry.Value)!;
            await Execute($"UPDATE {Backup("ReportSnapshots")} SET [Orders]=@orders,[Sales]=@sales,[TokenLiability]=@tokens WHERE [Id]=@id", ("@orders", snapshot.Orders), ("@sales", snapshot.Sales), ("@tokens", snapshot.TokenLiability), ("@id", snapshot.Id));
        }
        await transaction.CommitAsync();
    }
    private async Task<int> BackupCount()
    {
        var count = 0;
        foreach (var table in Tables())
        {
            await using var command = Command("SELECT COUNT(*) FROM sys.tables WHERE schema_id=SCHEMA_ID('dbo') AND name=@name");
            var parameter = command.CreateParameter(); parameter.ParameterName = "@name"; parameter.Value = "AmHerbDemoBaseline_" + table; command.Parameters.Add(parameter);
            count += Convert.ToInt32(await command.ExecuteScalarAsync());
        }
        return count;
    }
    private async Task RestoreBackup(string table)
    {
        var columns = new List<string>(); var identity = false;
        await using (var command = Command("SELECT name,is_identity FROM sys.columns WHERE object_id=OBJECT_ID(@table) AND system_type_id<>189 AND is_computed=0 ORDER BY column_id"))
        {
            var parameter = command.CreateParameter(); parameter.ParameterName = "@table"; parameter.Value = "dbo." + table; command.Parameters.Add(parameter);
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) { columns.Add(Quote(reader.GetString(0))); identity |= reader.GetBoolean(1); }
        }
        var names = string.Join(',', columns);
        if (identity) await Execute($"SET IDENTITY_INSERT [dbo].{Quote(table)} ON");
        try { await Execute($"INSERT INTO [dbo].{Quote(table)} ({names}) SELECT {names} FROM {Backup(table)}"); }
        finally { if (identity) await Execute($"SET IDENTITY_INSERT [dbo].{Quote(table)} OFF"); }
    }
    public async Task ResetAsync(string actorId)
    {
        Guard();
        var settings = await db.SystemSettings.AsNoTracking().ToDictionaryAsync(x => x.Key, x => x.Value);
        if (!settings.ContainsKey("Demo.State")) throw new BusinessException("ไม่มีชุดข้อมูลจำลองที่สามารถรีเซ็ตได้");
        var numeric = NumericTables().Keys.ToList();
        if (numeric.Any(table => !settings.ContainsKey("Demo.Baseline." + table))) throw new BusinessException("ข้อมูลจุดเริ่มต้นไม่ครบ หยุดรีเซ็ตเพื่อรักษาข้อมูลเดิม");
        var baselineUsers = JsonSerializer.Deserialize<List<string>>(settings["Demo.Baseline.Users"]) ?? throw new BusinessException("ไม่พบข้อมูลบัญชีเดิม");
        var tables = Tables();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var backupCount = await BackupCount();
        if (backupCount != 0 && backupCount != tables.Count) throw new BusinessException("สำเนาข้อมูลตั้งต้นไม่ครบ จึงไม่รีเซ็ตข้อมูล");
        var triggers = new List<(string Name, string Table)>();
        await using (var command = Command("SELECT t.name, OBJECT_NAME(t.parent_id) FROM sys.triggers t WHERE t.is_disabled=0 AND OBJECT_SCHEMA_NAME(t.parent_id)='dbo'"))
        await using (var reader = await command.ExecuteReaderAsync())
            while (await reader.ReadAsync()) if (tables.Contains(reader.GetString(1))) triggers.Add((reader.GetString(0), reader.GetString(1)));
        // SQL schema locks remain held until commit, preventing concurrent writes during the reset.
        foreach (var table in tables) await Execute($"ALTER TABLE [dbo].{Quote(table)} NOCHECK CONSTRAINT ALL");
        foreach (var trigger in triggers) await Execute($"DISABLE TRIGGER [dbo].{Quote(trigger.Name)} ON [dbo].{Quote(trigger.Table)}");
        if (backupCount == tables.Count)
        {
            foreach (var table in tables) await Execute($"DELETE FROM [dbo].{Quote(table)}");
            foreach (var table in tables) await RestoreBackup(table);
        }
        else
        {
        var memberMax = long.Parse(settings["Demo.Baseline.Members"]);
        await Execute("DELETE FROM [dbo].[MemberClosures] WHERE [AncestorMemberId]>@memberMax OR [DescendantMemberId]>@memberMax", ("@memberMax", memberMax));
        var userNames = baselineUsers.Select((_, i) => "@u" + i).ToList();
        var userParameters = baselineUsers.Select((value, i) => (Name: "@u" + i, Value: (object)value)).ToArray();
        var userPredicate = baselineUsers.Count == 0 ? "1=1" : "[UserId] NOT IN (" + string.Join(',', userNames) + ")";
        foreach (var table in new[] { "AspNetUserRoles", "AspNetUserLogins", "AspNetUserTokens" })
            await Execute($"DELETE FROM [dbo].{Quote(table)} WHERE {userPredicate}", userParameters);
        foreach (var table in numeric)
            await Execute($"DELETE FROM [dbo].{Quote(table)} WHERE [Id]>@baseline", ("@baseline", long.Parse(settings["Demo.Baseline." + table])));
        var accountPredicate = baselineUsers.Count == 0 ? "1=1" : "[Id] NOT IN (" + string.Join(',', userNames) + ")";
        await Execute($"DELETE FROM [dbo].[AspNetUsers] WHERE {accountPredicate}", userParameters);
        foreach (var setting in settings.Where(x => x.Key.StartsWith("Demo.Original.", StringComparison.Ordinal)))
            await Execute("UPDATE [dbo].[SystemSettings] SET [Value]=@value WHERE [Key]=@key", ("@value", setting.Value), ("@key", setting.Key[14..]));
        foreach (var entry in settings.Where(x => x.Key.StartsWith("Demo.Snapshot.", StringComparison.Ordinal)))
        {
            var snapshot = JsonSerializer.Deserialize<ReportSnapshot>(entry.Value)!;
            await Execute("UPDATE [dbo].[ReportSnapshots] SET [Orders]=@orders,[Sales]=@sales,[TokenLiability]=@tokens WHERE [Id]=@id", ("@orders", snapshot.Orders), ("@sales", snapshot.Sales), ("@tokens", snapshot.TokenLiability), ("@id", snapshot.Id));
        }
        }
        foreach (var table in tables) await Execute($"ALTER TABLE [dbo].{Quote(table)} WITH CHECK CHECK CONSTRAINT ALL");
        foreach (var trigger in triggers) await Execute($"ENABLE TRIGGER [dbo].{Quote(trigger.Name)} ON [dbo].{Quote(trigger.Table)}");
        if (backupCount == tables.Count) foreach (var table in tables) await Execute($"DROP TABLE {Backup(table)}");
        await transaction.CommitAsync(); db.ChangeTracker.Clear();
        db.AuditLogs.Add(new AuditLog { ActorId = actorId, Action = "Demo.Reset", Subject = "AmHerb", Detail = "Removed sandbox records after the recorded baseline; preserved original users, catalog, images and settings." });
        await db.SaveChangesAsync();
    }
}
