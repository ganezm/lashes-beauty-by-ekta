using LashesWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace LashesWebsite.Controllers;

[Route("services")]
public sealed class ServicesController(IGessFlowApiClient api) : SiteControllerBase(api)
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = PageTitle("title.services", $"Services — {SiteContent!.BusinessName}");
        return View(Storefront);
    }
}
