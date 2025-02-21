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

type AdvertisersServiceTests(fixture: PromountTestContainers) =
     
    let getTestAdvertisers (count: int) : Advertiser[] =
        [|
            for i in 1..count -> {
                advertiser_id = Guid.NewGuid()
                name = $"login {i}"
            }
        |]
        
    let prefixUrl = "advertisers"

    [<Fact>]
    let ``Bulk insertion return inserted advertisers and 201`` () = task {
        let advertisers = getTestAdvertisers 10
        let! response = fixture.Post<_, Advertiser[]> $"{prefixUrl}/bulk" advertisers
        
        test {
            let! successResponse = isSuccess response
            let! _ = statusCode HttpStatusCode.Created response
            let! _ = where (fun c -> c |> arraysEquivalent advertisers) "Полученные объекты не совпадают" successResponse

            Assert.True(true)
        }
        |> function
        | TestResult.Success _ -> ()
        | TestResult.Failure msg -> Assert.True(false, msg)
    }
    
    [<Fact>]
    let ``Get not exits advertiser returns 404`` () = task {
        let! response = fixture.Get $"{prefixUrl}/invalid-id"
        
        test {
            let! _ = statusCode HttpStatusCode.NotFound response

            Assert.True(true)
        }
        |> function
        | TestResult.Success _ -> ()
        | TestResult.Failure msg -> Assert.True(false, msg)
    }
    
    [<Fact>]
    let ``Get exits client returns advertiser and returns 200`` () = task {
        let advertisers = getTestAdvertisers 1
        let! _ = fixture.Post<_, Advertiser[]> $"{prefixUrl}/bulk" advertisers
        
        let advertiser = advertisers |> Array.head
        let! response = fixture.Get $"{prefixUrl}/{advertiser.advertiser_id}"
        
        test {
            let! successResponse = isSuccess response
            let! _ = statusCode HttpStatusCode.OK response
            let! _ = where (fun c -> c = advertiser) "Полученные объекты не совпадают" successResponse

            Assert.True(true)
        }
        |> function
        | TestResult.Success _ -> ()
        | TestResult.Failure msg -> Assert.True(false, msg)
    }
    
    [<Fact>]
    let ``Insert same advertiser update them and returns 201`` () = task {
        let advertisers = getTestAdvertisers 10
        let! _ = fixture.Post<_, Advertiser[]> $"{prefixUrl}/bulk" advertisers
        
        let advertisers = advertisers |> Array.mapi (fun i c -> { c with name = $"{-i}" })
        let! response = fixture.Post<_, Advertiser[]> $"{prefixUrl}/bulk" advertisers
        
        test {
            let! successResponse = isSuccess response
            let! _ = statusCode HttpStatusCode.Created response
            let! _ = where (fun c -> c |> arraysEquivalent advertisers) "Полученные объекты не совпадают" successResponse

            Assert.True(true)
        }
        |> function
        | TestResult.Success _ -> ()
        | TestResult.Failure msg -> Assert.True(false, msg)
    }
        
    interface IAssemblyFixture<PromountTestContainers>