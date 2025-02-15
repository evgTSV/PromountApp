module PromountApp.Api.AdMatchEngine

open System
open PromountApp.Api.Models

[<Literal>]
let private age_score = 0.4
[<Literal>]
let private gender_score = 0.4
[<Literal>]
let private location_score = 0.2
[<Literal>]
let private costPerImpression_ratio = 0.7
[<Literal>]
let private costPerClick_ratio = 0.3
[<Literal>]
let private impressionsProgress_ratio = 0.4
[<Literal>]
let private clicksProgress_ratio = 0.6

let private costPerAd (adCampaign: CampaignDb) =
    adCampaign.cost_per_impression * costPerImpression_ratio
    + adCampaign.cost_per_click * costPerClick_ratio
    
let private getScoreForAge (adCampaign: CampaignDb) (client: Client) =
    if (adCampaign.age_from |> defaultIfNullV 0) < client.age
        && (adCampaign.age_to |> defaultIfNullV 100) > client.age
        && (adCampaign.age_from.HasValue || adCampaign.age_to.HasValue)
        then age_score else 0
        
let private getScoreFor (score: float) (adField: 'a Nullable) (clientField: 'a) : float =
    if adField.HasValue
        && clientField = adField.Value
        then score else 0
        
let private getScoreFor' (score: float) (adField: 'a | null) (clientField: 'a) : float =
    match adField with
    | null -> 0
    | adField -> if adField = clientField then score else 0
    
let private getTargetsScore (adCampaign: CampaignDb) (client: Client)=
    getScoreForAge adCampaign client
    + ((adCampaign.gender, client.gender) ||> getScoreFor' gender_score)
    + ((adCampaign.location, client.location) ||> getScoreFor' location_score)
    
let private adProgress (adCampaign: CampaignDb) (stats: Stats)=
    ((stats.impressions_count |> float) / (adCampaign.impressions_limit |> float)) * impressionsProgress_ratio
    +
    ((stats.clicks_count |> float) / (adCampaign.clicks_limit |> float)) * clicksProgress_ratio