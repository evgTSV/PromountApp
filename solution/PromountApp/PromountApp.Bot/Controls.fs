module PromountApp.Bot.State

open Funogram.Telegram.Bot
open Funogram.Telegram.Types
open PromountApp.Bot.Localization.Translator

type Command =
    | Start
    | SelectLang
    | ChangeLang of string
    | NewMessage
    
type UserState = {
    User: User
    Lang: LangProvider.Root
}
    
type BotMailbox = MailboxProcessor<Command * UpdateContext>