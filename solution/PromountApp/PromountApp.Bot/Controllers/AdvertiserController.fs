namespace PromountApp.Bot.Controllers

open System
open System.Net.Http
open PromountApp.Api.Bot.Models
open PromountApp.Bot.Utils
    
type AdvertiserController() =
    inherit BaseController()
    
    member this.CreateAdvertiser (advertiser: Advertiser)=
        this.Post<_, Advertiser list> "advertisers/bulk" [| advertiser |]
        
    member this.GetAdvertiser id =
        this.Get<Advertiser> $"advertisers/{id}"