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
using Moq;
using Xunit;

#pragma warning disable 1998

namespace HttpQuerying.Middleware.Tests
{
    public class When_executing_query
    {
        [Fact]
        public async Task It_should_return_results()
        {
            var registryMock = new Mock<IRegistry<IQuery>>();
            registryMock.SetupGet(p => p["test-query"])
                .Returns((query: typeof(TestQuery), queryHandler: typeof(TestQueryHandler)));
            
            var middleware = new QueryingMiddleware.Middleware(
                async context => { },
                registryMock.Object,
                Mock.Of<IMemoryCache>(),
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
            
            var middleware = new QueryingMiddleware.Middleware(
                async context => { },
                registryMock.Object,
                Mock.Of<IMemoryCache>(),
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
    }
}