open System
open System.Collections.Concurrent
open Funogram
open Funogram.Api
open Funogram.Telegram
open Funogram.Telegram.Bot
open Funogram.Telegram.Types
open Microsoft.FSharp.Control
open PromountApp.Bot.Keyboards
open PromountApp.Bot.Localization.Translator
open PromountApp.Bot.Pages
open PromountApp.Bot.State
open PromountApp.Bot.Utils

#nowarn 26

let states = ConcurrentDictionary<int64, BotMailbox>()

let advertisersState() = MailboxProcessor.Start(fun inbox ->
        let rec loop (page: IPage option, pred: IPage list) = async {
            let! command, ctx = inbox.Receive()
            match command with
            | Back ->
                return! loop(pred |> List.tryHead, match pred with | [] -> [] | _ -> pred |> List.tail)
            | _ ->
                let! newState = 
                    async {
                        match page with
                        | Some page ->
                            return! page.OnMassageHandler ctx command
                        | None ->
                            inbox.Post(Command.Start, ctx)
                            return Some (StartPage(inbox))
                    }

                if newState.IsNone then 
                    return! loop (page, pred)
                else
                    return! loop (newState,
                                 page |> Option.map (fun p -> p :: pred) |> Option.defaultValue pred)
        }
        
        loop (None, [])
    )

let onMessage (ctx: UpdateContext) =
    let state =
        match ctx.Update.Message with
        | Some { From = Some from } ->
            states.GetOrAdd(from.Id, advertisersState())
        | _ ->
            match ctx.Update.CallbackQuery with
            | Some { From = from } ->
                states.GetOrAdd(from.Id, advertisersState())
            | _ -> failwith "invalid user"
    let result =
        processCommands ctx [|
          cmd "/start" (fun _ -> state.Post(Command.Start, ctx))
        |]
    if result then
        state.Post(Command.NewMessage, ctx)

let configureBot() = async {
    let config = Config.defaultConfig |> Config.withReadTokenFromEnv "TG_BOT_TOKEN"
    let config = { config with RequestLogger = Some(ConsoleLogger(ConsoleColor.Green))}
    let! _ = Api.deleteWebhookBase () |> api config
    return! startBot config onMessage None
}
    
[<EntryPoint>]
let main _ =
    configureBot() |> Async.RunSynchronously
    0