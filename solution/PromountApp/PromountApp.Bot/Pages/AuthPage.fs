namespace PromountApp.Bot.Pages

open System
open Microsoft.FSharp.Control
open PromountApp.Api.Bot.Models
open Funogram.Telegram.Bot
open PromountApp.Bot.Controllers
open PromountApp.Bot.Keyboards
open PromountApp.Bot.State
open PromountApp.Bot.Utils

type AuthPageState =
    | Start
    | AwaitUser
    | SignIn
    | SignUp

type AuthPage(userState: UserState, inbox: BotMailbox, state: AuthPageState) =
    
    interface IPage with
        member this.OnMassageHandler ctx command = async {
            match state with
            | Start ->
                let keyboard = authKeyboard userState.Lang
                sendMessageMarkup userState.User.Id (sprintf (Printf.StringFormat<_>(userState.Lang.WelcomeText)) userState.User.FirstName) keyboard ctx
                return AuthPage(userState, inbox, AuthPageState.AwaitUser)
            | AwaitUser ->
                match ctx.Update.CallbackQuery with
                | Some { Data = Some callback } ->
                    match callback with
                    | s when s = signInOutput ->
                        sendMessage userState.User.Id userState.Lang.LoginInputTipText ctx
                        return AuthPage(userState, inbox, AuthPageState.SignIn)
                    | s when s = signUpOutput ->
                        sendMessage userState.User.Id userState.Lang.RegisterInputTipText ctx
                        return AuthPage(userState, inbox, AuthPageState.SignUp)
                    | _ -> return this
                | _ ->
                    return this
            | SignUp ->
                match ctx.Update.Message with
                | Some { Text = Some name } ->
                    let advertiser = {
                        advertiser_id = Guid.NewGuid()
                        name = name
                    }
                    use controller = new AdvertiserController()
                    let! response = controller.CreateAdvertiser advertiser |> Async.AwaitTask
                    match response with
                    | Success [advertiser] ->
                        sendMessage userState.User.Id (sprintf (Printf.StringFormat<_>(userState.Lang.SuccessAuthText))
                                                           (advertiser.advertiser_id.ToString())) ctx
                        inbox.Post(Command.Start, ctx)
                        return AdvertiserPage(userState, inbox, advertiser)
                    | Error code ->
                        sendMessage userState.User.Id (sprintf (Printf.StringFormat<_>(userState.Lang.ErrorText))
                                                           (code.ToString())) ctx
                        inbox.Post(Command.Start, ctx)
                        return AuthPage(userState, inbox, AuthPageState.Start)
                    | _ ->
                        sendMessage userState.User.Id (sprintf (Printf.StringFormat<_>(userState.Lang.ErrorText))
                                                           "Internal Error") ctx
                        inbox.Post(Command.Start, ctx)
                        return AuthPage(userState, inbox, AuthPageState.Start)
                | _ ->
                    return this
            | SignIn ->
                match ctx.Update.Message with
                | Some { Text = Some id } ->
                    use controller = new AdvertiserController()
                    let! response = controller.GetAdvertiser id |> Async.AwaitTask
                    match response with
                    | Success advertiser ->
                        sendMessage userState.User.Id (sprintf (Printf.StringFormat<_>(userState.Lang.SuccessAuthText))
                                                           (advertiser.advertiser_id.ToString())) ctx
                        inbox.Post(Command.Start, ctx)
                        return AdvertiserPage(userState, inbox, advertiser)
                    | Error code ->
                        sendMessage userState.User.Id (sprintf (Printf.StringFormat<_>(userState.Lang.ErrorText))
                                                           (code.ToString())) ctx
                        inbox.Post(Command.Start, ctx)                                   
                        return AuthPage(userState, inbox, AuthPageState.Start)
                | _ ->
                    return this
        }    