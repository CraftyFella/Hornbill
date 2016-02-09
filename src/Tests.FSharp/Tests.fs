﻿module Tests

open Hornbill.FSharp
open System.Net.Http
open System.Net
open Hornbill
open System.Threading
open ResponsesParser

let createFakeService() = 
  let fakeService = new FakeService()
  fakeService.Start() |> ignore
  let httpClient = new HttpClient(BaseAddress = fakeService.Uri)
  fakeService, httpClient

[<Test>]
let body() = 
  let fakeService, httpClient = createFakeService()
  Response.withBody 200 "foo" |> fakeService.AddResponse "/foo" Method.GET
  httpClient.GetStringAsync("/foo").Result == "foo"
  fakeService.Stop()

[<Test>]
let headers() = 
  let fakeService, httpClient = createFakeService()
  Response.withHeaders 200 [ "foo", "bar" ] |> fakeService.AddResponse "/foo" Method.GET
  let response = httpClient.GetAsync("/foo").Result
  (response.Headers |> Seq.head).Key == "foo"
  (response.Headers |> Seq.head).Value == [| "bar" |]
  fakeService.Stop()

[<Test>]
let dlg() = 
  let fakeService, httpClient = createFakeService()
  Response.withDelegate (fun _ -> Response.withStatusCode 200) |> fakeService.AddResponse "/foo" Method.GET
  httpClient.GetAsync("/foo").Result.StatusCode == HttpStatusCode.OK
  fakeService.Stop()

[<Test>]
let evnt() = 
  let fakeService, httpClient = createFakeService()
  Response.WithStatusCode 200 |> fakeService.AddResponse "/foo" Method.GET
  let autoResetEvent = new AutoResetEvent false
  fakeService.OnRequestReceived(fun x -> 
    if x.Path = "/foo" then autoResetEvent.Set() |> ignore)
  httpClient.GetAsync("/foo").Result.StatusCode == HttpStatusCode.OK
  autoResetEvent.WaitOne 1000 == true
  fakeService.Stop()

[<TestCase("GET")>]
[<TestCase("POST")>]
[<TestCase("PUT")>]
[<TestCase("OPTIONS")>]
[<TestCase("HEAD")>]
[<TestCase("DELETE")>]
[<TestCase("TRACE")>]
let ``parsing responses supports all methods`` methd = 
  let text = """
GET statuscode
200
"""
  let text = text.Replace("GET", methd)
  let fakeService, httpClient = createFakeService()
  fakeService.AddResponsesFromText text
  let response = (new HttpRequestMessage(HttpMethod methd, "statuscode") |> httpClient.SendAsync).Result
  response.StatusCode == HttpStatusCode.OK

[<Test>]
let ``throws when parsing responses with invalid method``() = 
  let text = "FOO path"
  let fakeService = new FakeService()
  assertThrows<InvalidMethodAndPath> (fun () -> fakeService.AddResponsesFromText text) "FOO path"

[<Test>]
let ``throws when parsing responses with invalid path``() = 
  let text = "GET"
  let fakeService = new FakeService()
  assertThrows<InvalidMethodAndPath> (fun () -> fakeService.AddResponsesFromText text) "FOO path"

[<Test>]
let ``does not throw when parsing empty responses``() = 
  let fakeService = new FakeService()
  assertDoesNotThrow (fun () -> fakeService.AddResponsesFromText "")

[<Test>]
let ``throws when parsing responses with invalid status code``() = 
  let text = """
GET path
foo
"""
  let fakeService = new FakeService()
  assertThrows<InvalidStatusCode> (fun () -> fakeService.AddResponsesFromText text) "foo"

[<Test>]
let ``throws when parsing responses with invalid header``() = 
  let text = """
GET path
200
foo
"""
  let fakeService = new FakeService()
  assertThrows<InvalidHeader> (fun () -> fakeService.AddResponsesFromText text) "foo"
