namespace PromountApp.Api

open System
open System.ComponentModel.DataAnnotations.Schema
open Newtonsoft.Json
open PromountApp.Api.Utils

module ModerationModels =
    [<CLIMutable>]
    type Comment = {
        text: string
    }

    [<CLIMutable>]
    type RequestedAttributes = {
        TOXICITY: obj
        SEVERE_TOXICITY: obj
        IDENTITY_ATTACK: obj
        INSULT: obj
        PROFANITY: obj
        THREAT: obj
    }

    [<CLIMutable>]
    type AnalyzeRequest = {
        comment: Comment
        languages: string list
        requestedAttributes: RequestedAttributes
    } with
        static member CreateRequest(text: string) = {
            comment = {
                text = text
            }
            languages = ["ru"; "en"]
            requestedAttributes = {
                TOXICITY = Object
                SEVERE_TOXICITY = Object
                IDENTITY_ATTACK = Object
                INSULT = Object
                PROFANITY = Object
                THREAT = Object
            }
        }

    [<CLIMutable>]
    type SummaryScore = {
        value: float
        [<JsonProperty("type")>]
        ``type``: string
    }

    [<CLIMutable>]
    type AttributeScore = {
        summaryScore: SummaryScore
    }

    [<CLIMutable>]
    type AttributeScores = {
        TOXICITY: AttributeScore
        SEVERE_TOXICITY: AttributeScore
        IDENTITY_ATTACK: AttributeScore
        INSULT: AttributeScore
        PROFANITY: AttributeScore
        THREAT: AttributeScore
    }

    [<CLIMutable>]
    type AnalyzeResponse = {
        attributeScores: AttributeScores
        languages: string list
        detectedLanguages: string list
        ml_prediction_score: float Nullable
    } with
        member this.IsProbablyIncorrect() =
            (this.ml_prediction_score |> validateOptionV ((>=) 0.65)
            && this.attributeScores.TOXICITY.summaryScore.value <= 0.50
            && this.attributeScores.SEVERE_TOXICITY.summaryScore.value <= 0.50
            && this.attributeScores.IDENTITY_ATTACK.summaryScore.value <= 0.50
            && this.attributeScores.INSULT.summaryScore.value <= 0.50
            && this.attributeScores.PROFANITY.summaryScore.value <= 0.50
            && this.attributeScores.THREAT.summaryScore.value <= 0.50) |> not

open ModerationModels

module Models =

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
        override this.ToString() =
            $"Рекламодатель - {this.name}"
            
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
        override this.ToString() =
            $"Целевая аудитория:
                Пол - {this.gender |> defaultIfNull (Enum.GetName(Gender.ALL))};
                Возраст от {this.age_from |> defaultIfNullV 0} до {this.age_to |> defaultIfNullV 100}
                Местоположение - {this.location |> defaultIfNull null}"
        
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
        override this.ToString() =
            $"Рекламная кампания {this.ad_title}:
                Продолжительность - с {this.start_date} по {this.end_date};
                Описание - {this.ad_text};
                {this.targeting}"
                
        interface IValidatable with
            member this.Validate() =
                let timeService = ServiceLocator.GetService<TimeConfig>()
                this.impressions_limit > 0 && this.impressions_limit >= this.clicks_limit
                && this.clicks_limit > 0
                && this.cost_per_click > 0
                && this.cost_per_impression > 0
                && this.ad_title |> requiresTextLength (0, 200)
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
                this.impressions_limit |> validateOptionV ((<) 0) 
                && this.clicks_limit |> validateOptionV ((<) 0)
                && this.cost_per_click |> validateOptionV ((<) 0)
                && this.cost_per_impression |> validateOptionV ((<) 0)
                && this.ad_title |> validateOption' (requiresTextLength (0, 200))
                && this.ad_text |> validateOption' (requiresTextLength (0, 5000))
                && this.start_date |> validateOptionV (inRange (0, TimeSpan.MaxValue.TotalDays |> int)
                        |&&| (<=) totalDays)
                && this.end_date |> validateOptionV (inRange (0, TimeSpan.MaxValue.TotalDays |> int)
                        |&&| (<=) totalDays)
                && ((this.start_date.HasValue && this.end_date.HasValue) |> not
                    || this.start_date.Value <= this.end_date.Value)
                && ((this.impressions_limit.HasValue && this.clicks_limit.HasValue) |> not
                    || this.impressions_limit.Value >= this.clicks_limit.Value)
                && this.targeting |> validateOption' (fun t -> (t :> IValidatable).Validate())

    [<CLIMutable>]
    type Ad = {
        ad_id: Guid
        ad_title: string
        ad_text: string
        advertiser_id: Guid
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

    [<CLIMutable>]
    type Click = {
        [<Required>]
        client_id: Guid
    }

    [<CLIMutable>]
    type ImpressionLog = {
        [<Key>]
        id: Guid
        client_id: Guid
        campaign_id: Guid
        cost: float
        timestamp: int
    }

    [<CLIMutable>]
    type ClickLog = {
        [<Key>]
        id: Guid
        client_id: Guid
        campaign_id: Guid
        cost: float
        timestamp: int
    }
    
    [<CLIMutable>]
    type BanLog = {
        [<Key>]
        id: Guid
        advertiser_id: Guid
        campaign_id: Guid
        
        [<Column(TypeName = "jsonb")>]
        [<JsonConverter(typeof<JsonConvert>)>]
        probability: AnalyzeResponse
    }