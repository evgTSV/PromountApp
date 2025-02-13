namespace PromountApp.Api

open System
open System.Globalization
open Microsoft.Extensions.Caching.Distributed

type TimeConfig(cache: IDistributedCache) =
    let cacheTimeKey = "app:time"
    
    member this.CurrentTime
        with get() = DateTime.ParseExact(cache.GetString(cacheTimeKey), "yyyy-MM-dd", CultureInfo.InvariantCulture)
        and set (v : DateTime) =
            let dateStr = v.ToString("yyyy-MM-dd")
            cache.SetString(cacheTimeKey, dateStr)