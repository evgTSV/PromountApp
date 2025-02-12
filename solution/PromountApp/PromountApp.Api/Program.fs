namespace PromountApp.Api
#nowarn "20"
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Giraffe
open Giraffe.EndpointRouting
open Npgsql
open OpenTelemetry.Metrics

module Program =   
    let endpoints =
        [
            GET [
                route "/ping" (text "pong")
            ]
        ]
        
    let configureOTel (services:IServiceCollection) =
        let histogram = ExplicitBucketHistogramConfiguration()
        histogram.Boundaries <- [| 0; 0.005; 0.01; 0.025; 0.05; 0.075; 0.1; 0.25; 0.5; 0.75; 1; 2.5; 5; 7.5; 10 |]
        services.AddOpenTelemetry()
            .WithMetrics(fun builder ->
                builder
                    .AddPrometheusExporter()
                    .AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddNpgsqlInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter(
                           "Microsoft.AspNetCore.Hosting",
                           "Microsoft.AspNetCore.Server.Kestrel",
                           "Microsoft.AspNetCore.Http.Connections",
                           "Microsoft.AspNetCore.Routing",
                           "Microsoft.AspNetCore.Diagnostics",
                           "Microsoft.AspNetCore.RateLimiting"
                           )
                    .AddView("request-duration", histogram)
                |> ignore)
    
    let configureServices (services:IServiceCollection) =
        configureOTel services
        services
            .AddRouting()
            .AddGiraffe()
            .AddEndpointsApiExplorer()
            .AddSwaggerGen()
        |> ignore
        
    let configureApp (appBuilder : IApplicationBuilder) =
        appBuilder
            .UseRouting()
            .UseOpenTelemetryPrometheusScrapingEndpoint()
            .UseSwagger()
            .UseSwaggerUI()
            .UseGiraffe(endpoints)
        |> ignore

    [<EntryPoint>]
    let main _ =
        Host.CreateDefaultBuilder()
            .ConfigureWebHostDefaults(
                fun webHostBuilder ->
                    webHostBuilder
                        .Configure(configureApp)
                        .ConfigureServices(configureServices)
                    |> ignore)
            .Build()
            .Run()
        0