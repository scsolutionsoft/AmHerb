
using Microsoft.EntityFrameworkCore.Migrations;

namespace AmHerb.Web.Data.Migrations;



public partial class DeliveryDemoBaseline : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        IF OBJECT_ID(N'dbo.AmHerbDemoBaseline_Orders', N'U') IS NOT NULL
        BEGIN
            IF COL_LENGTH(N'dbo.AmHerbDemoBaseline_Orders', N'HouseNumber') IS NULL
                ALTER TABLE dbo.AmHerbDemoBaseline_Orders ADD
                    HouseNumber nvarchar(100) NOT NULL DEFAULT N'',
                    AddressExtra nvarchar(300) NOT NULL DEFAULT N'',
                    SubdistrictCode nvarchar(8) NOT NULL DEFAULT N'',
                    VillageCode nvarchar(8) NULL,
                    VillageName nvarchar(160) NOT NULL DEFAULT N'',
                    PostalCode nvarchar(5) NOT NULL DEFAULT N'',
                    SelectedCarrier nvarchar(120) NOT NULL DEFAULT N'',
                    SlipRequired bit NOT NULL DEFAULT 0;
            IF OBJECT_ID(N'dbo.AmHerbDemoBaseline_PaymentSlips', N'U') IS NULL
                SELECT TOP (0) * INTO dbo.AmHerbDemoBaseline_PaymentSlips FROM dbo.PaymentSlips;
            IF OBJECT_ID(N'dbo.AmHerbDemoBaseline_StoreShippingOptions', N'U') IS NULL
                SELECT TOP (0) * INTO dbo.AmHerbDemoBaseline_StoreShippingOptions FROM dbo.StoreShippingOptions;
            IF OBJECT_ID(N'dbo.AmHerbDemoBaseline_ShippingProviders', N'U') IS NULL
                SELECT * INTO dbo.AmHerbDemoBaseline_ShippingProviders FROM dbo.ShippingProviders WHERE Id <= 4;
        END;
        IF OBJECT_ID(N'dbo.AmHerbDemoBaseline_Payments', N'U') IS NOT NULL
            AND COL_LENGTH(N'dbo.AmHerbDemoBaseline_Payments', N'SubmittedReference') IS NULL
            ALTER TABLE dbo.AmHerbDemoBaseline_Payments ADD SubmittedReference nvarchar(200) NULL, SubmittedAt datetime2 NULL;
        IF OBJECT_ID(N'dbo.AmHerbDemoBaseline_Shipments', N'U') IS NOT NULL
            ALTER TABLE dbo.AmHerbDemoBaseline_Shipments ALTER COLUMN Carrier nvarchar(120) NOT NULL;
        IF EXISTS (SELECT 1 FROM dbo.SystemSettings WHERE [Key] = N'Demo.State')
        BEGIN
            INSERT INTO dbo.SystemSettings ([Key], [Value])
            SELECT v.[Key], v.[Value] FROM (VALUES
                (N'Demo.Baseline.PaymentSlips', N'0'),
                (N'Demo.Baseline.StoreShippingOptions', N'0'),
                (N'Demo.Baseline.ShippingProviders', N'4')
            ) v([Key], [Value]) WHERE NOT EXISTS (SELECT 1 FROM dbo.SystemSettings s WHERE s.[Key] = v.[Key]);
        END;
        """);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql("""
        DROP TABLE IF EXISTS dbo.AmHerbDemoBaseline_PaymentSlips;
        DROP TABLE IF EXISTS dbo.AmHerbDemoBaseline_StoreShippingOptions;
        DROP TABLE IF EXISTS dbo.AmHerbDemoBaseline_ShippingProviders;
        DELETE FROM dbo.SystemSettings WHERE [Key] IN (N'Demo.Baseline.PaymentSlips', N'Demo.Baseline.StoreShippingOptions', N'Demo.Baseline.ShippingProviders');
        -- Extra nullable/defaulted columns in the old snapshots can remain:
        -- restore explicitly selects the current application's columns.
        """);
}
