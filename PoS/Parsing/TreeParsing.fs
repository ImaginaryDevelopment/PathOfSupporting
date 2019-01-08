module PathOfSupporting.Parsing.Trees

open System
open System.Collections.Generic
open Newtonsoft.Json.Linq
open PathOfSupporting.Internal.BReusable
open PathOfSupporting.Internal.Helpers
open PathOfSupporting.Classes

type NodeType =
    |Normal
    |Notable
    |Keystone
    |Mastery
    |JewelSocket
type JsonResourcePath = {ResourceDirectory:string; Filename:string option}
module Impl =
    /// defaultFilename argument is intended for internal use
    /// should be something like "Gems.json", "Gems.3.5.json", "Passives.json" ...
    let getResourcePath defaultFilename {ResourceDirectory=rd; Filename=fOverrideOpt} =
            let jsonFilename = defaultArg fOverrideOpt defaultFilename
            let path = IO.Path.Combine(rd,jsonFilename)
            if IO.File.Exists path then Ok path
            else Result.ErrMsg <| sprintf "File not found:%s" path

/// things we can read out of the Path of Exile official Gems.json
module Gems =

    // make methods less stringly typed
    type Gem = {SkillId:string; Name:string; Level:int; Quality:int;Enabled:bool}
    let isGemNameEqual skillName (x:Gem) = String.equalsI x.Name skillName || String.equalsI (sprintf "%s support" skillName) x.Name
    let getSkillGems rp =
        Impl.getResourcePath "Gems3.5.json" rp
        |> Result.bind(fun path ->
            path
            |> IO.File.ReadAllText
            |> SuperSerial.deserialize<Gem list>
        )


    let getSkillGem sgjp skillName =
        getSkillGems sgjp
        |> Result.map (Seq.tryFind(isGemNameEqual skillName))
        |> Result.bind (function | None -> Result.ErrMsg <| sprintf "Gem %s not found" skillName | Some g -> Result.Ok g)

    // returns gems in the name terms you input, so if you Arcane Surge comes in, that's the name that goes back out
    // even if the gem name is officially Arcane Surge Support
    [<Struct>]
    type GemLevelInfo = {ProvidedName:string;MatchedName:string;LevelRequirement:int option}
    let getGemReqLevels sgjp skillNames =
        match getSkillGems sgjp with
        | Error msg -> Error msg
        | Ok gems ->
            skillNames
            |> Seq.map(fun skillName ->
                match gems |> List.tryFind(isGemNameEqual skillName) with
                // should we assume anything less than 1 means the parsing was bad or there isn't one, so we have a default coming in?
                |Some g -> {ProvidedName=skillName;MatchedName=g.Name; LevelRequirement=Some g.Level}
                | None -> {ProvidedName=skillName;MatchedName=null;LevelRequirement=None}
            )
            |> List.ofSeq
            :> IReadOnlyList<_>
            |> Ok


//// incomplete translation
//type SkillNodeGroup() = // https://github.com/PoESkillTree/PoESkillTree/blob/f4a6119be852315ca88c63d91a1acfb5901d4b8a/WPFSKillTree/SkillTreeFiles/SkillNodeGroup.cs
//    member val Nodes = ResizeArray<SkillNode>() with get,set
//// mostly complete
//and SkillNode () = // https://github.com/PoESkillTree/PoESkillTree/blob/f4a6119be852315ca88c63d91a1acfb5901d4b8a/WPFSKillTree/SkillTreeFiles/SkillNode.cs
//    member val Attributes = Dictionary<string,float list>() with get,set
//    member val Connections = Set<int> with get,set
//    member val Neighbor = ResizeArray<SkillNode>() :> IList<_>
//    // the subset of neighbors with connections to this one
//    member val VisibleNeighbors = ResizeArray<SkillNode>()
//    member val SkillNodeGroup = None with get,set
//    member val A = 0 with get,set
//    member val attributes=Array.empty<string> with get,set
//    member val Da = 0 with get,set
//    member val G = 0 with get,set
//    member val Ia = 0 with get,set
//    member val Icon = String.Empty with get,set
//    member val Id = 0us with get,set
//    member val Type = NodeType.Normal with get,set
//    member val LinkId = ResizeArray<uint16>()
//    member val Name=String.Empty with get,set
//    member val Orbit= 0 with get,set
//    member val Sa = 0 with get,set
//    member val IsSkilled = false with get,set
//    member val Spc = Option<int>.None with get,set
//    member val IsMultipleChoice = false with get,set
//    member val IsMultipleChoiceOption = false with get,set
//    member val passivePointsGranted = 0 with get,set //"passivePointsGranted": 1
//    member val ascendancyName = String.Empty with get,set //"ascendancyName": "Raider"
//    member val IsAscendancyStart = false with get,set //"isAscendancyStart": false
//    member val reminderText = Array.empty<string> with get,set



// based on the javascript object the poe official tree viewer/planner returns

/// things we can read out of the Path of Exile official Passives.json
// things we can read out of the Path of Exile official passive tree javascript
module PassiveJsParsing =
    module Impl =
        //open System.Buffers.Text

        type Node = {
                g:int
                m:bool
                o:int
                da:int
                /// name
                dn:string
                ia:int
                id:int
                ``in``:int list
                ks:bool
                sa:int
                /// skill effects/descriptions
                sd:string list
                ``not``:bool
                ``out``:int list
                /// appears to be related to Scion specialization nodes
                spc: int list
                icon:string
                oidx:int
                isJewelSocket:bool
                ascendancyName:string
                isMultipleChoice:bool
                isAscendancyStart:bool
                /// Scion ascendency option
                passivePointsGranted:int
                /// Scion Specialization nodes
                isMultipleChoiceOption:bool
        } with
            member x.Effects = x.sd
            member x.Name = x.dn
            member x.IsNotable = x.not
            member x.IsKeyStone = x.ks
            member x.IsMastery = x.m
            member x.StrengthAdded = x.sa
            member x.DexterityAdded = x.da
            member x.IntelligenceAdded = x.ia

        [<NoComparison>]
        type PassiveLookup = {
                root:JObject
                max_x:int
                max_y:int
                min_x:int
                min_y:int
                nodes:Dictionary<int,Node>
                groups:JObject
                extraImages:JObject
                // holds base attributes for classes
                characterData:JObject
                // don't want this one for now at least
                //assets:obj
                constants:JObject
        }

        let getMappedNodes rp =
            Impl.getResourcePath "Passives3.5.json" rp
            |> Result.bind(
                IO.File.ReadAllText
                >> SuperSerial.deserialize<PassiveLookup>
            )

        let decodebase64Url (x:string) =
            let partial =
                x
                 .Replace('-','+')
                 .Replace('_','/')
            match partial.Length % 4 with
            | 0 -> partial
            | 2 -> partial + "=="
            | 3 -> partial + "="
            | _ -> invalidArg "x" "Illegal base64url string"
            |> Convert.FromBase64String

        type Payload = {Version:int; CharClass:int; Ascendency:int; FullScreen:int; Nodes:int list}
        // translated from https://github.com/FWidm/poe-profile/blob/master/src/util/tree_codec.py
        // using https://repl.it/r
        // python struct pack/unpack reference: https://docs.python.org/3/library/struct.html
        let decodePayload (payload:byte[]) =
            let end' = if BitConverter.IsLittleEndian then Array.rev else id
            let bconv x = BitConverter.ToInt32(end' x,0)
            let bconv' x = BitConverter.ToUInt16(end' x,0) |> Convert.ToInt32
            let result =
                {
                    //bytes 0-3 contain the version
                    Version = bconv payload.[0..3] //BitConverter.ToInt32((if BitConverter.IsLittleEndian then Array.rev else id )payload.[0..3],0)
                    // bytes 4-6 contain cls, asc, full
                    CharClass= payload.[4] |> Convert.ToInt32
                    Ascendency = payload.[5] |> Convert.ToInt32
                    FullScreen = payload.[6] |> Convert.ToInt32
                    Nodes = Array.chunkBySize 2 payload.[7..] |> Array.map (bconv') |> List.ofArray
                }
            result

        let regPassiveTree =
            function
            | RMatch "AAAA[^?]+" m -> Some m.Value
            | _ -> None

    let mutable nodeCache : Impl.PassiveLookup option= None
    type Tree = {Version:int; Class:ChClass option;Nodes:Impl.Node list}
    let decodeUrl (nodes:IDictionary<int,Impl.Node>) url =
        url
        |> Impl.regPassiveTree
        |> Option.bind(fun x ->
            try
                x
                |> Impl.decodebase64Url
                |> Impl.decodePayload
                |> fun x ->
                    {
                        Version=x.Version
                        Class=
                            match x.CharClass,x.Ascendency with
                            | IsClass x -> Some x
                            | (cls,asc) ->
                                printfn "No class/asc setup for %A" (cls,asc)
                                None
                        Nodes= x.Nodes |> List.map(fun n -> nodes.[n])
                    }
                |> Some
            with ex ->
                printfn "Failed to decodeUrl '%s' '%s'" ex.Message url
                None
        )
    let decodePassives rp url =
        Impl.getMappedNodes rp
        |> Result.bind(fun nc ->
            decodeUrl nc.nodes url
            |> function
                |Some x -> Ok x
                |None -> Result.ErrMsg "Decode Failed"
        )


