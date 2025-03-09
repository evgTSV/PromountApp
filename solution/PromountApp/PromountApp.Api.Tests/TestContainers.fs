namespace PromountApp.Api.Tests

open System
open System.Collections.Generic
open System.IO
open System.Net
open System.Net.Http
open System.Text
open System.Threading.Tasks
open DotNet.Testcontainers.Builders
open DotNet.Testcontainers.Containers
open PromountApp.Api.Models
open PromountApp.Api.Tests.Helpers
open Testcontainers.PostgreSql
open Testcontainers.Redis
open Xunit

type PromountTestContainers() =
    let solution = CommonDirectoryPath.GetSolutionDirectory()
    let dataBase = "promountDB"
    
    let dbAlias = "db"
    let dbConnectionStr = $"Server={dbAlias};Database={dataBase};Port=5432;User Id=postgres;Password=postgres;"
    let postgresImage = "postgres:latest"
    
    let redisAlias = "redis"
    let redisUrl = "redis:6379"
    let redisImage = "redis:latest"
    
    let mutable httpClient: HttpClient = null
    let mutable uri: Uri = null
    
    let buildLogger = StringLogger()
    
    let envs =
        let lines = File.ReadAllLines "/.env"
        let dict = Dictionary<string, string>()
        lines
        |> Array.iter (fun x ->
            match x.Split('=') with
            | [| name; value |] ->
                dict[name] <- value
            | _ -> ())
        dict
        
    let appImage =
        ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(solution, "PromountApp.Api")
            .WithDockerfile("./Dockerfile")
            .WithName("promount-webapi-test")
            .WithBuildArgument("RESOURCE_REAPER_SESSION_ID", ResourceReaper.DefaultSessionId.ToString("D"))
            .WithCleanUp(false)
            .WithDeleteIfExists(true)
            .WithLogger(buildLogger)
            .Build()
            
    let testNet =
        NetworkBuilder()
            .Build()
            
    let dbContainer =
        PostgreSqlBuilder()
            .WithImage(postgresImage)
            .WithNetwork(testNet)
            .WithNetworkAliases(dbAlias)
            .WithEnvironment("POSTGRES_DB", dataBase)
            .WithEnvironment("POSTGRES_USERNAME", "postgres")
            .WithEnvironment("POSTGRES_PASSWORD", "postgres")
            .Build()
            
    let redisContainer =
        RedisBuilder()
            .WithImage(redisImage)
            .WithNetwork(testNet)
            .WithNetworkAliases(redisAlias)
            .WithEnvironment("REDIS_URL", redisUrl)
            .Build()
            
    let appContainer =
        ContainerBuilder()
            .WithName("web")
            .WithImage(appImage)
            .WithNetwork(testNet)
            .WithPortBinding(8080, true)
            .WithEnvironment(envs)
            .DependsOn(dbContainer)
            .DependsOn(redisContainer)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8080))
            .Build()
            
    let runCompose() = async {
        try
            let appTask = appImage.CreateAsync() |> Async.AwaitTask
            let dbTask = dbContainer.StartAsync() |> Async.AwaitTask
            let redisTask = redisContainer.StartAsync() |> Async.AwaitTask
            
            let! _ = [|appTask; dbTask; redisTask|] |> Async.Parallel
            ()
        with
        | ex ->
            let logs = buildLogger.ExtractMessages()
            let logs = if String.IsNullOrWhiteSpace(logs) then "empty" else logs
            let message = $"Container composing failure: {logs}\nException<{ex.GetType().ToString()}>: {ex.Message}"
            failwith message
    }
    
    interface IAsyncLifetime with
        member this.DisposeAsync() = task {
            do! appContainer.DisposeAsync()
            do! dbContainer.DisposeAsync()
        }
        
        member this.InitializeAsync() = task {
            try
                do! runCompose()
                do! appContainer.StartAsync()
                
                httpClient <- new HttpClient()
                uri <- Uri($"http://{appContainer.Hostname}:{appContainer.GetMappedPublicPort(8080)}")
                httpClient.BaseAddress <- uri
            finally
                if appContainer.State <> TestcontainersStates.Undefined then
                    let struct (_stdout, err) = appContainer.GetLogsAsync().Result
                    if err <> "" then
                        failwith $"Error: {err}; StdOut: {_stdout}"
        }
        
    member this.HttpClient = httpClient
    member this.Uri = uri
    
    member this.Get<'a> (url: string) : Task<Response<'a>> = task {
        try
            let! response = this.HttpClient.GetAsync(url)
            
            if response.IsSuccessStatusCode then
                let! object = deserializeJson (response.Content.ReadAsStream())
                return Success (object.Value, response.StatusCode)
            else
                return Error response.StatusCode
        with
        | _ -> return Error HttpStatusCode.InternalServerError
    }
    
    member this.Post<'a, 'b> (url: string) (data: 'a) : Task<Response<'b>> = task {
        try
            let jsonContent = serializeJson data
            let content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            let! response = this.HttpClient.PostAsync(url, content)
            
            if response.IsSuccessStatusCode then
                let! object = deserializeJson (response.Content.ReadAsStream())
                return Success (object.Value, response.StatusCode)
            else
                return Error response.StatusCode
        with
        | ex -> return Error HttpStatusCode.InternalServerError
    }
    
    member this.Put<'a, 'b> (url: string) (data: 'a) : Task<Response<'b>> = task {
        try
            let jsonContent = serializeJson data
            let content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            let! response = this.HttpClient.PutAsync(url, content)
            
            if response.IsSuccessStatusCode then
                let! object = deserializeJson (response.Content.ReadAsStream())
                return Success (object.Value, response.StatusCode)
            else
                return Error response.StatusCode
        with
        | _ -> return Error HttpStatusCode.InternalServerError
    }
    
    member this.Delete<'a> (url: string) : Task<Response<'a>> = task {
        try
            let! response = this.HttpClient.DeleteAsync(url)
            
            if response.IsSuccessStatusCode then
                let! object = deserializeJson (response.Content.ReadAsStream())
                return Success (object.Value, response.StatusCode)
            else
                return Error response.StatusCode
        with
        | _ -> return Error HttpStatusCode.InternalServerError
    }
    
    member this.TimeZero() = this.Post "time/advance" {| current_date = 0 |}
    member this.TimeAdvance (time: int) = task {
        return! this.Post "time/advance" {| current_date = time |}
    }
    
    member this.GetTime() = task {
        let! time = this.Get<Day> "time/current-time"
        match time with
        | Success (day, _) -> return day.current_date
        | _ -> return failwith "Time service error"
    }