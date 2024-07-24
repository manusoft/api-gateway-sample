# API-GATEWAY-SAMPLE
<p align="left"><img src="https://visitor-badge.laobi.icu/badge?page_id=manusoft.api-gateway-sample" alt="visitor" style="max-width: 100%;"></p>
One gateway API calls multiple APIs

``` http
GET https://localhost:7000/
GET https://localhost:7000/api/user
GET https://localhost:7000/api/weather
GET https://localhost:7000/api/aggregate
POST https://localhost:7000/api/account/{email}/{password}
GET https://localhost:7000/api/account/{email}/{password}
```

## 1. Gateway Project

### Ocelot Setup
#### Install nuget package Ocelot and Ocelot.Cache.CacheManager
``` xml
<ItemGroup>
  <PackageReference Include="Ocelot" Version="23.3.3" />
  <PackageReference Include="Ocelot.Cache.CacheManager" Version="23.3.3" />
</ItemGroup>
```

#### launchSettings.json
``` json
...
"https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "",
      "applicationUrl": "https://localhost:7000;http://localhost:5275",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
...
```

#### ocelot.json
``` json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/user",
      "DownstreamScheme": "Http",
      "DownstreamHostAndPorts": [
        {
          "Host": "localhost",
          "Port": 7001
        },
        {
          "Host": "localhost",
          "Port": 7004
        },
        {
          "Host": "localhost",
          "Port": 7005
        }
      ],
      "UpstreamPathTemplate": "/api/user",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ],
      "FileCacheOptions": {
        "TtlSeconds": 60,
        "Region": "default",
        "Header": "OC-Caching-Control",
        "EnableContentHashing": false
      },
      "RateLimitOptions": {
        "ClientWhitelist": [],
        "EnableRateLimiting": true,
        "Period": "60s",
        "PeriodTimespan": 6,
        "Limit": 2
      },
      "Key": "UserService",
      "LoadBalancerOptions": {
        "Type": "LeastConnection"
      }
    },
    {
      "DownstreamPathTemplate": "/api/weatherforecast",
      "DownstreamScheme": "Http",
      "DownstreamHostAndPorts": [
        {
          "Host": "localhost",
          "Port": 7002
        }
      ],
      "UpstreamPathTemplate": "/api/weatherforecast",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ],
      "FileCacheOptions": {
        "TtlSeconds": 60,
        "Region": "default",
        "Header": "OC-Caching-Control",
        "EnableContentHashing": false
      },
      "RateLimitOptions": {
        "ClientWhitelist": [],
        "EnableRateLimiting": true,
        "Period": "60s",
        "PeriodTimespan": 6,
        "Limit": 2
      },
      "Key": "WeatherService"
    },
    {
      "DownstreamPathTemplate": "/api/account/{email}/{password}",
      "DownstreamScheme": "Http",
      "DownstreamHostAndPorts": [
        {
          "Host": "localhost",
          "Port": 7003
        }
      ],
      "UpstreamPathTemplate": "/api/account/{email}/{password}",
      "UpstreamHttpMethod": [ "GET", "POST", "PUT", "DELETE" ]
    }
  ],
  "Aggregates": [
    {
      "RouteKeys": [
        "UserService",
        "WeatherService"
      ],
      "UpstreamPathTemplate": "/api/aggregate"
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "https://localhost:7000"
  }
}
```

#### Program.cs
``` csharp
using Gateway.Middlewares;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Cache.CacheManager;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot().AddCacheManager(options =>
{
    options.WithDictionaryHandle();
});
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

```

### Ocelot Cache Manager Configuration
#### ocelot.json 
``` json
...
"FileCacheOptions": {
  "TtlSeconds": 60,
  "Region": "default",
  "Header": "OC-Caching-Control",
  "EnableContentHashing": false
}
...
```

### Ocelot Rate Limit Configuration
#### ocelot.jso
``` json
...
"RateLimitOptions": {
  "ClientWhitelist": [],
  "EnableRateLimiting": true,
  "Period": "60s",
  "PeriodTimespan": 6,
  "Limit": 2
},
...
```

### Ocelot Agregate Configuration
#### ocelot.json
``` json
...
"Key": "UserService"
...
"Key": "WeatherSearvice"
...
"Aggregates": [
  {
    "RouteKeys": [
      "UserService",
      "WeatherService"
    ],
    "UpstreamPathTemplate": "/api/aggregate"
  }
],
...
```
### Ocelot Load Balance Configuration
#### ocelot.json
``` json
"DownstreamHostAndPorts": [
...
    {
      "Host": "localhost",
      "Port": 7004
    },
    {
      "Host": "localhost",
      "Port": 7005
    }
  ],
...
"LoadBalancerOptions": {
  "Type": "LeastConnection"
}
...
```

### Middelwares
#### InterceptionMiddleware.cs
``` csharp
namespace Gateway.Middlewares;

public class InterceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        context.Request.Headers["Referrer"] = "Api-Gateway";
        await next(context);
    }
}
```
#### TokenCheckerMiddleware.cs
``` csharp
namespace Gateway.Middlewares;

public class TokenCheckerMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        string requestPath = context.Request.Path.Value!;

        if (requestPath.Contains("account/login", StringComparison.InvariantCultureIgnoreCase)
            || requestPath.Contains("account/register", StringComparison.InvariantCultureIgnoreCase)
            || requestPath.Equals("/"))
        {
            await next(context);
        }
        else
        {
            var authHeader = context.Request.Headers.Authorization;

            if (authHeader.FirstOrDefault() == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Sorry, Access denied.");
            }
            else
            {
                await next(context);
            }

        }
    }
}
```
## 2. Shared Project
### Middleware
#### RestrictAccessMiddleware.cs
``` csharp
using Microsoft.AspNetCore.Http;

namespace Shared.Middlewares;

public class RestrictAccessMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var referrer = context.Request.Headers["Referrer"].FirstOrDefault();

        if (string.IsNullOrEmpty(referrer))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Hmmm, Can't reach this page.");
            return;
        }
        else
        {
            await next(context);
        }
    }
}
```
## 3. Identity Project
#### launchSettings.json
``` json
...
 "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:7001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
"https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:7242;http://localhost:7001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
...
```

#### Program.cs
``` csharp
...
app.UseMiddleware<RestrictAccessMiddleware>();
...
```

## 4. Weather Project
#### launchSettings.json
``` json
...
 "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:7002",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
"https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:7194;http://localhost:7002",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
...
```

#### Program.cs
``` csharp
...
app.UseMiddleware<RestrictAccessMiddleware>();
...
```

## 5. Authetication Project
#### launchSettings.json
``` json
...
 "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:7003",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
"https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:7246;http://localhost:7003",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
...
```


#### AppSettings.json
``` json
...
 "Authentication": {
   "Key": "AEkp1N6edDWq7ZBhS9QCtus7+emIxDy0PvoPZxrBmRk=",
   "Issuer": "http://localhost:7003",
   "Audience": "http://localhost:7003"
 }
...
```
#### Program.cs
``` csharp
...
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(builder.Configuration.GetSection("Authentication:Key").Value!);
        string issuer = builder.Configuration.GetSection("Authentication:Issuer").Value!;
        string audience = builder.Configuration.GetSection("Authentication:Audience").Value!;

        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(key),
        };
    });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
    });
});
...
app.UseMiddleware<RestrictAccessMiddleware>();
app.UseCors();
...
```

