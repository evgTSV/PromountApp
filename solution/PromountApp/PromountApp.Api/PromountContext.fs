namespace PromountApp.Api.Services

open Microsoft.EntityFrameworkCore
open PromountApp.Api.Models
open Newtonsoft.Json
open Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure
open Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal
open PromountApp.Api.ModerationModels

type PromountContext(options: DbContextOptions<PromountContext>) =
    inherit DbContext(options)
    
    [<DefaultValue>]
    val mutable private clients: DbSet<Client>
    member this.Clients with get() = this.clients and set v = this.clients <- v
    
    [<DefaultValue>]
    val mutable private advertisers: DbSet<Advertiser>
    member this.Advertisers with get() = this.advertisers and set v = this.advertisers <- v
    
    [<DefaultValue>]
    val mutable private mlScores: DbSet<MLScore>
    member this.MLScores with get() = this.mlScores and set v = this.mlScores <- v
    
    [<DefaultValue>]
    val mutable private campaigns: DbSet<CampaignDb>
    member this.Campaigns with get() = this.campaigns and set v = this.campaigns <- v
    
    [<DefaultValue>]
    val mutable private impressionLogs: DbSet<ImpressionLog>
    member this.ImpressionLogs with get() = this.impressionLogs and set v = this.impressionLogs <- v
    
    [<DefaultValue>]
    val mutable private clickLogs: DbSet<ClickLog>
    member this.ClickLogs with get() = this.clickLogs and set v = this.clickLogs <- v
    
    [<DefaultValue>]
    val mutable private banLogs: DbSet<BanLog>
    member this.BanLogs with get() = this.banLogs and set v = this.banLogs <- v
        
    override this.OnModelCreating builder =
        builder
            .Entity<MLScore>()
            .HasKey("client_id", "advertiser_id") |> ignore
        builder
            .Entity<BanLog>()
            .Property(_.probability)
            .HasConversion(
                JsonConvert.SerializeObject,
                JsonConvert.DeserializeObject<AnalyzeResponse>) |> ignore
        base.OnModelCreating(builder)
        
    override this.OnConfiguring(options: DbContextOptionsBuilder) : unit =
        options.UseNpgsql(fun builder ->
            builder
                .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
            |> ignore)
        |> ignore