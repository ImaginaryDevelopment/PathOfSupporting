namespace PathOfSupporting.Parsing.Items // from PoE calc item resistances
open System
open System.Text.RegularExpressions

open PathOfSupporting.Internal.BReusable
open PathOfSupporting.Internal.BReusable.StringPatterns
open PathOfSupporting.Internal.Helpers
open PathOfSupporting.Internal.Helpers.StringPatterns
type ModRange = {Range:decimal; Id:int}
// TODO: make ModRanges typed
type ParsedItem = {ItemId:int option; Rarity:string; Name:string; Type:string; Variants: string list; Quality:int option; Sockets:string; LevelReq:int option; Implicits: int option; Mods:string list; ModRanges: string list; Variant:int option; Raw:string}
    with
        static member private foldText (i:int,x:ParsedItem) =
            function
            | ""  | null ->
                x
            | After "Rarity:" (Trim r) ->
                {x with Rarity = r}
            | After "Quality: " (ParseInt q) ->
                {x with Quality= Some q}
            | StartsWith "{range:" as r ->
                {x with Mods = r::x.Mods}
            | StartsWith "<ModRange" as r -> {x with ModRanges = r::x.ModRanges }
            | name when i < 2 && String.IsNullOrEmpty x.Name ->
                {x with Name=name}
            | type' when i < 3 && not <| String.IsNullOrEmpty x.Name && String.IsNullOrEmpty x.Type ->
                {x with Type=type'}
            | _ -> x
            >> fun x -> (i+1,x)

            
        static member fromText : string -> ParsedItem option =
            function
            | "" | null -> None
            | x ->
                let result = 
                    let lines =
                        x.Split(Array.ofList ["\r\n";"\n";"\r"],System.StringSplitOptions.RemoveEmptyEntries)
                        |> Seq.map String.trim
                    let a = (0,{Raw=x;ItemId=None;Rarity=null;Name=null; Type=null;Variants=List.empty; Quality=None; Sockets=null; LevelReq=None;Implicits=None;Mods=List.empty;ModRanges=List.empty;Variant=None})
                    (a,lines)
                    ||> Seq.fold ParsedItem.foldText
                Some result
            >> Option.map snd

module Resistances =
    let (|Elemental|_|) =
        function
        |"Cold"
        |"Lightning"
        |"Fire" as x -> Some x
        | _ -> None

    let expandCaps (amount:int) caps =
        caps
        |> List.unfold(
            function
            | [] -> None
            | EqualsI "all Elemental"::tl ->
                let item = (amount,"Fire")
                let tl = ["Cold";"Lightning"]@tl
                (item,tl) |> Some
            | x::tl -> ((amount,x),tl) |> Some
        )

    let foldElemental=
        List.fold(fun elTotal (t,x) ->
                match t with
                | Elemental _ -> elTotal + x
                | _ -> elTotal
        ) 0

    let foldTotal items =
        let x =
            items
            |> Seq.map snd
            |> Seq.sum
        let elem = foldElemental items
        items@["Elemental",elem;"Total",x]

    // typically fed an item copied from within PoE fetched via
    // let getText () = System.Windows.Forms.Clipboard.GetText()
    // also see https://stackoverflow.com/questions/44205260/net-core-copy-to-clipboard for clipboard issues on .net core

    type ItemValue ={Resistance:string;Value:int}

    // get resistance totals and summary total
    let getValues =
        function
        | RMatches @"([+-]\d+)% to (?:(all Elemental|Fire|Cold|Chaos|Lightning)(?: and )?)+ Resistances?" r ->
            r
            |> Seq.cast<Match>
            |> Seq.map (fun m -> (m.Groups.[1].Value |> int), m.Groups.[2].Captures |> Seq.cast<Capture> |> Seq.map(fun c -> c.Value) |> List.ofSeq)
            |> Seq.collect(fun (amount,caps) -> expandCaps amount caps)
            |> Seq.groupBy snd
            |> Seq.map(fun (t,x) -> t, x |> Seq.map fst |> Seq.sum)
            |> List.ofSeq
            |> List.sortBy (fst >> (=) "Chaos")
            |> foldTotal
            |> List.map(fun (x,y) -> {Resistance=x;Value=y})
            |> Ok
        | txt -> errMsg <| sprintf "Could not match against %s" txt




