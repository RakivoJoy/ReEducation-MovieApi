using Microsoft.EntityFrameworkCore;
using MovieApi.Data;
using MovieApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<MovieDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("MovieApi")
        ?? throw new InvalidOperationException("The MovieApi connection string is required.");
    options.UseSqlite(connectionString);
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapGet("/", () => Results.Redirect("/openapi/v1.json")).ExcludeFromDescription();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.SeedData();
app.Run();
