namespace Hornbill.FSharp

open Hornbill

module Response = 
  let withHeaders statusCode (headers : seq<string * string>) = Response.WithHeaders(statusCode, headers |> dict)
  let withBody statusCode body = Response.WithBody(statusCode, body)
  let withStatusCode = Response.WithStatusCode
  let withBodyAndHeaders statusCode body (headers : seq<string * string>) = Response.BodyAndHeaders(statusCode, body, headers)
  let withResponses = Response.WithResponses
  let withDelegate = Response.Dlg