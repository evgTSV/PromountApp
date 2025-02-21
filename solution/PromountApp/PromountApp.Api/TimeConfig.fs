namespace PromountApp.Api

open System
open System.Globalization
open Microsoft.Extensions.Caching.Distributed

type TimeConfig(cache: IDistributedCache) =
    let cacheTimeKey = "app:time"
    
    member this.CurrentTime
        with get() =
            let days = cache.GetString(cacheTimeKey) |> defaultIfNull "0" |> int
            TimeSpan.FromDays(days)
        and set (v : TimeSpan) =
            let dateStr = v.TotalDays.ToString()
            cache.SetString(cacheTimeKey, dateStr)
            
    member this.GetTotalDays() = this.CurrentTime.TotalDays |> int