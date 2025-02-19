namespace PromountApp.Bot.Pages

open Funogram.Telegram.Bot
open PromountApp.Bot.Keyboards
open PromountApp.Bot.State
open PromountApp.Bot.Utils

type AuthPage(state: UserState, inbox: BotMailbox) =
    interface IPage with
        member this.OnMassageHandler ctx command = async {
            match ctx.Update.CallbackQuery with
            | Some { Data = Some callback } ->
                match callback with
                | s when s = signInOutput ->
                    sendMessage state.User.Id (sprintf (Printf.StringFormat<_>(state.Lang.LoginButtonText)) state.User.FirstName) ctx
                    return this
                | s when s = signUpOutput ->
                    sendMessage state.User.Id (sprintf (Printf.StringFormat<_>(state.Lang.RegisterButtonText)) state.User.FirstName) ctx
                    return this
                | _ ->
                    let keyboard = authKeyboard state.Lang
                    sendMessageMarkup state.User.Id (sprintf (Printf.StringFormat<_>(state.Lang.WelcomeText)) state.User.FirstName) keyboard ctx
                    return this
            | _ ->
                let keyboard = authKeyboard state.Lang
                sendMessageMarkup state.User.Id (sprintf (Printf.StringFormat<_>(state.Lang.WelcomeText)) state.User.FirstName) keyboard ctx
                return this
        }    