module PromountApp.Api.Utils

open System
open System.IO
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
    | NotFound
    | InvalidFormat of string
    | Success of 'a
    
module ServiceAsyncResult =
    let bind f xAsync =
        async {
            try
                let! x = xAsync
                match x with
                | Success value -> return! f value
                | NotFound -> return NotFound
                | Conflict -> return Conflict
            with
            | :? NullReferenceException -> return NotFound
        }

    let map f xAsync =
        async {
            try
                let! x = xAsync
                match x with
                | Success value -> return Success (f value)
                | NotFound -> return NotFound
                | Conflict -> return Conflict
            with
            | :? NullReferenceException -> return NotFound
        }
        
    let wrapAsync f xAsync =
        async {
            try
                let! x = xAsync
                return Success (f x)
            with
            | :? NullReferenceException -> return NotFound
        }

    let return' x =
        async {
            return Success x
        }

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
    
    
let inline (|++|) (tuple1: ^T * ^U) (tuple2: ^T * ^U) : ^T * ^U
    when ^T : (static member (+) : ^T * ^T -> ^T)
    and  ^U : (static member (+) : ^U * ^U -> ^U) =
    let a = fst tuple1 + fst tuple2
    let b = snd tuple1 + snd tuple2
    (a, b)