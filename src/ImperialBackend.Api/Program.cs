using CData.EntityFrameworkCore.Databricks;
using FluentValidation;
using FluentValidation.AspNetCore;
using ImperialBackend.Api.Middleware;
using ImperialBackend.Application.Common.Mappings;
using ImperialBackend.Application.Outlets.Commands.CreateOutlet;
using ImperialBackend.Domain.Interfaces;
using ImperialBackend.Infrastructure.Data;
using ImperialBackend.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;


var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Add API Explorer for Swagger
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with Azure AD authentication
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Imperial Backend API",
        Version = "v1",
        Description = "API for managing outlets and business operations with SSO token validation for 360 Salesforce integration",
        Contact = new OpenApiContact
        {
            Name = "Imperial Backend Team",
            Email = "support@imperialbackend.com"
        }
    });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    // Add JWT Authentication to Swagger
    //c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    //{
    //    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
    //    Name = "Authorization",
    //    In = ParameterLocation.Header,
    //    Type = SecuritySchemeType.ApiKey,
    //    Scheme = "Bearer",
    //    BearerFormat = "JWT"
    //});

    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/oauth2/v2.0/authorize"),
                TokenUrl = new Uri($"https://login.microsoftonline.com/{builder.Configuration["AzureAd:TenantId"]}/oauth2/v2.0/token"),
                Scopes = new Dictionary<string, string>
            {
                { $"{builder.Configuration["AzureAd:Audience"]}/access_as_user", "Access API as user" }
            }
            }
        }
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "oauth2"
            }
        },
        new[] { $"{builder.Configuration["AzureAd:Audience"]}/access_as_user" }
    }
});

});

// Configure JWT authentication for SSO validation
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        var secretKey = builder.Configuration["JWT:SecretKey"];
//        if (!string.IsNullOrWhiteSpace(secretKey))
//        {
//            options.TokenValidationParameters = new TokenValidationParameters
//            {
//                ValidateIssuerSigningKey = true,
//                IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secretKey)),
//                ValidateIssuer = true,
//                ValidIssuer = builder.Configuration["JWT:Issuer"],
//                ValidateAudience = true,
//                ValidAudience = builder.Configuration["JWT:Audience"],
//                ValidateLifetime = true,
//                ClockSkew = TimeSpan.FromMinutes(5),
//                RequireExpirationTime = true
//            };
//        }
//    });
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization();

// Configure Entity Framework with Microsoft's official SQL Server provider for Databricks
// Databricks SQL Warehouses are compatible with SQL Server driver
if (!useOdbcRepository)
{
	builder.Services.AddDbContext<ApplicationDbContext>(options =>
	{
		options.UseDatabricks(
			builder.Configuration.GetConnectionString("DefaultConnection")
		);
		if (builder.Environment.IsDevelopment())
		{
			options.EnableSensitiveDataLogging();
			options.EnableDetailedErrors();
		}
	});
}

// Configure MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateOutletCommand).Assembly);
});

// Configure FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(CreateOutletCommandValidator).Assembly);

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Register Databricks SQL REST client
builder.Services.AddHttpClient<ImperialBackend.Infrastructure.Data.DatabricksSqlRestClient>();

// Register repositories (use EF context for non-REST operations still in class)
builder.Services.AddScoped<IOutletRepository, OutletRepository>();

// Configure CORS for frontend integration
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ??
                new[] { "http://localhost:3000", "https://localhost:3000" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// Add health checks for Entity Framework
var healthChecks = builder.Services.AddHealthChecks();
if (!useOdbcRepository)
{
	healthChecks.AddDbContextCheck<ApplicationDbContext>("databricks");
}

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    //Set Default Api Version
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0); //v1.0

    //use default version when not specified 
    options.AssumeDefaultVersionWhenUnspecified = true;

    //configure how version is read from requests
    options.ApiVersionReader = ApiVersionReader.Combine(
        new QueryStringApiVersionReader("version"),  // ?version=1.0
        new HeaderApiVersionReader("X-Version"),     // X-Version: 1.0
        new UrlSegmentApiVersionReader()             // /api/v1.0/auth/me for ex
        );

    //configure version format
    options.ApiVersionSelector = new CurrentImplementationApiVersionSelector(options);
});



var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Imperial Backend API v1");

        c.RoutePrefix = "swagger";

        // OAuth2 config
        c.OAuthClientId(builder.Configuration["AzureAd:ClientId"]);
        c.OAuthUsePkce();
        c.OAuthScopes($"{builder.Configuration["AzureAd:Audience"]}/access_as_user");
        c.OAuthAppName("Imperial Backend API - Swagger");
        c.OAuth2RedirectUrl("https://localhost:62399/swagger/oauth2-redirect.html");
    });
}


// Security headers middleware
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseHttpsRedirection();

// Enable CORS
app.UseCors("FrontendPolicy");

// Add request/response logging middleware
app.UseMiddleware<RequestLoggingMiddleware>();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Health checks
app.MapHealthChecks("/health");

// API Controllers
app.MapControllers();

try
{
    Log.Information("Imperial Backend API with Entity Framework Core + Databricks starting up...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}