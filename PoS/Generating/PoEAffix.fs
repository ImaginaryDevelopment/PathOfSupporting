namespace PathOfSupporting.Generating.PoEAffix
open System

open PathOfSupporting.Internal.Helpers
open PathOfSupporting.Generating.Impl.FsHtml
open PathOfSupporting.Parsing.Html.Impl.Html

module Enchantment =
    open System.IO
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
module Basing =
    open PathOfSupporting.Internal.BReusable.StringHelpers
    let addBase depth x =
        match depth with
        | i when i < 1 -> x
        | i ->
            let prefix = [0..i-1] |> List.map(fun _ -> "..")
            x::prefix |> List.rev |> delimit "/"
open Basing

module HeadDetail =


    // headAllotment goes in after standard scripts before Google Ad Script stuff
    let generateHead title' depth headAlottment =
        head [] [
            yield E.meta ["charset" %= "utf-8"]
            yield Link.css <| addBase depth "css/my.css"
            yield Link.css <| addBase depth "css/menu.css"
            yield title [] [Text title']
            yield Script.src <| addBase depth "js/jquery-1.10.2.js"
            yield! headAlottment
            yield script [] %(Google.gaScript)
            yield link ["rel"%="shortcut icon";"type"%="image/ico";A.href <| addBase depth "favicon.ico"]
        ]

module Nav =
// let (~%) s = [Text(s.ToString())]
    let navItem title depth pageMap =
        li [] [
            Text title
            ul [] (pageMap |> List.map(fun (title,href) -> li [] [a ["href"%=addBase depth href] %(title)] ))
        ]
    let makeSiteNav depth =
        let navItem t = navItem t depth
        ul [A.className "nav"] [
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
                "Raw Abyss Mods", "RePoE/abyss_jewel.html"
                "Raw Area Mods", "RePoE/area.html"
                "Raw Item Mods - All", "RePoE/item.html"
                "Raw Item Mods - Unique", "RePoE/item-unique.html"
                "Raw Item Mods - Corrupted", "RePoE/item-corrupted.html"
                "Raw Item Mods - Enchantment", "RePoE/item-enchantment.html"
                "Raw Item Mods - Prefix", "RePoE/item-prefix.html"
                "Raw Item Mods - Suffix", "RePoE/item-suffix.html"
                "Raw Misc Mods", "RePoE/misc.html"
            ]
        ]
type BodyArg = {Main:Element list;Main2:Element list;Main3:Element list;InsertFullNav:bool; Corruption:Element list;EnchantPage:string option
                Updated:DateTime
                Left:Element list;Right:Element list;TrailingCenter:Element list}
module BodyDetail =
    let generateBody depth {Main=main;Main2=main2;Main3=main3;InsertFullNav=doNav; EnchantPage=enchantOpt;Corruption=corruption;Left=left;Right=right;TrailingCenter=tc;Updated=updated} scripts =
        body [] [
            yield div [A.id "wrapper"] [
                header [A.id "header"] [
                    div [A.id "logo"] [
                        a [A.href <| addBase depth "index.html"] [
                            img[A.src <|addBase depth "images/header.png"; "alt"%="header"]
                        ]
                    ]
                    Google.ad {TagId="ad";SlotId="5241339200";Comment="728x90 Banner";Width=728;Height=90;ExtraStyle=null}
                    nav [A.id "mainav"] (if doNav then [ Nav.makeSiteNav depth] else [])
                ]
                div[A.id "paypal"] [
                    a[A.href "https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=S6L2QZULXFK7E"][
                        img [A.src <| addBase depth "images/paypal.png";"alt"%="paypal"]
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
                div[A.id "trailing"] tc
                br []
                div [A.id"ADBAR"][
                    Google.ad {TagId= "TopSideBarAd";Comment= "300x250 Text Only";Width=300;Height=250;SlotId="9811139608";ExtraStyle=null}
                    Google.ad {TagId= "lowermiddlesidebarad";Comment= "300x250 Display";Width=300;Height=250;SlotId="3764606006";ExtraStyle=null}
                ]
            ]
            let updateString =updated.ToLongDateString()
            yield footer [A.className "footer"] [
                div [A.id "footermsg"]
                    %(sprintf "&copy; 2015-2017. This site is not affiliated with, endorsed, or sponsored by Grinding Gear Games. (last updated %s)" updateString)
                comment (sprintf "Updated %s" updateString)
            ]
            yield Script.src <| addBase depth "js/closemodal.js"
            yield Script.src <| addBase depth "js/mod.js"
            yield! scripts
        ]
module AffixPages =
    let generateAffixPage title' depth bodyArg=
        html [][
            yield HeadDetail.generateHead title' depth []
            yield BodyDetail.generateBody depth bodyArg []
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
        BodyDetail.generateBody 0
            {
                Main=[ h2 [A.Style.hidden] %"xx" ]
                Main2=[ h2 [A.Style.hidden] %"x" ]
                Main3=[ h2 ["style" %= "text-indent: 522px; margin-left: 0px; margin-right: 215px;"] %"Path of Exile Item Affixes" ]
                InsertFullNav=true
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
                TrailingCenter=[]
            }
            scripts


type Page = | Page of string with member x.Value = match x with |Page pg -> pg
module Impl =
    open System.IO

    open PathOfSupporting.Internal.BReusable
    open PathOfSupporting.Internal.BReusable.StringHelpers
    open PathOfSupporting.Parsing.Html.PoeDb.Tw
    open PathOfSupporting.Parsing.Html.PoeDb.Tw.Impl.Munge
    open HtmlAgilityPack
    open PathOfSupporting.Configuration.Pad

    let writeText location text = File.WriteAllText(path=location,contents=text)

    let parseNodeForChildren =
        sprintf "<span>%s</span>"
        >> HtmlAgilityPack.HtmlNode.CreateNode
        >> wrap 
        >> getChildNodes
    type Target = {ModPageInfo:ModPageInfo;Page:Page;CorruptionElements:Element list;Enchant:string option}
    let targets : Target list =
        let openModalDiv i ident style =
            div [A.id <| sprintf "openModal%i" i;A.className "modalDialog"][
                    div[][
                        a[A.href"#close";A.title "Close";A.className "close"] %("x")
                        div[yield A.id ident; if String.IsNullOrWhiteSpace style then () else yield "style"%=style ] %" Content "
                    ]
                ]
        let genStandardCorruption ident =
            [div [A.className "item"] [ a [A.href "#openModal1000"] %("Corruption")
                                        openModalDiv 1000 ident String.Empty
                ]]
        let setThisColor c = sprintf "this.style.color='%s'" c
        let complex =
            let combos = // cn, an start, corruption, triple file name, enchantment
                [
                    "Body Armour",Page "ch","chestcorr", Some "garb",None
                    "Helmet",Page "hm","helmcorr",None,Some "hm-enchant.html"
                    "Boots",Page "bt","corrboots",None,Some "gl-enchant.html"
                    "Gloves",Page "gl","corrgloves",None,Some "gl-enchant.html"
                    "Shield",Page "sh","corrshield",None,None
                ]
            let stats =[ "str","ar";"dex","ev";"int","es"]
            let genAn isShield items =
                let words = items |> List.map fst |> delimit "_"
                let an =  words |> sprintf "%s_armour"
                if isShield then
                    match words with
                    | "int" -> "focus"
                    | x -> sprintf "%s_shield" x
                    |> sprintf "%s,%s" an
                else an
            let genFn cn = List.map snd >> delimit String.Empty >> sprintf "%s-%s" cn
            let genComplex cn (Page slot) (corr,enchantOpt) items =  {cn=cn;an= items |> genAn (cn="Shield")},Page <| genFn slot items, genStandardCorruption corr,enchantOpt
            [
                for (cn,Page slot,corr, tripleName,enchOpt) in combos do
                    match tripleName with
                    | Some trip ->
                        yield {cn=cn;an=genAn (cn="Shield") stats}, Page <| sprintf "%s-%s" slot trip, genStandardCorruption corr,None
                    | None -> ()
                    
                    for i in [0..stats.Length - 1] do
                        let stat1 = stats.[i]
                        yield genComplex cn (Page slot) (corr,enchOpt) [stat1]
                        for stat2 in stats.[i+1 ..] do
                            yield genComplex cn (Page slot) (corr,enchOpt) [stat1;stat2]
            ]
        [
            // poedb name, local file name, corruption element
            "Amulet","ac-amulet", 
                                [   div [A.className "item"] [
                                        a [A.href "#openModal1000";"onMouseOver"%= setThisColor "#06ef89";"onMouseOut"%=setThisColor"#000"] %("Talisman")
                                        openModalDiv 1000 "talismanimplicit" "text-align:left; line-height:20px; font-size:17px"
                                        a [A.href "#openModal0"] %("Corruption")
                                        openModalDiv 0 "corrammy" "text-align:left" ]]
            "Bow","2h-bow", genStandardCorruption "bowcorr"
            "Belt","ac-belt",genStandardCorruption "corrbelt"
            "Claw","1h-claw",genStandardCorruption "clawcorr"
            "Dagger","1h-dagger",genStandardCorruption "daggcorr"
            "FishingRod","2h-fish",genStandardCorruption "fishcorr"
            "One Hand Axe","1h-axe",genStandardCorruption "1haxecorr"
            "One Hand Mace","1h-mace",genStandardCorruption "1hmacecorr"
            "One Hand Sword","1h-sword",genStandardCorruption "1hswordcorr"
            "Quiver","ac-quiver",genStandardCorruption "corrquiv"
            "Ring","ac-ring",genStandardCorruption "corrring"
            "Sceptre","1h-sceptre",genStandardCorruption "sceptrecorr"
            "Staff","2h-staff",genStandardCorruption "staffcorr"
            "Two Hand Axe", "2h-axe",genStandardCorruption "2haxecorr"
            "Two Hand Mace","2h-mace",genStandardCorruption "2hmacecorr"
            "Two Hand Sword", "2h-sword", genStandardCorruption "2hswordcorr"
            "Wand","1h-wand", genStandardCorruption "wandcorr"
        ]
        |> List.map(fun (title,pg,corr) -> title, Page pg,corr)
        |> List.map(fun (x,pg,corr) -> {cn=x;an=null},pg,corr,None)
        |> List.append complex
        |> List.map(fun (mi,pg, corr,enchOpt)-> {ModPageInfo=mi;Page=pg;CorruptionElements=corr;Enchant=enchOpt})

    let getIds x=
        let rec getIt x =
            match x with
            | Text _ -> []
            | Comment _ -> []
            | Element (_,attrs,children) ->
                [
                    let idAttrs= attrs |> Seq.choose (fun (Attr(name,v)) -> if name = "id" then Some v else None)
                    yield! idAttrs
                    yield! List.collect getIt children
                ]
        getIt x
    let generateToggle toggleId toggleAttr toggleContent contentAttr content =
        [   div (List.append toggleAttr ["onclick"%= sprintf "toggle('%s')" toggleId]) [
                    a [A.href "#/";"type"%="changecolor"] toggleContent
            ]
            div (List.append contentAttr [A.id toggleId;"style"%="display: none"]) content
        ]
    let generateAffix i (item:AffixTierContainer<TieredAffix>) =
        let cleanAffixDisplay =
            let pattern = @"data-tooltip=""(<[^""]+)"" " 
            // unwrap values
            rReplace @"\+\((\d+&ndash;\d+)\)" "$1"
            >> afterOrSelf "Adds"
            >> fun x ->
                let fossilInfo =
                    match x with
                    |RMatch pattern m ->
                        m.Groups.[1].Value
                        |> Some
                    | _ -> None
                fossilInfo, rReplace pattern String.Empty x
        let cleanMeta = 
            rReplace ".*\d" "<strong>$0</strong>"
        let generateAffixHead title =
            div[A.className "mod";"onclick"%= sprintf "toggle('mod%i')" i] [
                a[A.href "#/";"type"%="changecolor"] %(title)
            ]

        let generateAffixBlock ({ILvl=ilvl;DisplayHtml=innerHtml;Tier=tier;Meta=meta} as input) =
            try
                let meta = cleanMeta meta
                let tier = tier |> Option.ofValueString |> Option.map (replace "Tier " "T") |> Option.defaultValue null
                let fossilOpt,display=cleanAffixDisplay innerHtml
                let attrs = [
                    yield "data-ilvl"%=string ilvl
                    match fossilOpt with
                    | Some fo ->
                        let fo =
                            parseNodeForChildren fo
                            |> Seq.filter(isNodeType HtmlNodeType.Element)
                            |> List.ofSeq
                            |> Seq.map(fun n -> sprintf "%s=%s" (getAttrValueOrNull "data-tooltip" n) (getInnerText n))
                            |> delimit"&#10;"
                        yield "title"%=fo
                    | None -> ()
                ]
                li attrs %(sprintf "iLvl %i: %s (%s)%s" ilvl display meta tier)
            with _ ->
                eprintfn "failing on %A" input
                System.Diagnostics.Debugger.Launch() |> ignore
                reraise()
        
        let affixDescr= parseNodeForChildren item.Display |> Seq.map(getInnerText>>trim) |> Seq.filter(String.IsNullOrWhiteSpace >> not) |> delimit" "
        div ["data-descr"%=affixDescr;A.className "modWrapper";"data-modfor"%=string i][
            generateAffixHead item.Display
            div [A.id <| sprintf "mod%i" i;A.className "VAL"]
                [
                    br[]
                    ol [] (item.Children |> List.map generateAffixBlock)
                ]
        ]

    type FixDivisor<'t> = {Prefixes:'t list;Suffixes:'t list} with
        static member map f x = {Prefixes = f x.Prefixes;Suffixes=f x.Suffixes}
        static member mapi f x = 
            let foldIx f (i:int) items =
                ((i,List.empty),items)
                ||> List.fold(fun (i,ixes) ix ->
                    let (j,ix) = f i ix
                    if j < i then invalidOp <| sprintf "i should have been greater or equal (%i, %i)" j i
                    (j,ix :: ixes)
                )
                |> fun (i,items) -> i,List.rev items
                
            let i,prefixes = foldIx f 0 x.Prefixes
            let _i,suffixes = foldIx f i x.Suffixes
            {Prefixes = prefixes;Suffixes=suffixes}
        static member ofContainers x =
            let isPre x = x.EffixType |> containsI "prefix"
            let result = {Prefixes=List.filter isPre x;Suffixes=List.filter (isPre>>not) x}
    //        (result.Prefixes.Length,result.Suffixes.Length).Dump("pref,suff")
            if result.Prefixes.Length = x.Length then invalidOp "bad filter"
            if result.Suffixes.Length = x.Length then invalidOp "bad filter"
            result
    let guardIds title x =
        let allIds = getIds x
        let badSeeds =
            allIds
            |> Seq.groupBy id
            |> Seq.map (fun (x,y) -> x, Seq.length y)
            |> Seq.filter(fun (_,y) -> y > 1)
            |> List.ofSeq
        
        if Seq.exists(fun _ -> true) badSeeds then
            badSeeds.Dump(sprintf "%s is bad" title) |> ignore
            (toString x).Dump() |> ignore
            invalidOp "duplicate"
        x
    let processOne now =
        let generateAffixChild i (x:AffixContainer<AffixTierContainer<TieredAffix>>) =
            let j,children =
                ((i,List.empty),x.Children)
                ||> List.fold(fun (i,items) ix ->
                        let child = generateAffix i ix
                        i+1,child::items
                )
                |> Tuple2.mapSnd List.rev

            j,{   EffixType = x.EffixType
                  Children = children
            }
        let toHtmlModBucket (x:FixDivisor<AffixContainer<Element>>) =
            let makeSideBucket (x:AffixContainer<Element>) =
                div [A.className "modContainer";"data-bucket"%=x.EffixType][
                    yield br []
                    yield div [A.className "seperator"][
                        strong [] %(x.EffixType)
                    ]
                    yield div [A.className "affix"] x.Children
                ]
            let left = x.Prefixes |> List.map makeSideBucket
            guardIds "left" (element "bah" [] left) |> ignore
            let right = x.Suffixes |> List.map makeSideBucket
            guardIds "right" (element "bah" [] right) |> ignore
            br [] :: left,br [] :: right
        let mapItemAffixContainer insertFullNav (Page pg) corruption enchOpt {ItemType=item;Children=children} =
            let left,right =
                children
                    |> FixDivisor<_>.ofContainers
                    |> FixDivisor<_>.mapi (generateAffixChild )
                    |> toHtmlModBucket
            AffixPages.generateAffixPage item 0
                {   Main=[h2[] %("Prefix")]
                    Main2=[h2[] %(item)]
                    Main3=[h2[] %("Suffix")]
                    Corruption=corruption
                    InsertFullNav=insertFullNav
                    EnchantPage=enchOpt
                    Left=left
                    Right=right
                    TrailingCenter=[]
                    Updated=now
                }
            |> guardIds "fin"
            |> toString
            |> sprintf "<!doctype html>\r\n%s"
            |> writeText (sprintf @"C:\projects\poeaffix.github.io\%s.html" pg)

        fun (t,insertFullNav) ->
            t.ModPageInfo
            |> parseModPhp
            |> Async.map(
                Result.map(List.map (mapItemAffixContainer insertFullNav t.Page t.CorruptionElements t.Enchant))
            )
    //type Target = {ModPageInfo:ModPageInfo;Page:Page;CorruptionElements:Element list;Enchant:string option}
    let wrapProcess now insertFullNav t =
        processOne now (t,insertFullNav)
        |> Async.Catch
        |> Async.map(
            function
            |Choice1Of2 (Error e) ->
                printfn "Success for %A to %s" t.ModPageInfo t.Page.Value
                Some e
            |Choice1Of2 _x -> None
            |Choice2Of2 y ->
                printfn "Failed %s %A to %s" y.Message t.ModPageInfo t.Page.Value
                eprintfn "<%s>\r\n\r\n" y.StackTrace 
                y.Data.Add("cn",box t.ModPageInfo)
                y.Data.Add("pg",box t.Page.Value)
                let pe =PathOfSupporting.Internal.Helpers.PoSException.Exception y
                Some("processOne error",Some pe) // PathOfSupporting.Internal.Helpers
        )

    // doesn't currently write a file it appears
    let runHelmetEnchant() =
        let skills =
            match PathOfSupporting.Parsing.Trees.Gems.getSkillGems {ResourceDirectory="C:\projects\PathOfSupporting\PoS";Filename=None} with
            | Ok x -> x
            | Error e -> e.Dump("skills error") |> ignore; List.empty
        PathOfSupporting.Internal.Helpers.Api.fetch "http://poedb.tw/us/mod.php?type=enchantment&an=helmet"
        |> Async.map (
            Result.map parse
            >> Result.map(fun n ->
                let poeDb =
                    let n =
                        n
                        |> selectNodes ".//table//tbody/tr/td[2]"
                        |> Seq.choose(fun x ->
                            let text = getInnerText x
                            if text ="&nbsp;" then None
                            else Some (Enchantment.findEnchantMatch skills text,x)
                        )
                        |> List.ofSeq
                    (n |> Seq.filter(fst >> Option.isNone) |> Seq.map snd).Dump("unmatched") |> ignore
                    n
                    |> Seq.choose(fun (x,y) -> x |> Option.map(fun x -> x,y))
                    |> Seq.groupBy fst
                    |> Seq.map(fun (k,items) -> k, items |> Seq.map snd |> Seq.map getInnerText)
                    |> Seq.sortBy fst
                    |> List.ofSeq
                let affix = Enchantment.parseOldEnchantment @"C:\projects\poeaffix.github.io\templates\enchant.html" |> List.ofSeq |> List.collect snd
                (PathOfSupporting.Internal.Helpers.Seq.outerJoin affix poeDb fst fst)
            )
        )
        |> Async.RunSynchronously
        |> Dump
        |> ignore
    let runAffixes now =
        targets
        |> List.map (wrapProcess now false)
        |> List.map Async.RunSynchronously
        |> List.choose id
        |> fun x ->
            try
                Util.ClearResults()
            with ex -> eprintfn "Failed to clear results %s" ex.Message
            x.Dump(maxDepth=1)
        |> ignore

    let generateIndex now =
        html [] [
            HeadDetail.generateHead "PoE Affix" 0 []
            Index.generateIndexBody now []
        ]
        |> guardIds "fin"
        |> toString
        |> sprintf "<!doctype html>\r\n%s"
        |> fun x -> File.WriteAllText(sprintf @"C:\projects\poeaffix.github.io\%s.html" "index",x)

    let scrapeAll now =
        generateIndex now
        runAffixes now
        runHelmetEnchant()