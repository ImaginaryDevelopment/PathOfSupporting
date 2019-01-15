module PathOfSupporting.Parsing.Html
open System.Net.Http
open System.Collections.Generic
open PathOfSupporting.Internal.Helpers

type Character = {Name:string;League:string; Class:string;Level:int}
[<RequireQualifiedAccess>]
[<NoComparison>]
module Impl =
    open HtmlAgilityPack

    [<NoComparison>]
    type Wrap = private {node:HtmlNode}
        with
    //            static member Wrap n = {node=n}
            member x.Value:HtmlNode option= Option.ofObj x.node
            static member internal getValue (w:Wrap):HtmlNode option = w.Value
            member x.ToDump() = x.Value |> Option.map(fun x -> x.OuterHtml)
    module Html =
        let (>&>) f1 f2 x = f1 x && f2 x
        open System
        open PathOfSupporting.Internal.BReusable.StringHelpers

        let wrap x = {node=x}
        let wrapOpt = Option.defaultValue {node=null}
        let map f =
            Wrap.getValue
            >> Option.map f
        let mapNode f =
            Wrap.getValue
            >> Option.map (f>>wrap)
            >> Option.defaultValue {node=null}
        let mapNull f =
            Wrap.getValue
            >> Option.map f
            >> Option.defaultValue null
        let bind f =
            Wrap.getValue
            >> Option.bind f
        
        let getAttr name =
            Wrap.getValue
            >> Option.bind(fun w -> w.Attributes |> Seq.tryFind(fun xa -> xa.Name = name))
        let getAttrValue name =
            getAttr name
            >> Option.map(fun x -> x.Value)
        let getAttrValueOrNull name =
            getAttrValue name
            >> Option.defaultValue null
        let getInnerText = Wrap.getValue >> Option.map (fun x -> x.InnerText)
        let getInnerHtml = Wrap.getValue >> Option.map(fun x -> x.InnerHtml)
        let getFirstChild = mapNode (fun x -> x.FirstChild)
        let getOuterHtml = mapNull(fun x -> x.OuterHtml)
        let getParentNode = mapNode(fun x -> x.ParentNode)
        let selectNodes (xpath:string)= bind(fun x -> x.SelectNodes(xpath) |> Option.ofObj |> Option.map (Seq.map wrap)) >> Option.defaultValue Seq.empty
        let selectNode (xpath:string) = mapNode(fun x -> x.SelectSingleNode xpath)
        let getChildNodes = map(fun x -> x.ChildNodes |> Seq.cast<HtmlNode> |> Seq.map wrap) >> Option.defaultValue Seq.empty
        let getNextSibling = map(fun x -> x.NextSibling |> wrap)
        let getNodeType = map(fun x -> x.NodeType)
        let getName = mapNull(fun x -> x.Name)
        let isNodeType nt = map(fun x -> x.NodeType = nt) >> Option.defaultValue false
        let collectHtml x = x |> Seq.map getOuterHtml |> delimit String.Empty
        let hasClass cls = map(fun x -> x.HasClass cls) >> Option.defaultValue false
        let hasId = getAttrValueOrNull"id" >> String.IsNullOrWhiteSpace >> not
        let getFollowingSiblings = 
            Seq.unfold(getNextSibling >>
                    function
                    | None -> None
                    | Some sibling -> Some(sibling,sibling)
                )
        let hasText = map(fun x -> x.InnerText |> String.IsNullOrWhiteSpace |> not) >> Option.defaultValue false
        let hasChildWith f = getChildNodes >> Seq.exists(f)


[<NoComparison>]
type GetResult =
    |Success of Character[]
    |FailedDeserialize of PoSError
    |FailedHttp of string
module PathOfExile =
    module Com =
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

module PoeDb =
    module Tw=
        type ModPageInfo ={cn:string;an:string}
        module Impl =
            open System
            open HtmlAgilityPack
            open Impl.Html

            open PathOfSupporting.Internal.BReusable.StringPatterns
            open PathOfSupporting.Internal.BReusable.StringHelpers



            let makeTarget ({cn=cn;an=an} as targeting):PoSResult<_> =
                match cn, an with
                |ValueString cn, ValueString an -> sprintf "cn=%s&an=%s" cn an |> Some
                |ValueString cn, NonValueString _ -> sprintf "cn=%s" cn |> Some
                | _ -> None
                |> Option.map (sprintf "http://poedb.tw/us/mod.php?%s")
                |> function
                    |Some x -> Ok x
                    | None -> Error (sprintf "Could not determine target from %A" targeting,None)

            let fetch targeting =
                async{
                    match makeTarget targeting with
                    | Error e ->
                        return Error e
                    |Ok t ->
                        let! result = Api.fetch t
                        return result
                }
            let parse x =
                let hd = HtmlDocument()
                hd.LoadHtml x
                hd
            module Munge =
                open Impl.Html
                let mungeDoc (hd:HtmlDocument) =
                    let wrapped =
                        hd.DocumentNode
                        |> wrap
                    let nodes =
                        wrapped
                        |> selectNodes "//div/h4"
                        |> Seq.filter(hasId >> not >&> hasChildWith (isNodeType HtmlNodeType.Text >> not))
                        |> List.ofSeq
                    match nodes with
                    | [] -> Result.ErrMsg <| sprintf "could not find h4 in %A" (getOuterHtml wrapped)
                    | x -> Ok x

                type ItemAffixContainer<'t> = {Item:string;Children:'t list} with
                    static member mapChildren f (x:ItemAffixContainer<_>) = {Item=x.Item;Children=List.map f x.Children}
                    static member chooseChildren f (x:ItemAffixContainer<_>) = {Item=x.Item; Children = List.choose f x.Children}
                // headerNode is (H4 Amulets... but should be Prefix;Suffix;...)
                let mungeModCategory headerNode =
                //    headerNode.Dump("header")
                    let category = getInnerText headerNode |> Option.defaultValue null |> trim
                    printfn "munging %s" category
                    {Item=category;Children=getFollowingSiblings headerNode |> List.ofSeq}
                type AffixContainer<'t> = {EffixType:string;Children:'t list}
                module AffixContainer =
                    let mapChildren f x = {EffixType=x.EffixType;Children=f x.Children}
                    let inline unwrap x = x.EffixType,x.Children
                    let inline wrap (x,y) = {EffixType=x;Children=List.ofSeq y}
                    let mapRaw f x = unwrap x |> f |> wrap

                let mungeAffix titleNode =
                    match titleNode |> selectNodes "h4" |> Seq.tryHead |> Option.bind getInnerText with
                    | None -> None
                    // an affix or suffix
                    | Some effixType ->
                        let subCategories =
                            titleNode
                            |> getChildNodes
                            |> Seq.skipWhile (isNodeType HtmlNodeType.Element>>not)
                            |> Seq.tryTail
                            |> Seq.filter (getInnerText >> Option.defaultValue null >>String.IsNullOrWhiteSpace>>not)
                            |> Seq.pairwise
                            |> Seq.filter(fst>>hasClass "mod-title")
                            |> List.ofSeq
                        // title node has (dummy whitespace text node, x badge nodes, marked up text
                //        (effixType,subCategories).Dump("tryTail")
                         
                        Some {EffixType=trim effixType;Children=subCategories}

                [<NoComparison>]
                type AffixTierContainer<'t> = {Display:string;FossilCategories:string list;Children:'t list}
                let mungeSubCatPair (subCatNode,detailNode) =
                    let subId = subCatNode |> getAttrValueOrNull "id" |> afterOrSelf "accordion"
                    let detailId = detailNode|> getAttrValueOrNull "id" |> afterOrSelf "collapseOne"
                    if String.IsNullOrWhiteSpace subId || subId <> detailId then
                        //(subId,detailId,subCatNode,detailNode).Dump("fail")
                        Result.Error "expected accordion id to equal collapseOne id"
                      else
                        let fossilCategories =
                                subCatNode |> selectNodes(".//*[contains(@class,'badge')]") |> Seq.choose getInnerText |> List.ofSeq
                        let affixName = subCatNode|> getChildNodes |> Seq.skipWhile(hasText >> not) |> Seq.tryTail |> collectHtml
                        let detailTable = detailNode |> selectNode ".//table"
                //    let detailHead = detailTable |> selectNodes ".//thead/tr" |> Seq.tryHead |> wrapOpt
                    // the tbody is coming back as not directly in the table :sad face:
                        let detailBody = detailTable |> selectNodes ".//tbody/tr"
                        {Display=affixName;FossilCategories=fossilCategories;Children= List.ofSeq detailBody}
                        |> Ok

                // repair the elder and shaper things not having a prefix/suffix attached to the names
                let fixUpEffixCategories x =
                        x
                        |> Seq.pairwise
                        |> Seq.collect(fun ((prevTitle,x),(title,y)) ->
                            if prevTitle = title && not <| String.IsNullOrWhiteSpace title then
                                [(sprintf "%s Prefix" prevTitle,x);(sprintf "%s Suffix" title,y)]
                            else [prevTitle,x;title,y]
                        )
                        |> Seq.distinctBy fst
                        |> Seq.filter(fst>>fun x -> containsI "suffix" x || containsI "prefix" x)
                    
                type TieredAffix={Tier:string;Meta:string;ILvl:int;DisplayHtml:string;Chance:string}
                type MungedAffix={Display:string;FossilMods:string list;Tiers:TieredAffix list}

                let mungeDetails (x:AffixTierContainer<_>) =
                    let children=
                        x.Children
                        |> List.map (selectNodes "td" >> List.ofSeq)
                        |> List.map(List.map (getInnerHtml >> Option.defaultValue null))
                        |> List.choose (fun l ->
                            match l with
                            // shaper/vaal/elder
                            | ParseInt ilvl::fullDisplay::[] -> Some {Tier=null;Meta=null;ILvl=ilvl;DisplayHtml=fullDisplay;Chance=null}
                            // delve/essence/masters
                            | meta::ParseInt ilvl::fullDisplay::[] -> Some {Tier=null;Meta=meta;ILvl=ilvl;DisplayHtml=fullDisplay;Chance=null}
                            // generic rolled
                            | tier::name::ParseInt ilvl::fullDisplay::chance::[] ->Some {Tier=tier;Meta=name;ILvl=ilvl;DisplayHtml=fullDisplay;Chance=chance}
                            | bad::_ ->
                                if PathOfSupporting.Configuration.debug then
                                    eprintfn "you have a bad child %s, %A" bad l
                                None
                            | [] ->
                                if PathOfSupporting.Configuration.debug then
                                    eprintfn "you have a really bad child %A" l
                                None
                        )
                    {Display=x.Display;FossilCategories=x.FossilCategories;Children=children}
            open Munge
            let mapAffixChildren =
                List.choose mungeAffix
                >> List.map (AffixContainer.mapChildren (List.map mungeSubCatPair))
                >> List.map AffixContainer.unwrap
                >> fixUpEffixCategories
                >> Seq.map AffixContainer.wrap
                >> List.ofSeq
                >> List.map(AffixContainer.mapChildren(List.map (Result.map mungeDetails)))

        open Impl.Munge
        /// cn is like Amulet,Claw,
        let parseModPhp targeting =
            let discardFailures (x:Result<Impl.Munge.AffixTierContainer<_>,_> list) =
                List.choose Result.TryGetValue x

            let reWorkMungeModCategory (iac:ItemAffixContainer<Impl.Wrap>) =
                {   Item=iac.Item
                    Children=
                        let childStuff =
                            iac.Children
                            |> Impl.mapAffixChildren
                        childStuff
                        |> List.map(fun x ->
                            {EffixType=x.EffixType;Children = discardFailures x.Children
                            }
                        )
                }

            async{
                match! Impl.fetch targeting with
                | Error e -> return Error e
                | Ok fetched ->
                    return
                        Impl.parse fetched
                        |> mungeDoc
                        |> Result.map List.ofSeq
                        |> Result.map (List.map (
                                        mungeModCategory >> reWorkMungeModCategory
                        ))
            }
