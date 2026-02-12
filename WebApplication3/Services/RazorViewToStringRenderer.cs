using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc;

namespace WebApplication3.Services;

public interface IRazorViewToStringRenderer
{
    Task<string> RenderViewToStringAsync<TModel>(string viewPath, TModel model);
}

public class RazorViewToStringRenderer : IRazorViewToStringRenderer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ICompositeViewEngine _viewEngine;

    public RazorViewToStringRenderer(IServiceProvider serviceProvider, ICompositeViewEngine viewEngine)
    {
        _serviceProvider = serviceProvider;
        _viewEngine = viewEngine;
    }

    public async Task<string> RenderViewToStringAsync<TModel>(string viewPath, TModel model)
    {
        using var scope = _serviceProvider.CreateScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };

        var actionContext = new Microsoft.AspNetCore.Mvc.ActionContext(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());

        var viewResult = _viewEngine.GetView(executingFilePath: null, viewPath: viewPath, isMainPage: true);
        if (!viewResult.Success)
        {
            throw new InvalidOperationException($"Could not find view '{viewPath}'");
        }

        var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = model
        };

        await using var sw = new StringWriter();
        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewDictionary,
            new TempDataDictionary(actionContext.HttpContext, scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>()),
            sw,
            new HtmlHelperOptions()
        );

        await viewResult.View.RenderAsync(viewContext);
        return sw.ToString();
    }
}
