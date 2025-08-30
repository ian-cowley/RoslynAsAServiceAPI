using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynAsAServiceAPI;

// --- Reusable Endpoint Filter for Security ---
public class ApiKeyEndpointFilter : IEndpointFilter
{
    private readonly IConfiguration _configuration;

    // Use dependency injection to get the configuration
    public ApiKeyEndpointFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // Get the required key from appsettings.json
        var apiKey = _configuration["ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            return Results.Problem("API Key is not configured on the server.", statusCode: 500);
        }

        if (!context.HttpContext.Request.Headers.TryGetValue("X-API-Key", out var extractedApiKey) || apiKey != extractedApiKey)
        {
            return Results.Unauthorized();
        }

        return await next(context);
    }
}
