using Microsoft.EntityFrameworkCore;
using MovieApi.Data;
using MovieApi.Extensions;
using MovieApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Use the application's configured SQL Server database for each scoped context.

builder.Services.AddControllers();
builder.Services.AddScoped<CastAssignmentService>();
builder.Services.AddDbContext<MovieDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("MovieApi")
        ?? throw new InvalidOperationException("The MovieApi connection string is required.");
    options.UseSqlServer(connectionString);
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseHttpsRedirection();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Movie API v1"));
    app.MapGet("/", () => Results.Redirect("/swagger/index.html")).ExcludeFromDescription();
}

app.UseAuthorization();

app.MapControllers();

// Complete optional development seeding before the application accepts requests.
app.SeedData();
app.Run();
