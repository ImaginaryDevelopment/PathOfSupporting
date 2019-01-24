namespace PathOfSupporting.Parsing.Html
open System.Net.Http
open System.Collections.Generic
open PathOfSupporting.Internal.Helpers

type Character = {Name:string;League:string; Class:string;Level:int}
[<RequireQualifiedAccess>]
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
        let getInnerText = mapNull (fun x -> x.InnerText)
        let getInnerHtml = mapNull (fun x -> x.InnerHtml)
        let getFirstChild = mapNode (fun x -> x.FirstChild)
        let getOuterHtml = mapNull(fun x -> x.OuterHtml)
        let getParentNode = mapNode(fun x -> x.ParentNode)
        let selectNodes (xpath:string)= bind(fun x -> x.SelectNodes(xpath) |> Option.ofObj |> Option.map (Seq.map wrap)) >> Option.defaultValue Seq.empty
        let selectNode (xpath:string) = mapNode(fun x -> x.SelectSingleNode xpath)
        let getChildNodes = map(fun x -> x.ChildNodes |> Seq.cast<HtmlNode> |> Seq.map wrap) >> Option.defaultValue Seq.empty >> Seq.filter(getOuterHtml >> String.IsNullOrWhiteSpace >> not)
        let getNextSibling = map(fun x -> x.NextSibling |> wrap)
        let getNodeType = map(fun x -> x.NodeType)
        let getNodeName = map(fun x -> x.Name) >> Option.defaultValue null
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
        let parse x =
            let hd = HtmlDocument()
            hd.LoadHtml x
            hd.DocumentNode
            |> wrap
        let (|NodeName|_|) n x = bind(fun x -> if x.Name = n then Some (wrap x) else None) x

[<NoComparison>]
type GetResult =
    |Success of Character[]
    |FailedDeserialize of PoSError
    |FailedHttp of string
    with
        member x.GetSuccess() =
            match x with
            | Success ch -> Some ch
            | _ -> None
        member x.GetFailedDeserialize() =
            match x with
            | FailedDeserialize e -> Some e
            | _ -> None
        member x.GetFailedHttp() =
            match x with
            |FailedHttp msg -> Some msg
            | _ -> None
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
                        if PathOfSupporting.Configuration.debug then
                            match result with
                            | Ok _ ->
                                printfn "Fetched %s" t
                            | Error (msg,_) ->
                                eprintfn "Failed Fetch of %s,%s" t msg
                        return result
                }
            module Munge =
                open Impl.Html
                let mungeDoc wrapped =
                    let nodes =
                        wrapped
                        |> selectNodes "//div/h4"
                        |> Seq.filter(hasId >> not >&> hasChildWith (isNodeType HtmlNodeType.Text >> not))
                        |> List.ofSeq
                    match nodes with
                    | [] -> Result.ErrMsg <| sprintf "could not find h4 in %A" (getOuterHtml wrapped)
                    | x -> Ok x

                type ItemAffixContainer<'t> = {ItemType:string;Children:'t list} with
                    static member mapChildren f (x:ItemAffixContainer<_>) = {ItemType=x.ItemType;Children=List.map f x.Children}
                    static member chooseChildren f (x:ItemAffixContainer<_>) = {ItemType=x.ItemType; Children = List.choose f x.Children}
                    member x.ToDump() = sprintf "%A" x
                // headerNode is (H4 Amulets... but should be Prefix;Suffix;...)
                let mungeModCategory headerNode =
                //    headerNode.Dump("header")
                    let category = getInnerText headerNode |> trim
                    printfn "munging %s" category
                    {ItemType=category;Children=getFollowingSiblings headerNode |> List.ofSeq}
                type AffixContainer<'t> = {EffixType:string;Children:'t list}
                module AffixContainer =
                    let mapChildren f x = {EffixType=x.EffixType;Children=f x.Children}
                    let inline unwrap x = x.EffixType,x.Children
                    let inline wrap (x,y) = {EffixType=x;Children=List.ofSeq y}
                    let mapRaw f x = unwrap x |> f |> wrap

                let mungeAffix titleNode =
                    match titleNode |> selectNode "h4" |> getInnerText |> Option.ofValueString with
                    | None -> None
                    // an affix or suffix
                    | Some effixType ->
                        let subCategories =
                            titleNode
                            |> getChildNodes
                            |> Seq.skipWhile (isNodeType HtmlNodeType.Element>>not)
                            |> Seq.tryTail
                            |> Seq.filter (getInnerText>>String.IsNullOrWhiteSpace>>not)
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
                                subCatNode |> selectNodes(".//*[contains(@class,'badge')]") |> Seq.choose (getInnerText>>Option.ofValueString) |> List.ofSeq
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

                let (|OuterHtml|_|) = getOuterHtml >> Option.ofValueString
                let (|InnerHtml|_|) = getInnerHtml >> Option.ofValueString
                let mungeDetails (x:AffixTierContainer<_>) =
                    let children=
                        x.Children
                        |> List.map (selectNodes "td" >> List.ofSeq)
                        //|> List.map(List.map (getInnerHtml >> Option.defaultValue null))
                        |> List.choose (fun l ->
                            match l with
                            // shaper/vaal/elder
                            | InnerHtml(ParseInt ilvl)::fullDisplay::[] -> Some {Tier=null;Meta=null;ILvl=ilvl;DisplayHtml=getInnerHtml fullDisplay;Chance=null}
                            // delve/essence/masters
                            | meta::InnerHtml(ParseInt ilvl)::fullDisplay::[] -> Some {Tier=null;Meta=getInnerHtml meta;ILvl=ilvl;DisplayHtml=getInnerHtml fullDisplay;Chance=null}
                            // generic rolled
                            | tier::name::InnerHtml(ParseInt ilvl)::fullDisplay::chance::[] ->Some {Tier=getInnerHtml tier;Meta=getInnerHtml name;ILvl=ilvl;DisplayHtml=getInnerHtml fullDisplay;Chance=getInnerHtml chance}
                            | InnerHtml bad::_ ->
                                if PathOfSupporting.Configuration.debug then
                                    eprintfn "you have a bad child %s, %A" bad l
                                None
                            | [] ->
                                if PathOfSupporting.Configuration.debug then
                                    eprintfn "you have a really bad child %A" l
                                None
                            | x ->
                                if PathOfSupporting.Configuration.debug then
                                    eprintfn "idk what is going on %A" x
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
                {   ItemType=iac.ItemType
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
                        Impl.Html.parse fetched
                        |> mungeDoc
                        |> Result.map List.ofSeq
                        |> Result.map (List.map (
                                        mungeModCategory >> reWorkMungeModCategory
                        ))
            }

module PoeAffix =
    open PathOfSupporting.Parsing.Impl.FsHtml

    open System
    module Enchantment =
        open System.IO
        open Impl.Html
        open PathOfSupporting.Internal.BReusable.StringHelpers
        open PathOfSupporting.Parsing.Trees.Gems

        let findEnchantMatch (skills:Gem list) (htmlText:string) =
            let inline isPair (sk:Gem) htmlDelim skillName = (htmlText |> containsI htmlDelim) && sk.Name=skillName
            skills
            |> List.tryFind(fun sk ->
                let inline isPair d sn = isPair sk d sn
                containsI sk.Name htmlText
                || (isPair "Charged Slam" "Tectonic Slam")
                || (isPair "Skeletons" "Summon Skeleton")
                || (isPair "Animated Guardian" "Animate Guardian")
                || (isPair "Animated Weapons" "Animate Weapon")
                || (isPair "Holy Relic" "Summon Holy Relic")
                || (isPair "Spectre" "Raise Spectre")
                || (isPair "Zombie" "Raise Zombie")
                || (isPair "Chaos Golem" "Summon Chaos Golem")
                || (isPair "Lightning Golem" "Summon Lightning Golem")
                || (isPair "Flame Golem" "Summon Flame Golem")
                || (isPair "Ice Golem" "Summon Flame Golem")
                || (isPair "Stone Golem" "Summon Stone Golem")
                || (isPair "Fire Nova" "Fire Nova Mine")
                || (isPair "Agony Crawler" "Herald of Agony")
                || (isPair "Sentinels of Dominance" "Dominating Blow")
                || (isPair "Sentinel of Dominance" "Dominating Blow")
                || (isPair "Sentinels of Purity" "Herald of Purity")
                || (isPair "Converted Enemies" "Conversion Trap")
                || (isPair "Raging Spirits" "Summon Raging Spirit")
            )
            |> Option.map(fun x -> x.Name)


        let parseOldEnchantment target =
            let doc = 
                File.ReadAllText target
                |> parse
            let body = doc |> selectNode "html/body"

            body
            |> getChildNodes
            |> Seq.map(fun x ->
                let title = selectNode "div[@id]" x |> getAttrValueOrNull "id" |> function | null -> getAttrValueOrNull "id" x | x -> x
                let children =
                    match x |> getChildNodes |> List.ofSeq with
                    | (NodeName "div" d)::[] -> getChildNodes d
                    | x -> x |> Seq.ofList
                    |> Seq.filter(getNodeName>> (=) "br" >> not)
                    |> Seq.unflatten (getNodeName >> (<>) "li") getInnerText id
                title,children
            )

    ()

    type GoogleAd={TagId:string;SlotId:string;Comment:string;Width:int;Height:int;ExtraStyle:string}
    module Google =
        let gaScript = """(function(i,s,o,g,r,a,m){i['GoogleAnalyticsObject']=r;i[r]=i[r]||function(){
          (i[r].q=i[r].q||[]).push(arguments)},i[r].l=1*new Date();a=s.createElement(o),
          m=s.getElementsByTagName(o)[0];a.async=1;a.src=g;m.parentNode.insertBefore(a,m)
          })(window,document,'script','https://www.google-analytics.com/analytics.js','ga');
        
          ga('create', 'UA-98200285-1', 'auto');
          ga('send', 'pageview');"""
        let ad {TagId=tagId;SlotId=slotId;Comment=commentText;Width=w;Height=ht;ExtraStyle=extraStyle} =
            div[A.id tagId][
                Script.asyncScript "//pagead2.googlesyndication.com/pagead/js/adsbygoogle.js"
                comment commentText
                ins[
                    A.className "adsbygoogle"
                    "style"%= sprintf "display: inline-block; width: %ipx; height: %ipx;%s" w ht extraStyle
                    "data-ad-client"%="ca-pub-7924380187053536"
                    "data-ad-slot"%=slotId ][]
                Script.text "(adsbygoogle = window.adsbygoogle || []).push({});"
                
            ]
    // let (~%) s = [Text(s.ToString())]
    let generateHead title' scripts =
        head [] [
            yield E.meta ["charset" %= "utf-8"]
            yield Link.css "css/my.css"
            yield Link.css "css/menu.css"
            yield title [] [Text title']
            yield Script.src "js/jquery-1.10.2.js"
            yield! scripts
            yield script [] %(Google.gaScript)
            yield link ["rel"%="shortcut icon";"type"%="image/ico";A.href "favicon.ico"]
        ]
    let navItem title pageMap =
        li [] [
            Text title
            ul [] (pageMap |> List.map(fun (title,href) -> li [] [a ["href"%=href] %(title)] ))
        ]
    let siteNav =
        ul [] [
            navItem "One Hand" [
                "Axe", "1h-axe.html"
                "Claw", "1h-claw.html"
                "Dagger", "1h-dagger.html"
                "Mace", "1h-mace.html"
                "Sceptre", "1h-sceptre.html"
                "Sword", "1h-sword.html"
                "Wand", "1h-wand.html"
            ]
            navItem "Two Hand" [
                "Axe", "2h-axe.html"
                "Bow", "2h-bow.html"
                "Mace", "2h-mace.html"
                "Staff", "2h-staff.html"
                "Sword", "2h-sword.html"
                "Fishing", "2h-fish.html"
            ]
            navItem "Body Armour" [
                "Armour", "ch-ar.html"
                "Evasion", "ch-ev.html"
                "Energy", "ch-es.html"
                "Armour/Energy", "ch-ares.html"
                "Armour/Evasion", "ch-arev.html"
                "Evasion/Energy", "ch-eves.html"
                "Sacrificial", "ch-garb.html"
            ]
            navItem "Helmet" [
                "Armour", "hm-ar.html"
                "Evasion", "hm-ev.html"
                "Energy", "hm-es.html"
                "Armour/Energy", "hm-ares.html"
                "Armour/Evasion", "hm-arev.html"
                "Evasion/Energy", "hm-eves.html"
                "Enchantment", "hm-enchant.html"
            ]
            navItem "Gloves" [
                "Armour", "gl-ar.html"
                "Evasion", "gl-ev.html"
                "Energy", "gl-es.html"
                "Armour/Energy", "gl-ares.html"
                "Armour/Evasion", "gl-arev.html"
                "Evasion/Energy", "gl-eves.html"
                "Enchantment", "gl-enchant.html"
            ]
            navItem "Boots" [
                "Armour", "bt-ar.html"
                "Evasion", "bt-ev.html"
                "Energy", "bt-es.html"
                "Armour/Energy", "bt-ares.html"
                "Armour/Evasion", "bt-arev.html"
                "Evasion/Energy", "bt-eves.html"
                "Enchantment", "gl-enchant.html"
            ]
            navItem "Shield" [
                "Armour", "sh-ar.html"
                "Evasion", "sh-ev.html"
                "Energy", "sh-es.html"
                "Armour/Energy", "sh-ares.html"
                "Armour/Evasion", "sh-arev.html"
                "Evasion/Energy", "sh-eves.html"
            ]
            navItem "Accessories" [
                "Amulet", "ac-amulet.html"
                "Belt", "ac-belt.html"
                "Ring", "ac-ring.html"
                "Quiver", "ac-quiver.html"
                "Flask", "ac-flask.html"
                "Cobalt", "../jw-cobalt.html"
                "Crimson", "../jw-crimson.html"
                "Viridian", "../jw-viridian.html"
                "Murderous", "jw-murderous.html"
                "Searching", "jw-searching.html"
                "Hypnotic", "jw-hypnotic.html"
                "Ghastly", "jw-ghastly.html"
                "Jewel", "../jw-all.html"
            ]
            navItem "Other" [
                "Map", "ot-map.html"
                "Strongbox", "ot-box.html"
            ]
        ]
    type BodyArg = {Main:Element list;Main2:Element list;Main3:Element list;Corruption:Element list;EnchantPage:string option
                    Updated:DateTime
                    Left:Element list;Right:Element list}
    let generateBody {Main=main;Main2=main2;Main3=main3;EnchantPage=enchantOpt;Corruption=corruption;Left=left;Right=right;Updated=updated} scripts =
        body [] [
            yield div [A.id "wrapper"] [
                header [A.id "header"] [
                    div [A.id "logo"] [
                        a [A.href "index.html"] [
                            img[A.src "images/header.png"; "alt"%="header"]
                        ]
                    ]
                    Google.ad {TagId="ad";SlotId="5241339200";Comment="728x90 Banner";Width=728;Height=90;ExtraStyle=null}
                    nav [A.id "mainav"] [ siteNav ]
                ]
                div[A.id "paypal"] [
                    a[A.href "https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=S6L2QZULXFK7E"][
                        img [A.src "images/paypal.png";"alt"%="paypal"]
                    ]
                ]
                div[A.id "pageinfo"][
                    article [A.id "main"] main
                    article [A.id "main2"] main2
                    article [A.id "main3"] main3
                ]
                aside[A.id "corruption"] 
                    [
                        match enchantOpt with
                        | None -> ()
                        | Some enchant ->
                            yield div [A.className "item"][ a[A.href enchant; A.className "specialeffects"] %("Enchantment")]

                        yield div[A.id"ilvlFilter"] [
                            label [] %"ILvl filter"
                            input [A.id "ilvlInput";"type"%="number"]
                        ]
                        yield! corruption
                    ]

                aside[A.id "left";A.className "left list"] left
                aside[A.id "right";A.className "right list"] right
                div [A.id"ADBAR"][
                    Google.ad {TagId= "TopSideBarAd";Comment= "300x250 Text Only";Width=300;Height=250;SlotId="9811139608";ExtraStyle=null}
                    Google.ad {TagId= "lowermiddlesidebarad";Comment= "300x250 Display";Width=300;Height=250;SlotId="3764606006";ExtraStyle=null}
                ]
            ]
            yield footer [A.className "footer"] [
                div [A.id "footermsg"] %("© 2015-2017. This site is not affiliated with, endorsed, or sponsored by Grinding Gear Games.")
                comment (sprintf "Updated %s" <| updated.ToLongDateString())
            ]
            yield Script.src "js/closemodal.js"
            yield Script.src "js/mod.js"
            yield! scripts
        ]

    let generateAffixPage title' bodyArg=
        html [][
            yield generateHead title' []
            yield generateBody bodyArg []
        ]

    module Index =
        let generateIndexBody updated scripts =
            let blogItems =
                let blogItem (date:DateTime,x)=
                    // deviate, wrap each blurb in an element
                    div[] [
                        br []
                        div[A.className "seperatorINDEX";"align"%="center"][
                            strong [] %(date.ToLongDateString())
                        ]
                        div[A.className "affix index"] x
                    ]
                [
                    DateTime(2019,1,18),[center [] %"Quite a few betrayal affixes are in"]
                    DateTime(2019,1,9),[center [] %"Working on new betrayal affixes"]
                    DateTime(2018,8,4),[center[] [
                                                    Text "Incorrect or missing information should be reported at" 
                                                    u [] [a[A.href "https://github.com/poeaffix/poeaffix.github.io"] %"github.com/poeaffix"]
                                                ]
                                        br []
                                        Text "Added new Vaal orb corruptions."
                                        br []
                                        br []
                                        Text """If a piece of gear has more life, energy shield, evasion, or armour value than a single listed mod, it's
                    because the item has two mods that combine that stat. This is also the case for physical and spell damage on
                    weapons, maybe more. Thanks for all the support."""
                    ]
                    DateTime(2017,12,11), %"Added new Shaped/Elder mods, updated the ilvl requirements of the Abyss jewels, and fixed some mods GGG updated."
                    DateTime(2017,12,8), %"Added new Abyss jewel mods. In process of updating Shaper/Elder mods along with anything else that has changed. The ilvl of the jewel mods are incorrect atm."
                    DateTime(2017,8,9), %"Added new jewel and map mods, updated energy shield essences. Fixed some helmet enchants not showing"
                    DateTime(2017,8,4), %"""This update changed the mod values to the new 3.0 values (Beta wave 4). I did not include legacy values like
                        before because there are too many of them with this update (Sorry standard players). Updates to Essences,
                        jewels, and anything else changed after beta wave 4 will be addressed later this week. Thanks for all the
                        support."""
                    DateTime(2017,5,12),%"""This update is to add multi-mod viewing and further improve the layout of mods. It should be a little easier to
                    identify what can be crafted on each item. I will be making a more thorough update in the near future, possibly
                    even converting it to be mobile friendly. I have not cross checked every mod value, but they seem to be mostly
                    correct from what short time I spent looking. I have disabled the Helmet enchant page. I haven't had time to
                    get to it yet. Any missing or incorrect information should be reported to poeaffix@gmail.com. 3.0 information
                    will be updated after the beta balance changes are final. I would also like to add a crafting guide section,
                    you can email me any crafting techniques that you think should be included. I would like to thank all the
                    people that have shown support for this site. Special shout out to Twitch streamers that have continually
                    promoted the site."""
                ]
                |> List.map blogItem
            generateBody
                {
                    Main=[ h2 [A.Style.hidden] %"xx" ]
                    Main2=[ h2 [A.Style.hidden] %"x" ]
                    Main3=[ h2 ["style" %= "text-indent: 522px; margin-left: 0px; margin-right: 215px;"] %"Path of Exile Item Affixes" ]
                    Corruption=[div [][]]
                    EnchantPage=None
                    Updated=updated
                    Left=[
                            Google.ad {TagId="uppermiddlesidebaradINDEX";Width=300;Height=600;Comment="300x600 Display Only";ExtraStyle="margin-left: 0px";SlotId="2287872807"}
                        ]
                    Right=[
                        yield br []
                        yield! blogItems
                    ]
                }
                scripts
