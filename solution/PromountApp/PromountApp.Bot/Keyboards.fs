module PromountApp.Bot.Keyboards

open System
open Funogram.Telegram
open Funogram.Telegram.Types
open PromountApp.Bot.Localization.Translator
open PromountApp.Bot.Utils

let backOutput = "/back"
let backKeyboard (lang: LangProvider.Root) =
    let keyboard = [|
        [|
            InlineKeyboardButton.Create(lang.BackButton, callbackData = backOutput)
        |]
    |]
    let markup = Markup.InlineKeyboardMarkup { InlineKeyboard = keyboard }
    markup

let setLangOutput = "/set-lang "
let selectLangKeyboard=
    let keyboard =
        availableLanguages()
        |> Seq.toArray
        |> Array.map (fun l ->
            InlineKeyboardButton.Create(l.FlagEmoji + " " + l.LangName, callbackData = setLangOutput + l.LangName))
        |> to2DArray 3
    let markup = Markup.InlineKeyboardMarkup { InlineKeyboard = keyboard }
    markup
    
let signInOutput = "/auth login"
let signUpOutput = "/auth register"
let authKeyboard (lang: LangProvider.Root) =
    let keyboard = [|
        [|
            InlineKeyboardButton.Create(lang.LoginButtonText, callbackData = signInOutput)
            InlineKeyboardButton.Create(lang.RegisterButtonText, callbackData = signUpOutput)
        |]
    |]
    let markup = Markup.InlineKeyboardMarkup { InlineKeyboard = keyboard }
    markup
    
let createCampaignOutput = "/advertiser create-campaign"
let commonStatsOutput = "/advertiser stats"
let campaignsListOutput = "/advertiser campaigns"
let advertiserExitOutput = "/advertiser login"
let advertiserHomeKeyboard (lang: LangProvider.Root) =
    let keyboard = [|
        [|
            InlineKeyboardButton.Create(lang.CreateCampaignButtonText, callbackData = createCampaignOutput)
            InlineKeyboardButton.Create(lang.GetCommonStatsButton, callbackData = commonStatsOutput)
            InlineKeyboardButton.Create(lang.GetCampaignListButton, callbackData = campaignsListOutput)
        |]
        [|
            InlineKeyboardButton.Create(lang.ExitFromAccountButtonText, callbackData = advertiserExitOutput)
        |]
    |]
    let markup = Markup.InlineKeyboardMarkup { InlineKeyboard = keyboard }
    markup