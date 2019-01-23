namespace PathOfSupporting.NinjaAPI

open PathOfSupporting.Internal.Helpers

type FetchArguments = |TargetUrlOverrideOpt of string | League of string


[<NoComparison>]
type SparkLine ={TotalChange:decimal;Data:float System.Nullable list}
type ValueEntry = {id:int;League_id:int;Pay_Currency_Id:int;Get_Currency_Id:int;Count:int;Value:decimal;}
[<NoComparison>]
type Line = {CurrencyTypeName:string;ChaosEquivalent:decimal;Receive:ValueEntry;ReceiveSparkLine:SparkLine;DetailsId:string;Pay:ValueEntry;PaySparkLine:SparkLine;LowConfidencePaySparkLine:SparkLine}
type CurrencyDetail = {Id:int;Icon:string;Name:string;PoeTradeId:int}
[<NoComparison>]
type NinjaResponse = {Lines:Line list;CurrencyDetails:CurrencyDetail list}


module Impl =
    let getTargeting =
        function
        | TargetUrlOverrideOpt x -> x
        | League l -> sprintf "https://poe.ninja/api/Data/GetCurrencyOverview?league=%s" l

    let fetchCurrency t=
            Api.fetch t
            |> Async.Catch
            |> Async.map(function
                |Choice1Of2 x -> x
                |Choice2Of2 ex -> Result.ExDI (sprintf "Exception fetching %s" t) ex
            )
            |> Async.map(Result.bind(fun raw ->
                    match SuperSerial.deserialize<NinjaResponse> raw with
                    | Ok nr -> Ok (nr,raw)
                    | Error e -> Result.ErrAdd "raw" raw e |> Error
            ))
module Fetch =
    let fetchCurrency targeting =
        Impl.getTargeting targeting
        |> Impl.fetchCurrency
        |> Async.map Option.ofOk
        |> Async.map (Option.map fst)

