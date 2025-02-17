namespace PromountApp.Api.Controllers

open System
open Microsoft.AspNetCore.Http.HttpResults
open Microsoft.AspNetCore.Mvc
open PromountApp.Api.Models
open PromountApp.Api.Services
open PromountApp.Api.Utils

[<ApiController>]
[<Route("advertisers")>]
type CampaignsController(campaignsService: ICampaignsService) =
    inherit ControllerBase()
    
    [<HttpPost("{advertiserId:Guid}/campaigns")>]
    member this.CreateCampaign(advertiserId: Guid,
                               [<FromBody>] campaign: Campaign) = task {
        let campaign = { campaign with advertiser_id = advertiserId }
        match! campaignsService.CreateCampaign(campaign) with
        | Success campaign ->
            return OkObjectResult(campaign, StatusCode = 201) :> IActionResult
        | NotFound ->
            return NotFoundResult() :> IActionResult
        | _ ->
            return failwith "Internal Error"
    }
        
    [<HttpGet("{advertiserId:Guid}/campaigns")>]
    member this.GetCampaigns(advertiserId: Guid,
                             [<FromQuery>] size: int Nullable,
                             [<FromQuery>] page: int Nullable) = task {
        let size = defaultIfNullV Int32.MaxValue size
        let page = defaultIfNullV 0 page
        match! campaignsService.GetAllCampaigns(advertiserId) (size, page) with
        | Success (campaigns, count) ->
            this.Response.Headers["X-Total-Count"] <- count.ToString()
            return OkObjectResult(campaigns) :> IActionResult
        | NotFound ->
            return NotFoundResult() :> IActionResult
        | _ ->
            return failwith "Internal Error"
    }       
        
    [<HttpGet("{advertiserId:Guid}/campaigns/{campaignId:Guid}")>]
    member this.GetCampaign(advertiserId: Guid,
                            campaignId: Guid) = task {
        match! campaignsService.GetCampaign advertiserId campaignId with
        | Success campaign ->
            return OkObjectResult(campaign) :> IActionResult
        | NotFound ->
            return NotFoundResult() :> IActionResult
        | _ ->
            return failwith "Internal Error"
    }      
        
    [<HttpPut("{advertiserId:Guid}/campaigns/{campaignId:Guid}")>]
    member this.UpdateCampaign(advertiserId: Guid, campaignId: Guid,
                               [<FromBody>] campaign: CampaignUpdate) = task {
        let campaign = { campaign with campaign_id = campaignId }
        match! campaignsService.UpdateCampaign advertiserId campaign with
        | Success campaign ->
            return OkObjectResult(campaign) :> IActionResult
        | NotFound ->
            return NotFoundResult() :> IActionResult
        | Conflict ->
            return BadRequestResult() :> IActionResult
        | _ ->
            return failwith "Internal Error"
    }
        
    [<HttpDelete("{advertiserId:Guid}/campaigns/{campaignId:Guid}")>]
    member this.DeleteCampaign(advertiserId: Guid, campaignId: Guid) = task {
        match! campaignsService.DeleteCampaign advertiserId campaignId with
        | Success _ ->
            return NoContentResult() :> IActionResult
        | NotFound ->
            return NotFoundResult() :> IActionResult
        | _ ->
            return failwith "Internal Error"
    }
    
    [<HttpGet("{advertiserId:Guid}/campaigns/{campaignId:Guid}/gen-text")>]
    member this.GenTextForCampaign(advertiserId: Guid, campaignId: Guid) = task {
        match! campaignsService.GenTextForCampaign advertiserId campaignId with
        | Success text ->
            return OkObjectResult({| ad_text = text |}) :> IActionResult
        | NotFound ->
            return NotFoundResult() :> IActionResult
        | _ ->
            return failwith "Internal Error"
    }