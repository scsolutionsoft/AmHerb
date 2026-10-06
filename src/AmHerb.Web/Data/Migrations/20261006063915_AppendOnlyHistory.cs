using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AmHerb.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AppendOnlyHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // EXEC keeps CREATE TRIGGER first in its batch, including idempotent IF wrappers.
            migrationBuilder.Sql("EXEC(N'CREATE TRIGGER [dbo].[TR_TokenLedger_AppendOnly] ON [dbo].[TokenLedger] INSTEAD OF UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51001, ''Token ledger is append-only; use compensating entries.'', 1; END;');");
            migrationBuilder.Sql("EXEC(N'CREATE TRIGGER [dbo].[TR_AuditLogs_AppendOnly] ON [dbo].[AuditLogs] INSTEAD OF UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51002, ''Audit history is append-only.'', 1; END;');");
            migrationBuilder.Sql("EXEC(N'CREATE TRIGGER [dbo].[TR_InventoryTransactions_AppendOnly] ON [dbo].[InventoryTransactions] INSTEAD OF UPDATE, DELETE AS BEGIN SET NOCOUNT ON; THROW 51003, ''Inventory history is append-only.'', 1; END;');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER [dbo].[TR_InventoryTransactions_AppendOnly];");
            migrationBuilder.Sql("DROP TRIGGER [dbo].[TR_AuditLogs_AppendOnly];");
            migrationBuilder.Sql("DROP TRIGGER [dbo].[TR_TokenLedger_AppendOnly];");
        }
    }
}
