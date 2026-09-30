using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace IskurAuto.API.Infrastructure;

/// <summary>
/// Swashbuckle'ın ürettiği OpenAPI sürümünü "3.0.1" olarak sabitler.
/// Swashbuckle 6.x bazı ortamlarda "3.0.4" üretir; bu değer eski
/// Swagger UI sürümleriyle uyumsuz olabilir.
/// </summary>
public class OpenApiVersionFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Swagger UI "Unable to render" hatasını önlemek için sürümü sabitle
        swaggerDoc.Info.Version = "v1";
    }
}
