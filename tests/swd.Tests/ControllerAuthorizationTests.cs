using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using swd.Presentation.Controllers;

namespace swd.Tests;

public class ControllerAuthorizationTests
{
    [Theory]
    [InlineData(typeof(ProductsController), nameof(ProductsController.Create))]
    [InlineData(typeof(ProductsController), nameof(ProductsController.Update))]
    [InlineData(typeof(ProductsController), nameof(ProductsController.Delete))]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.Create))]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.Update))]
    [InlineData(typeof(CategoriesController), nameof(CategoriesController.Delete))]
    public void MutatingAdminEndpoints_ShouldRequireAdminOrStaff(Type controllerType, string methodName)
    {
        var method = controllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);

        Assert.NotNull(method);

        var authorize = method!.GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(authorize);
        Assert.Equal("Admin,Staff", authorize!.Roles);
    }
}
