namespace PromountApp.Api.Services

open System
open PromountApp.Api
open PromountApp.Api.AdMatchEngine
open PromountApp.Api.Models
open PromountApp.Api.Utils
open Microsoft.EntityFrameworkCore

type IAdsService =
    abstract member GetBestMatchAd: Guid -> Async<ServiceResponse<Ad>>
    abstract member ClickOnAd: Guid -> Guid -> Async<ServiceResponse<unit>>
    
type AdsService(dbContext: PromountContext, timeService: TimeConfig) =
    
    let clickOnAd (adId: Guid) (clientId: Guid) = async {
        let currDay = timeService.GetTotalDays()
        let! ad = dbContext.Campaigns.FindAsync(adId).AsTask() |> Async.AwaitTask
        let! client = dbContext.Clients.FindAsync(clientId).AsTask() |> Async.AwaitTask
        let! stats = getCampaignStatistics (dbContext, timeService) client ad |> Async.AwaitTask
        let isInLimits =
            stats
            |> Option.map snd
            |> Option.map (_.out_of_limits_click >> not)
            |> Option.defaultValue false
        
        if currDay >= ad.start_date && currDay <= ad.end_date && isInLimits then     
            let log = {
                id = Guid.NewGuid()
                client_id = client.client_id
                campaign_id = ad.campaign_id
                cost = ad.cost_per_click
                timestamp = currDay
            }
            let! _ = dbContext.ClickLogs.AddAsync(log).AsTask() |> Async.AwaitTask
            let! _ = dbContext.SaveChangesAsync() |> Async.AwaitTask
            return Success()
        else
            return NotFound
    }
    
    let getBestMatch (clientId: Guid) = async {
        let! client = dbContext.Clients.FindAsync(clientId).AsTask() |> Async.AwaitTask
        let! campaignId = getBestCampaign client (dbContext, timeService) |> Async.AwaitTask
        
        match campaignId with
        | Some campaignId ->
            let! campaign = dbContext.Campaigns.FindAsync(campaignId).AsTask() |> Async.AwaitTask
            let (log : ImpressionLog) = {
                id = Guid.NewGuid()
                client_id = client.client_id
                campaign_id = campaign.campaign_id
                cost = campaign.cost_per_click
                timestamp = timeService.GetTotalDays()
            }
            let! _ = dbContext.ImpressionLogs.AddAsync(log).AsTask() |> Async.AwaitTask
            let! _ = dbContext.SaveChangesAsync() |> Async.AwaitTask
            let ad = {
                ad_id = campaign.campaign_id
                ad_title = campaign.ad_title
                ad_text = campaign.ad_text
                advertiser_id = campaign.advertiser_id
            }
            return Success ad
        | None ->
            return NotFound
    }
    
    interface IAdsService with
        member this.ClickOnAd adId clientId =
            ServiceAsyncResult.bind ServiceAsyncResult.return' (clickOnAd adId clientId)
        member this.GetBestMatchAd clientId=
            ServiceAsyncResult.bind ServiceAsyncResult.return' (getBestMatch clientId)