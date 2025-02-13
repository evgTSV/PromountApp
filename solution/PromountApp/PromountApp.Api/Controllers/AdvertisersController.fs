namespace PromountApp.Api.Controllers

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open PromountApp.Api.Models
open PromountApp.Api.Services
open PromountApp.Api.Utils

[<ApiController>]
[<Route("advertisers")>]
type AdvertisersController(advertisersService: IAdvertisersService) =
    inherit ControllerBase()
    
    [<HttpGet("{id:Guid}")>]
    member this.Get(id: Guid) = task {
        match! advertisersService.GetAdvertiser(id) with
        | Success advertiser -> return OkObjectResult(advertiser) :> IActionResult
        | NotFounded -> return NotFoundResult() :> IActionResult
        | _ -> return failwith "Internal Error"
    }
        
    [<HttpPost("bulk")>]
    member this.BulkCopy([<FromBody>] advertisers: Advertiser seq) = task {
        match! advertisersService.BulkInsertion(advertisers) with
        | Success _ -> return CreatedResult() :> IActionResult
        | Conflict -> return ConflictResult() :> IActionResult
        | _ -> return failwith "Internal Error"
    }
    
    