using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MovieApi.Controllers;
using Xunit;

namespace MovieApi.IntegrationTests;

// The primary constructor receives the class fixture that xUnit creates and shares across these tests.
// WebApplicationFactory locates the API entry point through the controller's assembly and hosts it in memory.
public class ApiTests(WebApplicationFactory<WeatherForecastController> factory)
    : IClassFixture<WebApplicationFactory<WeatherForecastController>>
{
    [Fact]
    public async Task WeatherForecastReturnsFiveForecastsWithoutAuthentication()
    {
        // Arrange: Create an HTTP client without credentials. Using declarations dispose resources at scope exit.
        using var client = CreateClient(factory);

        // Act: Send the request through the real ASP.NET Core routing and middleware pipeline.
        using var response = await client.GetAsync("/WeatherForecast");

        // Assert: Check HTTP 200 before deserializing the response body into the forecast DTO array.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var forecasts = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();
        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Length);
        // Assert each randomly generated forecast has a summary without depending on its exact value.
        Assert.All(forecasts, forecast => Assert.False(string.IsNullOrWhiteSpace(forecast.Summary)));
    }

    [Fact]
    public async Task RootRedirectsToOpenApiInDevelopment()
    {
        // Arrange: The test host defaults to Development. The client exposes redirects.
        using var client = CreateClient(factory);

        // Act: Request the API root.
        using var response = await client.GetAsync("/");

        // Assert: HTTP 302 and the Location header together describe the redirect.
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        // The null-conditional operator lets a missing Location header produce an assertion failure.
        Assert.Equal("/openapi/v1.json", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task OpenApiDescribesWeatherForecastInDevelopment()
    {
        // Arrange: Use the Development test host.
        using var client = CreateClient(factory);

        // Act: GetFromJsonAsync checks for HTTP success and parses the body into a disposable JSON document.
        using var document = await client.GetFromJsonAsync<JsonDocument>("/openapi/v1.json");

        // Assert: OpenAPI lists route templates under paths and HTTP methods under each route.
        Assert.NotNull(document);
        var paths = document.RootElement.GetProperty("paths");
        // The discard (out _) ignores the operation value because this test checks only its presence.
        Assert.True(paths.GetProperty("/WeatherForecast").TryGetProperty("get", out _));
    }

    [Theory] 
    [InlineData("/")]
    [InlineData("/openapi/v1.json")]
    public async Task DevelopmentRoutesReturnNotFoundInProduction(string path)
    {
        // Arrange: Create a separate factory so the Production environment applies to this host only.
        using var productionFactory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = CreateClient(productionFactory);

        // Act: Request the route supplied by the current theory case.
        using var response = await client.GetAsync(path);

        // Assert: HTTP 404 confirms the route is unavailable in Production.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static HttpClient CreateClient(WebApplicationFactory<WeatherForecastController> app)
    {
        return app.CreateClient(new WebApplicationFactoryClientOptions
        {
            // Use the HTTPS scheme so the middleware sees a secure request in the in-memory host.
            BaseAddress = new Uri("https://localhost"),
            // Keep the initial response so the test can inspect its status code and Location header.
            AllowAutoRedirect = false
        });
    }
}
