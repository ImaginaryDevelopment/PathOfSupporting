module PathOfSupporting.HtmlParsing
open System.Net.Http
open System.Collections.Generic
open PathOfSupporting.Internal.Helpers

type Character = {Name:string;League:string; Class:string;Level:int}
[<RequireQualifiedAccess>]
[<NoComparison>]
type GetResult =
    |Success of Character[]
    |FailedDeserialize of PoSError
    |FailedHttp of string
let getCharacters accountName =
    async{
    
        use client = new HttpClient()
        use hc = new FormUrlEncodedContent(
                                [
                                    KeyValuePair<_,_>("accountName", accountName)
                                ] )
        let! resp = Async.AwaitTask <| client.PostAsync("https://www.pathofexile.com/character-window/get-characters", hc)
        if resp.IsSuccessStatusCode then
            let! raw = Async.AwaitTask <| resp.Content.ReadAsStringAsync()
            let chars:Character[] PoSResult = SuperSerial.deserialize raw
            match chars with
            | Ok chars ->
                return GetResult.Success chars
            | Error x ->
                return GetResult.FailedDeserialize x
        else return GetResult.FailedHttp <| sprintf "Fail:%A" resp.StatusCode
    }
