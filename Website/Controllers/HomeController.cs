using LashesWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace LashesWebsite.Controllers;

public sealed class HomeController(IGessFlowApiClient api) : SiteControllerBase(api)
{
    public IActionResult Index()
    {
        ViewData["Title"] = SiteContent!.BusinessName;
        return View(new HomeViewModel(SiteContent!, Storefront!));
    }
}

public sealed record HomeViewModel(Models.PublicWebsiteContent Content, Models.PublicBookingStorefront Storefront);
