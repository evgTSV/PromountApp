module PromountApp.Bot.Utils

open System
open Funogram
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
    | Error e -> printfn $"Internal error: %s{e.Description}"
    
    return result
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