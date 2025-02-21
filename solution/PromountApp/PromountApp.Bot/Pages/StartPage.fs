namespace PromountApp.Bot.Pages

open Funogram.Telegram.Bot
open PromountApp.Bot.Localization.Translator
open PromountApp.Bot.Pages.SelectLangPage
open PromountApp.Bot.State

type StartPage(inbox: BotMailbox) =
    interface IPage with
        member this.OnMassageHandler ctx command = async {
            match command with
            | Start ->
                match ctx.Update.Message with
                | Some { From = Some from } ->
                    let state = {
                        User = from
                        Lang = defaultLang
                    }
                    inbox.Post(Command.SelectLang, ctx)
                    return Some (SelectLangPage(state, inbox, fun s -> AuthPage(s, inbox, AuthPageState.Start)))
                | _ -> return None
            | _ ->
                return None
        }
            