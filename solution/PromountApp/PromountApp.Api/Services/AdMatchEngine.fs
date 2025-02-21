module PromountApp.Api.AdMatchEngine

open System
open System.Collections.Generic
open System.Linq
open System.Threading
open FSharp.Collections.ParallelSeq
open FSharp.Data.Validator
open Microsoft.FSharp.Collections
open PromountApp.Api.Models
open PromountApp.Api.Services
open PromountApp.Api.Utils
open Microsoft.EntityFrameworkCore

[<Literal>]
let private age_score = 0.4
[<Literal>]
let private gender_score = 0.4
[<Literal>]
let private location_score = 0.2
[<Literal>]
let private costPerImpression_ratio = 0.6
[<Literal>]
let private costPerClick_ratio = 0.4
[<Literal>]
let private impressionsProgress_ratio = 0.2
[<Literal>]
let private clicksProgress_ratio = 0.8

type ScoresCategories = {
    campaign_id: Guid
    ml_score: int
    ad_cost: float
    ad_progress: float
    out_of_limits_impression: bool
    out_of_limits_click: bool
    in_ban_list: bool
} with
    static member Default= {
        campaign_id = Guid.Empty
        ml_score = 0
        ad_cost = 0
        ad_progress = 0
        out_of_limits_impression = true
        out_of_limits_click = true
        in_ban_list = false
    }

let degreeOfParallel = Environment.ProcessorCount

let private costPerAd (adCampaign: CampaignDb) =
    adCampaign.cost_per_impression * costPerImpression_ratio
    + adCampaign.cost_per_click * costPerClick_ratio
    
let private getScoreForAge (adCampaign: CampaignDb) (client: Client) =
    if (adCampaign.age_from |> defaultIfNullV 0) <= client.age
        && (adCampaign.age_to |> defaultIfNullV 100) >= client.age
        then age_score else 0
        
let private getScoreFor (score: float) (adField: 'a Nullable) (clientField: 'a) : float =
    if adField.HasValue
        && clientField = adField.Value
        then score else 0
        
let private getScoreFor' (score: float) (adField: 'a | null) (clientField: 'a) : float =
    match adField with
    | null -> 0
    | adField -> if adField = clientField then score else 0
    
let private getTargetsScore (client: Client) (adCampaign: CampaignDb)=
    getScoreForAge adCampaign client
    + ((adCampaign.gender, client.gender) ||> getScoreFor' gender_score)
    + ((adCampaign.location, client.location) ||> getScoreFor' location_score)
    
let private adProgress (adCampaign: CampaignDb) (stats: Stats)=
    ((stats.impressions_count |> float) / (adCampaign.impressions_limit |> float)) * impressionsProgress_ratio
    +
    ((stats.clicks_count |> float) / (adCampaign.clicks_limit |> float)) * clicksProgress_ratio
    
let checkTargets (client: Client) (adCampaign: CampaignDb) =
    (adCampaign.gender |> validateOption' ((=) (Enum.GetName(Gender.ALL)) |=| (=) client.gender))
    && (adCampaign.age_from |> validateOptionV ((>=) client.age))
    && (adCampaign.age_to |> validateOptionV ((<=) client.age))
    && (adCampaign.location |> validateOption' ((=) client.location))
    
let isActive (currDay: int) (adCampaign: CampaignDb) =
    adCampaign.start_date <= currDay
    && adCampaign.end_date >= currDay
    
let isCommon (adCampaign: CampaignDb) =
    adCampaign.gender |> validateOption' ((=) (Enum.GetName(Gender.ALL)))
    && ((not adCampaign.age_from.HasValue) || adCampaign.age_from.Value = 0)
    && ((not adCampaign.age_to.HasValue) || adCampaign.age_to.Value = 100)
    && adCampaign.location = null
    
let isBest (bestTarget: ScoresCategories) (adCampaign: ScoresCategories) =
    adCampaign.ad_cost >= bestTarget.ad_cost
    || adCampaign.ml_score >= bestTarget.ml_score
    
let isOutOfImpressionLimit (stats: Stats) (adCampaign: CampaignDb) =
    stats.impressions_count > adCampaign.impressions_limit
    
let isOutOfClickLimit (stats: Stats) (adCampaign: CampaignDb) =
    stats.clicks_count > adCampaign.clicks_limit
    
let inBanList (adId: Guid) (dbContext: PromountContext)= async {
    let! result = dbContext.BanLogs.Where(fun l -> l.campaign_id = adId).CountAsync() |> Async.AwaitTask 
    return result > 0
}

let getCampaignStatistics (dbContext: PromountContext, timeService: TimeConfig) (client: Client) (adCampaign: CampaignDb) = task {
    let staticsService = StatisticsService(dbContext, timeService) :> IStatisticsService
    match! staticsService.GetCampaignStats(adCampaign.campaign_id) with
    | Success stats ->
        let! mlScore =
            dbContext.MLScores
                .Where(fun ml -> ml.client_id = client.client_id && ml.advertiser_id = adCampaign.advertiser_id)
                .ToArrayAsync()
        let mlScore = mlScore |> Array.tryHead |> Option.map _.score |> Option.defaultValue 0
        let! inBan = inBanList adCampaign.campaign_id dbContext
        let scores = {
            campaign_id = adCampaign.campaign_id
            ml_score = mlScore
            ad_cost = costPerAd adCampaign
            ad_progress = adProgress adCampaign stats
            out_of_limits_impression = isOutOfImpressionLimit stats adCampaign
            out_of_limits_click = isOutOfClickLimit stats adCampaign
            in_ban_list = inBan
        }
        return Some (adCampaign, scores)
    | _ ->
        return None
}
    
let getBestCampaignWithValidTarget (client: Client) (dbContext: PromountContext, timeService: TimeConfig)= task {
    try
        let services = dbContext, timeService
        let bestTargets =
              dbContext.Campaigns.AsEnumerable()
                  .Where(isActive (timeService.GetTotalDays()))
                  .Where(checkTargets client)
                  
        let bestTarget =
            bestTargets
            |> Seq.choose ((getCampaignStatistics services client) >> _.Result)
            |> Seq.filter (snd >> _.out_of_limits_impression >> not)
            |> Seq.filter (snd >> _.in_ban_list >> not)
            |> Seq.groupBy (fst >> getTargetsScore client)
            |> Seq.maxBy fst
            |> snd
            |> Seq.maxBy (fst >> costPerAd)
                  
        return Some (snd bestTarget)
    with
    | _ -> return None
}

let getCommonCampaign (dbContext: PromountContext, timeService: TimeConfig) = task {
    return dbContext.Campaigns.AsEnumerable()
                .Where(isActive (timeService.GetTotalDays()))
                .Where(isCommon)
                .ToArray()
}
 
let getBestCampaignByScores (bestTarget: ScoresCategories option) (scores: Dictionary<Guid, ScoresCategories>)=
    let bestTargetScores =
        bestTarget
        |> Option.defaultValue ScoresCategories.Default
        
    scores[bestTargetScores.campaign_id] <- bestTargetScores
        
    let bestCommonAdCampaigns =
        scores
        |> Seq.filter (fun s ->
             s.Value |> isBest bestTargetScores)
        |> Seq.filter (fun g -> g.Key <> Guid.Empty)
        |> Seq.groupBy _.Value.ad_progress
        |> Seq.sortBy fst
        |> Seq.toArray
        |> Array.tryHead
        
    bestCommonAdCampaigns |> Option.bind (fun ads ->
        if ads |> (snd >> Seq.isEmpty  >> not) then
            let random = ads |> (snd >> Seq.randomChoice)
            Some random.Key
        else
            None)
    
let semaphore = new SemaphoreSlim(3, 3)
let getBestCampaign (client: Client) (dbContext: PromountContext, timeService: TimeConfig) = task {
    try
        do! semaphore.WaitAsync()
        
        let services = dbContext, timeService
        let bestTargetTask = getBestCampaignWithValidTarget client services
        let commonCampaignsTask = getCommonCampaign services
        
        let! bestTargetStats = bestTargetTask
        let! commonCampaigns = commonCampaignsTask
        let commonCampaigns =
            commonCampaigns
            |> Seq.choose (fun bt -> getCampaignStatistics services client bt |> _.Result)
            |> Seq.filter (snd >> _.out_of_limits_impression >> not)
            |> Seq.filter (snd >> _.in_ban_list >> not)
            |> Seq.map (fun (c, s) -> (c.campaign_id, s))
            |> Seq.fold (fun (acc: Dictionary<_,_>) (key, value) ->
                acc[key] <- value
                acc
            ) (Dictionary<Guid, ScoresCategories>())
        
        return getBestCampaignByScores bestTargetStats commonCampaigns
    finally
        semaphore.Release() |> ignore
}