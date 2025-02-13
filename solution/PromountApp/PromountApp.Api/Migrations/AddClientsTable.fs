namespace PromountApp.Api.Migrations

open FluentMigrator

[<Migration(2025021301L)>]
type AddClientsTable() =
    inherit Migration()
    
    override _.Up() =
        base.Create.Table("Clients")
            .WithColumn("client_id").AsGuid().PrimaryKey().NotNullable()
            .WithColumn("login").AsString().NotNullable()
            .WithColumn("age").AsInt32().NotNullable()
            .WithColumn("location").AsString().NotNullable()
            .WithColumn("gender").AsString().NotNullable()
        |> ignore

    override _.Down() =
        base.Delete.Table("Clients")
        |> ignore