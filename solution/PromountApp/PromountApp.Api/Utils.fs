module PromountApp.Api.Utils

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http

type middleware = Func<HttpContext, Func<Task>, Task>

type ServiceResponse<'a> =
    | Conflict
    | NotFounded
    | Success of 'a

let getEnv name =
    match Environment.GetEnvironmentVariable(name) with
    | null -> ArgumentException($"Env {name} not founded") |> raise
    | value -> value
    
let getEnvOr name defaultValue =
    match Environment.GetEnvironmentVariable(name) with
    | null -> defaultValue
    | value -> value
    
let exceptionHandler =
    middleware(fun ctx next -> task {
        try
            do! next.Invoke() |> Async.AwaitTask
            return ()
        with
        | _ -> return ()
    })
    
let filterAggregate (handler: Exception -> 'a) (agg: AggregateException) (handleOnly: Exception[]) =
    let exTypes = handleOnly |> Array.map _.GetType()
    let res =
        agg.InnerExceptions |> Seq.tryFind(fun ex ->
        exTypes |> Array.contains(ex.GetType()))
    match res with
    | Some ex -> handler ex
    | None -> raise agg