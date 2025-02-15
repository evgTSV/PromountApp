namespace PromountApp.Api.Controllers

open System
open Microsoft.AspNetCore.Http.HttpResults
open Microsoft.AspNetCore.Mvc
open PromountApp.Api.Models
open PromountApp.Api.Services
open PromountApp.Api.Utils

[<ApiController>]
[<Route("ads")>]
type AdsController(adsService: IAdsService) =
    inherit ControllerBase()
    
    [<HttpGet>]
    member this.GetAds([<FromQuery>] client_id: Guid) = task {
        match! adsService.GetBestMatchAd client_id with
        | Success ad ->
            return OkObjectResult(ad) :> IActionResult
        | NotFound ->
            return NotFoundResult() :> IActionResult
        | _ ->
            return failwith "Internal Error"
    }
    
    [<HttpPost("{adId:Guid}/click")>]
    member this.Click(adId: Guid, [<FromBody>] click: Click) = task {
        match! adsService.ClickOnAd adId click.client_id with
        | Success _ ->
            return NoContentResult() :> IActionResult
        | NotFound ->
            return NotFoundResult() :> IActionResult
        | _ ->
            return failwith "Internal Error"
    }