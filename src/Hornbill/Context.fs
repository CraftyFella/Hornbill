module internal Context

open System
open Hornbill
open Suave
open Suave
open Suave.Http

let toMethod m = Enum.Parse(typeof<Method>, m) :?> Method

let writeResponseBody (body : string) ctx =
  { ctx with response = {ctx.response with content = System.Text.Encoding.UTF8.GetBytes body |> HttpContent.Bytes}}

let toRequest request = 
  { Method = 
      request.``method``
      |> string
      |> toMethod
    Path = request.url.AbsolutePath
    Body = request.rawForm |> System.Text.Encoding.UTF8.GetString
    Headers = request.headers |> dict
    Query = 
      request.query
      |> List.filter (fun (_, v) -> v.IsSome)
      |> List.map (fun (k, v) -> k, v.Value)
      |> dict }

let withStatusCode statusCode ctx =
  let statusCode = HttpCode.tryParse statusCode
  match statusCode with
  | Choice1Of2 statusCode -> { ctx with response = { ctx.response with status = statusCode ; content = HttpContent.Bytes [||] } }
  | Choice2Of2 msg -> failwith msg

let withHeaders headers (ctx : HttpContext) = { ctx with response = { ctx.response with headers = headers |> List.ofSeq } }
