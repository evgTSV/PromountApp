module PromountApp.Bot.Keyboards

open System
open Funogram.Telegram
open Funogram.Telegram.Types
open PromountApp.Bot.Localization.Translator
open PromountApp.Bot.Utils

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