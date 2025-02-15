namespace PromountApp.Api.Migrations

open FluentMigrator

[<Migration(2025021503L)>]
type AddФвыTable() =
    inherit Migration()
    
    override _.Up() =
        base.Create.Table("Ads")
            .WithColumn("ad_id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("ad_title").AsString().NotNullable()
            .WithColumn("ad_text").AsString().NotNullable()
            .WithColumn("advertiser_id").AsGuid().ForeignKey().NotNullable()
        |> ignore

    override _.Down() =
        base.Delete.Table("Ads") |> ignore