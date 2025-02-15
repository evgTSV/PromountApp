namespace PromountApp.Api.Migrations

open FluentMigrator

[<Migration(2025021502L)>]
type AddClickLogsTable() =
    inherit Migration()
    
    override _.Up() =
        base.Create.Table("ClickLogs")
            .WithColumn("id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("client_id").AsGuid().ForeignKey().NotNullable()
            .WithColumn("campaign_id").AsGuid().ForeignKey().NotNullable()
            .WithColumn("cost").AsFloat().NotNullable()
            .WithColumn("timestamp").AsInt32().NotNullable() |> ignore

    override _.Down() =
        base.Delete.Table("ClickLogs") |> ignore