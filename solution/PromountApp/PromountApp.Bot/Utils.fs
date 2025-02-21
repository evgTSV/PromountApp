module PromountApp.Bot.Utils

open System
open System.IO
open System.Net
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading.Tasks
open Funogram
open FSharp.Data.Validator
open Funogram.Api
open Funogram.Telegram
open Funogram.Telegram.Bot
open Funogram.Telegram.Types

type ConsoleLogger(color: ConsoleColor) =
  interface Funogram.Types.IBotLogger with
    member x.Log(text) =
      let fc = Console.ForegroundColor
      Console.ForegroundColor <- color
      Console.WriteLine(text)
      Console.ForegroundColor <- fc
    member x.Enabled = true
    
    
type ControllerResponse<'a> =
    | Error of HttpStatusCode
    | Success of 'a
    
module ServiceAsyncResult =
    let bind f xAsync =
        task {
            let! x = xAsync
            match x with
            | Success value -> return! f value
            | Error code -> return Error code
        }

    let map f xAsync =
        task {
            let! x = xAsync
            match x with
            | Success value -> return Success (f value)
            | Error code -> return Error code
        }
        
    let return' x =
        task {
            return Success x
        }
    
let validateOptionV (validator: ValidatorFunc<'a>) (value: 'a Nullable) =
    if value.HasValue then
        validator(value.Value)
    else true

let to2DArray (cols: int) (arr: 'a[]) =
    let rows = (Array.length arr + cols - 1) / cols
    Array.init rows (fun i ->
        Array.init cols (fun j ->
            let index = i * cols + j
            if index < Array.length arr then
                Some arr.[index]
            else
                None
        )
        |> Array.choose id
    )
    
let processResultWithValue (result: Async<Result<'a, Types.ApiResponseError>>) = async {
    let! result = result
    match result with
    | Ok _ -> ()
    | Result.Error e -> printfn $"Internal error: %s{e.Description}"
    
    return result
}

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

let parseMode = ParseMode.HTML

let processResult result = processResultWithValue result
let botResult config data = api config data
let bot config data = botResult config data |> processResult
let botIgnored config data = bot config data |> Async.Ignore |> Async.Start
let sendMessageFormatted chatId text parseMode ctx  =
  Req.SendMessage.Make(ChatId.Int chatId, text, parseMode = parseMode) |> bot ctx.Config |> Async.Ignore |> Async.Start
let sendMessageFormattedMarkup chatId text markup parseMode ctx  =
  Req.SendMessage.Make(ChatId.Int chatId, text, parseMode = parseMode, replyMarkup = markup) |> bot ctx.Config |> Async.Ignore |> Async.Start
let sendMessage chatId text ctx = sendMessageFormatted chatId text parseMode ctx
let sendMessageMarkup chatId text markup ctx = sendMessageFormattedMarkup chatId text markup parseMode ctx