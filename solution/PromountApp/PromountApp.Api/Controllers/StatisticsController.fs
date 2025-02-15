namespace PromountApp.Api.Controllers

open System
open Microsoft.AspNetCore.Http.HttpResults
open Microsoft.AspNetCore.Mvc
open PromountApp.Api.Models
open PromountApp.Api.Services
open PromountApp.Api.Utils

[<ApiController>]
[<Route("stats")>]
type StatisticsController(statisticsService: IStatisticsService) =
    inherit ControllerBase()
    
    let getStats (getFunc: IStatisticsService -> Async<ServiceResponse<'a>>) = task {
        match! getFunc statisticsService with
        | Success stats ->
            return OkObjectResult(stats) :> IActionResult
        | NotFound ->
            return NotFoundResult() :> IActionResult
        | _ ->
            return failwith "Internal Error"
    }
    
    [<HttpGet("campaigns/{campaignId:Guid}")>]
    member this.GetCampaignStats(campaignId: Guid) =
        getStats _.GetCampaignStats(campaignId)
    
    [<HttpGet("campaigns/{campaignId:Guid}/daily")>]
    member this.GetCampaignStatsDaily(campaignId: Guid) =
        getStats _.GetCampaignStatsDaily(campaignId)
    
    [<HttpGet("advertisers/{advertiserId:Guid}/campaigns")>]
    member this.GetAdvertiserStats(advertiserId: Guid) =
        getStats _.GetAdvertiserStats(advertiserId)
    
    [<HttpGet("advertisers/{advertiserId:Guid}/campaigns/daily")>]
    member this.GetAdvertiserStatsDaily(advertiserId: Guid) =
        getStats _.GetAdvertiserStatsDaily(advertiserId)