namespace PathOfSupporting.Parsing.PoEAffix
open System

open PathOfSupporting.Internal.Helpers
open PathOfSupporting.Parsing.Impl.FsHtml
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

module HeadDetail =
    open PathOfSupporting.Internal.BReusable.StringHelpers

    let addBase depth x =
        match depth with
        | i when i < 1 -> x
        | i ->
            let prefix = [0..i-1] |> List.map(fun _ -> "..")
            x::prefix |> List.rev |> delimit "/"

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
module BodyDetail =
    let private addBase = HeadDetail.addBase
    let generateBody depth {Main=main;Main2=main2;Main3=main3;EnchantPage=enchantOpt;Corruption=corruption;Left=left;Right=right;Updated=updated} scripts =
        body [] [
            yield div [A.id "wrapper"] [
                header [A.id "header"] [
                    div [A.id "logo"] [
                        a [A.href <| addBase depth "index.html"] [
                            img[A.src <|addBase depth "images/header.png"; "alt"%="header"]
                        ]
                    ]
                    Google.ad {TagId="ad";SlotId="5241339200";Comment="728x90 Banner";Width=728;Height=90;ExtraStyle=null}
                    nav [A.id "mainav"] [ Nav.siteNav ]
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
                div [A.id"ADBAR"][
                    Google.ad {TagId= "TopSideBarAd";Comment= "300x250 Text Only";Width=300;Height=250;SlotId="9811139608";ExtraStyle=null}
                    Google.ad {TagId= "lowermiddlesidebarad";Comment= "300x250 Display";Width=300;Height=250;SlotId="3764606006";ExtraStyle=null}
                ]
            ]
            yield footer [A.className "footer"] [
                div [A.id "footermsg"] %("© 2015-2017. This site is not affiliated with, endorsed, or sponsored by Grinding Gear Games.")
                comment (sprintf "Updated %s" <| updated.ToLongDateString())
            ]
            yield Script.src <| HeadDetail.addBase depth "js/closemodal.js"
            yield Script.src <| HeadDetail.addBase depth "js/mod.js"
            yield! scripts
        ]
module AffixPages =
    let generateAffixPage title' bodyArg=
        html [][
            yield HeadDetail.generateHead title' 0 []
            yield BodyDetail.generateBody 0 bodyArg []
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