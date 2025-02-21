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
            sendMessage userState.User.Id (sprintf (Printf.StringFormat<_>(userState.Lang.ConsoleHomeText))
                                                           advertiser.name) ctx
            return this
        }    