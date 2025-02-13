module ClientsServiceTests

open System
open PromountApp.Api.Models
open PromountApp.Api.Services
open Microsoft.Extensions.DependencyInjection
open PromountApp.Api.Tests.Helpers
open PromountApp.Api.Utils
open Xunit

let serviceProvider =
                let services = ServiceCollection()
                configureInMemoryDb services
                services
                    .BuildServiceProvider()
         
let getTestClients (count: int) : Client[] =
    [|
        for i in 1..count -> {
            client_id = Guid.NewGuid()
            login = $"login {i}"
            age = Random.Shared.Next(0, 100)
            location = $"location {i}"
            gender = Array.randomChoice [| "MALE"; "FEMALE" |]
        }
    |]

[<Fact>]
let ``GetClient returns existing clients`` () =
    let clients = getTestClients 10
    let context = getDbContext serviceProvider
    context.AddRange(clients)
    context.SaveChanges() |> ignore
    let service = ClientsService(context) :> IClientsService
    Assert.True(
        clients |> Array.forall (fun expected ->
            match service.GetClient(expected.client_id)
                  |> Async.RunSynchronously
            with
            | Success actual -> expected = actual
            | _ -> false)
        )
    