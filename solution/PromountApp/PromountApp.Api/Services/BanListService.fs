namespace PromountApp.Api.Services

open System
open System.Linq
open PromountApp.Api.Models
open PromountApp.Api.ModerationModels
open PromountApp.Api.Utils

type IBanListService =
    abstract member GetBanList: unit -> Async<ServiceResponse<BanLog seq>>
    abstract member Ban: Guid -> Guid -> AnalyzeResponse -> Async<ServiceResponse<unit>>
    abstract member Unban: Guid -> Async<ServiceResponse<unit>>
    
type BanListService(dbContext: PromountContext) =
    
    let getBanLog (id: Guid) = async {
        let! result = dbContext.BanLogs.FindAsync(id).AsTask() |> Async.AwaitTask 
        return result
    }
    
    let existsBanLog advertiserId campaignId = async {
        try
            let! result = dbContext.BanLogs.FindAsync(fun l -> l.campaign_id = campaignId && l.advertiser_id = advertiserId).AsTask() |> Async.AwaitTask
            return result.id = result.id
        with
        | _ ->
            return false
    }
    
    interface IBanListService with
        member this.GetBanList() = async {
            let banList = dbContext.BanLogs.ToArray()
            return Success banList
        }
        
        member this.Ban advertiserId campaignId analyzeResult = async {
            try
                let! campaign = dbContext.Campaigns.FindAsync(campaignId).AsTask() |> Async.AwaitTask
                let! exists = existsBanLog advertiserId campaignId
                if campaign.advertiser_id = advertiserId then
                    if exists |> not then
                        let log = {
                            id = campaignId
                            advertiser_id = advertiserId
                            campaign_id = campaignId
                            probability = analyzeResult
                        }
                        let! _ = dbContext.BanLogs.AddAsync(log).AsTask() |> Async.AwaitTask
                        let! _ = dbContext.SaveChangesAsync() |> Async.AwaitTask
                        return Success()
                    else
                        return Success()
                else
                    return NotFound
            with
            | :? NullReferenceException ->
                return NotFound
        }
        
        member this.Unban id= async {
            try
                let! log = getBanLog id
                let _ = dbContext.BanLogs.Remove(log)
                let! _ = dbContext.SaveChangesAsync() |> Async.AwaitTask
                return Success()
            with
            | _ ->
                return NotFound
        }