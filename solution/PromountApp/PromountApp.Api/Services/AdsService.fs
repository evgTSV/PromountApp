namespace PromountApp.Api.Services

open System
open PromountApp.Api.Models
open PromountApp.Api.Utils
open Microsoft.EntityFrameworkCore

type IAdsService =
    abstract member GetBestMatchAd: Guid -> Async<ServiceResponse<Ad>>
    abstract member ClickOnAd: Guid -> Guid -> Async<ServiceResponse<unit>>