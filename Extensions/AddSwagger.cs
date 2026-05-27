

using Microsoft.OpenApi.Models;
namespace EVerland.Extentions;

public static class SwaggerExtension
{
    public static IServiceCollection AddCustomSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.AddCustome();
        });

        return services;
    }

    public static void AddCustome(this Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
    {
        options.CustomSchemaIds(type =>
        {
            // Helper: prefix theo module để tránh trùng enum/class cùng tên
            static string PrefixByModule(Type t, string baseName)
            {
                var ns = t.Namespace ?? "";
                if (ns.StartsWith("Modules.Payment")) return "Payment_" + baseName;
                if (ns.StartsWith("Modules.Order")) return "Order_" + baseName;
                if (ns.StartsWith("Modules.User")) return "User_" + baseName;
                if (ns.StartsWith("SharedKernel")) return "Shared_" + baseName;
                return baseName;
            }

            if (!type.IsGenericType)
            {
                // Non-generic: dùng Name + prefix module
                return PrefixByModule(type, type.Name);
            }

            // Generic: PageResult_OrderOverviewResponseDto (có prefix module của generic type)
            var genericName = type.GetGenericTypeDefinition().Name.Split('`')[0];
            genericName = PrefixByModule(type, genericName);

            var args = string.Join("_",
                type.GetGenericArguments().Select(a =>
                {
                    // mỗi generic arg cũng nên unique (lỡ arg trùng tên giữa module)
                    var argBase = a.IsGenericType
                        ? a.GetGenericTypeDefinition().Name.Split('`')[0]
                        : a.Name;

                    return PrefixByModule(a, argBase);
                }));

            return $"{genericName}_{args}";
        });

        options.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
        options.SwaggerDoc("v2", new OpenApiInfo { Title = "API", Version = "v2" });

        options.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    }

}

public static class WebApplicationExtension
{
    public static WebApplication UseCustomSwagger(this WebApplication app)
    {
        app.MapGet("/swagger-download/v1", async (HttpContext context) =>
        {
            var url = $"{context.Request.Scheme}://{context.Request.Host}/swagger/v1/swagger.json";

            using var httpClient = new HttpClient();
            var json = await httpClient.GetStringAsync(url);

            return Results.File(
                System.Text.Encoding.UTF8.GetBytes(json),
                "application/json",
                "swagger-v1.json");
        });

        app.UseSwagger(options =>
        {
            options.RouteTemplate = "swagger/{documentName}/swagger.json";
        });

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "API V1");
            options.SwaggerEndpoint("/swagger/v2/swagger.json", "API V2");
            options.RoutePrefix = "api-docs";
        });

        return app;
    }
}
public static class WebApplicationBuilderExtension
{
    public static WebApplicationBuilder AddSwagger(this WebApplicationBuilder builder)
    {
        builder.Services.AddCustomSwagger();
        return builder;
    }
}