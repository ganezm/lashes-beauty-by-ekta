using LashesWebsite.Models;
using LashesWebsite.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LashesWebsite.Controllers;

/// Every real page controller inherits this. Fetches both public GessFlow payloads once per
/// request (site content: hero/gallery/team/academy/etc, and the booking storefront: services/
/// employees/policy) before the action runs, exposing them to the shared _Layout via ViewData and
/// to the page's own view via the protected properties. If either call fails, every page renders
/// an honest "temporarily unavailable" partial instead of guessing at fallback content -- unlike
/// the original static prototype's hardcoded-array fallback, that isn't appropriate for a real site.
public abstract class SiteControllerBase(IGessFlowApiClient api) : Controller
{
    protected PublicWebsiteContent? SiteContent { get; private set; }
    protected PublicBookingStorefront? Storefront { get; private set; }
    protected bool Unavailable { get; private set; }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var ct = HttpContext.RequestAborted;
        var contentTask = api.GetWebsiteContentAsync(ct);
        var storefrontTask = api.GetStorefrontAsync(ct);
        await Task.WhenAll(contentTask, storefrontTask);
        SiteContent = contentTask.Result;
        Storefront = storefrontTask.Result;
        Unavailable = SiteContent is null || Storefront is null;

        ViewData["Content"] = SiteContent;
        ViewData["Storefront"] = Storefront;

        if (Unavailable)
        {
            context.Result = View("~/Views/Shared/Unavailable.cshtml");
            return;
        }

        await next();
    }
}
