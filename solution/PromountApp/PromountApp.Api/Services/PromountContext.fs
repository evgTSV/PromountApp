namespace PromountApp.Api.Services

open Microsoft.EntityFrameworkCore
open PromountApp.Api.Models

type PromountContext(options: DbContextOptions<PromountContext>) =
    inherit DbContext(options)
    
    [<DefaultValue>]
    val mutable private clients: DbSet<Client>
    member this.Clients with get() = this.clients and set v = this.clients <- v
    
    [<DefaultValue>]
    val mutable private advertisers: DbSet<Advertiser>
    member this.Advertisers with get() = this.advertisers and set v = this.advertisers <- v
        
    override this.OnModelCreating builder =
        ()
        
    override this.OnConfiguring(options: DbContextOptionsBuilder) : unit =
        options.UseNpgsql(fun builder ->
            builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
            |> ignore)
        |> ignore