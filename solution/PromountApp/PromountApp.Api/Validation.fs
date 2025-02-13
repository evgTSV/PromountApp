module PromountApp.Api.Validation

open System
open System.Collections.Generic
open FSharp.Data.Validator
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Mvc.Filters

type ValidateModelFilter() =
    let isEnumerable obj =
        typeof<IEnumerable<obj>>.IsAssignableFrom(obj.GetType())
    
    interface IActionFilter with
        member _.OnActionExecuting(context: ActionExecutingContext) =
            context.ActionArguments
            |> Seq.collect (fun kvp ->
                if isEnumerable kvp.Value then kvp.Value :?> IEnumerable<obj> else [| kvp.Value |])
            |> Seq.filter (fun data -> typeof<IValidatable>.IsAssignableFrom(data.GetType()))
            |> Seq.iter (fun data ->
                let model = data :?> IValidatable
                if not (model.Validate()) then
                    context.Result <- BadRequestObjectResult("Invalid model") :> IActionResult
            )

        member _.OnActionExecuted(context: ActionExecutedContext) = ()
