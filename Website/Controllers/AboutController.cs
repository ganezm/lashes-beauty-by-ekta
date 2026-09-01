using LashesWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace LashesWebsite.Controllers;

[Route("about")]
public sealed class AboutController(IGessFlowApiClient api) : SiteControllerBase(api)
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = $"About — {SiteContent!.BusinessName}";
        return View(SiteContent);
    }
}
