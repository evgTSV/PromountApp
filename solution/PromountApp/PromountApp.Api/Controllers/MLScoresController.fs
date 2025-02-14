namespace PromountApp.Api.Controllers

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open PromountApp.Api.Models
open PromountApp.Api.Services
open PromountApp.Api.Utils

[<ApiController>]
[<Route("ml-scores")>]
type MLScoresController(advertisersService: IAdvertisersService) =
    inherit ControllerBase()
    
    [<HttpPost>]
    member this.SetMLScore([<FromBody>] mlScore: MLScore) = task {
        match! advertisersService.SetMLScore(mlScore) with
        | Success _ -> return OkResult() :> IActionResult
        | _ -> return failwith "Internal Error"
    }