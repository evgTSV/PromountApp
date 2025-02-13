namespace PromountApp.Api.Controllers

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open PromountApp.Api.Models
open PromountApp.Api.Services
open PromountApp.Api.Utils

[<ApiController>]
[<Route("clients")>]
type ClientsController(clientsService: IClientsService) =
    inherit ControllerBase()
    
    [<HttpGet("{id:Guid}")>]
    member this.Get(id: Guid) = task {
        match! clientsService.GetClient(id) with
        | Success client -> return OkObjectResult(client) :> IActionResult
        | NotFounded -> return NotFoundResult() :> IActionResult
        | _ -> return failwith "Internal Error"
    }
        
    [<HttpPost("bulk")>]
    member this.BulkCopy([<FromBody>] clients: Client seq) = task {
        match! clientsService.BulkInsertion(clients) with
        | Success _ -> return CreatedResult() :> IActionResult
        | Conflict -> return ConflictResult() :> IActionResult
        | _ -> return failwith "Internal Error"
    }