using LashesWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace LashesWebsite.Controllers;

[Route("contact")]
public sealed class ContactController(IGessFlowApiClient api) : SiteControllerBase(api)
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = $"Contact — {SiteContent!.BusinessName}";
        return View(new ContactViewModel(SiteContent!, Storefront!));
    }
}

public sealed record ContactViewModel(Models.PublicWebsiteContent Content, Models.PublicBookingStorefront Storefront);
