namespace PromountApp.Api.Migrations

open FluentMigrator

[<Migration(2025022001L)>]
type AddBanLogsTable() =
    inherit Migration()
    
    override _.Up() =
        base.Create.Table("BanLogs")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("advertiser_id").AsGuid().ForeignKey().NotNullable()
            .WithColumn("campaign_id").AsGuid().ForeignKey().NotNullable()
            .WithColumn("probability").AsCustom("jsonb").NotNullable() |> ignore

    override _.Down() =
        base.Delete.Table("BanLogs") |> ignore

