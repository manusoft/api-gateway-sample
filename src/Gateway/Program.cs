using Gateway.Middlewares;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddOcelot();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policyBuilder => policyBuilder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors();

app.UseHttpsRedirection();

app.UseMiddleware<TokenCheckerMiddleware>();
app.UseMiddleware<InterceptionMiddleware>();

app.UseAuthorization();

app.UseOcelot().Wait();

app.Run();
