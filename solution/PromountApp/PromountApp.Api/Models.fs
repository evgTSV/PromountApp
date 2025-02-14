module PromountApp.Api.Models

open System
open System.ComponentModel.DataAnnotations
open FSharp.Data.Validator

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
    login: string
    age: int
    location: string
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
    name: string
} with
    interface IValidatable with
        member this.Validate() =
            this.name |> requiresTextLength (1, 200)
            
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
} with
    interface IValidatable with
        member this.Validate() =
            this.gender |> validateOption' (isEnumCase typeof<Gender>)
            && this.age_from |> ((_.HasValue >> not) |=| inRange (0, 100))
            && this.age_to |> ((_.HasValue >> not) |=| inRange (0, 100))
            && this.location |> validateOption' (requiresTextLength (1, 1000))
            
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
} with
    interface IValidatable with
        member this.Validate() =
            this.ad_title |> requiresTextLength (0, 200)
            && this.ad_text |> requiresTextLength (0, 5000)
            && this.start_date |> inRange(0, TimeSpan.MaxValue.TotalDays |> int)
            && this.end_date |> inRange(0, TimeSpan.MaxValue.TotalDays |> int)
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