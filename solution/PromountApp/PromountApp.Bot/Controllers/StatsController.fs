namespace PromountApp.Bot.Controllers

open System
open System.Net.Http
open PromountApp.Api.Bot.Models
open PromountApp.Bot.Utils

type StatsController() =
    inherit BaseController()
    
    member this.GetAdvertiserStats (advertiser: Advertiser)=
        this.Get<Stats> $"stats/advertisers/{advertiser.advertiser_id}/campaigns"
        
    member this.GetAdvertiserStatsDaily (advertiser: Advertiser)=
        this.Get<DailyStats> $"stats/advertisers/{advertiser.advertiser_id}/campaigns/daily"
        
    member this.GetCampaignsStats (campaign: Campaign)=
        this.Get<Stats> $"stats/campaigns/{campaign.campaign_id}/campaigns"
        
    member this.GetCampaignsStatsDaily (campaign: Campaign)=
        this.Get<DailyStats> $"stats/campaigns/{campaign.campaign_id}/campaigns/daily"