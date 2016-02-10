module internal Handlers

open System
open Hornbill
open Context
open Suave

let responseHandler ctx = 
  function 
  | Body(statusCode, body) -> 
    ctx
    |> withStatusCode statusCode
    |> writeResponseBody body
  | StatusCode statusCode -> ctx |> withStatusCode statusCode
  | Headers(statusCode, headers) -> 
    ctx
    |> withStatusCode statusCode
    |> withHeaders headers
  | BodyAndHeaders(statusCode, body, headers) -> 
    ctx
    |> withStatusCode statusCode
    |> withHeaders headers
    |> writeResponseBody body
  | _ -> ctx |> withStatusCode 404

let requestHandler storeRequest findResponse setResponse requestReceived ctx = 
  async {
    let httpRequest = ctx.request
    let methd = Enum.Parse(typeof<Method>, httpRequest.``method`` |> string) :?> Method
    let key = httpRequest.url.PathAndQuery, methd
    let request = httpRequest |> toRequest
    storeRequest request
    requestReceived request
    let response = 
      match findResponse key with
      | Some(Responses(response :: responses)) -> 
        Responses responses |> setResponse key
        response
      | Some(Dlg dlg) -> dlg request
      | Some response -> response
      | _ -> Response.WithStatusCode 404
      |> responseHandler ctx
      |> Some
    return response
  }
