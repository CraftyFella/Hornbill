﻿namespace Hornbill

open System
open System.Net.Sockets
open System.Net
open System.Collections.Generic
open System.Text.RegularExpressions
open System.IO
open Suave
open System.Threading

type FakeService(port) = 
  let responses = Dictionary<string * Method, Response>()
  let requests = ResizeArray<Request>()
  let tryFindKey path methd = 
    responses.Keys |> Seq.tryFind (fun (p, m) -> m = methd && Regex.IsMatch(path, p, RegexOptions.IgnoreCase))
  let mutable url = ""
  let requestReceived = Event<Request>()
  
  let findResponse (path, methd) = 
    let path = 
      if Regex.IsMatch(path, ":/[^/]") then path.Replace(":/", "://")
      else path
    match tryFindKey path methd with
    | Some key -> Some responses.[key]
    | _ -> None
  
  let setResponse (path, methd) response = 
    match tryFindKey path methd with
    | Some key -> responses.[key] <- response
    | _ -> ()
  
  let findPort() = 
    TcpListener(IPAddress.Loopback, 0) |> fun l -> 
      l.Start()
      (l, (l.LocalEndpoint :?> IPEndPoint).Port) |> fun (l, p) -> 
        l.Stop()
        p
  
  let port = 
    if port = 0 then findPort()
    else port
  
  let mutable webApp = 
    { new IDisposable with
        member __.Dispose() = () }
  
  new() = new FakeService 0
  member __.OnRequestReceived(f : Action<Request>) = requestReceived.Publish.Add f.Invoke
  
  member __.AddResponse (path : string) verb response = 
    let formatter : Printf.StringFormat<_> = 
      match path.StartsWith "/", path.EndsWith "$" with
      | false, false -> "/%s$"
      | false, true -> "/%s"
      | true, false -> "%s$"
      | _ -> "%s"
    responses.Add((sprintf formatter path, verb), response)
  
  member this.AddResponsesFromText text = 
    for parsedRequest in ResponsesParser.parse text do
      let response = ResponsesParser.mapToResponse parsedRequest
      this.AddResponse parsedRequest.Path parsedRequest.Method response
  
  member this.AddResponsesFromFile filePath = File.ReadAllText filePath |> this.AddResponsesFromText
  
  member __.Url = 
    match url with
    | "" -> failwith "Service not started"
    | _ -> url
  
  member this.Uri = Uri this.Url
  
  member __.Start() =
    let cts = new CancellationTokenSource()
    
    let serverConfig = 
      { defaultConfig with bindings = [ HttpBinding.mkSimple HTTP "0.0.0.0" port ]
                           cancellationToken = cts.Token }
    
    let l, s = 
      startWebServerAsync serverConfig (Handlers.requestHandler requests.Add findResponse setResponse requestReceived.Trigger)
    Async.Start s
    Async.RunSynchronously l |> ignore
    webApp <- { new IDisposable with
                  member __.Dispose() = cts.Cancel() }
    url <- sprintf "http://localhost:%i" port
    url
  
  member __.Stop() = webApp.Dispose()
  member __.Requests = requests
  interface IDisposable with
    member this.Dispose() = this.Stop()
