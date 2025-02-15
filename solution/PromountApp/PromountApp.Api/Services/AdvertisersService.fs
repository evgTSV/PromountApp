namespace PromountApp.Api.Services

open System
open FSharp.Core
open FSharp.Collections
open EFCore.BulkExtensions
open Microsoft.EntityFrameworkCore
open PromountApp.Api.Models
open PromountApp.Api.Utils

type IAdvertisersService =
    abstract member GetAdvertiser: Guid -> Async<ServiceResponse<Advertiser>>
    abstract member BulkInsertion: Advertiser seq -> Async<ServiceResponse<unit>>
    abstract member SetMLScore: MLScore -> Async<ServiceResponse<unit>>
    
type AdvertisersService(dbContext: PromountContext) =
    interface IAdvertisersService with
        member this.GetAdvertiser(id) = async {
            try
                let! advertiser = dbContext.Advertisers.SingleAsync(fun a -> a.advertiser_id = id) |> Async.AwaitTask
                return Success advertiser
            with
            | :? AggregateException as agg ->
                return filterAggregate (fun _ -> NotFound)
                           agg [| InvalidOperationException() |]
        }
            
        member this.BulkInsertion(advertisers) = async {
            let advertisers = advertisers |> Seq.toList
            let advertisersWithoutDuplicates = advertisers |> List.distinctBy _.advertiser_id
            
            if advertisers.Length = advertisersWithoutDuplicates.Length then
                let toUpdate, toInsert =
                    advertisers |> List.partition (fun a1 ->
                        dbContext.Advertisers.AnyAsync(fun a2 -> a2.advertiser_id = a1.advertiser_id).Result)
                do! dbContext.BulkInsertAsync(toInsert) |> Async.AwaitTask
                do! dbContext.BulkUpdateAsync(toUpdate) |> Async.AwaitTask
                let! _ = dbContext.SaveChangesAsync() |> Async.AwaitTask
                return Success()
            else
                return Conflict
        }
        
        member this.SetMLScore(mlScore: MLScore) = async {
            let! isExisting = 
                dbContext.MLScores
                    .AnyAsync(fun m -> m.client_id = mlScore.client_id && m.advertiser_id = mlScore.advertiser_id)
                    |> Async.AwaitTask
            
            if isExisting then
                dbContext.Update(mlScore) |> ignore
                let! _ = dbContext.SaveChangesAsync() |> Async.AwaitTask
                return Success()
            else
                let! _ = dbContext.AddAsync(mlScore).AsTask() |> Async.AwaitTask
                let! _ = dbContext.SaveChangesAsync() |> Async.AwaitTask
                return Success()
        }