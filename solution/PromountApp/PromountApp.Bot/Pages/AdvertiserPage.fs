namespace PromountApp.Bot.Pages

open System
open Microsoft.FSharp.Control
open PromountApp.Api.Bot.Models
open Funogram.Telegram.Bot
open PromountApp.Bot.Controllers
open PromountApp.Bot.Keyboards
open PromountApp.Bot.State
open PromountApp.Bot.Utils

type AdvertiserPage(userState: UserState, inbox: BotMailbox, advertiser: Advertiser) =
    
    interface IPage with
        member this.OnMassageHandler ctx command = async {
            match ctx.Update.CallbackQuery with
            | Some { Data = Some callback } ->
                match callback with
                | s when s = createCampaignOutput ->
                    return None
                | s when s = commonStatsOutput ->
                    return Some (CommonStatsPage(userState, inbox, advertiser))
                | s when s = campaignsListOutput ->
                    return None
                | s when s = advertiserExitOutput ->
                    inbox.Post(Command.Back, ctx)
                    inbox.Post(Command.Start, ctx)
                    return None
                | _ ->
                    return None
            | _ ->    
                let keyboard = advertiserHomeKeyboard userState.Lang
                sendMessageMarkup userState.User.Id (sprintf (Printf.StringFormat<_>(userState.Lang.ConsoleHomeText))
                                                               advertiser.name) keyboard ctx
                return None
        }    