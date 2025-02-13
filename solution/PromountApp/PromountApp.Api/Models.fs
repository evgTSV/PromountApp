module PromountApp.Api.Models

open System
open System.ComponentModel.DataAnnotations
open FSharp.Data.Validator

type Gender = MALE = 0 | FEMALE = 1

[<CLIMutable>]
type Client = {
    [<Key>]
    client_id: Guid
    login: string
    age: int
    location: string
    gender: string
} with
    interface IValidatable with
        member this.Validate() =
            this.login |> requiresTextLength (1, 200)
            && this.age |> inRange (0, 100)
            && this.location |> requiresTextLength (1, 1000)
            && this.gender |> isEnumCase typeof<Gender>

[<CLIMutable>]
type Advertiser = {
    [<Key>]
    advertiser_id: Guid
    name: string
} with
    interface IValidatable with
        member this.Validate() =
            this.name |> requiresTextLength (1, 200)