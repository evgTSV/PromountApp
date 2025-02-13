module PromountApp.Api.Validation

open System
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Mvc.Filters

type ValidateModelFilter() =
    interface IActionFilter with
        member _.OnActionExecuting(context: ActionExecutingContext) =
            if not context.ModelState.IsValid then
                context.Result <- BadRequestObjectResult(context.ModelState) :> IActionResult

        member _.OnActionExecuted(context: ActionExecutedContext) = ()