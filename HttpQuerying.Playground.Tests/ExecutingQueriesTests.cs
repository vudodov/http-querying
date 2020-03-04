using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HttpQuerying.Playground.Tests
{
    public class ExecutingQueriesTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly HttpClient _client;

        public ExecutingQueriesTests(WebApplicationFactory<Startup> factory)
        {
            _client = factory.CreateClient(new WebApplicationFactoryClientOptions {AllowAutoRedirect = false});
        }

        [Fact]
        public async Task ExecuteQuerySuccessfully()
        {
            var query = new HttpRequestMessage(HttpMethod.Get, "/query/get-all-potatoes-query");
            query.Content = new StringContent("", Encoding.UTF8,MediaTypeNames.Application.Json);
            
            var responseMessage = await _client.SendAsync(query);
            
            responseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}