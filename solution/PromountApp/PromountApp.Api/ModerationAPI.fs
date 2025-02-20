module PromountApp.Api.ModerationAPI

open System
open System.Net.Http
open System.Text
open Newtonsoft.Json
open PromountApp.Api.Models
open PromountApp.Api.Utils

let key = getEnv "GC_PERSPECTIVE_TOKEN"

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

let analyzeCampaign (advertiser: Advertiser) (campaign: Campaign) = async {
    let text = $"Рекламодатель {advertiser.name}; {campaign}"
    let request = AnalyzeRequest.CreateRequest text
    
    let client = new HttpClient()
    let url = $"https://commentanalyzer.googleapis.com/v1alpha1/comments:analyze?key={key}"
    let jsonContent = JsonConvert.SerializeObject(request)
    let content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
    
    let! response = client.PostAsync(url, content) |> Async.AwaitTask
    if response.IsSuccessStatusCode then
        let! jsonContent = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        let result = JsonConvert.DeserializeObject<AnalyzeResponse>(jsonContent)
        return Success result
    else
        return NotFound
}