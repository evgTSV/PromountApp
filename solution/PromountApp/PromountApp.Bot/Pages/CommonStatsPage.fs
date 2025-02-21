namespace PromountApp.Bot.Pages

open System
open Microsoft.FSharp.Control
open PromountApp.Api.Bot.Models
open Funogram.Telegram.Bot
open PromountApp.Bot.Controllers
open PromountApp.Bot.Keyboards
open PromountApp.Bot.State
open PromountApp.Bot.Utils

type CommonStatsPage(userState: UserState, inbox: BotMailbox, advertiser: Advertiser) =
    let statsPageText (stat: Stats) (statDaily: DailyStats)= $"""
    <b>{userState.Lang.DailyStatsHeaderText}<\b> ({statDaily.date})
    
<b>{userState.Lang.ImpressionCountText}<\b> {stat.impressions_count}
<b>{userState.Lang.ClickCountText}<\b> {stat.clicks_count}
<b>{userState.Lang.ImpressionSpentText}<\b> {stat.spent_impressions}
<b>{userState.Lang.ClickSpentText}<\b> {stat.clicks_count}

<b>{userState.Lang.TotalSpentText}<\b> {stat.spent_total}

----------------->>

    <b>{userState.Lang.StatsHeaderText}<\b>

<b>{userState.Lang.ImpressionCountText}<\b> {stat.impressions_count}
<b>{userState.Lang.ClickCountText}<\b> {stat.clicks_count}
<b>{userState.Lang.ImpressionSpentText}<\b> {stat.spent_impressions}
<b>{userState.Lang.ClickSpentText}<\b> {stat.clicks_count}

<b>{userState.Lang.TotalSpentText}<\b> {stat.spent_total}
    """
    
    interface IPage with
        member this.OnMassageHandler ctx command = async {
            use controller = new StatsController()
            
            let! stats = controller.GetAdvertiserStats(advertiser) |> Async.AwaitTask
            let! statsDaily = controller.GetAdvertiserStatsDaily(advertiser) |> Async.AwaitTask
                        
            match stats with
            | Success stats ->
                match statsDaily with
                | Success statsDaily ->
                    let text = (statsPageText stats statsDaily)
                    sendMessage userState.User.Id text ctx
                    inbox.Post(Command.Back, ctx)
                    inbox.Post(Command.Start, ctx)
                    return None
                | Error code ->
                    sendMessage userState.User.Id (sprintf (Printf.StringFormat<_>(userState.Lang.ErrorText))
                                                       (code.ToString())) ctx
                    inbox.Post(Command.Back, ctx)
                    inbox.Post(Command.Start, ctx)
                    return None
            | Error code ->
                    sendMessage userState.User.Id (sprintf (Printf.StringFormat<_>(userState.Lang.ErrorText))
                                                       (code.ToString())) ctx
                    inbox.Post(Command.Back, ctx)
                    inbox.Post(Command.Start, ctx)
                    return None
        }    