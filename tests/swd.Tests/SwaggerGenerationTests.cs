using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using swd.Presentation.Controllers;

namespace swd.Tests;

public class SwaggerGenerationTests
{
    [Fact]
    public void SwaggerDocument_ShouldGenerate_ForAiControllerMultipartEndpoint()
    {
        var swagger = CreateSwagger();

        Assert.Contains("/api/ai/recommend", swagger.Paths.Keys);
    }

    [Fact]
    public void SwaggerDocument_ShouldNotExpose_VirtualTryOnEndpoint()
    {
        var swagger = CreateSwagger();

        Assert.DoesNotContain("/api/ai/virtual-try-on", swagger.Paths.Keys);
    }

    private static OpenApiDocument CreateSwagger()
    {
        var services = new ServiceCollection();
        var environment = new FakeHostEnvironment();
        services.AddSingleton<IHostEnvironment>(environment);
        services.AddSingleton<IWebHostEnvironment>(environment);
        services.AddLogging();
        services.AddEndpointsApiExplorer();
        services.AddControllers()
            .AddApplicationPart(typeof(AiController).Assembly);
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SWD API",
                Version = "v1"
            });
        });

        using var serviceProvider = services.BuildServiceProvider();
        var swaggerProvider = serviceProvider.GetRequiredService<ISwaggerProvider>();

        var swagger = swaggerProvider.GetSwagger("v1");

        Assert.NotNull(swagger);
        return swagger;
    }

    private sealed class FakeHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "swd.Tests";

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = Path.GetTempPath();

        public string EnvironmentName { get; set; } = Environments.Development;

        public string ContentRootPath { get; set; } = Path.GetTempPath();

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
