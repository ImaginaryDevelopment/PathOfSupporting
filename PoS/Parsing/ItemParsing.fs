module PathOfSupporting.ItemParsing // from PoE calc item resistances
open System.Text.RegularExpressions
open PathOfSupporting.Internal.Helpers
open PathOfSupporting.Internal.Helpers.StringPatterns

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
        | txt -> errMsg "Could not match"




