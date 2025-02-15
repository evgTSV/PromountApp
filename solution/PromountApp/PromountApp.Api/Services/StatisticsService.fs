namespace PromountApp.Api.Services

open System
open System.Linq
open PromountApp.Api
open PromountApp.Api.Models
open PromountApp.Api.Utils
open Microsoft.EntityFrameworkCore

type IStatisticsService =
    abstract member GetCampaignStats: Guid -> Async<ServiceResponse<Stats>>
    abstract member GetCampaignStatsDaily: Guid -> Async<ServiceResponse<DailyStats>>
    abstract member GetAdvertiserStats: Guid -> Async<ServiceResponse<Stats>>
    abstract member GetAdvertiserStatsDaily: Guid -> Async<ServiceResponse<DailyStats>>

type StatisticsService(dbContext: PromountContext, timeService: TimeConfig) =
    
    let createStats (impressionLogs: ImpressionLog array) (clickLogs: ClickLog array) =
        let impressionCount, impressionSpent =
            impressionLogs |> Array.fold (fun acc l -> acc |++| (1, l.cost)) (0, 0)
        let clickCount, clickSpent =
            clickLogs |> Array.fold (fun acc l -> acc |++| (1, l.cost)) (0, 0)
            
        {
            impressions_count = impressionCount
            clicks_count = clickCount
            conversion = if impressionCount = 0 then 0 else ((clickCount |> float) / (impressionCount |> float) * (100 |> float))
            spent_impressions = impressionSpent
            spent_clicks = clickSpent
            spent_total = impressionSpent + clickSpent
        }
        
    let createStatsDaily (impressionLogs: ImpressionLog array) (clickLogs: ClickLog array) =
        let currentDay = timeService.GetTotalDays()
        createStats
            (impressionLogs |> Array.filter (fun l -> l.timestamp = currentDay))
            (clickLogs |> Array.filter (fun l -> l.timestamp = currentDay))
        |> _.ToDaily(currentDay)
        
    let getCampaignLogs (campaignId: Guid) = async {
        let! campaign = dbContext.Campaigns.FindAsync(campaignId).AsTask() |> Async.AwaitTask
        let campaignId = campaign.campaign_id
        let impressionLogs = dbContext.ImpressionLogs.Where(fun l -> l.campaign_id = campaignId).ToArray()
        let clickLogs = dbContext.ClickLogs.Where(fun l -> l.campaign_id = campaignId).ToArray()
        
        return Success (impressionLogs, clickLogs)
    }
    
    let getAdvertiserLogs (advertiserId: Guid) = async {
        let! advertiser = dbContext.Advertisers.FindAsync(advertiserId).AsTask() |> Async.AwaitTask
        let advertiserId = advertiser.advertiser_id
        let campaigns = dbContext.Campaigns.Where(fun c -> c.advertiser_id = advertiserId).ToArray()
        
        let impressionLogs =
            campaigns |> Array.collect (fun c -> dbContext.ImpressionLogs.Where(fun l -> l.campaign_id = c.campaign_id).ToArray())
        let clickLogs =
            campaigns |> Array.collect (fun c -> dbContext.ClickLogs.Where(fun l -> l.campaign_id = c.campaign_id).ToArray())
        
        return Success (impressionLogs, clickLogs)
    }
    
    interface IStatisticsService with
    
        member this.GetCampaignStats(campaignId) =
            ServiceAsyncResult.bind (fun logs -> ServiceAsyncResult.return' (logs ||> createStats)) (getCampaignLogs campaignId)

        member this.GetCampaignStatsDaily(campaignId) =
            ServiceAsyncResult.bind (fun logs -> ServiceAsyncResult.return' (logs ||> createStatsDaily)) (getCampaignLogs campaignId)

        member this.GetAdvertiserStats(advertiserId) =
            ServiceAsyncResult.bind (fun logs -> ServiceAsyncResult.return' (logs ||> createStats)) (getAdvertiserLogs advertiserId)

        member this.GetAdvertiserStatsDaily(advertiserId) =
            ServiceAsyncResult.bind (fun logs -> ServiceAsyncResult.return' (logs ||> createStatsDaily)) (getAdvertiserLogs advertiserId)