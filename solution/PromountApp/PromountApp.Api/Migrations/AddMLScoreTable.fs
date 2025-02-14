namespace PromountApp.Api.Migrations

open FluentMigrator

[<Migration(2025021401L)>]
type AddMLScoreTable() =
    inherit Migration()
    
    override _.Up() =
        base.Create.Table("MLScores")
            .WithColumn("client_id").AsGuid().ForeignKey().NotNullable()
            .WithColumn("advertiser_id").AsGuid().ForeignKey().NotNullable()
            .WithColumn("score").AsInt32().NotNullable() |> ignore

    override _.Down() =
        base.Delete.Table("MLScores") |> ignore