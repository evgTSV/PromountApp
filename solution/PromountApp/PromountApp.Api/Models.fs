﻿module PromountApp.Api.Models

open System
open System.ComponentModel.DataAnnotations
open FSharp.Data.Validator
open Microsoft.FSharp.Linq
open PromountApp.Api.Utils

[<CLIMutable>]
type Day = {
    current_date: int
} with
    interface IValidatable with
        member this.Validate() =
            this.current_date |> inRange (0, TimeSpan.MaxValue.TotalDays |> int)

type Gender = MALE = 0 | FEMALE = 1 | ALL = 2

[<CLIMutable>]
type Client = {
    [<Key>]
    client_id: Guid
    [<Required>]
    login: string
    [<Required>]
    age: int
    [<Required>]
    location: string
    [<Required>]
    gender: string
} with
    interface IValidatable with
        member this.Validate() =
            this.login |> requiresTextLength (1, 200)
            && this.age |> inRange (0, 100)
            && this.location |> requiresTextLength (1, 1000)
            && this.gender |> (isEnumCase typeof<Gender> |&&| (<>) "ALL")

[<CLIMutable>]
type Advertiser = {
    [<Key>]
    advertiser_id: Guid
    [<Required>]
    name: string
} with
    interface IValidatable with
        member this.Validate() =
            this.name |> requiresTextLength (1, 200)
            
[<CLIMutable>]
type MLScore = {
    [<Required>]
    client_id: Guid
    [<Required>]
    advertiser_id: Guid
    [<Required>]
    score: int
}
  
[<CLIMutable>]
type Targeting = {
    gender: string | null
    age_from: int Nullable
    age_to: int Nullable
    location: string | null
} with
    interface IValidatable with
        member this.Validate() =
            this.gender |> validateOption' (isEnumCase typeof<Gender>)
            && this.age_from |> ((_.HasValue >> not) |=| inRange (0, 100))
            && this.age_to |> ((_.HasValue >> not) |=| inRange (0, 100))
            && ((this.age_from.HasValue && this.age_to.HasValue) |> not
                || this.age_from.Value <= this.age_to.Value)
            && this.location |> validateOption' (requiresTextLength (1, 1000))
            
[<CLIMutable>]
type Campaign = {
    [<Required>]
    campaign_id: Guid
    [<Required>]
    advertiser_id: Guid
    [<Required>]
    impressions_limit: int
    [<Required>]
    clicks_limit: int
    [<Required>]
    cost_per_impression: float
    [<Required>]
    cost_per_click: float
    [<Required>]
    ad_title: string
    [<Required>]
    ad_text: string
    [<Required>]
    start_date: int
    [<Required>]
    end_date: int
    [<Required>]
    targeting: Targeting
} with
    interface IValidatable with
        member this.Validate() =
            let timeService = ServiceLocator.GetService<TimeConfig>()
            this.ad_title |> requiresTextLength (0, 200)
            && this.ad_text |> requiresTextLength (0, 5000)
            && this.start_date |> (inRange (0, TimeSpan.MaxValue.TotalDays |> int)
                    |&&| (<=) (timeService.CurrentTime.TotalDays |> int))
            && this.end_date |> (inRange (0, TimeSpan.MaxValue.TotalDays |> int)
                    |&&| (<=) (timeService.CurrentTime.TotalDays |> int))
            && this.start_date <= this.end_date
            && (this.targeting :> IValidatable).Validate()
      
[<CLIMutable>]      
type CampaignDb = {
    [<Key>]
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
} with
    static member BuildFrom(campaign: Campaign) = {
            campaign_id = campaign.campaign_id
            advertiser_id = campaign.advertiser_id
            impressions_limit = campaign.impressions_limit
            clicks_limit = campaign.clicks_limit
            cost_per_impression = campaign.cost_per_impression
            cost_per_click = campaign.cost_per_click
            ad_title = campaign.ad_title
            ad_text = campaign.ad_text
            start_date = campaign.start_date
            end_date = campaign.end_date
            gender = campaign.targeting.gender
            age_from = campaign.targeting.age_from
            age_to = campaign.targeting.age_to
            location = campaign.targeting.location
        }
    member this.GetCampaign() = {
            campaign_id = this.campaign_id
            advertiser_id = this.advertiser_id
            impressions_limit = this.impressions_limit
            clicks_limit = this.clicks_limit
            cost_per_impression = this.cost_per_impression
            cost_per_click = this.cost_per_click
            ad_title = this.ad_title
            ad_text = this.ad_text
            start_date = this.start_date
            end_date = this.end_date
            targeting = {
                gender = this.gender
                age_from = this.age_from
                age_to = this.age_to
                location = this.location
            }
        }
        
            
[<CLIMutable>]
type CampaignUpdate = {
    campaign_id: Guid
    impressions_limit: int Nullable
    clicks_limit: int Nullable
    cost_per_impression: float Nullable
    cost_per_click: float Nullable
    ad_title: string | null
    ad_text: string | null
    start_date: int Nullable
    end_date: int Nullable
    targeting: Targeting | null
} with
    member this.TryUpdate(campaign: CampaignDb) =
        let timeService = ServiceLocator.GetService<TimeConfig>()
        let totalDays = timeService.GetTotalDays()
        let isValid =
            this.impressions_limit |> validateOptionV (fun _ -> campaign.start_date > totalDays)
            && this.clicks_limit |> validateOptionV (fun _ -> campaign.start_date > totalDays)
            && this.start_date |> validateOptionV (fun _ -> campaign.start_date > totalDays)
            && this.end_date |> validateOptionV (fun _ -> campaign.start_date > totalDays)
        if isValid then
            let campaign = campaign.GetCampaign()
            Some ({ campaign with
                        impressions_limit = defaultIfNullV campaign.impressions_limit this.impressions_limit
                        clicks_limit = defaultIfNullV campaign.clicks_limit this.clicks_limit
                        cost_per_impression = defaultIfNullV campaign.cost_per_impression this.cost_per_impression
                        cost_per_click = defaultIfNullV campaign.cost_per_click this.cost_per_click
                        ad_title = defaultIfNull campaign.ad_title this.ad_title
                        ad_text = defaultIfNull campaign.ad_text this.ad_text
                        start_date = defaultIfNullV campaign.start_date this.start_date
                        end_date = defaultIfNullV campaign.end_date this.end_date
                        targeting = defaultIfNull campaign.targeting this.targeting
            } |> CampaignDb.BuildFrom)
        else None
    
    interface IValidatable with
        member this.Validate() =
            let timeService = ServiceLocator.GetService<TimeConfig>()
            let totalDays = timeService.GetTotalDays()
            this.ad_title |> validateOption' (requiresTextLength (0, 200))
            && this.ad_text |> validateOption' (requiresTextLength (0, 5000))
            && this.start_date |> validateOptionV (inRange (0, TimeSpan.MaxValue.TotalDays |> int)
                    |&&| (<=) totalDays)
            && this.end_date |> validateOptionV (inRange (0, TimeSpan.MaxValue.TotalDays |> int)
                    |&&| (<=) totalDays)
            && ((this.start_date.HasValue && this.end_date.HasValue) |> not
                || this.start_date.Value <= this.end_date.Value)
            && this.targeting |> validateOption' (fun t -> (t :> IValidatable).Validate())

[<CLIMutable>]
type Stats = {
    impressions_count: int
    clicks_count: int
    conversion: float
    spent_impressions: float
    spent_clicks: float
    spent_total: float
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