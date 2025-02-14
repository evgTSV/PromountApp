module PromountApp.Api.Utils

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Http
open FSharp.Data.Validator
type ServiceLocator() =
    static let mutable serviceProvider: IServiceProvider option = None

    static member SetProvider(provider: IServiceProvider) =
        serviceProvider <- Some provider

    static member GetService<'a>() =
        match serviceProvider with
        | Some provider -> provider.GetService(typeof<'a>) :?> 'a
        | None -> failwith "Service provider is not set."

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
    
let required : ValidatorFunc<'a> =
    fun (value: 'a) -> value |> box <> null
    
let validateOptionV (validator: ValidatorFunc<'a>) (value: 'a Nullable) =
    if value.HasValue then
        validator(value.Value)
    else true