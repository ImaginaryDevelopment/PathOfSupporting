namespace PathOfSupporting.Parsing.PoB

open System
open PathOfSupporting.Parsing.Trees.Gems
open PathOfSupporting.Internal.BReusable
open PathOfSupporting.Internal.Helpers

type SkillGroup = {Gems:Gem list;IsEnabled:bool; Slot:string; IsSelectedGroup:bool}
type CharacterSkills = {MainSkillIndex:int; SkillGroups: SkillGroup list} with
    member x.MainSkillGroup =
        if x.MainSkillIndex >= 0 && x.MainSkillIndex < x.SkillGroups.Length then
            Some x.SkillGroups.[x.MainSkillIndex]
        else None
type Summary = Map<string,string>
type Item = {Id:int; Raw:string}
type ItemSlot = {Name:string; Id:int}
type MinionSummary = MinionSummary

[<NoComparison>]
type Character = {Level:int; Class:string;Ascendancy:string; AuraCount:int; Config:string; CurseCount:int; Items:Item seq; ItemSlots:ItemSlot seq ; Summary:Summary; MinionSummary:MinionSummary;Skills:CharacterSkills;Notes:string;Tree:string}

// based on https://github.com/Kyle-Undefined/PoE-Bot/blob/997a15352c83b0959da03b1f59db95e4a5df758c/Helpers/PathOfBuildingHelper.cs
module PathOfBuildingParsing =

    module Impl =
        open System.IO
        open Ionic.Zlib
        open System.Text
        open System.Xml.Linq
        open System.Net.Http

        open PathOfSupporting.Internal.Helpers.Xml
        open PathOfSupporting.Internal.BReusable.StringHelpers

        let getIntAttrib name =
            getAttribValue name
            >> Option.bind (|ParseInt|_|)

        let fromPasteBin (url) =
            if not <| String.startsWith "https://pastebin.com/" url then Result.ErrMsg "Unrecognized url"
            else 
                let raw = "https://pastebin.com/raw/"
                let last = url.Split('/') |> Array.last
                use client = new HttpClient()
                sprintf "%s%s" raw last
                |> client.GetStringAsync
                |> Async.AwaitTask
                |> Async.RunSynchronously
                |> Ok

        let fromBase64ToXml(base64:string) =
            let dec =
                base64
                |> replace "-" "+"
                |> replace "_" "/"
                |> Convert.FromBase64String
            use input = new MemoryStream(dec)
            use deflate = new ZlibStream(input, CompressionMode.Decompress)
            use output = new MemoryStream()
            deflate.CopyTo output
            output.ToArray()
            |> Encoding.UTF8.GetString

        // include validation, only create if result is valid
        let tryCreateStat (xe:XElement) : (string*string) option =
            match getAttribValue "stat" xe,getAttribValue "value" xe with
            | Some (ValueString stat), Some(ValueString value) ->
                Some(stat,value)
            | None,_ -> None
            | _,None -> None
            | Some(statOpt), Some(valueOpt) ->
                eprintfn "Unexpected (stat='%s',value='%s')" statOpt valueOpt
                None

        let mapElementSequence key subKey f =
            getElement key
            >> Option.map(
                    getElements subKey
                    >> Seq.map f
                    >> List.ofSeq
            )

        let getPlayerStats: _ -> Map<string,string> option =
            mapElementSequence "Build" "PlayerStat" tryCreateStat
            >> Option.map(
                Seq.choose id
                >> Seq.groupBy fst
                >> Seq.choose(fun (name,x) ->
                    match x |> Seq.map snd |> Seq.tryHead with
                    | Some "0" -> None
                    | Some value ->
                        Some (name,value)
                    | None -> eprintfn "unexpected stat/value"; None
                )
                >> Map.ofSeq
            )

        let getGemsFromSkill =
            getElements "Gem"
            >> Seq.map(fun c ->
                {   SkillId= getAttribValueOrNull "skillId" c
                    Name= getAttribValueOrNull "nameSpec" c
                    Level= getIntAttrib "level" c |> Option.defaultValue -1
                    Quality = getIntAttrib "quality" c  |> Option.defaultValue -1
                    Enabled = getAttribValue "enabled" c |> Option.bind (|ParseBoolean|_|) |> Option.defaultValue false
                }
            )

        let getCharacterSkills (xe:XElement) =
            let mainGroup = xe |> getElement "Build" |> Option.bind (getIntAttrib "mainSocketGroup")
            let skills =
                xe
                |> mapElementSequence "Skills" "Skill" (
                        fun c ->        {
                                            Gems= getGemsFromSkill c |> List.ofSeq
                                            Slot= getAttribValue "slot" c |> Option.defaultValue null
                                            IsEnabled=getAttribValue "enabled" c |>  Option.bind (|ParseBoolean|_|) |> Option.defaultValue false
                                            IsSelectedGroup = false
                                        }
                )
            match skills with
            | Some skills ->
                Some {SkillGroups=skills;MainSkillIndex = Option.defaultValue -1 mainGroup}
            | None -> None

        let getItemSlots =
            mapElementSequence "Items" "Slot" (fun c ->
                    {
                        Name=getAttribValueOrNull "name" c
                        Id=getAttribValue "itemId" c |> Option.bind (|ParseInt|_|) |> Option.defaultValue -1
                    }
            )
        let getItems =
            mapElementSequence "Items" "Item" (fun c ->
                    {   Id=getAttribValue "id" c |> Option.bind (|ParseInt|_|) |> Option.defaultValue -1
                        Raw = string c
                    }
            )
        let getNotes =
            getElement "Notes"
            >> Option.map (string>>trim)
            >> Option.defaultValue null

        let parseCode (base64:string) : Character =
            let xDoc =
                let xml = fromBase64ToXml base64 |> XDocument.Parse
                let tXml = xml |> string |> replace "Spec:" String.Empty
                dump (Some "pob") (Some "xml") id <| box tXml
                tXml |> XDocument.Parse
            dumpXE (Some "parsed") None xDoc.Root

            let minionSum = MinionSummary

            let skills = getCharacterSkills xDoc.Root |> Option.defaultValue {SkillGroups=List.empty; MainSkillIndex= -1}

            let build = xDoc.Root |> getElement "Build"
            let getBuildAttribValue name = build |> Option.bind(getAttribValue name)
            let result =
                {   Skills=skills
                    Level= getBuildAttribValue "level"|>Option.bind (|ParseInt|_|) |> Option.defaultValue -1
                    Class= getBuildAttribValue "className" |> Option.defaultValue null
                    Ascendancy= getBuildAttribValue "ascendClassName" |> Option.defaultValue null
                    AuraCount = -1
                    CurseCount= -1
                    Items= getItems xDoc.Root |> Option.defaultValue List.empty :> _ seq
                    ItemSlots= getItemSlots xDoc.Root |> Option.defaultValue List.empty :> _ seq
                    Config=null
                    Summary=getPlayerStats xDoc.Root |> Option.defaultValue Map.empty
                    MinionSummary = minionSum
                    Tree = null
                    Notes=getNotes xDoc.Root
                }
            Utils.dump (Some "character") (Some "txt") id <| box result
            result

    open Impl
    /// accepts the actual pob code, or a pastebin link
    let parseText =
        function
        | StartsWith "http" & Contains "pastebin" as x ->
            Impl.fromPasteBin x
        | StartsWith "http" as x ->
            Result.ErrMsg <| sprintf "Unrecognized url:%s" x
            // perhaps the text should be validated somehow?
        | x -> Ok x
        >> Result.map parseCode

    let processCodeOrPastebin x = parseText x
