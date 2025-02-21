module PromountApp.Api.Tests.Helpers

open System
open System.IO
open System.Net
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading.Tasks
open FluentResults
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Mvc.Testing
open PromountApp.Api
open PromountApp.Api.Services
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.DependencyInjection

let isSuccessStatusCode (statusCode: HttpStatusCode) =
    match statusCode with
    | HttpStatusCode.OK
    | HttpStatusCode.Created
    | HttpStatusCode.Accepted
    | HttpStatusCode.NoContent -> true
    | _ -> false

type Response<'a> =
    | Error of HttpStatusCode
    | Success of 'a * HttpStatusCode
    with
        member this.GetData() =
            match this with
            | Success (data, _) -> data
            | Error code -> failwith $"Response includes error: {code.ToString()}"
    
module TestMonad =
    [<RequireQualifiedAccess>]
    type TestResult<'a> =
        | Success of 'a
        | Failure of string

    type TestBuilder() =
        member _.Bind(x, f) =
            match x with
            | TestResult.Success v -> f v
            | TestResult.Failure msg -> TestResult.Failure msg

        member _.Return(v) =
            Success v

        member _.ReturnFrom(x) =
            x

        member _.Zero() =
            TestResult.Success ()

    let test = TestBuilder()
 
    let isSuccess (response: Response<'a>) =
        match response with
        | Success (value, statusCode) when isSuccessStatusCode statusCode ->
            TestResult.Success (value, statusCode)
        | Error statusCode ->
            TestResult.Failure $"Response error with status code {statusCode}"
        | _ ->
            TestResult.Failure "Response was not successful"

    let statusCode expectedStatus (response: Response<'a>) =
        match response with
        | Success (_, statusCode) when statusCode = expectedStatus ->
            TestResult.Success (response, statusCode)
        | Error statusCode when statusCode = expectedStatus ->
            TestResult.Success (response, statusCode)
        | _ ->
            TestResult.Failure $"Expected status code {expectedStatus}, but got different status"

    let where predicate message (value, statusCode) =
        if predicate value then
            TestResult.Success (value, statusCode)
        else
            TestResult.Failure message
            
let serializeJson<'a> (obj: 'a) =
    let options = JsonSerializerOptions()
    options.PropertyNameCaseInsensitive <- true
    options.Encoder <- System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    options.PropertyNamingPolicy <- JsonNamingPolicy.SnakeCaseLower
    options.DefaultIgnoreCondition <- JsonIgnoreCondition.WhenWritingNull
    options.WriteIndented <- true

    JsonSerializer.Serialize<'a>(obj, options)

let deserializeJson<'a> (stream: Stream) : Task<Option<'a>> =
    task {
        let options = JsonSerializerOptions()
        options.PropertyNameCaseInsensitive <- true
        options.Encoder <- System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        options.PropertyNamingPolicy <- JsonNamingPolicy.SnakeCaseLower
        
        try
            return Some(JsonSerializer.DeserializeAsync<'a>(stream, options).Result)
        with
        | _ -> return None
    }
    
let arraysEquivalent (arr1: 'a array) (arr2: 'a array) =
    let countElements (arr: 'a array) =
        arr |> Array.fold (fun acc elem ->
            match Map.tryFind elem acc with
            | Some count -> Map.add elem (count + 1) acc
            | None -> Map.add elem 1 acc
        ) Map.empty
    let counts1 = countElements arr1
    let counts2 = countElements arr2
    counts1 = counts2
    
let arraysEquivalent' (comparator: 'a -> 'a -> bool) (arr1: 'a array) (arr2: 'a array) =
    let countElements (arr: 'a array) =
        arr
        |> Array.fold (fun acc elem ->
            let count = 
                acc 
                |> List.tryFind (fun (key, _) -> comparator key elem) 
                |> function
                    | Some (_, count) -> count + 1
                    | None -> 1
            (elem, count) :: acc |> List.distinctBy fst
        ) []
    
    let counts1 = countElements arr1
    let counts2 = countElements arr2
    counts1 = counts2