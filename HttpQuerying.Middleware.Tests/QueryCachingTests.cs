using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DependencyRegistry;
using FluentAssertions;
using HttpQuerying.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace HttpQuerying.Middleware.Tests
{
    public class When_executing_query_with_caching
    {
                [Fact]
        public async Task It_should_return_results()
        {
            var registryMock = new Mock<IRegistry<IQuery>>();
            registryMock.SetupGet(p => p["test-query"])
                .Returns((query: typeof(TestQuery), queryHandler: typeof(TestQueryHandler)));
            
            // Memory cache setup
            var memoryCacheMock = new Mock<IMemoryCache>();
            object outValue;
            memoryCacheMock.Setup(memoryCache => memoryCache.TryGetValue(It.IsAny<object>(), out outValue))
                .Returns(false);
            memoryCacheMock.Setup(memoryCache => memoryCache.CreateEntry(It.IsAny<object>()))
                .Returns(Mock.Of<ICacheEntry>());
            
            var middleware = new QueryingMiddleware.Middleware(
                async context => { },
                registryMock.Object,
                memoryCacheMock.Object,
                Mock.Of<ILoggerFactory>());
            
            var bodyRequestStream = new MemoryStream();
            var bodyResponseStream = new MemoryStream();

            var httpContext = new DefaultHttpContext(new FeatureCollection
            {
                [typeof(IHttpResponseBodyFeature)] = new StreamResponseBodyFeature(bodyResponseStream),
                [typeof(IHttpResponseFeature)] = new HttpResponseFeature(),
                [typeof(IHttpRequestFeature)] = new HttpRequestFeature
                {
                    Body = bodyRequestStream,
                    Path = "/query/test-query",
                    Method = HttpMethods.Get
                }
            });

            httpContext.Request.ContentType = MediaTypeNames.Application.Json;

            await middleware.InvokeAsync(httpContext);

            bodyResponseStream.Position = 0;
            var bodyContent = await new StreamReader(bodyResponseStream).ReadToEndAsync();
            
            httpContext.Response.StatusCode.Should().Be((int) HttpStatusCode.OK);
            
            var jsonBody = JsonDocument.Parse(bodyContent).RootElement;
            jsonBody
                .GetProperty("queryId").GetGuid()
                .Should().NotBeEmpty();
            
            var resultJson = jsonBody.GetProperty("result");
            resultJson.GetProperty("stringProp").GetString().Should().Be("TestString");
            resultJson.GetProperty("intProp").GetInt32().Should().Be(12);
            resultJson.GetProperty("objectProp").GetProperty("stringProp").GetString().Should().Be("AnotherString");
            resultJson.GetProperty("objectProp").GetProperty("intProp").GetInt32().Should().Be(1);
        }
        
        [Fact]
        public async Task It_should_respect_query_data()
        {
            var registryMock = new Mock<IRegistry<IQuery>>();
            registryMock.SetupGet(p => p["test-condition-query"])
                .Returns((query: typeof(TestConditionQuery), queryHandler: typeof(TestConditionQueryHandler)));
            
            // Memory cache setup
            var memoryCacheMock = new Mock<IMemoryCache>();
            object outValue;
            memoryCacheMock.Setup(memoryCache => memoryCache.TryGetValue(It.IsAny<object>(), out outValue))
                .Returns(false);
            memoryCacheMock.Setup(memoryCache => memoryCache.CreateEntry(It.IsAny<object>()))
                .Returns(Mock.Of<ICacheEntry>());
            
            var middleware = new QueryingMiddleware.Middleware(
                async context => { },
                registryMock.Object,
                memoryCacheMock.Object,
                Mock.Of<ILoggerFactory>());
            
            var bodyRequestStream = new MemoryStream();
            var bodyResponseStream = new MemoryStream();
            await bodyRequestStream.WriteAsync(Encoding.UTF8.GetBytes(@"{""flag"": true }"));
            bodyRequestStream.Seek(0, SeekOrigin.Begin);

            var httpContext = new DefaultHttpContext(new FeatureCollection
            {
                [typeof(IHttpResponseBodyFeature)] = new StreamResponseBodyFeature(bodyResponseStream),
                [typeof(IHttpResponseFeature)] = new HttpResponseFeature(),
                [typeof(IHttpRequestFeature)] = new HttpRequestFeature
                {
                    Body = bodyRequestStream,
                    Path = "/query/test-condition-query",
                    Method = HttpMethods.Get
                },
            });

            httpContext.Request.ContentType = MediaTypeNames.Application.Json;

            await middleware.InvokeAsync(httpContext);

            bodyResponseStream.Position = 0;
            var bodyContent = await new StreamReader(bodyResponseStream).ReadToEndAsync();
            
            httpContext.Response.StatusCode.Should().Be((int) HttpStatusCode.OK);
            
            var jsonBody = JsonDocument.Parse(bodyContent).RootElement;
            jsonBody
                .GetProperty("queryId").GetGuid()
                .Should().NotBeEmpty();
            
            var resultJson = jsonBody.GetProperty("result");
            resultJson.GetProperty("flag").GetBoolean().Should().Be(true);
        }
        
        [Fact]
        public async Task It_should_return_etag()
        {
            var registryMock = new Mock<IRegistry<IQuery>>();
            registryMock.SetupGet(p => p["test-query"])
                .Returns((query: typeof(TestQuery), queryHandler: typeof(TestQueryHandler)));

            // Memory cache setup
            var memoryCacheMock = new Mock<IMemoryCache>();
            object outValue;
            memoryCacheMock.Setup(memoryCache => memoryCache.TryGetValue(It.IsAny<object>(), out outValue))
                .Returns(true);

            var middleware = new QueryingMiddleware.CacheMiddleware(
                async context => { },
                registryMock.Object,
                memoryCacheMock.Object,
                Mock.Of<ILoggerFactory>(), new MemoryCacheEntryOptions(), 
                (_, __) => "key", 
                _ => "etag_checksum");
            
            var httpContext = new DefaultHttpContext();

            httpContext.Request.Path = "/query/test-query";
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Request.ContentType = MediaTypeNames.Application.Json;

            await middleware.InvokeAsync(httpContext);

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            httpContext.Response.Headers[HeaderNames.ETag].ToString().Should().Be("etag_checksum");
        }

        [Fact]
        public async Task It_should_return_304_if_query_result_was_not_changed()
        {
            var registryMock = new Mock<IRegistry<IQuery>>();
            registryMock.SetupGet(p => p["test-query"])
                .Returns((query: typeof(TestQuery), queryHandler: typeof(TestQueryHandler)));
            
            // Memory cache setup
            var memoryCacheMock = new Mock<IMemoryCache>();
            object outValue;
            memoryCacheMock.Setup(memoryCache => memoryCache.TryGetValue(It.IsAny<object>(), out outValue))
                .Returns(true);

            var middleware = new QueryingMiddleware.CacheMiddleware(
                async context => { },
                registryMock.Object,
                memoryCacheMock.Object,
                Mock.Of<ILoggerFactory>(), new MemoryCacheEntryOptions(), 
                (_, __) => "key", 
                _ => "etag_checksum");
            
            var httpContext = new DefaultHttpContext();

            httpContext.Request.Path = "/query/test-query";
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Request.ContentType = MediaTypeNames.Application.Json;
            httpContext.Request.Headers[HeaderNames.IfNoneMatch] = "etag_checksum";

            await middleware.InvokeAsync(httpContext);

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status304NotModified);
        }
        
        [Fact]
        public async Task It_should_return_cached_value()
        {
            var registryMock = new Mock<IRegistry<IQuery>>();
            registryMock.SetupGet(p => p["test-query"])
                .Returns((query: typeof(TestQuery), queryHandler: typeof(TestQueryHandler)));
            
            var memoryCacheMock = new Mock<IMemoryCache>();
            object outValue = new StringQueryResult {StringProp = "test value"};
            bool isCreateMemoCacheEntryCalled = false;
            memoryCacheMock.Setup(memoryCache => memoryCache.TryGetValue("key", out outValue))
                .Returns(true);

            var middleware = new QueryingMiddleware.CacheMiddleware(
                async context => { },
                registryMock.Object,
                memoryCacheMock.Object,
                Mock.Of<ILoggerFactory>(), new MemoryCacheEntryOptions(), 
                (_, __) => "key", 
                _ => "etag_checksum");
            
            var bodyRequestStream = new MemoryStream();
            var bodyResponseStream = new MemoryStream();
            var httpContext = new DefaultHttpContext(new FeatureCollection
            {
                [typeof(IHttpResponseBodyFeature)] = new StreamResponseBodyFeature(bodyResponseStream),
                [typeof(IHttpResponseFeature)] = new HttpResponseFeature(),
                [typeof(IHttpRequestFeature)] = new HttpRequestFeature
                {
                    Body = bodyRequestStream,
                    Path = "/query/test-query",
                    Method = HttpMethods.Get
                },
            });

            httpContext.Request.Path = "/query/test-query";
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Request.ContentType = MediaTypeNames.Application.Json;

            await middleware.InvokeAsync(httpContext);
            
            bodyResponseStream.Position = 0;
            var bodyContent = await new StreamReader(bodyResponseStream).ReadToEndAsync();

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            var jsonBody = JsonDocument.Parse(bodyContent).RootElement;
            jsonBody
                .GetProperty("queryId").GetGuid()
                .Should().NotBeEmpty();
            
            var resultJson = jsonBody.GetProperty("result");
            resultJson.GetProperty("stringProp").GetString().Should().Be("test value");
            isCreateMemoCacheEntryCalled.Should().BeFalse();
        }
        
        [Fact]
        public async Task It_should_add_query_result_to_the_cache()
        {
            var registryMock = new Mock<IRegistry<IQuery>>();
            registryMock.SetupGet(p => p["test-query"])
                .Returns((query: typeof(TestQuery), queryHandler: typeof(TestQueryHandler)));
            
            // Memory cache setup
            var memoryCacheMock = new Mock<IMemoryCache>();
            object outValue;
            bool isCreateMemoCacheEntryCalled = false;
            memoryCacheMock.Setup(memoryCache => memoryCache.TryGetValue(It.IsAny<object>(), out outValue))
                .Returns(false);
            memoryCacheMock.Setup(memoryCache => memoryCache.CreateEntry(It.IsAny<object>()))
                .Callback(() => isCreateMemoCacheEntryCalled = true)
                .Returns(Mock.Of<ICacheEntry>());

            var middleware = new QueryingMiddleware.CacheMiddleware(
                async context => { },
                registryMock.Object,
                memoryCacheMock.Object,
                Mock.Of<ILoggerFactory>(), new MemoryCacheEntryOptions(), 
                (_, __) => "key", 
                _ => "etag_checksum");
            
            var bodyRequestStream = new MemoryStream();
            var bodyResponseStream = new MemoryStream();
            var httpContext = new DefaultHttpContext(new FeatureCollection
            {
                [typeof(IHttpResponseBodyFeature)] = new StreamResponseBodyFeature(bodyResponseStream),
                [typeof(IHttpResponseFeature)] = new HttpResponseFeature(),
                [typeof(IHttpRequestFeature)] = new HttpRequestFeature
                {
                    Body = bodyRequestStream,
                    Path = "/query/test-query",
                    Method = HttpMethods.Get
                },
            });

            httpContext.Request.Path = "/query/test-query";
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Request.ContentType = MediaTypeNames.Application.Json;

            await middleware.InvokeAsync(httpContext);
            
            bodyResponseStream.Position = 0;
            var bodyContent = await new StreamReader(bodyResponseStream).ReadToEndAsync();

            httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
            var jsonBody = JsonDocument.Parse(bodyContent).RootElement;
            jsonBody
                .GetProperty("queryId").GetGuid()
                .Should().NotBeEmpty();
            
            var resultJson = jsonBody.GetProperty("result");
            resultJson.GetProperty("stringProp").GetString().Should().Be("TestString");
            isCreateMemoCacheEntryCalled.Should().BeTrue();
        }
    }
}