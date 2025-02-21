namespace PromountApp.Api.Bot

open System

module Models =

    [<CLIMutable>]
    type Day = {
        current_date: int
    }

    [<CLIMutable>]
    type Client = {
        client_id: Guid
        login: string
        age: int
        location: string
        gender: string
    }

    [<CLIMutable>]
    type Advertiser = {
        advertiser_id: Guid
        name: string
    }
                
    [<CLIMutable>]
    type MLScore = {
        client_id: Guid
        advertiser_id: Guid
        score: int
    }
    
    [<CLIMutable>]
    type Targeting = {
        gender: string | null
        age_from: int Nullable
        age_to: int Nullable
        location: string | null
    }
            
    [<CLIMutable>]
    type Campaign = {
        campaign_id: Guid
        advertiser_id: Guid
        impressions_limit: int
        clicks_limit: int
        cost_per_impression: float
        cost_per_click: float
        ad_title: string
        ad_text: string
        start_date: int
        end_date: int
        targeting: Targeting
    }
          
    [<CLIMutable>]      
    type CampaignDb = {
        campaign_id: Guid
        advertiser_id: Guid
        impressions_limit: int
        clicks_limit: int
        cost_per_impression: float
        cost_per_click: float
        ad_title: string
        ad_text: string
        start_date: int
        end_date: int
        gender: string | null
        age_from: int Nullable
        age_to: int Nullable
        location: string | null
    }

    [<CLIMutable>]
    type DailyStats = {
        impressions_count: int
        clicks_count: int
        conversion: float
        spent_impressions: float
        spent_clicks: float
        spent_total: float
        date: int
    }

    [<CLIMutable>]
    type Stats = {
        impressions_count: int
        clicks_count: int
        conversion: float
        spent_impressions: float
        spent_clicks: float
        spent_total: float
    } with
        member this.ToDaily (day: int) = {
            impressions_count = this.impressions_count
            clicks_count = this.clicks_count
            conversion = this.conversion
            spent_impressions = this.spent_impressions
            spent_clicks = this.spent_clicks
            spent_total = this.spent_total
            date = day
        }       