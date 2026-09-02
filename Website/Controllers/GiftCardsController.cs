using LashesWebsite.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace LashesWebsite.Controllers;

/// The real "buy a gift card" flow -- the site's own JavaScript calls GessFlow's public API
/// directly (same client-direct pattern book.js already uses for booking), so this controller only
/// needs to render the two pages and inject the API base/slug, exactly like BookController.
[Route("gift-cards")]
public sealed class GiftCardsController(IGessFlowApiClient api, IOptions<GessFlowApiOptions> options) : SiteControllerBase(api)
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = $"Gift Cards — {SiteContent!.BusinessName}";
        ViewData["GessFlowApiBase"] = options.Value.BaseUrl;
        ViewData["GessFlowSlug"] = options.Value.Slug;
        return View(SiteContent);
    }

    [HttpGet("thank-you")]
    public IActionResult ThankYou()
    {
        ViewData["Title"] = $"Thank you — {SiteContent!.BusinessName}";
        ViewData["GessFlowApiBase"] = options.Value.BaseUrl;
        ViewData["GessFlowSlug"] = options.Value.Slug;
        return View(SiteContent);
    }
}
