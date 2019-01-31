module PathOfSupporting.Generating.Impl.FsHtml
// https://github.com/y2k/FsHtml/blob/master/FsHtml.fs

open System.Text
open PathOfSupporting.Internal.BReusable.StringHelpers

type Attr = Attr of string * string
type Element = Element of string * Attr list * Element list | Text of string | Comment of string
let toString (e : Element) : string = 
    let sb = StringBuilder()
    let inline append _parent (x:string) =
        sb.Append x |> ignore
    let appendAll parent = Seq.iter (append parent)
    let inline appendLine x = sb.AppendLine x |> ignore

    let rec toString' indent e =
        let offset = String.replicate (2 * indent) " "
        match e with
        | Comment text ->
            [   "<!-- "
                text
                " -->"
            ] |> appendAll e
        | Text text ->
            append e text
        | Element (name, attrs, children) ->
            [offset;"<";name] |> appendAll e
            attrs
            |> List.iter (fun (Attr (k, v)) -> [" ";k;"=\"";v;"\""] |> appendAll e)
            let hasChildren = List.exists (function _ -> true) children
            // scripts must not be self-closing
            let hasElementChildren = List.exists(function |Element _ -> true | _ -> false) children
            let disAllowSelfClose = name="script" || name="a"
            if name="script" && hasElementChildren then
                invalidOp "scripts can't have non-text children"
            if hasChildren || disAllowSelfClose then
                if hasElementChildren then
                    appendLine ">"
                else append e ">"
                children |> List.iter (toString' (indent + 1))
                if hasElementChildren && name <> "script" then append e offset
                sb.Append("</").Append(name).AppendLine(">") |> ignore
            else appendLine " />"
    toString' 0 e
    sb.ToString()
    |> replace "async=\"\"" "async"
    |> replace "<!-->" "<!-- "
    |> replace "</!-->" " -->"
    |> replace "<br></br>" "<br />"
let (%=) name value = Attr (name, value)
let (~%) s = [Text s]
let element (name : string) (attrs : Attr list) (children : Element list) =
    Element (name, attrs, children)
// https://developer.mozilla.org/en-US/docs/Glossary/Empty_element
let empty name attrs = element name attrs List.empty
// Gets from https://www.html-5-tutorial.com/all-html-tags.htm
let a = element "a"
let abbr = element "abbr"
let acronym = element "acronym"
let address = element "address"
let applet = element "applet"
let area = empty "area"
let article = element "article"
let aside = element "aside"
let audio = element "audio"
let b = element "b"
let base' = empty "base"
let basefont = element "basefont"
let bdi = element "bdi"
let bdo = element "bdo"
let big = element "big"
let blockquote = element "blockquote"
let body = element "body"
let br = empty "br"
let button = element "button"
let canvas = element "canvas"
let caption = element "caption"
let center = element "center"
let comment = Comment
let cite = element "cite"
let code = element "code"
let col = empty "col"
let colgroup = element "colgroup"
let command = element "command"
let datalist = element "datalist"
let dd = element "dd"
let del = element "del"
let details = element "details"
let dfn = element "dfn"
let dir = element "dir"
let div = element "div"
let dl = element "dl"
let dt = element "dt"
let em = element "em"
let embed = empty "embed"
let fieldset = element "fieldset"
let figcaption = element "figcaption"
let figure = element "figure"
let font = element "font"
let footer = element "footer"
let form = element "form"
let frame = element "frame"
let frameset = element "frameset"
let h1 = element "h1"
let h2 = element "h2"
let h3 = element "h3"
let h4 = element "h4"
let h5 = element "h5"
let h6 = element "h6"
let head = element "head"
let header = element "header"
let hgroup = element "hgroup"
let hr = empty "hr"
let html = element "html"
let i = element "i"
let iframe = element "iframe"
let img = empty "img"
let input = empty "input"
let ins = element "ins"
let kbd = element "kbd"
let keygen = empty "keygen"
let label = element "label"
let legend = element "legend"
let li = element "li"
let link = empty "link"
let map = element "map"
let mark = element "mark"
let menu = element "menu"
let meta = empty "meta"
let meter = element "meter"
let nav = element "nav"
let noframes = element "noframes"
let noscript = element "noscript"
let object = element "object"
let ol = element "ol"
let optgroup = element "optgroup"
let option = element "option"
let output = element "output"
let p = element "p"
let param = empty "param"
let pre = element "pre"
let progress = element "progress"
let q = element "q"
let rp = element "rp"
let rt = element "rt"
let ruby = element "ruby"
let s = element "s"
let samp = element "samp"
let script = element "script"
let section = element "section"
let select = element "select"
let small = element "small"
let source = empty "source"
let span = element "span"
let strike = element "strike"
let strong = element "strong"
let style = element "style"
let sub = element "sub"
let summary = element "summary"
let sup = element "sup"
let table = element "table"
let tbody = element "tbody"
let td = element "td"
let textarea = element "textarea"
let tfoot = element "tfoot"
let th = element "th"
let thead = element "thead"
let time = element "time"
let title = element "title"
let tr = element "tr"
let track = empty "track"
let tt = element "tt"
let u = element "u"
let ul = element "ul"
let var = element "var"
let video = element "video"
let wbr = empty "wbr"

[<RequireQualifiedAccess>]
module A =
    let id x = "id"%=x
    let href x = "href"%=x
    let src x = "src"%=x
    let title x = "title"%=x
    let className x = "class"%=x
    module Style =
        let hidden = "style"%="visibility:hidden"
module E =
    let meta attr = element "meta" attr List.empty
module Link =
    let css href = link [A.href href; "rel"%="stylesheet";"type"%="text/css"]
module Script =
    let asyncScript src = script ["async"%="";A.src src] []
    let text txt = script [] %(txt)
    // the type attribute is unnecessary for js resources
    let src src = script [A.src src][]
    
//let comment text = element "!--" [] %(text)

