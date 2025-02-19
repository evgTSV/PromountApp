module PromountApp.Bot.Pages.SelectLangPage

open Funogram.Telegram.Bot
open PromountApp.Bot.Keyboards
open PromountApp.Bot.Localization.Translator
open PromountApp.Bot.State
open PromountApp.Bot.Utils

type SelectLangPage(state: UserState, inbox: BotMailbox, next: UserState -> IPage) =
    interface IPage with
        member this.OnMassageHandler ctx command = async {
                match ctx.Update.CallbackQuery with
                | Some { Data = Some callback } when callback.StartsWith(setLangOutput) ->
                    let lang = callback.Substring(setLangOutput.Length)
                    let newLang = availableLanguages() |> Seq.find (fun l -> l.LangName = lang)
                    sendMessage state.User.Id (sprintf (Printf.StringFormat<_>(newLang.ChangedLanguageText)) newLang.LangName) ctx
                    inbox.Post(Command.Start, ctx)
                    let newState = { state with Lang = newLang }
                    return next(newState)
                | _ ->
                    match command with
                    | SelectLang ->
                        sendMessageMarkup state.User.Id state.Lang.SelectLanguageText selectLangKeyboard ctx
                        return this 
                    | _ ->
                        return this
            }