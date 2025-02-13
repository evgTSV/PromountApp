module PromountApp.Api.Models

open System
open System.ComponentModel.DataAnnotations

type Gender = MALE = 0 | FEMALE = 1

[<CLIMutable>]
type Client = {
    [<Key>]
    client_id: Guid
    login: string
    age: int
    location: string
    gender: string
}

[<CLIMutable>]
type Advertiser = {
    [<Key>]
    advertiser_id: Guid
    name: string
}