module PathOfSupporting.LogWatching
open System
open System.IO
(* TODO: auto location of poe log file method 
    let findLogFile hint =

*)

[<NoComparison>]
type WatchWrapper = private {Watcher:FileSystemWatcher;mutable IsDisposed:bool;Disposables:IDisposable list}
    with
        member x.Dispose() =
            if not x.IsDisposed then
                x.Disposables
                |> List.iter (fun x -> x.Dispose())
                x.Watcher.Dispose()
                x.IsDisposed <- true
        member x.WaitForChanged wct = x.Watcher.WaitForChanged(wct)
        member x.WaitForChanged (wct,timeout) = x.Watcher.WaitForChanged(wct,timeout)
        interface IDisposable with
            member x.Dispose() = x.Dispose()

/// concerns: https://stackoverflow.com/questions/239988/filesystemwatcher-vs-polling-to-watch-for-file-changes
let watchChanges path f =
    if File.Exists path then
        Some <| new FileSystemWatcher(path=Path.GetDirectoryName path,filter=Path.GetFileName path)
    else if Directory.Exists path then
        Some <| new FileSystemWatcher(path=path)
    else None
    |> Option.map(fun fsw ->
        let disposables = [
            fsw.Changed.Subscribe(fun x -> f x)
        ]
        fsw.EnableRaisingEvents <- true
        {Watcher=fsw;IsDisposed=false; Disposables=disposables}
    )
