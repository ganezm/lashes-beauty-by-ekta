using LashesWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace LashesWebsite.Controllers;

[Route("lookbook")]
public sealed class LookbookController(IGessFlowApiClient api) : SiteControllerBase(api)
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = PageTitle("title.lookbook", $"Lookbook — {SiteContent!.BusinessName}");
        ViewData["Description"] = $"Real before and after photos of classic, hybrid, volume and mega volume lashes by {SiteContent!.BusinessName}.";
        return View(SiteContent);
    }
}
