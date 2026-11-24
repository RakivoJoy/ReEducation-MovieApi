using Xunit;

namespace MovieApi.UnitTests;

public class WeatherForecastTests
{
    [Theory]
    [InlineData(-40, -40)]
    [InlineData(0, 32)]
    [InlineData(100, 212)]
    public void TemperatureFConvertsCelsiusToFahrenheit(int celsius, int expectedFahrenheit)
    {
        var forecast = new WeatherForecast { TemperatureC = celsius };

        Assert.Equal(expectedFahrenheit, forecast.TemperatureF);
    }
}
