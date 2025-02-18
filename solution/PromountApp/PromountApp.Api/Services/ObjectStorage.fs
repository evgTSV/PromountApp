namespace PromountApp.Api.Services

open System
open System.IO
open AspNetCore.Yandex.ObjectStorage
open Microsoft.AspNetCore.Http
open PromountApp.Api.Utils

type IImageStorage =
    abstract member GetImage: Guid -> Async<ServiceResponse<byte array>>
    abstract member PutImage: Guid -> IFormFile -> Async<ServiceResponse<unit>>
    abstract member DeleteImage: Guid -> Async<ServiceResponse<unit>>

type AdImageStorage(storage: IYandexStorageService) =
    let imageSizeThreshold = 1024L * 1024L // 1 Mib
    let imagesFolder = "ad_images"
    let getPath objKey = Path.Combine(imagesFolder, objKey.ToString())
    
    let acceptedImageTypes = [| "image/jpeg"; "image/png"; "image/gif" |]
    let acceptedExtensions = [| ".jpg"; ".jpeg"; ".png"; ".gif" |]

    let isImage (file: IFormFile) =
        if file <> null && file.Length > 0L then
            let fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant()
            Array.contains file.ContentType acceptedImageTypes && Array.contains fileExtension acceptedExtensions
        else
            false
    
    interface IImageStorage with
        member this.GetImage objKey = async {
            let! response = storage.ObjectService.GetAsync(getPath objKey) |> Async.AwaitTask
            if response.IsSuccessStatusCode then
                let! image = response.ReadAsByteArrayAsync() |> Async.AwaitTask
                return Success image.Value
            else
                return NotFound
        }
        
        member this.PutImage objKey image = async {           
            try
                if isImage image |> not then
                    ArgumentException($"Файл не является изображением; Доступные форматы: {String.Join(';', acceptedExtensions)}") |> raise
                use image = image.OpenReadStream()
                let size = image.Length
                if size > imageSizeThreshold then
                    ArgumentOutOfRangeException(nameof(image), "Изображение слишком большое, максимальный размер 1 MB") |> raise
                let! response = storage.ObjectService.PutAsync(image, getPath objKey) |> Async.AwaitTask
                if response.IsSuccessStatusCode then
                    return Success()
                else
                    return failwith "Internal S3 Error"
            with
            | :? ArgumentException as ex ->
                return InvalidFormat ex.Message
            | _ ->
                return InvalidFormat "Invalid img stream"
        }
        
        member this.DeleteImage objKey = async {
            let! response = storage.ObjectService.DeleteAsync(getPath objKey) |> Async.AwaitTask
            if response.IsSuccessStatusCode then
                return Success ()
            else
                return NotFound
        }
