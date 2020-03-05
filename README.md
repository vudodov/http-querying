[![Nuget](https://img.shields.io/nuget/v/http-querying-middleware)](https://www.nuget.org/packages/http-querying-middleware/)

# HTTP Querying Middleware
This is an easy-to-hook-up high-performance middleware for Web Applications aiming to implement the CQRS pattern. The Middleware provides tooling that will make your Web Application set up seamless and development efficient.
The Middleware is aiming to provide a framework for the Querying part of the CQRS pattern. 

The [Commanding middleware](https://github.com/vudodov/http-commanding) is available as a [separate package](https://www.nuget.org/packages/http-commanding-middleware/). Take adventage of the physical segregation. E.g. scale commands and queries independently.

## Usage
All you need to do is register the middleware in your [middleware pipeline](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-3.1), add some queries and that's it.

Spin up the project and hit the localhost with `query/<query-name>` add query data to the HTTP Request body and your query will be deserialized and delivered directly to the handler, once the query is executed and the result is formed, it will be delivered back to you in a JSON shape. For your convenience, you can find a ready-to-go playground inside the repository.

## The Playground
Inside of the repository you can find [the Playground](https://github.com/vudodov/http-querying/tree/master/HttpQuerying.Playground). This is essentially a web application with a couple of preset queries and handlers for you to get quick hands-on experience.
And [Some Tests](https://github.com/vudodov/http-querying/tree/master/HttpQuerying.Playground.Tests) that will spin up a test application server and emulate client requests.

## Under the hood

The framework consists of two main packages [the infrastructure](https://github.com/vudodov/http-querying/packages/141806) and [the middleware](https://github.com/vudodov/http-querying/packages/144615).

### The Infrastructure

#### Queries and Handlers

The Infrastructure provides an `IQuery` interface to identify your query data class
```csharp
public class GetSomePotatoesQuery : IQuery
{
    public int Amount { get; set; }
}
```
And `IQueryHandler<IQuery, TQueryResult>` interface to tie together query data, query result and query handling functionality. The rest will be done automatically.
```csharp
public class GetSomePotatoesQueryHandler : IQueryHandler<GetSomePotatoesQuery, List<Potato>>
{
    private readonly IBagWithPotatoes _bag;
    
    public GetSomePotatoesQueryHandler(IBagWithPotatoes bag)
    {
        _bag = bag;
    }
    
    public Task<List<Potato>> HandleAsync(GetSomePotatoesQuery query, Guid queryId, CancellationToken token)
    {
        List<Potato> potatoes = await _bag.GetPotatoesAsync(query.Amount);
        
        return potatoes;
    }
}
```

Once the query will hit the middleware it will be delivered to the proper handler. In the example above, once the `GetSomePotatoesQuery` is received it will be delivered to the handler, which will try to pull the required amount of potatoes from the bag and return whatever it managed to get.

### The Middleware

The middleware is very easy to set up. Just register the commanding middleware in the `Startup.cs` file. And you are ready to go. 
```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddHttpQuerying();
    ...
}

...

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    ...
    app.UseHttpQuerying();
    ...
}
```

#### Query Registry

By default, the Middleware will register all the queries and handlers in your current web project. If you'd like to store queries and handlers in other projects, you can easily configure that by passing assemblies where those queries and handlers are defined. Just as the following example does.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    var potatoesAssembly = typeof(GetSomePotatoesQuery).Assembly;
    var beerAssembly = typeof(GetSomeBeersQuery).Assembly;
    
    services.AddHttpCommanding(potatoesAssembly, beerAssembly);
    ...
}
```

#### Sending Queries

To hit the middleware with a query all you need is [properly constructed HTTP request](
https://valerii-udodov.com/2020/02/19/cqrs-querying-via-http/). 
The request should have 
 - a `GET` request type;
 - URI as following `http://<your-host>/query/<query-name>`;
 - `content-type` header set to `application/json`;
 Don't forget to include query payload in the request body ;)

![query-processing-image](/images/query-request-response-flow.png)

#### Caching

If required, out of the box, the Middleware supports two types of caching.

##### Runtime In-Memory Caching
This is [very common type of caching](https://docs.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-3.1), which is essentially a Hash Table.

Good for query(s) that will be executed frequently, but the result won't change every execution.

##### Caching of unchanged resource
Every response will contain [ETag](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/ETag). And the Middleware supports [If-None-Match](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/If-None-Match) header.

Good for all queries, to reduce network load and unnecessary UI re-rendering.

##### Configuration

By default, the Middleware won't use any of supported caching.
To enable caching use following configuration.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddHttpQuerying();
    ...
}

...

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    ...
    app.UseHttpQuerying(_cacheOptions, _getCacheKey, _etagCacheComputation);
    ...
}
```

The `UseHttpQuerying` will accept three parameters. And provides to you full control over the cache.
 1. Cache Options (`MemoryCacheEntryOptions`)- is [set of options](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.memorycacheentryoptions?view=dotnet-plat-ext-3.1) to configure in-memory caching. Please note, by default no size or expiration is configured, so I'd highly recommend to look into [SetSlidingExpiration](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.memorycacheentryextensions.setslidingexpiration?view=dotnet-plat-ext-3.1) and [SetSize](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.caching.memory.memorycacheentryextensions.setsize?view=dotnet-plat-ext-3.1) methods.
 2. Get Cache Key (`Func<string, IQuery, object>`)- is a delegate that defines how to calculate the in-memory cache key. It will receive the query name and the query data object. It should return an object which will be used as a query key.
 3. ETag Cache Computation (`Func<object, string>`)- is a delegate that defines how to calculate the [ETag](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/ETag) value which will be added to the response headers. It will receive the query result data object as an input and need to compute an ETag value that will represent a checksum of the query result.

#### Dependency Injection

Dependency injection for query handlers works out of the box as you would expect it to.
Everything that you've registered during application startup will be available in all your handlers via dependency injection, in the same way as it would be if you'd use controllers.

#### Query Id

For the tracing purposes, each query handler will receive a unique query id. The same query id will be added to the HTTP response automatically.

#### Responses

In case of success, the caller will receive `200 OK` Response Code with query id and query results as JSON.

__________________________
Any questions or problems, just add an issue.
