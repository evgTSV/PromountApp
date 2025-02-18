namespace PromountApp.Api.Controllers

open System
open System.Buffers
open System.IO
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.HttpResults
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Mvc.Formatters
open Microsoft.Net.Http.Headers
open PromountApp.Api.Models
open PromountApp.Api.Services
open PromountApp.Api.Utils

[<ApiController>]
[<Route("advertisers")>]
type AdImageController(campaignsService: ICampaignsService, storage: IImageStorage) =
    inherit ControllerBase()
    
    [<HttpGet("{advertiserId:Guid}/campaigns/{campaignId:Guid}/img")>]
    member this.GetCampaignImg(advertiserId: Guid, campaignId: Guid) = task {
        try
            let! result = 
                campaignsService.GetCampaign advertiserId campaignId
                |> ServiceAsyncResult.bind (fun _ -> storage.GetImage campaignId)
            match result with
            | Success image ->
                return FileContentResult(image, "image/jpeg") :> IActionResult
            | NotFound ->
                return NotFoundResult() :> IActionResult
            | _ ->
                return failwith "Internal Error"
        with
        | :? NullReferenceException ->
            return BadRequestResult() :> IActionResult
    }      
        
    [<HttpPost("{advertiserId:Guid}/campaigns/{campaignId:Guid}/img")>]
    member this.InsertCampaignImg(advertiserId: Guid, campaignId: Guid, image: IFormFile) = task {
        try
            let! result = 
                campaignsService.GetCampaign advertiserId campaignId
                |> ServiceAsyncResult.bind (fun _ -> storage.PutImage campaignId image)
            match result with
            | Success _ ->
                return OkResult() :> IActionResult
            | NotFound ->
                return NotFoundResult() :> IActionResult
            | InvalidFormat msg ->
                return BadRequestObjectResult(msg) :> IActionResult
            | _ ->
                return failwith "Internal Error"
        with
        | :? NullReferenceException ->
            return BadRequestResult() :> IActionResult
    }
        
    [<HttpDelete("{advertiserId:Guid}/campaigns/{campaignId:Guid}/img")>]
    member this.DeleteCampaignImg(advertiserId: Guid, campaignId: Guid) = task {
        try
            let! result = 
                campaignsService.GetCampaign advertiserId campaignId
                |> ServiceAsyncResult.bind (fun _ -> storage.DeleteImage campaignId)
            match result with
            | Success _ ->
                return OkResult() :> IActionResult
            | NotFound ->
                return NotFoundResult() :> IActionResult
            | _ ->
                return failwith "Internal Error"
        with
        | :? NullReferenceException ->
            return BadRequestResult() :> IActionResult
    }