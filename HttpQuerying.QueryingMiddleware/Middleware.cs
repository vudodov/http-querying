using System;
using System.Buffers;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using DependencyRegistry;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace HttpQuerying.QueryingMiddleware
{
    public class Middleware
    {
        private readonly IRegistry _registry;
        private readonly IMemoryCache _memoryCache;
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            IgnoreNullValues = true,
            PropertyNameCaseInsensitive = false
        };


        public Middleware(RequestDelegate next, IRegistry registry, IMemoryCache memoryCache,
            ILoggerFactory loggerFactory)
        {
            _next = next;
            _registry = registry;
            _memoryCache = memoryCache;
            _logger = loggerFactory.CreateLogger<Middleware>();
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            async Task ExecuteQuery(string queryName)
            {
                Guid queryId = Guid.NewGuid();

                void SetResponse(object result)
                {
                    httpContext.Response.StatusCode = (int) HttpStatusCode.OK;
                    httpContext.Response.ContentType = MediaTypeNames.Application.Json;
                    httpContext.Response.BodyWriter.Write(
                        JsonSerializer.SerializeToUtf8Bytes(result, _jsonSerializerOptions));
                    httpContext.Response.BodyWriter.Complete();
                }

                var theMap = _registry[queryName];

                var response = await QueryHandlerExecutor.Execute(
                    theMap.dependee, theMap.depender, queryId,
                    httpContext.Request.BodyReader, httpContext.RequestServices,
                    httpContext.RequestAborted, _jsonSerializerOptions);

                SetResponse(response);
            }

            var path = httpContext.Request.Path.Value.Split('/', StringSplitOptions.RemoveEmptyEntries);

            if (path[0] == "qry" || path[0] == "query")
                if (HttpMethods.IsGet(httpContext.Request.Method))
                    if (httpContext.Request.ContentType.StartsWith(MediaTypeNames.Application.Json) &&
                        !string.IsNullOrWhiteSpace(path[1]))
                        try
                        {
                            await ExecuteQuery(path[1]);
                        }
                        catch (Exception exception)
                        {
                            _logger.LogError(exception, "Failed execute the query");
                            throw;
                        }
                    else
                    {
                        var exception =
                            new HttpRequestException(
                                "Command content-type must be JSON and path should contain query name");
                        _logger.LogError(exception, "Failed execute query");

                        throw exception;
                    }
                else throw new HttpRequestException("HTTP method should be GET");
            else
                await _next.Invoke(httpContext);
        }
    }
}