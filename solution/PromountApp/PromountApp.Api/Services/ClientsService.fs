namespace PromountApp.Api.Services

open System
open FSharp.Core
open FSharp.Collections
open EFCore.BulkExtensions
open Microsoft.EntityFrameworkCore
open PromountApp.Api.Models
open PromountApp.Api.Utils

type IClientsService =
    abstract member GetClient: Guid -> Async<ServiceResponse<Client>>
    abstract member BulkInsertion: Client seq -> Async<ServiceResponse<unit>>
    
type ClientsService(dbContext: PromountContext) =
    interface IClientsService with
        member this.GetClient(id) = async {
            try
                let! client = dbContext.Clients.SingleAsync(fun c -> c.client_id = id) |> Async.AwaitTask
                return Success client
            with
            | :? AggregateException as agg ->
                return filterAggregate (fun _ -> NotFound)
                           agg [| InvalidOperationException() |]
        }
            
        member this.BulkInsertion(clients) = async {
            let clients = clients |> Seq.toList
            let clientsWithoutDuplicates = clients |> List.distinctBy _.client_id
            
            if clients.Length = clientsWithoutDuplicates.Length then
                let toUpdate, toInsert =
                    clients |> List.partition (fun c1 ->
                        dbContext.Clients.AnyAsync(fun c2 -> c2.client_id = c1.client_id).Result)
                do! dbContext.BulkInsertAsync(toInsert) |> Async.AwaitTask
                do! dbContext.BulkUpdateAsync(toUpdate) |> Async.AwaitTask
                let! _ = dbContext.SaveChangesAsync() |> Async.AwaitTask
                return Success()
            else
                return Conflict
        }