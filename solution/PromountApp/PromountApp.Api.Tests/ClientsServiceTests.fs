namespace PromountApp.Api.Tests

open System
open System.Net
open NetTopologySuite.Utilities
open PromountApp.Api.Models
open PromountApp.Api.Services
open Microsoft.Extensions.DependencyInjection
open PromountApp.Api.Tests.Helpers
open PromountApp.Api.Tests.Helpers.TestMonad
open PromountApp.Api.Utils
open Xunit
open Xunit.Extensions.AssemblyFixture

type ClientsServiceTests(fixture: PromountTestContainers) =
     
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
    let ``Bulk insertion return inserted clients and 201`` () = task {
        let clients = getTestClients 10
        let! response = fixture.Post<_, Client[]> "clients/bulk" clients
        
        test {
            let! successResponse = isSuccess response
            let! _ = statusCode HttpStatusCode.Created response
            let! _ = where (fun c -> c |> arraysEquivalent clients) "Полученные объекты не совпадают" successResponse

            Assert.True(true)
        }
        |> function
        | TestResult.Success _ -> ()
        | TestResult.Failure msg -> Assert.True(false, msg)
    }
    
    [<Fact>]
    let ``Get not exits client returns 404`` () = task {
        let! response = fixture.Get "clients/invalid-id"
        
        test {
            let! _ = statusCode HttpStatusCode.NotFound response

            Assert.True(true)
        }
        |> function
        | TestResult.Success _ -> ()
        | TestResult.Failure msg -> Assert.True(false, msg)
    }
    
    [<Fact>]
    let ``Get exits client returns client and returns 200`` () = task {
        let clients = getTestClients 1
        let! _ = fixture.Post<_, Client[]> "clients/bulk" clients
        
        let client = clients |> Array.head
        let! response = fixture.Get $"clients/{client.client_id}"
        
        test {
            let! successResponse = isSuccess response
            let! _ = statusCode HttpStatusCode.OK response
            let! _ = where (fun c -> c = client) "Полученные объекты не совпадают" successResponse

            Assert.True(true)
        }
        |> function
        | TestResult.Success _ -> ()
        | TestResult.Failure msg -> Assert.True(false, msg)
    }
    
    [<Fact>]
    let ``Insert same client update them and returns 201`` () = task {
        let clients = getTestClients 10
        let! _ = fixture.Post<_, Client[]> "clients/bulk" clients
        
        let clients = clients |> Array.mapi (fun i c -> { c with login = $"{-i}" })
        let! response = fixture.Post<_, Client[]> "clients/bulk" clients
        
        test {
            let! successResponse = isSuccess response
            let! _ = statusCode HttpStatusCode.Created response
            let! _ = where (fun c -> c |> arraysEquivalent clients) "Полученные объекты не совпадают" successResponse

            Assert.True(true)
        }
        |> function
        | TestResult.Success _ -> ()
        | TestResult.Failure msg -> Assert.True(false, msg)
    }
    
    [<Theory>]
    [<InlineData("age_b")>]
    [<InlineData("age_t")>]
    [<InlineData("gender")>]
    let ``Insert invalid clients returns 400`` (dataType: string) = task {
        let clients = getTestClients 1
        let clients = clients |>
                      match dataType with
                      | s when s = "age_b" -> Array.map (fun c -> { c with age = -1 })
                      | s when s = "age_t" -> Array.map (fun c -> { c with age = 1000 })
                      | s when s = "gender" -> Array.map (fun c -> { c with gender = "TEST" })
                      | _ -> failwith "todo"
        let! response = fixture.Post<_, Client[]> "clients/bulk" clients
        
        test {
            let! _ = statusCode HttpStatusCode.BadRequest response

            Assert.True(true)
        }
        |> function
        | TestResult.Success _ -> ()
        | TestResult.Failure msg -> Assert.True(false, msg)
    }
        
    interface IAssemblyFixture<PromountTestContainers>