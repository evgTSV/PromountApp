module PromountApp.Api.ModerationAPI

open System
open System.Net.Http
open System.Text
open Newtonsoft.Json
open PromountApp.Api.Models
open PromountApp.Api.ModerationModels
open PromountApp.Api.Utils

let key = getEnv "GC_PERSPECTIVE_TOKEN"

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