namespace PromountApp.Api

open System
open System.Globalization
open Microsoft.Extensions.Caching.Distributed

type TimeConfig(cache: IDistributedCache) =
    let cacheTimeKey = "app:time"
    
    member this.CurrentTime
        with get() = TimeSpan.FromDays(cache.GetString(cacheTimeKey) |> int)
        and set (v : TimeSpan) =
            let dateStr = v.TotalDays.ToString()
            cache.SetString(cacheTimeKey, dateStr)
            
    member this.GetTotalDays() = this.CurrentTime.TotalDays |> int