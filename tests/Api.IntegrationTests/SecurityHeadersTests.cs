using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Api.IntegrationTests;

[Collection(ApiTestCollection.Name)]
public class SecurityHeadersTests(CustomWebApplicationFactory factory)
{
    [Fact]
    public async Task HttpRequest_RedirectsToHttps()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.TemporaryRedirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Equal("https", response.Headers.Location!.Scheme);
    }
}
