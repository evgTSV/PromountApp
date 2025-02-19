module PromountApp.Bot.Localization.Translator

open System.IO
open FSharp.Data

[<Literal>]
let translationsFolder = "Localization/Translations/"

type LangProvider = JsonProvider<"Localization/Translations/russian.json">

let getTranslation (language: string) =
    LangProvider.Load (Path.Combine(translationsFolder, $"{language}.json"))
    
let private extractName (path: string) =
    Path.GetFileNameWithoutExtension(path)
    
let availableLanguages() =
    let files = Directory.GetFiles(translationsFolder, "*.json")
    seq {
        for file in files do
            let fileName = file |> extractName
            getTranslation fileName
    }
    |> Seq.cache
    
let defaultLang =
    availableLanguages()
    |> Seq.find (fun l -> l.LangName.ToLowerInvariant() = "русский")