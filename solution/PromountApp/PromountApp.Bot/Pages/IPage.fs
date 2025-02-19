namespace PromountApp.Bot.Pages

open Funogram.Telegram.Bot
open Funogram.Telegram.Types
open PromountApp.Bot.State

type IPage =
    abstract member OnMassageHandler: UpdateContext -> Command -> Async<IPage>