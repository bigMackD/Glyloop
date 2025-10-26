using System.Reflection;
using Microsoft.OpenApi.Models;

namespace Glyloop.API.Configuration;

/// <summary>
/// Configures Swagger/OpenAPI documentation for the API.
/// </summary>
public static class SwaggerConfiguration
{
    /// <summary>
    /// Adds Swagger generation services with JWT authentication support.
    /// </summary>
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Glyloop API",
                Version = "v1",
                Description = "REST API for Glyloop - Type 1 Diabetes management application. " +
                              "Provides endpoints for authentication, event logging, glucose data visualization, and Dexcom integration.",
                Contact = new OpenApiContact
                {
                    Name = "Glyloop Support",
                    Email = "support@glyloop.com"
                }
            });

            // Include XML comments for rich documentation
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // Add JWT authentication to Swagger UI
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme. " +
                              "Note: In production, JWT is stored in httpOnly cookies, but for Swagger testing use: Bearer {token}"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Support for polymorphic response types
            options.UseAllOfToExtendReferenceSchemas();
        });

        return services;
    }
}

