using System;
using System.Buffers;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using DependencyRegistry;
using HttpQuerying.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using MiddlewareMethodExecutor;

namespace HttpQuerying.QueryingMiddleware
{
    public class CacheMiddleware
    {
        private readonly IRegistry<IQuery> _registry;
        private readonly IMemoryCache _memoryCache;
        private readonly MemoryCacheEntryOptions _cacheOptions;
        private readonly Func<string, IQuery, object> _getCacheKey;
        private readonly Func<object, string> _etagCacheComputation;
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            IgnoreNullValues = true,
            PropertyNameCaseInsensitive = false
        };


        public CacheMiddleware(RequestDelegate next, IRegistry<IQuery> registry, IMemoryCache memoryCache,
            ILoggerFactory loggerFactory, MemoryCacheEntryOptions cacheOptions,
            Func<string, IQuery, object> getCacheKey,
            Func<object, string> etagCacheComputation)
        {
            _next = next;
            _registry = registry;
            _memoryCache = memoryCache;
            _cacheOptions = cacheOptions;
            _getCacheKey = getCacheKey;
            _etagCacheComputation = etagCacheComputation;
            _logger = loggerFactory.CreateLogger<CacheMiddleware>();
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            async Task ExecuteQuery(string queryName)
            {
                Guid queryId = Guid.NewGuid();

                void SetResponse(object queryResult)
                {
                    var checksum = _etagCacheComputation(queryResult);

                    if (httpContext.Request.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var etag) &&
                        checksum == etag)
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status304NotModified;
                        return;
                    }

                    httpContext.Response.Headers[HeaderNames.ETag] = checksum;
                    httpContext.Response.StatusCode = (int) HttpStatusCode.OK;
                    httpContext.Response.ContentType = MediaTypeNames.Application.Json;
                    httpContext.Response.BodyWriter.Write(
                        JsonSerializer.SerializeToUtf8Bytes(new HttpQueryResult
                        {
                            QueryId = queryId,
                            Result = queryResult
                        }, typeof(HttpQueryResult), _jsonSerializerOptions));
                    httpContext.Response.BodyWriter.Complete();
                }

                var (dependee, depender) = _registry[queryName];

                var (message, handleQuery) = await Executor
                    .ExecuteAsyncMethod(dependee, depender, "HandleAsync",
                        httpContext.Request.BodyReader, httpContext.RequestServices, _jsonSerializerOptions,
                        httpContext.RequestAborted, queryId);

                if (_memoryCache.TryGetValue(_getCacheKey(queryName, (IQuery) message), out object cachedResult))
                    SetResponse(cachedResult);
                else
                {
                    var queryResult = await handleQuery();
                    var cacheEntry = _memoryCache.CreateEntry(_getCacheKey(queryName, (IQuery) message));
                    cacheEntry.Value = queryResult;
                    cacheEntry.SetOptions(_cacheOptions);

                    SetResponse(queryResult);
                }
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