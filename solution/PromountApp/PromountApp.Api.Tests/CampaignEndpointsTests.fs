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

type CampaignEndpointsTests(fixture: PromountTestContainers) =
    do
        fixture.TimeZero() |> Async.AwaitTask |> Async.Ignore |> Async.RunSynchronously
    
    let getTestAdvertiser() = {
        advertiser_id = Guid.NewGuid()
        name = $"login {Random.Shared.Next(0, 1000)}"
    }
    
    let insertAdvertiser () = task {
        let advertiser = getTestAdvertiser()
        let! _ = fixture.Post<_, Advertiser[]> "advertisers/bulk" [| advertiser |]
        return advertiser
    }
    
    let insertCampaigns url (campaigns: Campaign[]) =
        campaigns |> Array.iter (fun c ->
            (fixture.Post<_, Campaign> url c) |> Async.AwaitTask |> Async.Ignore |> Async.RunSynchronously)
        
    let campaignComparasion (campaign1: Campaign) (campaign2: Campaign) =
        campaign1 = campaign2
     
    let getTestCampaign (day: int) : Campaign = {
        campaign_id = Guid.Empty
        advertiser_id = Guid.Empty
        impressions_limit = 100
        clicks_limit = 10
        cost_per_impression = 1
        cost_per_click = 1
        ad_title = "title"
        ad_text = "title"
        start_date = day
        end_date = day + 1
        targeting = {
            age_from = Nullable()
            age_to = Nullable()
            gender = null
            location = null
        }
    }
        
    let prefixUrlForCampaignList = "advertisers/%s/campaigns"
    let prefixUrlForCampaign = "advertisers/%s/campaigns/%s"

    [<Fact>]
    let ``Advertiser can create campaign and returns acreated campaign and 201`` () = task {
        let! day = fixture.GetTime()
        let! advertiser = insertAdvertiser()
        let campaign = getTestCampaign(day)
        let url = sprintf (Printf.StringFormat<string -> string>(prefixUrlForCampaignList)) (advertiser.advertiser_id.ToString())
        let! response = fixture.Post<_, Campaign> url campaign
        
        test {
            let! successResponse = isSuccess response
            let! _ = statusCode HttpStatusCode.Created response
            let! _ = where (fun (actual: Campaign) ->
                let expected = { campaign with campaign_id = actual.campaign_id; advertiser_id = advertiser.advertiser_id }
                expected = actual) "Полученные кампании не совпадают" successResponse

            Assert.True(true)
        }
        |> function
        | TestResult.Success _ -> ()
        | TestResult.Failure msg -> Assert.True(false, msg)
    }
    
    [<Theory>]
    [<InlineData("clicks_limit > impressions_limit")>]
    [<InlineData("impressions_limit <= 0")>]
    [<InlineData("clicks_limit <= 0")>]
    [<InlineData("cost_per_impression <= 0")>]
    [<InlineData("cost_per_click <= 0")>]
    [<InlineData("start_date < curr_day")>]
    [<InlineData("end_date < start_date")>]
    [<InlineData("age_to < age_from")>]
    [<InlineData("age_from < 0")>]
    [<InlineData("age_from > 100")>]
    [<InlineData("gender")>]
    let ``Advertiser can't create invalid campaign and returns 400`` (dataType: string) = task {
        let! day = fixture.GetTime()
        let! advertiser = insertAdvertiser()
        let campaign =
            match dataType with
            | s when s = "clicks_limit > impressions_limit" -> { getTestCampaign day with clicks_limit = 100; impressions_limit = 10 }
            | "impressions_limit <= 0" -> { getTestCampaign day with impressions_limit = 0 }
            | "clicks_limit <= 0" -> { getTestCampaign day with clicks_limit = 0 }
            | "cost_per_impression <= 0" -> { getTestCampaign day with cost_per_impression = 0 }
            | "cost_per_click <= 0" -> { getTestCampaign day with cost_per_click = 0 }
            | "start_date < curr_day" -> { getTestCampaign day with start_date = day - 1 }
            | "end_date < start_date" -> { getTestCampaign day with end_date = day - 2 }
            | "age_to < age_from" -> { getTestCampaign day with targeting = { (getTestCampaign day).targeting with age_from = Nullable 10; age_to = Nullable 5 } }
            | "age_from < 0" -> { getTestCampaign day with targeting = { (getTestCampaign day).targeting with age_from = Nullable -1 } }
            | "age_from > 100" -> { getTestCampaign day with targeting = { (getTestCampaign day).targeting with age_from = Nullable 101 } }
            | "gender" -> { getTestCampaign day with targeting = { (getTestCampaign day).targeting with gender = "InvalidGender" } }
            | _ -> getTestCampaign day
        let url = sprintf (Printf.StringFormat<string -> string>(prefixUrlForCampaignList)) (advertiser.advertiser_id.ToString())
        let! response = fixture.Post<_, Campaign> url campaign
        
        test {
            let! _ = statusCode HttpStatusCode.BadRequest response
            
            Assert.True(true)
        }
        |> function
        | TestResult.Success _ -> ()
        | TestResult.Failure msg -> Assert.True(false, msg)
    }
    
    [<Fact>]
    let ``Advertiser can get campaign list and returns 200`` () = task {
        let! day = fixture.GetTime()
        let! advertiser = insertAdvertiser()
        let campaigns = Array.create 10 (getTestCampaign day)
        let url = sprintf (Printf.StringFormat<string -> string>(prefixUrlForCampaignList)) (advertiser.advertiser_id.ToString())
        do campaigns |> insertCampaigns url
        let! response = fixture.Get<Campaign[]> url
        
        test {
            let! successResponse = isSuccess response
            let! _ = statusCode HttpStatusCode.OK response
            let! _ = where (fun (actual: Campaign[]) ->
                let expected = (campaigns, actual)
                               ||> Array.map2 (fun c a -> { c with campaign_id = a.campaign_id; advertiser_id = advertiser.advertiser_id })
                (actual, expected) ||> arraysEquivalent' campaignComparasion) "Полученные кампании не совпадают" successResponse

            Assert.True(true)
        }
        |> function
        | TestResult.Success _ -> ()
        | TestResult.Failure msg -> Assert.True(false, msg)
    }
    
    [<Fact>]
    let ``Advertiser can get created campaign and returns campaign and 200`` () = task {
        let! day = fixture.GetTime()
        let! advertiser = insertAdvertiser()
        let campaign = getTestCampaign(day)
        let url = sprintf (Printf.StringFormat<string -> string>(prefixUrlForCampaignList)) (advertiser.advertiser_id.ToString())
        let! campaign = fixture.Post<_, Campaign> url campaign
        let campaign = campaign.GetData()
        let url = sprintf (Printf.StringFormat<string -> string -> string>(prefixUrlForCampaign))
                      (advertiser.advertiser_id.ToString())
                      (campaign.campaign_id.ToString())
        let! getCampaignResponse =  fixture.Get<Campaign> url
        
        test {
            let! successResponse = isSuccess getCampaignResponse
            let! _ = statusCode HttpStatusCode.OK getCampaignResponse
            let! _ = where (fun (actual: Campaign) -> actual = campaign) "Полученные кампании не совпадают" successResponse

            Assert.True(true)
        }
        |> function
        | TestResult.Success _ -> ()
        | TestResult.Failure msg -> Assert.True(false, msg)
    }
        
    interface IAssemblyFixture<PromountTestContainers>