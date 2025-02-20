namespace PromountApp.Api.Controllers

open System
open Microsoft.AspNetCore.Http.HttpResults
open Microsoft.AspNetCore.Mvc
open PromountApp.Api.Models
open PromountApp.Api.Services
open PromountApp.Api.Utils

[<ApiController>]
[<Route("ads")>]
type BanListController(banListService: IBanListService) =
    inherit ControllerBase()
    
    [<HttpGet("ban-list")>]
    member this.GetBanList() = task {
        match! banListService.GetBanList() with
        | Success l ->
            return OkObjectResult(l) :> IActionResult
        | NotFound ->
            return NotFoundResult() :> IActionResult
        | _ ->
            return failwith "Internal Error"
    }
    
    [<HttpDelete("{campaignId:Guid}/ban")>]
    member this.Unban(campaignId: Guid) = task {
        match! banListService.Unban campaignId with
        | Success _ ->
            return NoContentResult() :> IActionResult
        | NotFound ->
            return NotFoundResult() :> IActionResult
        | _ ->
            return failwith "Internal Error"
    }