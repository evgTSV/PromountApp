namespace PromountApp.Bot.Controllers

open System
open System.Net
open System.Net.Http
open System.Text
open System.Threading.Tasks
open PromountApp.Bot.Utils

type BaseController() =
    let uri = Uri("http://web:8080")
    let httpClient = new HttpClient()
    do httpClient.BaseAddress <- uri
    
    member this.HttpClient = httpClient
    member this.Uri = uri
    
    member this.Get<'a> (url: string) : Task<ControllerResponse<'a>> = task {
        try
            let! response = this.HttpClient.GetAsync(url)
            
            if response.IsSuccessStatusCode then
                let! object = deserializeJson (response.Content.ReadAsStream())
                return Success object.Value
            else
                return Error response.StatusCode
        with
        | ex -> return Error HttpStatusCode.InternalServerError
    }
    
    member this.Post<'a, 'b> (url: string) (data: 'a) : Task<ControllerResponse<'b>> = task {
        try
            let jsonContent = serializeJson data
            let content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            let! response = this.HttpClient.PostAsync(url, content)
            
            if response.IsSuccessStatusCode then
                let! object = deserializeJson (response.Content.ReadAsStream())
                return Success object.Value
            else
                return Error response.StatusCode
        with
        | ex -> return Error HttpStatusCode.InternalServerError
    }
    
    member this.Put<'a, 'b> (url: string) (data: 'a) : Task<ControllerResponse<'b>> = task {
        try
            let jsonContent = serializeJson data
            let content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            let! response = this.HttpClient.PutAsync(url, content)
            
            if response.IsSuccessStatusCode then
                let! object = deserializeJson (response.Content.ReadAsStream())
                return Success object.Value
            else
                return Error response.StatusCode
        with
        | _ -> return Error HttpStatusCode.InternalServerError
    }
    
    member this.Delete<'a> (url: string) : Task<ControllerResponse<'a>> = task {
        try
            let! response = this.HttpClient.DeleteAsync(url)
            
            if response.IsSuccessStatusCode then
                let! object = deserializeJson (response.Content.ReadAsStream())
                return Success object.Value
            else
                return Error response.StatusCode
        with
        | _ -> return Error HttpStatusCode.InternalServerError
    }
    
    interface IDisposable with
        member this.Dispose() = httpClient.Dispose()