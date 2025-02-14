module PromountApp.Api.Migrations.AddCampaignTable

open FluentMigrator

[<Migration(2025021402L)>]
type AddCampaignsTable() =
    inherit Migration()
    
    override _.Up() =
        base.Create.Table("Campaigns")
            .WithColumn("campaign_id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("advertiser_id").AsGuid().ForeignKey().NotNullable()
            .WithColumn("impressions_limit").AsInt32().NotNullable()
            .WithColumn("clicks_limit").AsInt32().NotNullable()
            .WithColumn("cost_per_impression").AsFloat().NotNullable()
            .WithColumn("cost_per_click").AsFloat().NotNullable()
            .WithColumn("ad_title").AsString().NotNullable()
            .WithColumn("ad_text").AsString().NotNullable()
            .WithColumn("start_date").AsInt32().NotNullable()
            .WithColumn("end_date").AsInt32().NotNullable()
            .WithColumn("gender").AsString()
            .WithColumn("age_from").AsInt32()
            .WithColumn("age_to").AsInt32()
            .WithColumn("location").AsString() |> ignore

    override _.Down() =
        base.Delete.Table("Campaigns") |> ignore