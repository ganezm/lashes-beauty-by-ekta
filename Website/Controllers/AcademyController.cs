using LashesWebsite.Services;
using Microsoft.AspNetCore.Mvc;

namespace LashesWebsite.Controllers;

[Route("academy")]
public sealed class AcademyController(IGessFlowApiClient api) : SiteControllerBase(api)
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = PageTitle("title.academy", $"Academy — {SiteContent!.BusinessName}");
        ViewData["Description"] = $"Lash artistry training at {SiteContent!.BusinessName}: what the course covers, intake dates and how to enquire.";
        return View(SiteContent);
    }
}
