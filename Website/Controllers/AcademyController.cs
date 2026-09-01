using LashesWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace LashesWebsite.Controllers;

[Route("academy")]
public sealed class AcademyController(IGessFlowApiClient api) : SiteControllerBase(api)
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = $"Academy — {SiteContent!.BusinessName}";
        return View(SiteContent);
    }
}
