module PromountApp.Api.Tests.Helpers

open System
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Mvc.Testing
open PromountApp.Api
open PromountApp.Api.Services
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.DependencyInjection


let testDbName = "TestDB"

let configureInMemoryDb(services: IServiceCollection) =
    services.AddDbContext<PromountContext>(fun opt ->
        opt.UseInMemoryDatabase(testDbName) |> ignore)
    |> ignore
    
let getDbContext (provider: IServiceProvider) =
    provider.GetRequiredService<PromountContext>()