namespace PromountApp.Api.Migrations

open FluentMigrator

[<Migration(2025021302L)>]
type AddAdvertisersTable() =
    inherit Migration()
    
    override _.Up() =
        base.Create.Table("Advertisers")
            .WithColumn("advertiser_id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("name").AsString().NotNullable() |> ignore

    override _.Down() =
        base.Delete.Table("Advertisers") |> ignore