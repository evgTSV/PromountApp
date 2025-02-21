namespace PromountApp.Api
#nowarn "20"
open System
open System.Text.Json
open System.Text.Json.Serialization
open AspNetCore.Yandex.ObjectStorage
open AspNetCore.Yandex.ObjectStorage.Extensions
open FluentMigrator.Runner
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http.Json
open Microsoft.AspNetCore.Mvc
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Npgsql
open OpenTelemetry.Metrics
open PromountApp.Api.Services
open PromountApp.Api.Utils
open PromountApp.Api.Migrations
open PromountApp.Api.Validation
open Serilog
open Serilog.Sinks.Grafana.Loki

module Program =
    let configureDB (services: IServiceCollection) =
        let connectionStr = getEnv "DB_CONNECTION"
        services.AddDbContext<PromountContext>(fun builder ->
            builder.UseNpgsql(connectionStr)
            |> ignore)
        
    let configureMigration (services: IServiceCollection) =
        let connectionStr = getEnv "DB_CONNECTION"
        services
            .AddFluentMigratorCore()
            .ConfigureRunner(fun rb ->
                rb.AddPostgres()
                  .WithGlobalConnectionString(connectionStr)
                  .ScanIn(typedefof<AddClientsTable>.Assembly).For.Migrations() |> ignore)
            .AddLogging(fun lb -> lb.AddFluentMigratorConsole() |> ignore)
            .BuildServiceProvider(false)
            
    let updateDatabase (serviceProvider: IServiceProvider) =
        let runner = serviceProvider.GetRequiredService<IMigrationRunner>()
        runner.MigrateUp()
        
    let configureRedis (services: IServiceCollection)=
        services
            .AddStackExchangeRedisCache(fun options ->
                options.Configuration <- "redis:6379"
            ) |> ignore
    
    let configureOTel (services: IServiceCollection) =
        let histogram = ExplicitBucketHistogramConfiguration()
        histogram.Boundaries <- [| 0; 0.01; 0.1; 1; 10 |]
        services.AddOpenTelemetry()
            .WithMetrics(fun builder ->
                builder
                    .AddPrometheusExporter()
                    .AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddProcessInstrumentation()
                    .AddMeter(
                           "Microsoft.AspNetCore.Hosting",
                           "Microsoft.AspNetCore.Server.Kestrel",
                           "Microsoft.AspNetCore.Http.Connections",
                           "Microsoft.AspNetCore.Routing")
                    .AddView("request-duration", histogram)
                |> ignore)
            
    let configureLogger (services: IServiceCollection) =
        Log.Logger <- LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            // .WriteTo.GrafanaLoki("http://loki:3100")
            .WriteTo.Console()
            .CreateLogger()
        services.AddLogging(fun builder -> builder.AddSerilog(dispose = true) |> ignore) |> ignore
            
    let configureJsonOption=
        Action<JsonOptions>(fun options ->
        let opts = options.JsonSerializerOptions
        opts.PropertyNameCaseInsensitive <- true
        opts.Encoder <- System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        opts.PropertyNamingPolicy <- JsonNamingPolicy.SnakeCaseLower
        opts.DefaultIgnoreCondition <- JsonIgnoreCondition.WhenWritingNull
        opts.WriteIndented <- true
        opts.NumberHandling <- JsonNumberHandling.AllowNamedFloatingPointLiterals)

    let configureS3ObjectStorage (services: IServiceCollection) =
        services.AddYandexObjectStorage(fun options ->
            options.BucketName <- getEnv "S3_BUCKET_NAME"
            options.Location <- getEnvOr "S3_LOCATION" "ru-central1-a"
            options.AccessKey <- getEnv "S3_ACCESS_KEY"
            options.SecretKey <- getEnv "S3_SECRET_KEY")
        services.AddSingleton<IYandexStorageService, YandexStorageService>()
    
    let configureServiceLocator (services: IServiceCollection) =
        ServiceLocator.SetProvider(
            services
                .BuildServiceProvider())
    
    let configureServices (services: IServiceCollection) =
        configureDB services
        updateDatabase ((configureMigration services)
                            .CreateScope().ServiceProvider)
        configureRedis services
        configureOTel services
        configureLogger services
        configureS3ObjectStorage services
        services
            .AddSingleton<TimeConfig>()
            .AddScoped<IClientsService, ClientsService>()
            .AddScoped<IAdvertisersService, AdvertisersService>()
            .AddScoped<ICampaignsService, CampaignsService>()
            .AddScoped<IStatisticsService, StatisticsService>()
            .AddScoped<IAdsService, AdsService>()
            .AddScoped<IImageStorage, AdImageStorage>()
            .AddScoped<IBanListService, BanListService>()
            .AddRouting()
            .AddEndpointsApiExplorer()
            .AddSwaggerGen()
            .AddControllers(fun config ->
                config.Filters.Add<ValidateModelFilter>() |> ignore
            )
            .AddJsonOptions(configureJsonOption)
        |> ignore        
        configureServiceLocator services
        
    let configureApp (appBuilder: IApplicationBuilder) =
        appBuilder
            .UseRouting()
            .UseEndpoints(fun endpoints ->
                endpoints.MapControllers() |> ignore)
            .UseOpenTelemetryPrometheusScrapingEndpoint()
            .UseSwagger()
            .UseSwaggerUI()
        |> ignore

    [<EntryPoint>]
    let main _ =
        Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureWebHostDefaults(
                fun webHostBuilder ->
                    webHostBuilder
                        .Configure(configureApp)
                        .ConfigureServices(configureServices)
                    |> ignore)
            .Build()
            .Run()
        0