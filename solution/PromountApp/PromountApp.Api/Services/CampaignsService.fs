namespace PromountApp.Api.Services

open System
open System.Linq
open Microsoft.EntityFrameworkCore
open PromountApp.Api.Models
open PromountApp.Api.Utils

type ICampaignsService =
    abstract member CreateCampaign: Campaign -> Async<ServiceResponse<Campaign>>
    abstract member GetAllCampaigns: Guid -> int * int -> Async<ServiceResponse<Campaign seq * int>>
    abstract member GetCampaign: Guid -> Guid -> Async<ServiceResponse<Campaign>>
    abstract member UpdateCampaign: Guid -> CampaignUpdate -> Async<ServiceResponse<Campaign>>
    abstract member DeleteCampaign: Guid -> Guid -> Async<ServiceResponse<unit>>
    
type CampaignsService(dbContext: PromountContext, advertisersService: IAdvertisersService) =
    let isAdvertiserExists advertiserId = async {
        match! advertisersService.GetAdvertiser(advertiserId) with
        | Success _ -> return true
        | _ -> return false
    }
    
    interface ICampaignsService with
        member this.CreateCampaign campaign= async {
            let! advertiserExists = isAdvertiserExists campaign.advertiser_id
            if advertiserExists then
                let campaign = CampaignDb.BuildFrom campaign
                let! campaign = dbContext.Campaigns.AddAsync(campaign).AsTask() |> Async.AwaitTask
                let campaign = campaign.Entity.GetCampaign()
                
                let! _ = dbContext.SaveChangesAsync() |> Async.AwaitTask
                return Success campaign
            else
                return NotFounded
        }
        
        member this.GetAllCampaigns advertiserId (size, page) = async {
            let! advertiserExists = isAdvertiserExists advertiserId
            if advertiserExists then
                let campaigns = dbContext.Campaigns.Where(fun c -> c.advertiser_id = advertiserId)
                let! count = campaigns.CountAsync() |> Async.AwaitTask
                
                let offset = size * page
                let! campaigns = campaigns.Skip(offset).Take(size).ToArrayAsync() |> Async.AwaitTask
                
                let campaigns =
                    campaigns |> Array.map(_.GetCampaign())
                
                return Success (campaigns, count)
            else
                return NotFounded
        }
        
        member this.GetCampaign advertiserId campaignId = async {
            try
                let! advertiserExists = isAdvertiserExists advertiserId
                if advertiserExists then
                    let! campaign = dbContext.Campaigns.FindAsync(campaignId).AsTask() |> Async.AwaitTask
                    if campaign.advertiser_id = advertiserId then
                        let campaign = campaign.GetCampaign()
                        return Success campaign
                    else
                        return NotFounded
                else
                    return NotFounded
            with
            | :? NullReferenceException ->
                return NotFounded
        }
        
        member this.UpdateCampaign advertiserId campaignUpd = async {
            try
                let! advertiserExists = isAdvertiserExists advertiserId
                if advertiserExists then
                    let campaignId = campaignUpd.campaign_id
                    let! campaign = dbContext.Campaigns.AsNoTracking()
                                        .FirstOrDefaultAsync(fun c ->
                                            c.campaign_id = campaignId)
                                        |> Async.AwaitTask
                    if campaign.advertiser_id = advertiserId then
                        match campaignUpd.TryUpdate(campaign) with
                        | Some campaignUpdated ->
                            dbContext.Campaigns.Attach(campaignUpdated) |> ignore
                            dbContext.Entry(campaignUpdated).State <- EntityState.Modified
                            let! _ = dbContext.SaveChangesAsync() |> Async.AwaitTask
                            return Success (campaignUpdated.GetCampaign())
                        | None ->
                            return Conflict
                    else
                        return NotFounded
                else
                    return NotFounded
            with
            | :? NullReferenceException ->
                return NotFounded
        }
        
        member this.DeleteCampaign advertiserId campaignId = async {
            try
                let! advertiserExists = isAdvertiserExists advertiserId
                if advertiserExists then
                    let! campaign = dbContext.Campaigns.FindAsync(campaignId).AsTask() |> Async.AwaitTask
                    if campaign.advertiser_id = advertiserId then
                        dbContext.Campaigns.Remove(campaign) |> ignore
                        let! _ = dbContext.SaveChangesAsync() |> Async.AwaitTask
                        return Success()
                    else
                        return NotFounded
                else
                    return NotFounded
            with
            | :? NullReferenceException ->
                return NotFounded
        }
