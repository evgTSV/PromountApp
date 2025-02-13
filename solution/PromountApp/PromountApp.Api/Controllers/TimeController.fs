namespace PromountApp.Api.Controllers

open Microsoft.AspNetCore.Mvc
open System
open PromountApp.Api
open PromountApp.Api.Models

[<ApiController>]
[<Route("time")>]
type TimeController(timeConfig: TimeConfig) =
    inherit ControllerBase()
    
    [<HttpGet("current_time")>]
    member this.GetCurrentTime() =
        try          
            OkObjectResult({|current_date = timeConfig.CurrentTime|}) :> IActionResult
        with
        | ex -> failwith $"Error: {ex.Message}" :> IActionResult
    
    [<HttpPost("advance")>]
    member this.Advance([<FromBody>] day: Day) =
        try
            let current_day = day.current_date
            let date = DateTime().Add(TimeSpan.FromDays(current_day))
            
            timeConfig.CurrentTime <- date
            
            OkObjectResult({|current_date = current_day|}) :> IActionResult
        with
        | ex -> failwith $"Error: {ex.Message}" :> IActionResult