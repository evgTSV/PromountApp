module PromountApp.Api.ML

open System
open System.Linq
open System.Net.Http
open System.Text
open Microsoft.EntityFrameworkCore
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open PromountApp.Api.Models
open PromountApp.Api.Services
open PromountApp.Api.Utils

[<CLIMutable>]
type MessageContent = {
    ``type``: string
    text: string
}

[<CLIMutable>]
type Message = {
    role: string
    content: string
}

[<CLIMutable>]
type Request = {
    model: string
    messages: Message list
    request_id: string
    do_sample: bool
    stream: bool
    temperature: float
    top_p: float
    max_tokens: int
    user_id: string
} with
    static member CreateRequest input model = {
        model = model
        messages = [
            { role = "user"; content = input }
        ]
        request_id = $"unique_request_id_{Random.Shared.NextInt64()}"
        do_sample = true
        stream = false
        temperature = 0.8
        top_p = 0.6
        max_tokens = 1024
        user_id = $"unique_request_id_{Random.Shared.NextInt64()}"
    }   

let models = [|"glm-4-plus"; "glm-4-0520"; "glm-4"; "glm-4-air"; "glm-4-airx"; "glm-4-long"; "glm-4-flash"|]

let parseContent (json: string) =
    let jObject = JObject.Parse(json)
    let content = jObject.SelectToken("choices[0].message.content").ToString()
    content

let genTextFromML (dbContext: PromountContext) (advertiserId: Guid) (campaignId: Guid) = async {
        let! advertiser = dbContext.Advertisers.FindAsync(advertiserId).AsTask() |> Async.AwaitTask
        let! campaign = dbContext.Campaigns.FindAsync(campaignId).AsTask() |> Async.AwaitTask
        let campaign = campaign.GetCampaign()
        
        let key = getEnv "ZHIPU_KEY"
        let input =
            $"-Context: Создай текст для рекламной кампании {advertiser};
            Информация: {campaign};
            -Response Requirements: в своём ответе укажи только текст для кампании;
            -Language: {campaign.ad_title}"
            
        let client = new HttpClient()
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}")
        let url = "https://open.bigmodel.cn/api/paas/v4/chat/completions"
        
        let selectModel = Request.CreateRequest input
        
        return models
        |> Array.tryPick (fun m ->
            let jsonContent = JsonConvert.SerializeObject(selectModel m)
            let content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            let response = client.PostAsync(url, content).Result
            if response.IsSuccessStatusCode then
                let output = response.Content.ReadAsStringAsync().Result     
                Some (output |> parseContent)
            else None)
        |> Option.defaultValue "Все модели сейчас недоступны, попробуй позже"
        |> Success
    }