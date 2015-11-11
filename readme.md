# Hornbill

Easy http stubs for integration testing

#### Create a fake service

`var fakeService = new FakeService()`

#### Add some responses

`fakeService.AddResponse` requires a path, a method, and a `Response`

##### Status Code

`fakeService.AddResponse("/foo", Method.GET, Response.WithStatusCode(200))`

##### Body

`fakeService.AddResponse("/foo", Method.GET, Response.WithBody(200, "body"))`

##### Headers

`fakeService.AddResponse("/foo", Method.GET, Response.Headers(200, new[] { new KeyValuePair<string, string>("foo", "bar")}))`

##### Headers and body

`fakeService.AddResponse("/foo", Method.GET, Response.Headers(200, new[] { new KeyValuePair<string, string>("foo", "bar")}, "body"))`

##### Queue of responses

`fakeService.AddResponse("/foo", Method.GET, Response.WithResponses(new [] { Response.WithStatusCode(200), Response.WithStatusCode(500)}))`

##### Delegate

`fakeService.AddResponse("/foo", Method.GET, Response.WithDelegate(x => x.Query["foo"].Contains("bar") ? Response.WithStatusCode(200) : Response.WithStatusCode(404)))`

##### Raw

Requires a string in this format
```
HTTP/1.1 200 OK
foo: bar

Body
```
`fakeService.AddResponse("/foo", Method.GET, Response.WithRawResponse(Resources.rawResponse))`

#### Self hosting
Service will be hosted on a random available port

`var address = fakeService.Host()`

#### Requests
You can examine the requests sent to your service via `fakeService.Requests`