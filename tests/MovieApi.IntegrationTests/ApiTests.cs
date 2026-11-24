using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MovieApi.Controllers;
using Xunit;

namespace MovieApi.IntegrationTests;

public class ApiTests(WebApplicationFactory<WeatherForecastController> factory)
    : IClassFixture<WebApplicationFactory<WeatherForecastController>>
{
    [Fact]
    public async Task WeatherForecastReturnsFiveForecastsWithoutAuthentication()
    {
        using var client = CreateClient(factory);
        using var response = await client.GetAsync("/WeatherForecast");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var forecasts = await response.Content.ReadFromJsonAsync<WeatherForecast[]>();
        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Length);
        Assert.All(forecasts, forecast => Assert.False(string.IsNullOrWhiteSpace(forecast.Summary)));
    }

    [Fact]
    public async Task RootRedirectsToOpenApiInDevelopment()
    {
        using var client = CreateClient(factory);
        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/openapi/v1.json", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task OpenApiDescribesWeatherForecastInDevelopment()
    {
        using var client = CreateClient(factory);
        using var document = await client.GetFromJsonAsync<JsonDocument>("/openapi/v1.json");

        Assert.NotNull(document);
        var paths = document.RootElement.GetProperty("paths");
        Assert.True(paths.GetProperty("/WeatherForecast").TryGetProperty("get", out _));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/openapi/v1.json")]
    public async Task DevelopmentRoutesReturnNotFoundInProduction(string path)
    {
        using var productionFactory = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = CreateClient(productionFactory);
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static HttpClient CreateClient(WebApplicationFactory<WeatherForecastController> app)
    {
        return app.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }
}
