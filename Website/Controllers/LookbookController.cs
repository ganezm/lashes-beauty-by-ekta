using LashesWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace LashesWebsite.Controllers;

[Route("lookbook")]
public sealed class LookbookController(IGessFlowApiClient api) : SiteControllerBase(api)
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = $"Lookbook — {SiteContent!.BusinessName}";
        return View(SiteContent);
    }
}
