module TestAdMatchEngine

open System
open Microsoft.FSharp.Linq
open PromountApp.Api.Models
open PromountApp.Api.Services
open Microsoft.Extensions.DependencyInjection
open PromountApp.Api.Tests.Helpers
open PromountApp.Api.Utils
open Xunit

let createCampaign() = {
    campaign_id = Guid.NewGuid()
    advertiser_id = Guid.NewGuid()
    impressions_limit = 2
    clicks_limit = 1
    cost_per_impression = 1
    cost_per_click = 1
    ad_title = "test"
    ad_text = "test"
    start_date = 0
    end_date = 10
    gender = null
    age_from = Nullable()
    age_to = Nullable()
    location = null
}

let createCampaignWithTargeting (targeting: Targeting) = {
    createCampaign() with
        gender = targeting.gender
        age_from = targeting.age_from
        age_to = targeting.age_from
        location = targeting.location
}

[<Fact>]
let ``Common ad returns (exists only one campaign)`` () =
    let campaign = createCampaign()
    